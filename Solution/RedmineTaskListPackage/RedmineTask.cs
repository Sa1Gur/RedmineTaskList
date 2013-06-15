using System;
using System.Diagnostics;
using Microsoft.VisualStudio.Shell;
using Redmine;

namespace RedmineTaskListPackage
{
    public class RedmineTask : Task
    {
        private string issueUrl;

        public RedmineTask(PackageOptions options, RedmineIssue issue)
        {
            issueUrl = GetIssueUrl(options.URL, issue.Id);

            CanDelete = false;
            Checked = false;

            IsCheckedEditable = false;
            IsPriorityEditable = false;
            IsTextEditable = false;

            ImageIndex = 2;

            Category = TaskCategory.Misc;
            Priority = (TaskPriority)Math.Max(3 - issue.PriorityId, 0);

            Text = String.Format("#{0}\t{1}\t{2} ({3})", issue.Id, issue.TrackerName, issue.Subject, issue.StatusName);
        }

        protected override void OnNavigate(EventArgs e)
        {
            base.OnNavigate(e);

            Process.Start(issueUrl);
        }

        private static string GetIssueUrl(string baseUriString, int issueId)
        {
            var baseUri = new Uri(baseUriString);
            var uri = new Uri(baseUri, String.Concat("issues/", issueId));
            
            return uri.ToString();
        }
    }
}
