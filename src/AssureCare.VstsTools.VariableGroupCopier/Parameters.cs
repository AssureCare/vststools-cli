namespace AssureCare.VstsTools.VariableGroupCopier
{
    /// <summary>
    /// Parameters model
    /// </summary>
    internal class Parameters
    {
        /// <summary>
        /// VSTS account
        /// </summary>
        public string Account { get; set; }

        /// <summary>
        /// VSTS Personal Access Token (PAT) (must have all scopes permission)
        /// </summary>
        public string Token { get; set; }

        /// <summary>
        /// Source project name (from where to copy the variable group)
        /// </summary>
        public string SourceProject { get; set; }

        /// <summary>
        /// Target project name (where to copy the variable group)
        /// </summary>
        public string TargetProject { get; set; }

        /// <summary>
        /// Source variable group name
        /// </summary>
        public string SourceGroup { get; set; }

        /// <summary>
        /// Target variable group name
        /// </summary>
        public string TargetGroup { get; set; }
    }
}