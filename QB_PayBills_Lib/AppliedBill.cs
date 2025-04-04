using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QB_PayBills_Lib
{
    public class AppliedBill
    {
        public string BillTxnID { get; set; }

        public double BalanceRemaining { get; set; }

        public double AppliedAmount { get; set; }

        public AppliedBill(string txnID, double balanceRemaining, double amount)
        {
            BillTxnID = txnID;
            BalanceRemaining = balanceRemaining;
            AppliedAmount = amount;
        }
    }
}
