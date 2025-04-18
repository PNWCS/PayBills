namespace QB_PayBills_Lib
{
    public class AddBill
    {
        public string PayeeName { get; set; }

        public DateTime TxnDate { get; set; }

        public string BankName { get; set; }

        public string TxnID { get; set; }

        public double PaymentAmount { get; set; }

        public string CreditTxnID { get; set; }

        public double AppliedAmount { get; set; }

        public AddBill(string payeeName, DateTime txnDate, string bankName, string txnID, double paymentAmount, string creditTxnID, double appliedAmount)
        {
            PayeeName = payeeName;
            TxnDate = txnDate;
            BankName = bankName;
            TxnID = txnID;
            PaymentAmount = paymentAmount;
            CreditTxnID = creditTxnID;
            AppliedAmount = appliedAmount;
        }
    }
}
