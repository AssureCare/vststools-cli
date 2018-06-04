using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace AssureCare.VstsTools.VariableGroupCopier
{
    /// <summary>
    /// Implementation of <see cref="IParametersResolver"/> using the <see cref="IUserConsole"/>, command line arguments, and app.config settings
    /// </summary>
    internal class ParametersResolver : IParametersResolver
    {
        private readonly IUserConsole _console;

        private const string YesNoPrompt = "(Y/N)?";

        private const string GroupLocationIsFile = "/file";

        private const string AllGroups = "*";

        private static readonly IDictionary<ParameterPosition, string> ParameterPrompts = new Dictionary<ParameterPosition, string>
        {
            { ParameterPosition.Account, "VSTS account" },
            { ParameterPosition.OverrideExistentTarget, $"Override existent target {YesNoPrompt}" },
            { ParameterPosition.SourceGroup, "Source group name" },
            { ParameterPosition.SourceProject, $"Source project or '{GroupLocationIsFile}'" },
            { ParameterPosition.TargetGroup, "Target group name" },
            { ParameterPosition.TargetProject, $"Target project or '{GroupLocationIsFile}'" },
            { ParameterPosition.Token, "VSTS access token" }
        };

        public ParametersResolver([NotNull] IUserConsole console)
        {
            _console = console ?? throw new ArgumentNullException(nameof(console));
        }

        public bool InteractiveMode { get; private set; }

        public Parameters AcquireParameters(IReadOnlyList<string> commandArgs, Parameters previousParameters = null)
        {
            var result = previousParameters;

            // Check if this is not the first time we acquire parameters and the mode is not interactive or user dos not want to continue
            if (previousParameters != null)
            {
                if (!InteractiveMode || !_console.ReadYesNo($"Continue {YesNoPrompt}")) return null;

                previousParameters.SourceProject = PromptParameter(ParameterPosition.SourceProject, previousParameters.SourceProject);
            }
            else
            {
                result = new Parameters
                {
                    Account = EmptyToNull(Properties.Settings.Default.VstsAccount) ?? AcquireParameter(commandArgs, ParameterPosition.Account),
                    Token = EmptyToNull(Properties.Settings.Default.VstsUserToken) ?? AcquireParameter(commandArgs, ParameterPosition.Token),
                    SourceProject = AcquireParameter(commandArgs, ParameterPosition.SourceProject, Properties.Settings.Default.DefaultProject)
                };
            }
            
            ParameterPrompts[ParameterPosition.SourceGroup] = IsGroupLocationFile(result.SourceProject)
                ? "Source file name or folder name"
                : "Source group name";

            result.SourceGroup = AcquireParameter(commandArgs, ParameterPosition.SourceGroup, previousParameters?.SourceGroup);

            result.TargetProject = AcquireParameter(commandArgs, ParameterPosition.TargetProject,
                previousParameters?.TargetProject ?? EmptyToNull(Properties.Settings.Default.DefaultProject) ?? result.SourceProject);

            ParameterPrompts[ParameterPosition.TargetGroup] = IsGroupLocationFile(result.TargetProject)
                ? "Target file name or folder name"
                : "Target group name or prefix";

            result.TargetGroup = AcquireParameter(commandArgs, ParameterPosition.TargetGroup, GenerateDefaultTargetGroup(result));

            return result;
        }

        public string AcquireParameter(IReadOnlyList<string> commandArgs, ParameterPosition position, string defaultValue = null)
        {
            var index = (int)position;
            var result = index < commandArgs.Count ? commandArgs[index] : null;

            while (string.IsNullOrWhiteSpace(result))
            {
                result = PromptParameter(position, defaultValue);
            }

            return result;
        }

        private string PromptParameter(ParameterPosition position, string defaultValue = null)
        {
            // Since we have to prompt a parameter from user we are in the interactive mode now
            InteractiveMode = true;

            _console.Write(ParameterPrompts[position]);

            if (!string.IsNullOrWhiteSpace(defaultValue)) _console.Write($" or ENTER to accept default ({defaultValue})");
            _console.Write(":");

            var result = _console.ReadLine();

            return string.IsNullOrEmpty(result) ? defaultValue : result;
        }

        private static string EmptyToNull(string value) => !string.IsNullOrWhiteSpace(value) ? value : null;

        private string GenerateDefaultTargetGroup(Parameters param)
        {
            // If source is file and target is not then default to file name without extension


            return IsGroupLocationFile(param.TargetProject) ? $"{param.SourceGroup}.json" :
                param.SourceProject.Equals(param.TargetProject, StringComparison.InvariantCultureIgnoreCase) ? $"{param.SourceGroup} - Cloned" :
                $"{param.SourceGroup}";
        }

        public bool IsGroupLocationFile(string project) => GroupLocationIsFile.Equals(project, StringComparison.InvariantCultureIgnoreCase);

        public string ValidateParameters(Parameters parameters)
        {
            if (IsGroupLocationFile(parameters.SourceProject) && IsGroupLocationFile(parameters.TargetProject))
                return "Source and target cannot be both files";

            if (parameters.SourceProject.Equals(parameters.TargetProject,
                    StringComparison.InvariantCultureIgnoreCase) &&
                parameters.SourceGroup.Equals(parameters.TargetGroup, StringComparison.InvariantCultureIgnoreCase))
                return "Source and target cannot be the same";

            return null;
        }
    }
}