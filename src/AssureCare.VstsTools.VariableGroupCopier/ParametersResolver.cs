using System;
using System.Collections.Generic;
using System.IO;
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

        private readonly IKnownLiterals _knownLiterals;

        private readonly IDictionary<ParameterPosition, string> _parameterPrompts;

        public ParametersResolver([NotNull] IUserConsole console, [NotNull] IKnownLiterals knownLiterals)
        {
            _console = console ?? throw new ArgumentNullException(nameof(console));

            _knownLiterals = knownLiterals ?? throw new ArgumentNullException(nameof(knownLiterals));

            _parameterPrompts = new Dictionary<ParameterPosition, string>
            {
                { ParameterPosition.Account, "VSTS account" },
                { ParameterPosition.OverrideExistentTarget, $"Override existent target {YesNoPrompt}" },
                { ParameterPosition.SourceGroup, "Source group name" },
                { ParameterPosition.SourceProject, $"Source project or '{_knownLiterals.GroupLocationFileSelector}'" },
                { ParameterPosition.TargetGroup, "Target group name" },
                { ParameterPosition.TargetProject, $"Target project or '{_knownLiterals.GroupLocationFileSelector}'" },
                { ParameterPosition.Token, "VSTS access token" }
            };
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

            _parameterPrompts[ParameterPosition.SourceGroup] = IsGroupLocationFile(result.SourceProject)
                ? "Source file name or folder name"
                : "Source group name";

            result.SourceGroup = AcquireParameter(commandArgs, ParameterPosition.SourceGroup, previousParameters?.SourceGroup);

            result.TargetProject = AcquireParameter(commandArgs, ParameterPosition.TargetProject,
                previousParameters?.TargetProject ?? EmptyToNull(Properties.Settings.Default.DefaultProject) ?? result.SourceProject);

            _parameterPrompts[ParameterPosition.TargetGroup] = IsGroupLocationFile(result.TargetProject)
                ? "Target folder name"
                : "Target group name or prefix";

            result.TargetGroup = AcquireParameter(commandArgs, ParameterPosition.TargetGroup, GenerateDefaultTargetGroup(result));

            return result;
        }

        public bool ConfirmOverride(IReadOnlyList<string> commandArgs, string targetGroupName)
        {
            _parameterPrompts[ParameterPosition.OverrideExistentTarget] =
                $"Override existent target {targetGroupName} {YesNoPrompt}";

            return AcquireParameter(commandArgs, ParameterPosition.OverrideExistentTarget)
                .Equals("Y", StringComparison.InvariantCultureIgnoreCase);
        }

        private string AcquireParameter(IReadOnlyList<string> commandArgs, ParameterPosition position, string defaultValue = null)
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

            _console.Write(_parameterPrompts[position]);

            if (!string.IsNullOrWhiteSpace(defaultValue)) _console.Write($" or ENTER to accept default ({defaultValue})");
            _console.Write(":");

            var result = _console.ReadLine();

            return string.IsNullOrEmpty(result) ? defaultValue : result;
        }

        private static string EmptyToNull(string value) => !string.IsNullOrWhiteSpace(value) ? value : null;

        private string GenerateDefaultTargetGroup(Parameters param)
        {
            return IsGroupLocationFile(param.TargetProject) ? $"{param.SourceGroup}{_knownLiterals.FileExt}" :
                param.SourceProject.Equals(param.TargetProject, StringComparison.InvariantCultureIgnoreCase) ? $"{param.SourceGroup} - Cloned" :
                $"{param.SourceGroup}";
        }

        public bool IsGroupLocationFile(string project) => _knownLiterals.GroupLocationFileSelector.Equals(project, StringComparison.InvariantCultureIgnoreCase);

        public bool IsGroupNamePattern(string name) => _knownLiterals.AllGroupsSelector.Equals(name, StringComparison.InvariantCultureIgnoreCase);
        
        public string ValidateParameters(Parameters parameters)
        {
            if (IsGroupLocationFile(parameters.SourceProject) && IsGroupLocationFile(parameters.TargetProject))
                return "Source and target cannot be both files";

            if (parameters.SourceProject.Equals(parameters.TargetProject,
                    StringComparison.InvariantCultureIgnoreCase) &&
                parameters.SourceGroup.Equals(parameters.TargetGroup, StringComparison.InvariantCultureIgnoreCase))
                return "Source and target cannot be the same";

            if (IsGroupLocationFile(parameters.SourceProject) && IsGroupNamePattern(parameters.SourceGroup) ||
                IsGroupLocationFile(parameters.TargetProject) && IsGroupNamePattern(parameters.TargetGroup))
                return $"File location cannon be combined with multiple selector {_knownLiterals.AllGroupsSelector}";

            // Make sure we don't allow to override the group with itself in this case: vsts_project1/group1 => vsts_project1/*
            if (!IsGroupLocationFile(parameters.SourceProject) && !IsGroupNamePattern(parameters.SourceGroup) &&
                parameters.SourceProject.Equals(parameters.TargetProject, StringComparison.InvariantCultureIgnoreCase) && IsGroupNamePattern(parameters.TargetGroup))
                return "Source and target cannot be the same";

            return null;
        }

        public string GenerateActualTargetGroupName(string actualSourceGroupName, Parameters parameters)
        {
            // If target is requested to be the same as source (target group equals *)
            if (IsGroupNamePattern(parameters.TargetGroup)) return actualSourceGroupName;

            // If target is file, use target group as folder name and source group as file name
            if (IsGroupLocationFile(parameters.TargetProject))
                return Path.Combine(parameters.TargetGroup, $"{actualSourceGroupName}{_knownLiterals.FileExt}");

            // If the source is single group use target
            if (!IsGroupLocationFile(parameters.SourceProject) &&
                !IsGroupNamePattern(parameters.SourceGroup))
                return parameters.TargetGroup;

            // In remaining cases we treat input target group as prefix
            return $"{parameters.TargetGroup}{actualSourceGroupName}";
        }
    }
}