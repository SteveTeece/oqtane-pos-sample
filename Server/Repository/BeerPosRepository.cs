using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Oqtane.Modules;
using Yosh.uwa.Models;

namespace Yosh.uwa.Repository
{
    public interface IBeerPosRepository
    {
        PosCatalog GetCatalog(int siteId, int moduleId, bool canManage);
        PosStore SaveStore(int siteId, int moduleId, PosStore store);
        PosCategory SaveCategory(int siteId, int moduleId, PosCategory category);
        PosProduct SaveProduct(int siteId, int moduleId, PosProduct product);
        bool DeleteProduct(int siteId, int moduleId, int productId);
        PosReceipt Checkout(int siteId, int moduleId, PosCheckoutRequest request);
        PosReceipt GetReceipt(int siteId, int moduleId, int orderId);
        PosOperationResult CancelOrder(int siteId, int moduleId, int orderId);
        List<PosOrder> GetOrders(int siteId, int moduleId);
        List<PosStoreSummary> GetStoreSummaries();
    }

    public class BeerPosRepository : IBeerPosRepository, ITransientService
    {
        private readonly IDbContextFactory<Context> _factory;
        public BeerPosRepository(IDbContextFactory<Context> factory) => _factory = factory;

        public PosCatalog GetCatalog(int siteId, int moduleId, bool canManage)
        {
            using var db = _factory.CreateDbContext();
            var store = EnsureStore(db, siteId, moduleId);
            var products = db.PosProducts.AsNoTracking().Where(x => x.PosStoreId == store.PosStoreId && x.IsActive).OrderBy(x => x.SortOrder).ThenBy(x => x.Name).ToList();
            var orders = db.PosOrders.AsNoTracking().Where(x => x.PosStoreId == store.PosStoreId).OrderByDescending(x => x.PosOrderId).ToList();
            var paidOrders = orders.Where(x => x.Status == "Paid").ToList();
            var orderIds = paidOrders.Select(x => x.PosOrderId).ToList();
            var lines = orderIds.Count == 0 ? new List<PosOrderLine>() : db.PosOrderLines.AsNoTracking().Where(x => orderIds.Contains(x.PosOrderId)).ToList();
            var today = DateTime.UtcNow.Date;
            return new PosCatalog
            {
                Store = store,
                Categories = db.PosCategories.AsNoTracking().Where(x => x.PosStoreId == store.PosStoreId && x.IsActive).OrderBy(x => x.SortOrder).ThenBy(x => x.Name).ToList(),
                Products = products,
                RecentOrders = orders.Take(200).ToList(),
                Analytics = new PosSalesAnalytics
                {
                    TodayRevenueCents = paidOrders.Where(x => x.CreatedOn.Date == today).Sum(x => x.TotalCents),
                    TodayOrders = paidOrders.Count(x => x.CreatedOn.Date == today),
                    TotalRevenueCents = paidOrders.Sum(x => x.TotalCents),
                    TotalOrders = paidOrders.Count,
                    AverageOrderCents = paidOrders.Count == 0 ? 0 : (int)Math.Round(paidOrders.Average(x => x.TotalCents)),
                    LowStockProducts = products.Count(x => x.TrackInventory && x.StockQuantity <= x.LowStockThreshold),
                    PaymentMethods = paidOrders.GroupBy(x => x.PaymentMethod).Select(x => new PosSalesMetric { Label = x.Key, Count = x.Count(), AmountCents = x.Sum(y => y.TotalCents) }).OrderByDescending(x => x.AmountCents).ToList(),
                    TopProducts = lines.GroupBy(x => x.ProductName).Select(x => new PosProductPerformance { ProductName = x.Key, Quantity = x.Sum(y => y.Quantity), RevenueCents = x.Sum(y => y.LineTotalCents) }).OrderByDescending(x => x.Quantity).ThenByDescending(x => x.RevenueCents).Take(5).ToList()
                },
                CanManage = canManage
            };
        }

        public PosStore SaveStore(int siteId, int moduleId, PosStore input)
        {
            using var db = _factory.CreateDbContext();
            var store = EnsureStore(db, siteId, moduleId);
            store.Name = Clean(input.Name, "My Beer POS", 100);
            store.CurrencySymbol = Clean(input.CurrencySymbol, "$", 5);
            store.TaxRateBasisPoints = Math.Clamp(input.TaxRateBasisPoints, 0, 10000);
            store.ReceiptFooter = Clean(input.ReceiptFooter, string.Empty, 500);
            store.IsOpen = input.IsOpen;
            db.SaveChanges();
            return store;
        }

        public PosCategory SaveCategory(int siteId, int moduleId, PosCategory input)
        {
            using var db = _factory.CreateDbContext();
            var store = EnsureStore(db, siteId, moduleId);
            var category = input.PosCategoryId == 0 ? new PosCategory { PosStoreId = store.PosStoreId } :
                db.PosCategories.FirstOrDefault(x => x.PosCategoryId == input.PosCategoryId && x.PosStoreId == store.PosStoreId);
            if (category == null) return null;
            category.Name = Clean(input.Name, "Category", 80);
            category.SortOrder = input.SortOrder;
            category.IsActive = input.IsActive;
            if (category.PosCategoryId == 0) db.PosCategories.Add(category);
            db.SaveChanges();
            return category;
        }

        public PosProduct SaveProduct(int siteId, int moduleId, PosProduct input)
        {
            using var db = _factory.CreateDbContext();
            var store = EnsureStore(db, siteId, moduleId);
            if (!db.PosCategories.Any(x => x.PosCategoryId == input.PosCategoryId && x.PosStoreId == store.PosStoreId && x.IsActive))
                input.PosCategoryId = db.PosCategories.Where(x => x.PosStoreId == store.PosStoreId && x.IsActive).OrderBy(x => x.SortOrder).Select(x => x.PosCategoryId).FirstOrDefault();
            if (input.PosCategoryId == 0) return null;
            var product = input.PosProductId == 0 ? new PosProduct { PosStoreId = store.PosStoreId } :
                db.PosProducts.FirstOrDefault(x => x.PosProductId == input.PosProductId && x.PosStoreId == store.PosStoreId);
            if (product == null) return null;
            product.PosCategoryId = input.PosCategoryId;
            product.Name = Clean(input.Name, "New product", 120);
            product.Brewery = Clean(input.Brewery, string.Empty, 120);
            product.Style = Clean(input.Style, string.Empty, 80);
            product.Description = Clean(input.Description, string.Empty, 500);
            product.Abv = Clean(input.Abv, string.Empty, 20);
            product.PriceCents = Math.Clamp(input.PriceCents, 0, 100000000);
            product.Format = Clean(input.Format, "Pint", 40);
            product.Sku = Clean(input.Sku, string.Empty, 80);
            product.StockLabel = Clean(input.StockLabel, string.Empty, 80);
            product.Color = Clean(input.Color, "gold", 30);
            product.ImageDataUrl = ValidImage(input.ImageDataUrl) ? input.ImageDataUrl ?? string.Empty : product.ImageDataUrl ?? string.Empty;
            product.IsActive = input.IsActive;
            product.SortOrder = input.SortOrder;
            product.TrackInventory = input.TrackInventory;
            product.StockQuantity = Math.Max(0, input.StockQuantity);
            product.LowStockThreshold = Math.Max(0, input.LowStockThreshold);
            if (product.PosProductId == 0) db.PosProducts.Add(product);
            db.SaveChanges();
            return product;
        }

        public bool DeleteProduct(int siteId, int moduleId, int productId)
        {
            using var db = _factory.CreateDbContext();
            var store = db.PosStores.AsNoTracking().FirstOrDefault(x => x.SiteId == siteId && x.ModuleId == moduleId);
            var product = store == null ? null : db.PosProducts.FirstOrDefault(x => x.PosProductId == productId && x.PosStoreId == store.PosStoreId);
            if (product == null) return false;
            product.IsActive = false;
            db.SaveChanges();
            return true;
        }

        public PosReceipt Checkout(int siteId, int moduleId, PosCheckoutRequest request)
        {
            using var db = _factory.CreateDbContext();
            using var transaction = db.Database.BeginTransaction();
            var store = db.PosStores.FirstOrDefault(x => x.SiteId == siteId && x.ModuleId == moduleId);
            if (store == null || !store.IsOpen) return Failure("This POS is currently closed.");
            var requested = request.Lines.Where(x => x.Quantity > 0).GroupBy(x => x.ProductId).ToDictionary(x => x.Key, x => Math.Min(99, x.Sum(y => y.Quantity)));
            if (requested.Count == 0) return Failure("Add at least one product.");
            var products = db.PosProducts.Where(x => x.PosStoreId == store.PosStoreId && x.IsActive && requested.Keys.Contains(x.PosProductId)).ToList();
            if (products.Count != requested.Count) return Failure("One or more products are unavailable.");
            var unavailable = products.FirstOrDefault(x => x.TrackInventory && requested[x.PosProductId] > x.StockQuantity);
            if (unavailable != null) return Failure($"Only {unavailable.StockQuantity} × {unavailable.Name} remain in stock.");

            var subtotal = products.Sum(x => x.PriceCents * requested[x.PosProductId]);
            var tax = (int)Math.Round(subtotal * (store.TaxRateBasisPoints / 10000m), MidpointRounding.AwayFromZero);
            var order = new PosOrder
            {
                PosStoreId = store.PosStoreId,
                OrderNumber = store.NextOrderNumber,
                SubtotalCents = subtotal,
                TaxCents = tax,
                TotalCents = subtotal + tax,
                PaymentMethod = AllowedPayment(request.PaymentMethod),
                Status = "Paid",
                CustomerName = Clean(request.CustomerName, string.Empty, 120)
            };
            store.NextOrderNumber++;
            db.PosOrders.Add(order);
            db.SaveChanges();
            foreach (var product in products)
            {
                var quantity = requested[product.PosProductId];
                if (product.TrackInventory) product.StockQuantity -= quantity;
                db.PosOrderLines.Add(new PosOrderLine
                {
                    PosOrderId = order.PosOrderId,
                    PosProductId = product.PosProductId,
                    ProductName = product.Name,
                    Quantity = quantity,
                    UnitPriceCents = product.PriceCents,
                    LineTotalCents = product.PriceCents * quantity
                });
            }
            db.SaveChanges();
            transaction.Commit();
            return new PosReceipt
            {
                Succeeded = true, Message = "Payment recorded.", OrderId = order.PosOrderId, OrderNumber = order.OrderNumber,
                SubtotalCents = order.SubtotalCents, TaxCents = order.TaxCents, TotalCents = order.TotalCents,
                PaymentMethod = order.PaymentMethod, CreatedOn = order.CreatedOn, StoreName = store.Name,
                CurrencySymbol = store.CurrencySymbol, ReceiptFooter = store.ReceiptFooter,
                Lines = products.Select(x => new PosReceiptLine { ProductName = x.Name, Quantity = requested[x.PosProductId], UnitPriceCents = x.PriceCents, LineTotalCents = x.PriceCents * requested[x.PosProductId] }).ToList()
            };
        }

        public PosReceipt GetReceipt(int siteId, int moduleId, int orderId)
        {
            using var db = _factory.CreateDbContext();
            var store = db.PosStores.AsNoTracking().FirstOrDefault(x => x.SiteId == siteId && x.ModuleId == moduleId);
            var order = store == null ? null : db.PosOrders.AsNoTracking().FirstOrDefault(x => x.PosOrderId == orderId && x.PosStoreId == store.PosStoreId);
            if (store == null || order == null) return Failure("Sale not found.");
            var lines = db.PosOrderLines.AsNoTracking().Where(x => x.PosOrderId == order.PosOrderId).OrderBy(x => x.PosOrderLineId).ToList();
            return new PosReceipt
            {
                Succeeded = true, Message = order.Status, OrderId = order.PosOrderId, OrderNumber = order.OrderNumber,
                SubtotalCents = order.SubtotalCents, TaxCents = order.TaxCents, TotalCents = order.TotalCents,
                PaymentMethod = order.PaymentMethod, CreatedOn = order.CreatedOn, StoreName = store.Name,
                CurrencySymbol = store.CurrencySymbol, ReceiptFooter = store.ReceiptFooter,
                Lines = lines.Select(x => new PosReceiptLine { ProductName = x.ProductName, Quantity = x.Quantity, UnitPriceCents = x.UnitPriceCents, LineTotalCents = x.LineTotalCents }).ToList()
            };
        }

        public PosOperationResult CancelOrder(int siteId, int moduleId, int orderId)
        {
            using var db = _factory.CreateDbContext();
            using var transaction = db.Database.BeginTransaction();
            var store = db.PosStores.FirstOrDefault(x => x.SiteId == siteId && x.ModuleId == moduleId);
            var order = store == null ? null : db.PosOrders.FirstOrDefault(x => x.PosOrderId == orderId && x.PosStoreId == store.PosStoreId);
            if (order == null) return OperationFailure("Sale not found.");
            if (order.Status != "Paid") return OperationFailure($"Order #{order.OrderNumber} is already {order.Status.ToLowerInvariant()}.");
            var lines = db.PosOrderLines.Where(x => x.PosOrderId == order.PosOrderId).ToList();
            var productIds = lines.Select(x => x.PosProductId).Distinct().ToList();
            var products = db.PosProducts.Where(x => x.PosStoreId == store.PosStoreId && productIds.Contains(x.PosProductId)).ToDictionary(x => x.PosProductId);
            foreach (var line in lines)
                if (products.TryGetValue(line.PosProductId, out var product) && product.TrackInventory) product.StockQuantity += line.Quantity;
            order.Status = "Cancelled";
            db.SaveChanges();
            transaction.Commit();
            return new PosOperationResult { Succeeded = true, Message = $"Order #{order.OrderNumber} cancelled and tracked stock restored." };
        }

        public List<PosOrder> GetOrders(int siteId, int moduleId)
        {
            using var db = _factory.CreateDbContext();
            var storeId = db.PosStores.AsNoTracking().Where(x => x.SiteId == siteId && x.ModuleId == moduleId).Select(x => x.PosStoreId).FirstOrDefault();
            return storeId == 0 ? new List<PosOrder>() : db.PosOrders.AsNoTracking().Where(x => x.PosStoreId == storeId).OrderByDescending(x => x.PosOrderId).ToList();
        }

        public List<PosStoreSummary> GetStoreSummaries()
        {
            using var db = _factory.CreateDbContext();
            var stores = db.PosStores.AsNoTracking().OrderBy(x => x.SiteId).ThenBy(x => x.Name).ToList();
            var storeIds = stores.Select(x => x.PosStoreId).ToList();
            var products = db.PosProducts.AsNoTracking().Where(x => storeIds.Contains(x.PosStoreId) && x.IsActive).ToList();
            var orders = db.PosOrders.AsNoTracking().Where(x => storeIds.Contains(x.PosStoreId)).ToList();
            return stores.Select(store =>
            {
                var storeProducts = products.Where(x => x.PosStoreId == store.PosStoreId).ToList();
                var storeOrders = orders.Where(x => x.PosStoreId == store.PosStoreId).ToList();
                return new PosStoreSummary
                {
                    PosStoreId = store.PosStoreId, SiteId = store.SiteId, ModuleId = store.ModuleId, StoreName = store.Name,
                    CurrencySymbol = store.CurrencySymbol, IsOpen = store.IsOpen, ProductCount = storeProducts.Count,
                    LowStockProducts = storeProducts.Count(x => x.TrackInventory && x.StockQuantity <= x.LowStockThreshold),
                    PaidOrders = storeOrders.Count(x => x.Status == "Paid"), CancelledOrders = storeOrders.Count(x => x.Status == "Cancelled"),
                    RevenueCents = storeOrders.Where(x => x.Status == "Paid").Sum(x => x.TotalCents)
                };
            }).ToList();
        }

        private static PosStore EnsureStore(Context db, int siteId, int moduleId)
        {
            var store = db.PosStores.FirstOrDefault(x => x.SiteId == siteId && x.ModuleId == moduleId);
            if (store == null)
            {
                store = new PosStore { SiteId = siteId, ModuleId = moduleId, Name = "Pour House", CurrencySymbol = "$", TaxRateBasisPoints = 850, IsOpen = true, NextOrderNumber = 1001 };
                db.PosStores.Add(store);
                db.SaveChanges();
            }
            if (db.PosProducts.Any(x => x.PosStoreId == store.PosStoreId)) return store;
            var categories = db.PosCategories.Where(x => x.PosStoreId == store.PosStoreId).ToList();
            if (categories.Count == 0)
            {
                categories = new[] { "Lager", "IPA", "Wheat", "Dark", "Seasonal", "Alcohol-free" }
                    .Select((name, index) => new PosCategory { PosStoreId = store.PosStoreId, Name = name, SortOrder = index + 1, IsActive = true }).ToList();
                db.PosCategories.AddRange(categories);
                db.SaveChanges();
            }
            var seed = new[]
            {
                new { Name="Golden Hour", Brewery="Northline Brewing", Category="Lager", Abv="4.5%", Price=650, Format="Pint", Color="gold", Stock="Tap 01" },
                new { Name="Hazy Orbit", Brewery="Satellite Works", Category="IPA", Abv="6.4%", Price=800, Format="Pint", Color="orange", Stock="Tap 04" },
                new { Name="Coastal Pale", Brewery="Breakwater Co.", Category="IPA", Abv="5.2%", Price=750, Format="Pint", Color="blue", Stock="Tap 06" },
                new { Name="Cloud Nine", Brewery="Juniper House", Category="Wheat", Abv="4.8%", Price=700, Format="Pint", Color="cream", Stock="Tap 08" },
                new { Name="Midnight Porter", Brewery="Black Pine", Category="Dark", Abv="6.1%", Price=850, Format="Pint", Color="charcoal", Stock="Tap 11" },
                new { Name="Citrus Static", Brewery="Electric Field", Category="Seasonal", Abv="5.7%", Price=800, Format="Pint", Color="lime", Stock="Tap 13" },
                new { Name="Easy Street", Brewery="Northline Brewing", Category="Alcohol-free", Abv="0.4%", Price=550, Format="Can", Color="aqua", Stock="Cold" }
            };
            db.PosProducts.AddRange(seed.Select((x, i) => new PosProduct { PosStoreId = store.PosStoreId, PosCategoryId = categories.First(c => c.Name == x.Category).PosCategoryId, Name = x.Name, Brewery = x.Brewery, Style = x.Category, Abv = x.Abv, PriceCents = x.Price, Format = x.Format, Color = x.Color, StockLabel = x.Stock, TrackInventory = true, StockQuantity = 25, LowStockThreshold = 5, IsActive = true, SortOrder = i + 1 }));
            db.SaveChanges();
            return store;
        }

        private static string AllowedPayment(string value) => new[] { "Cash", "Card", "Tap" }.Contains(value) ? value : "Card";
        private static string Clean(string value, string fallback, int max) { var result = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim(); return result.Length <= max ? result : result[..max]; }
        private static bool ValidImage(string value) => string.IsNullOrWhiteSpace(value) || (value.Length <= 900000 && value.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase));
        private static PosReceipt Failure(string message) => new() { Succeeded = false, Message = message };
        private static PosOperationResult OperationFailure(string message) => new() { Succeeded = false, Message = message };
    }
}
