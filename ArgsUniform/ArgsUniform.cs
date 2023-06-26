using System.Reflection;

namespace ArgsUniform
{
    public class ArgsUniform<T>
    {
        private readonly object? defaultsProvider;
        private readonly IEnv.IEnv env;
        private readonly string[] args;
        private const int cliStart = 8;
        private const int shortStart = 38;
        private const int envStart = 48;
        private const int descStart = 80;

        public ArgsUniform(params string[] args)
            : this(null!, args)
        {
        }

        public ArgsUniform(object defaultsProvider, params string[] args)
            : this(defaultsProvider, new IEnv.Env(), args)
        {
        }

        public ArgsUniform(IEnv.IEnv env, params string[] args)
            : this(null!, env, args)
        {
        }

        public ArgsUniform(object defaultsProvider, IEnv.IEnv env, params string[] args)
        {
            this.defaultsProvider = defaultsProvider;
            this.env = env;
            this.args = args;
        }

        public T Parse(bool printResult = false)
        {
            var result = Activator.CreateInstance<T>();
            var uniformProperties = typeof(T).GetProperties().Where(m => m.GetCustomAttributes(typeof(UniformAttribute), false).Length == 1).ToArray();
            var missingRequired = new List<PropertyInfo>();
            foreach (var uniformProperty in uniformProperties)
            {
                var attr = uniformProperty.GetCustomAttribute<UniformAttribute>();
                if (attr != null)
                {
                    if (!UniformAssign(result, attr, uniformProperty) && attr.Required)
                    {
                        missingRequired.Add(uniformProperty);
                    }
                }
            }

            if (missingRequired.Any())
            {
                Print("");
                foreach (var missing in missingRequired)
                {
                    var attr = missing.GetCustomAttribute<UniformAttribute>()!;
                    var exampleArg = $"--{attr.Arg}=...";
                    var exampleEnvVar = $"{attr.EnvVar}=...";
                    Print($" ! Missing required input. Use argument: '{exampleArg}' or environment variable: '{exampleEnvVar}'.");
                }

                PrintHelp();
                throw new ArgumentException("Unable to assemble all required arguments");
            }

            if (printResult)
            {
                Print("");
                foreach (var p in uniformProperties)
                {
                    Print($"\t{p.Name} = {p.GetValue(result)}");
                }
                Print("");
            }

            return result;
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

        private bool UniformAssign(T result, UniformAttribute attr, PropertyInfo uniformProperty)
        {
            if (AssignFromArgsIfAble(result, attr, uniformProperty)) return true;
            if (AssignFromEnvVarIfAble(result, attr, uniformProperty)) return true;
            if (AssignFromDefaultsIfAble(result, uniformProperty)) return true;
            return false;
        }

        private bool AssignFromDefaultsIfAble(T result, PropertyInfo uniformProperty)
        {
            if (defaultsProvider == null) return true;

            var defaultProperty = defaultsProvider.GetType().GetProperties().SingleOrDefault(p => p.Name == uniformProperty.Name);
            if (defaultProperty == null) return true;

            var value = defaultProperty.GetValue(defaultsProvider);
            if (value != null)
            {
                Assign(result, uniformProperty, value);
                return true;
            }
            return false;
        }

        private bool AssignFromEnvVarIfAble(T result, UniformAttribute attr, PropertyInfo uniformProperty)
        {
            var e = env.GetEnvVarOrDefault(attr.EnvVar, string.Empty);
            if (!string.IsNullOrEmpty(e))
            {
                Assign(result, uniformProperty, e);
                return true;
            }
            return false;
        }

        private bool AssignFromArgsIfAble(T result, UniformAttribute attr, PropertyInfo uniformProperty)
        {
            var fromArg = GetFromArgs(attr.Arg);
            if (fromArg != null)
            {
                Assign(result, uniformProperty, fromArg);
                return true;
            }
            var fromShort = GetFromArgs(attr.ArgShort);
            if (fromShort != null)
            {
                Assign(result, uniformProperty, fromShort);
                return true;
            }
            return false;
        }

        private bool Assign(T result, PropertyInfo uniformProperty, object value)
        {
            if (uniformProperty.PropertyType == value.GetType())
            {
                uniformProperty.SetValue(result, value);
                return true;
            }
            else
            {
                if (uniformProperty.PropertyType == typeof(string) || uniformProperty.PropertyType == typeof(int))
                {
                    uniformProperty.SetValue(result, Convert.ChangeType(value, uniformProperty.PropertyType));
                    return true;
                }
                else
                {
                    if (uniformProperty.PropertyType == typeof(int?)) return AssignOptionalInt(result, uniformProperty, value);
                    if (uniformProperty.PropertyType.IsEnum) return AssignEnum(result, uniformProperty, value);
                    
                    throw new NotSupportedException();
                }
            }
        }

        private static bool AssignEnum(T result, PropertyInfo uniformProperty, object value)
        {
            var s = value.ToString();
            if (Enum.TryParse(uniformProperty.PropertyType, s, out var e))
            {
                uniformProperty.SetValue(result, e);
                return true;
            }
            return false;
        }

        private static bool AssignOptionalInt(T result, PropertyInfo uniformProperty, object value)
        {
            if (int.TryParse(value.ToString(), out int i))
            {
                uniformProperty.SetValue(result, i);
                return true;
            }
            return false;
        }

        private string? GetFromArgs(string key)
        {
            var argKey = $"--{key}=";
            var arg = args.FirstOrDefault(a => a.StartsWith(argKey));
            if (arg != null)
            {
                return arg.Substring(argKey.Length);
            }
            return null;
        }
    }
}
