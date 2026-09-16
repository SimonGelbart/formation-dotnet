namespace Catalogue;

// Support pédagogique pour des manipulations séquentielles en local.
// Le dictionnaire et ses objets ne constituent pas un stockage concurrent.
public sealed class MemoryProductRepository : IProductRepository
{
    private readonly Dictionary<Guid, Product> _products = new();

    public IReadOnlyList<Product> GetAll() => _products.Values.ToList();
    public Product? GetById(Guid id) => _products.GetValueOrDefault(id);
    public void Add(Product product) => _products.Add(product.Id, product);
    public bool Remove(Guid id) => _products.Remove(id);
}
