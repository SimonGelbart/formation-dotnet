public sealed class ProductService
{
    private readonly List<Product> _products = new();
    public IReadOnlyList<Product> GetAll() => _products;
    public Product? GetById(Guid id) => _products.FirstOrDefault(p => p.Id == id);
    public Product Create(string name, decimal price)
    {
        var product = new Product(name, price);
        _products.Add(product);
        return product;
    }
}
