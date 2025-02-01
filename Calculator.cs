using System.Diagnostics;
using ccxt;
using Newtonsoft.Json;

namespace OneMoreSpreadSearcher;

public static class Calculator
{
    public static async Task SearchSpread(TickerStorage tickerStorage, FeeStorage feeStorage, RabbitMqService rabbitMqService)
    {
        await Task.Delay(5000); // Небольшая задержка перед запуском на прогрузку бирж
        Console.WriteLine("Start calculation2...");
        Stopwatch sw = new Stopwatch();
        while (true)
        {
            sw.Restart();
            List<ExchangePrices> exchangePricesList = new List<ExchangePrices>(tickerStorage._storage.Count);  
            
            foreach (var symbol in tickerStorage._storage)
            {
                var symbolName = symbol.Key;    // Символ торговой пары (например, BTC/USDT)
                var exchanges = symbol.Value;    // Данные по биржам для этой пары
                
                Dictionary<ExchangeEnum, ExchangePrice> prices = new(exchanges.Count);  // Словарь для хранения цен (bid/ask) с каждой биржи
                foreach (var exchangeTicker in exchanges) // Проходим по каждой бирже и добавляем данные о ценах
                {
                    var networks = feeStorage.GetNetworks(symbolName, exchangeTicker.Key);
                    var price = new ExchangePrice(
                        (double)exchangeTicker.Value.Bid.Price!,    // Цена bid
                        (double)exchangeTicker.Value.Ask.Price!,    // Цена ask
                        (double)exchangeTicker.Value.Bid.Volume!,   // Объем bid
                        (double)exchangeTicker.Value.Ask.Volume!,    // Объем ask
                        (long)exchangeTicker.Value.LastUpdate!,
                        networks
                        // fee
                    );
                    prices[exchangeTicker.Key] = price;
                }
                exchangePricesList.Add(new ExchangePrices(symbolName, prices, ref feeStorage));  // Добавляем информацию о ценах для этой пары   В ЭТОТ МОМЕНТ ПРОИСХОДИТ РАССЧЕТ СПРЕДА ДЛЯ ДАННОЙ ПАРЫ
                
            }
            
            // Фильтруем и сортируем топ-5 арбитражных возможностей
            var topOpportunities = exchangePricesList.SelectMany(e => e.ArbitrageOpportunities)
                // .Where(e => Math.Abs(e.BuyTimestamp - e.SellTimestamp) < 2000)
                // .Where(e => e is { SpreadPercentage: > 0 })
                // .Where(e => (e.AvailableVolume * e.BuyPrice) is > 20 or  < 0.01 and > 0.0004)
                // .Where(e => e.Fee * e.BuyPrice is < 0.5)
                
                .Where(e => e.Profit100 > 0.15)
                .OrderByDescending(e => e.Profit100)
                
                // .Where(e => e.AvailableVolumeUsdt < 1000)
                // .Where(e => e.Profit > 0.15)
                // .OrderByDescending(e => e.Profit)
                
                .Take(5);
            
            // Печатаем лучшие возможности
            PrintPrices(topOpportunities);
            
            // var balances = await GetBalancesAsync(managers);
            
            var json = JsonConvert.SerializeObject(topOpportunities);
            rabbitMqService.SendMessage(json);
            sw.Stop();
            Console.WriteLine("Calculation time: " + sw.ElapsedMilliseconds);
            foreach (var errorMessage in tickerStorage.ErrorMessages)
                Console.WriteLine(errorMessage);
            
            await Task.Delay(250); // Задержка между обновлениями экрана, fps
        }
        
    }
    
    // Метод для вывода топовых арбитражных возможностей
    private static void PrintPrices(IEnumerable<ArbitrageOpportunity> topOpportunities)
    {
        Console.Clear();
        Console.WriteLine("Top opportunities:");
        Console.WriteLine("----------------------------------------------------------------------------------" +
                          "-----------------------------------------------------------------------------");
        Console.WriteLine("{0,-15}{1,-15}{2,-12}{3,-15}{4,-12}{5,-10}{6,-15}{7, -12}{8, -12}{9, -10}{10, -10}{11, -10}{12, -10}",
            "Symbol", "Buy Exchange", "Buy Price", "Sell Exchange", "Sell Price", "Diff(%)", "Volume (USDT)","Buy Update", "Sell Update","Network","Fee", "Profit", "Profit100");
        Console.WriteLine("----------------------------------------------------------------------------------" +
                          "-----------------------------------------------------------------------------");

        foreach (var opp in topOpportunities)
        {
            Console.WriteLine("{0,-15}{1,-15}{2,-12:F5}{3,-15}{4,-12:F5}{5,-10:F2}{6,-15:F5}{7, -12}{8, -12}{9, -10}{10, -10:F5}{11, -10:F6}{12, -10:F3}",
                opp.Symbol,
                opp.BuyExchange,
                opp.BuyPrice,
                opp.SellExchange,
                opp.SellPrice,
                opp.SpreadPercentage,
                opp.AvailableVolumeUsdt,
                FormatMilliseconds(opp.BuyTimestamp),
                FormatMilliseconds(opp.SellTimestamp),
                opp.Network,
                opp.FeeUsdt,
                opp.Profit,
                opp.Profit100
                );
        }
    }
 
    private static string FormatMilliseconds(long milliseconds)
    {
        return TimeSpan.FromMilliseconds(milliseconds).ToString("mm\\:ss\\.fff");
    }
}

