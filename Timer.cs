using System.Diagnostics;

namespace OneMoreSpreadSearcher;

public class Timer
{
    public Dictionary<string, List<long>> Checks = new Dictionary<string, List<long>>();
    public Dictionary<string, long> AvgChecks => Checks.Where(check => check.Value.Count > 0).ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Sum()/kvp.Value.Count);
    public Dictionary<string, int> ChecksCount => Checks.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Count);
    public int NotCheckCount => Checks.Count(check => check.Value.Count == 0);
    public Stopwatch extSw;

    public Timer()
    {
        
    }
    public Timer(List<string> symbols)
    {
        foreach (var symbol in symbols)
        {
            Checks.Add(symbol, []);
        }
    }
    

    public void Add(string symbol, long value)
    {
        if (Checks.ContainsKey(symbol))
            Checks[symbol].Add(value);
        else
            Checks.Add(symbol, [value]);
    }
}