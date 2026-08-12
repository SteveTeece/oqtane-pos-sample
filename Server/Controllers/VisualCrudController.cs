using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Oqtane.Controllers;
using Oqtane.Enums;
using Oqtane.Infrastructure;
using Oqtane.Shared;
using Yosh.uwa.Models;
using Yosh.uwa.Services;

namespace Yosh.uwa.Controllers
{
    [Route(ControllerRoutes.ApiRoute)]
    public class VisualCrudController : ModuleControllerBase
    {
        private readonly IServerVisualCrudService _service;

        public VisualCrudController(IServerVisualCrudService service, ILogManager logger, IHttpContextAccessor accessor) : base(logger, accessor)
        {
            _service = service;
        }

        [HttpGet]
        [Authorize(Policy = PolicyNames.ViewModule)]
        public async Task<IEnumerable<CrudDefinition>> Get(string moduleid)
        {
            if (!Authorized(moduleid, out var moduleId)) return null;
            return await _service.GetDefinitionsAsync(moduleId);
        }

        [HttpGet("definition/{id}/{moduleid}")]
        [Authorize(Policy = PolicyNames.ViewModule)]
        public async Task<CrudDefinition> GetDefinition(int id, int moduleid)
            => await _service.GetDefinitionAsync(id, moduleid);

        [HttpPost]
        [Authorize(Policy = PolicyNames.EditModule)]
        public async Task<CrudDefinition> Post([FromBody] CrudDefinition definition, [FromQuery(Name = "authmoduleid")] int authorizedModuleId)
        {
            if (!ValidDefinition(definition))
            {
                HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return null;
            }
            if (definition.ModuleId != authorizedModuleId)
            {
                HttpContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                return null;
            }
            return await _service.AddDefinitionAsync(definition);
        }

        [HttpPut("{id}")]
        [Authorize(Policy = PolicyNames.EditModule)]
        public async Task<CrudDefinition> Put(int id, [FromBody] CrudDefinition definition, [FromQuery(Name = "authmoduleid")] int authorizedModuleId)
        {
            if (!ValidDefinition(definition) || id != definition.CrudDefinitionId)
            {
                HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return null;
            }
            if (definition.ModuleId != authorizedModuleId)
            {
                HttpContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                return null;
            }
            return await _service.UpdateDefinitionAsync(definition);
        }

        [HttpDelete("{id}/{moduleid}")]
        [Authorize(Policy = PolicyNames.EditModule)]
        public async Task Delete(int id, int moduleid)
        {
            if (!IsAuthorizedEntityId(EntityNames.Module, moduleid))
            {
                HttpContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                return;
            }
            await _service.DeleteDefinitionAsync(id, moduleid);
        }

        [HttpGet("schema/tables/{moduleid}")]
        [Authorize(Policy = PolicyNames.EditModule)]
        public async Task<List<DatabaseTableInfo>> GetTables(int moduleid)
            => await _service.GetTablesAsync(moduleid);

        [HttpGet("schema/columns/{moduleid}")]
        [Authorize(Policy = PolicyNames.EditModule)]
        public async Task<List<DatabaseColumnInfo>> GetColumns(int moduleid, string schema, string table)
            => await _service.GetColumnsAsync(moduleid, schema, table);

        [HttpGet("data/{id}/{moduleid}")]
        [Authorize(Policy = PolicyNames.ViewModule)]
        public async Task<CrudDataResult> GetData(int id, int moduleid)
            => await _service.GetDataAsync(id, moduleid);

        [HttpPost("data")]
        [Authorize(Policy = PolicyNames.EditModule)]
        public Task<CrudOperationResult> Insert([FromBody] CrudDataRequest request) => _service.InsertAsync(request);

        [HttpPut("data")]
        [Authorize(Policy = PolicyNames.EditModule)]
        public Task<CrudOperationResult> UpdateData([FromBody] CrudDataRequest request) => _service.UpdateAsync(request);

        [HttpPost("data/delete")]
        [Authorize(Policy = PolicyNames.EditModule)]
        public Task<CrudOperationResult> DeleteData([FromBody] CrudDataRequest request) => _service.DeleteAsync(request);

        private bool Authorized(string moduleIdValue, out int moduleId)
        {
            if (int.TryParse(moduleIdValue, out moduleId) && IsAuthorizedEntityId(EntityNames.Module, moduleId)) return true;
            HttpContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            return false;
        }

        private static bool ValidDefinition(CrudDefinition definition)
            => definition != null
                && !string.IsNullOrWhiteSpace(definition.Name)
                && !string.IsNullOrWhiteSpace(definition.SourceSchema)
                && !string.IsNullOrWhiteSpace(definition.SourceTable)
                && !string.IsNullOrWhiteSpace(definition.PrimaryKeyColumn);
    }
}
