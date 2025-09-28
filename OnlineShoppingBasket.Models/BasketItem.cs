namespace OnlineShoppingBasket.Models;

public class BasketItem
{
    public string ProductId { get; set; }
    public int Quantity { get; set; }
    public string? DiscountId { get; set; }
}