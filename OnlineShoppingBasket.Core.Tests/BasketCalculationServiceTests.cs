using Moq;
using OnlineShoppingBasket.Core.Repositories;
using OnlineShoppingBasket.Core.Services;
using OnlineShoppingBasket.Models;

namespace OnlineShoppingBasket.Core.Tests;

public class BasketCalculationServiceTests
{
    private readonly Mock<IProductRepository> _productRepository;
    private readonly Mock<IDiscountRepository> _discountRepository;
    private readonly Mock<IShippingCostRepository> _shippingCostRepository;
    private readonly BasketCalculationService _basketCalculationService;

    public BasketCalculationServiceTests()
    {
        _productRepository = new Mock<IProductRepository>();
        _discountRepository = new Mock<IDiscountRepository>();
        _shippingCostRepository = new Mock<IShippingCostRepository>();
        _basketCalculationService = new BasketCalculationService(_productRepository.Object, _discountRepository.Object, _shippingCostRepository.Object);
    }

    [Fact]
    public void CalculateTotal_DoesNotApplyBasketDiscountToItemsWithIndividualDiscounts()
    {
        // Arrange
        var basket = new Basket
        {
            Id = "basket1",
            ShippingTo = "FR",
            Items = new List<BasketItem>
            {
                new() { ProductId = "1", Quantity = 1, DiscountId = "item-discount" },
                new() { ProductId = "2", Quantity = 1 }
            },
            DiscountId = "basket-discount"
        };

        var products = new Dictionary<string, Product>
        {
            { "1", new Product { Id = "1", Price = 10.00m } },
            { "2", new Product { Id = "2", Price = 20.00m } }
        };

        var discounts = new Dictionary<string, Discount>
        {
            { "item-discount", new Discount { Id = "item-discount", Percentage = 10.00m } },
            { "basket-discount", new Discount { Id = "basket-discount", Percentage = 20.00m } }
        };

        var shippingCost = new ShippingCost { Country = "FR", Price = 5.00m };

        _productRepository.Setup(x => x.GetProductsById(It.IsAny<IEnumerable<string>>())).Returns(products);
        _discountRepository.Setup(x => x.GetDiscountsById(It.IsAny<IEnumerable<string>>())).Returns(discounts);
        _shippingCostRepository.Setup(x => x.GetShippingCostByCountry("FR")).Returns(shippingCost);

        // Act
        var result = _basketCalculationService.CalculateTotal(basket);

        // Expected: Item1(9.00) + Item2(16.00) + Shipping(5.00) = 30.00
        Assert.Equal(30.00m, result);
    }

    [Fact]
    public void CalculateTotal_AppliesBasketDiscountToAllItems_WhenNoIndividualDiscounts()
    {
        // Arrange
        var basket = new Basket
        {
            Id = "basket1",
            ShippingTo = "FR",
            Items = new List<BasketItem>
            {
                new() { ProductId = "1", Quantity = 1 },
                new() { ProductId = "2", Quantity = 1 }
            },
            DiscountId = "basket-discount"
        };

        var products = new Dictionary<string, Product>
        {
            { "1", new Product { Id = "1", Price = 10.00m } },
            { "2", new Product { Id = "2", Price = 20.00m } }
        };

        var discounts = new Dictionary<string, Discount>
        {
            { "basket-discount", new Discount { Id = "basket-discount", Percentage = 20.00m } }
        };

        var shippingCost = new ShippingCost { Country = "FR", Price = 5.00m };

        _productRepository.Setup(x => x.GetProductsById(It.IsAny<IEnumerable<string>>())).Returns(products);
        _discountRepository.Setup(x => x.GetDiscountsById(It.IsAny<IEnumerable<string>>())).Returns(discounts);
        _shippingCostRepository.Setup(x => x.GetShippingCostByCountry("FR")).Returns(shippingCost);

        // Act
        var result = _basketCalculationService.CalculateTotal(basket);

        // Expected: (30.00 - 6.00 basket discount) + 5.00 shipping = 29.00
        Assert.Equal(29.00m, result);
    }

    [Fact]
    public void CalculateTotal_AppliesOnlyIndividualDiscounts_WhenAllItemsHaveIndividualDiscounts()
    {
        // Arrange
        var basket = new Basket
        {
            Id = "basket1",
            ShippingTo = "FR",
            Items = new List<BasketItem>
            {
                new() { ProductId = "1", Quantity = 1, DiscountId = "discount1" },
                new() { ProductId = "2", Quantity = 1, DiscountId = "discount2" }
            },
            DiscountId = "basket-discount"
        };

        var products = new Dictionary<string, Product>
        {
            { "1", new Product { Id = "1", Price = 10.00m } },
            { "2", new Product { Id = "2", Price = 20.00m } }
        };

        var discounts = new Dictionary<string, Discount>
        {
            { "discount1", new Discount { Id = "discount1", Percentage = 10.00m } },
            { "discount2", new Discount { Id = "discount2", Percentage = 15.00m } },
            { "basket-discount", new Discount { Id = "basket-discount", Percentage = 50.00m } }
        };

        var shippingCost = new ShippingCost { Country = "FR", Price = 5.00m };

        _productRepository.Setup(x => x.GetProductsById(It.IsAny<IEnumerable<string>>())).Returns(products);
        _discountRepository.Setup(x => x.GetDiscountsById(It.IsAny<IEnumerable<string>>())).Returns(discounts);
        _shippingCostRepository.Setup(x => x.GetShippingCostByCountry("FR")).Returns(shippingCost);

        // Act
        var result = _basketCalculationService.CalculateTotal(basket);

        // Expected: Item1(9.00) + Item2(17.00) + Shipping(5.00) = 31.00
        Assert.Equal(31.00m, result);
    }

    [Fact]
    public void CalculateTotal_ReturnsZero_WhenBasketIsEmpty()
    {
        // Arrange
        var basket = new Basket
        {
            Id = "basket1",
            ShippingTo = "FR",
            Items = new List<BasketItem>()
        };

        _productRepository.Setup(x => x.GetProductsById(It.IsAny<IEnumerable<string>>())).Returns(new Dictionary<string, Product>());
        _discountRepository.Setup(x => x.GetDiscountsById(It.IsAny<IEnumerable<string>>())).Returns(new Dictionary<string, Discount>());

        // Act
        var result = _basketCalculationService.CalculateTotal(basket);

        // Assert
        Assert.Equal(0m, result);
    }

    [Fact]
    public void CalculateTotal_IncludesShippingCost_WhenShippingDestinationSet()
    {
        // Arrange
        var basket = new Basket
        {
            Id = "basket1",
            ShippingTo = "US",
            Items = new List<BasketItem>
            {
                new() { ProductId = "1", Quantity = 2 }
            }
        };

        var products = new Dictionary<string, Product>
        {
            { "1", new Product { Id = "1", Price = 10.00m } }
        };

        var shippingCost = new ShippingCost { Country = "US", Price = 15.00m };

        _productRepository.Setup(x => x.GetProductsById(It.IsAny<IEnumerable<string>>())).Returns(products);
        _discountRepository.Setup(x => x.GetDiscountsById(It.IsAny<IEnumerable<string>>())).Returns(new Dictionary<string, Discount>());
        _shippingCostRepository.Setup(x => x.GetShippingCostByCountry("US")).Returns(shippingCost);

        // Act
        var result = _basketCalculationService.CalculateTotal(basket);

        // Expected: (10.00 * 2) + 15.00 = 35.00
        Assert.Equal(35.00m, result);
    }

    [Fact]
    public void CalculateTotal_ExcludesShippingCost_WhenShippingCostIsNull()
    {
        // Arrange
        var basket = new Basket
        {
            Id = "basket1",
            ShippingTo = "INVALID",
            Items = new List<BasketItem>
            {
                new() { ProductId = "1", Quantity = 1 }
            }
        };

        var products = new Dictionary<string, Product>
        {
            { "1", new Product { Id = "1", Price = 10.00m } }
        };

        _productRepository.Setup(x => x.GetProductsById(It.IsAny<IEnumerable<string>>())).Returns(products);
        _discountRepository.Setup(x => x.GetDiscountsById(It.IsAny<IEnumerable<string>>())).Returns(new Dictionary<string, Discount>());
        _shippingCostRepository.Setup(x => x.GetShippingCostByCountry("INVALID")).Returns((ShippingCost)null);

        // Act
        var result = _basketCalculationService.CalculateTotal(basket);

        // Expected: 10.00 * 1 = 10.00 (no shipping cost)
        Assert.Equal(10.00m, result);
    }

    [Fact]
    public void CalculateTotalWithVat_AppliesVatCorrectly()
    {
        // Arrange
        var basket = new Basket
        {
            Id = "basket1",
            ShippingTo = "FR",
            Items = new List<BasketItem>
            {
                new() { ProductId = "1", Quantity = 1 }
            }
        };

        var products = new Dictionary<string, Product>
        {
            { "1", new Product { Id = "1", Price = 10.00m } }
        };

        var shippingCost = new ShippingCost { Country = "FR", Price = 5.00m };

        _productRepository.Setup(x => x.GetProductsById(It.IsAny<IEnumerable<string>>())).Returns(products);
        _discountRepository.Setup(x => x.GetDiscountsById(It.IsAny<IEnumerable<string>>())).Returns(new Dictionary<string, Discount>());
        _shippingCostRepository.Setup(x => x.GetShippingCostByCountry("FR")).Returns(shippingCost);

        // Act
        var result = _basketCalculationService.CalculateTotalWithVat(basket, 0.20m);

        // Expected: (10.00 + 5.00) * 1.20 = 18.00
        Assert.Equal(18.00m, result);
    }

    [Fact]
    public void CalculateTotalWithVat_UsesDefaultVatRate_WhenNotSpecified()
    {
        // Arrange
        var basket = new Basket
        {
            Id = "basket1",
            ShippingTo = "FR",
            Items = new List<BasketItem>
            {
                new() { ProductId = "1", Quantity = 1 }
            }
        };

        var products = new Dictionary<string, Product>
        {
            { "1", new Product { Id = "1", Price = 10.00m } }
        };

        var shippingCost = new ShippingCost { Country = "FR", Price = 5.00m };

        _productRepository.Setup(x => x.GetProductsById(It.IsAny<IEnumerable<string>>())).Returns(products);
        _discountRepository.Setup(x => x.GetDiscountsById(It.IsAny<IEnumerable<string>>())).Returns(new Dictionary<string, Discount>());
        _shippingCostRepository.Setup(x => x.GetShippingCostByCountry("FR")).Returns(shippingCost);

        // Act
        var result = _basketCalculationService.CalculateTotalWithVat(basket);

        // Expected: (10.00 + 5.00) * 1.20 = 18.00 (default 20% VAT)
        Assert.Equal(18.00m, result);
    }
}