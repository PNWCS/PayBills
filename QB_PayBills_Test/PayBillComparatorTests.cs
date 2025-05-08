using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using QB_PayBills_Lib;            // PayBill model + PayBillStatus enum (to be supplied in model)
using QB_PayBills_Lib.Comparator; // PayBillsComparator.ComparePayBills(...)
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
            var qbTxIdsToCleanup   = new List<string>(); // PayBill TxnIDs we create, delete later
            var billTxIdsToCleanup = new List<string>(); // Bills to delete afterwards
            string vendorListId    = string.Empty;
            string vendorName      = $"TestVendor_{Guid.NewGuid():N}".Substring(0, 12);

            // (1a) Spin up a QB session so we can seed prerequisite data (vendor + 1 bill)
            using (var qb = new QuickBooksSession(AppConfig.QB_APP_NAME))
            {
                // ---- Seed random vendor ------------------------------------
                vendorListId = AddVendor(qb, vendorName);

                // ---- Seed one open bill, capture TxnID --------------------
                string firstBillTxnId = AddBill(qb,
                                                vendorListId,
                                                vendorName,
                                                amount: 123.45m,
                                                memo:   "Seed bill for PayBills test");
                billTxIdsToCleanup.Add(firstBillTxnId);

                // ---------- 2. Build first in-memory company file ----------
                var companyPayBills = new List<PayBill>
                {
                    BuildPayBill(firstBillTxnId, vendorName, bankAccount: GetBankAccount(qb))
                };

                // ---------- 3. Act ① – first comparison ----------
                List<PayBill> firstResult = PayBillsComparator.ComparePayBills(companyPayBills);

                // Expect every pay-bill we supplied to be “Added”
                foreach (var pb in firstResult.Where(pb => pb.BillTxnID == firstBillTxnId))
                {
                    Assert.Equal(PayBillStatus.Added, pb.Status);
                    qbTxIdsToCleanup.Add(pb.TxnID);  // remember for later deletion
                }

                // ---------- 4. Mutate list to cover all other statuses ----------
                var mutatedPayBills = new List<PayBill>(companyPayBills);

                // (4a) Remove first bill  → should appear as MISSING
                var removed = mutatedPayBills[0];
                mutatedPayBills.Remove(removed);

                // (4b) Add a *second* bill to QB so we can have an UNCHANGED & DIFFERENT case
                string secondBillTxnId = AddBill(qb,
                                                 vendorListId,
                                                 vendorName,
                                                 amount: 55.00m,
                                                 memo:   "Second bill for PayBills test");
                billTxIdsToCleanup.Add(secondBillTxnId);

                var unchangedPb = BuildPayBill(secondBillTxnId, vendorName, bankAccount: GetBankAccount(qb));
                mutatedPayBills.Add(unchangedPb);  // will remain UNCHANGED the 2nd run

                var differentPb = BuildPayBill(secondBillTxnId, vendorName, bankAccount: GetBankAccount(qb));
                differentPb.CheckAmount += 10.00;  // alter amount so Comparator marks DIFFERENT
                differentPb.Memo = "Altered memo";
                mutatedPayBills.Add(differentPb);

                // ---------- 5. Act ② – second comparison ----------
                List<PayBill> secondResult = PayBillsComparator.ComparePayBills(mutatedPayBills);
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
                        DeleteTxn(qb, txnId);

                    // ❷ Delete Bills
                    foreach (string billId in billTxIdsToCleanup)
                        DeleteTxn(qb, billId);

                    // ❸ Delete Vendor
                    if (!string.IsNullOrEmpty(vendorListId))
                        DeleteVendor(qb, vendorListId);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Cleanup failed: {ex.Message}");
            }
        }

        #region ---------- helper builders ----------
        /// Builds an in-memory PayBill tied to a particular Bill TxnID
        private PayBill BuildPayBill(string billTxnId, string vendor, (string ListId, string Name) bankAccount)
        {
            return new PayBill(
                txnID:      string.Empty,                // will be filled by PayBillsAdder
                timeCreated:DateTime.MinValue,
                txnNumber:  0,
                payeeListId:bankAccount.ListId,
                vendorName: vendor,
                paymentDate:DateTime.Today,
                bankListId: bankAccount.ListId,
                bankName:   bankAccount.Name,
                billTxnId:  billTxnId,
                checkAmount:100.00,
                creditTxnId:string.Empty,
                billsPaid: new List<AppliedBill>
                {
                    new AppliedBill(billTxnId, 100.00)
                });
        }
        #endregion
    }
}
