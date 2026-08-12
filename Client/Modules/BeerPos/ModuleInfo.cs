using Oqtane.Models;
using Oqtane.Modules;

namespace Yosh.uwa.BeerPos
{
    public class ModuleInfo : IModule
    {
        public ModuleDefinition ModuleDefinition => new()
        {
            Name = "Beer POS",
            Description = "A fast, touch-friendly point of sale for taprooms and beer bars.",
            Version = "1.2.0",
            ServerManagerType = "Yosh.uwa.Manager.BeerPosManager, Yosh.uwa.Server.Oqtane",
            ReleaseVersions = "1.0.0,1.1.0,1.2.0",
            Dependencies = "Yosh.uwa.Shared.Oqtane",
            PackageName = "Yosh.uwa"
        };
    }
}
