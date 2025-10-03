using OnlineShoppingBasket.Core.Repositories;
using OnlineShoppingBasket.Models;

namespace OnlineShoppingBasket.Infrastructure.InMemoryRepositories;

public class BasketRepository : IBasketRepository
{
    private readonly Dictionary<string, Basket> _baskets = new();
    
    public void SaveBasket(Basket basket)
    {
        _baskets[basket.Id] = basket;
    }

    public Basket? GetBasket(string id)
    {
        return _baskets.GetValueOrDefault(id);
    }

    public void AddItemToBasket(string basketId, BasketItem item)
    {
        var basket = _baskets[basketId];

        if (basket.Items.Any(x => x.ProductId == item.ProductId))
        {
            basket.Items.Single(x => x.ProductId == item.ProductId).Quantity += item.Quantity;
            return;
        }

        basket.Items.Add(item);
    }
}