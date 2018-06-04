using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using NLog;
using NLog.Fluent;

namespace AssureCare.VstsTools.VariableGroupCopier
{
    public class Program
    {
        private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

        private static readonly IUserConsole UserConsole = new UserConsole();

        private static readonly IParametersResolver ParametersResolver = new ParametersResolver(UserConsole);

        static void Main(string[] args)
        {
            var parameters = ParametersResolver.AcquireParameters(args);
            
            // Proceed only if no validation errors
            if (ValidateParameters(parameters))
            {
                CopyVariableGroupAsync(parameters, p =>
                    {
                        p = ParametersResolver.AcquireParameters(args, p);
                        return ValidateParameters(p) ? p : null;
                    },

                    p => ParametersResolver.AcquireParameter(args, ParameterPosition.OverrideExistentTarget)
                        .Equals("Y", StringComparison.InvariantCultureIgnoreCase));
            }
            
            if (ParametersResolver.InteractiveMode) UserConsole.WaitAnyKey();
        }

        private static bool ValidateParameters(Parameters param)
        {
            if (param == null) return false;

            var errorMessage = ParametersResolver.ValidateParameters(param);

            var hasErrors = !string.IsNullOrEmpty(errorMessage);

            if (hasErrors)
            {
                Logger.Error().Property("Error", errorMessage)
                    .Message("Incorrect input parameters")
                    .Write();
            }

            return !hasErrors;
        }

        private static void CopyVariableGroupAsync(Parameters param, Func<Parameters, Parameters> nextParamFunc,
            Func<Parameters, bool> overrideExistingTargetGroup)
        {
            VariableGroupVstsRepository vstsRepository = null;

            var logContext = param.GenerateLogContext();

            try
            {
                vstsRepository = new VariableGroupVstsRepository(param.Account, param.Token);
                var fileRepository = new VariableGroupFileRepository();

                do
                {
                    logContext = param.GenerateLogContext();

                    var sourceRepository = ChooseRepository(param.SourceProject, fileRepository, vstsRepository);
                    var targeRepository = ChooseRepository(param.TargetProject, fileRepository, vstsRepository);

                    var sourceVariableGroup = sourceRepository.Get(param.SourceProject, param.SourceGroup);

                    if (sourceVariableGroup == null)
                    {
                        Logger.Error().Properties(logContext)
                            .Message("Cannot find the source group")
                            .Write();
                        continue;
                    }

                    // Check if the target group already exists
                    var targetVariableGroup = targeRepository.Get(param.TargetProject, param.TargetGroup);

                    if (targetVariableGroup != null)
                    {
                        if (overrideExistingTargetGroup(param))
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

                    targeRepository.Add(param.TargetProject, param.TargetGroup, targetVariableGroup);

                    Logger.Info().Properties(logContext).Message("Successfully cloned variable group");
                } while ((param = nextParamFunc(param)) != null);
            }
            catch (Exception ex)
            {
                Logger.Error().Exception(ex)
                    .Properties(logContext)
                    .Message("Error while cloning variable group")
                    .Write();
            }
            finally
            {
                vstsRepository?.Dispose();
            }
        }

        private static IVariableGroupRepository ChooseRepository(string project, VariableGroupFileRepository fileRepository, VariableGroupVstsRepository vstsRepository)
        {
            if (ParametersResolver.IsGroupLocationFile(project)) return fileRepository;

            return vstsRepository;
        }
    }
}
