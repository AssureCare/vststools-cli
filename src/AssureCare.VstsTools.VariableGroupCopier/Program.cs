namespace AssureCare.VstsTools.VariableGroupCopier
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Poor man DI
            IUserConsole userConsole = new UserConsole();
            IKnownLiterals knownLiterals = new KnownLiterals();
            IParametersResolver parametersResolver = new ParametersResolver(userConsole, knownLiterals);

            new VariableGroupCopier(parametersResolver, 
                (a, t) => new VariableGroupVstsRepository(a, t, parametersResolver),
                () => new VariableGroupFileRepository(knownLiterals, parametersResolver)).Copy(args);

            if (parametersResolver.InteractiveMode) userConsole.WaitAnyKey();
        }
    }
}
