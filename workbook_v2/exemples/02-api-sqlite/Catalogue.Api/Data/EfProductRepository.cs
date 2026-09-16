using Microsoft.EntityFrameworkCore;

namespace Catalogue;

public sealed class EfProductRepository : IProductRepository
{
    private readonly AppDbContext _db;
    public EfProductRepository(AppDbContext db) => _db = db;

    public Task<List<Product>> GetAllAsync(CancellationToken cancellationToken)
        => _db.Products.AsNoTracking().OrderBy(p => p.Name).ToListAsync(cancellationToken);

    // Lecture suivie : le service peut modifier ce produit puis sauvegarder.
    public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => _db.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public void Add(Product product) => _db.Products.Add(product);
    public void Remove(Product product) => _db.Products.Remove(product);
    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _db.SaveChangesAsync(cancellationToken);
}
