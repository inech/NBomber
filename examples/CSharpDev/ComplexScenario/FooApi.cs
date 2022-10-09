using System;
using Serilog;

namespace CSharpDev.ComplexScenario;

public class FooApi
{
    private readonly ILogger _logger;

    public FooApi(ILogger logger)
    {
        _logger = logger;
    }

    public string Login(string user, string password)
    {
        _logger.Information("Simulate login.");
        return "SOME_AUTH_TOKEN";
    }

    public string RefreshToken(string token)
    {
        _logger.Information("Simulate token refresh.");
        return token;
    }

    public string PlaceBet(string authToken, string selection)
    {
        _logger.Information("Simulate place bet.");
        return "SOME_BET_ID";
    }

    private int _historyCallCount;
    public string[] GetPlacedBetsHistory(string authToken)
    {
        _historyCallCount++;
        if (_historyCallCount >= 5)
        {
            _logger.Information("Simulate get history: bet appeared.");
            return new[] {"SOME_BET_ID"};
        }
        else
        {
            _logger.Information("Simulate get history: bet not appeared yet.");
            return Array.Empty<string>();
        }
    }
}
