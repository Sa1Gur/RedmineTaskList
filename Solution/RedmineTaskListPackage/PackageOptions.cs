using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace RedmineTaskListPackage
{
    [CLSCompliant(false), ComVisible(true)]
    public class PackageOptions : DialogPage
    {
        public const string DefaultLogin = "admin";
        public const string DefaultUrl = "http://localhost:3000/";
        public const string DefaultTaskDescriptionFormat = "#{0}\\t{4}\\t{13} ({6})";

        [Category("Authentication"), DefaultValue(DefaultLogin)]
        [Description("Username used for authentication. Only issues assigned to this user will be requested.")]
        public string Username { get; set; }
        
        [Category("Authentication"), PasswordPropertyText(true)]
        [Description("Password used for authentication.")]
        public string Password { get; set; }
        
        [Category("Redmine Server"), DefaultValue(DefaultUrl)]
        [Description("Redmine server URL. (Please note that REST web service must be enabled).")]
        public string URL { get; set; }

        [Category("Misc."), DefaultValue(false)]
        [DisplayName("Request on Startup"), Description("Specifies issues will be requested on startup.")]
        public bool RequestOnStartup { get; set; }

        [Category("Query"), DefaultValue(0)]
        [Description("Specifies number of issues requested.")]
        public int Limit { get; set; }

        [Category("Formatting"), DefaultValue(DefaultTaskDescriptionFormat)]
        [DisplayName("Task Description Format"), Description("Formatting used for task description. {0} - Id, {1} - ProjectId, {2} - ProjectName, {3} - TrackerId, {4} - TrackerName, {5} - StatusId, {6} - StatusName, {7} - PriorityId, {8} - PriorityName, {9} - AuthorId, {10} - AuthorName, {11} - AssigneeId, {12} - AssigneeName, {13} - Subject, {14} - Description, {15} - StartDate, {16} - DueDate, {17} - DoneRatio, {18} - EstimatedHours, {19} - CreationTime, {20} - LastUpdateTime, {21} - ClosingTime.")]
        public string TaskDescriptionFormat { get; set; }


        public PackageOptions()
        {
            ResetSettings();
        }

        public override void ResetSettings()
        {
            base.ResetSettings();
            
            Username = DefaultLogin;
            Password = DefaultLogin;
            URL = DefaultUrl;
            RequestOnStartup = false;
            Limit = 0;
            TaskDescriptionFormat = DefaultTaskDescriptionFormat;
        }
    }
}
