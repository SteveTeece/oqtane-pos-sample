using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Oqtane.Models;

namespace Yosh.uwa.Models
{
    public class PosStore : IAuditable
    {
        [Key] public int PosStoreId { get; set; }
        public int SiteId { get; set; }
        public int ModuleId { get; set; }
        [Required] public string Name { get; set; } = "Pour House";
        public string CurrencySymbol { get; set; } = "$";
        public int TaxRateBasisPoints { get; set; } = 850;
        public string ReceiptFooter { get; set; } = "Thanks for supporting local beer.";
        public bool IsOpen { get; set; } = true;
        public int NextOrderNumber { get; set; } = 1001;
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedOn { get; set; }
        public string ModifiedBy { get; set; } = string.Empty;
        public DateTime ModifiedOn { get; set; }
    }

    public class PosCategory : IAuditable
    {
        [Key] public int PosCategoryId { get; set; }
        public int PosStoreId { get; set; }
        [Required] public string Name { get; set; } = string.Empty;
        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedOn { get; set; }
        public string ModifiedBy { get; set; } = string.Empty;
        public DateTime ModifiedOn { get; set; }
    }

    public class PosProduct : IAuditable
    {
        [Key] public int PosProductId { get; set; }
        public int PosStoreId { get; set; }
        public int PosCategoryId { get; set; }
        [Required] public string Name { get; set; } = string.Empty;
        public string Brewery { get; set; } = string.Empty;
        public string Style { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Abv { get; set; } = string.Empty;
        public int PriceCents { get; set; }
        public string Format { get; set; } = "Pint";
        public string Sku { get; set; } = string.Empty;
        public string StockLabel { get; set; } = string.Empty;
        public string ImageDataUrl { get; set; } = string.Empty;
        public string Color { get; set; } = "gold";
        public bool TrackInventory { get; set; } = true;
        public int StockQuantity { get; set; }
        public int LowStockThreshold { get; set; } = 5;
        public bool IsActive { get; set; } = true;
        public int SortOrder { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedOn { get; set; }
        public string ModifiedBy { get; set; } = string.Empty;
        public DateTime ModifiedOn { get; set; }
    }

    public class PosOrder : IAuditable
    {
        [Key] public int PosOrderId { get; set; }
        public int PosStoreId { get; set; }
        public int OrderNumber { get; set; }
        public int SubtotalCents { get; set; }
        public int TaxCents { get; set; }
        public int TotalCents { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string Status { get; set; } = "Paid";
        public string CustomerName { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedOn { get; set; }
        public string ModifiedBy { get; set; } = string.Empty;
        public DateTime ModifiedOn { get; set; }
    }

    public class PosOrderLine : IAuditable
    {
        [Key] public int PosOrderLineId { get; set; }
        public int PosOrderId { get; set; }
        public int PosProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public int UnitPriceCents { get; set; }
        public int LineTotalCents { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedOn { get; set; }
        public string ModifiedBy { get; set; } = string.Empty;
        public DateTime ModifiedOn { get; set; }
    }

    public class PosCatalog
    {
        public PosStore Store { get; set; }
        public List<PosCategory> Categories { get; set; } = new();
        public List<PosProduct> Products { get; set; } = new();
        public List<PosOrder> RecentOrders { get; set; } = new();
        public PosSalesAnalytics Analytics { get; set; } = new();
        public bool CanManage { get; set; }
        public bool IsMasterAdmin { get; set; }
        public List<PosStoreSummary> MasterStores { get; set; } = new();
    }

    public class PosStoreSummary
    {
        public int PosStoreId { get; set; }
        public int SiteId { get; set; }
        public int ModuleId { get; set; }
        public string SiteName { get; set; } = string.Empty;
        public string StoreName { get; set; } = string.Empty;
        public string CurrencySymbol { get; set; } = "$";
        public bool IsOpen { get; set; }
        public int ProductCount { get; set; }
        public int LowStockProducts { get; set; }
        public int PaidOrders { get; set; }
        public int CancelledOrders { get; set; }
        public int RevenueCents { get; set; }
    }

    public class PosSalesAnalytics
    {
        public int TodayRevenueCents { get; set; }
        public int TodayOrders { get; set; }
        public int TotalRevenueCents { get; set; }
        public int TotalOrders { get; set; }
        public int AverageOrderCents { get; set; }
        public int LowStockProducts { get; set; }
        public List<PosSalesMetric> PaymentMethods { get; set; } = new();
        public List<PosProductPerformance> TopProducts { get; set; } = new();
    }

    public class PosSalesMetric
    {
        public string Label { get; set; } = string.Empty;
        public int Count { get; set; }
        public int AmountCents { get; set; }
    }

    public class PosProductPerformance
    {
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public int RevenueCents { get; set; }
    }

    public class PosCheckoutRequest
    {
        public int ModuleId { get; set; }
        public string PaymentMethod { get; set; }
        public string CustomerName { get; set; }
        public List<PosCheckoutLine> Lines { get; set; } = new();
    }

    public class PosCheckoutLine
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }

    public class PosReceipt
    {
        public bool Succeeded { get; set; }
        public string Message { get; set; }
        public int OrderId { get; set; }
        public int OrderNumber { get; set; }
        public int SubtotalCents { get; set; }
        public int TaxCents { get; set; }
        public int TotalCents { get; set; }
        public string PaymentMethod { get; set; }
        public DateTime CreatedOn { get; set; }
        public string StoreName { get; set; } = string.Empty;
        public string CurrencySymbol { get; set; } = "$";
        public string ReceiptFooter { get; set; } = string.Empty;
        public List<PosReceiptLine> Lines { get; set; } = new();
    }

    public class PosOperationResult
    {
        public bool Succeeded { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class PosReceiptLine
    {
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public int UnitPriceCents { get; set; }
        public int LineTotalCents { get; set; }
    }
}
