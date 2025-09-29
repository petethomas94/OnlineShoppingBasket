# Online Shopping Basket

A .NET Core Web API solution for managing shopping baskets with support for products, discounts, and multi country shipping. The solution currently uses an in memory storage mechanism so persistence between sessions is not supported.

## Solution Structure
- `OnlineShoppingBasket.Api` - All controllers and model validation
- `OnlineShoppingBasket.Core` - Repository interfaces and business logic around calculating totals
- `OnlineShoppingBasket.Infrastructure` - Repository implementations - currently just in memory
- `OnlineShoppingBasket.Models` - Data models - currently one model for controllers and data layer
- `Tests` - Solution folder containing unit tests for business logic

## Running the API

To run the code you can either run the API project from within visual studio or run the commands below.


```bash

cd OnlineShoppingBasket
dotnet restore
dotnet build
dotnet run
```
Tests can be run from within visual studio or with

```bash
dotnet test
```

The API will be available at `https://localhost:7213` (HTTPS) or `http://localhost:5233` (HTTP). Swagger is available at `/swagger/index.html` for testing purposes.

## API Endpoints

### Basket Operations

- `POST /api/basket` - Create a new basket
- `GET /api/basket/{basketId}` - Get basket details
- `POST /api/basket/{basketId}/items` - Add item to basket 
  - Set quantity if adding multiple items
  - Call `GET /api/product` to retrieve valid product
  - Call `GET /api/discount` to retrieve valid discount
  - Sample payload
    ```bash
    [
      {
        "productId": "11111111-1111-1111-1111-111111111111",
        "quantity": 2,
        "discountId": "1"
      },
      {
        "productId": "22222222-2222-2222-2222-222222222222",
        "quantity": 2
      }
    ]
    ```
- `DELETE /api/basket/{basketId}/items/{productId}` - Delete item from basket
- `GET /api/basket/{basketId}/total` - Get basket total with VAT
- `GET /api/basket/{basketId}/totalWithoutVat` - Get basket total without VAT
- `POST /api/basket/{basketId}/discount/{discountId}` - Apply discount to basket 
  - Call `GET /api/discount` to retrieve valid discount
- `POST /api/basket/{basketId}/shippingcost/{country}` - Apply discount to basket
  - Call `GET /api/shippingdiscount` to retrieve shipping rates per country

## Current limitations

- Data model is tightly coupled between controllers and data layer. 
  - Adding response and request objects per endpoint would allow for more robust input validation.
  - Adding separate data model would allow for more complex data model objects.
- Only percentage based discounts are supported. The discount model could be extended to include amount based discounts eg "£5 off".
- In memory storage solution is not thread safe meaning multiple users could modify the same basket.
- No authentication meaning users can access all baskets.

