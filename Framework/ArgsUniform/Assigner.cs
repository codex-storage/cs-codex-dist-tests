using System.Globalization;
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

        public string DescribeDefaultFor(PropertyInfo property)
        {
            var obj = Activator.CreateInstance<T>();
            var defaultValue = GetDefaultValue(obj, property);
            if (defaultValue == null) return "";
            if (defaultValue is string str)
            {
                return "\"" + str + "\"";
            }
            return defaultValue.ToString() ?? string.Empty;
        }

        private object? GetDefaultValue(T result, PropertyInfo uniformProperty)
        {
            // Get value from object's static initializer if it's there.
            var currentValue = uniformProperty.GetValue(result);
            if (currentValue != null) return currentValue;

            // Get value from defaults-provider object if it's there.
            if (defaultsProvider == null) return null;
            var defaultProperty = defaultsProvider.GetType().GetProperties().SingleOrDefault(p => p.Name == uniformProperty.Name);
            if (defaultProperty == null) return null;
            return defaultProperty.GetValue(defaultsProvider);
        }

        private bool AssignFromDefaultsIfAble(T result, PropertyInfo uniformProperty)
        {
            var defaultValue = GetDefaultValue(result, uniformProperty);
            var isEmptryString = (defaultValue as string) == string.Empty;
            if (defaultValue != null && defaultValue != GetDefaultValueForType(uniformProperty.PropertyType) && !isEmptryString)
            {
                return Assign(result, uniformProperty, defaultValue);
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
                    if (uniformProperty.PropertyType == typeof(ulong)) return AssignUlong(result, uniformProperty, value);

                    throw new NotSupportedException(
                        $"Unsupported property type '${uniformProperty.PropertyType}' " +
                        $"for property '${uniformProperty.Name}'.");
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
            if (int.TryParse(value.ToString(), CultureInfo.InvariantCulture, out int i))
            {
                uniformProperty.SetValue(result, i);
                return true;
            }
            return false;
        }

        private bool AssignUlong(T? result, PropertyInfo uniformProperty, object value)
        {
            if (ulong.TryParse(value.ToString(), CultureInfo.InvariantCulture, out ulong i))
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
