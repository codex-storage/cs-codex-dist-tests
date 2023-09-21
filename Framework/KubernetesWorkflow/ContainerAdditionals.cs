using Newtonsoft.Json.Linq;

namespace KubernetesWorkflow
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
            if (userData == null) return default(T);
            var jobject = (JObject)userData.UserData;
            return jobject.ToObject<T>();
        }

        private static Additional ConvertToAdditional(object userData)
        {
            var typeName = GetTypeName(userData.GetType());
            return new Additional(typeName, userData);
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
        public Additional(string type, object userData)
        {
            Type = type;
            UserData = userData;
        }

        public string Type { get; }
        public object UserData { get; }
    }
}
