namespace OnlineShoppingBasket.Api;

public static class ErrorMessages
{
    public const string BasketNotFound = "Basket not found.";
    public const string MissingShippingDestination = "Add a shipping destination before calculating total.";
    public const string NoItemsProvided = "No items provided.";
    public const string DiscountNotFound = "Discount not found {0}.";
    public const string UnsupportedShipping = "Shipping not supported for {0}";
    public const string ProductNotFound = "Product {0} not found.";
    public const string InvalidQuantity = "Quantity must be greater than 0 for product {0}.";
    public const string ProductNotFoundInBasket = "Product {0} not found in basket";
}