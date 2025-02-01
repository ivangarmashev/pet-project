namespace OneMoreSpreadSearcher;

public class TickerEventArgs : EventArgs
{
    public string Symbol { get; }
    public ExchangeEnum Exchange { get; }
    

    public TickerEventArgs(string symbol, ExchangeEnum exchange)
    {
        Symbol = symbol;
        Exchange = exchange;
    }
}