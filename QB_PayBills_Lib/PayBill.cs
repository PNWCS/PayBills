namespace QB_PayBills_Lib
{
    public class PayBill
    {
        public string TxnID { get; set; }

        public DateTime TimeCreated { get; set; }

        public int TxnNumber { get; set; }

        public string PayeeName { get; set; }

        public string PayeeListId { get; set; }

        public string VendorName { get; set; }

        public string BillTxnID { get; set; }

        public DateTime PaymentDate { get; set; }

        public string BankListId { get; set; }

        public string BankName { get; set; }

        public string Memo { get; set; }

        public double CheckAmount { get; set; }

        public string CreditTxnID { get; set; }

        public List<AppliedBill> BillsPaid { get; set; }

        public PayBill(string txnID, DateTime timeCreated, int txnNumber, string payeeListId, string vendorName, DateTime paymentDate, string bankListId, string bankName, string billTxnId, double checkAmount, string creditTxnId, List<AppliedBill> billsPaid)
        {
            TxnID = txnID;
            TimeCreated = timeCreated;
            TxnNumber = txnNumber;
            PayeeListId = payeeListId;
            VendorName = vendorName;
            PaymentDate = paymentDate;
            BankListId = bankListId;
            BankName = bankName;
            BillTxnID = billTxnId;
            CheckAmount = checkAmount;
            CreditTxnID = creditTxnId;
            BillsPaid = billsPaid;
        }
    }

    public class OpenBills
    {
        public string TxnID { get; set; }

        public string VendorName { get; set; }

        public double Amount { get; set; }

        public OpenBills(string txnId, string vendorName, double amount)
        {
            TxnID = txnId;
            VendorName = vendorName;
            Amount = amount;
        }
    }
}
