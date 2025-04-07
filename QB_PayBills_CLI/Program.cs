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
                        List<AddBill> add = new List<AddBill>();
                        AddBill bill1 = new AddBill("Amazon", DateTime.Now, "Chase", "109-1743995108", 300, "F4-1743994088", 300);
                        AddBill bill2 = new AddBill("Amazon", DateTime.Now, "Chase", "10D-1743995125", 400, "F4-1743994088", 400);
                        AddBill bill3 = new AddBill("Amazon", DateTime.Now, "Chase", "111-1743995141", 500, "F4-1743994088", 500);
                        AddBill bill4 = new AddBill("Amazon", DateTime.Now, "Chase", "115-1743995158", 600, "F4-1743994088", 600);
                        add.Add(bill1);
                        PayBillAdder.AddPayBill(add);
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