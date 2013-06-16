﻿using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms.Design;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Redmine;
using RedmineTaskListPackage.Tree;

namespace RedmineTaskListPackage
{
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    [ProvideOptionPage(typeof(PackageOptions), PackageOptions.Category, PackageOptions.Page, 101, 106, true)]
    [ProvideToolWindow(typeof(RedmineExplorerToolWindow))]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.NoSolution_string)] 
    [Guid(Guids.guidRedminePkgString)]
    public sealed class RedmineTaskListPackage : Package, IDisposable
    {
        private RedmineTaskProvider taskProvider;
        private MenuCommand getTasksMenuCommand;
        private MenuCommand showExplorerMenuCommand;
        private object syncRoot;
        private bool running;

        public RedmineTaskListPackage()
        {
            var getTasksCommandID = new CommandID(Guids.guidRedmineCmdSet, (int)CommandIDs.cmdidGetTasks);
            getTasksMenuCommand = new MenuCommand(GetTasksMenuItemCallback, getTasksCommandID);
            
            var showExplorerCommandID = new CommandID(Guids.guidRedmineCmdSet, (int)CommandIDs.cmdidRedmineExplorer);
            showExplorerMenuCommand = new MenuCommand(ShowExplorerMenuItemCallback, showExplorerCommandID);
            
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
                menuCommandService.AddCommand(showExplorerMenuCommand);
            }
        }

        private void GetTasksMenuItemCallback(object sender, EventArgs e)
        {
            RefreshTasksAsync(taskProvider.Show, ShowOutputPane);
        }

        private void ShowExplorerMenuItemCallback(object sender, EventArgs e)
        {
            var window = FindToolWindow(typeof(RedmineExplorerToolWindow), 0, true) as RedmineExplorerToolWindow;
            
            if (window == null || window.Frame == null)
            {
                throw new NotSupportedException(Resources.CanNotCreateWindow);
            }

            var options = GetOptions();
            var projects = RedmineService.GetProjects(options.Username, options.Password, options.URL);
            var tree = RedmineProjectTree.Create(projects);
            
            window.SetTree(tree);
            
            var windowFrame = (IVsWindowFrame)window.Frame;
            ErrorHandler.ThrowOnFailure(windowFrame.Show());
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
                var issues = RedmineService.GetIssues(options.Username, options.Password, options.URL, options.Query);
                
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
            return PackageOptions.GetOptions(this);
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
