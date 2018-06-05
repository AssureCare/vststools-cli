namespace AssureCare.VstsTools.VariableGroupCopier
{
    internal interface IKnownLiterals
    {
        string FileExt { get; }

        string AllGroupsSelector { get; }

        string GroupLocationFileSelector { get; }
    }
}