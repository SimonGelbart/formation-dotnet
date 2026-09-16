namespace Catalogue;

public sealed class Product
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public decimal Price { get; private set; }

    private Product() { } // Utilisé par EF dans la seconde application.

    public Product(string name, decimal price)
    {
        Id = Guid.NewGuid();
        Update(name, price);
    }

    public void Update(string name, decimal price)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Le nom est obligatoire.", nameof(name));
        if (price < 0)
            throw new ArgumentOutOfRangeException(nameof(price), "Le prix doit être positif ou nul.");
        Name = name.Trim();
        Price = price;
    }
}
