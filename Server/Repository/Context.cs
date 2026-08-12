using Microsoft.EntityFrameworkCore;
using Oqtane.Modules;
using Oqtane.Repository;
using Oqtane.Repository.Databases.Interfaces;

namespace Yosh.uwa.Repository
{
    public class Context : DBContextBase, ITransientService, IMultiDatabase
    {
        public virtual DbSet<Models.CrudDefinition> CrudDefinitions { get; set; }
        public virtual DbSet<Models.PosStore> PosStores { get; set; }
        public virtual DbSet<Models.PosCategory> PosCategories { get; set; }
        public virtual DbSet<Models.PosProduct> PosProducts { get; set; }
        public virtual DbSet<Models.PosOrder> PosOrders { get; set; }
        public virtual DbSet<Models.PosOrderLine> PosOrderLines { get; set; }

        public Context(IDBContextDependencies DBContextDependencies) : base(DBContextDependencies)
        {
            // ContextBase handles multi-tenant database connections
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Models.CrudDefinition>().ToTable(ActiveDatabase.RewriteName("VisualCrudDefinition"));
            builder.Entity<Models.PosStore>().ToTable(ActiveDatabase.RewriteName("BeerPosStore"));
            builder.Entity<Models.PosCategory>().ToTable(ActiveDatabase.RewriteName("BeerPosCategory"));
            builder.Entity<Models.PosProduct>().ToTable(ActiveDatabase.RewriteName("BeerPosProduct"));
            builder.Entity<Models.PosOrder>().ToTable(ActiveDatabase.RewriteName("BeerPosOrder"));
            builder.Entity<Models.PosOrderLine>().ToTable(ActiveDatabase.RewriteName("BeerPosOrderLine"));
        }
    }
}
