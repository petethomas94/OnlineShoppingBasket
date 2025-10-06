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

    private readonly Basket _basket;
    private readonly Product _product;
    private readonly string _nonExistentBasketId = "nonexistent-basket";

    public BasketControllerTests()
    {
        _basketController = new BasketController(
            _basketRepository.Object, 
            _productRepository.Object, 
            _discountRepository.Object,
            _shippingCostRepository.Object,
            _basketCalculationService.Object);

        _basket = new Basket
        {
            Id = Guid.NewGuid().ToString(),
            ShippingTo = "GB",
            Items = new List<BasketItem>()
        };

        _product = new Product { Id = "product123" };
    }

    private void SetupBasketExists()
    {
        _basketRepository.Setup(x => x.GetBasket(_basket.Id)).Returns(_basket);
    }

    private void SetupProductExists()
    {
        _productRepository.Setup(x => x.GetProductById(_product.Id)).Returns(_product);
    }

    private void SetupBasketDoesNotExist()
    {
        _basketRepository.Setup(x => x.GetBasket(_nonExistentBasketId)).Returns((Basket)null);
    }

    #region CreateBasket Tests
    
    [Fact]
    public void CreateBasket_ReturnsNewBasket()
    {
        // Act
        var result = _basketController.CreateBasket();

        // Assert
        _basketRepository.Verify(x => x.SaveBasket(It.IsAny<Basket>()));
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var basket = Assert.IsType<Basket>(createdResult.Value);
        Assert.NotNull(basket.Id);
        Assert.Equal(StatusCodes.Status201Created, createdResult.StatusCode);
    }

    #endregion

    #region GetBasket Tests
    
    [Fact]
    public void GetBasket_ReturnsBasket_WhenBasketExists()
    {
        // Arrange
        SetupBasketExists();

        // Act
        var result = _basketController.GetBasket(_basket.Id);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var basketResult = Assert.IsType<Basket>(okResult.Value);
        Assert.Equal(_basket.Id, basketResult.Id);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
    }
    
    [Fact]
    public void GetBasket_ReturnsNotFound_WhenBasketDoesNotExist()
    {
        // Act
        var result = _basketController.GetBasket("nonexistent-id");
        
        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal("Basket not found.", notFoundResult.Value);
        Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
    }

    #endregion

    #region AddItem Tests

    [Fact]
    public void AddItem_ReturnsOk_WhenItemAddedSuccessfully()
    {
        // Arrange
        var basketItem = new BasketItem { ProductId = _product.Id, Quantity = 2 };
        var basketItems = new List<BasketItem> { basketItem };
        
        SetupBasketExists();
        SetupProductExists();
        
        // Act
        var result = _basketController.AddItem(_basket.Id, basketItems);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
        _basketRepository.Verify(x => x.AddItemToBasket(_basket.Id, basketItem), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AddItem_ReturnsBadRequest_WhenQuantityIsInvalid(int quantity)
    {
        // Arrange
        var basketItem = new BasketItem { ProductId = _product.Id, Quantity = quantity };
        var basketItems = new List<BasketItem> { basketItem };
        
        SetupBasketExists();
        SetupProductExists();
        
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
        var basketItem = new BasketItem { ProductId = _product.Id, Quantity = 1 };
        SetupBasketDoesNotExist();

        // Act
        var result = _basketController.AddItem(_nonExistentBasketId, new List<BasketItem> { basketItem });

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal("Basket not found.", notFoundResult.Value);
        Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
        _basketRepository.Verify(x => x.AddItemToBasket(It.IsAny<string>(), It.IsAny<BasketItem>()), Times.Never);
    }

    [Fact]
    public void AddItem_ReturnsBadRequest_WhenProductDoesNotExist()
    {
        // Arrange
        var productId = "nonexistent-product";
        var basketItem = new BasketItem { ProductId = productId, Quantity = 1 };

        SetupBasketExists();
        _productRepository.Setup(x => x.GetProductById(productId)).Returns((Product)null);

        // Act
        var result = _basketController.AddItem(_basket.Id, new List<BasketItem> { basketItem });

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Product nonexistent-product not found.", badRequestResult.Value);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
        _basketRepository.Verify(x => x.AddItemToBasket(It.IsAny<string>(), It.IsAny<BasketItem>()), Times.Never);
    }
    
    [Fact]
    public void AddItem_ReturnsBadRequest_WhenDiscountDoesNotExist()
    {
        // Arrange
        var discountId = "nonexistent-discount";
        var basketItem = new BasketItem { ProductId = _product.Id, Quantity = 1, DiscountId = discountId };

        SetupBasketExists();
        SetupProductExists();
        _discountRepository.Setup(x => x.GetDiscountById(discountId)).Returns((Discount)null);

        // Act
        var result = _basketController.AddItem(_basket.Id, new List<BasketItem> { basketItem });

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Discount nonexistent-discount not found.", badRequestResult.Value);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
        _basketRepository.Verify(x => x.AddItemToBasket(It.IsAny<string>(), It.IsAny<BasketItem>()), Times.Never);
    }

    [Fact]
    public void AddItem_ReturnsOk_WhenMultipleItemsAddedSuccessfully()
    {
        // Arrange
        var product2 = new Product { Id = "product456" };
        var basketItem1 = new BasketItem { ProductId = _product.Id, Quantity = 2 };
        var basketItem2 = new BasketItem { ProductId = product2.Id, Quantity = 1 };
        var basketItems = new List<BasketItem> { basketItem1, basketItem2 };
        
        SetupBasketExists();
        SetupProductExists();
        _productRepository.Setup(x => x.GetProductById(product2.Id)).Returns(product2);
        
        // Act
        var result = _basketController.AddItem(_basket.Id, basketItems);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
        _basketRepository.Verify(x => x.AddItemToBasket(_basket.Id, basketItem1), Times.Once);
        _basketRepository.Verify(x => x.AddItemToBasket(_basket.Id, basketItem2), Times.Once);
    }

    [Fact]
    public void AddItem_ReturnsBadRequest_WhenMultipleValidationErrors()
    {
        // Arrange
        var basketItem1 = new BasketItem { ProductId = "nonexistent-product", Quantity = 0 };
        var basketItem2 = new BasketItem { ProductId = _product.Id, Quantity = -1 };
        var basketItems = new List<BasketItem> { basketItem1, basketItem2 };
        
        SetupBasketExists();
        _productRepository.Setup(x => x.GetProductById("nonexistent-product")).Returns((Product)null);
        SetupProductExists();
        
        // Act
        var result = _basketController.AddItem(_basket.Id, basketItems);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var errorMessage = badRequestResult.Value as string;
        Assert.Contains("Quantity must be greater than 0 for product nonexistent-product", errorMessage);
        Assert.Contains("Product nonexistent-product not found", errorMessage);
        Assert.Contains("Quantity must be greater than 0 for product " + _product.Id, errorMessage);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
        _basketRepository.Verify(x => x.AddItemToBasket(It.IsAny<string>(), It.IsAny<BasketItem>()), Times.Never);
    }

    #endregion

    #region DeleteItem Tests

    [Fact]
    public void DeleteItem_ReturnsOk_WhenItemDeletedSuccessfully()
    {
        // Arrange
        var basketItem = new BasketItem { ProductId = _product.Id, Quantity = 2 };
        _basket.Items = new List<BasketItem> { basketItem };

        SetupBasketExists();

        // Act
        var result = _basketController.DeleteItem(_basket.Id, _product.Id);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(_basket, okResult.Value);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
        _basketRepository.Verify(x => x.RemoveItemFromBasket(_basket.Id, _product.Id), Times.Once);
    }

    [Fact]
    public void DeleteItem_ReturnsNotFound_WhenBasketDoesNotExist()
    {
        // Arrange
        SetupBasketDoesNotExist();

        // Act
        var result = _basketController.DeleteItem(_nonExistentBasketId, _product.Id);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal("Basket not found.", notFoundResult.Value);
        Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
        _basketRepository.Verify(x => x.RemoveItemFromBasket(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void DeleteItem_ReturnsNotFound_WhenProductNotInBasket()
    {
        // Arrange
        var productId = "nonexistent-product";
        var basketItem = new BasketItem { ProductId = "different-product", Quantity = 1 };
        _basket.Items = new List<BasketItem> { basketItem };

        SetupBasketExists();

        // Act
        var result = _basketController.DeleteItem(_basket.Id, productId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal($"Product {productId} not found in basket", notFoundResult.Value);
        Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
        _basketRepository.Verify(x => x.RemoveItemFromBasket(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    #endregion

    #region GetBasketTotal Tests

    [Fact]
    public void GetBasketTotal_ReturnsTotal_WhenBasketExists()
    {
        // Arrange
        var expectedTotal = 25.50m;
        SetupBasketExists();
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
        SetupBasketDoesNotExist();

        // Act
        var result = _basketController.GetBasketTotal(_nonExistentBasketId);

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
        _basket.ShippingTo = null;
        SetupBasketExists();

        // Act
        var result = _basketController.GetBasketTotal(_basket.Id);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Add a shipping destination before calculating total.", badRequestResult.Value);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
        _basketCalculationService.Verify(x => x.CalculateTotalWithVat(It.IsAny<Basket>(), 0.2m), Times.Never);
    }

    #endregion

    #region GetBasketTotalWithoutVat Tests

    [Fact]
    public void GetBasketTotalWithoutVat_ReturnsTotal_WhenBasketExists()
    {
        // Arrange
        var expectedTotal = 20.00m;
        SetupBasketExists();
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
        SetupBasketDoesNotExist();

        // Act
        var result = _basketController.GetBasketTotalWithoutVat(_nonExistentBasketId);

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
        _basket.ShippingTo = null;
        SetupBasketExists();

        // Act
        var result = _basketController.GetBasketTotalWithoutVat(_basket.Id);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Add a shipping destination before calculating total.", badRequestResult.Value);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
        _basketCalculationService.Verify(x => x.CalculateTotal(It.IsAny<Basket>()), Times.Never);
    }

    #endregion

    #region AddDiscountToBasket Tests

    [Fact]
    public void AddDiscountToBasket_ReturnsOk_WhenDiscountAddedSuccessfully()
    {
        // Arrange
        var discountId = "discount123";
        var discount = new Discount { Id = discountId };

        SetupBasketExists();
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
        var discountId = "discount123";
        SetupBasketDoesNotExist();

        // Act
        var result = _basketController.AddDiscountToBasket(_nonExistentBasketId, discountId);

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

        SetupBasketExists();
        _discountRepository.Setup(x => x.GetDiscountById(discountId)).Returns((Discount)null);

        // Act
        var result = _basketController.AddDiscountToBasket(_basket.Id, discountId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal($"Discount not found {discountId}", notFoundResult.Value);
        Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
        _basketRepository.Verify(x => x.SaveBasket(It.IsAny<Basket>()), Times.Never);
    }

    #endregion

    #region AddShippingCostToBasket Tests

    [Fact]
    public void AddShippingCostToBasket_ReturnsOk_WhenShippingCostAddedSuccessfully()
    {
        // Arrange
        var country = "US";
        var shippingCost = new ShippingCost { Country = country, Price = 15.00m };

        SetupBasketExists();
        _shippingCostRepository.Setup(x => x.GetShippingCostByCountry(country)).Returns(shippingCost);

        // Act
        var result = _basketController.AddShippingCostToBasket(_basket.Id, country);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(_basket, okResult.Value);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
        Assert.Equal(country, _basket.ShippingTo);
        _basketRepository.Verify(x => x.SaveBasket(_basket), Times.Once);
    }

    [Fact]
    public void AddShippingCostToBasket_ReturnsNotFound_WhenBasketDoesNotExist()
    {
        // Arrange
        var country = "US";
        SetupBasketDoesNotExist();

        // Act
        var result = _basketController.AddShippingCostToBasket(_nonExistentBasketId, country);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal("Basket not found.", notFoundResult.Value);
        Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
        _basketRepository.Verify(x => x.SaveBasket(It.IsAny<Basket>()), Times.Never);
    }

    [Fact]
    public void AddShippingCostToBasket_ReturnsNotFound_WhenShippingNotSupportedForCountry()
    {
        // Arrange
        var country = "UNSUPPORTED";

        SetupBasketExists();
        _shippingCostRepository.Setup(x => x.GetShippingCostByCountry(country)).Returns((ShippingCost)null);

        // Act
        var result = _basketController.AddShippingCostToBasket(_basket.Id, country);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal($"Shipping not supported for {country}", notFoundResult.Value);
        Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
        _basketRepository.Verify(x => x.SaveBasket(It.IsAny<Basket>()), Times.Never);
    }

    [Fact]
    public void AddShippingCostToBasket_UpdatesExistingShippingDestination_WhenBasketAlreadyHasShipping()
    {
        // Arrange
        var oldCountry = "FR";
        var newCountry = "DE";
        var newShippingCost = new ShippingCost { Country = newCountry, Price = 12.00m };
        
        _basket.ShippingTo = oldCountry;
        SetupBasketExists();
        _shippingCostRepository.Setup(x => x.GetShippingCostByCountry(newCountry)).Returns(newShippingCost);

        // Act
        var result = _basketController.AddShippingCostToBasket(_basket.Id, newCountry);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(_basket, okResult.Value);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
        Assert.Equal(newCountry, _basket.ShippingTo);
        _basketRepository.Verify(x => x.SaveBasket(_basket), Times.Once);
    }

    #endregion
}

