namespace AssureCare.VstsTools.VariableGroupCopier
{
    /// <summary>
    /// Contract for all user interactions
    /// </summary>
    internal interface IUserConsole
    {
        void Write(string message);

        string ReadLine();

        bool ReadYesNo(string message);

        void WaitAnyKey(string message = null);
    }
}