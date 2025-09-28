using OnlineShoppingBasket.Core;
using OnlineShoppingBasket.Models;

namespace OnlineShoppingBasket.Infrastructure;

public class InMemoryProductRepository : IProductRepository
{
    private static readonly Dictionary<string, Product> _products = new()
    {
        ["11111111-1111-1111-1111-111111111111"] = new Product { Id = "11111111-1111-1111-1111-111111111111", Name = "Apple", Price = 0.50m },
        ["22222222-2222-2222-2222-222222222222"] = new Product { Id = "22222222-2222-2222-2222-222222222222", Name = "Banana", Price = 0.30m },
        ["33333333-3333-3333-3333-333333333333"] = new Product { Id = "33333333-3333-3333-3333-333333333333", Name = "Orange", Price = 0.60m }
    };
    
    public List<Product> GetAllProducts()
    {
        return _products.Values.ToList();
    }

    public Product? GetProductById(string id)
    {
        return _products.GetValueOrDefault(id);
    }

    public Dictionary<string, Product> GetProductsById(IEnumerable<string> ids)
    {
        return _products.Where(p => ids.Contains(p.Key)).ToDictionary(p => p.Key, p => p.Value);
    }
}