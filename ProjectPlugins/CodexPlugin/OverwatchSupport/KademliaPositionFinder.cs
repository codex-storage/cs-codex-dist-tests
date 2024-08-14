using System.Numerics;
using Utils;
using YamlDotNet.Core.Tokens;

namespace CodexPlugin.OverwatchSupport
{
    public class KademliaPositionFinder
    {
        public CodexNodeIdentity[] DeterminePositions(CodexNodeIdentity[] identities)
        {
            var zero = identities.First();
            var distances = CalculateDistances(zero, identities);

            var maxDistance = distances.Values.Max();
            CalculateNormalizedPositions(distances, maxDistance);

            return identities;
        }

        private Dictionary<CodexNodeIdentity, BigInteger>  CalculateDistances(CodexNodeIdentity zero, CodexNodeIdentity[] identities)
        {
            var result = new Dictionary<CodexNodeIdentity, BigInteger>();
            foreach (var id in identities.Skip(1))
            {
                result.Add(id, GetDistance(zero.NodeId, id.NodeId));
            }
            return result;
        }

        private BigInteger GetDistance(string id1, string id2)
        {
            var one = BigInteger.Parse(id1, System.Globalization.NumberStyles.HexNumber).ToByteArray();
            var two = BigInteger.Parse(id2, System.Globalization.NumberStyles.HexNumber).ToByteArray();

            var x = Xor(one, two);
            return new BigInteger(x, isUnsigned: true);
        }

        private byte[] Xor(byte[] one, byte[] two)
        {
            if (one.Length != two.Length) throw new Exception("Not equal length");

            var result = new byte[one.Length];
            for (int i = 0; i < one.Length; i++)
            {
                uint a = one[i];
                uint b = two[i];
                uint c = (a ^ b);
                result[i] = (byte)c;
            }
            return result;
        }

        private void CalculateNormalizedPositions(Dictionary<CodexNodeIdentity, BigInteger> distances, BigInteger maxDistance)
        {
            foreach (var pair in distances)
            {
                pair.Key.KademliaNormalizedPosition = DeterminePosition(pair.Value, maxDistance);
            }
        }

        private float DeterminePosition(BigInteger value, BigInteger maxDistance)
        {
            var f = (value * 10000) / maxDistance;
            return ((float)f) / 10000.0f;
        }
    }
}
