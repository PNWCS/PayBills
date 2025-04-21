using System;
using System.Data;
using QB_PayBills_Lib;
using QBFC16Lib;

namespace QB_PayBills_CLI
{
    public class Sample
    {
        public static void Main(string[] args)
        {
            List<PayBill> list = new List<PayBill>();
            int index = 1;
            while (index == 1)
            {
                Console.WriteLine("Select the Functinality you want to perform \n 1) Show all Paid Payment Bills \n 2) Make payment \n 3) Exit");
                string result = Console.ReadLine();

                switch (result)
                {
                    case "1":
                        list = PayBillReader.QueryAllPayBills();
                        break;
                    case "2":
                        Console.WriteLine("Select one of the Open Bills to Pay");
                        List<OpenBills> openbills = PayBillAdder.QueryUnpaidBills();
                        Console.WriteLine("Enter the bill number : ");
                        string option = Console.ReadLine();
                        int num;

                        bool success = int.TryParse(option, out num);

                        if (success)
                        {
                            List<PayBill> add = new List<PayBill>();
                            OpenBills openbill = openbills[num - 1];
                            PayBill addbill = new PayBill("", DateTime.Now, 0, "", openbill.VendorName, DateTime.Now, "", "Chase", openbill.TxnID, openbill.Amount, "F4-1743994088", new List<AppliedBill>());
                            add.Add(addbill);
                            PayBillAdder.AddPayBills(add);
                        }
                        else
                        {
                            Console.WriteLine("Enter current option for Open bills");
                        }
                        break;
                    case "3":
                        index = 2;
                        break;
                    default:
                        Console.WriteLine("Invalid option. Please choose 1, 2, or 3.");
                        break;
                }
            }
        }

    }
}