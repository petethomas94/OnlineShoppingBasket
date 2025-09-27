using OnlineShoppingBasket.Models;

namespace OnlineShoppingBasket.Core;

public interface IProductRepository
{
    List<Product> GetAllProducts();
    Product? GetProductById(string id);
}