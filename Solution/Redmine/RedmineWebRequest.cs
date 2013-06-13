using System;
using System.IO;
using System.Net;
using System.Text;

namespace Redmine
{
    public class RedmineWebRequest
    {
        private WebRequest _request;

        public RedmineWebRequest(string requestUriString, string username, string password)
        {
            var authString = String.Concat(username, ':', password);
            
            _request = WebRequest.Create(requestUriString);
            _request.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(Encoding.ASCII.GetBytes(authString)));
        }

        public string GetResponse()
        {
            WebResponse response = _request.GetResponse();
            
            using (StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
            {
                return reader.ReadToEnd();
            }
        }
    }
}