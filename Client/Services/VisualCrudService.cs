using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Oqtane.Services;
using Oqtane.Shared;
using Yosh.uwa.Models;

namespace Yosh.uwa.Services
{
    public interface IVisualCrudService
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
        Task<CrudOperationResult> UpdateDataAsync(CrudDataRequest request);
        Task<CrudOperationResult> DeleteDataAsync(CrudDataRequest request);
    }

    public class VisualCrudService : ServiceBase, IVisualCrudService
    {
        private readonly HttpClient _http;

        public VisualCrudService(HttpClient http, SiteState siteState) : base(http, siteState)
        {
            _http = http;
        }

        private string ApiUrl => CreateApiUrl("VisualCrud");
        private string Authorized(string url, int moduleId) => CreateAuthorizationPolicyUrl(url, EntityNames.Module, moduleId);

        public async Task<List<CrudDefinition>> GetDefinitionsAsync(int moduleId)
        {
            var definitions = await GetJsonAsync<List<CrudDefinition>>(Authorized($"{ApiUrl}?moduleid={moduleId}", moduleId), new());
            return definitions.OrderBy(item => item.MenuTitle).ThenBy(item => item.Name).ToList();
        }

        public Task<CrudDefinition> GetDefinitionAsync(int definitionId, int moduleId)
            => GetJsonAsync<CrudDefinition>(Authorized($"{ApiUrl}/definition/{definitionId}/{moduleId}", moduleId));

        public Task<CrudDefinition> AddDefinitionAsync(CrudDefinition definition)
            => PostJsonAsync<CrudDefinition>(Authorized(ApiUrl, definition.ModuleId), definition);

        public Task<CrudDefinition> UpdateDefinitionAsync(CrudDefinition definition)
            => PutJsonAsync<CrudDefinition>(Authorized($"{ApiUrl}/{definition.CrudDefinitionId}", definition.ModuleId), definition);

        public Task DeleteDefinitionAsync(int definitionId, int moduleId)
            => DeleteAsync(Authorized($"{ApiUrl}/{definitionId}/{moduleId}", moduleId));

        public Task<List<DatabaseTableInfo>> GetTablesAsync(int moduleId)
            => GetJsonAsync<List<DatabaseTableInfo>>(Authorized($"{ApiUrl}/schema/tables/{moduleId}", moduleId), new());

        public Task<List<DatabaseColumnInfo>> GetColumnsAsync(int moduleId, string schema, string table)
            => GetJsonAsync<List<DatabaseColumnInfo>>(Authorized($"{ApiUrl}/schema/columns/{moduleId}?schema={Uri.EscapeDataString(schema)}&table={Uri.EscapeDataString(table)}", moduleId), new());

        public Task<CrudDataResult> GetDataAsync(int definitionId, int moduleId)
            => GetJsonAsync<CrudDataResult>(Authorized($"{ApiUrl}/data/{definitionId}/{moduleId}", moduleId), new CrudDataResult());

        public Task<CrudOperationResult> InsertAsync(CrudDataRequest request)
            => SendAsync(HttpMethod.Post, Authorized($"{ApiUrl}/data", request.ModuleId), request);

        public Task<CrudOperationResult> UpdateDataAsync(CrudDataRequest request)
            => SendAsync(HttpMethod.Put, Authorized($"{ApiUrl}/data", request.ModuleId), request);

        public Task<CrudOperationResult> DeleteDataAsync(CrudDataRequest request)
            => SendAsync(HttpMethod.Post, Authorized($"{ApiUrl}/data/delete", request.ModuleId), request);

        private async Task<CrudOperationResult> SendAsync(HttpMethod method, string url, CrudDataRequest request)
        {
            using var message = new HttpRequestMessage(method, url) { Content = JsonContent.Create(request) };
            using var response = await _http.SendAsync(message);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<CrudOperationResult>() ?? CrudOperationResult.Failure("The server returned no result.");
        }
    }
}
