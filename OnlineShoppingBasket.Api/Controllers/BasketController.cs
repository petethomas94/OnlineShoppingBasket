using Microsoft.AspNetCore.Mvc;
using OnlineShoppingBasket.Core.Repositories;
using OnlineShoppingBasket.Core.Services;
using OnlineShoppingBasket.Models;

namespace OnlineShoppingBasket.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class BasketController : ControllerBase
{
    private readonly IBasketRepository _basketRepository;
    private readonly IProductRepository _productRepository;
    private readonly IDiscountRepository _discountRepository;
    private readonly IShippingCostRepository _shippingCostRepository;
    private readonly IBasketCalculationService _basketCalculationService;

    public BasketController(
        IBasketRepository basketRepository, 
        IProductRepository productRepository,
        IDiscountRepository discountRepository,
        IShippingCostRepository shippingCostRepository,
        IBasketCalculationService basketCalculationService)
    {
        _basketRepository = basketRepository;
        _productRepository = productRepository;
        _discountRepository = discountRepository;
        _shippingCostRepository = shippingCostRepository;
        _basketCalculationService = basketCalculationService;
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
        return basket == null ? NotFound(ErrorMessages.BasketNotFound) : Ok(basket);
    }

    [HttpPost("{basketId}/items")]
    public ActionResult<Basket> AddItem(string basketId, [FromBody] IEnumerable<BasketItem> basketItems)
    {
        var basket = _basketRepository.GetBasket(basketId);
        if (basket == null)
        {
            return NotFound(ErrorMessages.BasketNotFound);
        }

        if (basketItems == null || !basketItems.Any())
        {
            return BadRequest(ErrorMessages.NoItemsProvided);
        }

        var validationErrors = ValidateBasketItems(basketItems);

        if (validationErrors.Any())
        {
            return BadRequest(string.Join(", ", validationErrors));
        }

        foreach (var basketItem in basketItems)
        {
            _basketRepository.AddItemToBasket(basketId, basketItem);
        }

        return Ok(_basketRepository.GetBasket(basketId));
    }

    [HttpDelete("{basketId}/items/{productId}")]
    public ActionResult<Basket> DeleteItem(string basketId, string productId)
    {
        var basket = _basketRepository.GetBasket(basketId);
        if (basket == null)
        {
            return NotFound(ErrorMessages.BasketNotFound);
        }

        if (basket.Items.All(x => x.ProductId != productId))
        {
            return NotFound(string.Format(ErrorMessages.ProductNotFoundInBasket, productId));
        }

        _basketRepository.RemoveItemFromBasket(basketId, productId);
        return Ok(_basketRepository.GetBasket(basketId));
    }

    [HttpGet("{basketId}/total")]
    public ActionResult<decimal> GetBasketTotal(string basketId)
    {
        var basket = _basketRepository.GetBasket(basketId);
        if (basket == null)
        {
            return NotFound(ErrorMessages.BasketNotFound);
        }
        if (string.IsNullOrEmpty(basket.ShippingTo))
        {
            return BadRequest(ErrorMessages.MissingShippingDestination);
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
            return NotFound(ErrorMessages.BasketNotFound);
        }
        if (string.IsNullOrEmpty(basket.ShippingTo))
        {
            return BadRequest(ErrorMessages.MissingShippingDestination);
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
            return NotFound(ErrorMessages.BasketNotFound);
        }

        var discount = _discountRepository.GetDiscountById(discountId);
        if (discount == null)
        {
            return NotFound(string.Format(ErrorMessages.DiscountNotFound, discountId));
        }

        basket.DiscountId = discountId;
        _basketRepository.SaveBasket(basket);
        return Ok(basket);
    }

    [HttpPost("{basketId}/shippingcost/{country}")]
    public ActionResult<Basket> AddShippingCostToBasket(string basketId, string country)
    {
        var basket = _basketRepository.GetBasket(basketId);
        if (basket == null)
        {
            return NotFound(ErrorMessages.BasketNotFound);
        }

        var shippingCost = _shippingCostRepository.GetShippingCostByCountry(country);
        if (shippingCost == null)
        {
            return NotFound(string.Format(ErrorMessages.UnsupportedShipping, country));
        }

        basket.ShippingTo = country;
        _basketRepository.SaveBasket(basket);
        return Ok(basket);
    }

    private List<string> ValidateBasketItems(IEnumerable<BasketItem> basketItems)
    {
        var validationErrors = new List<string>();
        foreach (var basketItem in basketItems)
        {
            if (basketItem.Quantity <= 0)
            {
                validationErrors.Add(string.Format(ErrorMessages.InvalidQuantity, basketItem.ProductId));
            }

            var product = _productRepository.GetProductById(basketItem.ProductId);
            if (product == null)
            {
                validationErrors.Add(string.Format(ErrorMessages.ProductNotFound, basketItem.ProductId));
            }

            if (string.IsNullOrEmpty(basketItem.DiscountId))
            {
                continue;    
            }
            var discount = _discountRepository.GetDiscountById(basketItem.DiscountId);
            if (discount == null)
            {
                validationErrors.Add(string.Format(ErrorMessages.DiscountNotFound, basketItem.DiscountId));
            }
        }

        return validationErrors;
    }
}