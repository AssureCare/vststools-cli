using System;

namespace AssureCare.VstsTools.VariableGroupCopier
{
    /// <summary>
    /// Implementation of <see cref="IUserConsole"/> using <see cref="System.Console"/>
    /// </summary>
    public class UserConsole : IUserConsole
    {
        public void Write(string message) => Console.Write(message);

        public void WriteLine(string message) => Console.WriteLine(message);

        public string ReadLine() => Console.ReadLine();
        
        public bool ReadYesNo(string message)
        {
            Write(message);

            return ReadLine().Equals("Y", StringComparison.InvariantCultureIgnoreCase);
        }

        public void WaitAnyKey(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) message = "Press any key to exit.";

            WriteLine(message);

            Console.ReadKey();
        }
    }
}