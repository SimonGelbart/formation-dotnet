namespace Catalogue;

public interface IProductRepository
{
    IReadOnlyList<Product> GetAll();
    Product? GetById(Guid id);
    void Add(Product product);
    bool Remove(Guid id);
}
