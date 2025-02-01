
using ccxt;

namespace OneMoreSpreadSearcher.Exchanges;

public class BitgetManager(string apiKey, string apiSecret, string passPhrase) : ManagerBase(apiKey, apiSecret, passPhrase)
{
    public override ExchangeEnum ExchangeType => ExchangeEnum.Bitget;
    
    protected override async Task<Tickers> WatchTickers(List<string> symbols)
    {
        var tickers = await _client.WatchBidsAsks(symbols);
        // foreach (var tickerPair in s.tickers)
        // Console.WriteLine(ExchangeType + ": " + tickerPair.Key + ": " + tickerPair.Value + " ");
        return tickers;
    }
}