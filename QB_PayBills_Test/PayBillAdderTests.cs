using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using QBFC16Lib;
using System.Diagnostics;  // For Debug.WriteLine if needed
using QB_PayBills_Lib;
using Serilog;

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
            var createdCreditTxnIDs = new List<string>();  // We expect new VendorCredit TxnIDs
            var createdVendorAccountIDs = new List<string>(); // Store the random vendor account IDs
            try
            {
                // 1) Create some random Vendors
                using (var qbSession = new QuickBooksSession(AppConfig.QB_APP_NAME))
                {
                    for (int i = 0; i < 2; i++)
                    {
                        string vendName = "AdderTestVend_" + Guid.NewGuid().ToString("N").Substring(0, 6);
                        string vendListID = AddVendor(qbSession, vendName);
                        Assert.False(string.IsNullOrWhiteSpace(vendListID), "Vendor ListID was not created.");

                        string vendorAccountListID = AddVendorAccount(qbSession, vendName);
                        Assert.False(string.IsNullOrWhiteSpace(vendorAccountListID), "Vendor Account ListID was not created.");
                        createdVendorAccountIDs.Add(vendorAccountListID);
                        string vendcreditID = AddVendorCredit(qbSession, vendName, vendListID);
                        Assert.False(string.IsNullOrWhiteSpace(vendcreditID), "Vendor Credit TxnID was not created.");
                        createdCreditTxnIDs.Add(vendcreditID);
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
                var payBillList = new List<QB_PayBills_Lib.PayBill>();

                // For the first Bill
                payBillList.Append(new QB_PayBills_Lib.PayBill(
                txnID: "",
                timeCreated: DateTime.MinValue,
                txnNumber: 0,
                payeeListId: createdVendorListIDs[0],
                vendorName: createdVendorNames[0],
                paymentDate: DateTime.Today,
                bankListId: "",
                bankName: "Checking",
                billTxnId: createdBillTxnIDs[0],
                checkAmount: 45.00,
                creditTxnId: createdCreditTxnIDs[0],
                billsPaid: new List<QB_PayBills_Lib.AppliedBill>
                {
                    new QB_PayBills_Lib.AppliedBill(
                        txnID: createdBillTxnIDs[0],
                        balanceRemaining: 0.00,
                        amount: 82.00
                    )
                }
                ));

                // For the second Bill
                payBillList.Append(new QB_PayBills_Lib.PayBill(
                txnID: "",
                timeCreated: DateTime.MinValue,
                txnNumber: 0,
                payeeListId: createdVendorListIDs[1],
                vendorName: createdVendorNames[1],
                paymentDate: DateTime.Today,
                bankListId: "",
                bankName: "Checking",
                billTxnId: createdBillTxnIDs[1],
                checkAmount: 82.00,
                creditTxnId: createdCreditTxnIDs[1],
                billsPaid: new List<QB_PayBills_Lib.AppliedBill>
                {
                    new QB_PayBills_Lib.AppliedBill(
                        txnID: createdBillTxnIDs[1],
                        balanceRemaining: 0.00,
                        amount: 82.00
                    )
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
                    foreach (var creditTxnID in createdCreditTxnIDs)
                    {
                        DeleteVendorCredit(qbSession, creditTxnID);
                    }
                }

                using (var qbSession = new QuickBooksSession(AppConfig.QB_APP_NAME))
                {
                    foreach (var vendorAccountListID in createdVendorAccountIDs)
                    {
                        DeleteVendorAccount(qbSession, vendorAccountListID);
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
            if (checkRetList == null || checkRetList.Count == 0)
                return null; // No data returned  

            // Return the first item in the list  
            return checkRetList.GetAt(0);
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

        private string AddVendorAccount(QuickBooksSession qbSession, string vendorName)
        {
            IMsgSetRequest request = qbSession.CreateRequestSet();
            IAccountAdd AccountAddRq = request.AppendAccountAddRq();

            AccountAddRq.Name.SetValue(vendorName); // Set the account name
            AccountAddRq.IsActive.SetValue(true);  // Mark the account as active
            AccountAddRq.AccountType.SetValue(ENAccountType.atExpense); // Set the required AccountType (e.g., Expense)

            IMsgSetResponse response = qbSession.SendRequest(request);
            CheckForError(response, $"Adding Vendor Account: {vendorName}");
            IAccountRet details = (IAccountRet)response.ResponseList.GetAt(0).Detail;
            return details.ListID.GetValue();
        }




        private string AddVendorCredit(QuickBooksSession qbSession, string vendorName, string vendorListId)
        {
            IMsgSetRequest request = qbSession.CreateRequestSet();
            IVendorCreditAdd vendAdd = request.AppendVendorCreditAddRq();
            vendAdd.VendorRef.ListID.SetValue(vendorListId);
            IExpenseLineAdd ExpenseLineAdd = vendAdd.ExpenseLineAddList.Append();
            ExpenseLineAdd.AccountRef.FullName.SetValue(vendorName);
            ExpenseLineAdd.Amount.SetValue(10000);


            IMsgSetResponse response = qbSession.SendRequest(request);
            return ExtractCreditTxnID(response);
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

        private void DeleteVendorCredit(QuickBooksSession qbSession, string txnID)
        {
            IMsgSetRequest request = qbSession.CreateRequestSet();
            var delRq = request.AppendTxnDelRq();
            delRq.TxnDelType.SetValue(ENTxnDelType.tdtVendorCredit);
            delRq.TxnID.SetValue(txnID);

            var resp = qbSession.SendRequest(request);
            CheckForError(resp, $"Deleting VendorCredit TxnID: {txnID}");
        }

        private void DeleteVendorAccount(QuickBooksSession qbSession, string account)
        {
            IMsgSetRequest request = qbSession.CreateRequestSet();
            var listDelRq = request.AppendListDelRq();
            listDelRq.ListDelType.SetValue(ENListDelType.ldtAccount);
            listDelRq.ListID.SetValue(account);

            var resp = qbSession.SendRequest(request);
            CheckForError(resp, $"Deleting Account {account}");
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


        private string ExtractCreditTxnID(IMsgSetResponse resp)
        {
            var respList = resp.ResponseList;
            if (respList == null || respList.Count == 0)
                throw new Exception("No response from BillAdd.");

            IResponse firstResp = respList.GetAt(0);
            if (firstResp.StatusCode != 0)
                throw new Exception($"CreditAdd failed: {firstResp.StatusMessage}");

            var billRet = firstResp.Detail as IVendorCreditRet;
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
        public string BankListId { get; set; }
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
            string bankListId,
            string bankName,
            string billTxnId,
            double checkAmount,
            string creditTxnId,
            List<AppliedBill> billsPaid
        )
        {
            TxnID = txnID;
            TimeCreated = timeCreated;
            TxnNumber = txnNumber;
            PayeeListId = payeeListId;
            VendorName = vendorName;
            PaymentDate = paymentDate;
            BankListId = bankListId;
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
