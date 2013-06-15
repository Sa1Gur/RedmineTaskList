using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace RedmineTaskListPackage
{
    [CLSCompliant(false), ComVisible(true)]
    public class RedmineOptions : DialogPage
    {
        [Category("Authentication")]
        [Description("Username used for authentication. Only issues assigned to this user will be requested.")]
        public string Username { get; set; }
        
        [Category("Authentication"), PasswordPropertyText(true)]
        [Description("Password used for authentication.")]
        public string Password { get; set; }
        
        [Category("Redmine Server")]
        [Description("Redmine server URL. (Please note that REST web service must be enabled).")]
        public string URL { get; set; }


        public RedmineOptions()
        {
            Username = "admin";
            Password = Username;
            URL = "http://localhost:3000";
        }
    }
}
