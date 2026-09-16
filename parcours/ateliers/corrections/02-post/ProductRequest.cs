using System.ComponentModel.DataAnnotations;

public sealed class ProductRequest
{
    [Required]
    public string Name { get; set; } = "";
    [Range(typeof(decimal), "0", "1000000")]
    public decimal Price { get; set; }
}
