public sealed class Product
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = "";
    public decimal Price { get; private set; }
    private Product() { }
    public Product(string name, decimal price)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Nom obligatoire", nameof(name));
        if (price < 0) throw new ArgumentOutOfRangeException(nameof(price));
        Id = Guid.NewGuid();
        Name = name.Trim();
        Price = price;
    }
}
