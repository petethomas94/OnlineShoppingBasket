using OnlineShoppingBasket.Models;

namespace OnlineShoppingBasket.Core;

public interface IBasketCalculationService
{
    decimal CalculateTotal(Basket basket);
    decimal CalculateTotalWithVat(Basket basket, decimal vatRate = 0.20m);
}