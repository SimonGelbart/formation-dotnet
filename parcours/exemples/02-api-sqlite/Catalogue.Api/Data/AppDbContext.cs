using Microsoft.EntityFrameworkCore;

namespace Catalogue;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var order = modelBuilder.Entity<Order>();
        order.Ignore(o => o.Total);
        modelBuilder.Entity<OrderItem>().Property(i => i.Id).ValueGeneratedNever();
        order.HasMany(o => o.Items).WithOne().HasForeignKey(i => i.OrderId);
        order.Navigation(o => o.Items).HasField("_items")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
        // ProductId est une référence historique, sans navigation ni FK vers Products.
        // Supprimer un produit du catalogue conserve donc les anciennes lignes.
    }
}
