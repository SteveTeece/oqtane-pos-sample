using Oqtane.Enums;
using Oqtane.Infrastructure;
using Oqtane.Interfaces;
using Oqtane.Models;
using Oqtane.Modules;
using Oqtane.Repository;
using Yosh.uwa.Repository;

namespace Yosh.uwa.Manager
{
    public class BeerPosManager : MigratableModuleBase, IInstallable
    {
        private readonly IDBContextDependencies _dependencies;
        public BeerPosManager(IDBContextDependencies dependencies) => _dependencies = dependencies;
        public bool Install(Tenant tenant, string version) => Migrate(new Context(_dependencies), tenant, MigrationType.Up);
        public bool Uninstall(Tenant tenant) => true;
    }
}
