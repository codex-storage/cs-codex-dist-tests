using System.Reflection;

namespace ArgsUniform
{
    public class Assigner<T>
    {
        private readonly IEnv.IEnv env;
        private readonly string[] args;
        private readonly object? defaultsProvider;

        public Assigner(IEnv.IEnv env, string[] args, object? defaultsProvider)
        {
            this.env = env;
            this.args = args;
            this.defaultsProvider = defaultsProvider;
        }

        public bool UniformAssign(T result, UniformAttribute attr, PropertyInfo uniformProperty)
        {
            if (AssignFromArgsIfAble(result, attr, uniformProperty)) return true;
            if (AssignFromEnvVarIfAble(result, attr, uniformProperty)) return true;
            if (AssignFromDefaultsIfAble(result, uniformProperty)) return true;
            return false;
        }

        private bool AssignFromDefaultsIfAble(T result, PropertyInfo uniformProperty)
        {
            var currentValue = uniformProperty.GetValue(result);
            var isEmptryString = (currentValue as string) == string.Empty;
            if (currentValue != GetDefaultValueForType(uniformProperty.PropertyType) && !isEmptryString) return true;
            if (defaultsProvider == null) return false;

            var defaultProperty = defaultsProvider.GetType().GetProperties().SingleOrDefault(p => p.Name == uniformProperty.Name);
            if (defaultProperty == null) return false;

            var value = defaultProperty.GetValue(defaultsProvider);
            if (value != null)
            {
                return Assign(result, uniformProperty, value);
            }
            return false;
        }

        private bool AssignFromEnvVarIfAble(T result, UniformAttribute attr, PropertyInfo uniformProperty)
        {
            var e = env.GetEnvVarOrDefault(attr.EnvVar, string.Empty);
            if (!string.IsNullOrEmpty(e))
            {
                return Assign(result, uniformProperty, e);
            }
            return false;
        }

        private bool AssignFromArgsIfAble(T result, UniformAttribute attr, PropertyInfo uniformProperty)
        {
            var fromArg = GetFromArgs(attr.Arg);
            if (fromArg != null)
            {
                return Assign(result, uniformProperty, fromArg);
            }
            var fromShort = GetFromArgs(attr.ArgShort);
            if (fromShort != null)
            {
                return Assign(result, uniformProperty, fromShort);
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
                    if (uniformProperty.PropertyType == typeof(bool)) return AssignBool(result, uniformProperty, value);

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

        private static bool AssignBool(T result, PropertyInfo uniformProperty, object value)
        {
            var s = value.ToString();
            if (s == "1" || (s != null && s.ToLowerInvariant() == "true"))
            {
                uniformProperty.SetValue(result, true);
            }
            return true;
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

        private static object GetDefaultValueForType(Type t)
        {
            if (t.IsValueType) return Activator.CreateInstance(t)!;
            return null!;
        }
    }
}
