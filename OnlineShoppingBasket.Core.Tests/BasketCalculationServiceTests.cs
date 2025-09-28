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
    public void CalculateTotal_ReturnsZero_WhenBasketIsEmpty()
    {
        // Arrange
        var basket = new Basket { Items = new List<BasketItem>() };

        // Act
        var result = _basketCalculationService.CalculateTotal(basket);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void CalculateTotal_ReturnsItemsTotal_WhenNoDiscountsOrShipping()
    {
        // Arrange
        var basket = new Basket 
        { 
            Items = new List<BasketItem> 
            { 
                new() { ProductId = "product1", Quantity = 2 },
                new() { ProductId = "product2", Quantity = 1 }
            }
        };

        var products = new Dictionary<string, Product>
        {
            { "product1", new Product { Id = "product1", Price = 10.00m } },
            { "product2", new Product { Id = "product2", Price = 15.00m } }
        };

        var shippingCost = new ShippingCost { Price = 0 };

        _productRepository.Setup(x => x.GetProductsById(It.IsAny<IEnumerable<string>>())).Returns(products);
        _discountRepository.Setup(x => x.GetDiscountsById(It.IsAny<IEnumerable<string>>())).Returns(new Dictionary<string, Discount>());
        _shippingCostRepository.Setup(x => x.GetShippingCostByCountry(It.IsAny<string>())).Returns(shippingCost);

        // Act
        var result = _basketCalculationService.CalculateTotal(basket);

        // Assert
        Assert.Equal(35.00m, result); // (10 * 2) + (15 * 1) = 35
    }

    [Fact]
    public void CalculateTotal_AppliesItemLevelDiscount_WhenItemHasDiscount()
    {
        // Arrange
        var basket = new Basket 
        { 
            Items = new List<BasketItem> 
            { 
                new() { ProductId = "product1", Quantity = 2, DiscountId = "discount1" }
            }
        };

        var products = new Dictionary<string, Product>
        {
            { "product1", new Product { Id = "product1", Price = 10.00m } }
        };

        var discounts = new Dictionary<string, Discount>
        {
            { "discount1", new Discount { Id = "discount1", Percentage = 20 } }
        };

        var shippingCost = new ShippingCost { Price = 0 };

        _productRepository.Setup(x => x.GetProductsById(It.IsAny<IEnumerable<string>>())).Returns(products);
        _discountRepository.Setup(x => x.GetDiscountsById(It.IsAny<IEnumerable<string>>())).Returns(discounts);
        _shippingCostRepository.Setup(x => x.GetShippingCostByCountry(It.IsAny<string>())).Returns(shippingCost);

        // Act
        var result = _basketCalculationService.CalculateTotal(basket);

        // Assert
        Assert.Equal(16.00m, result); // 20 - (20 * 0.20) = 16
    }

    [Fact]
    public void CalculateTotal_AppliesBasketLevelDiscount_OnlyToItemsWithoutDiscounts()
    {
        // Arrange
        var basket = new Basket 
        { 
            DiscountId = "basketDiscount",
            Items = new List<BasketItem> 
            { 
                new() { ProductId = "product1", Quantity = 1, DiscountId = "itemDiscount" }, // Has item discount
                new() { ProductId = "product2", Quantity = 1 } // No item discount
            }
        };

        var products = new Dictionary<string, Product>
        {
            { "product1", new Product { Id = "product1", Price = 10.00m } },
            { "product2", new Product { Id = "product2", Price = 20.00m } }
        };

        var discounts = new Dictionary<string, Discount>
        {
            { "itemDiscount", new Discount { Id = "itemDiscount", Percentage = 10 } },
            { "basketDiscount", new Discount { Id = "basketDiscount", Percentage = 25 } }
        };

        var shippingCost = new ShippingCost { Price = 0 };

        _productRepository.Setup(x => x.GetProductsById(It.IsAny<IEnumerable<string>>())).Returns(products);
        _discountRepository.Setup(x => x.GetDiscountsById(It.IsAny<IEnumerable<string>>())).Returns(discounts);
        _shippingCostRepository.Setup(x => x.GetShippingCostByCountry(It.IsAny<string>())).Returns(shippingCost);

        // Act
        var result = _basketCalculationService.CalculateTotal(basket);

        // Assert
        // Item 1: 10 - (10 * 0.10) = 9
        // Item 2: 20 - (20 * 0.25) = 15 (basket discount applied)
        Assert.Equal(24.00m, result);
    }

    [Fact]
    public void CalculateTotal_IncludesShippingCost_WhenShippingCountryIsSet()
    {
        // Arrange
        var basket = new Basket 
        { 
            ShippingTo = "UK",
            Items = new List<BasketItem> 
            { 
                new() { ProductId = "product1", Quantity = 1 }
            }
        };

        var products = new Dictionary<string, Product>
        {
            { "product1", new Product { Id = "product1", Price = 10.00m } }
        };

        var shippingCost = new ShippingCost { Price = 5.00m };

        _productRepository.Setup(x => x.GetProductsById(It.IsAny<IEnumerable<string>>())).Returns(products);
        _discountRepository.Setup(x => x.GetDiscountsById(It.IsAny<IEnumerable<string>>())).Returns(new Dictionary<string, Discount>());
        _shippingCostRepository.Setup(x => x.GetShippingCostByCountry("UK")).Returns(shippingCost);

        // Act
        var result = _basketCalculationService.CalculateTotal(basket);

        // Assert
        Assert.Equal(15.00m, result); // 10 + 5 = 15
    }

    [Fact]
    public void CalculateTotalWithVat_AppliesVatCorrectly_WithDefaultRate()
    {
        // Arrange
        var basket = new Basket 
        { 
            Items = new List<BasketItem> 
            { 
                new() { ProductId = "product1", Quantity = 1 }
            }
        };

        var products = new Dictionary<string, Product>
        {
            { "product1", new Product { Id = "product1", Price = 10.00m } }
        };

        var shippingCost = new ShippingCost { Price = 0 };

        _productRepository.Setup(x => x.GetProductsById(It.IsAny<IEnumerable<string>>())).Returns(products);
        _discountRepository.Setup(x => x.GetDiscountsById(It.IsAny<IEnumerable<string>>())).Returns(new Dictionary<string, Discount>());
        _shippingCostRepository.Setup(x => x.GetShippingCostByCountry(It.IsAny<string>())).Returns(shippingCost);

        // Act
        var result = _basketCalculationService.CalculateTotalWithVat(basket);

        // Assert
        Assert.Equal(12.00m, result); // 10 * 1.20 = 12
    }

    [Fact]
    public void CalculateTotalWithVat_AppliesVatCorrectly_WithCustomRate()
    {
        // Arrange
        var basket = new Basket 
        { 
            Items = new List<BasketItem> 
            { 
                new() { ProductId = "product1", Quantity = 1 }
            }
        };

        var products = new Dictionary<string, Product>
        {
            { "product1", new Product { Id = "product1", Price = 10.00m } }
        };

        var shippingCost = new ShippingCost { Price = 0 };

        _productRepository.Setup(x => x.GetProductsById(It.IsAny<IEnumerable<string>>())).Returns(products);
        _discountRepository.Setup(x => x.GetDiscountsById(It.IsAny<IEnumerable<string>>())).Returns(new Dictionary<string, Discount>());
        _shippingCostRepository.Setup(x => x.GetShippingCostByCountry(It.IsAny<string>())).Returns(shippingCost);

        // Act
        var result = _basketCalculationService.CalculateTotalWithVat(basket, 0.25m);

        // Assert
        Assert.Equal(12.50m, result); // 10 * 1.25 = 12.50
    }
}