using ccxt;

namespace OneMoreSpreadSearcher.Exchanges;

public class BinanceManager(string apiKey, string apiSecret)
    : ManagerBase(apiKey, apiSecret)
{
    public override ExchangeEnum ExchangeType => ExchangeEnum.Binance;
    
    protected override async Task SetupTickers()
    {
        Tickers allTickers = await _client.FetchTickers();
        var filtredTickers = allTickers.tickers
            .Where(ticker => MarginSymbols.Contains(ticker.Key))
            .ToDictionary(ticker => ticker.Key, ticker => ticker.Value);
        _tickers = new Tickers { info = null, tickers = filtredTickers };
    }
    
    /*protected override async Task<Tickers> WatchTickers(List<string> symbols)
    {
        var tickers = new Tickers();
        var ticker = new Ticker();
        var orderBook = await _client.WatchOrderBookForSymbols(symbols, limit2: 1) as OrderBook;
        
        var ask  = (orderBook.asks as Asks)?.FirstOrDefault() as List<object>;
        var bid  = (orderBook.bids as Bids)?.FirstOrDefault() as List<object>;
        if(bid is null || ask is null)
            return new Tickers();

       
        
        var bidPrice = bid.First() as double?;
        var bidVolume = bid.Last();
        bidVolume = bidVolume switch
        {
            Decimal decBidVolume => (double)decBidVolume,
            Double dBidVolume => dBidVolume,
            _ => null
        };

        var askPrice = ask.First() as double?;
        var askVolume = ask.Last();
        askVolume = askVolume switch
        {
            Decimal decAskVolume => (double)decAskVolume,
            Double dAskVolume => dAskVolume,
            _ => null
        };
        if(bidVolume is null || bidPrice is null || askPrice is null || askVolume is null)
            return new Tickers();
        ticker.ask = askPrice;
        ticker.askVolume = (double?)askVolume;
        ticker.bid = bidPrice;
        ticker.bidVolume = (double?)bidVolume;
        ticker.symbol = orderBook["symbol"] as string;
        tickers.tickers = new Dictionary<string, Ticker> { { ticker.symbol, ticker } };

        // var n = bid.First() as List<object>;
        // var bidValue = n.First() as double?;
        // t.askVolume = (double?)orderBook.asks.First()
        // orderBook.
        // foreach (var tickerPair in s.tickers)
        // Console.WriteLine(ExchangeType + ": " + tickerPair.Key + ": " + tickerPair.Value + " ");
        return tickers;
    }*/
}