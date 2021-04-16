using System;
using System.Net.Http;
using HdrHistogram;
using NBomber;
using NBomber.Contracts;
using NBomber.CSharp;
using static NBomber.Time;

namespace CSharpDev.HttpTests
{
    public class SimpleHttpTest
    {
        public static void Run()
        {
            using var httpClient = new HttpClient();

            var step = Step.Create("fetch_html_page", async context =>
            {
                var response = await httpClient.GetAsync("https://nbomber.com", context.CancellationToken);

                return response.IsSuccessStatusCode
                    ? Response.Ok(statusCode: (int) response.StatusCode)
                    : Response.Fail(statusCode: (int) response.StatusCode);
            });

            var scenario = ScenarioBuilder
                .CreateScenario("simple_http", step)
                .WithWarmUpDuration(Seconds(5))
                .WithLoadSimulations(
                    Simulation.InjectPerSec(rate: 1, during: TimeSpan.FromSeconds(30))
                );

            NBomberRunner
                .RegisterScenarios(scenario)
                .Run();
        }
    }
}
