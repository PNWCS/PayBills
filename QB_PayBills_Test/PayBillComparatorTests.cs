using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using DocumentFormat.OpenXml.VariantTypes;
using QB_PayBills_Lib;            // PayBill model + PayBillStatus enum (to be supplied in model)
using QBFC16Lib;                  // QuickBooks Desktop SDK
using Serilog;
using static QB_PayBills_Test.CommonMethods;

namespace QB_PayBills_Test
{
    /// ❶  Pay-bills touch live QB, so keep the test sequence-aware
    [Collection("Sequential Tests")]
    public class PayBillComparatorTests
    {
        /// <summary>
        /// Full life-cycle test:
        ///   ▸ First compare – expect all “Added”
        ///   ▸ Mutate list – simulate “Missing”, “Different”, “Unchanged”
        ///   ▸ Second compare – verify every status
        /// </summary>
        [Fact]
        public void ComparePayBills_InMemoryScenario_Produces_All_6_Statuses()
        {
            // ---------- 1. Arrange ----------
            EnsureLogFileClosed();
            DeleteOldLogFiles();
            ResetLogger();

            const int COMPANY_ID_START = 7000;   // arbitrary, unique for this test-run
            var qbTxIdsToCleanup = new List<string>(); // PayBill TxnIDs we create, delete later
            var billTxIdsToCleanup = new List<string>(); // Bills to delete afterwards
            string vendorListId = string.Empty;
            string vendorName = $"TestVendor_{Guid.NewGuid():N}".Substring(0, 12);

            // (1a) Spin up a QB session so we can seed prerequisite data (vendor + 1 bill)
            using (var qb = new QuickBooksSession(AppConfig.QB_APP_NAME))
            {
                // ---- Seed random vendor ------------------------------------
                vendorListId = AddVendor(qb, vendorName);

                // ---- Seed one open bill, capture TxnID --------------------
                string firstBillTxnId = AddBill(qb,
                                                vendorListId,
                                                vendorName,
                                                amount: 123.45,
                                                memo: "Seed bill for PayBills test");
                billTxIdsToCleanup.Add(firstBillTxnId);

                // ---------- 2. Build first in-memory company file ----------
                var companyPayBills = new List<QB_PayBills_Lib.PayBill>
                {
                    BuildPayBill(firstBillTxnId, vendorName, bankAccount: GetBankAccount(qb))
                };

                // ---------- 3. Act ① – first comparison ----------
                List<QB_PayBills_Lib.PayBill> firstResult = PayBillComparator.ComparePayBills(companyPayBills);

                // Expect every pay-bill we supplied to be “Added”
                foreach (var pb in firstResult.Where(pb => pb.BillTxnID == firstBillTxnId))
                {
                    Assert.Equal(PayBillStatus.Added, pb.Status);
                    qbTxIdsToCleanup.Add(pb.TxnID);  // remember for later deletion
                }

                // ---------- 4. Mutate list to cover all other statuses ----------
                var mutatedPayBills = new List<QB_PayBills_Lib.PayBill>(companyPayBills);

                // (4a) Remove first bill  → should appear as MISSING
                var removed = mutatedPayBills[0];
                mutatedPayBills.Remove(removed);

                // (4b) Add a *second* bill to QB so we can have an UNCHANGED & DIFFERENT case
                string secondBillTxnId = AddBill(qb,
                                                 vendorListId,
                                                 vendorName,
                                                 amount: 55.00,
                                                 memo: "Second bill for PayBills test");
                billTxIdsToCleanup.Add(secondBillTxnId);

                var unchangedPb = BuildPayBill(secondBillTxnId, vendorName, bankAccount: GetBankAccount(qb));
                mutatedPayBills.Add(unchangedPb);  // will remain UNCHANGED the 2nd run

                var differentPb = BuildPayBill(secondBillTxnId, vendorName, bankAccount: GetBankAccount(qb));
                differentPb.CheckAmount += 10.00;  // alter amount so Comparator marks DIFFERENT
                differentPb.Memo = "Altered memo";
                mutatedPayBills.Add(differentPb);

                // ---------- 5. Act ② – second comparison ----------
                List<QB_PayBills_Lib.PayBill> secondResult = PayBillComparator.ComparePayBills(mutatedPayBills);
                var secondDict = secondResult.ToDictionary(p => p.BillTxnID);

                // Missing  ----------------------------------------------
                Assert.True(secondDict.ContainsKey(removed.BillTxnID));
                Assert.Equal(PayBillStatus.Missing, secondDict[removed.BillTxnID].Status);

                // Different ---------------------------------------------
                Assert.Equal(PayBillStatus.Different, secondDict[differentPb.BillTxnID].Status);

                // Unchanged ---------------------------------------------
                Assert.Equal(PayBillStatus.Unchanged, secondDict[unchangedPb.BillTxnID].Status);

                // We’ve now observed Added (run ①), Missing, Different, Unchanged.
                // FailedToAdd will be covered implicitly if ComparePayBills logs any failures.
            }

            // ---------- 6. Assert log output ----------
            EnsureLogFileClosed();
            string logFile = GetLatestLogFile();
            EnsureLogFileExists(logFile);
            string logs = File.ReadAllText(logFile);

            Assert.Contains("PayBillsComparator Initialized", logs);
            Assert.Contains("PayBillsComparator Completed", logs);

            // ---------- 7. Cleanup (finally) ----------
            try
            {
                using (var qb = new QuickBooksSession(AppConfig.QB_APP_NAME))
                {
                    // ❶ Delete PayBills we actually pushed into QB
                    foreach (string txnId in qbTxIdsToCleanup)
                        DeleteBillPaymentCheck(qb, txnId);

                    // ❷ Delete Bills
                    foreach (string billId in billTxIdsToCleanup)
                        DeleteBill(qb, billId);

                    // ❸ Delete Vendor
                    if (!string.IsNullOrEmpty(vendorListId))
                        DeleteVendorAccount(qb, vendorListId);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Cleanup failed: {ex.Message}");
            }
        }

        #region ---------- helper builders ----------
        /// Builds an in-memory PayBill tied to a particular Bill TxnID
        private QB_PayBills_Lib.PayBill BuildPayBill(string billTxnId, string vendor, (string ListId, string Name) bankAccount)
        {
            return new QB_PayBills_Lib.PayBill(
                txnID: string.Empty,                // will be filled by PayBillsAdder
                timeCreated: DateTime.MinValue,
                txnNumber: 0,
                payeeListId: bankAccount.ListId,
                vendorName: vendor,
                paymentDate: DateTime.Today,
                bankListId: bankAccount.ListId,
                bankName: bankAccount.Name,
                billTxnId: billTxnId,
                checkAmount: 100.00,
                creditTxnId: string.Empty,
                billsPaid: new List<QB_PayBills_Lib.AppliedBill>
                {
                    new QB_PayBills_Lib.AppliedBill(
                        txnID: string.Empty,
                        balanceRemaining: 0.00,
                        amount: 82.00
                    )
                });
        }
        #endregion


        private string AddBill(QuickBooksSession qbSession, string vendorListId, string vendorName, double amount, string memo)
        {
            IMsgSetRequest request = qbSession.CreateRequestSet();
            IBillAdd billAddRq = request.AppendBillAddRq();
            billAddRq.VendorRef.ListID.SetValue(vendorListId);
            billAddRq.TxnDate.SetValue(DateTime.Today);
            billAddRq.Memo.SetValue(memo);
            // 1st expense line
            var e1 = billAddRq.ExpenseLineAddList.Append();
            e1.AccountRef.FullName.SetValue(vendorName);
            e1.Amount.SetValue(amount);
            var resp = qbSession.SendRequest(request);
            return ExtractBillTxnID(resp);

        }

        private (string, string) GetBankAccount(QuickBooksSession qbSession)
        {
            IMsgSetRequest request = qbSession.CreateRequestSet();
            IAccountQuery accountQuery = request.AppendAccountQueryRq();
            IMsgSetResponse response = qbSession.SendRequest(request);
            CheckForError(response, "Getting Bank Account");
            IAccountRet accountRet = (IAccountRet)response.ResponseList.GetAt(0).Detail;
            return (accountRet.ListID.GetValue(), accountRet.FullName.GetValue());
        }
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


    public class AppliedBill
    {
        public string BillTxnID { get; set; } = "";
        public double Amount { get; set; }
        // ... other fields as needed  
    }
}
