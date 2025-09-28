using OnlineShoppingBasket.Models;

namespace OnlineShoppingBasket.Core;

public class BasketCalculationService : IBasketCalculationService
{
    private readonly IProductRepository _productRepository;
    private readonly IDiscountRepository _discountRepository;

    public BasketCalculationService(IProductRepository productRepository, IDiscountRepository discountRepository)
    {
        _productRepository = productRepository;
        _discountRepository = discountRepository;
    }

    public decimal CalculateTotal(Basket basket)
    {
        if (!basket.Items.Any())
        {
            return 0;
        }

        var productIds = basket.Items.Select(x => x.ProductId);
        var products = _productRepository.GetProductsById(productIds);

        var discountIds = basket.Items
            .Where(x => !string.IsNullOrEmpty(x.DiscountId))
            .Select(x => x.DiscountId)
            .Distinct();
        
        var discounts = _discountRepository.GetDiscountsById(discountIds);

        var itemsWithDiscounts = 0m;
        var itemsWithoutDiscounts = 0m;

        foreach (var item in basket.Items)
        {
            var itemTotal = products[item.ProductId].Price * item.Quantity;
            
            if (!string.IsNullOrEmpty(item.DiscountId) && discounts.ContainsKey(item.DiscountId))
            {
                var discount = discounts[item.DiscountId];
                var discountAmount = itemTotal * (discount.Percentage / 100m);
                itemsWithDiscounts += itemTotal - Math.Min(discountAmount, itemTotal);
            }
            else
            {
                itemsWithoutDiscounts += itemTotal;
            }
        }

        // Apply basket-level discount only to items without individual discounts
        if (!string.IsNullOrEmpty(basket.DiscountId) && discounts.ContainsKey(basket.DiscountId))
        {
            var basketDiscount = discounts[basket.DiscountId];
            var basketDiscountAmount = itemsWithoutDiscounts * (basketDiscount.Percentage / 100m);
            itemsWithoutDiscounts -= Math.Min(basketDiscountAmount, itemsWithoutDiscounts);
        }

        return itemsWithDiscounts + itemsWithoutDiscounts;
    }

    public decimal CalculateTotalWithVat(Basket basket, decimal vatRate = 0.20m)
    {
        var subtotal = CalculateTotal(basket);
        return subtotal * (1 + vatRate);
    }
}