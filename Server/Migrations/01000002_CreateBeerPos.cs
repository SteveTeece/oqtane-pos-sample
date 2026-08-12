using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Oqtane.Databases.Interfaces;
using Oqtane.Migrations;
using Yosh.uwa.Migrations.EntityBuilders;
using Yosh.uwa.Repository;

namespace Yosh.uwa.Migrations
{
    [DbContext(typeof(Context))]
    [Migration("Yosh.uwa.01.00.00.02")]
    public class CreateBeerPos : MultiDatabaseMigration
    {
        public CreateBeerPos(IDatabase database) : base(database) { }
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            new PosStoreEntityBuilder(migrationBuilder, ActiveDatabase).Create();
            new PosCategoryEntityBuilder(migrationBuilder, ActiveDatabase).Create();
            new PosProductEntityBuilder(migrationBuilder, ActiveDatabase).Create();
            new PosOrderEntityBuilder(migrationBuilder, ActiveDatabase).Create();
            new PosOrderLineEntityBuilder(migrationBuilder, ActiveDatabase).Create();
        }
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            new PosOrderLineEntityBuilder(migrationBuilder, ActiveDatabase).Drop();
            new PosOrderEntityBuilder(migrationBuilder, ActiveDatabase).Drop();
            new PosProductEntityBuilder(migrationBuilder, ActiveDatabase).Drop();
            new PosCategoryEntityBuilder(migrationBuilder, ActiveDatabase).Drop();
            new PosStoreEntityBuilder(migrationBuilder, ActiveDatabase).Drop();
        }
    }
}
