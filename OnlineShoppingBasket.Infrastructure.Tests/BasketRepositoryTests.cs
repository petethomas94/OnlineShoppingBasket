using OnlineShoppingBasket.Infrastructure.InMemoryRepositories;
using OnlineShoppingBasket.Models;

namespace OnlineShoppingBasket.Infrastructure.Tests;

public class BasketRepositoryTests
{
    private readonly BasketRepository _basketRepository;

    public BasketRepositoryTests()
    {
        _basketRepository = new BasketRepository();
    }

    [Fact]
    public void AddItemToBasket_SumsQuantity_WhenSameItemAddedMultipleTimes()
    {
        // Arrange
        var basket = new Basket { Id = "basket1" };
        _basketRepository.SaveBasket(basket);
        
        var basketItem1 = new BasketItem { ProductId = "product1", Quantity = 2 };
        var basketItem2 = new BasketItem { ProductId = "product1", Quantity = 3 };

        // Act
        _basketRepository.AddItemToBasket(basket.Id, basketItem1);
        _basketRepository.AddItemToBasket(basket.Id, basketItem2);

        // Assert
        var updatedBasket = _basketRepository.GetBasket(basket.Id);
        var item = updatedBasket.Items.FirstOrDefault(x => x.ProductId == "product1");
        
        Assert.NotNull(item);
        Assert.Equal(5, item.Quantity); // Should sum: 2 + 3 = 5
        Assert.Single(updatedBasket.Items); // Should only have one item, not two separate entries
    }

    [Fact]
    public void AddItemToBasket_AddsNewItem_WhenBasketIsEmpty()
    {
        // Arrange
        var basket = new Basket { Id = "basket1" };
        _basketRepository.SaveBasket(basket);
        
        var basketItem = new BasketItem { ProductId = "product1", Quantity = 2 };

        // Act
        _basketRepository.AddItemToBasket(basket.Id, basketItem);

        // Assert
        var updatedBasket = _basketRepository.GetBasket(basket.Id);
        Assert.Single(updatedBasket.Items);
        Assert.Equal("product1", updatedBasket.Items.First().ProductId);
        Assert.Equal(2, updatedBasket.Items.First().Quantity);
    }

    [Fact]
    public void AddItemToBasket_AddsSeparateItems_WhenDifferentProducts()
    {
        // Arrange
        var basket = new Basket { Id = "basket1" };
        _basketRepository.SaveBasket(basket);
        
        var basketItem1 = new BasketItem { ProductId = "product1", Quantity = 2 };
        var basketItem2 = new BasketItem { ProductId = "product2", Quantity = 3 };

        // Act
        _basketRepository.AddItemToBasket(basket.Id, basketItem1);
        _basketRepository.AddItemToBasket(basket.Id, basketItem2);

        // Assert
        var updatedBasket = _basketRepository.GetBasket(basket.Id);
        Assert.Equal(2, updatedBasket.Items.Count);
        
        var item1 = updatedBasket.Items.FirstOrDefault(x => x.ProductId == "product1");
        var item2 = updatedBasket.Items.FirstOrDefault(x => x.ProductId == "product2");
        
        Assert.NotNull(item1);
        Assert.NotNull(item2);
        Assert.Equal(2, item1.Quantity);
        Assert.Equal(3, item2.Quantity);
    }

    [Fact]
    public void SaveBasket_StoresBasket_CanBeRetrievedById()
    {
        // Arrange
        var basket = new Basket { Id = "basket1" };

        // Act
        _basketRepository.SaveBasket(basket);
        var retrievedBasket = _basketRepository.GetBasket(basket.Id);

        // Assert
        Assert.NotNull(retrievedBasket);
        Assert.Equal(basket.Id, retrievedBasket.Id);
    }

    [Fact]
    public void GetBasket_ReturnsNull_WhenBasketDoesNotExist()
    {
        // Act
        var basket = _basketRepository.GetBasket("nonexistent-basket");

        // Assert
        Assert.Null(basket);
    }
}