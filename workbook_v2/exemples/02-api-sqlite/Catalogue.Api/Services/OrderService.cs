using Microsoft.EntityFrameworkCore;

namespace Catalogue;

public sealed class OrderService
{
    private readonly AppDbContext _db;
    public OrderService(AppDbContext db) => _db = db;

    public async Task<Order> CreateAsync(CancellationToken ct)
    {
        var order = new Order();
        _db.Orders.Add(order);
        await _db.SaveChangesAsync(ct);
        return order;
    }

    public Task<Order?> GetByIdAsync(Guid id, CancellationToken ct)
        => _db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id, ct);

    public async Task<bool> AddItemAsync(Guid id, Guid productId, int quantity, CancellationToken ct)
    {
        var order = await GetByIdAsync(id, ct);
        var product = await _db.Products.FindAsync(new object[] { productId }, ct);
        if (order is null || product is null) return false;
        order.AddItem(product, quantity);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> ConfirmAsync(Guid id, CancellationToken ct)
    {
        var order = await GetByIdAsync(id, ct);
        if (order is null) return false;
        order.Confirm();
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
