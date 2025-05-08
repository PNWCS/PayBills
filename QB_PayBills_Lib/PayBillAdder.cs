using System;
using System.Collections.Generic;
using QBFC16Lib;

namespace QB_PayBills_Lib
{
    public class PayBillAdder
    {
        public static List<PayBill> AddPayBills(List<PayBill> bills)
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

                    List<PayBill> res = WalkBillPaymentCheckAddRs(responseMsgSet);
                    if (res.Count != 0)
                    {
                        res[0].Status = PayBillStatus.Added;
                        list.Add(res[0]);

                        bill.TxnID = res[0].TxnID;

                        Console.WriteLine("------------------------------------");
                        Console.WriteLine("Successfully paid the payment bill");
                        Console.WriteLine("------------------------------------");
                    }

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
            return list;
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

        public static List<PayBill> WalkBillPaymentCheckAddRs(IMsgSetResponse responseMsgSet)
        {
            List<PayBill> list = new List<PayBill>();
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
                        list.Add(WalkBillPaymentCheckRet(result));
                    }
                }
            }
            return list;
        }

        public static PayBill WalkBillPaymentCheckRet(IBillPaymentCheckRet ret)
        {
            if (ret == null) return null;

            string txnID = ret.TxnID?.GetValue() ?? "";
            DateTime timeCreated = ret.TimeCreated?.GetValue() ?? DateTime.Now;
            int txnNumber = ret.TxnNumber?.GetValue() ?? 0;
            string payeeListId = ret.PayeeEntityRef?.ListID?.GetValue() ?? "";
            string payeeFullName = ret.PayeeEntityRef?.FullName?.GetValue() ?? "";
            DateTime paymentDate = ret.TxnDate?.GetValue() ?? DateTime.Now;
            string bankName = ret.BankAccountRef?.FullName?.GetValue() ?? "";
            double amount = ret.Amount?.GetValue() ?? 0;

            List<AppliedBill> appliedBills = new List<AppliedBill>();
            if (ret.AppliedToTxnRetList != null)
            {
                for (int j = 0; j < ret.AppliedToTxnRetList.Count; j++)
                {
                    IAppliedToTxnRet appliedTxn = ret.AppliedToTxnRetList.GetAt(j);
                    string appliedTxnID = appliedTxn.TxnID?.GetValue() ?? "";
                    double balanceRemaining = appliedTxn.BalanceRemaining?.GetValue() ?? 0;
                    double appliedAmount = appliedTxn.Amount?.GetValue() ?? 0;

                    appliedBills.Add(new AppliedBill(appliedTxnID, balanceRemaining, appliedAmount));
                }
            }
            return new PayBill(txnID, timeCreated, txnNumber, payeeListId, payeeFullName, paymentDate, "", bankName, "", amount, "", appliedBills);
        }

        public static List<OpenBills> QueryUnpaidBills(bool display)
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
                            if (display)
                            {
                                Console.WriteLine("-------------------");
                                Console.WriteLine($"Bill Number : {count++}");
                                Console.WriteLine($"TxnID       : {txnID}");
                                Console.WriteLine($"Vendor      : {vendorName}");
                                Console.WriteLine($"AmountDue   : {amountDue}");
                                Console.WriteLine("---------------------");
                            }
                            openBills.Add(new OpenBills(txnID, vendorName, amountDue));
                        }
                    }
                }
            }
            sessionManager.EndSession();
            sessionManager.CloseConnection();

            return openBills;
        }

        public static string GetCreditTxnID(string vendorName)
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
                        if (vendor.Equals(vendorName, StringComparison.OrdinalIgnoreCase))
                        {
                            return creditTxnId;
                        }
                    }
                }
            }
            sessionManager.EndSession();
            sessionManager.CloseConnection();
            return null;
        }
    }
}
