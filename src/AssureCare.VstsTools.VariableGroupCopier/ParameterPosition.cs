namespace AssureCare.VstsTools.VariableGroupCopier
{
    /// <summary>
    /// Enumeration of all clone task parameters and their positions
    /// </summary>
    public enum ParameterPosition
    {
        SourceProject = 0,
        SourceGroup,
        TargetProject,
        TargetGroup,
        Token,
        OverrideExistentTarget,
        Account
    }
}