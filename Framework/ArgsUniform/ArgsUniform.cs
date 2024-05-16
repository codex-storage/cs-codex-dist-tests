using System.Reflection;

namespace ArgsUniform
{
    public class ArgsUniform<T>
    {
        private readonly Assigner<T> assigner;
        private readonly Action printAppInfo;
        private readonly string[] args;
        private const int cliStart = 8;
        private const int shortStart = 38;
        private const int envStart = 48;
        private const int descStart = 80;

        public ArgsUniform(Action printAppInfo, params string[] args)
            : this(printAppInfo, new IEnv.Env(), args)
        {
        }

        public ArgsUniform(Action printAppInfo, object defaultsProvider, params string[] args)
            : this(printAppInfo, defaultsProvider, new IEnv.Env(), args)
        {
        }

        public ArgsUniform(Action printAppInfo, IEnv.IEnv env, params string[] args)
            : this(printAppInfo, null!, env, args)
        {
        }

        public ArgsUniform(Action printAppInfo, object defaultsProvider, IEnv.IEnv env, params string[] args)
        {
            this.printAppInfo = printAppInfo;
            this.args = args;

            assigner = new Assigner<T>(env, args, defaultsProvider);
        }

        public T Parse(bool printResult = false)
        {
            if (args.Any(a => a == "-h" || a == "--help" || a == "-?"))
            {
                printAppInfo();
                PrintHelp();
                Environment.Exit(0);
            }

            var result = Activator.CreateInstance<T>();
            var uniformProperties = typeof(T).GetProperties().Where(m => m.GetCustomAttributes(typeof(UniformAttribute), false).Length == 1).ToArray();
            var missingRequired = new List<PropertyInfo>();
            foreach (var uniformProperty in uniformProperties)
            {
                var attr = uniformProperty.GetCustomAttribute<UniformAttribute>();
                if (attr != null)
                {
                    if (!assigner.UniformAssign(result, attr, uniformProperty) && attr.Required)
                    {
                        missingRequired.Add(uniformProperty);
                    }
                }
            }

            if (missingRequired.Any())
            {
                PrintResults(result, uniformProperties);
                Print("");
                foreach (var missing in missingRequired)
                {
                    var attr = missing.GetCustomAttribute<UniformAttribute>()!;
                    var exampleArg = $"--{attr.Arg}=...";
                    var exampleEnvVar = $"{attr.EnvVar}=...";
                    Print($" ! Missing required input. Use argument: '{exampleArg}' or environment variable: '{exampleEnvVar}'.");
                }

                PrintHelp();
                Environment.Exit(1);
            }

            if (printResult)
            {
                PrintResults(result, uniformProperties);
            }

            return result;
        }

        private void PrintResults(T result, PropertyInfo[] uniformProperties)
        {
            Print("");
            foreach (var p in uniformProperties)
            {
                Print($"\t{p.Name} = {p.GetValue(result)}");
            }
            Print("");
        }

        public void PrintHelp()
        {
            Print("");
            PrintAligned("CLI option:", "(short)", "Environment variable:", "Description");
            var attrs = typeof(T).GetProperties().Where(m => m.GetCustomAttributes(typeof(UniformAttribute), false).Length == 1).Select(p => p.GetCustomAttribute<UniformAttribute>()).Where(a => a != null).ToArray();
            foreach (var attr in attrs)
            {
                var a = attr!;
                var optional = !a.Required ? " *" : "";
                PrintAligned($"--{a.Arg}=...", $"({a.ArgShort})", a.EnvVar, a.Description + optional);
            }
            Print("");
        }

        private void Print(string msg)
        {
            Console.WriteLine(msg);
        }

        private void PrintAligned(string cli, string s, string env, string desc)
        {
            Console.CursorLeft = cliStart;
            Console.Write(cli);
            Console.CursorLeft = shortStart;
            Console.Write(s);
            Console.CursorLeft = envStart;
            Console.Write(env);
            Console.CursorLeft = descStart;
            Console.Write(desc + Environment.NewLine);
        }
    }
}
