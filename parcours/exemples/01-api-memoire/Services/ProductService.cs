namespace Catalogue;

public sealed class ProductService
{
    private readonly IProductRepository _repository;
    public ProductService(IProductRepository repository) => _repository = repository;

    public IReadOnlyList<Product> GetAll() => _repository.GetAll();
    public Product? GetById(Guid id) => _repository.GetById(id);

    public Product Create(string name, decimal price)
    {
        var product = new Product(name, price);
        _repository.Add(product);
        return product;
    }

    public bool Update(Guid id, string name, decimal price)
    {
        var product = _repository.GetById(id);
        if (product is null) return false;
        product.Update(name, price);
        return true;
    }

    public bool Delete(Guid id) => _repository.Remove(id);
}
