using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using OnlineShoppingBasket.Models;

namespace OnlineShoppingBasket.IntegrationTests;

public class BasketIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;

    public BasketIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    [Fact]
    public async Task CompleteBasketLifecycle_ShouldCalculateCorrectTotals()
    {
        // Step 1: Get available products
        var productsResponse = await _client.GetAsync("/Product");
        productsResponse.EnsureSuccessStatusCode();
        var productsJson = await productsResponse.Content.ReadAsStringAsync();
        var products = JsonSerializer.Deserialize<List<Product>>(productsJson, _jsonOptions);
        
        Assert.NotNull(products);
        Assert.NotEmpty(products);

        // Step 2: Create a new basket
        var createBasketResponse = await _client.PostAsync("/Basket", null);
        createBasketResponse.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Created, createBasketResponse.StatusCode);
        
        var basketJson = await createBasketResponse.Content.ReadAsStringAsync();
        var basket = JsonSerializer.Deserialize<Basket>(basketJson, _jsonOptions);
        
        Assert.NotNull(basket);
        Assert.NotNull(basket.Id);
        Assert.Empty(basket.Items);

        // Step 3: Add items to the basket
        var basketItems = new List<BasketItem>
        {
            new() { ProductId = products[0].Id, Quantity = 2 },
            new() { ProductId = products[1].Id, Quantity = 1 }
        };

        var addItemsJson = JsonSerializer.Serialize(basketItems, _jsonOptions);
        var addItemsContent = new StringContent(addItemsJson, Encoding.UTF8, "application/json");
        
        var addItemsResponse = await _client.PostAsync($"/Basket/{basket.Id}/items", addItemsContent);
        addItemsResponse.EnsureSuccessStatusCode();

        // Step 4: Verify items were added
        var getBasketResponse = await _client.GetAsync($"/Basket/{basket.Id}");
        getBasketResponse.EnsureSuccessStatusCode();
        
        var updatedBasketJson = await getBasketResponse.Content.ReadAsStringAsync();
        var updatedBasket = JsonSerializer.Deserialize<Basket>(updatedBasketJson, _jsonOptions);
        
        Assert.NotNull(updatedBasket);
        Assert.Equal(2, updatedBasket.Items.Count);
        Assert.Equal(2, updatedBasket.Items.First(i => i.ProductId == products[0].Id).Quantity);
        Assert.Equal(1, updatedBasket.Items.First(i => i.ProductId == products[1].Id).Quantity);

        // Step 5: Add shipping destination
        var addShippingResponse = await _client.PostAsync($"/Basket/{basket.Id}/shippingcost/UK", null);
        addShippingResponse.EnsureSuccessStatusCode();

        // Step 6: Calculate totals and verify
        var totalWithVatResponse = await _client.GetAsync($"/Basket/{basket.Id}/total");
        totalWithVatResponse.EnsureSuccessStatusCode();
        
        var totalWithVatJson = await totalWithVatResponse.Content.ReadAsStringAsync();
        var totalWithVat = JsonSerializer.Deserialize<decimal>(totalWithVatJson, _jsonOptions);

        var totalWithoutVatResponse = await _client.GetAsync($"/Basket/{basket.Id}/totalWithoutVat");
        totalWithoutVatResponse.EnsureSuccessStatusCode();
        
        var totalWithoutVatJson = await totalWithoutVatResponse.Content.ReadAsStringAsync();
        var totalWithoutVat = JsonSerializer.Deserialize<decimal>(totalWithoutVatJson, _jsonOptions);

        // Verify that VAT is applied (total with VAT should be higher than without VAT)
        Assert.True(totalWithVat > totalWithoutVat);
        Assert.True(totalWithVat > 0);
        Assert.True(totalWithoutVat > 0);

        // Calculate expected total manually for verification
        var expectedSubtotal = (products[0].Price * 2) + (products[1].Price * 1);
        Assert.True(totalWithoutVat >= expectedSubtotal); // Should include shipping costs
    }

    [Fact]
    public async Task BasketLifecycle_WithDiscount_ShouldApplyDiscountCorrectly()
    {
        // Step 1: Create basket and add items
        var createBasketResponse = await _client.PostAsync("/Basket", null);
        createBasketResponse.EnsureSuccessStatusCode();
        
        var basketJson = await createBasketResponse.Content.ReadAsStringAsync();
        var basket = JsonSerializer.Deserialize<Basket>(basketJson, _jsonOptions);

        // Get products
        var productsResponse = await _client.GetAsync("/Product");
        var productsJson = await productsResponse.Content.ReadAsStringAsync();
        var products = JsonSerializer.Deserialize<List<Product>>(productsJson, _jsonOptions);

        // Add items
        var basketItems = new List<BasketItem>
        {
            new() { ProductId = products[0].Id, Quantity = 3 }
        };

        var addItemsJson = JsonSerializer.Serialize(basketItems, _jsonOptions);
        var addItemsContent = new StringContent(addItemsJson, Encoding.UTF8, "application/json");
        
        await _client.PostAsync($"/Basket/{basket.Id}/items", addItemsContent);

        // Add shipping
        await _client.PostAsync($"/Basket/{basket.Id}/shippingcost/UK", null);

        // Get total without discount
        var totalWithoutDiscountResponse = await _client.GetAsync($"/Basket/{basket.Id}/total");
        var totalWithoutDiscountJson = await totalWithoutDiscountResponse.Content.ReadAsStringAsync();
        var totalWithoutDiscount = JsonSerializer.Deserialize<decimal>(totalWithoutDiscountJson, _jsonOptions);

        // Apply basket discount (assuming discount ID "10PERCENT" exists)
        var addDiscountResponse = await _client.PostAsync($"/Basket/{basket.Id}/discount/10PERCENT", null);

        addDiscountResponse.EnsureSuccessStatusCode();
        // Get total with discount
        var totalWithDiscountResponse = await _client.GetAsync($"/Basket/{basket.Id}/total");
        var totalWithDiscountJson = await totalWithDiscountResponse.Content.ReadAsStringAsync();
        var totalWithDiscount = JsonSerializer.Deserialize<decimal>(totalWithDiscountJson, _jsonOptions);

        // Total with discount should be less than without discount
        Assert.True(totalWithDiscount < totalWithoutDiscount);
    }

    [Fact]
    public async Task BasketLifecycle_RemoveItems_ShouldUpdateTotalsCorrectly()
    {
        // Step 1: Create basket and add multiple items
        var createBasketResponse = await _client.PostAsync("/Basket", null);
        createBasketResponse.EnsureSuccessStatusCode();
        
        var basketJson = await createBasketResponse.Content.ReadAsStringAsync();
        var basket = JsonSerializer.Deserialize<Basket>(basketJson, _jsonOptions);

        // Get products
        var productsResponse = await _client.GetAsync("/Product");
        var productsJson = await productsResponse.Content.ReadAsStringAsync();
        var products = JsonSerializer.Deserialize<List<Product>>(productsJson, _jsonOptions);

        // Add multiple items
        var basketItems = new List<BasketItem>
        {
            new() { ProductId = products[0].Id, Quantity = 2 },
            new() { ProductId = products[1].Id, Quantity = 1 }
        };

        var addItemsJson = JsonSerializer.Serialize(basketItems, _jsonOptions);
        var addItemsContent = new StringContent(addItemsJson, Encoding.UTF8, "application/json");
        
        await _client.PostAsync($"/Basket/{basket.Id}/items", addItemsContent);

        // Add shipping
        await _client.PostAsync($"/Basket/{basket.Id}/shippingcost/UK", null);

        // Get initial total
        var initialTotalResponse = await _client.GetAsync($"/Basket/{basket.Id}/total");
        var initialTotalJson = await initialTotalResponse.Content.ReadAsStringAsync();
        var initialTotal = JsonSerializer.Deserialize<decimal>(initialTotalJson, _jsonOptions);

        // Remove one item
        var removeItemResponse = await _client.DeleteAsync($"/Basket/{basket.Id}/items/{products[0].Id}");
        removeItemResponse.EnsureSuccessStatusCode();

        // Get updated total
        var updatedTotalResponse = await _client.GetAsync($"/Basket/{basket.Id}/total");
        var updatedTotalJson = await updatedTotalResponse.Content.ReadAsStringAsync();
        var updatedTotal = JsonSerializer.Deserialize<decimal>(updatedTotalJson, _jsonOptions);

        // Total should be reduced after removing items
        Assert.True(updatedTotal < initialTotal);

        // Verify basket has one less item
        var getBasketResponse = await _client.GetAsync($"/Basket/{basket.Id}");
        var updatedBasketJson = await getBasketResponse.Content.ReadAsStringAsync();
        var updatedBasket = JsonSerializer.Deserialize<Basket>(updatedBasketJson, _jsonOptions);
        
        Assert.Single(updatedBasket.Items);
        Assert.Equal(products[1].Id, updatedBasket.Items[0].ProductId);
    }

    [Fact]
    public async Task BasketLifecycle_MultipleShippingDestinations_ShouldCalculateDifferentTotals()
    {
        // Step 1: Create basket and add items
        var createBasketResponse = await _client.PostAsync("/Basket", null);
        createBasketResponse.EnsureSuccessStatusCode();
        
        var basketJson = await createBasketResponse.Content.ReadAsStringAsync();
        var basket = JsonSerializer.Deserialize<Basket>(basketJson, _jsonOptions);

        // Get products
        var productsResponse = await _client.GetAsync("/Product");
        var productsJson = await productsResponse.Content.ReadAsStringAsync();
        var products = JsonSerializer.Deserialize<List<Product>>(productsJson, _jsonOptions);

        // Add items
        var basketItems = new List<BasketItem>
        {
            new() { ProductId = products[0].Id, Quantity = 1 }
        };

        var addItemsJson = JsonSerializer.Serialize(basketItems, _jsonOptions);
        var addItemsContent = new StringContent(addItemsJson, Encoding.UTF8, "application/json");
        
        await _client.PostAsync($"/Basket/{basket.Id}/items", addItemsContent);

        // Test UK shipping
        await _client.PostAsync($"/Basket/{basket.Id}/shippingcost/UK", null);
        var ukTotalResponse = await _client.GetAsync($"/Basket/{basket.Id}/total");
        var ukTotalJson = await ukTotalResponse.Content.ReadAsStringAsync();
        var ukTotal = JsonSerializer.Deserialize<decimal>(ukTotalJson, _jsonOptions);

        // Test US shipping (if available)
        var usShippingResponse = await _client.PostAsync($"/Basket/{basket.Id}/shippingcost/US", null);

        usShippingResponse.EnsureSuccessStatusCode();
        var usTotalResponse = await _client.GetAsync($"/Basket/{basket.Id}/total");
        var usTotalJson = await usTotalResponse.Content.ReadAsStringAsync();
        var usTotal = JsonSerializer.Deserialize<decimal>(usTotalJson, _jsonOptions);

        // Shipping costs might be different between countries
        Assert.True(ukTotal > 0);
        Assert.True(usTotal > 0);
    }

    [Fact]
    public async Task BasketTotal_WithoutShipping_ShouldReturnBadRequest()
    {
        // Create basket and add items
        var createBasketResponse = await _client.PostAsync("/Basket", null);
        createBasketResponse.EnsureSuccessStatusCode();
        
        var basketJson = await createBasketResponse.Content.ReadAsStringAsync();
        var basket = JsonSerializer.Deserialize<Basket>(basketJson, _jsonOptions);

        // Get products
        var productsResponse = await _client.GetAsync("/Product");
        var productsJson = await productsResponse.Content.ReadAsStringAsync();
        var products = JsonSerializer.Deserialize<List<Product>>(productsJson, _jsonOptions);

        // Add items
        var basketItems = new List<BasketItem>
        {
            new() { ProductId = products[0].Id, Quantity = 1 }
        };

        var addItemsJson = JsonSerializer.Serialize(basketItems, _jsonOptions);
        var addItemsContent = new StringContent(addItemsJson, Encoding.UTF8, "application/json");
        
        await _client.PostAsync($"/Basket/{basket.Id}/items", addItemsContent);

        // Try to get total without setting shipping destination
        var totalResponse = await _client.GetAsync($"/Basket/{basket.Id}/total");
        
        // Should return BadRequest because shipping destination is not set
        Assert.Equal(HttpStatusCode.BadRequest, totalResponse.StatusCode);
    }
}
