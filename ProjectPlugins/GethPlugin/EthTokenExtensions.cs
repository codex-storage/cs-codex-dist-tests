namespace GethPlugin
{
    public class Ether : IComparable<Ether>
    {
        public Ether(decimal wei)
        {
            Wei = wei;
            Eth = wei / TokensIntExtensions.WeiPerEth;
        }

        public decimal Wei { get; }
        public decimal Eth { get; }

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
            return $"{Eth} Eth";
        }
    }

    public static class TokensIntExtensions
    {
        public const decimal WeiPerEth = 1000000000000000000;

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
            return new Ether(i * WeiPerEth);
        }

        public static Ether Wei(this decimal i)
        {
            return new Ether(i);
        }
    }
}
