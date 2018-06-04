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

        [CanBeNull]
        Parameters AcquireParameters([NotNull] IReadOnlyList<string> commandArgs, Parameters previousParameters = null);

        [NotNull]
        string AcquireParameter([NotNull] IReadOnlyList<string> commandArgs, ParameterPosition position, string defaultValue = null);
        
        bool IsGroupLocationFile(string project);

        string ValidateParameters(Parameters parameters);
    }
}