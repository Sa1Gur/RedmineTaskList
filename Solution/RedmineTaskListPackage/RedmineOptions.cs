using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace RedmineTaskListPackage
{
    [CLSCompliant(false), ComVisible(true)]
    public class RedmineOptions : DialogPage
    {
        [Category("Connection")]
        public string Username { get; set; }
        
        [Category("Connection"), PasswordPropertyText]
        public string Password { get; set; }
        
        [Category("Connection")]
        public string URL { get; set; }
    }
}
