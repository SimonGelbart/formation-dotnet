using Catalogue;
using Xunit;

public sealed class DomainTests
{
    [Fact]
    public void NegativePriceIsRejected()
        => Assert.Throws<ArgumentOutOfRangeException>(() => new Product("Clavier", -1m));

    [Fact]
    public void EmptyOrderCannotBeConfirmed()
        => Assert.Throws<OrderConflictException>(() => new Order().Confirm());

    [Fact]
    public void OrderKeepsOriginalPrice()
    {
        var product = new Product("Clavier", 30m);
        var order = new Order();
        order.AddItem(product, 2);
        product.Update("Clavier", 50m);
        Assert.Equal(60m, order.Total);
        Assert.Equal(order.Id, Assert.Single(order.Items).OrderId);
    }

    [Fact]
    public void ConfirmedOrderCannotBeModified()
    {
        var product = new Product("Clavier", 30m);
        var order = new Order();
        order.AddItem(product, 1);
        order.Confirm();
        Assert.Throws<OrderConflictException>(() => order.AddItem(product, 1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void NonPositiveQuantityIsRejected(int quantity)
        => Assert.Throws<ArgumentOutOfRangeException>(
            () => new Order().AddItem(new Product("Clavier", 30m), quantity));
}
