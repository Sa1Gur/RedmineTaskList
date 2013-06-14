using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace RedmineTaskListPackage
{
    [Guid("c69a7a86-945e-4884-85d6-eebae9247598")]
    public class RedmineTaskProvider : TaskProvider
    {
        public RedmineTaskProvider(IServiceProvider provider)
            : base(provider)
        {
            ProviderName = "Redmine";
        }
    }
}
