using Newtonsoft.Json.Linq;

namespace CodexPlugin
{
    public class Mapper
    {
        public DebugInfo Map(CodexOpenApi.DebugInfo debugInfo)
        {
            return new DebugInfo
            {
                Id = debugInfo.Id,
                Spr = debugInfo.Spr,
                Addrs = debugInfo.Addrs.ToArray(),
                AnnounceAddresses = ((JArray) debugInfo.AdditionalProperties["announceAddresses"]).Select(x => x.ToString()).ToArray(),
            };
        }
    }
}
