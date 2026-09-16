namespace Catalogue;

public sealed class ProductService
{
    private readonly IProductRepository _repository;
    public ProductService(IProductRepository repository) => _repository = repository;

    public Task<List<Product>> GetAllAsync(CancellationToken ct) => _repository.GetAllAsync(ct);
    public Task<Product?> GetByIdAsync(Guid id, CancellationToken ct) => _repository.GetByIdAsync(id, ct);

    public async Task<Product> CreateAsync(string name, decimal price, CancellationToken ct)
    {
        var product = new Product(name, price);
        _repository.Add(product);
        await _repository.SaveChangesAsync(ct);
        return product;
    }

    public async Task<bool> UpdateAsync(Guid id, string name, decimal price, CancellationToken ct)
    {
        var product = await _repository.GetByIdAsync(id, ct);
        if (product is null) return false;
        product.Update(name, price);
        await _repository.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
    {
        var product = await _repository.GetByIdAsync(id, ct);
        if (product is null) return false;
        _repository.Remove(product);
        await _repository.SaveChangesAsync(ct);
        return true;
    }
}
