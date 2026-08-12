using Oqtane.Models;
using Oqtane.Modules;

namespace Yosh.uwa.VisualCrud
{
    public class ModuleInfo : IModule
    {
        public ModuleDefinition ModuleDefinition => new ModuleDefinition
        {
            Name = "Visual CRUD Builder",
            Description = "Build secure CRUD experiences from existing database tables without writing repetitive screens.",
            Version = "1.0.0",
            ServerManagerType = "Yosh.uwa.Manager.VisualCrudManager, Yosh.uwa.Server.Oqtane",
            ReleaseVersions = "1.0.0",
            Dependencies = "Yosh.uwa.Shared.Oqtane",
            PackageName = "Yosh.uwa" 
        };
    }
}
