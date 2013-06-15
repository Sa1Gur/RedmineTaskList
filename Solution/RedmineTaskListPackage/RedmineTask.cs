using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
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

            Text = String.Format(
                Regex.Unescape(options.TaskDescriptionFormat),
                issue.Id,
                issue.ProjectId,
                issue.ProjectName,
                issue.TrackerId,
                issue.TrackerName,
                issue.StatusId,
                issue.StatusName,
                issue.PriorityId,
                issue.PriorityName,
                issue.AuthorId,
                issue.AuthorName,
                issue.AssigneeId,
                issue.AssigneeName,
                issue.Subject,
                issue.Description,
                issue.StartDate,
                issue.DueDate,
                issue.DoneRatio,
                issue.EstimatedHours,
                issue.CreationTime,
                issue.LastUpdateTime,
                issue.ClosingTime
             );
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
