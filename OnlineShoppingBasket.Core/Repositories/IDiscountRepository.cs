using OnlineShoppingBasket.Models;

namespace OnlineShoppingBasket.Core.Repositories;

public interface IDiscountRepository
{
    List<Discount> GetAllDiscounts();
    Discount? GetDiscountById(string discountId);
    Dictionary<string, Discount> GetDiscountsById(IEnumerable<string> ids);
}