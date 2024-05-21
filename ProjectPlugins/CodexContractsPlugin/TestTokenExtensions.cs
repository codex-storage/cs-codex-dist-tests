using System.Numerics;

namespace CodexContractsPlugin
{
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
        public static TestToken TestTokens(this int i)
        {
            return TestTokens(Convert.ToDecimal(i));
        }

        public static TestToken TestTokens(this decimal i)
        {
            return new TestToken(i);
        }

        public static TestToken TestTokens(this BigInteger i)
        {
            return new TestToken((decimal)i);
        }
    }
}
