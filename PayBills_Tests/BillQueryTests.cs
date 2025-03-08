

using System.Diagnostics;
using PayBills_Lib;
using QBFC16Lib;


namespace PayBills_Tests
{
    public class BillQueryTests
    {
        [Fact]
        public void DoBillToPayQuery_ShouldReturnResults()
        {
            using (var qbSession = new QuickBooksSession("Integration Test - Bill Query"))
            {
                try
                {
                    Bill_Query.DoBillToPayQuery();
                    var response = QueryBillToPay();
                    Assert.NotNull(response);
                    Assert.True(response.Count > 0, "Expected at least one bill to pay.");
                }
                catch (Exception ex)
                {
                    Assert.Fail($"Test failed with exception: {ex.Message}");
                }
            }
        }

        [Fact]
        public void DoBillToPayQuery_Performance_ShouldNotExceed2Sec()
        {
            const int MaxQueryTimeMilliseconds = 2000; // 2 seconds

            using (var qbSession = new QuickBooksSession("Performance Test - Bill Query"))
            {
                try
                {
                    var stopwatch = Stopwatch.StartNew();
                    Bill_Query.DoBillToPayQuery();
                    stopwatch.Stop();
                    long elapsedMs = stopwatch.ElapsedMilliseconds;
                    Assert.True(elapsedMs <= MaxQueryTimeMilliseconds,
                        $"Query took {elapsedMs}ms, exceeding 2-second limit.");
                }
                catch (Exception ex)
                {
                    Assert.Fail($"Performance test failed with exception: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Queries BillToPay and returns the response list.
        /// </summary>
        private IResponseList QueryBillToPay()
        {
            using (var qbSession = new QuickBooksSession("Query Test - BillToPay"))
            {
                IMsgSetRequest requestMsgSet = qbSession.CreateRequestSet();
                Bill_Query.BuildBillToPayQueryRq(requestMsgSet);
                IMsgSetResponse responseMsgSet = qbSession.SendRequest(requestMsgSet);

                return responseMsgSet.ResponseList;
            }
        }
    }

    /// <summary>
    /// Encapsulates QuickBooks session handling.
    /// </summary>
    public class QuickBooksSession : IDisposable
    {
        private QBSessionManager _sessionManager;
        private bool _sessionBegun;
        private bool _connectionOpen;

        public QuickBooksSession(string appName)
        {
            _sessionManager = new QBSessionManager();
            _sessionManager.OpenConnection("", appName);
            _connectionOpen = true;
            _sessionManager.BeginSession("", ENOpenMode.omDontCare);
            _sessionBegun = true;
        }

        public IMsgSetRequest CreateRequestSet()
        {
            IMsgSetRequest requestMsgSet = _sessionManager.CreateMsgSetRequest("US", 16, 0);
            requestMsgSet.Attributes.OnError = ENRqOnError.roeContinue;
            return requestMsgSet;
        }

        public IMsgSetResponse SendRequest(IMsgSetRequest requestMsgSet)
        {
            return _sessionManager.DoRequests(requestMsgSet);
        }

        public void Dispose()
        {
            if (_sessionBegun)
            {
                _sessionManager.EndSession();
            }
            if (_connectionOpen)
            {
                _sessionManager.CloseConnection();
            }
        }
    }
}
