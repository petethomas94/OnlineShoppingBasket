using OnlineShoppingBasket.Models;

namespace OnlineShoppingBasket.Core.Repositories;

public interface IProductRepository
{
    List<Product> GetAllProducts();
    Product? GetProductById(string id);
    Dictionary<string, Product> GetProductsById(IEnumerable<string> ids);
}