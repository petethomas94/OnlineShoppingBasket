using OnlineShoppingBasket.Core.Repositories;
using OnlineShoppingBasket.Models;

namespace OnlineShoppingBasket.Core.Services;

public class BasketCalculationService : IBasketCalculationService
{
    private readonly IProductRepository _productRepository;
    private readonly IDiscountRepository _discountRepository;
    private readonly IShippingCostRepository _shippingCostRepository;

    public BasketCalculationService(
        IProductRepository productRepository, 
        IDiscountRepository discountRepository,
        IShippingCostRepository shippingCostRepository)
    {
        _productRepository = productRepository;
        _discountRepository = discountRepository;
        _shippingCostRepository = shippingCostRepository;
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
            .Union(string.IsNullOrEmpty(basket.DiscountId) ? new string[0] : new[] { basket.DiscountId })
            .Distinct();
        
        var discounts = _discountRepository.GetDiscountsById(discountIds);

        var shippingCost = _shippingCostRepository.GetShippingCostByCountry(basket.ShippingTo);

        var itemsWithDiscountTotal = 0m;
        var itemsWithoutDiscountTotal = 0m;

        foreach (var item in basket.Items)
        {
            var itemTotal = products[item.ProductId].Price * item.Quantity;
            
            if (!string.IsNullOrEmpty(item.DiscountId) && discounts.ContainsKey(item.DiscountId))
            {
                // Item has individual discount - apply it and exclude from basket discount
                var discount = discounts[item.DiscountId];
                var discountAmount = itemTotal * (discount.Percentage / 100m);
                itemsWithDiscountTotal += itemTotal - discountAmount;
            }
            else
            {
                // Item has no individual discount - eligible for basket discount
                itemsWithoutDiscountTotal += itemTotal;
            }
        }

        // Apply basket-level discount only to items that don't have individual discounts
        if (!string.IsNullOrEmpty(basket.DiscountId) && discounts.ContainsKey(basket.DiscountId))
        {
            var basketDiscount = discounts[basket.DiscountId];
            var basketDiscountAmount = itemsWithoutDiscountTotal * (basketDiscount.Percentage / 100m);
            itemsWithoutDiscountTotal -= basketDiscountAmount;
        }

        return itemsWithDiscountTotal + itemsWithoutDiscountTotal + (shippingCost?.Price ?? 0);
    }

    public decimal CalculateTotalWithVat(Basket basket, decimal vatRate = 0.20m)
    {
        var subtotal = CalculateTotal(basket);
        return subtotal * (1 + vatRate);
    }
}