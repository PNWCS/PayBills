using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PayBills_Lib
{
    public class Bill
    {
        public string TransactionId { get; set; }
        //public string QbRev { get; set; }

        //public int TransactionNumber { get; set; }
        public string ListId { get; set; }
        public string FullName { get; set; }

        public DateTime TxnDate { get; set; }

        public string RefNumber { get; set; }
        public DateTime DueDate { get; set; }
        public double AmountDue { get; set; }


        public Bill(string transactionId, string listId, string fullName, DateTime txnDate, string refNumber, DateTime dueDate, double amountDue)
        {
            TransactionId = transactionId;
            ListId = listId;
            FullName = fullName;
            TxnDate = txnDate;
            RefNumber = refNumber;
            DueDate = dueDate;
            AmountDue = amountDue;
        }



    }
}
