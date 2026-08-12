using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Migrations.Operations.Builders;
using Oqtane.Databases.Interfaces;
using Oqtane.Migrations;
using Oqtane.Migrations.EntityBuilders;

namespace Yosh.uwa.Migrations.EntityBuilders
{
    public class CrudDefinitionEntityBuilder : AuditableBaseEntityBuilder<CrudDefinitionEntityBuilder>
    {
        private const string EntityTableNameValue = "VisualCrudDefinition";
        private readonly PrimaryKey<CrudDefinitionEntityBuilder> _primaryKey = new("PK_VisualCrudDefinition", x => x.CrudDefinitionId);
        private readonly ForeignKey<CrudDefinitionEntityBuilder> _moduleForeignKey = new("FK_VisualCrudDefinition_Module", x => x.ModuleId, "Module", "ModuleId", ReferentialAction.Cascade);

        public CrudDefinitionEntityBuilder(MigrationBuilder migrationBuilder, IDatabase database) : base(migrationBuilder, database)
        {
            EntityTableName = EntityTableNameValue;
            PrimaryKey = _primaryKey;
            ForeignKeys.Add(_moduleForeignKey);
        }

        protected override CrudDefinitionEntityBuilder BuildTable(ColumnsBuilder table)
        {
            CrudDefinitionId = AddAutoIncrementColumn(table, "CrudDefinitionId");
            ModuleId = AddIntegerColumn(table, "ModuleId");
            Name = AddMaxStringColumn(table, "Name");
            MenuTitle = AddMaxStringColumn(table, "MenuTitle");
            Icon = AddMaxStringColumn(table, "Icon");
            SourceSchema = AddMaxStringColumn(table, "SourceSchema");
            SourceTable = AddMaxStringColumn(table, "SourceTable");
            PrimaryKeyColumn = AddMaxStringColumn(table, "PrimaryKeyColumn");
            ConfigurationJson = AddMaxStringColumn(table, "ConfigurationJson");
            IsPublished = AddBooleanColumn(table, "IsPublished");
            AddAuditableColumns(table);
            return this;
        }

        public OperationBuilder<AddColumnOperation> CrudDefinitionId { get; set; }
        public OperationBuilder<AddColumnOperation> ModuleId { get; set; }
        public OperationBuilder<AddColumnOperation> Name { get; set; }
        public OperationBuilder<AddColumnOperation> MenuTitle { get; set; }
        public OperationBuilder<AddColumnOperation> Icon { get; set; }
        public OperationBuilder<AddColumnOperation> SourceSchema { get; set; }
        public OperationBuilder<AddColumnOperation> SourceTable { get; set; }
        public OperationBuilder<AddColumnOperation> PrimaryKeyColumn { get; set; }
        public OperationBuilder<AddColumnOperation> ConfigurationJson { get; set; }
        public OperationBuilder<AddColumnOperation> IsPublished { get; set; }
    }
}
