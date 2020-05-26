using Microsoft;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Redmine;
using RedmineTaskListPackage.Forms;
using RedmineTaskListPackage.ViewModel;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

using Task = System.Threading.Tasks.Task;//essential, because compiler takes wrong Task somehow...

namespace RedmineTaskListPackage
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("#110", "#112", "#version", IconResourceID = 400)]
    //[ProvideService(typeof(MyService), IsAsyncQueryable = true)]
    [ProvideOptionPage(typeof(PackageOptions), PackageOptions.Category, PackageOptions.Page, 101, 106, true)]
    [ProvideToolWindow(typeof(RedmineIssueViewerToolWindow))]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.NoSolution_string, PackageAutoLoadFlags.BackgroundLoad)]
    [Guid(Guids.guidRedminePkgString)]
    public sealed class RedmineTaskListPackage : AsyncPackage, IDisposable, IDebug
    {
        private RedmineTaskProvider taskProvider;
        private MenuCommand getTasksMenuCommand;
        private MenuCommand viewIssueMenuCommand;
        private MenuCommand projectSettingsMenuCommand;
        private RedmineIssueViewerToolWindow issueViewerWindow;
        private RedmineWebBrowser webBrowser;
        
        object syncRoot;
        bool refreshing;
        
        EnvDTE.SolutionEvents solutionEvents;

        LoadedRedmineIssue[] _currentIssues;

        PackageOptions Options { get => PackageOptions.GetOptions(this); }

        public RedmineTaskListPackage()
        {
            var getTasksCommandID = new CommandID(Guids.guidRedmineCmdSet, (int)CommandIDs.cmdidGetTasks);
            getTasksMenuCommand = new MenuCommand(GetTasksMenuItemCallback, getTasksCommandID);

            var viewIssueCommandID = new CommandID(Guids.guidRedmineCmdSet, (int)CommandIDs.cmdidViewIssues);
            viewIssueMenuCommand = new MenuCommand(ViewIssueMenuItemCallback, viewIssueCommandID);

            var projectSettingsCommandID = new CommandID(Guids.guidRedmineCmdSet, (int)CommandIDs.cmdidProjectSettings);
            projectSettingsMenuCommand = new MenuCommand(ProjectSettingsMenuItemCallback, projectSettingsCommandID);

            webBrowser = new RedmineWebBrowser { ServiceProvider = this };
            syncRoot = new object();
        }

        public void Dispose() => taskProvider.Dispose();

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            InitializeTaskProvider();
            await AddMenuCommandsAsync();

            //await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var dte = await GetServiceAsync(typeof(EnvDTE.DTE)) as EnvDTE.DTE;
            Assumes.Present(dte);
            solutionEvents = dte.Events.SolutionEvents;
            

            solutionEvents.Opened += RefreshTasksAsync;
            solutionEvents.AfterClosing += () => taskProvider.Tasks.Clear();

            PackageOptions.Applied += (s, e) => OnPackageOptionsApplied();

            if (Options.RequestOnStartup)
            {
                RefreshTasksAsync();
            }
        }

        void OnPackageOptionsApplied() => PopulateTaskList(_currentIssues);

        RedmineTask CreateTask(LoadedRedmineIssue issue, string format)
        {
            var task = new RedmineTask(issue, format);

            task.Navigate += (s, e) => Show(issue);

            return task;
        }

        async Task AddMenuCommandsAsync()
        {
            var menuService = await GetServiceAsync(typeof(IMenuCommandService)) as IMenuCommandService;

            menuService?.AddCommand(getTasksMenuCommand);
            menuService?.AddCommand(viewIssueMenuCommand);
            menuService?.AddCommand(projectSettingsMenuCommand);
        }

        async void GetTasksMenuItemCallback(object sender, EventArgs e)
        {
            InitializeIssueViewerWindow();

            await RefreshTasksAsync(taskProvider.Show);
        }

        void ViewIssueMenuItemCallback(object sender, EventArgs e)
        {
            InitializeIssueViewerWindow();

            issueViewerWindow.Show();
        }

        async void ProjectSettingsMenuItemCallback(object sender, EventArgs e)
        {
            var project = GetSelectedProject() as EnvDTE.Project;
            var storage = GetConnectionSettingsStorage(project);

            if (storage == null)
            {
                return;
            }

            var dialog = new ConnectionSettingsDialog
            {
                ConnectionSettings = storage.Load(),
                StartPosition = FormStartPosition.CenterParent,
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                storage.Save(dialog.ConnectionSettings);
                await RefreshTasksAsync(null);
            }
        }


        private object GetSelectedProject()
        {
            var dte = (EnvDTE.DTE)GetService(typeof(EnvDTE.DTE));
            var projects = dte.ActiveSolutionProjects as Array;

            return projects.Length > 0 ? projects.GetValue(0) : null;
        }

        void InitializeTaskProvider()
        {
            taskProvider = new RedmineTaskProvider(this);
            taskProvider.Register();
        }


        async void RefreshTasksAsync() => await RefreshTasksAsync(null);        

        async Task RefreshTasksAsync(Action callback)
        {
            return;

            lock (syncRoot)
            {
                BeginRefreshTasks(callback);
            }
        }

        private void BeginRefreshTasks(Action callback)
        {
            if (refreshing)
            {
                return;
            }

            refreshing = true;

            return;

            var refresh = new Action(RefreshTasks);

            callback = callback ?? (() => { });

            refresh.BeginInvoke(x =>
            {
                refresh.EndInvoke(x);
                callback.Invoke();
                refreshing = false;
            }, null);
        }

        private void RefreshTasks()
        {
            var issues = LoadIssues();

            PopulateTaskList(issues);
        }

        private void PopulateTaskList(LoadedRedmineIssue[] issues)
        {
            _currentIssues = issues;

            taskProvider.SuspendRefresh();
            taskProvider.Tasks.Clear();

            foreach (var issue in issues ?? new LoadedRedmineIssue[0])
            {
                taskProvider.Tasks.Add(CreateTask(issue, Options.TaskDescriptionFormat));
            }

            taskProvider.ResumeRefresh();
        }

        private LoadedRedmineIssue[] LoadIssues()
        {
            CertificateValidator.ValidateAny = Options.ValidateAnyCertificate;
            CertificateValidator.Thumbprint = Options.CertificateThumbprint;

            var loader = new IssueLoader
            {
                Proxy = Options.GetProxy(),
                Debug = this as IDebug,
            };

            return loader.LoadIssues(GetConnectionSettings());
        }

        ConnectionSettings[] GetConnectionSettings()
        {
            var settings = GetProjectConnectionSettings();

            if (Options.RequestGlobal)
            {
                settings.Insert(0, Options.GetConnectionSettings());
            }

            return settings.ToArray();
        }

        List<ConnectionSettings> GetProjectConnectionSettings()
        {
            var dte = (EnvDTE.DTE)GetService(typeof(EnvDTE.DTE));

            return dte.Solution.Projects.Cast<EnvDTE.Project>().Select(LoadConectionSettings).Where(x => x != null).ToList();
        }


        private ConnectionSettings LoadConectionSettings(EnvDTE.Project project)
        {
            var storage = GetConnectionSettingsStorage(project);

            return storage != null ? storage.Load() : null;
        }

        private ConnectionSettingsStorage GetConnectionSettingsStorage(EnvDTE.Project project)
        {
            if (project == null || !TryGetProjectFullName(project, out string projectPath))
            {
                return null;
            }

            return GetConnectionSettingsStorage(projectPath);
        }

        private ConnectionSettingsStorage GetConnectionSettingsStorage(string projectPath)
        {
            var propertyStorage = GetPropertyStorage(projectPath);

            if (propertyStorage == null)
            {
                return null;
            }

            return new ConnectionSettingsStorage(propertyStorage);
        }

        IVsBuildPropertyStorage GetPropertyStorage(string projectPath)
        {
            var solution = GetService(typeof(SVsSolution)) as IVsSolution;
            
            //TODO handle solution null
            solution.GetProjectOfUniqueName(projectPath, out IVsHierarchy hierarchy);

            return hierarchy as IVsBuildPropertyStorage;
        }

        bool TryGetProjectFullName(EnvDTE.Project project, out string fullName)
        {
            var result = true;
            fullName = null;

            try
            {
                fullName = project.FullName;
            }
            catch
            {
                result = false;
            }

            return result;
        }

        void IDebug.WriteLine(string s)
        {
            if (Options.EnableDebugOutput)
            {
                OutputLine(s);
            }
        }

        void OutputLine(string s) => GetOutputPane().OutputString(s + Environment.NewLine);        

        IVsOutputWindowPane GetOutputPane() => GetOutputPane(VSConstants.SID_SVsGeneralOutputWindowPane, "Redmine");        

        void InitializeIssueViewerWindow()
        {
            if (issueViewerWindow != null)
            {
                return;
            }

            issueViewerWindow = FindToolWindow(typeof(RedmineIssueViewerToolWindow), 0, true) as RedmineIssueViewerToolWindow;

            if (issueViewerWindow == null || issueViewerWindow.Frame == null)
            {
                throw new NotSupportedException(Resources.CanNotCreateWindow);
            }
        }

        void Show(LoadedRedmineIssue issue)
        {
            if (Options.OpenTasksInWebBrowser)
            {
                webBrowser.Open(issue);
            }
            else
            {
                ViewIssue(issue);
            }
        }

        private void ViewIssue(LoadedRedmineIssue issue)
        {
            InitializeIssueViewerWindow();

            var issueWithJournals = LoadIssueWithJournals(issue.ConnectionSettings, issue.Id) ?? new RedmineIssue();

            issueViewerWindow.Show(new RedmineIssueViewModel(issueWithJournals)
            {
                WebBrowser = webBrowser
            });
        }

        private RedmineIssue LoadIssueWithJournals(ConnectionSettings settings, int id)
        {
            var issueWithJournals = default(RedmineIssue);
            var redmine = IssueLoader.GetRedmineService(settings, Options.GetProxy());

            try
            {
                issueWithJournals = redmine.GetIssueWithJournals(id);
            }
            catch (Exception e)
            {
                var Debug = (IDebug)this;

                Debug.WriteLine($"User-name: {settings.Username} URL: {redmine.BaseUriString}");
                Debug.WriteLine(e.ToString());
            }

            return issueWithJournals;
        }
    }
}
