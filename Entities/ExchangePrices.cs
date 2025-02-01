namespace OneMoreSpreadSearcher;
public class ExchangePrices
{
    public string Symbol { get; }
    public Dictionary<ExchangeEnum, ExchangePrice> Prices { get; }
    
    public Dictionary<string, List<ExchangeEnum>> CommonNetworks { get; private set; }
    public List<ArbitrageOpportunity> ArbitrageOpportunities { get; private set; }

    public ExchangePrices(string symbol, Dictionary<ExchangeEnum, ExchangePrice> prices, ref FeeStorage feeStorage)
    {
        Symbol = symbol;
        Prices = prices;
        ArbitrageOpportunities = [];
        FindCommonNetworks();
        // CalculateArbitrage(ref feeStorage);
        CalculateArbitrageOpportunitiesForCommonNetworks(ref feeStorage);
    }
    
    private void FindCommonNetworks()
    {
        CommonNetworks = new Dictionary<string, List<ExchangeEnum>>();
        foreach (var price in Prices)
            foreach (var network in price.Value.Networks)
                if (CommonNetworks.TryGetValue(network, out List<ExchangeEnum>? value))
                    value.Add(price.Key);
                else
                    CommonNetworks[network] = [price.Key];
            
        

        foreach (var valuePair in CommonNetworks.Where(network => network.Value.Count < 2))
            CommonNetworks.Remove(valuePair.Key);
        
    }
    
    // Дополнительные поля для сохранения результатов вычислений
    public bool HasArbitrageOpportunity { get; private set; }

    /// Фунция для расчета арбитражных возможностей с учетом общих сетей и их комиссий
    public void CalculateArbitrageOpportunitiesForCommonNetworks(ref FeeStorage feeStorage)
    {
        bool disableChainFilters = false;
        ArbitrageOpportunities.Clear();

        if (disableChainFilters)
        {
            CalculateArbitrage(ref feeStorage);
            return;
        }

        foreach (var network in CommonNetworks)
        {
            var pricesInNetwork = Prices.Where(p => network.Value.Contains(p.Key)).ToDictionary(p => p.Key, p => p.Value);
            if (pricesInNetwork.Count < 2) continue;

            var bestBuy = pricesInNetwork.OrderBy(p => p.Value.AskPrice).FirstOrDefault();
            var bestBuyExchange = bestBuy.Key;
            var bestBuyPrice = bestBuy.Value.AskPrice;
            var bestBuyVolume = bestBuy.Value.AskVolume;

            var bestSell = pricesInNetwork.OrderByDescending(p => p.Value.BidPrice).FirstOrDefault();
            var bestSellExchange = bestSell.Key;
            var bestSellPrice = bestSell.Value.BidPrice;
            var bestSellVolume = bestSell.Value.BidVolume;

            var spreadPercentage = ((bestSellPrice - bestBuyPrice) / bestBuyPrice) * 100;
            if (bestSellPrice > bestBuyPrice && bestSellExchange != bestBuyExchange && spreadPercentage > 0 && spreadPercentage < 100)
            {
                var arbitrageOpportunity = new ArbitrageOpportunity
                {
                    Symbol = Symbol,
                    BuyExchange = bestBuyExchange,
                    SellExchange = bestSellExchange,
                    BuyPrice = bestBuyPrice,
                    SellPrice = bestSellPrice,
                    SpreadPercentage = spreadPercentage,
                    AvailableVolume = Math.Min(bestBuyVolume, bestSellVolume),
                    BuyTimestamp = bestBuy.Value.LastUpdate,
                    SellTimestamp = bestSell.Value.LastUpdate,
                    Fee = feeStorage.GetSymbolFee(Symbol, bestBuyExchange, bestSellExchange),
                    Network = network.Key
                };
                ArbitrageOpportunities.Add(arbitrageOpportunity);
            }
        }

        HasArbitrageOpportunity = ArbitrageOpportunities.Any();
    }

    public void CalculateArbitrage(ref FeeStorage feeStorage)
    {
        // Находим минимальный ask и соответствующую биржу
        var bestBuy = Prices.OrderBy<KeyValuePair<ExchangeEnum, ExchangePrice>, object>(p => p.Value.AskPrice).FirstOrDefault();
        var bestBuyExchange = bestBuy.Key;
        var bestBuyPrice = bestBuy.Value.AskPrice;
        var bestBuyVolume = bestBuy.Value.AskVolume;

        // Находим максимальный bid и соответствующую биржу
        var bestSell = Prices.OrderByDescending(p => p.Value.BidPrice).FirstOrDefault();
        var bestSellExchange = bestSell.Key;
        var bestSellPrice = bestSell.Value.BidPrice;
        var bestSellVolume = bestSell.Value.BidVolume;

        var spreadPercentage = ((bestSellPrice - bestBuyPrice) / bestBuyPrice) * 100;
        // Проверяем, есть ли возможность арбитража
        if (bestSellPrice > bestBuyPrice && bestSellExchange != bestBuyExchange && spreadPercentage > 0 && spreadPercentage < 100)
        {
            var arbitrageOpportunity = new ArbitrageOpportunity
            {
                Symbol = Symbol,
                BuyExchange = bestBuyExchange,
                SellExchange = bestSellExchange,
                BuyPrice = bestBuyPrice,
                SellPrice = bestSellPrice,
                SpreadPercentage = ((bestSellPrice - bestBuyPrice) / bestBuyPrice) * 100,
                AvailableVolume = Math.Min(bestBuyVolume, bestSellVolume),
                BuyTimestamp = bestBuy.Value.LastUpdate,
                SellTimestamp = bestSell.Value.LastUpdate,
                Fee = feeStorage.GetSymbolFee(Symbol, bestBuyExchange, bestSellExchange)
            };
            if (arbitrageOpportunity.Fee != -1)
            {
                ArbitrageOpportunities.Add(arbitrageOpportunity);
                HasArbitrageOpportunity = true;
            }
        }
        else
        {
            HasArbitrageOpportunity = false;
        }
    }
}

public class ArbitrageOpportunity
{
    public string Symbol { get; set; }
    public ExchangeEnum BuyExchange { get; set; }
    public ExchangeEnum SellExchange { get; set; }
    public double BuyPrice { get; set; }
    public double SellPrice { get; set; }
    public double SpreadPercentage { get; set; }
    public double AvailableVolume { get; set; }

    public double AvailableVolumeUsdt => AvailableVolume * BuyPrice;
    public long BuyTimestamp { get; set; }
    public long SellTimestamp { get; set; }
    public double Fee { get; set; }
    public double FeeUsdt => Fee * BuyPrice;
    public string Network { get; set; }
    public double Profit => (AvailableVolume - Fee) * SellPrice - AvailableVolume * BuyPrice;

    public double Profit100
    {
        get
        {
            double amountUsdt = 150;
            double coeff = 1;
            if (AvailableVolumeUsdt > amountUsdt)
                coeff = amountUsdt / AvailableVolumeUsdt;
            var availableVolumeCoeff = AvailableVolume * coeff;
            var sellCost = availableVolumeCoeff * SellPrice;
            var feeCost = Fee * SellPrice;
            var buyCost = availableVolumeCoeff * BuyPrice;
            var profit = sellCost - feeCost - buyCost;
            return profit;
        }
    }



    public double ProfitPercentage => Profit / (AvailableVolume * BuyPrice) * 100;
}