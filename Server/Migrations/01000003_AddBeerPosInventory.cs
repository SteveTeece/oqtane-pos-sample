using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Oqtane.Databases.Interfaces;
using Oqtane.Migrations;
using Yosh.uwa.Repository;

namespace Yosh.uwa.Migrations
{
    [DbContext(typeof(Context))]
    [Migration("Yosh.uwa.01.00.00.03")]
    public class AddBeerPosInventory : MultiDatabaseMigration
    {
        public AddBeerPosInventory(IDatabase database) : base(database) { }
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var table = ActiveDatabase.RewriteName("BeerPosProduct");
            migrationBuilder.AddColumn<bool>(name: "TrackInventory", table: table, nullable: false, defaultValue: true);
            migrationBuilder.AddColumn<int>(name: "StockQuantity", table: table, nullable: false, defaultValue: 25);
            migrationBuilder.AddColumn<int>(name: "LowStockThreshold", table: table, nullable: false, defaultValue: 5);
        }
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            var table = ActiveDatabase.RewriteName("BeerPosProduct");
            migrationBuilder.DropColumn(name: "LowStockThreshold", table: table);
            migrationBuilder.DropColumn(name: "StockQuantity", table: table);
            migrationBuilder.DropColumn(name: "TrackInventory", table: table);
        }
    }
}
