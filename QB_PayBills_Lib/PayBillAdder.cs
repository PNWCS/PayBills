using System;
using System.Collections.Generic;
using QBFC16Lib;

namespace QB_PayBills_Lib
{
    public class PayBillAdder
    {
        public static void AddPayBills(List<PayBill> bills)
        {
            List<PayBill> list = new List<PayBill>();
            foreach (var bill in bills)
            {
                bool sessionBegun = false;
                bool connectionOpen = false;
                QBSessionManager sessionManager = null;

                try
                {
                    sessionManager = new QBSessionManager();

                    IMsgSetRequest requestMsgSet = sessionManager.CreateMsgSetRequest("US", 16, 0);
                    requestMsgSet.Attributes.OnError = ENRqOnError.roeContinue;

                    BuildBillPaymentCheckAddRq(requestMsgSet, bill);

                    sessionManager.OpenConnection("", "PayBills Test Runner");
                    connectionOpen = true;
                    sessionManager.BeginSession("", ENOpenMode.omDontCare);
                    sessionBegun = true;

                    IMsgSetResponse responseMsgSet = sessionManager.DoRequests(requestMsgSet);

                    sessionManager.EndSession();
                    sessionBegun = false;
                    sessionManager.CloseConnection();
                    connectionOpen = false;

                    PayBill res = WalkBillPaymentCheckAddRs(responseMsgSet);

                    bill.TxnID = res.TxnID;

                    Console.WriteLine("------------------------------------");
                    Console.WriteLine("Successfully paid the payment bill");
                    Console.WriteLine("------------------------------------");
                }
                catch (Exception e)
                {
                    Console.WriteLine("Bill NOT paid");
                    Console.WriteLine($"Exception: {e.Message}");

                    if (sessionBegun)
                        sessionManager.EndSession();
                    if (connectionOpen)
                        sessionManager.CloseConnection();
                }
            }
        }

        public static void BuildBillPaymentCheckAddRq(IMsgSetRequest requestMsgSet, PayBill bill)
        {
            IBillPaymentCheckAdd BillPaymentCheckAddRq = requestMsgSet.AppendBillPaymentCheckAddRq();

            BillPaymentCheckAddRq.PayeeEntityRef.FullName.SetValue(bill.VendorName);
            BillPaymentCheckAddRq.TxnDate.SetValue(bill.TimeCreated);
            BillPaymentCheckAddRq.BankAccountRef.FullName.SetValue(bill.BankName);

            // Always mark as to be printed
            BillPaymentCheckAddRq.ORCheckPrint.IsToBePrinted.SetValue(true);

            IAppliedToTxnAdd appliedTxn = BillPaymentCheckAddRq.AppliedToTxnAddList.Append();
            appliedTxn.TxnID.SetValue(bill.BillTxnID);
            appliedTxn.PaymentAmount.SetValue(bill.CheckAmount);

            // Conditionally add credit if available
            if (!string.IsNullOrWhiteSpace(bill.CreditTxnID))
            {
                ISetCredit credit = appliedTxn.SetCreditList.Append();
                credit.CreditTxnID.SetValue(bill.CreditTxnID);
                credit.AppliedAmount.SetValue(bill.CheckAmount);
                credit.Override.SetValue(true);
            }

            // Add element to return (for possible validation)
            BillPaymentCheckAddRq.IncludeRetElementList.Add("TxnID");
        }

        public static PayBill WalkBillPaymentCheckAddRs(IMsgSetResponse responseMsgSet)
        {
            PayBill list = null;
            if (responseMsgSet == null) return list;
            IResponseList responseList = responseMsgSet.ResponseList;
            if (responseList == null) return list;

            for (int i = 0; i < responseList.Count; i++)
            {
                IResponse response = responseList.GetAt(i);

                if (response.StatusCode >= 0 && response.Detail != null)
                {
                    if ((ENResponseType)response.Type.GetValue() == ENResponseType.rtBillPaymentCheckAddRs)
                    {
                        IBillPaymentCheckRet result = (IBillPaymentCheckRet)response.Detail;
                        list = WalkBillPaymentCheckRet(result);
                    }
                }
            }
            return list;
        }

        public static PayBill WalkBillPaymentCheckRet(IBillPaymentCheckRet ret)
        {
            PayBill list = null;
            var bill = ret;
            if (ret == null) return list;
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

            list = new PayBill(txnID, timeCreated, txnNumber, payeelistId, payeefullname, PaymentDate, banklist, bankname, "", amount, "", appliedBills);
            Console.WriteLine("Bill payement by Check");
            Console.WriteLine("---------------------------------------------------");
            Console.WriteLine($"Transaction Id :  ${txnID}");
            Console.WriteLine($"Transaction Number : {txnNumber}");
            Console.WriteLine($"Time created : {timeCreated}");
            Console.WriteLine($"Payee Id :{payeelistId}");
            Console.WriteLine($"Payee Name : {payeefullname}");
            Console.WriteLine($"Transaction date : {PaymentDate}");
            Console.WriteLine($"Bank Name : {bankname}");
            Console.WriteLine($"Amount : {amount}");
            Console.WriteLine("---------------------------------------------------");
            return list;
        }

        public static List<OpenBills> QueryUnpaidBills()
        {
            List<OpenBills> openBills = new List<OpenBills>();
            int count = 1;

            QBSessionManager sessionManager = new QBSessionManager();
            IMsgSetRequest requestMsgSet = sessionManager.CreateMsgSetRequest("US", 16, 0);
            requestMsgSet.Attributes.OnError = ENRqOnError.roeContinue;

            IBillQuery billQuery = requestMsgSet.AppendBillQueryRq();

            sessionManager.OpenConnection("", "QueryUnpaidBills");
            sessionManager.BeginSession("", ENOpenMode.omDontCare);

            IMsgSetResponse responseMsgSet = sessionManager.DoRequests(requestMsgSet);
            IResponseList responses = responseMsgSet.ResponseList;

            for (int i = 0; i < responses.Count; i++)
            {
                IResponse response = responses.GetAt(i);
                if (response.Detail is IBillRetList billList)
                {
                    for (int j = 0; j < billList.Count; j++)
                    {
                        IBillRet bill = billList.GetAt(j);
                        if (!bill.IsPaid.GetValue())
                        {
                            string txnID = bill.TxnID?.GetValue();
                            string vendorName = bill.VendorRef?.FullName?.GetValue() ?? "Unknown Vendor";
                            double amountDue = bill.AmountDue.GetValue();

                            Console.WriteLine("----- Open Bill -----");
                            Console.WriteLine($"#{count++}");
                            Console.WriteLine($"TxnID     : {txnID}");
                            Console.WriteLine($"Vendor    : {vendorName}");
                            Console.WriteLine($"AmountDue : {amountDue}");
                            Console.WriteLine("---------------------");

                            openBills.Add(new OpenBills(txnID, vendorName, amountDue));
                        }
                    }
                }
            }

            sessionManager.EndSession();
            sessionManager.CloseConnection();

            return openBills;
        }

        public static void GetCreditTxnID()
        {
            QBSessionManager sessionManager = new QBSessionManager();
            IMsgSetRequest requestMsgSet = sessionManager.CreateMsgSetRequest("US", 16, 0);
            requestMsgSet.Attributes.OnError = ENRqOnError.roeContinue;

            IVendorCreditQuery creditQuery = requestMsgSet.AppendVendorCreditQueryRq();

            sessionManager.OpenConnection("", "QueryVendorCredits");
            sessionManager.BeginSession("", ENOpenMode.omDontCare);

            IMsgSetResponse responseMsgSet = sessionManager.DoRequests(requestMsgSet);
            IResponseList responseList = responseMsgSet.ResponseList;

            for (int i = 0; i < responseList.Count; i++)
            {
                IResponse response = responseList.GetAt(i);

                if (response.StatusCode >= 0 && response.Detail != null &&
                    (ENResponseType)response.Type.GetValue() == ENResponseType.rtVendorCreditQueryRs)
                {
                    IVendorCreditRetList creditList = (IVendorCreditRetList)response.Detail;
                    for (int j = 0; j < creditList.Count; j++)
                    {
                        IVendorCreditRet credit = creditList.GetAt(j);
                        string creditTxnId = credit.TxnID?.GetValue();
                        string vendor = credit.VendorRef?.FullName?.GetValue() ?? "Unknown Vendor";

                        Console.WriteLine($"Credit Found: TxnID = {creditTxnId}, Vendor = {vendor}");
                    }
                }
            }

            sessionManager.EndSession();
            sessionManager.CloseConnection();
        }
    }
}
