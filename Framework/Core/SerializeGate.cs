using Newtonsoft.Json;

namespace Core
{
    public static class SerializeGate
    {
        /// <summary>
        /// SerializeGate was added to help ensure deployment objects are serializable
        /// and remain viable after deserialization.
        /// Tools can be built on top of the core interface that rely on deployment objects being serializable.
        /// Insert the serialization gate after deployment but before wrapping to ensure any future changes
        /// don't break this requirement.
        /// </summary>
        public static T Gate<T>(T anything)
        {
            var json = JsonConvert.SerializeObject(anything);
            return JsonConvert.DeserializeObject<T>(json)!;
        }
    }
}
