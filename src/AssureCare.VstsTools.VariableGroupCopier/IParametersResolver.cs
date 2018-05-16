using System.Collections.Generic;
using JetBrains.Annotations;

namespace AssureCare.VstsTools.VariableGroupCopier
{
    /// <summary>
    /// Contract for resolving input parameters
    /// </summary>
    internal interface IParametersResolver
    {
        bool InteractiveMode { get; }

        [NotNull]
        Parameters AcquireInitialParameters([NotNull] IReadOnlyList<string> commandArgs);

        [NotNull]
        string AcquireParameter([NotNull] IReadOnlyList<string> commandArgs, ParameterPosition position, string defaultValue = null);

        [CanBeNull]
        Parameters PromptNextParameters([NotNull] Parameters previousParameters);
    }
}