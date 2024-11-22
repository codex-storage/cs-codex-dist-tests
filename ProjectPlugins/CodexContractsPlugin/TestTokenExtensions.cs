using System.Numerics;

namespace CodexContractsPlugin
{
    public class TestToken : IComparable<TestToken>
    {
        public static BigInteger WeiFactor = new BigInteger(1000000000000000000);

        public TestToken(BigInteger tstWei)
        {
            TstWei = tstWei;
            Tst = tstWei / WeiFactor;
        }

        public BigInteger TstWei { get; }
        public BigInteger Tst { get; }

        public int CompareTo(TestToken? other)
        {
            return TstWei.CompareTo(other!.TstWei);
        }

        public override bool Equals(object? obj)
        {
            return obj is TestToken token && TstWei == token.TstWei;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(TstWei);
        }

        public override string ToString()
        {
            var weiOnly = TstWei % WeiFactor;

            var tokens = new List<string>();
            if (Tst > 0) tokens.Add($"{Tst} TST");
            if (weiOnly > 0) tokens.Add($"{weiOnly} TSTWEI");

            return string.Join(" + ", tokens);
        }

        public static TestToken operator +(TestToken a, TestToken b)
        {
            return new TestToken(a.TstWei + b.TstWei);
        }

        public static TestToken operator -(TestToken a, TestToken b)
        {
            return new TestToken(a.TstWei - b.TstWei);
        }

        public static TestToken operator *(TestToken a, int b)
        {
            return new TestToken(a.TstWei * b);
        }

        public static bool operator <(TestToken a, TestToken b)
        {
            return a.TstWei < b.TstWei;
        }

        public static bool operator >(TestToken a, TestToken b)
        {
            return a.TstWei > b.TstWei;
        }

        public static bool operator ==(TestToken a, TestToken b)
        {
            return a.TstWei == b.TstWei;
        }

        public static bool operator !=(TestToken a, TestToken b)
        {
            return a.TstWei != b.TstWei;
        }
    }

    public static class TestTokensExtensions
    {
        public static TestToken TstWei(this int i)
        {
            return TstWei(Convert.ToDecimal(i));
        }

        public static TestToken TstWei(this decimal i)
        {
            return new TestToken(new BigInteger(i));
        }

        public static TestToken TstWei(this BigInteger i)
        {
            return new TestToken(i);
        }

        public static TestToken Tst(this int i)
        {
            return Tst(Convert.ToDecimal(i));
        }

        public static TestToken Tst(this decimal i)
        {
            return new TestToken(new BigInteger(i) * TestToken.WeiFactor);
        }

        public static TestToken Tst(this BigInteger i)
        {
            return new TestToken(i * TestToken.WeiFactor);
        }
    }
}
