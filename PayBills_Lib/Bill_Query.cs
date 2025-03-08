using System.Data;
using QBFC16Lib;

namespace PayBills_Lib
{
    public class Bill_Query
    {

        public static void DoBillToPayQuery()
        {
            bool sessionBegun = false;
            bool connectionOpen = false;
            QBSessionManager sessionManager = null;

            try
            {
                sessionManager = new QBSessionManager();
                IMsgSetRequest requestMsgSet = sessionManager.CreateMsgSetRequest("US", 16, 0);
                requestMsgSet.Attributes.OnError = ENRqOnError.roeContinue;

                BuildBillToPayQueryRq(requestMsgSet);
                sessionManager.OpenConnection("", "Bill To Pay Query");
                connectionOpen = true;
                sessionManager.BeginSession("", ENOpenMode.omDontCare);
                sessionBegun = true;

                IMsgSetResponse responseMsgSet = sessionManager.DoRequests(requestMsgSet);
                sessionManager.EndSession();
                sessionBegun = false;
                sessionManager.CloseConnection();
                connectionOpen = false;

                WalkBillToPayQueryRs(responseMsgSet);
            }
            catch (Exception e)
            {
                if (sessionBegun) sessionManager.EndSession();
                if (connectionOpen) sessionManager.CloseConnection();
            }
        }

        public static void BuildBillToPayQueryRq(IMsgSetRequest requestMsgSet)
        {
            IBillToPayQuery BillToPayQueryRq = requestMsgSet.AppendBillToPayQueryRq();
            BillToPayQueryRq.metaData.SetValue(ENmetaData.mdNoMetaData);
            BillToPayQueryRq.PayeeEntityRef.FullName.SetValue("Amazon");
        }

        public static void WalkBillToPayQueryRs(IMsgSetResponse responseMsgSet)
        {
            if (responseMsgSet == null) return;
            IResponseList responseList = responseMsgSet.ResponseList;
            if (responseList == null) return;

            for (int i = 0; i < responseList.Count; i++)
            {
                IResponse response = responseList.GetAt(i);
                if (response.StatusCode >= 0 && response.Detail != null)
                {
                    ENResponseType responseType = (ENResponseType)response.Type.GetValue();
                    if (responseType == ENResponseType.rtBillToPayQueryRs)
                    {
                        IBillToPayRetList BillToPayRet = (IBillToPayRetList)response.Detail;
                        DisplayBillToPayResults(BillToPayRet);
                    }
                }
            }
        }

        public static void DisplayBillToPayResults(IBillToPayRetList BillToPayRet)
        {
            if (BillToPayRet == null) return;
            for (int i = 0; i < BillToPayRet.Count; i++)
            {
                var bills = BillToPayRet.GetAt(i);
                if (bills.ORBillToPayRet?.BillToPay != null)
                {
                    Console.WriteLine("Bills to Pay:");
                    Console.WriteLine("--------------------------------");
                    var bill = bills.ORBillToPayRet.BillToPay;
                    Console.WriteLine($"Txn ID: {bill.TxnID.GetValue()}");
                    Console.WriteLine($"AP Account: {bill.APAccountRef.FullName.GetValue()}");
                    Console.WriteLine($"Due Date: {bill.DueDate.GetValue():d}");
                    Console.WriteLine($"Amount Due: {bill.AmountDue.GetValue():C}");
                    Console.WriteLine("--------------------------------");

                }
            }
        }
    }
}
