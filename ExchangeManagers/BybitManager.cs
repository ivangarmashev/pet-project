namespace OneMoreSpreadSearcher.Exchanges;

public class BybitManager(string apiKey, string apiSecret) : ManagerBase(apiKey, apiSecret)
{
    public override ExchangeEnum ExchangeType => ExchangeEnum.Bybit;
    private HashSet<string>? _hashSetSymbols;
    private HashSet<string> HashSetSymbols => _hashSetSymbols ??= MarginSymbols.ToHashSet();
    
    // protected override async Task<Tickers> WatchTickers(List<string> symbols)
    // { 
    //     var tickerSymbol = "";
    //     Tickers tickers = new Tickers();
    //     while (!HashSetSymbols.Contains(tickerSymbol))
    //     {
    //         tickers = await _client.WatchTickers();
    //         tickerSymbol = tickers.tickers.First().Key;
    //     }
    //     return tickers;
    // }
}