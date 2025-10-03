using Microsoft.AspNetCore.Mvc;
using OnlineShoppingBasket.Core.Repositories;
using OnlineShoppingBasket.Models;

namespace OnlineShoppingBasket.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class DiscountController : ControllerBase
{
    private readonly IDiscountRepository _discountRepository;
    
    public DiscountController(IDiscountRepository discountRepository)
    {
        _discountRepository = discountRepository;
    }

    [HttpGet]
    public ActionResult<IEnumerable<Discount>> GetDiscounts()
    {
        var discounts = _discountRepository.GetAllDiscounts();
        return Ok(discounts);
    }
}