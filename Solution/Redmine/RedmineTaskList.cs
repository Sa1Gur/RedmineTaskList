using System;
using System.Linq;

namespace Redmine
{
    public class RedmineTaskList
    {
        public static RedmineIssue[] Get(string username, string password, string baseUriString)
        {
            var baseUri = new Uri(baseUriString);
            var userId = FindUserId(username, password, baseUri);

            var uri = new Uri(baseUri, String.Concat("issues.xml?assigned_to_id=", userId));
            var xml = new RedmineWebRequest(username, password, uri).GetResponse();
            
            return RedmineXmlParser.ParseIssues(xml);
        }

        private static int FindUserId(string username, string password, Uri baseUri)
        {
            var uri = new Uri(baseUri, "users.xml");
            var xml = new RedmineWebRequest(username, password, uri).GetResponse();
            var users = RedmineXmlParser.ParseUsers(xml);
            var user = users.FirstOrDefault(x => x.Login.Equals(username));
            
            return user != null ? user.Id : 0;
        }
    }
}