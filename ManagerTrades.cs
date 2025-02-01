using ccxt;

namespace OneMoreSpreadSearcher;

// Нереализованные методы торговли
public abstract partial class ManagerBase
{
    private static readonly Dictionary<string, object> _marginOrderParams = new() { ["margin"] = true, ["marginMode"] = "cross" };

    public async Task TradeSpot(string symbol, decimal amount, decimal price)
    {
        return;
    }
    
    public async Task TradeMargin(string symbol, double amount, double? price = null)
    {
        Order order;
        if(price == null)
            order = await _client.CreateMarketOrder(symbol, "sell", amount, parameters: _marginOrderParams);
        else
            order = await _client.CreateMarketOrder(symbol, "sell", amount, price.Value, parameters: _marginOrderParams);
        
    }
}