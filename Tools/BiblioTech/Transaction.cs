namespace BiblioTech
{
    public class Transaction<T>
    {
        public Transaction(T amountSent, T amountMinted, string transactionHash)
        {
            AmountSent = amountSent;
            AmountMinted = amountMinted;
            TransactionHash = transactionHash;
        }

        public T AmountSent { get; }
        public T AmountMinted { get; }
        public string TransactionHash { get; }

        public override string ToString()
        {
            var result = "";
            if (AmountSent == null) result += "sent:null";
            else result += "send:" + AmountSent.ToString();
            if (AmountMinted == null) result += " minted:null";
            else result += " minted:" + AmountMinted.ToString();
            return result;
        }
    }
}
