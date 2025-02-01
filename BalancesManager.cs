using ccxt;
using Newtonsoft.Json;

namespace OneMoreSpreadSearcher;
public class MyBalances
{
    public Dictionary<string, double> Coins = new();
    public string Exchange;
    
    public MyBalances(ExchangeEnum exchange)
    {
        Exchange = exchange.ToString();
    }
    
}
public class BalancesManager(RabbitMqService rabbitMqService)
{
    private RabbitMqService _rabbitMqService = rabbitMqService;

    public async Task UpdateBalancesAsync(IEnumerable<ManagerBase> managers)
    {
        while (true)
        {
            await Task.Delay(6000);
            var balances = await GetBalancesAsync(managers);
            var json = ConvertBalancesToJson(balances);
            _rabbitMqService.SendMessage(json);
        }
    }
    private static async Task<Dictionary<ExchangeEnum, Balances>> GetBalancesAsync(IEnumerable<ManagerBase> managers)
    {
        var balances = new Dictionary<ExchangeEnum, Balances>();
        var arManagers = managers.ToArray();
        
        var tasks = managers.Select(manager => manager.GetBalancesAsync()).ToList();

        var loadedBalances = await Task.WhenAll(tasks);

        for(var i = 0; i < arManagers.Length; i++)
            balances[arManagers[i].ExchangeType] = loadedBalances[i];
        
        return balances;
    }
    
    private static string ConvertBalancesToJson(Dictionary<ExchangeEnum, Balances> balances)
    {
        var myBalances = new List<MyBalances>();
        
        foreach(var balance in balances)
        {
            var myBalance = new MyBalances(balance.Key);
            foreach(var coin in balance.Value.total.Where(e => e.Value > 0))
                myBalance.Coins[coin.Key] = coin.Value;
            
            myBalances.Add(myBalance);
        }
        return JsonConvert.SerializeObject(myBalances);
    }
}