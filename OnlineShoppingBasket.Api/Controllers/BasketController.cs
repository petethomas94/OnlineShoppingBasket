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

    public BasketController(IBasketRepository basketRepository, IProductRepository productRepository)
    {
        _basketRepository = basketRepository;
        _productRepository = productRepository;
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
        if (basket == null)
            return NotFound("Basket not found.");
        else
            return Ok(basket);
    }

    [HttpPost("{basketId}/items")]
    public ActionResult<Basket> AddItem(string basketId, [FromBody] BasketItem basketItem)
    {
        if (basketItem.Quantity <= 0)
        {
            return BadRequest("Quantity must be greater than 0.");
        }
        
        var basket = _basketRepository.GetBasket(basketId);
        if (basket == null)
        {
            return NotFound("Product not found.");
        }
        
        var product = _productRepository.GetProductById(basketItem.ProductId);
        if (product == null)
        {
            return NotFound("Product not found.");
        }
        
        _basketRepository.AddItemToBasket(basketId, basketItem);
        return Ok();
    }
}