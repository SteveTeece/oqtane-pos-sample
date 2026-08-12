using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using Oqtane.Controllers;
using Oqtane.Enums;
using Oqtane.Infrastructure;
using Oqtane.Shared;
using Yosh.uwa.Models;
using Yosh.uwa.Services;

namespace Yosh.uwa.Controllers
{
    [Route(ControllerRoutes.ApiRoute)]
    public class BeerPosController : ModuleControllerBase
    {
        private readonly IServerBeerPosService _service;
        public BeerPosController(IServerBeerPosService service, ILogManager logger, IHttpContextAccessor accessor) : base(logger, accessor) => _service = service;

        [HttpGet]
        [Authorize(Policy = PolicyNames.ViewModule)]
        public PosCatalog Get(int moduleid) => _service.GetCatalog(moduleid);

        [HttpPut("store/{moduleid}")]
        [Authorize(Policy = PolicyNames.EditModule)]
        public PosStore PutStore(int moduleid, [FromBody] PosStore store) => _service.SaveStore(moduleid, store);

        [HttpPost("category/{moduleid}")]
        [Authorize(Policy = PolicyNames.EditModule)]
        public PosCategory PostCategory(int moduleid, [FromBody] PosCategory category) => _service.SaveCategory(moduleid, category);

        [HttpPost("product/{moduleid}")]
        [Authorize(Policy = PolicyNames.EditModule)]
        public PosProduct PostProduct(int moduleid, [FromBody] PosProduct product) => _service.SaveProduct(moduleid, product);

        [HttpDelete("product/{productid}/{moduleid}")]
        [Authorize(Policy = PolicyNames.EditModule)]
        public bool DeleteProduct(int productid, int moduleid) => _service.DeleteProduct(moduleid, productid);

        [HttpPost("checkout/{moduleid}")]
        [Authorize(Policy = PolicyNames.ViewModule)]
        public PosReceipt Checkout(int moduleid, [FromBody] PosCheckoutRequest request)
        {
            if (request == null || request.ModuleId != moduleid) return new PosReceipt { Succeeded = false, Message = "Invalid checkout request." };
            return _service.Checkout(moduleid, request);
        }

        [HttpGet("receipt/{orderid}/{moduleid}")]
        [Authorize(Policy = PolicyNames.EditModule)]
        public PosReceipt GetReceipt(int orderid, int moduleid) => _service.GetReceipt(moduleid, orderid);

        [HttpPost("cancel/{orderid}/{moduleid}")]
        [Authorize(Policy = PolicyNames.EditModule)]
        public PosOperationResult CancelOrder(int orderid, int moduleid) => _service.CancelOrder(moduleid, orderid);

        [HttpGet("sales/{moduleid}/csv")]
        [Authorize(Policy = PolicyNames.EditModule)]
        public FileContentResult ExportSales(int moduleid) => File(Encoding.UTF8.GetBytes(_service.GetSalesCsv(moduleid)), "text/csv", $"pos-sales-{System.DateTime.UtcNow:yyyyMMdd-HHmm}.csv");
    }
}
