using System.ComponentModel.DataAnnotations;

namespace Catalogue;

public sealed class ProductRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;

    [Range(typeof(decimal), "0", "1000000")]
    public decimal Price { get; set; }
}

public record ProductResponse(Guid Id, string Name, decimal Price)
{
    public static ProductResponse From(Product product)
        => new(product.Id, product.Name, product.Price);
}
