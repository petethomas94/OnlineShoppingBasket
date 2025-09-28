using Microsoft.AspNetCore.Mvc;
using OnlineShoppingBasket.Core;
using OnlineShoppingBasket.Models;

namespace OnlineShoppingBasket.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class BasketController : ControllerBase
{
    private readonly IBasketRepository _basketRepository;
    private readonly IProductRepository _productRepository;
    private readonly IDiscountRepository _discountRepository;
    private readonly IBasketCalculationService _basketCalculationService;

    public BasketController(
        IBasketRepository basketRepository, 
        IProductRepository productRepository,
        IBasketCalculationService basketCalculationService, 
        IDiscountRepository discountRepository)
    {
        _basketRepository = basketRepository;
        _productRepository = productRepository;
        _basketCalculationService = basketCalculationService;
        _discountRepository = discountRepository;
    }

    [HttpPost]
    public ActionResult<Basket> CreateBasket()
    {
        var basket = new Basket{ Id = Guid.NewGuid().ToString() };
        _basketRepository.SaveBasket(basket);
        return CreatedAtAction(nameof(GetBasket), new { basketId = basket.Id }, basket);
    }

    [HttpGet("{basketId}")]
    public ActionResult<Basket> GetBasket(string basketId)
    {
        var basket = _basketRepository.GetBasket(basketId);
        return basket == null ? NotFound("Basket not found.") : Ok(basket);
    }

    [HttpPost("{basketId}/items")]
    public ActionResult<Basket> AddItem(string basketId, [FromBody] IEnumerable<BasketItem> basketItems)
    {
        var basket = _basketRepository.GetBasket(basketId);
        if (basket == null)
        {
            return NotFound("Basket not found.");
        }

        foreach (var basketItem in basketItems)
        {
            if (basketItem.Quantity <= 0)
            {
                return BadRequest($"Quantity must be greater than 0 for product {basketItem.ProductId}.");
            }

            var product = _productRepository.GetProductById(basketItem.ProductId);
            if (product == null)
            {
                return NotFound($"Product {basketItem.ProductId} not found.");
            }

            if (basketItem.DiscountId != null)
            {
                var discount = _discountRepository.GetDiscountById(basketItem.DiscountId);
                if (discount == null)
                {
                    return NotFound($"Discount {basketItem.DiscountId} not found.");
                }    
            }

            _basketRepository.AddItemToBasket(basketId, basketItem);
        }

        return Ok(basket);
    }
    
    [HttpGet("{basketId}/total")]
    public ActionResult<decimal> GetBasketTotal(string basketId)
    {
        var basket = _basketRepository.GetBasket(basketId);
        if (basket == null)
        {
            return NotFound("Basket not found.");
        }

        var totalWithVat = _basketCalculationService.CalculateTotalWithVat(basket);
        return Ok(totalWithVat);
    }
    
    [HttpGet("{basketId}/totalWithoutVat")]
    public ActionResult<decimal> GetBasketTotalWithoutVat(string basketId)
    {
        var basket = _basketRepository.GetBasket(basketId);
        if (basket == null)
        {
            return NotFound("Basket not found.");
        }

        var totalWithoutVat = _basketCalculationService.CalculateTotal(basket);
        return Ok(totalWithoutVat);
    }

    [HttpPost("{basketId}/discount/{discountId}")]
    public ActionResult<Basket> AddDiscountToBasket(string basketId, string discountId)
    {
        var basket = _basketRepository.GetBasket(basketId);
        if (basket == null)
        {
            return NotFound("Basket not found.");
        }

        var discount = _discountRepository.GetDiscountById(discountId);
        if (discount == null)
        {
            return NotFound($"Discount not found {discountId}");
        }

        basket.DiscountId = discountId;
        _basketRepository.SaveBasket(basket);
        return Ok(basket);
    }
}