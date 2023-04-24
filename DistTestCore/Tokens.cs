namespace DistTestCore
{
    public class Ether : IComparable<Ether>
    {
        public Ether(decimal wei)
        {
            Wei = wei;
        }

        public decimal Wei { get; }

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
            return $"{Wei} Wei";
        }
    }

    public class TestToken : IComparable<TestToken>
    {
        public TestToken(decimal amount)
        {
            Amount = amount;
        }

        public decimal Amount { get; }

        public int CompareTo(TestToken? other)
        {
            return Amount.CompareTo(other!.Amount);
        }

        public override bool Equals(object? obj)
        {
            return obj is TestToken token && Amount == token.Amount;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Amount);
        }

        public override string ToString()
        {
            return $"{Amount} TestTokens";
        }
    }

    public static class TokensIntExtensions
    {
        private const decimal weiPerEth = 1000000000000000000;

        public static TestToken TestTokens(this int i)
        {
            return TestTokens(Convert.ToDecimal(i));
        }

        public static TestToken TestTokens(this decimal i)
        {
            return new TestToken(i);
        }

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
            return new Ether(i * weiPerEth);
        }

        public static Ether Wei(this decimal i)
        {
            return new Ether(i);
        }
    }
}
