using DistTestCore;
using DistTestCore.Codex;
using Logging;
using Utils;

namespace ContinuousTests
{
    public class CodexNodeFactory
    {
        public CodexNode[] Create(string[] urls, BaseLog log, ITimeSet timeSet)
        {
            return urls.Select(url =>
            {
                var cutIndex = url.LastIndexOf(':');
                var host = url.Substring(0, cutIndex);
                var port = url.Substring(cutIndex + 1);
                var address = new Address(host, Convert.ToInt32(port));
                return new CodexNode(log, timeSet, address);
            }).ToArray();
        }
    }
}
