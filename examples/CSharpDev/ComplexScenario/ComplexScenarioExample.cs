using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.FSharp.Core;
using NBomber.Contracts;
using NBomber.CSharp;

namespace CSharpDev.ComplexScenario;

public class ComplexScenarioExample
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
        var steps = CreateSteps().ToArray();
        var scenario = ScenarioBuilder
            .CreateScenario("login_place_bet", steps)
            .WithStepInterception(context =>
            {
                string nextStepName = null;
                if (context.IsNone)
                {
                    nextStepName = "login";
                }
                else
                {
                    var previousStepName = context.Value.PrevStepContext.StepName;
                    var contextData = context.Value.PrevStepContext.Data;

                    // Keep number of times the step has been executed in current scenario
                    var stepCounters = (Dictionary<string, int>) contextData.GetValueOrDefault("stepCounters", new Dictionary<string, int>());
                    contextData["stepCounters"] = stepCounters;
                    var previousStepCounter = stepCounters[previousStepName] = stepCounters.GetValueOrDefault(previousStepName) + 1;

                    if (previousStepName == "login")
                    {
                        nextStepName = "refresh";
                    }
                    else if (previousStepName == "refresh")
                    {
                        if (previousStepCounter >= 10)
                        {
                            nextStepName = "placeBet";
                        }
                        else
                        {
                            // Delay between repeats.
                            // Note: Current method does not support async.
                            Thread.Sleep(TimeSpan.FromMilliseconds(500));
                            nextStepName = "refresh";
                        }
                    }
                    else if (previousStepName == "placeBet")
                    {
                        nextStepName = "checkHistory";
                    }
                    else if (previousStepName == "checkHistory")
                    {
                        var bets = context.Value.PrevStepContext.GetPreviousStepResponse<string[]>();
                        var placedBetId = (string)contextData["betId"];
                        if (bets.Contains(placedBetId))
                        {
                            // Finish scenario
                            nextStepName = null;
                        }
                        else
                        {
                            if (previousStepCounter > 10)
                            {
                                throw new Exception("Bet not found after 10 tries.");
                            }
                            else
                            {
                                nextStepName = "checkHistory";
                            }
                        }
                    }
                }

                return nextStepName ?? FSharpValueOption<string>.ValueNone;
            })
            .WithoutWarmUp()
            .WithLoadSimulations(
                Simulation.KeepConstant(copies: 1, during: TimeSpan.FromSeconds(60))
            );
        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .WithTestSuite("example")
            .WithTestName("complex_scenario_test")
            .Run();
    }

    private static IEnumerable<IStep> CreateSteps()
    {
        yield return Step.Create("login", async context =>
        {
            var fooApi = new FooApi(context.Logger);
            context.Data["fooApi"] = fooApi;

            var authToken = fooApi.Login("testUser", "testPassword");
            context.Data["authToken"] = authToken;

            return Response.Ok(authToken);
        });

        // Logic to hit it 10 times with delays not visible here
        yield return Step.Create("refresh", async context =>
        {
            var fooApi = (FooApi) context.Data["fooApi"];
            var authToken = (string) context.Data["authToken"];

            context.Data["authToken"] = authToken = fooApi.RefreshToken(authToken);

            return Response.Ok(authToken);
        });

        yield return Step.Create("placeBet", async context =>
        {
            var fooApi = (FooApi) context.Data["fooApi"];
            var authToken = (string) context.Data["authToken"];

            var betId = fooApi.PlaceBet(authToken, "SOME_SELECTION");
            context.Data["betId"] = betId;
            return Response.Ok(betId);
        });

        // Retry logic not visible here, will be added later to interceptor
        yield return Step.Create("checkHistory", async context =>
        {
            var fooApi = (FooApi) context.Data["fooApi"];
            var authToken = (string) context.Data["authToken"];

            var historyBets = fooApi.GetPlacedBetsHistory(authToken);
            return Response.Ok(historyBets);
        });
    }
}
