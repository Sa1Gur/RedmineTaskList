using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
using RedmineTaskListPackage.Tree;

namespace RedmineTaskListPackage
{
    [Guid("e17deb2b-3254-42f9-83cb-ad86d57afebd")]
    public class RedmineExplorerToolWindow : ToolWindowPane
    {
        RedmineExplorer explorer;

        public RedmineExplorerToolWindow() :
            base()
        {
            explorer = new RedmineExplorer();

            Caption = Resources.ToolWindowTitle;
            Content = explorer;
        }

        public void SetTree(RedmineProjectTree tree)
        {
            explorer.DataContext = tree;
        }
    }
}
