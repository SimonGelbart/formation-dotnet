namespace Catalogue;

public enum OrderStatus { Draft, Confirmed }

public sealed class Order
{
    private readonly List<OrderItem> _items = new();
    public Guid Id { get; private set; }
    public OrderStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();
    public decimal Total => _items.Sum(item => item.UnitPrice * item.Quantity);

    public Order()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        Status = OrderStatus.Draft;
    }

    public void AddItem(Product product, int quantity)
    {
        if (Status != OrderStatus.Draft)
            throw new OrderConflictException("Une commande confirmée ne peut plus être modifiée.");
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity));
        _items.Add(new OrderItem(Id, product.Id, product.Name, product.Price, quantity));
    }

    public void Confirm()
    {
        if (Status != OrderStatus.Draft)
            throw new OrderConflictException("Cette commande est déjà confirmée.");
        if (_items.Count == 0)
            throw new OrderConflictException("Une commande vide ne peut pas être confirmée.");
        Status = OrderStatus.Confirmed;
    }
}

public sealed class OrderConflictException : Exception
{
    public OrderConflictException(string message) : base(message) { }
}
