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

        private static readonly IDictionary<ParameterPosition, string> ParameterPrompts = new Dictionary<ParameterPosition, string>
        {
            { ParameterPosition.Account, "VSTS Account" },
            { ParameterPosition.OverrideExistentTarget, $"Override existent target {YesNoPrompt}" },
            { ParameterPosition.SourceGroup, "Source Group" },
            { ParameterPosition.SourceProject, "Source Project" },
            { ParameterPosition.TargetGroup, "Target Group" },
            { ParameterPosition.TargetProject, "Target Project" },
            { ParameterPosition.Token, "VSTS Access Token" }
        };

        public ParametersResolver([NotNull] IUserConsole console)
        {
            _console = console ?? throw new ArgumentNullException(nameof(console));
        }

        public bool InteractiveMode { get; private set; }

        public Parameters AcquireInitialParameters(IReadOnlyList<string> commandArgs)
        {
            var result = new Parameters
            {
                Account = EmptyToNull(Properties.Settings.Default.VstsAccount) ?? AcquireParameter(commandArgs, ParameterPosition.Account),
                Token = EmptyToNull(Properties.Settings.Default.VstsUserToken) ?? AcquireParameter(commandArgs, ParameterPosition.Token),
                SourceProject = AcquireParameter(commandArgs, ParameterPosition.SourceProject, Properties.Settings.Default.DefaultProject),
                SourceGroup = AcquireParameter(commandArgs, ParameterPosition.SourceGroup)
                
            };

            result.TargetProject = AcquireParameter(commandArgs, ParameterPosition.TargetProject,
                EmptyToNull(Properties.Settings.Default.DefaultProject) ?? result.SourceProject);
            result.TargetGroup = AcquireParameter(commandArgs, ParameterPosition.TargetGroup, GenerateDefaultTargetGroup(result.SourceGroup));

            return result;
        }

        public Parameters PromptNextParameters(Parameters previousParameters)
        {
            if (!InteractiveMode || !_console.ReadYesNo($"Continue {YesNoPrompt}")) return null;

            previousParameters.SourceProject = PromptParameter(ParameterPosition.SourceProject, previousParameters.SourceProject);
            previousParameters.SourceGroup = PromptParameter(ParameterPosition.SourceGroup, previousParameters.SourceGroup);
            previousParameters.TargetProject = PromptParameter(ParameterPosition.TargetProject, previousParameters.TargetProject);
            previousParameters.TargetGroup = PromptParameter(ParameterPosition.TargetGroup, GenerateDefaultTargetGroup(previousParameters.SourceGroup));

            return previousParameters;
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

        private static string GenerateDefaultTargetGroup(string sourceGroup) => $"{sourceGroup} - Cloned";
    }
}