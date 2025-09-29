using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using OnlineShoppingBasket.Api.Controllers;
using OnlineShoppingBasket.Core.Repositories;
using OnlineShoppingBasket.Core.Services;
using OnlineShoppingBasket.Models;

namespace OnlineShoppingBasket.Api.Tests;

public class BasketControllerTests
{
    private readonly BasketController _basketController;

    private readonly Mock<IBasketRepository> _basketRepository = new();
    private readonly Mock<IProductRepository> _productRepository = new();
    private readonly Mock<IBasketCalculationService> _basketCalculationService = new();
    private readonly Mock<IShippingCostRepository> _shippingCostRepository = new();
    private readonly Mock<IDiscountRepository> _discountRepository = new();

    
    private readonly Basket _basket = new()
    {
        Id = Guid.NewGuid().ToString()
    };

    private readonly Product _product = new() { Id = "product123" };

    public BasketControllerTests()
    {
        _basketController = new BasketController(
            _basketRepository.Object, 
            _productRepository.Object, 
            _discountRepository.Object,
            _shippingCostRepository.Object,
            _basketCalculationService.Object);
    }
    
    [Fact]
    public void CreateBasket_ReturnsNewBasket()
    {
        // Act
        var result = _basketController.CreateBasket();

        _basketRepository.Verify(x => x.SaveBasket(It.IsAny<Basket>()));
        var createdResult = result.Result as CreatedAtActionResult;
        Assert.NotNull(createdResult.Value);
        Assert.IsType<Basket>(createdResult.Value);
        
        var basket = createdResult.Value as Basket;
        Assert.NotNull(basket.Id);
        Assert.Equal(StatusCodes.Status201Created, createdResult.StatusCode);
    }
    
    [Fact]
    public void GetBasket_ReturnsBasket()
    {
        _basketRepository.Setup(x => x.GetBasket(_basket.Id)).Returns(_basket);

        var result = _basketController.GetBasket(_basket.Id);
        var createdResult = result.Result as OkObjectResult;
        Assert.NotNull(createdResult.Value);
        Assert.IsType<Basket>(createdResult.Value);
        
        var basketResult = createdResult.Value as Basket;
        Assert.NotNull(basketResult.Id);
        Assert.Equal(StatusCodes.Status200OK, createdResult.StatusCode);
    }
    
    [Fact]
    public void GetBasket_ReturnsNotFoundWhenBasketDoesNotExist()
    {
        var result = _basketController.GetBasket("id");
        
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal("Basket not found.", notFoundResult.Value);
        Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
    }

    [Fact]
    public void AddItem_ReturnsOk_WhenItemAddedSuccessfully()
    {
        // Arrange
        var basketItem = new BasketItem { ProductId = _product.Id, Quantity = 2 };
        var basketItems = new List<BasketItem> { basketItem };
        
        _basketRepository.Setup(x => x.GetBasket(_basket.Id)).Returns(_basket);
        _productRepository.Setup(x => x.GetProductById(_product.Id)).Returns(_product);
        
        // Act
        var result = _basketController.AddItem(_basket.Id, basketItems);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
        _basketRepository.Verify(x => x.AddItemToBasket(_basket.Id, basketItem), Times.Once);
    }

    [Fact]
    public void AddItem_ReturnsBadRequest_WhenQuantityIsZero()
    {
        // Arrange
        var basketItem = new BasketItem { ProductId = _product.Id, Quantity = 0 };
        var basketItems = new List<BasketItem> { basketItem };
        _basketRepository.Setup(x => x.GetBasket(_basket.Id)).Returns(_basket);
        
        // Act
        var result = _basketController.AddItem(_basket.Id, basketItems);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal($"Quantity must be greater than 0 for product {_product.Id}.", badRequestResult.Value);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
        _basketRepository.Verify(x => x.AddItemToBasket(It.IsAny<string>(), It.IsAny<BasketItem>()), Times.Never);
    }

    [Fact]
    public void AddItem_ReturnsBadRequest_WhenQuantityIsNegative()
    {
        // Arrange
        var basketItem = new BasketItem { ProductId = _product.Id, Quantity = -1 };
        var basketItems = new List<BasketItem> { basketItem };
        _basketRepository.Setup(x => x.GetBasket(_basket.Id)).Returns(_basket);
        
        // Act
        var result = _basketController.AddItem(_basket.Id, basketItems);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal($"Quantity must be greater than 0 for product {_product.Id}.", badRequestResult.Value);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
        _basketRepository.Verify(x => x.AddItemToBasket(It.IsAny<string>(), It.IsAny<BasketItem>()), Times.Never);
    }

    [Fact]
    public void AddItem_ReturnsNotFound_WhenBasketDoesNotExist()
    {
        // Arrange
        var basketId = "nonexistent-basket";
        var basketItem = new BasketItem { ProductId = _product.Id, Quantity = 1 };

        _basketRepository.Setup(x => x.GetBasket(basketId)).Returns((Basket)null);

        // Act
        var result = _basketController.AddItem(basketId, new List<BasketItem> {basketItem});

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal("Basket not found.", notFoundResult.Value);
        Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
        _basketRepository.Verify(x => x.AddItemToBasket(It.IsAny<string>(), It.IsAny<BasketItem>()), Times.Never);
    }

    [Fact]
    public void AddItem_ReturnsNotFound_WhenProductDoesNotExist()
    {
        // Arrange
        var productId = "nonexistent-product";
        var basketItem = new BasketItem { ProductId = productId, Quantity = 1 };

        _basketRepository.Setup(x => x.GetBasket(_basket.Id)).Returns(_basket);
        _productRepository.Setup(x => x.GetProductById(productId)).Returns((Product)null);

        // Act
        var result = _basketController.AddItem(_basket.Id, new List<BasketItem> {basketItem});

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal("Product nonexistent-product not found.", notFoundResult.Value);
        Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
        _basketRepository.Verify(x => x.AddItemToBasket(It.IsAny<string>(), It.IsAny<BasketItem>()), Times.Never);
    }
    
    [Fact]
    public void AddItem_ReturnsNotFound_WhenDiscountDoesNotExist()
    {
        // Arrange
        var discountId = "nonexistent-discount";
        var basketItem = new BasketItem { ProductId = _product.Id, Quantity = 1, DiscountId = discountId};

        _basketRepository.Setup(x => x.GetBasket(_basket.Id)).Returns(_basket);
        _productRepository.Setup(x => x.GetProductById(_product.Id)).Returns(_product);

        // Act
        var result = _basketController.AddItem(_basket.Id, new List<BasketItem> {basketItem});

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal("Discount nonexistent-discount not found.", notFoundResult.Value);
        Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
        _basketRepository.Verify(x => x.AddItemToBasket(It.IsAny<string>(), It.IsAny<BasketItem>()), Times.Never);
    }

    [Fact]
    public void GetBasketTotal_ReturnsTotal_WhenBasketExists()
    {
        // Arrange
        var basketItem = new BasketItem { ProductId = _product.Id, Quantity = 2 };
        _basket.Items = new List<BasketItem> { basketItem };
        var expectedTotal = 25.50m;

        _basketRepository.Setup(x => x.GetBasket(_basket.Id)).Returns(_basket);
        _basketCalculationService.Setup(x => x.CalculateTotalWithVat(_basket, 0.2m)).Returns(expectedTotal);

        // Act
        var result = _basketController.GetBasketTotal(_basket.Id);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(expectedTotal, okResult.Value);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
        _basketCalculationService.Verify(x => x.CalculateTotalWithVat(_basket, 0.2m), Times.Once);
    }

    [Fact]
    public void GetBasketTotal_ReturnsNotFound_WhenBasketDoesNotExist()
    {
        // Arrange
        var basketId = "nonexistent-basket";
        _basketRepository.Setup(x => x.GetBasket(basketId)).Returns((Basket)null);

        // Act
        var result = _basketController.GetBasketTotal(basketId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal("Basket not found.", notFoundResult.Value);
        Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
        _basketCalculationService.Verify(x => x.CalculateTotalWithVat(It.IsAny<Basket>(), 0.2m), Times.Never);
    }

    [Fact]
    public void GetBasketTotal_ReturnsBadRequest_WhenShippingToIsNull()
    {
        // Arrange
        var basketItem = new BasketItem { ProductId = _product.Id, Quantity = 2 };
        _basket.Items = new List<BasketItem> { basketItem };
        _basket.ShippingTo = null;

        _basketRepository.Setup(x => x.GetBasket(_basket.Id)).Returns(_basket);

        // Act
        var result = _basketController.GetBasketTotal(_basket.Id);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Add a shipping destination before calculating total.", badRequestResult.Value);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
        _basketCalculationService.Verify(x => x.CalculateTotalWithVat(It.IsAny<Basket>(), 0.2m), Times.Never);
    }

    [Fact]
    public void GetBasketTotalWithoutVat_ReturnsTotal_WhenBasketExists()
    {
        // Arrange
        var basketItem = new BasketItem { ProductId = _product.Id, Quantity = 2 };
        _basket.Items = new List<BasketItem> { basketItem };
        var expectedTotal = 20.00m;

        _basketRepository.Setup(x => x.GetBasket(_basket.Id)).Returns(_basket);
        _basketCalculationService.Setup(x => x.CalculateTotal(_basket)).Returns(expectedTotal);

        // Act
        var result = _basketController.GetBasketTotalWithoutVat(_basket.Id);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(expectedTotal, okResult.Value);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
        _basketCalculationService.Verify(x => x.CalculateTotal(_basket), Times.Once);
    }

    [Fact]
    public void GetBasketTotalWithoutVat_ReturnsNotFound_WhenBasketDoesNotExist()
    {
        // Arrange
        var basketId = "nonexistent-basket";
        _basketRepository.Setup(x => x.GetBasket(basketId)).Returns((Basket)null);

        // Act
        var result = _basketController.GetBasketTotalWithoutVat(basketId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal("Basket not found.", notFoundResult.Value);
        Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
        _basketCalculationService.Verify(x => x.CalculateTotal(It.IsAny<Basket>()), Times.Never);
    }

    [Fact]
    public void GetBasketTotalWithoutVat_ReturnsBadRequest_WhenShippingToIsNull()
    {
        // Arrange
        var basketItem = new BasketItem { ProductId = _product.Id, Quantity = 2 };
        _basket.Items = new List<BasketItem> { basketItem };
        _basket.ShippingTo = null;

        _basketRepository.Setup(x => x.GetBasket(_basket.Id)).Returns(_basket);

        // Act
        var result = _basketController.GetBasketTotalWithoutVat(_basket.Id);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Add a shipping destination before calculating total.", badRequestResult.Value);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
        _basketCalculationService.Verify(x => x.CalculateTotal(It.IsAny<Basket>()), Times.Never);
    }

    [Fact]
    public void AddDiscountToBasket_ReturnsOk_WhenDiscountAddedSuccessfully()
    {
        // Arrange
        var discountId = "discount123";
        var discount = new Discount { Id = discountId };

        _basketRepository.Setup(x => x.GetBasket(_basket.Id)).Returns(_basket);
        _discountRepository.Setup(x => x.GetDiscountById(discountId)).Returns(discount);

        // Act
        var result = _basketController.AddDiscountToBasket(_basket.Id, discountId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(_basket, okResult.Value);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
        Assert.Equal(discountId, _basket.DiscountId);
        _basketRepository.Verify(x => x.SaveBasket(_basket), Times.Once);
    }

    [Fact]
    public void AddDiscountToBasket_ReturnsNotFound_WhenBasketDoesNotExist()
    {
        // Arrange
        var basketId = "nonexistent-basket";
        var discountId = "discount123";

        _basketRepository.Setup(x => x.GetBasket(basketId)).Returns((Basket)null);

        // Act
        var result = _basketController.AddDiscountToBasket(basketId, discountId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal("Basket not found.", notFoundResult.Value);
        Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
        _basketRepository.Verify(x => x.SaveBasket(It.IsAny<Basket>()), Times.Never);
    }

    [Fact]
    public void AddDiscountToBasket_ReturnsNotFound_WhenDiscountDoesNotExist()
    {
        // Arrange
        var discountId = "nonexistent-discount";

        _basketRepository.Setup(x => x.GetBasket(_basket.Id)).Returns(_basket);
        _discountRepository.Setup(x => x.GetDiscountById(discountId)).Returns((Discount)null);

        // Act
        var result = _basketController.AddDiscountToBasket(_basket.Id, discountId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal($"Discount not found {discountId}", notFoundResult.Value);
        Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
        _basketRepository.Verify(x => x.SaveBasket(It.IsAny<Basket>()), Times.Never);
    }

    [Fact]
    public void DeleteItem_ReturnsOk_WhenItemDeletedSuccessfully()
    {
        // Arrange
        var basketItem = new BasketItem { ProductId = _product.Id, Quantity = 2 };
        _basket.Items = new List<BasketItem> { basketItem };

        _basketRepository.Setup(x => x.GetBasket(_basket.Id)).Returns(_basket);

        // Act
        var result = _basketController.DeleteItem(_basket.Id, _product.Id);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(_basket, okResult.Value);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
        Assert.Empty(_basket.Items);
        _basketRepository.Verify(x => x.SaveBasket(_basket), Times.Once);
    }

    [Fact]
    public void DeleteItem_ReturnsNotFound_WhenBasketDoesNotExist()
    {
        // Arrange
        var basketId = "nonexistent-basket";
        var productId = "product123";

        _basketRepository.Setup(x => x.GetBasket(basketId)).Returns((Basket)null);

        // Act
        var result = _basketController.DeleteItem(basketId, productId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal("Basket not found.", notFoundResult.Value);
        Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
        _basketRepository.Verify(x => x.SaveBasket(It.IsAny<Basket>()), Times.Never);
    }

    [Fact]
    public void DeleteItem_ReturnsNotFound_WhenProductNotInBasket()
    {
        // Arrange
        var productId = "nonexistent-product";
        var basketItem = new BasketItem { ProductId = "different-product", Quantity = 1 };
        _basket.Items = new List<BasketItem> { basketItem };

        _basketRepository.Setup(x => x.GetBasket(_basket.Id)).Returns(_basket);

        // Act
        var result = _basketController.DeleteItem(_basket.Id, productId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal($"Trying to remove product which does not exist in order {productId}", notFoundResult.Value);
        Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
        _basketRepository.Verify(x => x.SaveBasket(It.IsAny<Basket>()), Times.Never);
    }

    [Fact]
    public void DeleteItem_RemovesCorrectItem_WhenMultipleItemsInBasket()
    {
        // Arrange
        var basketItem1 = new BasketItem { ProductId = "product1", Quantity = 1 };
        var basketItem2 = new BasketItem { ProductId = "product2", Quantity = 2 };
        _basket.Items = new List<BasketItem> { basketItem1, basketItem2 };

        _basketRepository.Setup(x => x.GetBasket(_basket.Id)).Returns(_basket);

        // Act
        var result = _basketController.DeleteItem(_basket.Id, "product1");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(_basket, okResult.Value);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
        Assert.Single(_basket.Items);
        Assert.Equal("product2", _basket.Items.First().ProductId);
        _basketRepository.Verify(x => x.SaveBasket(_basket), Times.Once);
    }
}