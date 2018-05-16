using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using NLog;
using NLog.Fluent;

namespace AssureCare.VstsTools.VariableGroupCopier
{
    class Program
    {
        private const string UrlFormat = "https://{0}.visualstudio.com/";

        private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

        private static readonly IUserConsole UserConsole = new UserConsole();

        private static readonly IParametersResolver ParametersResolver = new ParametersResolver(UserConsole);

        static void Main(string[] args)
        {
            var parameters = ParametersResolver.AcquireInitialParameters(args);

            CopyVariableGroupAsync(parameters, p => ParametersResolver.PromptNextParameters(p),
                p => ParametersResolver.AcquireParameter(args, ParameterPosition.OverrideExistentTarget)
                    .Equals("Y", StringComparison.InvariantCultureIgnoreCase)).SyncResult();

            if (ParametersResolver.InteractiveMode) UserConsole.WaitAnyKey();
        }

        private static async Task CopyVariableGroupAsync(Parameters param, Func<Parameters, Parameters> nextParamFunc,
            Func<Parameters, bool> overrideExistingTargetGroup)
        {
            VssConnection connection = null;

            var logContext = param.GenerateLogContext();

            try
            {
                connection = new VssConnection(new Uri(string.Format(UrlFormat, param.Account)),
                    new VssBasicCredential(string.Empty, param.Token));

                using (var client = await connection.GetClientAsync<TaskAgentHttpClient>())
                {
                    do
                    {
                        var sourceVariableGroup =
                            (await client.GetVariableGroupsAsync(param.SourceProject, param.SourceGroup))
                            .SingleOrDefault();

                        if (sourceVariableGroup == null)
                        {
                            Logger.Error().Properties(logContext)
                                .Message("Cannot find the source group")
                                .Write();
                            continue;
                        }

                        // Check if the target group already exists
                        var targetVariableGroup =
                            (await client.GetVariableGroupsAsync(param.TargetProject, param.TargetGroup))
                            .SingleOrDefault();

                        if (targetVariableGroup != null)
                        {
                            if (overrideExistingTargetGroup(param))
                                await client.DeleteVariableGroupAsync(param.TargetProject, targetVariableGroup.Id);
                            else
                            {
                                Logger.Info().Properties(logContext)
                                    .Message("Skipping existent target group")
                                    .Write();
                                continue;
                            }
                        }

                        targetVariableGroup = VariableGroupUtility
                            .CloneVariableGroups(new List<VariableGroup> {sourceVariableGroup}).First();

                        targetVariableGroup.Name = param.TargetGroup;

                        await client.AddVariableGroupAsync(param.TargetProject, targetVariableGroup);

                        Logger.Info().Properties(logContext).Message("Successfully cloned variable group");
                    } while ((param = nextParamFunc(param)) != null);
                }
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
                connection?.Disconnect();
            }
        }
    }
}
