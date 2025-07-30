using Nethereum.Hex.HexConvertors.Extensions;

namespace TraceContract
{
    public class Input
    {
        public string PurchaseId
        {
            get
            {
                var v = Environment.GetEnvironmentVariable("PURCHASE_ID");
                if (!string.IsNullOrEmpty(v)) return v;

                return
                    // expired:
                    "a7fe97dc32216aba0cbe74b87beb3f919aa116090dd5e0d48085a1a6b0080e82";

                    // started:
                    //"066df09a3a2e2587cfd577a0e96186c915b113d02b331b06e56f808494cff2b4";
            }
        }

        public byte[] RequestId
        {
            get
            {
                var r = PurchaseId.HexToByteArray();
                if (r == null || r.Length != 32) throw new ArgumentException(nameof(PurchaseId));
                return r;
            }
        }
    }
}
