using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using NLog;
using NLog.Fluent;

namespace AssureCare.VstsTools.VariableGroupCopier
{
    internal class VariableGroupCopier : IVariableGroupCopier
    {
        private readonly IParametersResolver _parametersResolver;
        private readonly Func<string, string, VariableGroupVstsRepository> _vstsRepositoryFactoryFunc;
        private readonly Func<VariableGroupFileRepository> _fileRepositoryFactoryFunc;
        
        private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

        public VariableGroupCopier([NotNull] IParametersResolver parametersResolver,
            [NotNull] Func<string, string, VariableGroupVstsRepository> vstsRepositoryFactoryFunc,
            [NotNull] Func<VariableGroupFileRepository> fileRepositoryFactoryFunc)
        {
            _parametersResolver = parametersResolver ?? throw new ArgumentNullException(nameof(parametersResolver));
            _vstsRepositoryFactoryFunc = vstsRepositoryFactoryFunc ?? throw new ArgumentNullException(nameof(vstsRepositoryFactoryFunc));
            _fileRepositoryFactoryFunc = fileRepositoryFactoryFunc ?? throw new ArgumentNullException(nameof(fileRepositoryFactoryFunc));
        }

        public void Copy(string[] args)
        {
            var parameters = _parametersResolver.AcquireParameters(args);

            // Proceed only if no validation errors
            if (ValidateParameters(parameters))
            {
                CopyVariableGroupAsync(parameters, p =>
                    {
                        p = _parametersResolver.AcquireParameters(args, p);
                        return ValidateParameters(p) ? p : null;
                    },

                    t => _parametersResolver.ConfirmOverride(args, t));
            }
        }

        private bool ValidateParameters(Parameters param)
        {
            if (param == null) return false;

            var errorMessage = _parametersResolver.ValidateParameters(param);

            var hasErrors = !string.IsNullOrEmpty(errorMessage);

            if (hasErrors)
            {
                Logger.Error().Property("Error", errorMessage)
                    .Message("Incorrect input parameters")
                    .Write();
            }

            return !hasErrors;
        }

        private void CopyVariableGroupAsync(Parameters param, Func<Parameters, Parameters> nextParamFunc,
            Func<string, bool> overrideExistingTargetGroup)
        {
            VariableGroupVstsRepository vstsRepository = null;

            var logContext = param.GenerateLogContext();

            try
            {
                vstsRepository = _vstsRepositoryFactoryFunc(param.Account, param.Token);
                var fileRepository = _fileRepositoryFactoryFunc();

                do
                {
                    logContext = param.GenerateLogContext();

                    var sourceRepository = ChooseRepository(param.SourceProject, fileRepository, vstsRepository);
                    var targeRepository = ChooseRepository(param.TargetProject, fileRepository, vstsRepository);

                    // Obtain all source groups
                    var sourceGroups = sourceRepository.GetAll(param.SourceProject, param.SourceGroup).ToArray();

                    if (!sourceGroups.Any())
                    {
                        Logger.Warn().Properties(logContext)
                            .Message("Cannot find any matching source groups")
                            .Write();
                        continue;
                    }
                    
                    foreach (var sourceVariableGroup in sourceGroups)
                    {
                        // Generate actual target name based on actual source group name and input parameters
                        var targetGroupName = _parametersResolver.GenerateActualTargetGroupName(sourceVariableGroup.Name, param);

                        logContext["ActualSourceGroup"] = sourceVariableGroup.Name;
                        logContext["ActualTargetGroup"] = targetGroupName;

                        Logger.Info().Properties(logContext).Message("Trying to copy a variable group").Write();

                        // Check if the target group already exists
                        var targetVariableGroup = targeRepository.Get(param.TargetProject, targetGroupName);
                        if (targetVariableGroup != null)
                        {
                            // Check if we should override the target
                            if (overrideExistingTargetGroup(targetGroupName))
                            {
                                targeRepository.Delete(param.TargetProject, targetVariableGroup.Id);
                            }

                            else
                            {
                                Logger.Info().Properties(logContext)
                                    .Message("Skipping existent target group")
                                    .Write();
                                continue;
                            }
                        }

                        targetVariableGroup = VariableGroupUtility
                            .CloneVariableGroups(new List<VariableGroup> { sourceVariableGroup }).First();

                        targeRepository.Add(param.TargetProject, targetGroupName, targetVariableGroup);

                        Logger.Info().Properties(logContext).Message("Successfully copied a variable group").Write();
                    }
                } while ((param = nextParamFunc(param)) != null);
            }
            catch (Exception ex)
            {
                Logger.Error().Exception(ex)
                    .Properties(logContext)
                    .Message("Error while copying a variable group")
                    .Write();
            }
            finally
            {
                vstsRepository?.Dispose();
            }
        }

        private IVariableGroupRepository ChooseRepository(string project, VariableGroupFileRepository fileRepository, VariableGroupVstsRepository vstsRepository)
        {
            if (_parametersResolver.IsGroupLocationFile(project)) return fileRepository;

            return vstsRepository;
        }
    }
}