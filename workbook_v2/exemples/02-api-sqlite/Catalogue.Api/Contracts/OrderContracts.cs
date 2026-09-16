using System.ComponentModel.DataAnnotations;

namespace Catalogue;

public sealed class AddItemRequest
{
    public Guid ProductId { get; set; }
    [Range(1, 100)]
    public int Quantity { get; set; }
}

public record OrderItemResponse(string ProductName, decimal UnitPrice, int Quantity);
public record OrderResponse(Guid Id, string Status, decimal Total, List<OrderItemResponse> Items)
{
    public static OrderResponse From(Order order) => new(
        order.Id, order.Status.ToString(), order.Total,
        order.Items.Select(i => new OrderItemResponse(i.ProductName, i.UnitPrice, i.Quantity)).ToList());
}
