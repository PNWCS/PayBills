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
            list = PayBillReader.QueryAllPayBills();
        }

    }
}