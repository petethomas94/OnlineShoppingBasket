using OnlineShoppingBasket.Core.Repositories;
using OnlineShoppingBasket.Models;

namespace OnlineShoppingBasket.Infrastructure.InMemoryRepositories;

public class ShippingCostRepository : IShippingCostRepository
{
    private readonly Dictionary<string, ShippingCost> _shippingCosts = new()
    {
        { "UK", new ShippingCost() { Country = "UK", Price = 3 } },
        { "FR", new ShippingCost() { Country = "FR", Price = 5 } },
        { "US", new ShippingCost() { Country = "US", Price = 7 } }
    };
    
    public List<ShippingCost> GetAllShippingCosts()
    {
        return _shippingCosts.Values.ToList();
    }

    public ShippingCost? GetShippingCostByCountry(string country)
    {
        return _shippingCosts.GetValueOrDefault(country);
    }
}