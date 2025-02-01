namespace OneMoreSpreadSearcher;

public class ExchangePrice
{
    public double BidPrice { get; }
    public double AskPrice { get; }
    public double BidVolume { get; }
    public double AskVolume { get; }
    public long LastUpdate { get; }
    // public SymbolFee Fee { get; }
    public List<string> Networks { get; private set; }
    public Dictionary<string, double> Fees { get; private set; }
    public ExchangePrice(double bidPrice, double askPrice, double bidVolume, double askVolume, long lastUpdate, List<string> networks /*, SymbolFee fee*/)
    {
        BidPrice = bidPrice;
        AskPrice = askPrice;
        BidVolume = bidVolume;
        AskVolume = askVolume;
        LastUpdate = lastUpdate;
        Networks = networks;
        // Fee = fee;
    }
}