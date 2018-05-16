using System.Collections;
using System.Collections.Generic;

namespace AssureCare.VstsTools.VariableGroupCopier
{
    /// <summary>
    /// Parameter model extensions
    /// </summary>
    internal static class ParametersExt
    {
        /// <summary>
        /// Generates log context for model
        /// </summary>
        public static IDictionary GenerateLogContext(this Parameters param)
        {
            return new Dictionary<string, string>
            {
                { "Account", param.Account },
                { "SourceProject", param.SourceProject },
                { "SourceGroup", param.SourceGroup },
                { "TargetProject", param.TargetProject },
                { "TargetGroup", param.TargetGroup }
            };
        }
    }
}