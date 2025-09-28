namespace OnlineShoppingBasket.Models;

public class Basket
{
    public Basket()
    {
        Items = new List<BasketItem>();
    }

    public string Id { get; set; }
    public List<BasketItem> Items { get; set; }
    public string DiscountId { get; set; }
    public string? ShippingTo { get; set; }
}