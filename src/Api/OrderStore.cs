using Api.Contracts;
using Domain.After;

namespace Api;

internal sealed class OrderStore
{
    private readonly Lock _lock = new();
    private readonly Dictionary<OrderId, Order> _orders = [];
    private readonly List<OrderEvent> _events = [];

    public Order Place()
    {
        lock (_lock)
        {
            var order = new Order(new OrderId(_orders.Count + 1), new Draft());
            _orders[order.Id] = order;
            _events.Add(new OrderPlaced(order.Id.Value));
            return order;
        }
    }

    public bool TryGet(OrderId id, out Order order)
    {
        lock (_lock) return _orders.TryGetValue(id, out order);
    }

    public OrderPaid Save(OrderId id, Paid paid, string method)
    {
        lock (_lock)
        {
            _orders[id] = new Order(id, paid);
            var e = new OrderPaid(id.Value, method);
            _events.Add(e);
            return e;
        }
    }

    public OrderShipped Save(OrderId id, Shipped shipped)
    {
        lock (_lock)
        {
            _orders[id] = new Order(id, shipped);
            var e = new OrderShipped(id.Value, shipped.TrackingNumber.Value);
            _events.Add(e);
            return e;
        }
    }

    public OrderEvent[] Events(int id)
    {
        lock (_lock) return [.. _events.Where(e => e.Id == id)];
    }
}
