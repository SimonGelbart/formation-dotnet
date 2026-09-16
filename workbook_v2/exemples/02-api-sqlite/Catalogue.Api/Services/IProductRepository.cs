namespace Catalogue;

public interface IProductRepository
{
    Task<List<Product>> GetAllAsync(CancellationToken cancellationToken);
    Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    void Add(Product product);
    void Remove(Product product);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
