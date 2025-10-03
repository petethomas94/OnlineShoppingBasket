using OnlineShoppingBasket.Core.Repositories;
using OnlineShoppingBasket.Models;

namespace OnlineShoppingBasket.Infrastructure.InMemoryRepositories;

public class DiscountRepository : IDiscountRepository
{
    private readonly Dictionary<string, Discount> _discounts = new()
    {
        {
            "1", new Discount()
            {
                Name = "5% Off",
                Percentage = 5m,
                Id = "1"
            }
        }
    };

    public List<Discount> GetAllDiscounts()
    {
        return _discounts.Values.ToList();
    }

    public Discount? GetDiscountById(string discountId)
    {
        return _discounts.GetValueOrDefault(discountId);
    }

    public Dictionary<string, Discount> GetDiscountsById(IEnumerable<string> ids)
    {
        return _discounts.Where(p => ids.Contains(p.Key)).ToDictionary(p => p.Key, p => p.Value);
    }
}