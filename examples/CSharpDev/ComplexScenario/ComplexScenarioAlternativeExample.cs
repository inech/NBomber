using System;
using System.Linq;
using System.Threading.Tasks;
using NBomber.Contracts;
using NBomber.CSharp;

namespace CSharpDev.ComplexScenario;

public class ComplexScenarioAlternativeExample
{
    public static void Run()
    {
        // Simulate user scenario:
        // - Login
        // - Refresh token should be called at least 10 times but with a delay between calls
        // - Place bet
        // - Poll History until bet appears
        // We need to:
        // - Track throughput and latency of each individual api call
        var step = Step.Create("login_place_bet", async context =>
        {
            var fooApi = new FooApi(context.Logger);

            var authToken = context.Measure("login", () =>
                MyResponse.Ok(fooApi.Login("testUser", "testPassword")));

            for (int i = 0; i < 10; i++)
            {
                authToken = context.Measure("refresh", () =>
                    MyResponse.Ok(fooApi.RefreshToken(authToken)));
                await Task.Delay(TimeSpan.FromMilliseconds(500));
            }

            var betId = context.Measure("placeBet", () =>
                MyResponse.Ok(fooApi.PlaceBet(authToken, "SOME_SELECTION")));

            var checkHistoryRemainingAttempts = 10;
            while (true)
            {
                var historyBets = context.Measure("checkHistory", () =>
                    MyResponse.Ok(fooApi.GetPlacedBetsHistory(authToken)));
                if (historyBets.Contains(betId))
                {
                    break;
                }

                if (--checkHistoryRemainingAttempts <= 0)
                {
                    return Response.Fail("Bet not found after 10 tries.");
                }
            }

            return Response.Ok();
        });
        var scenario = ScenarioBuilder
            .CreateScenario("login_place_bet", step)
            .WithoutWarmUp()
            .WithLoadSimulations(
                Simulation.KeepConstant(copies: 1, during: TimeSpan.FromSeconds(10))
            );
        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .WithTestSuite("example")
            .WithTestName("complex_scenario_alternative_test")
            .Run();
    }
}

