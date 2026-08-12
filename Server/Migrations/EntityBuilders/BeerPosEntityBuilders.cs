using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Migrations.Operations.Builders;
using Oqtane.Databases.Interfaces;
using Oqtane.Migrations;
using Oqtane.Migrations.EntityBuilders;

namespace Yosh.uwa.Migrations.EntityBuilders
{
    public class PosStoreEntityBuilder : AuditableBaseEntityBuilder<PosStoreEntityBuilder>
    {
        public PosStoreEntityBuilder(MigrationBuilder migrationBuilder, IDatabase database) : base(migrationBuilder, database)
        {
            EntityTableName = "BeerPosStore";
            PrimaryKey = new PrimaryKey<PosStoreEntityBuilder>("PK_BeerPosStore", x => x.PosStoreId);
        }
        protected override PosStoreEntityBuilder BuildTable(ColumnsBuilder table)
        {
            PosStoreId = AddAutoIncrementColumn(table, "PosStoreId");
            SiteId = AddIntegerColumn(table, "SiteId");
            ModuleId = AddIntegerColumn(table, "ModuleId");
            Name = AddMaxStringColumn(table, "Name");
            CurrencySymbol = AddMaxStringColumn(table, "CurrencySymbol");
            TaxRateBasisPoints = AddIntegerColumn(table, "TaxRateBasisPoints");
            ReceiptFooter = AddMaxStringColumn(table, "ReceiptFooter");
            IsOpen = AddBooleanColumn(table, "IsOpen");
            NextOrderNumber = AddIntegerColumn(table, "NextOrderNumber");
            AddAuditableColumns(table);
            return this;
        }
        public OperationBuilder<AddColumnOperation> PosStoreId { get; set; }
        public OperationBuilder<AddColumnOperation> SiteId { get; set; }
        public OperationBuilder<AddColumnOperation> ModuleId { get; set; }
        public OperationBuilder<AddColumnOperation> Name { get; set; }
        public OperationBuilder<AddColumnOperation> CurrencySymbol { get; set; }
        public OperationBuilder<AddColumnOperation> TaxRateBasisPoints { get; set; }
        public OperationBuilder<AddColumnOperation> ReceiptFooter { get; set; }
        public OperationBuilder<AddColumnOperation> IsOpen { get; set; }
        public OperationBuilder<AddColumnOperation> NextOrderNumber { get; set; }
    }

    public class PosCategoryEntityBuilder : AuditableBaseEntityBuilder<PosCategoryEntityBuilder>
    {
        public PosCategoryEntityBuilder(MigrationBuilder migrationBuilder, IDatabase database) : base(migrationBuilder, database)
        {
            EntityTableName = "BeerPosCategory";
            PrimaryKey = new PrimaryKey<PosCategoryEntityBuilder>("PK_BeerPosCategory", x => x.PosCategoryId);
        }
        protected override PosCategoryEntityBuilder BuildTable(ColumnsBuilder table)
        {
            PosCategoryId = AddAutoIncrementColumn(table, "PosCategoryId");
            PosStoreId = AddIntegerColumn(table, "PosStoreId");
            Name = AddMaxStringColumn(table, "Name");
            SortOrder = AddIntegerColumn(table, "SortOrder");
            IsActive = AddBooleanColumn(table, "IsActive");
            AddAuditableColumns(table);
            return this;
        }
        public OperationBuilder<AddColumnOperation> PosCategoryId { get; set; }
        public OperationBuilder<AddColumnOperation> PosStoreId { get; set; }
        public OperationBuilder<AddColumnOperation> Name { get; set; }
        public OperationBuilder<AddColumnOperation> SortOrder { get; set; }
        public OperationBuilder<AddColumnOperation> IsActive { get; set; }
    }

    public class PosProductEntityBuilder : AuditableBaseEntityBuilder<PosProductEntityBuilder>
    {
        public PosProductEntityBuilder(MigrationBuilder migrationBuilder, IDatabase database) : base(migrationBuilder, database)
        {
            EntityTableName = "BeerPosProduct";
            PrimaryKey = new PrimaryKey<PosProductEntityBuilder>("PK_BeerPosProduct", x => x.PosProductId);
        }
        protected override PosProductEntityBuilder BuildTable(ColumnsBuilder table)
        {
            PosProductId = AddAutoIncrementColumn(table, "PosProductId");
            PosStoreId = AddIntegerColumn(table, "PosStoreId");
            PosCategoryId = AddIntegerColumn(table, "PosCategoryId");
            Name = AddMaxStringColumn(table, "Name");
            Brewery = AddMaxStringColumn(table, "Brewery");
            Style = AddMaxStringColumn(table, "Style");
            Description = AddMaxStringColumn(table, "Description");
            Abv = AddMaxStringColumn(table, "Abv");
            PriceCents = AddIntegerColumn(table, "PriceCents");
            Format = AddMaxStringColumn(table, "Format");
            Sku = AddMaxStringColumn(table, "Sku");
            StockLabel = AddMaxStringColumn(table, "StockLabel");
            ImageDataUrl = AddMaxStringColumn(table, "ImageDataUrl");
            Color = AddMaxStringColumn(table, "Color");
            IsActive = AddBooleanColumn(table, "IsActive");
            SortOrder = AddIntegerColumn(table, "SortOrder");
            AddAuditableColumns(table);
            return this;
        }
        public OperationBuilder<AddColumnOperation> PosProductId { get; set; }
        public OperationBuilder<AddColumnOperation> PosStoreId { get; set; }
        public OperationBuilder<AddColumnOperation> PosCategoryId { get; set; }
        public OperationBuilder<AddColumnOperation> Name { get; set; }
        public OperationBuilder<AddColumnOperation> Brewery { get; set; }
        public OperationBuilder<AddColumnOperation> Style { get; set; }
        public OperationBuilder<AddColumnOperation> Description { get; set; }
        public OperationBuilder<AddColumnOperation> Abv { get; set; }
        public OperationBuilder<AddColumnOperation> PriceCents { get; set; }
        public OperationBuilder<AddColumnOperation> Format { get; set; }
        public OperationBuilder<AddColumnOperation> Sku { get; set; }
        public OperationBuilder<AddColumnOperation> StockLabel { get; set; }
        public OperationBuilder<AddColumnOperation> ImageDataUrl { get; set; }
        public OperationBuilder<AddColumnOperation> Color { get; set; }
        public OperationBuilder<AddColumnOperation> IsActive { get; set; }
        public OperationBuilder<AddColumnOperation> SortOrder { get; set; }
    }

    public class PosOrderEntityBuilder : AuditableBaseEntityBuilder<PosOrderEntityBuilder>
    {
        public PosOrderEntityBuilder(MigrationBuilder migrationBuilder, IDatabase database) : base(migrationBuilder, database)
        {
            EntityTableName = "BeerPosOrder";
            PrimaryKey = new PrimaryKey<PosOrderEntityBuilder>("PK_BeerPosOrder", x => x.PosOrderId);
        }
        protected override PosOrderEntityBuilder BuildTable(ColumnsBuilder table)
        {
            PosOrderId = AddAutoIncrementColumn(table, "PosOrderId");
            PosStoreId = AddIntegerColumn(table, "PosStoreId");
            OrderNumber = AddIntegerColumn(table, "OrderNumber");
            SubtotalCents = AddIntegerColumn(table, "SubtotalCents");
            TaxCents = AddIntegerColumn(table, "TaxCents");
            TotalCents = AddIntegerColumn(table, "TotalCents");
            PaymentMethod = AddMaxStringColumn(table, "PaymentMethod");
            Status = AddMaxStringColumn(table, "Status");
            CustomerName = AddMaxStringColumn(table, "CustomerName");
            AddAuditableColumns(table);
            return this;
        }
        public OperationBuilder<AddColumnOperation> PosOrderId { get; set; }
        public OperationBuilder<AddColumnOperation> PosStoreId { get; set; }
        public OperationBuilder<AddColumnOperation> OrderNumber { get; set; }
        public OperationBuilder<AddColumnOperation> SubtotalCents { get; set; }
        public OperationBuilder<AddColumnOperation> TaxCents { get; set; }
        public OperationBuilder<AddColumnOperation> TotalCents { get; set; }
        public OperationBuilder<AddColumnOperation> PaymentMethod { get; set; }
        public OperationBuilder<AddColumnOperation> Status { get; set; }
        public OperationBuilder<AddColumnOperation> CustomerName { get; set; }
    }

    public class PosOrderLineEntityBuilder : AuditableBaseEntityBuilder<PosOrderLineEntityBuilder>
    {
        public PosOrderLineEntityBuilder(MigrationBuilder migrationBuilder, IDatabase database) : base(migrationBuilder, database)
        {
            EntityTableName = "BeerPosOrderLine";
            PrimaryKey = new PrimaryKey<PosOrderLineEntityBuilder>("PK_BeerPosOrderLine", x => x.PosOrderLineId);
        }
        protected override PosOrderLineEntityBuilder BuildTable(ColumnsBuilder table)
        {
            PosOrderLineId = AddAutoIncrementColumn(table, "PosOrderLineId");
            PosOrderId = AddIntegerColumn(table, "PosOrderId");
            PosProductId = AddIntegerColumn(table, "PosProductId");
            ProductName = AddMaxStringColumn(table, "ProductName");
            Quantity = AddIntegerColumn(table, "Quantity");
            UnitPriceCents = AddIntegerColumn(table, "UnitPriceCents");
            LineTotalCents = AddIntegerColumn(table, "LineTotalCents");
            AddAuditableColumns(table);
            return this;
        }
        public OperationBuilder<AddColumnOperation> PosOrderLineId { get; set; }
        public OperationBuilder<AddColumnOperation> PosOrderId { get; set; }
        public OperationBuilder<AddColumnOperation> PosProductId { get; set; }
        public OperationBuilder<AddColumnOperation> ProductName { get; set; }
        public OperationBuilder<AddColumnOperation> Quantity { get; set; }
        public OperationBuilder<AddColumnOperation> UnitPriceCents { get; set; }
        public OperationBuilder<AddColumnOperation> LineTotalCents { get; set; }
    }
}
