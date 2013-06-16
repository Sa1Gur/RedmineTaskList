using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace RedmineTaskListPackage
{
    [Guid("e17deb2b-3254-42f9-83cb-ad86d57afebd")]
    public class RedmineExplorerToolWindow : ToolWindowPane
    {
        public RedmineExplorerToolWindow() :
            base()
        {
            Caption = Resources.ToolWindowTitle;
            Content = new RedmineExplorer();
        }
    }
}
