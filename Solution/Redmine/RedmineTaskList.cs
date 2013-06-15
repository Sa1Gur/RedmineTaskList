using System;
using System.Collections.Generic;
using System.Linq;

namespace Redmine
{
    public static class RedmineTaskList
    {
        private static Dictionary<string, int> UserCache;

        static RedmineTaskList()
        {
            UserCache = new Dictionary<string, int>();
        }

        public static void ClearUserCache()
        {
            UserCache.Clear();
        }

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
            var cacheKey = GetCacheKey(username, baseUri);
            var cachedUser = GetCachedUser(cacheKey);

            if (cachedUser.Key == null)
            {
                cachedUser = RequestAndCache(username, password, baseUri);
            }

            return cachedUser.Value;
        }

        private static KeyValuePair<string, int> RequestAndCache(string username, string password, Uri baseUri)
        {
            var cacheKey = GetCacheKey(username, baseUri);
            var uri = new Uri(baseUri, "users.xml");
            var xml = new RedmineWebRequest(username, password, uri).GetResponse();
            var users = RedmineXmlParser.ParseUsers(xml);
            var user = users.FirstOrDefault(x => x.Login.Equals(username));

            if (user != null)
            {
                UserCache.Add(cacheKey, user.Id);
            }

            return GetCachedUser(cacheKey);
        }

        private static string GetCacheKey(string username, Uri baseUri)
        {
            return String.Concat(username, '@', baseUri);
        }

        private static KeyValuePair<string, int> GetCachedUser(string cacheKey)
        {
            return UserCache.FirstOrDefault(x => x.Key.Equals(cacheKey));;
        }
    }
}