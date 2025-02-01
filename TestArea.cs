using System.Collections.Concurrent;
using System.Diagnostics;
using ccxt;
using ccxt.pro;
using OneMoreSpreadSearcher.Exchanges;

namespace OneMoreSpreadSearcher;

// Класс для тестирования скорости обновления тикеров
public class TestArea
{
    public static async Task Run(List<ManagerBase> managers, Dictionary<string, Dictionary<ExchangeEnum, Ticker>> tickers)
    {
        try
        {
            List<Task> tasks = new List<Task>();

            Dictionary<ManagerBase, List<long>> outElapsedTimesList = new Dictionary<ManagerBase, List<long>>();
            Dictionary<ManagerBase, List<long>> inElapsedTimesList = new Dictionary<ManagerBase, List<long>>();
            foreach (var manager in managers)
            {
                List<long> outElapsedTimes = new List<long>();
                List<long> inElapsedTimes = new List<long>();
                outElapsedTimesList[manager] = outElapsedTimes;
                inElapsedTimesList[manager] = inElapsedTimes;
                tasks.Add(UpdateFunc(manager, tickers, outElapsedTimes, inElapsedTimes));
            }

            await Task.WhenAll(tasks);

        }
        catch (Exception ex)
        {

        }
    }

    private static async Task UpdateFunc(ManagerBase managerBase, Dictionary<string, Dictionary<ExchangeEnum, Ticker>> tickers,
        List<long> outElapsedTimes, List<long> inElapsedTimes)
    {
        Stopwatch outFuncTimer = new Stopwatch();
        Stopwatch inFuncTimer = new Stopwatch();
        long avgTime = 0;
        while (true)
        {
            outFuncTimer.Stop();
            outElapsedTimes.Add(outFuncTimer.ElapsedMilliseconds);

            inFuncTimer.Restart();
            var watchTickers =
                await managerBase._client.WatchTickers(managerBase.MarginSymbols); // WebSocket обновление тикера

            // if(watchTickers ) continue;

            foreach (var tickerPair in
                     watchTickers
                         .tickers) // Вообще в websocket запросах всегда возвращается по одному обновленному тикеру(дельта), на всякий случай добавил перебор всех, но можно и ограничиться .First() 
            {
                try
                {
                    tickers[tickerPair.Key][ExchangeEnum.Binance] = tickerPair.Value;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    throw new Exception($"Could not watch ticker {tickerPair.Key}");
                }
            }

            inFuncTimer.Stop();
            inElapsedTimes.Add(inFuncTimer.ElapsedMilliseconds);

            outFuncTimer.Restart();
            if (inElapsedTimes.Count == 1000)
            {
                avgTime = inElapsedTimes.Sum() / inElapsedTimes.Count;
            }

            if (managerBase is BinanceManager && avgTime != 0)
                // Timer.Add(watchTickers.tickers.First().Value.symbol, stopwatch.ElapsedMilliseconds);   
            {
                int n = 0;
            }
        }
    }


    public class Checks
    {
        public ConcurrentDictionary<string, Check> checks;

        public void Add(string symbol, ExchangeEnum exchange, double price)
        {
            
        }
    }

    public class Check
    {
        public string Symbol;
        public ExchangeEnum Exchange;
        public int UpdatesCount;
        public double Price;
    }

    public class CustomTickers
    {   
        // public Dictionary<string, Dictionary<ExchangeEnum, Ticker>> tickers;
        public Dictionary<string, CustomTicker> tickers;

    }

    public class CustomTicker
    {
        public string TickerName;
        public ExchangeEnum ExchangeEnum;
        public double Price;
        public int UpdateCounts;
    }
    
}