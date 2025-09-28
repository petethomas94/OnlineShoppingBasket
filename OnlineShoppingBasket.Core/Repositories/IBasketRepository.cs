using OnlineShoppingBasket.Models;

namespace OnlineShoppingBasket.Core.Repositories;

public interface IBasketRepository
{
    void SaveBasket(Basket basket);
    Basket? GetBasket(string id);
    void AddItemToBasket(string basketId, BasketItem item);
}