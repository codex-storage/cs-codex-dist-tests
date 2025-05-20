using CodexContractsPlugin;
using CodexContractsPlugin.Marketplace;

namespace TraceContract
{
    public class Output
    {
        public void LogRequest(Request request)
        {
            throw new NotImplementedException();
        }

        public void LogEventOrCall<T>(T[] calls) where T : IHasRequestId, IHasBlock
        {
            throw new NotImplementedException();
        }
    }
}
