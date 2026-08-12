using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Oqtane.Models;

namespace Yosh.uwa.Models
{
    public class CrudDefinition : IAuditable
    {
        [Key]
        public int CrudDefinitionId { get; set; }
        public int ModuleId { get; set; }

        [Required]
        public string Name { get; set; }
        public string MenuTitle { get; set; }
        public string Icon { get; set; }
        public string SourceSchema { get; set; }
        public string SourceTable { get; set; }
        public string PrimaryKeyColumn { get; set; }
        public string ConfigurationJson { get; set; }
        public bool IsPublished { get; set; }

        public string CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public string ModifiedBy { get; set; }
        public DateTime ModifiedOn { get; set; }
    }

    public class CrudBuilderConfiguration
    {
        public List<CrudColumnDefinition> Columns { get; set; } = new();
        public List<CrudJoinDefinition> Joins { get; set; } = new();
        public string BusinessRuleKey { get; set; } = "default";
        public CrudPageSetRequirements PageSet { get; set; } = new();
        public List<GeneratedCrudPage> GeneratedPages { get; set; } = new();
    }

    public class CrudPageSetRequirements
    {
        public string BasePath { get; set; }
        public int? ParentPageId { get; set; }
        public string Pane { get; set; } = "Default";
        public bool GenerateCreate { get; set; } = true;
        public bool GenerateEdit { get; set; } = true;
        public bool GenerateDetails { get; set; } = true;
    }

    public class GeneratedCrudPage
    {
        public string Role { get; set; }
        public string Path { get; set; }
        public int PageId { get; set; }
        public int ModuleId { get; set; }
    }

    public class CrudPageGenerationResult
    {
        public bool Succeeded { get; set; }
        public string Message { get; set; }
        public string ListPath { get; set; }
        public List<GeneratedCrudPage> Pages { get; set; } = new();
    }

    public class CrudColumnDefinition
    {
        public string Schema { get; set; }
        public string Table { get; set; }
        public string Column { get; set; }
        public string Label { get; set; }
        public bool IsVisible { get; set; } = true;
        public bool IsEditable { get; set; } = true;
        public bool IsRequired { get; set; }
        public int SortOrder { get; set; }
        public string DataKey => $"{Table}.{Column}";
    }

    public class CrudJoinDefinition
    {
        public string JoinType { get; set; } = "LEFT";
        public string Schema { get; set; }
        public string Table { get; set; }
        public string LeftColumn { get; set; }
        public string RightColumn { get; set; }
    }

    public class DatabaseTableInfo
    {
        public string Schema { get; set; }
        public string Name { get; set; }
        public string DisplayName => string.IsNullOrWhiteSpace(Schema) ? Name : $"{Schema}.{Name}";
    }

    public class DatabaseColumnInfo
    {
        public string Schema { get; set; }
        public string Table { get; set; }
        public string Name { get; set; }
        public string DataType { get; set; }
        public bool IsNullable { get; set; }
        public bool IsPrimaryKey { get; set; }
        public bool IsIdentity { get; set; }
        public int Ordinal { get; set; }
    }

    public class CrudDataResult
    {
        public List<CrudColumnDefinition> Columns { get; set; } = new();
        public List<Dictionary<string, string>> Rows { get; set; } = new();
    }

    public class CrudDataRequest
    {
        public int ModuleId { get; set; }
        public int DefinitionId { get; set; }
        public string KeyValue { get; set; }
        public Dictionary<string, string> Values { get; set; } = new();
    }

    public class CrudOperationResult
    {
        public bool Succeeded { get; set; }
        public string Message { get; set; }

        public static CrudOperationResult Success(string message = null) => new() { Succeeded = true, Message = message };
        public static CrudOperationResult Failure(string message) => new() { Succeeded = false, Message = message };
    }
}
