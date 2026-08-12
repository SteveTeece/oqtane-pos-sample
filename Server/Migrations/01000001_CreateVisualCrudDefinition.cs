using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Oqtane.Databases.Interfaces;
using Oqtane.Migrations;
using Yosh.uwa.Migrations.EntityBuilders;
using Yosh.uwa.Repository;

namespace Yosh.uwa.Migrations
{
    [DbContext(typeof(Context))]
    [Migration("Yosh.uwa.01.00.00.01")]
    public class CreateVisualCrudDefinition : MultiDatabaseMigration
    {
        public CreateVisualCrudDefinition(IDatabase database) : base(database)
        {
        }

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var definitionBuilder = new CrudDefinitionEntityBuilder(migrationBuilder, ActiveDatabase);
            definitionBuilder.Create();
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            var definitionBuilder = new CrudDefinitionEntityBuilder(migrationBuilder, ActiveDatabase);
            definitionBuilder.Drop();
        }
    }
}
