namespace DistTestCore
{
    public class Ether
    {
        public Ether(decimal wei)
        {
            Wei = wei;
        }

        public decimal Wei { get; }
    }

    public class TestToken
    {
        public TestToken(decimal amount)
        {
            Amount = amount;
        }

        public decimal Amount { get; }

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
