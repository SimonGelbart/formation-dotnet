using Catalogue;
using Xunit;

public sealed class ProductServiceTests
{
    [Fact]
    public async Task CreateSavesProduct()
    {
        var repository = new FakeProductRepository();
        var service = new ProductService(repository);
        var product = await service.CreateAsync("Clavier", 30m, CancellationToken.None);
        Assert.Equal(product.Id, Assert.Single(repository.Saved).Id);
    }

    private sealed class FakeProductRepository : IProductRepository
    {
        private readonly List<Product> _pending = new();
        public List<Product> Saved { get; } = new();
        public void Add(Product product) => _pending.Add(product);
        public void Remove(Product product) => Saved.Remove(product);
        public Task<List<Product>> GetAllAsync(CancellationToken ct) => Task.FromResult(Saved.ToList());
        public Task<Product?> GetByIdAsync(Guid id, CancellationToken ct)
            => Task.FromResult(Saved.FirstOrDefault(p => p.Id == id));
        public Task SaveChangesAsync(CancellationToken ct)
        {
            Saved.AddRange(_pending);
            _pending.Clear();
            return Task.CompletedTask;
        }
    }
}
