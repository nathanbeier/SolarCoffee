using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SolarCoffee.Data;
using SolarCoffee.Data.Models;
using SolarCoffee.Services.Inventory;
using SolarCoffee.Services.Product;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SolarCoffee.Services.Order
{
    public class OrderService : IOrderService
    {
        private readonly SolarDbContext _db;
        private readonly ILogger<OrderService> _logger;
        private readonly IInventoryService _inventoryService;
        private readonly IProductService _productService;

        public OrderService(SolarDbContext dbContext, ILogger<OrderService> logger, IInventoryService inventoryService, IProductService productService)
        {
            _db = dbContext;
            _logger = logger;
            _inventoryService = inventoryService;
            _productService = productService;
        }

        /// <summary>
        /// Creates an open SalesOrder
        /// </summary>
        /// <param name="order">The salesorder to update</param>
        /// <returns>Service response</returns>
        public ServiceResponse<bool> GenerateOpenOrder(SalesOrder order)
        {
            var now = DateTime.UtcNow;

            _logger.LogInformation("Generating new order");

            foreach (var item in order.SalesOrderItems)
            {
                item.Product = _productService.GetProductById(item.Product.Id);

                var inventoryId = _inventoryService.GetByProductId(item.Product.Id).Id;

                _inventoryService.UpdateUnitsAvailable(inventoryId, -item.Quantity);
                
            };

            try
            {
                _db.SalesOrders.Add(order);
                _db.SaveChanges();

                return new ServiceResponse<bool>
                {
                    IsSuccess = true,
                    Data = true,
                    Message = "Invoice created",
                    Time = now
                };
            }
            catch (Exception e)
            {
                return new ServiceResponse<bool>
                {
                    IsSuccess = false,
                    Data = false,
                    Message = e.StackTrace,
                    Time = now
                };
            }
        }

        /// <summary>
        /// Get all SalesORders from database
        /// </summary>
        /// <returns>a list of SalesOrders</returns>
        public List<SalesOrder> GetOrders()
        {
            return _db.SalesOrders
                .Include(s => s.Customer)
                    .ThenInclude(c => c.PrimaryAddress)
                .Include(s => s.SalesOrderItems)
                    .ThenInclude(si => si.Product)
                .ToList();
        }

        /// <summary>
        /// Marks an open SalesOrder as paid
        /// </summary>
        /// <param name="id">SalesOrder id</param>
        /// <returns>ServiceResponse</returns>
        public ServiceResponse<bool> WorkFulfilled(int id)
        {
            var now = DateTime.UtcNow;

            var order = _db.SalesOrders.Find(id);

            order.UpdatedOn = now;
            order.IsPaid = true;

            try
            {
                _db.SalesOrders.Update(order);
                _db.SaveChanges();

                return new ServiceResponse<bool>
                {
                    IsSuccess = true,
                    Data = true,
                    Message = $"Order {order.Id} closed: Invoice paid in full.",
                    Time = now
                };
            }
            catch (Exception e)
            {
                return new ServiceResponse<bool>
                {
                    IsSuccess = false,
                    Data = false,
                    Message = e.StackTrace,
                    Time = now
                };
            }
        }
    }
}
