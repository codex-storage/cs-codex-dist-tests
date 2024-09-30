namespace BiblioTech
{
    public class Transaction<T>
    {
        public Transaction(T tokenAmount, string transactionHash)
        {
            TokenAmount = tokenAmount;
            TransactionHash = transactionHash;
        }

        public T TokenAmount { get; }
        public string TransactionHash { get; }

        public override string ToString()
        {
            if (TokenAmount == null) return "NULL";
            return TokenAmount.ToString()!;
        }
    }
}
