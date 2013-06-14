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
        private RedmineTaskProvider taskProvider;
        private MenuCommand getTasksMenuCommand;

        public RedmineTaskListPackage()
        {
            var getTasksCommandID = new CommandID(Guids.guidVSRedmineCmdSet, (int)CommandIDs.cmdidGetTasks);
            
            getTasksMenuCommand = new MenuCommand(GetTasksMenuItemCallback, getTasksCommandID);
        }


        protected override void Initialize()
        {
            base.Initialize();

            var menuCommandService = GetService(typeof(IMenuCommandService)) as IMenuCommandService;
            
            menuCommandService.AddCommand(getTasksMenuCommand);
        }

        private void GetTasksMenuItemCallback(object sender, EventArgs e)
        {
            try
            {
                InitializeTaskProvider();
                RefreshTasks();
                ShowTaskList();
            }
            catch (Exception exception)
            {
                Debug.WriteLine(exception);
            }
        }

        private void InitializeTaskProvider()
        {
            if (taskProvider == null)
            {
                taskProvider = new RedmineTaskProvider(this);
            }
        }

        private void RefreshTasks()
        {
            ClearTasks();

            foreach (var issue in GetTasks())
            {
                AddTask(issue);
            }
        }

        private void ClearTasks()
        {
            taskProvider.Tasks.Clear();
        }

        private RedmineIssue[] GetTasks()
        {
            var options = GetOptions();

            return RedmineTaskList.Get(options.Username, options.Password, options.URL);
        }

        private RedmineOptions GetOptions()
        {
            var dte = (DTE)GetService(typeof(DTE));
            var properties = dte.get_Properties("Redmine Task List", "Connection settings");

            return new RedmineOptions() {
                Username = Convert.ToString(properties.Item("Username").Value),
                Password = Convert.ToString(properties.Item("Password").Value),
                URL = Convert.ToString(properties.Item("URL").Value),
            };
        }

        private void AddTask(RedmineIssue issue)
        {
            taskProvider.Tasks.Add(new Task() {
                Priority = TaskPriority.Normal,
                IsPriorityEditable = false,
                IsCheckedEditable = false,
                IsTextEditable = false,
                CanDelete = false,
                Category = TaskCategory.Misc, // Category seems to be ignored by VS
                Text = String.Format("#{0} - {1}", issue.Id, issue.Subject),
            });
        }

        private void ShowTaskList()
        {
            var guidProvider = typeof(RedmineTaskProvider).GUID;
            var taskList = GetService(typeof(SVsTaskList)) as IVsTaskList2;

            if (taskList != null)
            {
                taskList.SetActiveProvider(ref guidProvider);
            }

            taskProvider.Show();
        }
    }
}
