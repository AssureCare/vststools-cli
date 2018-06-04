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
        
        bool ConfirmOverride(IReadOnlyList<string> commandArgs, string targetGroupName);
        
        bool IsGroupLocationFile(string project);

        bool IsGroupNamePattern(string name);

        string ValidateParameters(Parameters parameters);

        string GenerateActualTargetGroupName(string actualSourceGroupName, Parameters parameters);
    }
}