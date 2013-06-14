using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Runtime.InteropServices;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Redmine;

namespace RedmineTaskListPackage
{
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    [ProvideOptionPage(typeof(RedmineOptions), "Redmine Task List", "Connection settings", 101, 106, true)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(Guids.guidVSRedminePkgString)]
    public sealed class RedmineTaskListPackage : Package
    {
        private RedmineTaskProvider _taskProvider;

        public RedmineTaskListPackage() { }


        protected override void Initialize()
        {
            base.Initialize();

            var menuCommandService = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;

            if (menuCommandService != null)
            {
                menuCommandService.AddCommand(new MenuCommand(GetTasksMenuItemCallback, new CommandID(Guids.guidVSRedmineCmdSet, (int)CommandIDs.cmdidGetTasks)));
            }
        }

        private void GetTasksMenuItemCallback(object sender, EventArgs e)
        {
            try
            {
                if (_taskProvider == null)
                {
                    _taskProvider = new RedmineTaskProvider(this);
                    _taskProvider.ProviderName = "Redmine";
                }

                _taskProvider.Tasks.Clear();

                var dte = (DTE)GetService(typeof(DTE));
                var properties = dte.get_Properties("Redmine Task List", "Connection settings");

                var username = Convert.ToString(properties.Item("Username").Value);
                var password = Convert.ToString(properties.Item("Password").Value);
                var url = Convert.ToString(properties.Item("URL").Value);

                foreach (var issue in RedmineTaskList.Get(username, password, url))
                {
                    _taskProvider.Tasks.Add(new Task() {
                        Priority = TaskPriority.Normal,
                        IsPriorityEditable = false,
                        IsCheckedEditable = false,
                        IsTextEditable = false,
                        CanDelete = false,
                        Category = TaskCategory.Misc, // Category seems to be ignored by VS
                        Text = String.Format("#{0} - {1}", issue.Id, issue.Subject),
                    });
                }

                _taskProvider.Show();

                var taskList = GetService(typeof(SVsTaskList)) as IVsTaskList2;

                if (taskList == null)
                {
                    return;
                }

                var guidProvider = typeof(RedmineTaskProvider).GUID;
                
                taskList.SetActiveProvider(ref guidProvider);
            }
            catch (Exception exception)
            {
                Debug.WriteLine(exception);
            }
        }
    }
}
