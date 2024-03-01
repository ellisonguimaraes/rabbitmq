using RPCServer.Domain;

namespace RPCServer.Services;

public sealed class OrderServices
{
    public static OrderStatus OnStore(decimal amount)
        => (amount < 0 || amount > 10000)? OrderStatus.Declined : OrderStatus.Aproved;
}