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
        }
    }
}
