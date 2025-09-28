using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using OnlineShoppingBasket.Api.Controllers;
using OnlineShoppingBasket.Core;
using OnlineShoppingBasket.Models;

namespace OnlineShoppingBasket.Api.Tests;

public class BasketControllerTests
{
    private readonly BasketController _basketController;

    private readonly Mock<IBasketRepository> _basketRepository = new Mock<IBasketRepository>();
    private readonly Mock<IProductRepository> _productRepository = new Mock<IProductRepository>();
    
    public BasketControllerTests()
    {
        _basketController = new BasketController(_basketRepository.Object, _productRepository.Object);
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
        var id = Guid.NewGuid().ToString();
        
        var basket = new Basket()
        {
            Id = id
        };

        _basketRepository.Setup(x => x.GetBasket(id)).Returns(basket);

        var result = _basketController.GetBasket(id);
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
        var basketId = Guid.NewGuid().ToString();
        var productId = "product123";
        var basket = new Basket { Id = basketId };
        var product = new Product { Id = productId };
        var basketItem = new BasketItem { ProductId = productId, Quantity = 2 };

        _basketRepository.Setup(x => x.GetBasket(basketId)).Returns(basket);
        _productRepository.Setup(x => x.GetProductById(productId)).Returns(product);

        // Act
        var result = _basketController.AddItem(basketId, basketItem);

        // Assert
        var okResult = Assert.IsType<OkResult>(result.Result);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
        _basketRepository.Verify(x => x.AddItemToBasket(basketId, basketItem), Times.Once);
    }

    [Fact]
    public void AddItem_ReturnsBadRequest_WhenQuantityIsZero()
    {
        // Arrange
        var basketId = Guid.NewGuid().ToString();
        var basketItem = new BasketItem { ProductId = "product123", Quantity = 0 };

        // Act
        var result = _basketController.AddItem(basketId, basketItem);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Quantity must be greater than 0.", badRequestResult.Value);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
        _basketRepository.Verify(x => x.AddItemToBasket(It.IsAny<string>(), It.IsAny<BasketItem>()), Times.Never);
    }

    [Fact]
    public void AddItem_ReturnsBadRequest_WhenQuantityIsNegative()
    {
        // Arrange
        var basketId = Guid.NewGuid().ToString();
        var basketItem = new BasketItem { ProductId = "product123", Quantity = -1 };

        // Act
        var result = _basketController.AddItem(basketId, basketItem);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Quantity must be greater than 0.", badRequestResult.Value);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
        _basketRepository.Verify(x => x.AddItemToBasket(It.IsAny<string>(), It.IsAny<BasketItem>()), Times.Never);
    }

    [Fact]
    public void AddItem_ReturnsNotFound_WhenBasketDoesNotExist()
    {
        // Arrange
        var basketId = "nonexistent-basket";
        var basketItem = new BasketItem { ProductId = "product123", Quantity = 1 };

        _basketRepository.Setup(x => x.GetBasket(basketId)).Returns((Basket)null);

        // Act
        var result = _basketController.AddItem(basketId, basketItem);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal("Product not found.", notFoundResult.Value);
        Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
        _basketRepository.Verify(x => x.AddItemToBasket(It.IsAny<string>(), It.IsAny<BasketItem>()), Times.Never);
    }

    [Fact]
    public void AddItem_ReturnsNotFound_WhenProductDoesNotExist()
    {
        // Arrange
        var basketId = Guid.NewGuid().ToString();
        var productId = "nonexistent-product";
        var basket = new Basket { Id = basketId };
        var basketItem = new BasketItem { ProductId = productId, Quantity = 1 };

        _basketRepository.Setup(x => x.GetBasket(basketId)).Returns(basket);
        _productRepository.Setup(x => x.GetProductById(productId)).Returns((Product)null);

        // Act
        var result = _basketController.AddItem(basketId, basketItem);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal("Product not found.", notFoundResult.Value);
        Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
        _basketRepository.Verify(x => x.AddItemToBasket(It.IsAny<string>(), It.IsAny<BasketItem>()), Times.Never);
    }
}