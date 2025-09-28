using Microsoft.AspNetCore.Mvc;
using OnlineShoppingBasket.Core;
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
        return _discountRepository.GetAllDiscounts();
    }
}