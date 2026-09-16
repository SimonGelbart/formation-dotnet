using Microsoft.EntityFrameworkCore;

public sealed class ProductService
{
    private readonly AppDbContext _db;
    public ProductService(AppDbContext db) => _db = db;

    public Task<List<Product>> GetAllAsync(CancellationToken ct)
        => _db.Products.AsNoTracking().OrderBy(p => p.Name).ToListAsync(ct);

    public Task<Product?> GetByIdAsync(Guid id, CancellationToken ct)
        => _db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<Product> CreateAsync(string name, decimal price, CancellationToken ct)
    {
        var product = new Product(name, price);
        _db.Products.Add(product);
        await _db.SaveChangesAsync(ct);
        return product;
    }
}
