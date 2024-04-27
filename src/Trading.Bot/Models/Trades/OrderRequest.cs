namespace Trading.Bot.Models.Trades;

public class OrderRequest
{
    public Order Order { get; set; }

    public OrderRequest(Order order)
    {
        Order = order;
    }
}