namespace RPCClient.Domain;

public class Order(decimal amount)
{
    public long Id { get; set; } = DateTime.Now.Ticks;

    public decimal Amount { get; set; } = amount;

    public OrderStatus OrderStatus { get; set; } = OrderStatus.Processing;

    public string Status => OrderStatus.ToString();
}
