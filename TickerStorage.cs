using System.Collections.Concurrent;
using ccxt;

namespace OneMoreSpreadSearcher;

public class TickerStorage
{
    public readonly ConcurrentDictionary<string, ConcurrentDictionary<ExchangeEnum, CustomTicker>> _storage;
    public readonly ConcurrentBag<string> ErrorMessages = new ConcurrentBag<string>();
    public Dictionary<string, Dictionary<ExchangeEnum, Fee>> Fees;

    public TickerStorage()
    {
        _storage = new ConcurrentDictionary<string, ConcurrentDictionary<ExchangeEnum, CustomTicker>>();
    }

    // Добавить или обновить тикер
    public void AddOrUpdate(string symbol, ExchangeEnum exchange, CustomTicker ticker)
    {
        var exchangeTickers = _storage.GetOrAdd(symbol, _ => new ConcurrentDictionary<ExchangeEnum, CustomTicker>());
        exchangeTickers.AddOrUpdate(exchange, ticker, (_, _) => ticker);
    }

    // Получить тикер
    public bool TryGet(string symbol, ExchangeEnum exchange, out CustomTicker ticker)
    {
        ticker = default;
        if (_storage.TryGetValue(symbol, out var exchangeTickers))
        {
            return exchangeTickers.TryGetValue(exchange, out ticker);
        }
        return false;
    }

    // Получить все данные
    public Dictionary<string, Dictionary<ExchangeEnum, CustomTicker>> GetAll()
    {
        var result = new Dictionary<string, Dictionary<ExchangeEnum, CustomTicker>>();

        foreach (var symbol in _storage)
        {
            result[symbol.Key] = new Dictionary<ExchangeEnum, CustomTicker>(symbol.Value);
        }

        return result;
    }
}

public class CustomTicker
{
    public Offer Bid { get; set; }
    public Offer Ask { get; set; }
    public long? LastUpdate { get; set; }
    public decimal Fee { get; set; }
}


public class Offer
{
    public double? Price { get; set; }
    public double? Volume { get; set; }
}
