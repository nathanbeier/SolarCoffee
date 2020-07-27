using SolarCoffee.Data.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace SolarCoffee.Services.Inventory
{
    public interface IInventoryService
    {
        List<ProductInventory> GetCurrentInventory();

        ServiceResponse<ProductInventory> UpdateUnitsAvailable(int id, int adjustment);

        ProductInventory GetByProductId(int id);

        List<ProductInventorySnapshot> GetSnapshotHistory();
    }
}
