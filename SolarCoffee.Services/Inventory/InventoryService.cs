using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SolarCoffee.Data;
using SolarCoffee.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;

namespace SolarCoffee.Services.Inventory
{
    public class InventoryService : IInventoryService
    {
        private readonly SolarDbContext _db;
        private readonly ILogger<InventoryService> _logger;

        public InventoryService(SolarDbContext dbContext, ILogger<InventoryService> logger)
        {
            _db = dbContext;
            _logger = logger;
        }

        /// <summary>
        /// Get product by ID
        /// </summary>
        /// <param name="id">int id</param>
        /// <returns>ProductInventory</returns>
        public ProductInventory GetByProductId(int id)
        {
            return _db.ProductInventories
                .Include(pi => pi.Product)
                .FirstOrDefault(pi => pi.Product.Id == id);
        }

        /// <summary>
        /// Returns all current Inventory from the database
        /// </summary>
        /// <returns>List<ProductInventory>></returns>
        public List<ProductInventory> GetCurrentInventory()
        {
            return _db.ProductInventories
                .Include(pi => pi.Product)
                .Where(pi => !pi.Product.IsArchived)
                .ToList();
        }

        /// <summary>
        /// Return snapshot history for the previous 6 hours
        /// </summary>
        /// <returns>List<ProductInventorySnapshot></returns>
        public List<ProductInventorySnapshot> GetSnapshotHistory()
        {
            var timespan = DateTime.UtcNow - TimeSpan.FromHours(6);

            return _db.ProductInventorySnapshots
                .Include(p => p.Product)
                .Where(p => p.SnapshotTime > timespan && !p.Product.IsArchived)
                .ToList();
        }

        /// <summary>
        /// Create updates to Inventory records via Id, and the amount to adjust.
        /// Pass a negative as adjustment amount to subtract.
        /// </summary>
        /// <param name="id">productId</param>
        /// <param name="adjustment">amount to adjust</param>
        /// <returns>ServiceResponse<ProductInventory></returns>
        public ServiceResponse<ProductInventory> UpdateUnitsAvailable(int id, int adjustment)
        {
            var now = DateTime.UtcNow;

            try
            {
                var inventory = _db.ProductInventories
                    .Include(pi => pi.Product)
                    .First(pi => pi.Product.Id == id);

                inventory.QuantityOnHand += adjustment;

                try
                {
                    CreateSnapshot(inventory);
                }
                catch(Exception e)
                {
                    _logger.LogError("Error creating inventory snapshot");
                    _logger.LogError(e.StackTrace);
                }

                _db.SaveChanges();

                return new ServiceResponse<ProductInventory>
                {
                    IsSuccess = true,
                    Data = inventory,
                    Message = $"Product {id} inventory adjusted",
                    Time = now
                };
            }
            catch
            {
                return new ServiceResponse<ProductInventory>
                {
                    IsSuccess = false,
                    Data = null,
                    Message = $"Error updating product {id} with adjustment of {adjustment}",
                    Time = now
                };
            }
        }

        /// <summary>
        /// Creates an InventorySnapshot record
        /// </summary>
        private void CreateSnapshot(ProductInventory inventory)
        {
            var now = DateTime.UtcNow;

            var snapshot = new ProductInventorySnapshot
            {
                SnapshotTime = now,
                Product = inventory.Product,
                QuantityOnHand = inventory.QuantityOnHand
            };

            _db.Add(snapshot);
        }

    }
}
