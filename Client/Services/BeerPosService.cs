using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Oqtane.Services;
using Oqtane.Shared;
using Yosh.uwa.Models;

namespace Yosh.uwa.Services
{
    public interface IBeerPosService
    {
        Task<PosCatalog> GetCatalogAsync(int moduleId);
        Task<PosStore> SaveStoreAsync(int moduleId, PosStore store);
        Task<PosCategory> SaveCategoryAsync(int moduleId, PosCategory category);
        Task<PosProduct> SaveProductAsync(int moduleId, PosProduct product);
        Task DeleteProductAsync(int moduleId, int productId);
        Task<PosReceipt> CheckoutAsync(PosCheckoutRequest request);
        Task<PosReceipt> GetReceiptAsync(int moduleId, int orderId);
        Task<PosOperationResult> CancelOrderAsync(int moduleId, int orderId);
        string GetSalesExportUrl(int moduleId);
    }

    public class BeerPosService : ServiceBase, IBeerPosService
    {
        private readonly HttpClient _http;
        public BeerPosService(HttpClient http, SiteState siteState) : base(http, siteState) => _http = http;
        private string ApiUrl => CreateApiUrl("BeerPos");
        private string Authorized(string url, int moduleId) => CreateAuthorizationPolicyUrl(url, EntityNames.Module, moduleId);
        public Task<PosCatalog> GetCatalogAsync(int moduleId) => GetJsonAsync<PosCatalog>(Authorized($"{ApiUrl}?moduleid={moduleId}", moduleId), new PosCatalog());
        public Task<PosStore> SaveStoreAsync(int moduleId, PosStore store) => PutJsonAsync<PosStore>(Authorized($"{ApiUrl}/store/{moduleId}", moduleId), store);
        public Task<PosCategory> SaveCategoryAsync(int moduleId, PosCategory category) => PostJsonAsync<PosCategory>(Authorized($"{ApiUrl}/category/{moduleId}", moduleId), category);
        public Task<PosProduct> SaveProductAsync(int moduleId, PosProduct product) => PostJsonAsync<PosProduct>(Authorized($"{ApiUrl}/product/{moduleId}", moduleId), product);
        public Task DeleteProductAsync(int moduleId, int productId) => DeleteAsync(Authorized($"{ApiUrl}/product/{productId}/{moduleId}", moduleId));
        public async Task<PosReceipt> CheckoutAsync(PosCheckoutRequest request)
        {
            using var response = await _http.PostAsJsonAsync(Authorized($"{ApiUrl}/checkout/{request.ModuleId}", request.ModuleId), request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<PosReceipt>() ?? new PosReceipt { Succeeded = false, Message = "The server returned no receipt." };
        }
        public Task<PosReceipt> GetReceiptAsync(int moduleId, int orderId) => GetJsonAsync<PosReceipt>(Authorized($"{ApiUrl}/receipt/{orderId}/{moduleId}", moduleId), new PosReceipt { Succeeded = false, Message = "Sale not found." });
        public async Task<PosOperationResult> CancelOrderAsync(int moduleId, int orderId)
        {
            using var response = await _http.PostAsJsonAsync(Authorized($"{ApiUrl}/cancel/{orderId}/{moduleId}", moduleId), new { });
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<PosOperationResult>() ?? new PosOperationResult { Succeeded = false, Message = "The server returned no result." };
        }
        public string GetSalesExportUrl(int moduleId) => Authorized($"{ApiUrl}/sales/{moduleId}/csv", moduleId);
    }
}
