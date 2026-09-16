namespace Catalogue;

public sealed class OrderItem
{
    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; } = string.Empty;
    public decimal UnitPrice { get; private set; }
    public int Quantity { get; private set; }

    private OrderItem() { }

    internal OrderItem(Guid orderId, Guid productId, string name, decimal price, int quantity)
    {
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));
        if (price < 0) throw new ArgumentOutOfRangeException(nameof(price));
        Id = Guid.NewGuid();
        OrderId = orderId;
        ProductId = productId;
        ProductName = name;
        UnitPrice = price;
        Quantity = quantity;
    }
}
