namespace AutoClient.Modes.FolderStore
{
    public class PurchaseInfo
    {
        public PurchaseInfo(TimeSpan purchaseDurationTotal, TimeSpan purchaseDurationSafe)
        {
            PurchaseDurationTotal = purchaseDurationTotal;
            PurchaseDurationSafe = purchaseDurationSafe;

            if (PurchaseDurationTotal < TimeSpan.Zero) throw new Exception(nameof(PurchaseDurationTotal));
            if (PurchaseDurationSafe < TimeSpan.Zero) throw new Exception(nameof(PurchaseDurationSafe));
            if (PurchaseDurationTotal < PurchaseDurationSafe) throw new Exception("TotalDuration < SafeDuration");
        }

        public TimeSpan PurchaseDurationTotal { get; }
        public TimeSpan PurchaseDurationSafe { get; }
    }
}
