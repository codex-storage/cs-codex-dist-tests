using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace KubernetesWorkflow.Recipe
{
    public class ContainerAdditionals
    {
        public ContainerAdditionals(Additional[] additionals)
        {
            Additionals = additionals;
        }

        public static ContainerAdditionals CreateFromUserData(IEnumerable<object> userData)
        {
            return new ContainerAdditionals(userData.Select(ConvertToAdditional).ToArray());
        }

        public Additional[] Additionals { get; }

        public T? Get<T>()
        {
            var typeName = GetTypeName(typeof(T));
            var userData = Additionals.SingleOrDefault(a => a.Type == typeName);
            if (userData == null) return default;
            return JsonConvert.DeserializeObject<T>(userData.UserData);
        }

        private static Additional ConvertToAdditional(object userData)
        {
            var typeName = GetTypeName(userData.GetType());
            return new Additional(typeName, JsonConvert.SerializeObject(userData));
        }

        private static string GetTypeName(Type type)
        {
            var typeName = type.FullName;
            if (string.IsNullOrEmpty(typeName)) throw new Exception("Object type fullname is null or empty: " + type);
            return typeName;
        }
    }

    public class Additional
    {
        public Additional(string type, string userData)
        {
            Type = type;
            UserData = userData;
        }

        public string Type { get; }
        public string UserData { get; }
    }
}
