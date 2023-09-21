using Nethereum.Hex.HexTypes;
using System.Numerics;

namespace NethereumWorkflow
{
    public static class ConversionExtensions
    {
        public static HexBigInteger ToHexBig(this decimal amount)
        {
            var bigint = ToBig(amount);
            var str = bigint.ToString("X");
            return new HexBigInteger(str);
        }

        public static BigInteger ToBig(this decimal amount)
        {
            return new BigInteger(amount);
        }

        public static decimal ToDecimal(this HexBigInteger hexBigInteger)
        {
            return ToDecimal(hexBigInteger.Value);
        }

        public static decimal ToDecimal(this BigInteger bigInteger)
        {
            return (decimal)bigInteger;
        }
    }
}
