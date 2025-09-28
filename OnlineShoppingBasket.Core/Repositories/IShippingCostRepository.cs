using OnlineShoppingBasket.Models;

namespace OnlineShoppingBasket.Core.Repositories;

public interface IShippingCostRepository
{
    List<ShippingCost> GetAllShippingCosts();
    ShippingCost? GetShippingCostByCountry(string country);
}