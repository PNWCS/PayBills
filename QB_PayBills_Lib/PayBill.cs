namespace QB_PayBills_Lib
{
    public class PayBill
    {
        public string TxnID { get; set; }

        public DateTime TimeCreated { get; set; }

        public int TxnNumber { get; set; }

        public string PayeeListId { get; set; }

        public string VendorName { get; set; }

        public DateTime PaymentDate { get; set; }

        public string BankList { get; set; }

        public string BankName { get; set; }

        public double CheckAmount { get; set; }

        public List<AppliedBill> BillsPaid { get; set; }

        public PayBill(string txnID, DateTime timeCreated, int txnNumber, string payeeListId, string vendorName, DateTime paymentDate, string bankList, string bankName, double checkAmount, List<AppliedBill> billsPaid)
        {
            TxnID = txnID;
            TimeCreated = timeCreated;
            TxnNumber = txnNumber;
            PayeeListId = payeeListId;
            VendorName = vendorName;
            PaymentDate = paymentDate;
            BankList = bankList;
            BankName = bankName;
            CheckAmount = checkAmount;
            BillsPaid = billsPaid;
        }
    }
}
