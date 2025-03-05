using System.Data;
using QBFC16Lib;

namespace PayBills_Lib
{
    public class Bill_Query
    {
        public void DoBillQuery()
        {
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

                BuildBillQueryRq(requestMsgSet);

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

                WalkBillQueryRs(responseMsgSet);
            }
            catch (Exception e)
            {
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
        void BuildBillQueryRq(IMsgSetRequest requestMsgSet)
        {
            IBillQuery BillQueryRq = requestMsgSet.AppendBillQueryRq();
            //Set attributes
            //Set field value for metaData
            BillQueryRq.metaData.SetValue("IQBENmetaDataType");
            //Set field value for iterator
            BillQueryRq.iterator.SetValue("IQBENiteratorType");
            //Set field value for iteratorID
            BillQueryRq.iteratorID.SetValue("IQBUUIDType");
            string ORBillQueryElementType1739 = "TxnIDList";
            if (ORBillQueryElementType1739 == "TxnIDList")
            {
                //Set field value for TxnIDList
                //May create more than one of these if needed
                BillQueryRq.ORBillQuery.TxnIDList.Add("200000-1011023419");
            }
            if (ORBillQueryElementType1739 == "RefNumberList")
            {
                //Set field value for RefNumberList
                //May create more than one of these if needed
                BillQueryRq.ORBillQuery.RefNumberList.Add("ab");
            }
            if (ORBillQueryElementType1739 == "RefNumberCaseSensitiveList")
            {
                //Set field value for RefNumberCaseSensitiveList
                //May create more than one of these if needed
                BillQueryRq.ORBillQuery.RefNumberCaseSensitiveList.Add("ab");
            }
            if (ORBillQueryElementType1739 == "BillFilter")
            {
                //Set field value for MaxReturned
                BillQueryRq.ORBillQuery.BillFilter.MaxReturned.SetValue(6);
                string ORDateRangeFilterElementType1740 = "ModifiedDateRangeFilter";
                if (ORDateRangeFilterElementType1740 == "ModifiedDateRangeFilter")
                {
                    //Set field value for FromModifiedDate
                    BillQueryRq.ORBillQuery.BillFilter.ORDateRangeFilter.ModifiedDateRangeFilter.FromModifiedDate.SetValue(DateTime.Parse("12/15/2007 12:15:12"), false);
                    //Set field value for ToModifiedDate
                    BillQueryRq.ORBillQuery.BillFilter.ORDateRangeFilter.ModifiedDateRangeFilter.ToModifiedDate.SetValue(DateTime.Parse("12/15/2007 12:15:12"), false);
                }
                if (ORDateRangeFilterElementType1740 == "TxnDateRangeFilter")
                {
                    string ORTxnDateRangeFilterElementType1741 = "TxnDateFilter";
                    if (ORTxnDateRangeFilterElementType1741 == "TxnDateFilter")
                    {
                        //Set field value for FromTxnDate
                        BillQueryRq.ORBillQuery.BillFilter.ORDateRangeFilter.TxnDateRangeFilter.ORTxnDateRangeFilter.TxnDateFilter.FromTxnDate.SetValue(DateTime.Parse("12/15/2007"));
                        //Set field value for ToTxnDate
                        BillQueryRq.ORBillQuery.BillFilter.ORDateRangeFilter.TxnDateRangeFilter.ORTxnDateRangeFilter.TxnDateFilter.ToTxnDate.SetValue(DateTime.Parse("12/15/2007"));
                    }
                    if (ORTxnDateRangeFilterElementType1741 == "DateMacro")
                    {
                        //Set field value for DateMacro
                        BillQueryRq.ORBillQuery.BillFilter.ORDateRangeFilter.TxnDateRangeFilter.ORTxnDateRangeFilter.DateMacro.SetValue(ENDateMacro.dmAll);
                    }
                }
                string OREntityFilterElementType1742 = "ListIDList";
                if (OREntityFilterElementType1742 == "ListIDList")
                {
                    //Set field value for ListIDList
                    //May create more than one of these if needed
                    BillQueryRq.ORBillQuery.BillFilter.EntityFilter.OREntityFilter.ListIDList.Add("200000-1011023419");
                }
                if (OREntityFilterElementType1742 == "FullNameList")
                {
                    //Set field value for FullNameList
                    //May create more than one of these if needed
                    BillQueryRq.ORBillQuery.BillFilter.EntityFilter.OREntityFilter.FullNameList.Add("ab");
                }
                if (OREntityFilterElementType1742 == "ListIDWithChildren")
                {
                    //Set field value for ListIDWithChildren
                    BillQueryRq.ORBillQuery.BillFilter.EntityFilter.OREntityFilter.ListIDWithChildren.SetValue("200000-1011023419");
                }
                if (OREntityFilterElementType1742 == "FullNameWithChildren")
                {
                    //Set field value for FullNameWithChildren
                    BillQueryRq.ORBillQuery.BillFilter.EntityFilter.OREntityFilter.FullNameWithChildren.SetValue("ab");
                }
                string ORAccountFilterElementType1743 = "ListIDList";
                if (ORAccountFilterElementType1743 == "ListIDList")
                {
                    //Set field value for ListIDList
                    //May create more than one of these if needed
                    BillQueryRq.ORBillQuery.BillFilter.AccountFilter.ORAccountFilter.ListIDList.Add("200000-1011023419");
                }
                if (ORAccountFilterElementType1743 == "FullNameList")
                {
                    //Set field value for FullNameList
                    //May create more than one of these if needed
                    BillQueryRq.ORBillQuery.BillFilter.AccountFilter.ORAccountFilter.FullNameList.Add("ab");
                }
                if (ORAccountFilterElementType1743 == "ListIDWithChildren")
                {
                    //Set field value for ListIDWithChildren
                    BillQueryRq.ORBillQuery.BillFilter.AccountFilter.ORAccountFilter.ListIDWithChildren.SetValue("200000-1011023419");
                }
                if (ORAccountFilterElementType1743 == "FullNameWithChildren")
                {
                    //Set field value for FullNameWithChildren
                    BillQueryRq.ORBillQuery.BillFilter.AccountFilter.ORAccountFilter.FullNameWithChildren.SetValue("ab");
                }
                string ORRefNumberFilterElementType1744 = "RefNumberFilter";
                if (ORRefNumberFilterElementType1744 == "RefNumberFilter")
                {
                    //Set field value for MatchCriterion
                    BillQueryRq.ORBillQuery.BillFilter.ORRefNumberFilter.RefNumberFilter.MatchCriterion.SetValue(ENMatchCriterion.mcStartsWith);
                    //Set field value for RefNumber
                    BillQueryRq.ORBillQuery.BillFilter.ORRefNumberFilter.RefNumberFilter.RefNumber.SetValue("ab");
                }
                if (ORRefNumberFilterElementType1744 == "RefNumberRangeFilter")
                {
                    //Set field value for FromRefNumber
                    BillQueryRq.ORBillQuery.BillFilter.ORRefNumberFilter.RefNumberRangeFilter.FromRefNumber.SetValue("ab");
                    //Set field value for ToRefNumber
                    BillQueryRq.ORBillQuery.BillFilter.ORRefNumberFilter.RefNumberRangeFilter.ToRefNumber.SetValue("ab");
                }
                string ORCurrencyFilterElementType1745 = "ListIDList";
                if (ORCurrencyFilterElementType1745 == "ListIDList")
                {
                    //Set field value for ListIDList
                    //May create more than one of these if needed
                    BillQueryRq.ORBillQuery.BillFilter.CurrencyFilter.ORCurrencyFilter.ListIDList.Add("200000-1011023419");
                }
                if (ORCurrencyFilterElementType1745 == "FullNameList")
                {
                    //Set field value for FullNameList
                    //May create more than one of these if needed
                    BillQueryRq.ORBillQuery.BillFilter.CurrencyFilter.ORCurrencyFilter.FullNameList.Add("ab");
                }
                //Set field value for PaidStatus
                BillQueryRq.ORBillQuery.BillFilter.PaidStatus.SetValue(ENPaidStatus.psAll[DEFAULT]);
            }
            //Set field value for IncludeLineItems
            BillQueryRq.IncludeLineItems.SetValue(true);
            //Set field value for IncludeLinkedTxns
            BillQueryRq.IncludeLinkedTxns.SetValue(true);
            //Set field value for IncludeRetElementList
            //May create more than one of these if needed
            BillQueryRq.IncludeRetElementList.Add("ab");
            //Set field value for OwnerIDList
            //May create more than one of these if needed
            BillQueryRq.OwnerIDList.Add(Guid.NewGuid().ToString());
        }




        void WalkBillQueryRs(IMsgSetResponse responseMsgSet)
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
                        if (responseType == ENResponseType.rtBillQueryRs)
                        {
                            //upcast to more specific type here, this is safe because we checked with response.Type check above
                            IBillRetList BillRet = (IBillRetList)response.Detail;
                            WalkBillRet(BillRet);
                        }
                    }
                }
            }
        }




        void WalkBillRet(IBillRetList BillRet)
        {
            if (BillRet == null) return;
            //Go through all the elements of IBillRetList
            //Get value of TxnID
            string TxnID1746 = (string)BillRet.TxnID.GetValue();
            //Get value of TimeCreated
            DateTime TimeCreated1747 = (DateTime)BillRet.TimeCreated.GetValue();
            //Get value of TimeModified
            DateTime TimeModified1748 = (DateTime)BillRet.TimeModified.GetValue();
            //Get value of EditSequence
            string EditSequence1749 = (string)BillRet.EditSequence.GetValue();
            //Get value of TxnNumber
            if (BillRet.TxnNumber != null)
            {
                int TxnNumber1750 = (int)BillRet.TxnNumber.GetValue();
            }
            //Get value of ListID
            if (BillRet.VendorRef.ListID != null)
            {
                string ListID1751 = (string)BillRet.VendorRef.ListID.GetValue();
            }
            //Get value of FullName
            if (BillRet.VendorRef.FullName != null)
            {
                string FullName1752 = (string)BillRet.VendorRef.FullName.GetValue();
            }
            if (BillRet.VendorAddress != null)
            {
                //Get value of Addr1
                if (BillRet.VendorAddress.Addr1 != null)
                {
                    string Addr11753 = (string)BillRet.VendorAddress.Addr1.GetValue();
                }
                //Get value of Addr2
                if (BillRet.VendorAddress.Addr2 != null)
                {
                    string Addr21754 = (string)BillRet.VendorAddress.Addr2.GetValue();
                }
                //Get value of Addr3
                if (BillRet.VendorAddress.Addr3 != null)
                {
                    string Addr31755 = (string)BillRet.VendorAddress.Addr3.GetValue();
                }
                //Get value of Addr4
                if (BillRet.VendorAddress.Addr4 != null)
                {
                    string Addr41756 = (string)BillRet.VendorAddress.Addr4.GetValue();
                }
                //Get value of Addr5
                if (BillRet.VendorAddress.Addr5 != null)
                {
                    string Addr51757 = (string)BillRet.VendorAddress.Addr5.GetValue();
                }
                //Get value of City
                if (BillRet.VendorAddress.City != null)
                {
                    string City1758 = (string)BillRet.VendorAddress.City.GetValue();
                }
                //Get value of State
                if (BillRet.VendorAddress.State != null)
                {
                    string State1759 = (string)BillRet.VendorAddress.State.GetValue();
                }
                //Get value of PostalCode
                if (BillRet.VendorAddress.PostalCode != null)
                {
                    string PostalCode1760 = (string)BillRet.VendorAddress.PostalCode.GetValue();
                }
                //Get value of Country
                if (BillRet.VendorAddress.Country != null)
                {
                    string Country1761 = (string)BillRet.VendorAddress.Country.GetValue();
                }
                //Get value of Note
                if (BillRet.VendorAddress.Note != null)
                {
                    string Note1762 = (string)BillRet.VendorAddress.Note.GetValue();
                }
            }
            if (BillRet.APAccountRef != null)
            {
                //Get value of ListID
                if (BillRet.APAccountRef.ListID != null)
                {
                    string ListID1763 = (string)BillRet.APAccountRef.ListID.GetValue();
                }
                //Get value of FullName
                if (BillRet.APAccountRef.FullName != null)
                {
                    string FullName1764 = (string)BillRet.APAccountRef.FullName.GetValue();
                }
            }
            //Get value of TxnDate
            DateTime TxnDate1765 = (DateTime)BillRet.TxnDate.GetValue();
            //Get value of DueDate
            if (BillRet.DueDate != null)
            {
                DateTime DueDate1766 = (DateTime)BillRet.DueDate.GetValue();
            }
            //Get value of AmountDue
            double AmountDue1767 = (double)BillRet.AmountDue.GetValue();
            if (BillRet.CurrencyRef != null)
            {
                //Get value of ListID
                if (BillRet.CurrencyRef.ListID != null)
                {
                    string ListID1768 = (string)BillRet.CurrencyRef.ListID.GetValue();
                }
                //Get value of FullName
                if (BillRet.CurrencyRef.FullName != null)
                {
                    string FullName1769 = (string)BillRet.CurrencyRef.FullName.GetValue();
                }
            }
            //Get value of ExchangeRate
            if (BillRet.ExchangeRate != null)
            {
                IQBFloatType ExchangeRate1770 = (IQBFloatType)BillRet.ExchangeRate.GetValue();
            }
            //Get value of AmountDueInHomeCurrency
            if (BillRet.AmountDueInHomeCurrency != null)
            {
                double AmountDueInHomeCurrency1771 = (double)BillRet.AmountDueInHomeCurrency.GetValue();
            }
            //Get value of RefNumber
            if (BillRet.RefNumber != null)
            {
                string RefNumber1772 = (string)BillRet.RefNumber.GetValue();
            }
            //Get value of IsPending
            if (BillRet.IsPending != null)
            {
                bool isPending = (string)BillRet.IsPending.GetValue();
            }
            if (BillRet.TermsRef != null)
            {
                //Get value of ListID
                if (BillRet.TermsRef.ListID != null)
                {
                    string ListID1773 = (string)BillRet.TermsRef.ListID.GetValue();
                }
                //Get value of FullName
                if (BillRet.TermsRef.FullName != null)
                {
                    string FullName1774 = (string)BillRet.TermsRef.FullName.GetValue();
                }
            }
            //Get value of Memo
            if (BillRet.Memo != null)
            {
                string Memo1775 = (string)BillRet.Memo.GetValue();
            }
            //Get value of IsTaxIncluded
            if (BillRet.IsTaxIncluded != null)
            {
                bool IsTaxIncluded1776 = (bool)BillRet.IsTaxIncluded.GetValue();
            }
            if (BillRet.SalesTaxCodeRef != null)
            {
                //Get value of ListID
                if (BillRet.SalesTaxCodeRef.ListID != null)
                {
                    string ListID1777 = (string)BillRet.SalesTaxCodeRef.ListID.GetValue();
                }
                //Get value of FullName
                if (BillRet.SalesTaxCodeRef.FullName != null)
                {
                    string FullName1778 = (string)BillRet.SalesTaxCodeRef.FullName.GetValue();
                }
            }
            //Get value of IsPaid
            if (BillRet.IsPaid != null)
            {
                bool IsPaid1779 = (bool)BillRet.IsPaid.GetValue();
            }
            //Get value of ExternalGUID
            if (BillRet.ExternalGUID != null)
            {
                string ExternalGUID1780 = (string)BillRet.ExternalGUID.GetValue();
            }
            if (BillRet.LinkedTxnList != null)
            {
                for (int i1781 = 0; i1781 < BillRet.LinkedTxnList.Count; i1781++)
                {
                    ILinkedTxn LinkedTxn = BillRet.LinkedTxnList.GetAt(i1781);
                    //Get value of TxnID
                    string TxnID1782 = (string)LinkedTxn.TxnID.GetValue();
                    //Get value of TxnType
                    ENTxnType TxnType1783 = (ENTxnType)LinkedTxn.TxnType.GetValue();
                    //Get value of TxnDate
                    DateTime TxnDate1784 = (DateTime)LinkedTxn.TxnDate.GetValue();
                    //Get value of RefNumber
                    if (LinkedTxn.RefNumber != null)
                    {
                        string RefNumber1785 = (string)LinkedTxn.RefNumber.GetValue();
                    }
                    //Get value of LinkType
                    if (LinkedTxn.LinkType != null)
                    {
                        ENLinkType LinkType1786 = (ENLinkType)LinkedTxn.LinkType.GetValue();
                    }
                    //Get value of Amount
                    double Amount1787 = (double)LinkedTxn.Amount.GetValue();
                }
            }
            if (BillRet.ExpenseLineRetList != null)
            {
                for (int i1788 = 0; i1788 < BillRet.ExpenseLineRetList.Count; i1788++)
                {
                    IExpenseLineRet ExpenseLineRet = BillRet.ExpenseLineRetList.GetAt(i1788);
                    //Get value of TxnLineID
                    string TxnLineID1789 = (string)ExpenseLineRet.TxnLineID.GetValue();
                    if (ExpenseLineRet.AccountRef != null)
                    {
                        //Get value of ListID
                        if (ExpenseLineRet.AccountRef.ListID != null)
                        {
                            string ListID1790 = (string)ExpenseLineRet.AccountRef.ListID.GetValue();
                        }
                        //Get value of FullName
                        if (ExpenseLineRet.AccountRef.FullName != null)
                        {
                            string FullName1791 = (string)ExpenseLineRet.AccountRef.FullName.GetValue();
                        }
                    }
                    //Get value of Amount
                    if (ExpenseLineRet.Amount != null)
                    {
                        double Amount1792 = (double)ExpenseLineRet.Amount.GetValue();
                    }
                    //Get value of Memo
                    if (ExpenseLineRet.Memo != null)
                    {
                        string Memo1793 = (string)ExpenseLineRet.Memo.GetValue();
                    }
                    if (ExpenseLineRet.CustomerRef != null)
                    {
                        //Get value of ListID
                        if (ExpenseLineRet.CustomerRef.ListID != null)
                        {
                            string ListID1794 = (string)ExpenseLineRet.CustomerRef.ListID.GetValue();
                        }
                        //Get value of FullName
                        if (ExpenseLineRet.CustomerRef.FullName != null)
                        {
                            string FullName1795 = (string)ExpenseLineRet.CustomerRef.FullName.GetValue();
                        }
                    }
                    if (ExpenseLineRet.ClassRef != null)
                    {
                        //Get value of ListID
                        if (ExpenseLineRet.ClassRef.ListID != null)
                        {
                            string ListID1796 = (string)ExpenseLineRet.ClassRef.ListID.GetValue();
                        }
                        //Get value of FullName
                        if (ExpenseLineRet.ClassRef.FullName != null)
                        {
                            string FullName1797 = (string)ExpenseLineRet.ClassRef.FullName.GetValue();
                        }
                    }
                    if (ExpenseLineRet.SalesTaxCodeRef != null)
                    {
                        //Get value of ListID
                        if (ExpenseLineRet.SalesTaxCodeRef.ListID != null)
                        {
                            string ListID1798 = (string)ExpenseLineRet.SalesTaxCodeRef.ListID.GetValue();
                        }
                        //Get value of FullName
                        if (ExpenseLineRet.SalesTaxCodeRef.FullName != null)
                        {
                            string FullName1799 = (string)ExpenseLineRet.SalesTaxCodeRef.FullName.GetValue();
                        }
                    }
                    //Get value of BillableStatus
                    if (ExpenseLineRet.BillableStatus != null)
                    {
                        ENBillableStatus BillableStatus1800 = (ENBillableStatus)ExpenseLineRet.BillableStatus.GetValue();
                    }
                    if (ExpenseLineRet.SalesRepRef != null)
                    {
                        //Get value of ListID
                        if (ExpenseLineRet.SalesRepRef.ListID != null)
                        {
                            string ListID1801 = (string)ExpenseLineRet.SalesRepRef.ListID.GetValue();
                        }
                        //Get value of FullName
                        if (ExpenseLineRet.SalesRepRef.FullName != null)
                        {
                            string FullName1802 = (string)ExpenseLineRet.SalesRepRef.FullName.GetValue();
                        }
                    }
                    if (ExpenseLineRet.DataExtRetList != null)
                    {
                        for (int i1803 = 0; i1803 < ExpenseLineRet.DataExtRetList.Count; i1803++)
                        {
                            IDataExtRet DataExtRet = ExpenseLineRet.DataExtRetList.GetAt(i1803);
                            //Get value of OwnerID
                            if (DataExtRet.OwnerID != null)
                            {
                                string OwnerID1804 = (string)DataExtRet.OwnerID.GetValue();
                            }
                            //Get value of DataExtName
                            string DataExtName1805 = (string)DataExtRet.DataExtName.GetValue();
                            //Get value of DataExtType
                            ENDataExtType DataExtType1806 = (ENDataExtType)DataExtRet.DataExtType.GetValue();
                            //Get value of DataExtValue
                            string DataExtValue1807 = (string)DataExtRet.DataExtValue.GetValue();
                        }
                    }
                }
            }
            if (BillRet.ORItemLineRetList != null)
            {
                for (int i1808 = 0; i1808 < BillRet.ORItemLineRetList.Count; i1808++)
                {
                    IORItemLineRet ORItemLineRet1809 = BillRet.ORItemLineRetList.GetAt(i1808);
                    if (ORItemLineRet1809.ItemLineRet != null)
                    {
                        if (ORItemLineRet1809.ItemLineRet != null)
                        {
                            //Get value of TxnLineID
                            string TxnLineID1810 = (string)ORItemLineRet1809.ItemLineRet.TxnLineID.GetValue();
                            if (ORItemLineRet1809.ItemLineRet.ItemRef != null)
                            {
                                //Get value of ListID
                                if (ORItemLineRet1809.ItemLineRet.ItemRef.ListID != null)
                                {
                                    string ListID1811 = (string)ORItemLineRet1809.ItemLineRet.ItemRef.ListID.GetValue();
                                }
                                //Get value of FullName
                                if (ORItemLineRet1809.ItemLineRet.ItemRef.FullName != null)
                                {
                                    string FullName1812 = (string)ORItemLineRet1809.ItemLineRet.ItemRef.FullName.GetValue();
                                }
                            }
                            if (ORItemLineRet1809.ItemLineRet.InventorySiteRef != null)
                            {
                                //Get value of ListID
                                if (ORItemLineRet1809.ItemLineRet.InventorySiteRef.ListID != null)
                                {
                                    string ListID1813 = (string)ORItemLineRet1809.ItemLineRet.InventorySiteRef.ListID.GetValue();
                                }
                                //Get value of FullName
                                if (ORItemLineRet1809.ItemLineRet.InventorySiteRef.FullName != null)
                                {
                                    string FullName1814 = (string)ORItemLineRet1809.ItemLineRet.InventorySiteRef.FullName.GetValue();
                                }
                            }
                            if (ORItemLineRet1809.ItemLineRet.InventorySiteLocationRef != null)
                            {
                                //Get value of ListID
                                if (ORItemLineRet1809.ItemLineRet.InventorySiteLocationRef.ListID != null)
                                {
                                    string ListID1815 = (string)ORItemLineRet1809.ItemLineRet.InventorySiteLocationRef.ListID.GetValue();
                                }
                                //Get value of FullName
                                if (ORItemLineRet1809.ItemLineRet.InventorySiteLocationRef.FullName != null)
                                {
                                    string FullName1816 = (string)ORItemLineRet1809.ItemLineRet.InventorySiteLocationRef.FullName.GetValue();
                                }
                            }
                            if (ORItemLineRet1809.ItemLineRet.ORSerialLotNumber != null)
                            {
                                if (ORItemLineRet1809.ItemLineRet.ORSerialLotNumber.SerialNumber != null)
                                {
                                    //Get value of SerialNumber
                                    if (ORItemLineRet1809.ItemLineRet.ORSerialLotNumber.SerialNumber != null)
                                    {
                                        string SerialNumber1818 = (string)ORItemLineRet1809.ItemLineRet.ORSerialLotNumber.SerialNumber.GetValue();
                                    }
                                }
                                if (ORItemLineRet1809.ItemLineRet.ORSerialLotNumber.LotNumber != null)
                                {
                                    //Get value of LotNumber
                                    if (ORItemLineRet1809.ItemLineRet.ORSerialLotNumber.LotNumber != null)
                                    {
                                        string LotNumber1819 = (string)ORItemLineRet1809.ItemLineRet.ORSerialLotNumber.LotNumber.GetValue();
                                    }
                                }
                            }
                            //Get value of Desc
                            if (ORItemLineRet1809.ItemLineRet.ExpirationDateForSerialLotNumber != null)
                            {
                                string ExpDate = (string)ORItemLineRet1809.ItemLineRet.ExpirationDateForSerialLotNumber.GetValue();
                            }
                            //Get value of Desc
                            if (ORItemLineRet1809.ItemLineRet.Desc != null)
                            {
                                string Desc1820 = (string)ORItemLineRet1809.ItemLineRet.Desc.GetValue();
                            }
                            //Get value of Quantity
                            if (ORItemLineRet1809.ItemLineRet.Quantity != null)
                            {
                                int Quantity1821 = (int)ORItemLineRet1809.ItemLineRet.Quantity.GetValue();
                            }
                            //Get value of UnitOfMeasure
                            if (ORItemLineRet1809.ItemLineRet.UnitOfMeasure != null)
                            {
                                string UnitOfMeasure1822 = (string)ORItemLineRet1809.ItemLineRet.UnitOfMeasure.GetValue();
                            }
                            if (ORItemLineRet1809.ItemLineRet.OverrideUOMSetRef != null)
                            {
                                //Get value of ListID
                                if (ORItemLineRet1809.ItemLineRet.OverrideUOMSetRef.ListID != null)
                                {
                                    string ListID1823 = (string)ORItemLineRet1809.ItemLineRet.OverrideUOMSetRef.ListID.GetValue();
                                }
                                //Get value of FullName
                                if (ORItemLineRet1809.ItemLineRet.OverrideUOMSetRef.FullName != null)
                                {
                                    string FullName1824 = (string)ORItemLineRet1809.ItemLineRet.OverrideUOMSetRef.FullName.GetValue();
                                }
                            }
                            //Get value of Cost
                            if (ORItemLineRet1809.ItemLineRet.Cost != null)
                            {
                                double Cost1825 = (double)ORItemLineRet1809.ItemLineRet.Cost.GetValue();
                            }
                            //Get value of Amount
                            if (ORItemLineRet1809.ItemLineRet.Amount != null)
                            {
                                double Amount1826 = (double)ORItemLineRet1809.ItemLineRet.Amount.GetValue();
                            }
                            if (ORItemLineRet1809.ItemLineRet.CustomerRef != null)
                            {
                                //Get value of ListID
                                if (ORItemLineRet1809.ItemLineRet.CustomerRef.ListID != null)
                                {
                                    string ListID1827 = (string)ORItemLineRet1809.ItemLineRet.CustomerRef.ListID.GetValue();
                                }
                                //Get value of FullName
                                if (ORItemLineRet1809.ItemLineRet.CustomerRef.FullName != null)
                                {
                                    string FullName1828 = (string)ORItemLineRet1809.ItemLineRet.CustomerRef.FullName.GetValue();
                                }
                            }
                            if (ORItemLineRet1809.ItemLineRet.ClassRef != null)
                            {
                                //Get value of ListID
                                if (ORItemLineRet1809.ItemLineRet.ClassRef.ListID != null)
                                {
                                    string ListID1829 = (string)ORItemLineRet1809.ItemLineRet.ClassRef.ListID.GetValue();
                                }
                                //Get value of FullName
                                if (ORItemLineRet1809.ItemLineRet.ClassRef.FullName != null)
                                {
                                    string FullName1830 = (string)ORItemLineRet1809.ItemLineRet.ClassRef.FullName.GetValue();
                                }
                            }
                            if (ORItemLineRet1809.ItemLineRet.SalesTaxCodeRef != null)
                            {
                                //Get value of ListID
                                if (ORItemLineRet1809.ItemLineRet.SalesTaxCodeRef.ListID != null)
                                {
                                    string ListID1831 = (string)ORItemLineRet1809.ItemLineRet.SalesTaxCodeRef.ListID.GetValue();
                                }
                                //Get value of FullName
                                if (ORItemLineRet1809.ItemLineRet.SalesTaxCodeRef.FullName != null)
                                {
                                    string FullName1832 = (string)ORItemLineRet1809.ItemLineRet.SalesTaxCodeRef.FullName.GetValue();
                                }
                            }
                            //Get value of BillableStatus
                            if (ORItemLineRet1809.ItemLineRet.BillableStatus != null)
                            {
                                ENBillableStatus BillableStatus1833 = (ENBillableStatus)ORItemLineRet1809.ItemLineRet.BillableStatus.GetValue();
                            }
                            if (ORItemLineRet1809.ItemLineRet.SalesRepRef != null)
                            {
                                //Get value of ListID
                                if (ORItemLineRet1809.ItemLineRet.SalesRepRef.ListID != null)
                                {
                                    string ListID1834 = (string)ORItemLineRet1809.ItemLineRet.SalesRepRef.ListID.GetValue();
                                }
                                //Get value of FullName
                                if (ORItemLineRet1809.ItemLineRet.SalesRepRef.FullName != null)
                                {
                                    string FullName1835 = (string)ORItemLineRet1809.ItemLineRet.SalesRepRef.FullName.GetValue();
                                }
                            }
                            if (ORItemLineRet1809.ItemLineRet.DataExtRetList != null)
                            {
                                for (int i1836 = 0; i1836 < ORItemLineRet1809.ItemLineRet.DataExtRetList.Count; i1836++)
                                {
                                    IDataExtRet DataExtRet = ORItemLineRet1809.ItemLineRet.DataExtRetList.GetAt(i1836);
                                    //Get value of OwnerID
                                    if (DataExtRet.OwnerID != null)
                                    {
                                        string OwnerID1837 = (string)DataExtRet.OwnerID.GetValue();
                                    }
                                    //Get value of DataExtName
                                    string DataExtName1838 = (string)DataExtRet.DataExtName.GetValue();
                                    //Get value of DataExtType
                                    ENDataExtType DataExtType1839 = (ENDataExtType)DataExtRet.DataExtType.GetValue();
                                    //Get value of DataExtValue
                                    string DataExtValue1840 = (string)DataExtRet.DataExtValue.GetValue();
                                }
                            }
                        }
                    }
                    if (ORItemLineRet1809.ItemGroupLineRet != null)
                    {
                        if (ORItemLineRet1809.ItemGroupLineRet != null)
                        {
                            //Get value of TxnLineID
                            string TxnLineID1841 = (string)ORItemLineRet1809.ItemGroupLineRet.TxnLineID.GetValue();
                            //Get value of ListID
                            if (ORItemLineRet1809.ItemGroupLineRet.ItemGroupRef.ListID != null)
                            {
                                string ListID1842 = (string)ORItemLineRet1809.ItemGroupLineRet.ItemGroupRef.ListID.GetValue();
                            }
                            //Get value of FullName
                            if (ORItemLineRet1809.ItemGroupLineRet.ItemGroupRef.FullName != null)
                            {
                                string FullName1843 = (string)ORItemLineRet1809.ItemGroupLineRet.ItemGroupRef.FullName.GetValue();
                            }
                            //Get value of Desc
                            if (ORItemLineRet1809.ItemGroupLineRet.Desc != null)
                            {
                                string Desc1844 = (string)ORItemLineRet1809.ItemGroupLineRet.Desc.GetValue();
                            }
                            //Get value of Quantity
                            if (ORItemLineRet1809.ItemGroupLineRet.Quantity != null)
                            {
                                int Quantity1845 = (int)ORItemLineRet1809.ItemGroupLineRet.Quantity.GetValue();
                            }
                            //Get value of UnitOfMeasure
                            if (ORItemLineRet1809.ItemGroupLineRet.UnitOfMeasure != null)
                            {
                                string UnitOfMeasure1846 = (string)ORItemLineRet1809.ItemGroupLineRet.UnitOfMeasure.GetValue();
                            }
                            if (ORItemLineRet1809.ItemGroupLineRet.OverrideUOMSetRef != null)
                            {
                                //Get value of ListID
                                if (ORItemLineRet1809.ItemGroupLineRet.OverrideUOMSetRef.ListID != null)
                                {
                                    string ListID1847 = (string)ORItemLineRet1809.ItemGroupLineRet.OverrideUOMSetRef.ListID.GetValue();
                                }
                                //Get value of FullName
                                if (ORItemLineRet1809.ItemGroupLineRet.OverrideUOMSetRef.FullName != null)
                                {
                                    string FullName1848 = (string)ORItemLineRet1809.ItemGroupLineRet.OverrideUOMSetRef.FullName.GetValue();
                                }
                            }
                            //Get value of TotalAmount
                            double TotalAmount1849 = (double)ORItemLineRet1809.ItemGroupLineRet.TotalAmount.GetValue();
                            if (ORItemLineRet1809.ItemGroupLineRet.ItemLineRetList != null)
                            {
                                for (int i1850 = 0; i1850 < ORItemLineRet1809.ItemGroupLineRet.ItemLineRetList.Count; i1850++)
                                {
                                    IItemLineRet ItemLineRet = ORItemLineRet1809.ItemGroupLineRet.ItemLineRetList.GetAt(i1850);
                                    //Get value of TxnLineID
                                    string TxnLineID1851 = (string)ItemLineRet.TxnLineID.GetValue();
                                    if (ItemLineRet.ItemRef != null)
                                    {
                                        //Get value of ListID
                                        if (ItemLineRet.ItemRef.ListID != null)
                                        {
                                            string ListID1852 = (string)ItemLineRet.ItemRef.ListID.GetValue();
                                        }
                                        //Get value of FullName
                                        if (ItemLineRet.ItemRef.FullName != null)
                                        {
                                            string FullName1853 = (string)ItemLineRet.ItemRef.FullName.GetValue();
                                        }
                                    }
                                    if (ItemLineRet.InventorySiteRef != null)
                                    {
                                        //Get value of ListID
                                        if (ItemLineRet.InventorySiteRef.ListID != null)
                                        {
                                            string ListID1854 = (string)ItemLineRet.InventorySiteRef.ListID.GetValue();
                                        }
                                        //Get value of FullName
                                        if (ItemLineRet.InventorySiteRef.FullName != null)
                                        {
                                            string FullName1855 = (string)ItemLineRet.InventorySiteRef.FullName.GetValue();
                                        }
                                    }
                                    if (ItemLineRet.InventorySiteLocationRef != null)
                                    {
                                        //Get value of ListID
                                        if (ItemLineRet.InventorySiteLocationRef.ListID != null)
                                        {
                                            string ListID1856 = (string)ItemLineRet.InventorySiteLocationRef.ListID.GetValue();
                                        }
                                        //Get value of FullName
                                        if (ItemLineRet.InventorySiteLocationRef.FullName != null)
                                        {
                                            string FullName1857 = (string)ItemLineRet.InventorySiteLocationRef.FullName.GetValue();
                                        }
                                    }
                                    if (ItemLineRet.ORSerialLotNumber != null)
                                    {
                                        if (ItemLineRet.ORSerialLotNumber.SerialNumber != null)
                                        {
                                            //Get value of SerialNumber
                                            if (ItemLineRet.ORSerialLotNumber.SerialNumber != null)
                                            {
                                                string SerialNumber1859 = (string)ItemLineRet.ORSerialLotNumber.SerialNumber.GetValue();
                                            }
                                        }
                                        if (ItemLineRet.ORSerialLotNumber.LotNumber != null)
                                        {
                                            //Get value of LotNumber
                                            if (ItemLineRet.ORSerialLotNumber.LotNumber != null)
                                            {
                                                string LotNumber1860 = (string)ItemLineRet.ORSerialLotNumber.LotNumber.GetValue();
                                            }
                                        }
                                    }
                                    //Get value of ExpirationDateForSerialLotNumber
                                    if (ItemLineRet.ExpirationDateForSerialLotNumber != null)
                                    {
                                        string ExpDate = (string)ItemLineRet.ExpirationDateForSerialLotNumber.GetValue();
                                    }
                                    //Get value of Desc
                                    if (ItemLineRet.Desc != null)
                                    {
                                        string Desc1861 = (string)ItemLineRet.Desc.GetValue();
                                    }
                                    //Get value of Quantity
                                    if (ItemLineRet.Quantity != null)
                                    {
                                        int Quantity1862 = (int)ItemLineRet.Quantity.GetValue();
                                    }
                                    //Get value of UnitOfMeasure
                                    if (ItemLineRet.UnitOfMeasure != null)
                                    {
                                        string UnitOfMeasure1863 = (string)ItemLineRet.UnitOfMeasure.GetValue();
                                    }
                                    if (ItemLineRet.OverrideUOMSetRef != null)
                                    {
                                        //Get value of ListID
                                        if (ItemLineRet.OverrideUOMSetRef.ListID != null)
                                        {
                                            string ListID1864 = (string)ItemLineRet.OverrideUOMSetRef.ListID.GetValue();
                                        }
                                        //Get value of FullName
                                        if (ItemLineRet.OverrideUOMSetRef.FullName != null)
                                        {
                                            string FullName1865 = (string)ItemLineRet.OverrideUOMSetRef.FullName.GetValue();
                                        }
                                    }
                                    //Get value of Cost
                                    if (ItemLineRet.Cost != null)
                                    {
                                        double Cost1866 = (double)ItemLineRet.Cost.GetValue();
                                    }
                                    //Get value of Amount
                                    if (ItemLineRet.Amount != null)
                                    {
                                        double Amount1867 = (double)ItemLineRet.Amount.GetValue();
                                    }
                                    if (ItemLineRet.CustomerRef != null)
                                    {
                                        //Get value of ListID
                                        if (ItemLineRet.CustomerRef.ListID != null)
                                        {
                                            string ListID1868 = (string)ItemLineRet.CustomerRef.ListID.GetValue();
                                        }
                                        //Get value of FullName
                                        if (ItemLineRet.CustomerRef.FullName != null)
                                        {
                                            string FullName1869 = (string)ItemLineRet.CustomerRef.FullName.GetValue();
                                        }
                                    }
                                    if (ItemLineRet.ClassRef != null)
                                    {
                                        //Get value of ListID
                                        if (ItemLineRet.ClassRef.ListID != null)
                                        {
                                            string ListID1870 = (string)ItemLineRet.ClassRef.ListID.GetValue();
                                        }
                                        //Get value of FullName
                                        if (ItemLineRet.ClassRef.FullName != null)
                                        {
                                            string FullName1871 = (string)ItemLineRet.ClassRef.FullName.GetValue();
                                        }
                                    }
                                    if (ItemLineRet.SalesTaxCodeRef != null)
                                    {
                                        //Get value of ListID
                                        if (ItemLineRet.SalesTaxCodeRef.ListID != null)
                                        {
                                            string ListID1872 = (string)ItemLineRet.SalesTaxCodeRef.ListID.GetValue();
                                        }
                                        //Get value of FullName
                                        if (ItemLineRet.SalesTaxCodeRef.FullName != null)
                                        {
                                            string FullName1873 = (string)ItemLineRet.SalesTaxCodeRef.FullName.GetValue();
                                        }
                                    }
                                    //Get value of BillableStatus
                                    if (ItemLineRet.BillableStatus != null)
                                    {
                                        ENBillableStatus BillableStatus1874 = (ENBillableStatus)ItemLineRet.BillableStatus.GetValue();
                                    }
                                    if (ItemLineRet.SalesRepRef != null)
                                    {
                                        //Get value of ListID
                                        if (ItemLineRet.SalesRepRef.ListID != null)
                                        {
                                            string ListID1875 = (string)ItemLineRet.SalesRepRef.ListID.GetValue();
                                        }
                                        //Get value of FullName
                                        if (ItemLineRet.SalesRepRef.FullName != null)
                                        {
                                            string FullName1876 = (string)ItemLineRet.SalesRepRef.FullName.GetValue();
                                        }
                                    }
                                    if (ItemLineRet.DataExtRetList != null)
                                    {
                                        for (int i1877 = 0; i1877 < ItemLineRet.DataExtRetList.Count; i1877++)
                                        {
                                            IDataExtRet DataExtRet = ItemLineRet.DataExtRetList.GetAt(i1877);
                                            //Get value of OwnerID
                                            if (DataExtRet.OwnerID != null)
                                            {
                                                string OwnerID1878 = (string)DataExtRet.OwnerID.GetValue();
                                            }
                                            //Get value of DataExtName
                                            string DataExtName1879 = (string)DataExtRet.DataExtName.GetValue();
                                            //Get value of DataExtType
                                            ENDataExtType DataExtType1880 = (ENDataExtType)DataExtRet.DataExtType.GetValue();
                                            //Get value of DataExtValue
                                            string DataExtValue1881 = (string)DataExtRet.DataExtValue.GetValue();
                                        }
                                    }
                                }
                            }
                            if (ORItemLineRet1809.ItemGroupLineRet.DataExtList != null)
                            {
                                for (int i1882 = 0; i1882 < ORItemLineRet1809.ItemGroupLineRet.DataExtList.Count; i1882++)
                                {
                                    IDataExt DataExt = ORItemLineRet1809.ItemGroupLineRet.DataExtList.GetAt(i1882);
                                    //Get value of OwnerID
                                    string OwnerID1883 = (string)DataExt.OwnerID.GetValue();
                                    //Get value of DataExtName
                                    string DataExtName1884 = (string)DataExt.DataExtName.GetValue();
                                    //Get value of DataExtValue
                                    string DataExtValue1885 = (string)DataExt.DataExtValue.GetValue();
                                }
                            }
                        }
                    }
                }
            }
            //Get value of OpenAmount
            if (BillRet.OpenAmount != null)
            {
                double OpenAmount1886 = (double)BillRet.OpenAmount.GetValue();
            }
            if (BillRet.DataExtRetList != null)
            {
                for (int i1887 = 0; i1887 < BillRet.DataExtRetList.Count; i1887++)
                {
                    IDataExtRet DataExtRet = BillRet.DataExtRetList.GetAt(i1887);
                    //Get value of OwnerID
                    if (DataExtRet.OwnerID != null)
                    {
                        string OwnerID1888 = (string)DataExtRet.OwnerID.GetValue();
                    }
                    //Get value of DataExtName
                    string DataExtName1889 = (string)DataExtRet.DataExtName.GetValue();
                    //Get value of DataExtType
                    ENDataExtType DataExtType1890 = (ENDataExtType)DataExtRet.DataExtType.GetValue();
                    //Get value of DataExtValue
                    string DataExtValue1891 = (string)DataExtRet.DataExtValue.GetValue();
                }
            }
        }




    }
}
