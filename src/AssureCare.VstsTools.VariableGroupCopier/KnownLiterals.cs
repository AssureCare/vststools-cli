namespace AssureCare.VstsTools.VariableGroupCopier
{
    internal class KnownLiterals : IKnownLiterals
    {
        public string FileExt => ".json";

        public string AllGroupsSelector => "*";

        public string GroupLocationFileSelector => "/file";
    }
}