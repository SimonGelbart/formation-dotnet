using System.Net;
using System.Net.Http.Json;
using Catalogue;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

public sealed class ApiTests
{
    [Fact]
    public async Task OrderFlowPersistsHistoricalPriceAndHandlesErrors()
    {
        // Une factory ET une connexion par test : aucun partage de données entre tests.
        using var factory = new TestApiFactory();
        using var client = factory.CreateClient();
        await factory.CreateDatabaseAsync();

        var invalid = await client.PostAsJsonAsync("/products", new { name = "Clavier", price = -1 });
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        var missing = await client.GetAsync($"/products/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        var missingConfirm = await client.PostAsync($"/orders/{Guid.NewGuid()}/confirm", null);
        Assert.Equal(HttpStatusCode.NotFound, missingConfirm.StatusCode);

        var createdProduct = await client.PostAsJsonAsync("/products", new { name = "Clavier", price = 30 });
        Assert.Equal(HttpStatusCode.Created, createdProduct.StatusCode);
        var product = (await createdProduct.Content.ReadFromJsonAsync<ProductResponse>())!;
        var createdOrder = await client.PostAsync("/orders", null);
        Assert.Equal(HttpStatusCode.Created, createdOrder.StatusCode);
        var order = (await createdOrder.Content.ReadFromJsonAsync<OrderResponse>())!;
        Assert.NotNull(createdOrder.Headers.Location);

        var emptyConfirm = await client.PostAsync($"/orders/{order.Id}/confirm", null);
        Assert.Equal(HttpStatusCode.Conflict, emptyConfirm.StatusCode);
        var added = await client.PostAsJsonAsync($"/orders/{order.Id}/items", new { productId = product.Id, quantity = 2 });
        Assert.Equal(HttpStatusCode.NoContent, added.StatusCode);
        var updated = await client.PutAsJsonAsync($"/products/{product.Id}", new { name = "Clavier", price = 50 });
        Assert.Equal(HttpStatusCode.NoContent, updated.StatusCode);
        var confirmed = await client.PostAsync($"/orders/{order.Id}/confirm", null);
        Assert.Equal(HttpStatusCode.NoContent, confirmed.StatusCode);

        var read = await client.GetAsync(createdOrder.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, read.StatusCode);
        var persisted = (await read.Content.ReadFromJsonAsync<OrderResponse>())!;
        Assert.Equal("Confirmed", persisted.Status);
        Assert.Equal(60m, persisted.Total);
        var forbidden = await client.PostAsJsonAsync($"/orders/{order.Id}/items", new { productId = product.Id, quantity = 1 });
        Assert.Equal(HttpStatusCode.Conflict, forbidden.StatusCode);
    }
}

internal sealed class TestApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    public TestApiFactory() => _connection.Open();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseContentRoot(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../Catalogue.Api")));
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.AddDbContext<AppDbContext>(options => options.UseSqlite(_connection));
        });
    }

    public async Task CreateDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) _connection.Dispose();
    }
}
