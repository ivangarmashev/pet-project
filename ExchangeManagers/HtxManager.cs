using ccxt;

namespace OneMoreSpreadSearcher.Exchanges;

public class HtxManager(string apiKey, string apiSecret) : ManagerBase(apiKey, apiSecret)
{
    public override ExchangeEnum ExchangeType => ExchangeEnum.Htx;
    protected internal override async Task SetupClient()
    {
        await base.SetupClient();
    }
}