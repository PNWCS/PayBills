using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QBFC16Lib;

namespace QB_PayBills_Lib
{
    public class PayBillAdder
    {
        public static void AddPayBill(List<AddBill> bills)
        {

            bool sessionBegun = false;
            bool connectionOpen = false;
            QBSessionManager sessionManager = null;
            for (int i = 0; i < bills.Count; i++)
            {
                try
                {
                    //Create the session Manager object
                    sessionManager = new QBSessionManager();

                    //Create the message set request object to hold our request
                    IMsgSetRequest requestMsgSet = sessionManager.CreateMsgSetRequest("US", 16, 0);
                    requestMsgSet.Attributes.OnError = ENRqOnError.roeContinue;

                    BuildBillPaymentCheckAddRq(requestMsgSet, bills[i]);

                    //Connect to QuickBooks and begin a session
                    sessionManager.OpenConnection("", "Sample Code from OSR");
                    connectionOpen = true;
                    sessionManager.BeginSession("", ENOpenMode.omDontCare);
                    sessionBegun = true;

                    //Send the request and get the response from QuickBooks
                    IMsgSetResponse responseMsgSet = sessionManager.DoRequests(requestMsgSet);

                    //End the session and close the connection to QuickBooks
                    sessionManager.EndSession();
                    sessionBegun = false;
                    sessionManager.CloseConnection();
                    connectionOpen = false;

                    WalkBillPaymentCheckAddRs(responseMsgSet);
                    Console.WriteLine("------------------------------------");
                    Console.WriteLine("Successfully paid the payment bill");
                    Console.WriteLine("------------------------------------");
                }
                catch (Exception e)
                {
                    Console.WriteLine("Bill Not paid ");
                    Console.WriteLine($"Exception: {e.Message}");
                    if (sessionBegun)
                    {
                        sessionManager.EndSession();
                    }
                    if (connectionOpen)
                    {
                        sessionManager.CloseConnection();
                    }
                }
            }
        }

        public static void BuildBillPaymentCheckAddRq(IMsgSetRequest requestMsgSet, AddBill bill)
        {
            IBillPaymentCheckAdd BillPaymentCheckAddRq = requestMsgSet.AppendBillPaymentCheckAddRq();
            //Set field value for FullName
            BillPaymentCheckAddRq.PayeeEntityRef.FullName.SetValue(bill.PayeeName);
            //Set field value for TxnDate
            BillPaymentCheckAddRq.TxnDate.SetValue(bill.TxnDate);
            //Set field value for FullName
            BillPaymentCheckAddRq.BankAccountRef.FullName.SetValue(bill.BankName);
            string ORCheckPrintElementType1159 = "IsToBePrinted";
            if (ORCheckPrintElementType1159 == "IsToBePrinted")
            {
                //Set field value for IsToBePrinted
                BillPaymentCheckAddRq.ORCheckPrint.IsToBePrinted.SetValue(true);
            }
            if (ORCheckPrintElementType1159 == "RefNumber")
            {
                //Set field value for RefNumber
                BillPaymentCheckAddRq.ORCheckPrint.RefNumber.SetValue("ab");
            }
            //BillPaymentCheckAddRq.Memo.SetValue("ab");
            IAppliedToTxnAdd AppliedToTxnAdd1160 = BillPaymentCheckAddRq.AppliedToTxnAddList.Append();
            AppliedToTxnAdd1160.TxnID.SetValue(bill.TxnID);
            AppliedToTxnAdd1160.PaymentAmount.SetValue(bill.PaymentAmount);
            ISetCredit SetCredit1161 = AppliedToTxnAdd1160.SetCreditList.Append();
            SetCredit1161.CreditTxnID.SetValue(bill.CreditTxnID);
            //Set attributes
            //Set field value for AppliedAmount
            SetCredit1161.AppliedAmount.SetValue(bill.AppliedAmount);
            //Set field value for Override
            SetCredit1161.Override.SetValue(true);
            //Set field value for IncludeRetElementList
            //May create more than one of these if needed
            BillPaymentCheckAddRq.IncludeRetElementList.Add("Chase");
        }

        public static void WalkBillPaymentCheckAddRs(IMsgSetResponse responseMsgSet)
        {
            if (responseMsgSet == null) return;
            IResponseList responseList = responseMsgSet.ResponseList;
            if (responseList == null) return;
            //if we sent only one request, there is only one response, we'll walk the list for this sample
            for (int i = 0; i < responseList.Count; i++)
            {
                IResponse response = responseList.GetAt(i);
                //check the status code of the response, 0=ok, >0 is warning
                if (response.StatusCode >= 0)
                {
                    //the request-specific response is in the details, make sure we have some
                    if (response.Detail != null)
                    {
                        //make sure the response is the type we're expecting
                        ENResponseType responseType = (ENResponseType)response.Type.GetValue();
                        if (responseType == ENResponseType.rtBillPaymentCheckAddRs)
                        {
                            //upcast to more specific type here, this is safe because we checked with response.Type check above
                            IBillPaymentCheckRet BillPaymentCheckRet = (IBillPaymentCheckRet)response.Detail;
                            WalkBillPaymentCheckRet(BillPaymentCheckRet);
                        }
                    }
                }
            }
        }

        public static void WalkBillPaymentCheckRet(IBillPaymentCheckRet BillPaymentCheckRet)
        {
            if (BillPaymentCheckRet == null) return;
            //Go through all the elements of IBillPaymentCheckRet
            //Get value of TxnID
            string TxnId = "";
            if (BillPaymentCheckRet.TxnID != null)
            {
                TxnId = (string)BillPaymentCheckRet.TxnID.GetValue();
            }
            //Get value of TimeCreated
            DateTime TimeCreated = DateTime.Now;
            if (BillPaymentCheckRet.TimeCreated != null)
            {
                TimeCreated = (DateTime)BillPaymentCheckRet.TimeCreated.GetValue();
            }
            string payeeListId = "";
            string payeeName = "";
            if (BillPaymentCheckRet.PayeeEntityRef != null)
            {
                //Get value of ListID
                if (BillPaymentCheckRet.PayeeEntityRef.ListID != null)
                {
                    payeeListId = (string)BillPaymentCheckRet.PayeeEntityRef.ListID.GetValue();
                }
                //Get value of FullName
                if (BillPaymentCheckRet.PayeeEntityRef.FullName != null)
                {
                    payeeName = (string)BillPaymentCheckRet.PayeeEntityRef.FullName.GetValue();
                }
            }
            string bankListId = "";
            string bankName = "";
            if (BillPaymentCheckRet.BankAccountRef != null)
            {
                //Get value of ListID
                if (BillPaymentCheckRet.BankAccountRef.ListID != null)
                {
                    bankListId = (string)BillPaymentCheckRet.BankAccountRef.ListID.GetValue();
                }
                //Get value of FullName
                if (BillPaymentCheckRet.BankAccountRef.FullName != null)
                {
                    bankName = (string)BillPaymentCheckRet.BankAccountRef.FullName.GetValue();
                }
            }
            //Get value of Amount
            double amount = 0;
            if (BillPaymentCheckRet.Amount != null)
            {
                amount = (double)BillPaymentCheckRet.Amount.GetValue();
            }

            Console.WriteLine("Bill Paid :");
            Console.WriteLine("-------------------------");
            Console.WriteLine($"TxnId : {TxnId}");
            Console.WriteLine($"Time Created : {TimeCreated}");
            Console.WriteLine($"Payee Name : {payeeName}");
            Console.WriteLine($"Bank Account : {bankName}");
            Console.WriteLine($"Amount : {amount}");
            Console.WriteLine("-------------------------");
        }

    }
}
