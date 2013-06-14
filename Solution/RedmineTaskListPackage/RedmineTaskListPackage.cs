using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Runtime.InteropServices;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Redmine;

namespace RedmineTaskListPackage
{
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    [ProvideOptionPage(typeof(RedmineOptions), "Redmine Task List", "Connection settings", 101, 106, true)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.NoSolution_string)] 
    [Guid(Guids.guidRedminePkgString)]
    public sealed class RedmineTaskListPackage : Package, IDisposable
    {
        private RedmineTaskProvider taskProvider;
        private MenuCommand getTasksMenuCommand;

        public RedmineTaskListPackage()
        {
            var getTasksCommandID = new CommandID(Guids.guidRedmineCmdSet, (int)CommandIDs.cmdidGetTasks);
            
            getTasksMenuCommand = new MenuCommand(GetTasksMenuItemCallback, getTasksCommandID);
        }


        protected override void Initialize()
        {
            base.Initialize();

            InitializeTaskProvider();
            AddMenuCommands();
        }

        private void AddMenuCommands()
        {
            var menuCommandService = GetService(typeof(IMenuCommandService)) as IMenuCommandService;

            if (menuCommandService != null)
            {
                menuCommandService.AddCommand(getTasksMenuCommand);
            }
        }

        private void GetTasksMenuItemCallback(object sender, EventArgs e)
        {
            try
            {
                RefreshTasks();
                taskProvider.Show();
            }
            catch (Exception exception)
            {
                Debug.WriteLine(exception);
            }
        }


        private void InitializeTaskProvider()
        {
            taskProvider = new RedmineTaskProvider(this);
            taskProvider.Register();
        }

        private void RefreshTasks()
        {
            taskProvider.SuspendRefresh();
            taskProvider.Tasks.Clear();

            foreach (var issue in GetTasks())
            {
                AddTask(issue);
            }

            taskProvider.ResumeRefresh();
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
                ImageIndex = 2,
                Category = TaskCategory.Misc,
                Text = String.Format("#{0} - {1}", issue.Id, issue.Subject),
            });
        }

        public void Dispose()
        {
            taskProvider.Dispose();
        }
    }
}
