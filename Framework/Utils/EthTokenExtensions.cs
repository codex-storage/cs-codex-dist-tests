namespace Utils
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
