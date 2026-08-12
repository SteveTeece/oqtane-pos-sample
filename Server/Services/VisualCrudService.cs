using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Oqtane.Enums;
using Oqtane.Infrastructure;
using Oqtane.Models;
using Oqtane.Security;
using Oqtane.Shared;
using Yosh.uwa.Models;
using Yosh.uwa.Repository;

namespace Yosh.uwa.Services
{
    public interface IServerVisualCrudService
    {
        Task<List<CrudDefinition>> GetDefinitionsAsync(int moduleId);
        Task<CrudDefinition> GetDefinitionAsync(int definitionId, int moduleId);
        Task<CrudDefinition> AddDefinitionAsync(CrudDefinition definition);
        Task<CrudDefinition> UpdateDefinitionAsync(CrudDefinition definition);
        Task DeleteDefinitionAsync(int definitionId, int moduleId);
        Task<List<DatabaseTableInfo>> GetTablesAsync(int moduleId);
        Task<List<DatabaseColumnInfo>> GetColumnsAsync(int moduleId, string schema, string table);
        Task<CrudDataResult> GetDataAsync(int definitionId, int moduleId);
        Task<CrudOperationResult> InsertAsync(CrudDataRequest request);
        Task<CrudOperationResult> UpdateAsync(CrudDataRequest request);
        Task<CrudOperationResult> DeleteAsync(CrudDataRequest request);
    }

    public class ServerVisualCrudService : IServerVisualCrudService
    {
        private readonly ICrudDefinitionRepository _definitions;
        private readonly IDatabaseSchemaProvider _schema;
        private readonly IDynamicCrudService _dynamicCrud;
        private readonly IUserPermissions _permissions;
        private readonly ILogManager _logger;
        private readonly IHttpContextAccessor _accessor;
        private readonly Alias _alias;

        public ServerVisualCrudService(ICrudDefinitionRepository definitions, IDatabaseSchemaProvider schema, IDynamicCrudService dynamicCrud, IUserPermissions permissions, ITenantManager tenantManager, ILogManager logger, IHttpContextAccessor accessor)
        {
            _definitions = definitions;
            _schema = schema;
            _dynamicCrud = dynamicCrud;
            _permissions = permissions;
            _logger = logger;
            _accessor = accessor;
            _alias = tenantManager.GetAlias();
        }

        public Task<List<CrudDefinition>> GetDefinitionsAsync(int moduleId)
            => CanView(moduleId) ? Task.FromResult(_definitions.GetDefinitions(moduleId).ToList()) : Task.FromResult<List<CrudDefinition>>(null);

        public Task<CrudDefinition> GetDefinitionAsync(int definitionId, int moduleId)
        {
            var definition = CanView(moduleId) ? _definitions.GetDefinition(definitionId, false) : null;
            return Task.FromResult(IsDefinitionModule(definition, moduleId) ? definition : null);
        }

        public Task<CrudDefinition> AddDefinitionAsync(CrudDefinition definition)
        {
            var saved = _definitions.AddDefinition(definition);
            _logger.Log(LogLevel.Information, this, LogFunction.Create, "Visual CRUD definition added {DefinitionId}", saved.CrudDefinitionId);
            return Task.FromResult(saved);
        }

        public Task<CrudDefinition> UpdateDefinitionAsync(CrudDefinition definition)
        {
            var existing = _definitions.GetDefinition(definition.CrudDefinitionId, false);
            if (existing == null || existing.ModuleId != definition.ModuleId)
            {
                return Task.FromResult<CrudDefinition>(null);
            }
            var saved = _definitions.UpdateDefinition(definition);
            _logger.Log(LogLevel.Information, this, LogFunction.Update, "Visual CRUD definition updated {DefinitionId}", saved.CrudDefinitionId);
            return Task.FromResult(saved);
        }

        public Task DeleteDefinitionAsync(int definitionId, int moduleId)
        {
            var existing = _definitions.GetDefinition(definitionId, false);
            if (CanEdit(moduleId) && existing?.ModuleId == moduleId)
            {
                _definitions.DeleteDefinition(definitionId);
                _logger.Log(LogLevel.Information, this, LogFunction.Delete, "Visual CRUD definition deleted {DefinitionId}", definitionId);
            }
            return Task.CompletedTask;
        }

        public Task<List<DatabaseTableInfo>> GetTablesAsync(int moduleId)
            => CanEdit(moduleId) ? _schema.GetTablesAsync() : Task.FromResult<List<DatabaseTableInfo>>(null);

        public Task<List<DatabaseColumnInfo>> GetColumnsAsync(int moduleId, string schema, string table)
            => CanEdit(moduleId) ? _schema.GetColumnsAsync(schema, table) : Task.FromResult<List<DatabaseColumnInfo>>(null);

        public Task<CrudDataResult> GetDataAsync(int definitionId, int moduleId)
            => CanView(moduleId) ? _dynamicCrud.GetDataAsync(definitionId, moduleId) : Task.FromResult<CrudDataResult>(null);

        public Task<CrudOperationResult> InsertAsync(CrudDataRequest request)
            => CanEdit(request.ModuleId) ? _dynamicCrud.InsertAsync(request) : Task.FromResult(CrudOperationResult.Failure("Not authorized."));

        public Task<CrudOperationResult> UpdateAsync(CrudDataRequest request)
            => CanEdit(request.ModuleId) ? _dynamicCrud.UpdateAsync(request) : Task.FromResult(CrudOperationResult.Failure("Not authorized."));

        public Task<CrudOperationResult> DeleteAsync(CrudDataRequest request)
            => CanEdit(request.ModuleId) ? _dynamicCrud.DeleteAsync(request) : Task.FromResult(CrudOperationResult.Failure("Not authorized."));

        private bool CanView(int moduleId) => IsAuthorized(moduleId, PermissionNames.View);
        private bool CanEdit(int moduleId) => IsAuthorized(moduleId, PermissionNames.Edit);

        private static bool IsDefinitionModule(CrudDefinition definition, int moduleId)
        {
            if (definition == null) return false;
            if (definition.ModuleId == moduleId) return true;
            var configuration = string.IsNullOrWhiteSpace(definition.ConfigurationJson)
                ? new CrudBuilderConfiguration()
                : JsonSerializer.Deserialize<CrudBuilderConfiguration>(definition.ConfigurationJson) ?? new();
            return configuration.GeneratedPages.Any(page => page.ModuleId == moduleId);
        }

        private bool IsAuthorized(int moduleId, string permission)
        {
            var authorized = _permissions.IsAuthorized(_accessor.HttpContext.User, _alias.SiteId, EntityNames.Module, moduleId, permission);
            if (!authorized)
            {
                _logger.Log(LogLevel.Error, this, LogFunction.Security, "Unauthorized Visual CRUD attempt {ModuleId} {Permission}", moduleId, permission);
            }
            return authorized;
        }
    }
}
