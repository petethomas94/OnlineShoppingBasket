namespace OnlineShoppingBasket.Models;

public class Basket
{
    public Basket()
    {
        Items = new List<BasketItem>();
    }

    public string Id { get; set; }
    public List<BasketItem> Items { get; set; }
}

public class BasketItem
{
    public string ProductId { get; set; }
    public int Quantity { get; set; }
}

public class Product
{
    public string Id { get; set; }
    public decimal Price { get; set; }
    public string Name { get; set; }
}