using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Redmine;

namespace RedmineTaskListPackage
{
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    [ProvideOptionPage(typeof(RedmineOptions), OptionsCategoryName, OptionsPageName, 101, 106, true)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.NoSolution_string)] 
    [Guid(Guids.guidRedminePkgString)]
    public sealed class RedmineTaskListPackage : Package, IDisposable
    {
        public const string OptionsCategoryName = "Redmine Task List";
        public const string OptionsPageName = "General";

        private RedmineTaskProvider taskProvider;
        private MenuCommand getTasksMenuCommand;
        private object syncRoot;
        private bool running;

        public RedmineTaskListPackage()
        {
            var getTasksCommandID = new CommandID(Guids.guidRedmineCmdSet, (int)CommandIDs.cmdidGetTasks);
            
            getTasksMenuCommand = new MenuCommand(GetTasksMenuItemCallback, getTasksCommandID);
            syncRoot = new object();
        }
        

        public void Dispose()
        {
            taskProvider.Dispose();
        }

        protected override void Initialize()
        {
            base.Initialize();

            InitializeTaskProvider();
            AddMenuCommands();

            if (GetOptions().RequestOnStartup)
            {
                RefreshTasksAsync();
            }
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
            RefreshTasksAsync(taskProvider.Show);
        }


        private void InitializeTaskProvider()
        {
            taskProvider = new RedmineTaskProvider(this);
            taskProvider.Register();
        }

        private void RefreshTasksAsync()
        {
            RefreshTasksAsync(() => { });
        }

        private void RefreshTasksAsync(Action callback)
        {
            lock (syncRoot)
            {
                if (!running)
                {
                    running = true;
                    Action action = new Action(RefreshTasks);
                    action.BeginInvoke((AsyncCallback)(x => { 
                        action.EndInvoke(x); 
                        callback.Invoke(); 
                        running = false; 
                    }), null);
                }
            }
        }

        private void RefreshTasks()
        {
            taskProvider.SuspendRefresh();
            taskProvider.Tasks.Clear();

            try
            {
                foreach (var issue in GetTasks())
                {
                    AddTask(issue);
                }
            }
            catch (Exception exception)
            {
                Debug.WriteLine(exception);
            }
            finally
            {
                taskProvider.ResumeRefresh();
            }
        }

        private RedmineIssue[] GetTasks()
        {
            var options = GetOptions();

            OutputLine(String.Format("Retrieving issues from {0} as {1}...", options.URL, options.Username));

            try
            {
                var issues = RedmineTaskList.Get(options.Username, options.Password, options.URL);
                
                OutputLine("Done");

                return issues;
            }
            catch
            {
                OutputLine("Error");

                throw;
            }
        }

        private RedmineOptions GetOptions()
        {
            var dte = (DTE)GetService(typeof(DTE));
            var properties = dte.get_Properties(OptionsCategoryName, OptionsPageName);

            return new RedmineOptions() {
                Username = (string)properties.Item("Username").Value,
                Password = (string)properties.Item("Password").Value,
                URL = (string)properties.Item("URL").Value,
                RequestOnStartup = (bool)properties.Item("RequestOnStartup").Value,
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

        private void OutputLine(string s)
        {
            var outputPane = GetOutputPane(VSConstants.SID_SVsGeneralOutputWindowPane, "Redmine");
            
            outputPane.OutputString(s + '\n');
        }
    }
}
