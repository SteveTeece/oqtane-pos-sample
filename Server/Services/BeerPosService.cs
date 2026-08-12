using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Http;
using Oqtane.Infrastructure;
using Oqtane.Models;
using Oqtane.Repository;
using Oqtane.Security;
using Oqtane.Shared;
using Yosh.uwa.Models;
using Yosh.uwa.Repository;

namespace Yosh.uwa.Services
{
    public interface IServerBeerPosService
    {
        PosCatalog GetCatalog(int moduleId);
        PosStore SaveStore(int moduleId, PosStore store);
        PosCategory SaveCategory(int moduleId, PosCategory category);
        PosProduct SaveProduct(int moduleId, PosProduct product);
        bool DeleteProduct(int moduleId, int productId);
        PosReceipt Checkout(int moduleId, PosCheckoutRequest request);
        PosReceipt GetReceipt(int moduleId, int orderId);
        PosOperationResult CancelOrder(int moduleId, int orderId);
        string GetSalesCsv(int moduleId);
    }

    public class ServerBeerPosService : IServerBeerPosService
    {
        private readonly IBeerPosRepository _repository;
        private readonly IUserPermissions _permissions;
        private readonly IHttpContextAccessor _accessor;
        private readonly ISiteRepository _sites;
        private readonly Alias _alias;
        public ServerBeerPosService(IBeerPosRepository repository, IUserPermissions permissions, IHttpContextAccessor accessor, ITenantManager tenantManager, ISiteRepository sites)
        {
            _repository = repository;
            _permissions = permissions;
            _accessor = accessor;
            _sites = sites;
            _alias = tenantManager.GetAlias();
        }
        public PosCatalog GetCatalog(int moduleId)
        {
            var catalog = _repository.GetCatalog(_alias.SiteId, moduleId, CanEdit(moduleId));
            catalog.IsMasterAdmin = IsMasterAdmin();
            if (catalog.IsMasterAdmin)
            {
                var siteNames = _sites.GetSites().ToDictionary(x => x.SiteId, x => x.Name);
                catalog.MasterStores = _repository.GetStoreSummaries();
                foreach (var store in catalog.MasterStores) store.SiteName = siteNames.TryGetValue(store.SiteId, out var siteName) ? siteName : $"Site {store.SiteId}";
            }
            return catalog;
        }
        public PosStore SaveStore(int moduleId, PosStore store) => CanEdit(moduleId) ? _repository.SaveStore(_alias.SiteId, moduleId, store) : null;
        public PosCategory SaveCategory(int moduleId, PosCategory category) => CanEdit(moduleId) ? _repository.SaveCategory(_alias.SiteId, moduleId, category) : null;
        public PosProduct SaveProduct(int moduleId, PosProduct product) => CanEdit(moduleId) ? _repository.SaveProduct(_alias.SiteId, moduleId, product) : null;
        public bool DeleteProduct(int moduleId, int productId) => CanEdit(moduleId) && _repository.DeleteProduct(_alias.SiteId, moduleId, productId);
        public PosReceipt Checkout(int moduleId, PosCheckoutRequest request) => _repository.Checkout(_alias.SiteId, moduleId, request);
        public PosReceipt GetReceipt(int moduleId, int orderId) => CanEdit(moduleId) ? _repository.GetReceipt(_alias.SiteId, moduleId, orderId) : new PosReceipt { Succeeded = false, Message = "Not authorized." };
        public PosOperationResult CancelOrder(int moduleId, int orderId) => CanEdit(moduleId) ? _repository.CancelOrder(_alias.SiteId, moduleId, orderId) : new PosOperationResult { Succeeded = false, Message = "Not authorized." };
        public string GetSalesCsv(int moduleId)
        {
            if (!CanEdit(moduleId)) return string.Empty;
            var rows = _repository.GetOrders(_alias.SiteId, moduleId);
            var csv = new StringBuilder("Order,Date (UTC),Customer,Payment,Status,Subtotal,Tax,Total\r\n");
            foreach (var order in rows)
                csv.Append(order.OrderNumber).Append(',').Append(order.CreatedOn.ToString("O", CultureInfo.InvariantCulture)).Append(',')
                    .Append(Csv(order.CustomerName)).Append(',').Append(Csv(order.PaymentMethod)).Append(',').Append(Csv(order.Status)).Append(',')
                    .Append((order.SubtotalCents / 100m).ToString("0.00", CultureInfo.InvariantCulture)).Append(',')
                    .Append((order.TaxCents / 100m).ToString("0.00", CultureInfo.InvariantCulture)).Append(',')
                    .Append((order.TotalCents / 100m).ToString("0.00", CultureInfo.InvariantCulture)).Append("\r\n");
            return csv.ToString();
        }
        private bool CanEdit(int moduleId) => _permissions.IsAuthorized(_accessor.HttpContext.User, _alias.SiteId, EntityNames.Module, moduleId, PermissionNames.Edit);
        private bool IsMasterAdmin() => _accessor.HttpContext?.User?.IsInRole("Host Users") == true;
        private static string Csv(string value) => $"\"{(value ?? string.Empty).Replace("\"", "\"\"")}\"";
    }
}
