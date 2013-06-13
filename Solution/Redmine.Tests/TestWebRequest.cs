using System;
using System.Net;

namespace Redmine.Tests
{
    public class TestWebRequest : WebRequest, IWebRequestCreate
    {
        private TestWebRequest _request;

        public TestWebRequest() { }

        private TestWebRequest(TestWebRequest request)
        {
            _request = request;
        }
        
        new public virtual void Create(Uri uri) { } // Used for mock expectation

        public static IWebRequestCreate GetCreator(TestWebRequest request)
        {
            return new TestWebRequest(request);
        }

        WebRequest IWebRequestCreate.Create(Uri uri)
        {
            _request.Create(uri);

            return _request;
        }
    }
}
