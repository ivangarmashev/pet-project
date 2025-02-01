namespace OneMoreSpreadSearcher.Exchanges;

public class OkxManager(string apiKey, string apiSecret, string passPhrase) : ManagerBase(apiKey, apiSecret, passPhrase)
{
    public override ExchangeEnum ExchangeType => ExchangeEnum.Okx;
}