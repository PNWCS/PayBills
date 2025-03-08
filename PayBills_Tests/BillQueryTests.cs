using QBFC16Lib;
namespace PayBills_Tests
{
    public class BillQueryTests
    {
        [Fact]
        public void Test1()
        {

        }
    }
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