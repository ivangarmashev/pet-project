using System;
using System.Collections.Generic;

namespace OneMoreSpreadSearcher;


public class FeeStorage
{
    // Dictionary для быстрого доступа к SymbolFee
    private readonly Dictionary<Tuple<string, ExchangeEnum>, SymbolFee> symbolFees = new();

    public void AddSymbolFee(SymbolFee symbolFee)
    {
        var key = Tuple.Create(symbolFee.SymbolName, symbolFee.ExchangeEnum);
        symbolFees[key] = symbolFee;
    }

    public List<string> GetNetworks(string symbolName, ExchangeEnum exchange)
    {
        List<string> networks = new List<string>();
        
        if (symbolName.Contains('/'))
            symbolName = symbolName.Split('/').First();
        
        var key= Tuple.Create(symbolName, exchange);
        if (symbolFees.TryGetValue(key, out var symbolFee))
            networks =  symbolFee.NetworksFee.Values.Select(n => n.Network).ToList();
        
        return networks;
    }

    public double GetSymbolFee(string symbolName, ExchangeEnum buyExchange, ExchangeEnum sellExchange)
    {
        if (symbolName.Contains('/'))
            symbolName = symbolName.Split('/').First();
        var buyKey = Tuple.Create(symbolName, buyExchange);
        symbolFees.TryGetValue(buyKey, out var buyFeeInfo);
        var sellKey = Tuple.Create(symbolName, sellExchange);
        symbolFees.TryGetValue(sellKey, out var sellFeeInfo);
        var buyNetworks = buyFeeInfo.NetworksFee.Keys.ToList();
        var sellNetworks = sellFeeInfo.NetworksFee.Keys.ToList();
        var commonNetworks = buyNetworks.Intersect(sellNetworks).ToList();
        if (commonNetworks.Count == 0)
            return -1;
        var commonFees = commonNetworks.Select(networkName => buyFeeInfo.GetNetworkFee(networkName));
        var minFee = commonFees.Min();
        if (minFee is { } dMinFee)
            return dMinFee;
        return  -1;
    }

//     public bool RemoveSymbolFee(string symbolName, ExchangeEnum exchangeEnum)
//     {
//         var key = Tuple.Create(symbolName, exchangeEnum);
//         return symbolFees.Remove(key);
//     }
//
//     public IEnumerable<SymbolFee> GetAllSymbolFees()
//     {
//         return symbolFees.Values;
//     }
}

public class SymbolFee
{
    public string SymbolName { get; set; }
    public ExchangeEnum ExchangeEnum { get; set; }

    // Dictionary для быстрого доступа к NetworkFee по названию сети
    public readonly Dictionary<string, NetworkFee> NetworksFee = new();

    public void AddNetworkFee(NetworkFee networkFee)
    {
        NetworksFee[networkFee.Network] = networkFee;
    }

    // public NetworkFee GetNetworkFee(string network)
    // {
    //     return NetworksFee.TryGetValue(network, out var fee) ? fee : null;
    // }
    
    public double? GetNetworkFee(string network)
    {
        NetworksFee.TryGetValue(network, out var fee);
        return (double)fee!.Fee!;
    }
    

    // public bool RemoveNetworkFee(string network)
    // {
    //     return networksFee.Remove(network);
    // }

    public IEnumerable<NetworkFee> GetAllNetworkFees()
    {
        return NetworksFee.Values;
    }
}

public class NetworkFee
{
    public string Network { get; set; }
    public double? Fee { get; set; }
}