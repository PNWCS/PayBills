using System;
using System.Data;
using QB_PayBills_Lib;
using ClosedXML.Excel;
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
                Console.WriteLine("Select the Functinality you want to perform \n 1) Show all Paid Payment Bills \n 2) Make payment \n 3) Compare \n 4) Exit");
                string result = Console.ReadLine();

                switch (result)
                {
                    case "1":
                        list = PayBillReader.QueryAllPayBills();
                        break;
                    case "2":
                        Console.WriteLine("Select one of the Open Bills to Pay");
                        List<OpenBills> openbills = PayBillAdder.QueryUnpaidBills(true);
                        Console.WriteLine("Enter the bill number : ");
                        string option = Console.ReadLine();
                        int num;

                        bool success = int.TryParse(option, out num);

                        if (success)
                        {
                            List<PayBill> add = new List<PayBill>();
                            OpenBills openbill = openbills[num - 1];
                            PayBill addbill = new PayBill("", DateTime.Now, 0, "", openbill.VendorName, DateTime.Now, "", "Chase", openbill.TxnID, openbill.Amount, PayBillAdder.GetCreditTxnID(openbill.VendorName), new List<AppliedBill>());
                            add.Add(addbill);
                            PayBillAdder.AddPayBills(add);
                        }
                        else
                        {
                            Console.WriteLine("Enter current option for Open bills");
                        }
                        break;
                    case "3":
                        List<PayBill> compareBills = new List<PayBill>();
                        string filePath = "C:\\Users\\MohammedS\\source\\repos\\PayBills\\QB_PayBills_Test\\Sample_Company_Data.xlsx";
                        if (!File.Exists(filePath))
                            throw new FileNotFoundException($"The file '{filePath}' does not exist.");
                        using (var workbook = new XLWorkbook(filePath))
                        {
                            var worksheet = workbook.Worksheet("payment_bills");

                            // Get the range of used rows

                            var range = worksheet.RangeUsed();
                            if (range == null)
                            {
                                Console.WriteLine("Warning: The worksheet is empty or contains no used range.");
                            }
                            else
                            {
                                var rows = range.RowsUsed();
                                foreach (var row in rows.Skip(1)) // Skip header row
                                {
                                    var cells = row.CellsUsed();
                                    if (cells.Count() >= 2)
                                    {
                                        string txnID = cells.ElementAt(0).GetString();
                                        string vendorName = cells.ElementAt(1).GetString();
                                        string amount = cells.ElementAt(2).GetString();
                                        PayBill addbill = new PayBill("", DateTime.Now, 0, "", vendorName, DateTime.Now, "", "Chase", txnID, double.Parse(amount), PayBillAdder.GetCreditTxnID(vendorName), new List<AppliedBill>());
                                        compareBills.Add(addbill);
                                    }
                                    else
                                    {
                                        Console.WriteLine("Warning: Row does not contain enough data.");
                                    }

                                }


                            }
                        }
                        List<PayBill> bills = PayBillComparator.ComparePayBills(compareBills);
                        foreach (var bill in bills)
                        {
                            Console.WriteLine($"Amount {bill.CheckAmount} is Paid by {bill.VendorName}. Bill Status : {bill.Status}\n");
                        }
                        break;
                    case "4":
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