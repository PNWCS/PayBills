using QBFC16Lib;
using Serilog;

namespace QB_PayBills_Lib
{
    public class PayBillReader
    {
        public static List<PayBill> QueryAllPayBills()
        {

            List<PayBill> list = new List<PayBill>();

            bool sessionBegun = false;
            bool connectionOpen = false;
            QBSessionManager sessionManager = null;

            try
            {
                //Create the session Manager object
                sessionManager = new QBSessionManager();

                //Create the message set request object to hold our request
                IMsgSetRequest requestMsgSet = sessionManager.CreateMsgSetRequest("US", 16, 0);
                requestMsgSet.Attributes.OnError = ENRqOnError.roeContinue;

                BuildBillPaymentCheckQueryRq(requestMsgSet);

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

                list = WalkBillPaymentCheckQueryRs(responseMsgSet);

                return list;
            }
            catch (Exception e)
            {
                if (sessionBegun)
                {
                    return list;
                    sessionManager.EndSession();
                }
                if (connectionOpen)
                {
                    return list;
                    sessionManager.CloseConnection();
                }
                return list;
            }
        }
        public static void BuildBillPaymentCheckQueryRq(IMsgSetRequest requestMsgSet)
        {
            IBillPaymentCheckQuery BillPaymentCheckQueryRq = requestMsgSet.AppendBillPaymentCheckQueryRq();
            //Set attributes
            //Set field value for metaData
            BillPaymentCheckQueryRq.metaData.SetValue(ENmetaData.mdNoMetaData);

            BillPaymentCheckQueryRq.IncludeLineItems.SetValue(true);

        }

        public static List<PayBill> WalkBillPaymentCheckQueryRs(IMsgSetResponse responseMsgSet)
        {
            List<PayBill> list = new List<PayBill>();
            if (responseMsgSet == null) return list;
            IResponseList responseList = responseMsgSet.ResponseList;
            if (responseList == null) return list;
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
                        if (responseType == ENResponseType.rtBillPaymentCheckQueryRs)
                        {
                            //upcast to more specific type here, this is safe because we checked with response.Type check above
                            IBillPaymentCheckRetList BillPaymentCheckRet = (IBillPaymentCheckRetList)response.Detail;
                            list = WalkBillPaymentCheckRet(BillPaymentCheckRet);
                        }
                    }
                }
            }
            return list;
        }

        public static List<PayBill> WalkBillPaymentCheckRet(IBillPaymentCheckRetList BillPaymentCheckRet)
        {
            List<PayBill> list = new List<PayBill>();
            for (int i = 0; i < BillPaymentCheckRet.Count; i++)
            {
                var bill = BillPaymentCheckRet.GetAt(i);
                if (BillPaymentCheckRet == null) return list;
                //Go through all the elements of IBillPaymentCheckRetList
                //Get value of TxnID
                String txnID = "";
                if (bill.TxnID != null)
                {
                    txnID = (string)bill.TxnID.GetValue();
                }
                //Get value of TimeCreated
                DateTime timeCreated = DateTime.Now;
                if (bill.TimeCreated != null)
                {
                    timeCreated = (DateTime)bill.TimeCreated.GetValue();
                }
                //Get value of TxnNumber
                int txnNumber = 0;
                if (bill.TxnNumber != null)
                {
                    txnNumber = (int)bill.TxnNumber.GetValue();
                }
                string payeelistId = "";
                string payeefullname = "";
                if (bill.PayeeEntityRef != null)
                {
                    //Get value of ListID
                    if (bill.PayeeEntityRef.ListID != null)
                    {
                        payeelistId = (string)bill.PayeeEntityRef.ListID.GetValue();
                    }
                    //Get value of FullName
                    if (bill.PayeeEntityRef.FullName != null)
                    {
                        payeefullname = (string)bill.PayeeEntityRef.FullName.GetValue();
                    }
                }
                //Get value of TxnDate
                DateTime PaymentDate = DateTime.Now;
                if (bill.TxnDate != null)
                {
                    PaymentDate = (DateTime)bill.TxnDate.GetValue();
                }
                string banklist = "";
                string bankname = "";
                if (bill.BankAccountRef != null)
                {
                    //Get value of ListID
                    if (bill.BankAccountRef.ListID != null)
                    {
                        banklist = (string)bill.BankAccountRef.ListID.GetValue();
                    }
                    //Get value of FullName
                    if (bill.BankAccountRef.FullName != null)
                    {
                        bankname = (string)bill.BankAccountRef.FullName.GetValue();
                    }
                }
                //Get value of Amount
                double amount = 0;
                if (bill.Amount != null)
                {
                    amount = (double)bill.Amount.GetValue();
                }
                string memo = "";
                if (bill.Memo != null)
                {
                    memo = (string)bill.Memo.GetValue();
                }
                List<AppliedBill> appliedBills = new List<AppliedBill>();
                if (bill.AppliedToTxnRetList != null)
                {

                    for (int j = 0; j < bill.AppliedToTxnRetList.Count; j++)
                    {
                        IAppliedToTxnRet AppliedToTxnRet = bill.AppliedToTxnRetList.GetAt(j);
                        //Get value of TxnID

                        string TxnID = (string)AppliedToTxnRet.TxnID.GetValue();
                        //Get value of BalanceRemaining
                        double BalanceRemaining = 0;
                        if (AppliedToTxnRet.BalanceRemaining != null)
                        {
                            BalanceRemaining = (double)AppliedToTxnRet.BalanceRemaining.GetValue();
                        }
                        //Get value of Amount
                        double Amount = 0;
                        if (AppliedToTxnRet.Amount != null)
                        {
                            Amount = (double)AppliedToTxnRet.Amount.GetValue();
                        }

                        AppliedBill appliedbill = new AppliedBill(TxnID, BalanceRemaining, Amount);
                        appliedBills.Add(appliedbill);
                    }
                }

                PayBill payBill = new PayBill(txnID, timeCreated, txnNumber, payeelistId, payeefullname, PaymentDate, banklist, bankname, "", amount, "", appliedBills);
                Console.WriteLine("Bill payement by Check");
                Console.WriteLine("---------------------------------------------------");
                Console.WriteLine($"Transaction Id : {txnID}");
                Console.WriteLine($"Transaction Number : {txnNumber}");
                Console.WriteLine($"Time created : {timeCreated}");
                Console.WriteLine($"Payee Id :{payeelistId}");
                Console.WriteLine($"Payee Name : {payeefullname}");
                Console.WriteLine($"Transaction date : {PaymentDate}");
                Console.WriteLine($"Bank Name : {bankname}");
                Console.WriteLine($"Amount : {amount}");
                Console.WriteLine("---------------------------------------------------");

                list.Add(payBill);

            }

            return list;

        }
    }
}