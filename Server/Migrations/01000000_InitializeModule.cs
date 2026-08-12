using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Oqtane.Databases.Interfaces;
using Oqtane.Migrations;
using Yosh.uwa.Repository;

namespace Yosh.uwa.Migrations
{
    [DbContext(typeof(Context))]
    [Migration("Yosh.uwa.01.00.00.00")]
    public class InitializeModule : MultiDatabaseMigration
    {
        public InitializeModule(IDatabase database) : base(database)
        {
        }

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Reserved initial module migration. Storage is created in 01.00.00.01;
            // early development builds recorded this migration before the CRUD
            // definition table was introduced.
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
