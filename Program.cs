using System.Diagnostics;
using System.Text;
using ccxt;
using ccxt.pro;
using OneMoreSpreadSearcher.Exchanges;

namespace OneMoreSpreadSearcher;

class Program
{
    static bool Logged = false;

    static async Task Main(string[] args)
    {
        try
        {
            Console.WriteLine("Start setup.");
            
            // await TestArea.Run(GetAvaliableManagers(necessaryExchangeName: ExchangeEnum.Binance.ToString()));
            RabbitMqService rabbitMqService = new RabbitMqService();
            
            // Инициализируем менеджеры для бирж
            var managers = GetAvaliableManagersAsync([
                // ExchangeEnum.Bybit.ToString(),
                // ExchangeEnum.Binance.ToString(),
                // ExchangeEnum.Bitget.ToString(),
                // ExchangeEnum.Okx.ToString(),
                // ExchangeEnum.KuCoin.ToString(),
                // ExchangeEnum.Mexc.ToString(),
                // ExchangeEnum.Htx.ToString(),
            ]).Result;
            
            // Объединяем символы с различных бирж
            var joinedSymbols = JoinSymbols(managers.SelectMany(manager => manager.MarginSymbols).ToList())
                // .Take(300)       // Временное ограничение на 70 символов
                .ToList();

            var tickerStorage = new TickerStorage();
            
            // Обновляем маржинальные символы в каждом менеджере
            foreach (var manager in managers)
                manager.MarginSymbols = joinedSymbols;
            
            FeeStorage feeStorage = new FeeStorage();

            // Устанавливаем комиссии
            foreach (var manager in managers)
                manager.SetFees(feeStorage);

            // await WebSocketModelRun(tickerStorage, feeStorage, managers);
            await RestModelRun(tickerStorage, feeStorage, managers, rabbitMqService);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    private static async Task WebSocketModelRun(TickerStorage tickerStorage, FeeStorage feeStorage, List<ManagerBase> managers, RabbitMqService rabbitMqService)
    {
        var updateTasks = new List<Task>();     // Задачи связанные с обновлением тикеров в реальном времени
        foreach (var manager in managers){
            updateTasks.Add(manager.UpdateCoins(tickerStorage));
            updateTasks.Add(manager.Ping());
        }
        updateTasks.Add(Calculator.SearchSpread(tickerStorage, feeStorage, rabbitMqService));
        updateTasks.Add(Logging(managers));
            
        Console.WriteLine("Setup done. Start watching");

        await Task.WhenAll(updateTasks);
    }

    // Метод для обновления тикеров при помощи REST
    private static async Task RestModelRun(TickerStorage tickerStorage, FeeStorage feeStorage, List<ManagerBase> managers, RabbitMqService rabbitMqService)
    {
        BalancesManager balancesManager = new BalancesManager(rabbitMqService);
        await Task.WhenAll(
            RestUpdatePart(tickerStorage, feeStorage, managers), 
            Calculator.SearchSpread(tickerStorage, feeStorage, rabbitMqService), 
            Logging(managers),
            balancesManager.UpdateBalancesAsync(managers));
    }
    
    
    private static async Task RestUpdatePart(TickerStorage tickerStorage, FeeStorage feeStorage, List<ManagerBase> managers)
    {
        var updateTasks = new List<Task>(); 
        while (true)
        {
            foreach (var manager in managers)
                updateTasks.Add(manager.UpdateCoinsRest(tickerStorage));
            
            updateTasks.Add(Task.Delay(1010));
            await Task.WhenAll(updateTasks);
        }
        
    }
    
    // Просто костыльный метод отладки чтоб можно было например через 90 секунд провалиться в дебаг и посмотреть состояние менеджеров/клиентов/таймеров
    private static async Task Logging(List<ManagerBase> managers = null, Timer timer = null)
    {
        while (true)
        {
            await Task.Delay(90000);
            
        }
    }

    private static async Task<List<ManagerBase>> GetAvaliableManagersAsync(List<string>? excludeExchanges = null,
        string necessaryExchangeName = "")
    {
        var managers = new List<ManagerBase>();
        var names = Enum.GetNames<ExchangeEnum>();

        foreach (var exchangeName in names)
        {
            if (string.IsNullOrEmpty(exchangeName) ||
                (excludeExchanges != null && excludeExchanges.Contains(exchangeName)) ||
                (!string.IsNullOrEmpty(necessaryExchangeName) && exchangeName != necessaryExchangeName))
                continue;

            if(GetManager(exchangeName) is { } manager)
                managers.Add(manager);
        }

        var setupClientTasks = managers.Select(m => m.SetupClient());
        await Task.WhenAll(setupClientTasks);
        
        return managers.Where(m => m.IsWorking).ToList();
    }
    
    private static ManagerBase? GetManager(string exchangeName)
    {
        try
        {
            string key = Environment.GetEnvironmentVariable(exchangeName.ToUpper() + "_API_KEY")!;
            string secret = Environment.GetEnvironmentVariable(exchangeName.ToUpper() + "_API_SECRET")!;
            string passPhrase = Environment.GetEnvironmentVariable(exchangeName.ToUpper() + "_API_PASS")!;

            return exchangeName switch
            {
                nameof(ExchangeEnum.Binance) => new BinanceManager(key, secret),
                nameof(ExchangeEnum.Bitget) => new BitgetManager(key, secret, passPhrase),
                nameof(ExchangeEnum.Bybit) => new BybitManager(key, secret),
                nameof(ExchangeEnum.KuCoin) => new KuCoinManager(key, secret, passPhrase),
                nameof(ExchangeEnum.Okx) => new OkxManager(key, secret, passPhrase),
                nameof(ExchangeEnum.Mexc) => new MexcManager(key, secret),
                nameof(ExchangeEnum.Htx) => new HtxManager(key, secret),
                _ => null
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine("Не удалось найти менеджер для " + exchangeName);
            return null;
        }
    }

    // Метод для объединения тикеров
    private static Dictionary<string, Dictionary<ExchangeEnum, Ticker>> JoinTickers(List<string> jointSymbols, List<ManagerBase> managers)
    {
        Dictionary<string, Dictionary<ExchangeEnum, Ticker>> tickers = new Dictionary<string, Dictionary<ExchangeEnum, Ticker>>();
        foreach (var symbol in jointSymbols)
        {
            tickers[symbol] = new Dictionary<ExchangeEnum, Ticker>();
            
            foreach (var manager in managers)
            {
                var foundSymbol =
                    manager.Tickers.tickers.FirstOrDefault(ticker => ticker.Key == symbol);
                if(foundSymbol.Value.symbol != null)
                    tickers[symbol][manager.ExchangeType] = foundSymbol.Value;
            }
        }
        return tickers;
    }


    // Метод для объединения символов из нескольких списков
    private static List<string> JoinSymbols(params List<string>[] symbols)
    {
        var result = symbols
            .SelectMany(list => list) // Объединяем все списки в один
            .GroupBy(item => item)    // Группируем элементы
            .Where(group => group.Count() >= 2) // Оставляем только те, которые встречаются >= 2 раза
            .Select(group => group.Key) // Берем сами элементы
            .ToList();
        return result;
    }

    private static void TickerChanged(object sender, EventArgs tickerEventArgs)
    {
        if(!Logged) return;
        
        var symbol = ((TickerEventArgs)tickerEventArgs).Symbol;
        var exchange = ((TickerEventArgs)tickerEventArgs).Exchange;
        StringBuilder sb = new StringBuilder();
        sb.Append("Symbol: ");
        sb.Append(symbol);
        sb.Append(',');
        for(int i = 0; i < 15 - symbol.Length; i++)
            sb.Append(' ');
        sb.Append("Exchange: ");
        sb.Append(exchange);
        Console.WriteLine(sb.ToString());
    }
    
    
    static async Task Test(ManagerBase binance, Timer timer)
    {
        Stopwatch mainSw = Stopwatch.StartNew();
        bool firstRun = true;
        timer.extSw = mainSw;
        Stopwatch sw = new Stopwatch();

        while (true)
        {
            sw.Restart();
            var LimitTickersCount = 10;
            var splittedSymbolsList = binance.MarginSymbols
                .Select((value, index) => new { value, index })
                .GroupBy(x => x.index / LimitTickersCount)
                .Select(g => g.Select(x => x.value).ToList())
                .ToList();
            // foreach (var symbols in splittedSymbolsList)
            // {

            var orderBooks = binance._client.FetchOrderBook(splittedSymbolsList.First().First()).Result;
            
            
            var orders = await binance._client.WatchBidsAsks(splittedSymbolsList.First());
            if (binance._client.bidsasks is CustomConcurrentDictionary<string, object> dict && dict.Count > 0)
            {
                var ticker = dict[orders.tickers.First().Value.symbol!];
            }

            // }
            sw.Stop();
            timer.Add(orders.tickers.First().Key, sw.ElapsedMilliseconds);
            if (firstRun)
            {
                firstRun = false;
                mainSw.Restart();
            }
        }
    }

}