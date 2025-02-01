using System.Diagnostics;
using ccxt;

namespace OneMoreSpreadSearcher;

// Абстрактный класс для управления подключением к биржам и обработки данных
public abstract partial class ManagerBase
{
     public ccxt.Exchange _client; // Клиент биржи
     protected Balances _balances; // Балансы аккаунта на бирже 
     protected Currencies _currencies; // Доступные валюты на бирже. Пока что не нужны, но позже отсюда будут выгружаться комиссии сетей
     protected Tickers _tickers; // Тикеры (цены) на бирже
     protected Dictionary<string, MarketInterface> _marginMarkets; // Рынки с маржинальной торговлей
     // protected Dictionary<string, MarketInterface> _allMarkets; // Все рынки на бирже. Пока что тоже не нужны.. хз зачем добавил :/
     // protected List<long> timers = new List<long>(); // Таймеры для записи времени операций
     public abstract ExchangeEnum ExchangeType { get; } // Тип биржи, реализуется в наследниках
     public Timer Timer; // Таймер для управления задачами
     public bool IsWorking = true; // Флаг отключения менеджера (по сути костыль отрубающий биржу при критичных ошибках)
     public Tickers Tickers => _tickers;     // Свойство для доступа к тикерам

     protected ManagerBase(string apiKey, string apiSecret, string passPhrase = "")
     {
          // Console.Write($"Setup {ExchangeType}...");
          var config = new Dictionary<string, object>()
          {
               { "apiKey", apiKey },
               { "secret", apiSecret },
               { "newUpdate", true },
               { "useMessageQueue", false },
               // { "sandbox", true }
          };
          if (!string.IsNullOrEmpty(passPhrase))  // Добавляем passPhrase, если он нужен на бирже
               config.Add("password", passPhrase);
          
          var exchangeName = ExchangeType.ToString().ToLower();
          _client = Exchange.DynamicallyCreateInstance(exchangeName, config, true); // Динамически создаем клиент биржи
     }
     
     // Преднастройка клиента: загрузка рынков и валют
     protected internal virtual async Task SetupClient()
     {
          try
          {
               Console.WriteLine($"Loading markets on {ExchangeType}. Wait...");
               var allMarkets = await _client.LoadMarkets();
               _marginMarkets = MarginMarketsFilter(allMarkets);
               if (MarginSymbols.Count == 0)
                    throw new Exception("No margin symbols found on " + ExchangeType);
               _currencies = await _client.FetchCurrencies();
               await SetupTickers();

               Console.WriteLine($"Setup {ExchangeType} done.");

          }
          catch (Exception e)
          {
               Console.WriteLine("Error setup client: " + e.Message);
               IsWorking = false;
          }
     }
     
     // Символы маржинальной торговли, динамически вычисляются исхдя из доступных пар монет("рынков") - marginMarkets
     public List<string> MarginSymbols            // Свойства надо будете переделать на функции GetMarginSymbols и Set.. так будет логичнее
     {
          get => _marginMarkets.Keys.ToList();
          set
          {
               _marginMarkets = _marginMarkets
                    .Where(m => value.Contains(m.Key))
                    .ToDictionary(m => m.Key, m => m.Value);
          }
     }
     
     // Фильтр рынков(пар монет) для выбора только маржинальных
     private Dictionary<string, MarketInterface> MarginMarketsFilter(Dictionary<string, MarketInterface> allMarkets)
     {
          return allMarkets
               .Where(market => market.Value.margin != null && (bool)market.Value.margin)
               .ToDictionary(market => market.Key, market => market.Value);
     }
       
     // Получение тикеров для заданных символов (или всех маржинальных, если символы не заданы) Работает через REST
     public async Task<Tickers> GetTickers(List<string>? symbols = null)
     {
          symbols ??= MarginSymbols;
          var result =await _client.FetchTickers(symbols);  // Для некоторых бирж (Binance) доступен метод FetchBidsAsks(), надо будет заменить
          return result;
     }
      
     // Установка комиссий сетей
     public void SetFees(FeeStorage feeStorage)
     {
          foreach(var currency in _currencies.currencies)
          {
               string symbolName = currency.Key;
               var symbolFee = new SymbolFee{ SymbolName = symbolName, ExchangeEnum = ExchangeType};
               foreach (var network in currency.Value.networks)
               {
                    if(network.Value.active == false || network.Value.withdraw == false)
                         continue;
                    if(network.Value.active == null)
                    {
                         int n = 0;
                    }
                    var networkFee = new NetworkFee
                    {
                         Network = network.Key,
                         Fee = network.Value.fee,
                    };
                    
                    if (network.Value.fee != null)
                         symbolFee.AddNetworkFee(networkFee);
                    else
                    {
                         int n = 0;
                    }
               }
               feeStorage.AddSymbolFee(symbolFee);
          }

     }
     
     // Настройка тикеров (виртуальный метод для переопределения в наследниках) Для бинанс почему-то пришлось переопределить, но есть шанс что будет работать и этот вариант
     protected virtual async Task SetupTickers()
     {
          _tickers = await _client.FetchTickers(MarginSymbols);
     }

     //Механизм событий, пока что отключен, и хз пригодится ли
     public event EventHandler OnChange;
     private void TickersChangedEvent(string symbol, ExchangeEnum exchange)
     {
          OnChange.Invoke(this, new TickerEventArgs(symbol, exchange));
     }
     
     public async Task<Balances> GetBalancesAsync()
     {
          var balance = await _client.FetchBalance();
          return balance;
     }

     // Обновление информации о монетах в заданном словаре тикеров
     // На вход принимает словарь тикеров общий для всех менеджеров бирж, надо заменить на потокобезопасный словарь
     public async Task UpdateCoins(/*Dictionary<string, Dictionary<ExchangeEnum, Ticker>> tickers, */TickerStorage tickerStorage)
     {
          try
          {
               Stopwatch stopwatch = new Stopwatch();  // Таймер для замеров времени обновления тикеров, может пригодиться
               while (true)
               {
                    try
                    {
                         stopwatch.Restart();
                         var watchTickers = await WatchTickers(MarginSymbols); // WebSocket обновление тикера
                         
                         var deltaTicker = watchTickers.tickers.First();
                         
                         // tickers[tickerPair.Key][ExchangeType] = tickerPair.Value;
                         tickerStorage.AddOrUpdate(deltaTicker.Key, ExchangeType, new CustomTicker
                         {
                              Ask = new Offer
                                   { Price = deltaTicker.Value.ask, Volume = deltaTicker.Value.askVolume },
                              Bid = new Offer
                                   { Price = deltaTicker.Value.bid, Volume = deltaTicker.Value.bidVolume },
                              LastUpdate = deltaTicker.Value.timestamp
                         });
                         // TickersChangedEvent(tickerPair.Key, ExchangeType);


                         stopwatch.Stop();
                    }
                    catch (Exception e)
                    {
                         tickerStorage.ErrorMessages.Add(ExchangeType + " manager. UpdateCoins error: " + e.Message);
                    }

                    // Timer.Add(watchTickers.tickers.First().Value.symbol, stopwatch.ElapsedMilliseconds);   
               }
          }
          catch (Exception e)
          {
               Console.WriteLine(e);
               throw new Exception("Error watching tickers");
          }
     }

     public async Task UpdateCoinsRest(TickerStorage tickerStorage)
     {
          try
          {
               // Stopwatch stopwatch = new Stopwatch(); // Таймер для замеров времени обновления тикеров, может пригодиться
               // stopwatch.Restart();
               var fetchedTickers = await _client.FetchTickers(MarginSymbols);

               await Parallel.ForEachAsync(fetchedTickers.tickers, (tickerPair, _) =>
               {
                    tickerStorage.AddOrUpdate(tickerPair.Key, ExchangeType, new CustomTicker
                    {
                         Ask = new Offer
                              { Price = tickerPair.Value.ask, Volume = tickerPair.Value.askVolume },
                         Bid = new Offer
                              { Price = tickerPair.Value.bid, Volume = tickerPair.Value.bidVolume },
                         LastUpdate = tickerPair.Value.timestamp ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                    });
                    return ValueTask.CompletedTask;
               });
               
          }
          catch (Exception ex)
          {
               tickerStorage.ErrorMessages.Add(ExchangeType + ": " + ex.Message);
          }
     }
     
     // Наблюдение за тикерами (виртуальный метод для переопределения в наследниках) В некоторых биржах основной запрос может отличаться или не быть реализован. Но на данный момент не поддерживается только bybit из-за ограничения в 10 символов
     protected virtual async Task<Tickers> WatchTickers(List<string> symbols)
     {
          var tickers = await _client.WatchTickers(symbols);
          
          return tickers;
     }
     
     // Пинг соединения (проверка активности) ccxt почему-то не все биржи пингует сама, поэтому добавил этот метод для поддержания соединения
     public async Task Ping()
     {
          bool firstRun = true;
          while (true)
          {
               await Task.Delay(10000);
               var wsClients = _client.clients;
               if (wsClients == null || wsClients.Count == 0)
                    throw new Exception("No websocket clients have been watched.");
               foreach (var wsClient in wsClients)
                    _client.ping(wsClient.Value);
               
          }
     }
}