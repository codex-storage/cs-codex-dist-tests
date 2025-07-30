using System.Numerics;

namespace Utils
{
    public class Ether : IComparable<Ether>
    {
        public Ether(BigInteger wei)
        {
            Wei = wei;
            Eth = wei / TokensIntExtensions.WeiPerEth;
        }

        public BigInteger Wei { get; }
        public BigInteger Eth { get; }

        public int CompareTo(Ether? other)
        {
            return Wei.CompareTo(other!.Wei);
        }

        public override bool Equals(object? obj)
        {
            return obj is Ether ether && Wei == ether.Wei;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Wei);
        }

        public override string ToString()
        {
            var weiOnly = Wei % TokensIntExtensions.WeiPerEth;

            var tokens = new List<string>();
            if (Eth > 0) tokens.Add($"{Eth} Eth");
            if (weiOnly > 0) tokens.Add($"{weiOnly} Wei");

            return string.Join(" + ", tokens);
        }

        public static Ether operator +(Ether a, Ether b)
        {
            return new Ether(a.Wei + b.Wei);
        }

        public static Ether operator -(Ether a, Ether b)
        {
            return new Ether(a.Wei - b.Wei);
        }

        public static Ether operator *(Ether a, int b)
        {
            return new Ether(a.Wei * b);
        }

        public static bool operator <(Ether a, Ether b)
        {
            return a.Wei < b.Wei;
        }

        public static bool operator >(Ether a, Ether b)
        {
            return a.Wei > b.Wei;
        }

        public static bool operator ==(Ether a, Ether b)
        {
            return a.Wei == b.Wei;
        }

        public static bool operator !=(Ether a, Ether b)
        {
            return a.Wei != b.Wei;
        }
    }

    public static class TokensIntExtensions
    {
        public static readonly BigInteger WeiPerEth = new BigInteger(1000000000000000000);

        public static Ether Eth(this int i)
        {
            return Eth(Convert.ToDecimal(i));
        }

        public static Ether Wei(this int i)
        {
            return Wei(Convert.ToDecimal(i));
        }

        public static Ether Eth(this decimal i)
        {
            var a = new BigInteger(i);
            return new Ether(a * WeiPerEth);
        }

        public static Ether Wei(this decimal i)
        {
            var a = new BigInteger(i);
            return new Ether(a);
        }

        public static Ether Eth(this BigInteger i)
        {
            return new Ether(i * WeiPerEth);
        }

        public static Ether Wei(this BigInteger i)
        {
            return new Ether(i);
        }
    }
}
