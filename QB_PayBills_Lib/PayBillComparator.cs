using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog;

namespace QB_PayBills_Lib
{
    public class PayBillComparator
    {
        static PayBillComparator()
        {
            Log.Information("TermsComparator Initialized.");
        }

        public static List<PayBill> ComparePayBills(List<PayBill> bills)
        {
            List<OpenBills> unpaidBills = PayBillAdder.QueryUnpaidBills(false);
            List<PayBill> queryBills = PayBillReader.QueryAllPayBills();
            List<PayBill> newBillsToAdd = new List<PayBill>();

            for (int i = 0; i < queryBills.Count; i++)
            {
                queryBills[i].Status = PayBillStatus.MissingInFile;
            }
            for (int i = 0; i < bills.Count; i++)
            {
                var bill = unpaidBills.FirstOrDefault(b => b.TxnID == bills[i].BillTxnID);
                if (bill != null)
                {
                    var newBill = new PayBill("", DateTime.Now, 0, "", bills[i].VendorName, DateTime.Now, "", "Chase", bills[i].BillTxnID, bills[i].CheckAmount, PayBillAdder.GetCreditTxnID(bills[i].VendorName), new List<AppliedBill>());
                    newBill.Status = PayBillStatus.Added;
                    newBillsToAdd.Add(newBill);
                }
                else
                {
                    for (int j = 0; j < queryBills.Count; j++)
                    {
                        if (queryBills[j].CheckAmount == bills[i].CheckAmount && queryBills[j].VendorName == bills[i].VendorName)
                        {
                            bills[i].Status = PayBillStatus.Unchanged;
                            queryBills[j].Status = PayBillStatus.Unchanged;
                        }
                    }
                }
            }

            List<PayBill> finalList = queryBills.Concat(newBillsToAdd).Distinct().ToList();

            if (newBillsToAdd.Count > 0)
            {
                var list = PayBillAdder.AddPayBills(newBillsToAdd);
                if (list.Count > 0)
                {
                    foreach (var bill in list)
                    {
                        Console.WriteLine($"\nPay Bill {bill.TxnID} added successfully.\n");
                    }
                }
            }
            else
            {
                Console.WriteLine("\nNo new Pay Bills to add. \n\n");
            }
            return finalList;
        }
    }
}