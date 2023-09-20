using Newtonsoft.Json;

namespace Core
{
    public static class SerializeGate
    {
        public static T Gate<T>(T anything)
        {
            var json = JsonConvert.SerializeObject(anything);
            return JsonConvert.DeserializeObject<T>(json)!;
        }
    }
}
