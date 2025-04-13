using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using QBFC16Lib;
using System.Diagnostics;  // For Debug.WriteLine if needed

namespace QB_PayBills_Test
{
    [Collection("Sequential Tests")]
    public class PayBillAdderTests
    {
        [Fact]
        public void AddPayBills_ThenCheckTxnIDs_ThenVerifyInQB()
        {
            // We'll keep track of what we create so we can clean up afterward.
            var createdVendorListIDs = new List<string>();
            var createdVendorNames = new List<string>();   // Store the random vendor names
            var createdBillTxnIDs = new List<string>();
            var createdPaymentTxnIDs = new List<string>();  // We expect new BillPaymentCheck TxnIDs
            try
            {
                // 1) Create some random Vendors
                using (var qbSession = new QuickBooksSession(AppConfig.QB_APP_NAME))
                {
                    for (int i = 0; i < 2; i++)
                    {
                        string vendName = "AdderTestVend_" + Guid.NewGuid().ToString("N").Substring(0, 6);
                        string vendListID = AddVendor(qbSession, vendName);
                        createdVendorListIDs.Add(vendListID);
                        createdVendorNames.Add(vendName);
                    }
                }

                // 2) Create two Bills for those Vendors
                //    We'll do purely "expense" bills here for simplicity.
                string billTxnID1, billTxnID2;
                double billTotal1 = 45.00;
                double billTotal2 = 82.00;

                using (var qbSession = new QuickBooksSession(AppConfig.QB_APP_NAME))
                {
                    // Bill #1: from first vendor, referencing "Utilities" & "Office Supplies"
                    billTxnID1 = AddExpenseBillWithTwoLines(
                        qbSession,
                        vendorListID: createdVendorListIDs[0],
                        billDate: DateTime.Today,
                        refNumber: "AdderBillRef_1",
                        memo: "Test Bill #1",
                        expAcct1: "Utilities",
                        amt1: 20.00,
                        expAcct2: "Office Supplies",
                        amt2: 25.00
                    );
                    createdBillTxnIDs.Add(billTxnID1);

                    // Bill #2: from second vendor, referencing "Computer and Internet Expenses" & "Utilities"
                    billTxnID2 = AddExpenseBillWithTwoLines(
                        qbSession,
                        vendorListID: createdVendorListIDs[1],
                        billDate: DateTime.Today,
                        refNumber: "AdderBillRef_2",
                        memo: "Test Bill #2",
                        expAcct1: "Computer and Internet Expenses",
                        amt1: 50.00,
                        expAcct2: "Utilities",
                        amt2: 32.00
                    );
                    createdBillTxnIDs.Add(billTxnID2);
                }

                // 3) Build the PayBill objects we want to add. 
                //    Initially, TxnID should be empty; the Adder sets it once inserted.
                var payBillList = new List<PayBill>();

                // For the first Bill
                payBillList.Add(new PayBill(
                    txnID: "",
                    timeCreated: DateTime.MinValue,
                    txnNumber: 0,
                    payeeListId: createdVendorListIDs[0],      // must match Bill's Vendor
                    vendorName: createdVendorNames[0],         // use the actual random vendor name
                    paymentDate: DateTime.Today,
                    bankList: "",                              // If your code references the bank by ListID, set it here
                    bankName: "Checking",                      // The QB bank account name from which we pay
                    checkAmount: billTotal1,
                    billsPaid: new List<AppliedBill>
                    {
                        new AppliedBill
                        {
                            BillTxnID = billTxnID1,
                            Amount = billTotal1
                        }
                    }
                ));

                // For the second Bill
                payBillList.Add(new PayBill(
                    txnID: "",
                    timeCreated: DateTime.MinValue,
                    txnNumber: 0,
                    payeeListId: createdVendorListIDs[1],
                    vendorName: createdVendorNames[1],
                    paymentDate: DateTime.Today,
                    bankList: "",
                    bankName: "Checking",
                    checkAmount: billTotal2,
                    billsPaid: new List<AppliedBill>
                    {
                        new AppliedBill
                        {
                            BillTxnID = billTxnID2,
                            Amount = billTotal2
                        }
                    }
                ));

                // 4) Call your library method under test
                PayBillAdder.AddPayBills(payBillList);

                // 5) After AddPayBills(...), each PayBill should have a TxnID
                foreach (var pb in payBillList)
                {
                    Assert.False(string.IsNullOrWhiteSpace(pb.TxnID),
                        $"PayBill for vendor='{pb.VendorName}' was not assigned a TxnID after AddPayBills().");
                    createdPaymentTxnIDs.Add(pb.TxnID!);  // store for final cleanup
                }

                // 6) For each newly added PayBill, check QuickBooks by TxnID 
                //    We do our own custom BillPaymentCheckQuery, not the reader code.
                using (var qbSession = new QuickBooksSession(AppConfig.QB_APP_NAME))
                {
                    foreach (var pb in payBillList)
                    {
                        var billPayRet = QueryBillPaymentCheckByTxnID(qbSession, pb.TxnID);
                        Assert.NotNull(billPayRet);  // If not found, test fails
                    }
                }
            }
            finally
            {
                // 7) Cleanup in reverse order: BillPaymentChecks -> Bills -> Vendors
                using (var qbSession = new QuickBooksSession(AppConfig.QB_APP_NAME))
                {
                    foreach (var payTxnID in createdPaymentTxnIDs)
                    {
                        DeleteBillPaymentCheck(qbSession, payTxnID);
                    }
                }

                using (var qbSession = new QuickBooksSession(AppConfig.QB_APP_NAME))
                {
                    foreach (var billTxnID in createdBillTxnIDs)
                    {
                        DeleteBill(qbSession, billTxnID);
                    }
                }

                using (var qbSession = new QuickBooksSession(AppConfig.QB_APP_NAME))
                {
                    foreach (var vendListID in createdVendorListIDs)
                    {
                        DeleteListObject(qbSession, vendListID, ENListDelType.ldtVendor);
                    }
                }
            }
        }

        //------------------------------------------------------------------------------
        // Query a single BillPaymentCheck by TxnID
        //------------------------------------------------------------------------------

        private IBillPaymentCheckRet? QueryBillPaymentCheckByTxnID(QuickBooksSession qbSession, string txnID)
        {
            IMsgSetRequest request = qbSession.CreateRequestSet();
            var bpQuery = request.AppendBillPaymentCheckQueryRq();
            // We'll just search by TxnID:
            bpQuery.ORTxnQuery.TxnIDList.Add(txnID);
            // Optionally, we can request line items or not:
            bpQuery.IncludeLineItems.SetValue(true);

            IMsgSetResponse response = qbSession.SendRequest(request);
            if (response == null || response.ResponseList == null || response.ResponseList.Count == 0)
                return null;

            var firstResp = response.ResponseList.GetAt(0);
            if (firstResp.StatusCode != 0)
            {
                // Could throw an exception or just return null
                Debug.WriteLine($"QueryBillPaymentCheckByTxnID failed: {firstResp.StatusMessage}");
                return null;
            }

            // If success, parse out the BillPaymentCheckRet
            var checkRetList = firstResp.Detail as IBillPaymentCheckRetList;
            // It's possible there's no match or multiple. Typically, you'd expect 0 or 1.
            if (checkRetList == null)
                return null;

            // checkRetList doesn't have a .Count property like arrays, 
            // because IBillPaymentCheckRetList is not a standard List<T>.
            // For single returns, you can just do .GetAt(0) if Count>0.
            if (checkRetList == null)
                return null; // No data returned

            // The BillPaymentCheckQuery in QuickBooks typically returns exactly 1 if TxnID matches
            // or 0 if not found, but let's be safe:
            return checkRetList;
        }

        //------------------------------------------------------------------------------
        // EXAMPLE HELPERS (adapt as needed or place in CommonMethods)
        //------------------------------------------------------------------------------

        private string AddVendor(QuickBooksSession qbSession, string vendorName)
        {
            IMsgSetRequest request = qbSession.CreateRequestSet();
            IVendorAdd vendAdd = request.AppendVendorAddRq();
            vendAdd.Name.SetValue(vendorName);

            IMsgSetResponse response = qbSession.SendRequest(request);
            return ExtractVendorListID(response);
        }

        private string AddExpenseBillWithTwoLines(
            QuickBooksSession qbSession,
            string vendorListID,
            DateTime billDate,
            string refNumber,
            string memo,
            string expAcct1,
            double amt1,
            string expAcct2,
            double amt2
        )
        {
            IMsgSetRequest request = qbSession.CreateRequestSet();
            IBillAdd billAddRq = request.AppendBillAddRq();

            billAddRq.VendorRef.ListID.SetValue(vendorListID);
            billAddRq.TxnDate.SetValue(billDate);
            billAddRq.RefNumber.SetValue(refNumber);
            billAddRq.Memo.SetValue(memo);

            // 1st expense line
            var e1 = billAddRq.ExpenseLineAddList.Append();
            e1.AccountRef.FullName.SetValue(expAcct1);
            e1.Amount.SetValue(amt1);

            // 2nd expense line
            var e2 = billAddRq.ExpenseLineAddList.Append();
            e2.AccountRef.FullName.SetValue(expAcct2);
            e2.Amount.SetValue(amt2);

            var resp = qbSession.SendRequest(request);
            return ExtractBillTxnID(resp);
        }

        private void DeleteBillPaymentCheck(QuickBooksSession qbSession, string txnID)
        {
            IMsgSetRequest request = qbSession.CreateRequestSet();
            var delRq = request.AppendTxnDelRq();
            delRq.TxnDelType.SetValue(ENTxnDelType.tdtBillPaymentCheck);
            delRq.TxnID.SetValue(txnID);

            var resp = qbSession.SendRequest(request);
            CheckForError(resp, $"Deleting BillPaymentCheck TxnID: {txnID}");
        }

        private void DeleteBill(QuickBooksSession qbSession, string txnID)
        {
            IMsgSetRequest request = qbSession.CreateRequestSet();
            var delRq = request.AppendTxnDelRq();
            delRq.TxnDelType.SetValue(ENTxnDelType.tdtBill);
            delRq.TxnID.SetValue(txnID);

            var resp = qbSession.SendRequest(request);
            CheckForError(resp, $"Deleting Bill TxnID: {txnID}");
        }

        private void DeleteListObject(QuickBooksSession qbSession, string listID, ENListDelType listDelType)
        {
            IMsgSetRequest request = qbSession.CreateRequestSet();
            var listDelRq = request.AppendListDelRq();
            listDelRq.ListDelType.SetValue(listDelType);
            listDelRq.ListID.SetValue(listID);

            var resp = qbSession.SendRequest(request);
            CheckForError(resp, $"Deleting {listDelType} {listID}");
        }

        //------------------------------------------------------------------------------
        // EXTRACTORS
        //------------------------------------------------------------------------------

        private string ExtractVendorListID(IMsgSetResponse resp)
        {
            var respList = resp.ResponseList;
            if (respList == null || respList.Count == 0)
                throw new Exception("No response from VendorAdd.");

            IResponse firstResp = respList.GetAt(0);
            if (firstResp.StatusCode != 0)
                throw new Exception($"VendorAdd failed: {firstResp.StatusMessage}");

            var vendRet = firstResp.Detail as IVendorRet;
            if (vendRet == null)
                throw new Exception("No IVendorRet returned.");

            return vendRet.ListID.GetValue();
        }

        private string ExtractBillTxnID(IMsgSetResponse resp)
        {
            var respList = resp.ResponseList;
            if (respList == null || respList.Count == 0)
                throw new Exception("No response from BillAdd.");

            IResponse firstResp = respList.GetAt(0);
            if (firstResp.StatusCode != 0)
                throw new Exception($"BillAdd failed: {firstResp.StatusMessage}");

            var billRet = firstResp.Detail as IBillRet;
            if (billRet == null)
                throw new Exception("No IBillRet returned.");

            return billRet.TxnID.GetValue();
        }

        private void CheckForError(IMsgSetResponse resp, string context)
        {
            if (resp?.ResponseList == null || resp.ResponseList.Count == 0)
                return;

            var firstResp = resp.ResponseList.GetAt(0);
            if (firstResp.StatusCode != 0)
            {
                throw new Exception($"Error {context}: {firstResp.StatusMessage}. Status code: {firstResp.StatusCode}");
            }
            else
            {
                Debug.WriteLine($"OK: {context}");
            }
        }
    }


    //--------------------------------------------------------------------------
    // Example Model classes, if needed
    // (In your scenario, these likely exist in a separate .cs file)
    //--------------------------------------------------------------------------
    public class PayBill
    {
        public string TxnID { get; set; }
        public DateTime TimeCreated { get; set; }
        public int TxnNumber { get; set; }
        public string PayeeListId { get; set; }
        public string VendorName { get; set; }
        public DateTime PaymentDate { get; set; }
        public string BankList { get; set; }
        public string BankName { get; set; }
        public double CheckAmount { get; set; }
        public List<AppliedBill> BillsPaid { get; set; }

        public PayBill(
            string txnID,
            DateTime timeCreated,
            int txnNumber,
            string payeeListId,
            string vendorName,
            DateTime paymentDate,
            string bankList,
            string bankName,
            double checkAmount,
            List<AppliedBill> billsPaid
        )
        {
            TxnID = txnID;
            TimeCreated = timeCreated;
            TxnNumber = txnNumber;
            PayeeListId = payeeListId;
            VendorName = vendorName;
            PaymentDate = paymentDate;
            BankList = bankList;
            BankName = bankName;
            CheckAmount = checkAmount;
            BillsPaid = billsPaid;
        }
    }

    public class AppliedBill
    {
        public string BillTxnID { get; set; } = "";
        public double Amount { get; set; }
        // ... other fields as needed
    }
}
