using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms.Design;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Redmine;

namespace RedmineTaskListPackage
{
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    [ProvideOptionPage(typeof(PackageOptions), OptionsCategoryName, OptionsPageName, 101, 106, true)]
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
            RefreshTasksAsync(taskProvider.Show, ShowOutputPane);
        }


        private void InitializeTaskProvider()
        {
            taskProvider = new RedmineTaskProvider(this);
            taskProvider.Register();
        }

        private void RefreshTasksAsync(Action onSuccess = null, Action onFailure = null)
        {
            lock (syncRoot)
            {
                if (!running)
                {
                    running = true;
                    Action action = new Action(RefreshTasks);
                    action.BeginInvoke((AsyncCallback)(x => {
                        Action callback = onSuccess;

                        try
                        {
                            action.EndInvoke(x);
                        }
                        catch (Exception e)
                        {
                            Debug.WriteLine(e);
                            callback = onFailure;
                        }

                        if (callback != null)
                        {
                            callback.Invoke();
                        }

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
                var options = GetOptions();

                foreach (var issue in GetTasks(options))
                {
                    taskProvider.Tasks.Add(new RedmineTask(options, issue));
                }
            }
            finally
            {
                taskProvider.ResumeRefresh();
            }
        }

        private RedmineIssue[] GetTasks(PackageOptions options)
        {
            OutputLine(String.Format("Retrieving issues from {0} as {1}...", options.URL, options.Username));

            try
            {
                var issues = RedmineTaskList.Get(options.Username, options.Password, options.URL, options.Query);
                
                OutputLine("Done");

                return issues;
            }
            catch
            {
                OutputLine("Error");

                throw;
            }
        }

        private PackageOptions GetOptions()
        {
            var dte = (DTE)GetService(typeof(DTE));
            var properties = dte.get_Properties(OptionsCategoryName, OptionsPageName);

            return new PackageOptions() {
                Username = (string)properties.Item("Username").Value,
                Password = (string)properties.Item("Password").Value,
                URL = (string)properties.Item("URL").Value,
                RequestOnStartup = (bool)properties.Item("RequestOnStartup").Value,
                Query = (string)properties.Item("Query").Value,
                TaskDescriptionFormat = (string)properties.Item("TaskDescriptionFormat").Value,
            };
        }


        private void OutputLine(string s)
        {
            GetOutputPane().OutputString(s + '\n');
        }

        private void ShowOutputPane()
        {
            if (!VsShellUtilities.ShellIsShuttingDown)
            {
                IUIService service = this.GetService(typeof(IUIService)) as IUIService;
                
                if (service != null)
                {
                    service.ShowToolWindow(new Guid(ToolWindowGuids.Outputwindow));
                }

                GetOutputPane().Activate();
            }
        }

        private IVsOutputWindowPane GetOutputPane()
        {
            return GetOutputPane(VSConstants.SID_SVsGeneralOutputWindowPane, "Redmine");
        }
    }
}
