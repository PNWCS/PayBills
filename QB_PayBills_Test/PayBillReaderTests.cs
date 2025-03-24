using System.Diagnostics;
using Serilog;
using QBFC16Lib;
using static QB_PayBills_Test.CommonMethods; // Reuse or adapt your shared helpers

namespace QB_PayBill_Test
{
    [Collection("Sequential Tests")]
    public class PayBillReaderTests
    {
        [Fact]
        public void CreateVendorsPartsBills_ThenPayBills()
        {
            // We track everything for final cleanup:
            var createdVendorListIDs = new List<string>();
            var createdPartListIDs = new List<string>();
            var createdBillTxnIDs = new List<string>();
            var createdPaymentTxnIDs = new List<string>();

            // We'll store vendor / part names for clarity
            var vendorNames = new List<string>();
            var partNames = new List<string>();
            var partPrices = new List<double>();

            // We'll store Bill test data
            var billTestData = new List<BillTestInfo>();

            // We'll store Payment test data
            var paymentTestData = new List<PayBillTestInfo>();

            try
            {
                // 1) Clean logs
                EnsureLogFileClosed();
                DeleteOldLogFiles();
                ResetLogger();

                // 2) Create 2 vendors
                using (var qbSession = new QuickBooksSession(AppConfig.QB_APP_NAME))
                {
                    for (int i = 0; i < 2; i++)
                    {
                        string vendName = "RandVend_" + Guid.NewGuid().ToString("N").Substring(0, 6);
                        string vendListID = AddVendor(qbSession, vendName);
                        createdVendorListIDs.Add(vendListID);
                        vendorNames.Add(vendName);
                    }
                }

                // 3) Create 2 parts (inventory items), one for each vendor scenario
                using (var qbSession = new QuickBooksSession(AppConfig.QB_APP_NAME))
                {
                    for (int i = 0; i < 2; i++)
                    {
                        // We'll have 2 random parts
                        string partName = "RandPart_" + Guid.NewGuid().ToString("N").Substring(0, 6);
                        double partPrice = 7.50 + i; // vary the price

                        string partListID = AddInventoryPart(qbSession, partName, partPrice);
                        createdPartListIDs.Add(partListID);

                        partNames.Add(partName);
                        partPrices.Add(partPrice);
                    }
                }

                // 4) Create 2 Bills:
                //    Bill #1 is from Vendor#1, uses 2 item lines referencing the 2 parts
                //    Bill #2 is from Vendor#2, uses 2 expense lines: "Utilities", "Computer and Internet Expenses"

                // We'll store the total for each bill so we can pay them in full later.
                string billTxnID1 = null!;
                string billTxnID2 = null!;
                double billTotal1 = 0.0;
                double billTotal2 = 0.0;

                using (var qbSession = new QuickBooksSession(AppConfig.QB_APP_NAME))
                {
                    // Bill #1 (Item lines, from vendor #0)
                    string vendListID1 = createdVendorListIDs[0];
                    string vendName1 = vendorNames[0];

                    // Use 2 item lines referencing the same part or different? We'll do 2 lines each referencing the same part for simplicity.
                    string partListID0 = createdPartListIDs[0];
                    double price0 = partPrices[0];

                    // line1: quantity=2, line2: quantity=3 for variety
                    double line1Qty = 2;
                    double line2Qty = 3;
                    double line1Total = price0 * line1Qty;
                    double line2Total = price0 * line2Qty;
                    billTotal1 = line1Total + line2Total;

                    billTxnID1 = AddItemBillWithTwoLines(
                        qbSession,
                        vendListID1,
                        vendName1,
                        DateTime.Today,
                        "BILL_100", // vendor invoice ref
                        memo: "100", // numeric
                        itemListID1: partListID0, price1: price0, qty1: (int)line1Qty,
                        itemListID2: partListID0, price2: price0, qty2: (int)line2Qty
                    );
                    createdBillTxnIDs.Add(billTxnID1);

                    // Bill #2 (Expense lines, from vendor #1)
                    string vendListID2 = createdVendorListIDs[1];
                    string vendName2 = vendorNames[1];

                    // We'll do 2 expense lines: "Utilities" & "Computer and Internet Expenses"
                    double utilAmt = 50.00;
                    double compAmt = 20.00;
                    billTotal2 = utilAmt + compAmt;

                    billTxnID2 = AddExpenseBillWithTwoLines(
                        qbSession,
                        vendListID2,
                        vendName2,
                        DateTime.Today,
                        "BILL_200", // vendor invoice ref
                        memo: "101", // numeric
                        expAcct1: "Utilities",
                        amt1: utilAmt,
                        expAcct2: "Computer and Internet Expenses",
                        amt2: compAmt
                    );
                    createdBillTxnIDs.Add(billTxnID2);

                    // Record the test data
                    billTestData.Add(new BillTestInfo
                    {
                        TxnID = billTxnID1,
                        VendorName = vendName1,
                        RefNumber = "BILL_100",
                        Memo = "100",
                        Total = billTotal1
                    });
                    billTestData.Add(new BillTestInfo
                    {
                        TxnID = billTxnID2,
                        VendorName = vendName2,
                        RefNumber = "BILL_200",
                        Memo = "101",
                        Total = billTotal2
                    });
                }

                // 5) Create 2 pay bills: one for Bill #1, one for Bill #2
                //    We'll use BillPaymentCheckAdd, each referencing the bill's TxnID in AppliedToTxnAdd
                //    We'll assume each is paid in full, referencing "Checking"

                using (var qbSession = new QuickBooksSession(AppConfig.QB_APP_NAME))
                {
                    // Payment #1 pays Bill #1
                    string payTxnID1 = AddBillPaymentCheck(
                        qbSession,
                        payeeVendorListID: createdVendorListIDs[0],  // vendor #0
                        bankAccount: "Checking",                     // The bank account from which we pay
                        checkDate: DateTime.Today,
                        billTxnID: billTxnID1,
                        paymentAmount: billTotal1
                    );
                    createdPaymentTxnIDs.Add(payTxnID1);

                    paymentTestData.Add(new PayBillTestInfo
                    {
                        TxnID = payTxnID1,
                        VendorName = vendorNames[0],
                        CheckDate = DateTime.Today,
                        AppliedBills = new List<AppliedBill>
                        {
                            new AppliedBill { BillTxnID = billTxnID1, Amount = billTotal1 }
                        }
                    });

                    // Payment #2 pays Bill #2
                    string payTxnID2 = AddBillPaymentCheck(
                        qbSession,
                        payeeVendorListID: createdVendorListIDs[1],
                        bankAccount: "Checking",
                        checkDate: DateTime.Today,
                        billTxnID: billTxnID2,
                        paymentAmount: billTotal2
                    );
                    createdPaymentTxnIDs.Add(payTxnID2);

                    paymentTestData.Add(new PayBillTestInfo
                    {
                        TxnID = payTxnID2,
                        VendorName = vendorNames[1],
                        CheckDate = DateTime.Today,
                        AppliedBills = new List<AppliedBill>
                        {
                            new AppliedBill { BillTxnID = billTxnID2, Amount = billTotal2 }
                        }
                    });
                }

                // 6) Query & verify pay bills
                var allPayments = PayBillReader.QueryAllPayBills();
                // This method returns a list of PayBill POCO objects from QB.

                foreach (var payInfo in paymentTestData)
                {
                    var matchingPayment = allPayments.FirstOrDefault(x => x.TxnID == payInfo.TxnID);
                    Assert.NotNull(matchingPayment);

                    // Confirm vendor name, date, total, and applied bills
                    // If your PayBill includes an "Amount" property with the total check:
                    double expectedSum = payInfo.AppliedBills.Sum(b => b.Amount);

                    Assert.Equal(payInfo.VendorName, matchingPayment.VendorName);
                    Assert.Equal(payInfo.CheckDate.Date, matchingPayment.PaymentDate.Date);
                    Assert.Equal(expectedSum, matchingPayment.CheckAmount);

                    // Check the individual bills
                    Assert.Equal(payInfo.AppliedBills.Count, matchingPayment.BillsPaid.Count);

                    // If the order is consistent or if you do a more robust match by TxnID:
                    for (int i = 0; i < payInfo.AppliedBills.Count; i++)
                    {
                        Assert.Equal(payInfo.AppliedBills[i].BillTxnID, matchingPayment.BillsPaid[i].BillTxnID);
                        Assert.Equal(payInfo.AppliedBills[i].Amount, matchingPayment.BillsPaid[i].AppliedAmount);
                    }
                }
            }
            finally
            {
                // 7) Cleanup in reverse order: pay bills first, then bills, then items, then vendors
                using (var qbSession = new QuickBooksSession(AppConfig.QB_APP_NAME))
                {
                    foreach (var payID in createdPaymentTxnIDs)
                    {
                        DeleteBillPaymentCheck(qbSession, payID);
                    }
                }

                using (var qbSession = new QuickBooksSession(AppConfig.QB_APP_NAME))
                {
                    foreach (var billID in createdBillTxnIDs)
                    {
                        DeleteBill(qbSession, billID);
                    }
                }

                using (var qbSession = new QuickBooksSession(AppConfig.QB_APP_NAME))
                {
                    foreach (var partID in createdPartListIDs)
                    {
                        DeleteListObject(qbSession, partID, ENListDelType.ldtItemInventory);
                    }
                }

                using (var qbSession = new QuickBooksSession(AppConfig.QB_APP_NAME))
                {
                    foreach (var vendID in createdVendorListIDs)
                    {
                        DeleteListObject(qbSession, vendID, ENListDelType.ldtVendor);
                    }
                }
            }
        }

        //------------------------------------------------------------------------------
        // Creating a Bill that uses item lines
        //------------------------------------------------------------------------------

        private string AddItemBillWithTwoLines(
            QuickBooksSession qbSession,
            string vendorListID,
            string vendorName,
            DateTime billDate,
            string refNumber,
            string memo,
            string itemListID1,
            double price1,
            int qty1,
            string itemListID2,
            double price2,
            int qty2
        )
        {
            IMsgSetRequest request = qbSession.CreateRequestSet();
            IBillAdd billAddRq = request.AppendBillAddRq();

            billAddRq.VendorRef.ListID.SetValue(vendorListID);
            billAddRq.TxnDate.SetValue(billDate);
            billAddRq.RefNumber.SetValue(refNumber);
            billAddRq.Memo.SetValue(memo);

            // 1st item line
            var line1 = billAddRq.ORItemLineAddList.Append().ItemLineAdd;
            line1.ItemRef.ListID.SetValue(itemListID1);
            line1.Quantity.SetValue(qty1);
            line1.Cost.SetValue(price1);
            line1.Amount.SetValue(price1 * qty1);

            // 2nd item line
            var line2 = billAddRq.ORItemLineAddList.Append().ItemLineAdd;
            line2.ItemRef.ListID.SetValue(itemListID2);
            line2.Quantity.SetValue(qty2);
            line2.Cost.SetValue(price2);
            line2.Amount.SetValue(price2 * qty2);

            var resp = qbSession.SendRequest(request);
            return ExtractBillTxnID(resp);
        }

        //------------------------------------------------------------------------------
        // Creating a Bill that uses expense lines
        //------------------------------------------------------------------------------

        private string AddExpenseBillWithTwoLines(
            QuickBooksSession qbSession,
            string vendorListID,
            string vendorName,
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

        //------------------------------------------------------------------------------
        // Bill Payment: BillPaymentCheckAdd
        //------------------------------------------------------------------------------

        private string AddBillPaymentCheck(
            QuickBooksSession qbSession,
            string payeeVendorListID,
            string bankAccount,
            DateTime checkDate,
            string billTxnID,
            double paymentAmount
        )
        {
            IMsgSetRequest request = qbSession.CreateRequestSet();
            IBillPaymentCheckAdd payAddRq = request.AppendBillPaymentCheckAddRq();

            // Required: the payee vendor
            payAddRq.PayeeEntityRef.ListID.SetValue(payeeVendorListID);

            // Payment date
            payAddRq.TxnDate.SetValue(checkDate);

            // BankAccountRef, e.g. "Checking"
            payAddRq.BankAccountRef.FullName.SetValue(bankAccount);

            // We won't set a check number, so let's mark it "To Be Printed"
            payAddRq.ORCheckPrint.IsToBePrinted.SetValue(true);

            // AppliedToTxnAdd for the single bill
            var appliedTxn = payAddRq.AppliedToTxnAddList.Append();
            appliedTxn.TxnID.SetValue(billTxnID);
            appliedTxn.PaymentAmount.SetValue(paymentAmount);

            var resp = qbSession.SendRequest(request);
            return ExtractPaymentCheckTxnID(resp);
        }

        //------------------------------------------------------------------------------
        // Delete Bill Payment Check
        //------------------------------------------------------------------------------

        private void DeleteBillPaymentCheck(QuickBooksSession qbSession, string txnID)
        {
            IMsgSetRequest request = qbSession.CreateRequestSet();
            var delRq = request.AppendTxnDelRq();
            delRq.TxnDelType.SetValue(ENTxnDelType.tdtBillPaymentCheck);
            delRq.TxnID.SetValue(txnID);

            var resp = qbSession.SendRequest(request);
            CheckForError(resp, $"Deleting BillPaymentCheck TxnID: {txnID}");
        }

        //------------------------------------------------------------------------------
        // Delete Bill
        //------------------------------------------------------------------------------

        private void DeleteBill(QuickBooksSession qbSession, string txnID)
        {
            IMsgSetRequest request = qbSession.CreateRequestSet();
            var delRq = request.AppendTxnDelRq();
            delRq.TxnDelType.SetValue(ENTxnDelType.tdtBill);
            delRq.TxnID.SetValue(txnID);

            var resp = qbSession.SendRequest(request);
            CheckForError(resp, $"Deleting Bill TxnID: {txnID}");
        }

        //------------------------------------------------------------------------------
        // Delete an Inventory Part or Vendor
        //------------------------------------------------------------------------------

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
        // Add Vendor
        //------------------------------------------------------------------------------

        private string AddVendor(QuickBooksSession qbSession, string vendorName)
        {
            IMsgSetRequest request = qbSession.CreateRequestSet();
            IVendorAdd vendAdd = request.AppendVendorAddRq();
            vendAdd.Name.SetValue(vendorName);

            var resp = qbSession.SendRequest(request);
            return ExtractVendorListID(resp);
        }

        //------------------------------------------------------------------------------
        // Add Inventory Part
        //------------------------------------------------------------------------------

        private string AddInventoryPart(QuickBooksSession qbSession, string partName, double price)
        {
            IMsgSetRequest request = qbSession.CreateRequestSet();
            IItemInventoryAdd itemAdd = request.AppendItemInventoryAddRq();

            itemAdd.Name.SetValue(partName);
            itemAdd.IncomeAccountRef.FullName.SetValue("Sales");
            itemAdd.AssetAccountRef.FullName.SetValue("Inventory Asset");
            itemAdd.COGSAccountRef.FullName.SetValue("Cost of Goods Sold");

            itemAdd.SalesPrice.SetValue(price);

            var resp = qbSession.SendRequest(request);
            return ExtractItemListID(resp);
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

        private string ExtractItemListID(IMsgSetResponse resp)
        {
            var respList = resp.ResponseList;
            if (respList == null || respList.Count == 0)
                throw new Exception("No response from ItemInventoryAdd.");

            IResponse firstResp = respList.GetAt(0);
            if (firstResp.StatusCode != 0)
                throw new Exception($"ItemInventoryAdd failed: {firstResp.StatusMessage}");

            var itemRet = firstResp.Detail as IItemInventoryRet;
            if (itemRet == null)
                throw new Exception("No IItemInventoryRet returned.");

            return itemRet.ListID.GetValue();
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

        private string ExtractPaymentCheckTxnID(IMsgSetResponse resp)
        {
            var respList = resp.ResponseList;
            if (respList == null || respList.Count == 0)
                throw new Exception("No response from BillPaymentCheckAdd.");

            IResponse firstResp = respList.GetAt(0);
            if (firstResp.StatusCode != 0)
                throw new Exception($"BillPaymentCheckAdd failed: {firstResp.StatusMessage}");

            var payRet = firstResp.Detail as IBillPaymentCheckRet;
            if (payRet == null)
                throw new Exception("No IBillPaymentCheckRet returned.");

            return payRet.TxnID.GetValue();
        }

        //------------------------------------------------------------------------------
        // ERROR HANDLER
        //------------------------------------------------------------------------------

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

        //------------------------------------------------------------------------------
        // POCO classes
        //------------------------------------------------------------------------------

        private class BillTestInfo
        {
            public string TxnID { get; set; } = "";
            public string VendorName { get; set; } = "";
            public string RefNumber { get; set; } = "";
            public string Memo { get; set; } = "";
            public double Total { get; set; }
        }

        private class PayBillTestInfo
        {
            public string TxnID { get; set; } = "";
            public string VendorName { get; set; } = "";
            public DateTime CheckDate { get; set; }
            public List<AppliedBill> AppliedBills { get; set; } = new();
        }

        private class AppliedBill
        {
            public string BillTxnID { get; set; } = "";
            public double Amount { get; set; }
        }
    }
}
