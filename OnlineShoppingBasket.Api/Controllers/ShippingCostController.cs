using Microsoft.AspNetCore.Mvc;
using OnlineShoppingBasket.Core.Repositories;
using OnlineShoppingBasket.Models;

namespace OnlineShoppingBasket.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class ShippingCostController
{
    private readonly IShippingCostRepository _shippingCostRepository;

    public ShippingCostController(IShippingCostRepository shippingCostRepository)
    {
        _shippingCostRepository = shippingCostRepository;
    }
    
    [HttpGet]
    public ActionResult<IEnumerable<ShippingCost>> GetAllShippingCosts()
    {
        return _shippingCostRepository.GetAllShippingCosts();
    }   
}