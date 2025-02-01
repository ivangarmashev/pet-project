using ccxt;

namespace OneMoreSpreadSearcher.Exchanges;

public class MexcManager(string apiKey, string apiSecret) : ManagerBase(apiKey, apiSecret)
{
    public override ExchangeEnum ExchangeType => ExchangeEnum.Mexc;
    protected override Task SetupTickers()
    {
        return base.SetupTickers();
    }
}
