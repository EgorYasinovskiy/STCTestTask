using System.Diagnostics;
using Xunit;
using Xunit.Abstractions;

namespace Router.Tests;

public class LoadTests
{
    private readonly ITestOutputHelper _output;
    public LoadTests(ITestOutputHelper output) => _output = output;

    [Fact]
    public void GeneratedRoutes_AllResolve()
    {
        var routes = RouteGenerator.Generate(100);
        var router = new Router.Core.Router();
        foreach (var r in routes)
            router.RegisterRoute(r.Template, r.Handler);

        foreach (var r in routes)
            router.Route(r.Sample);
    }

    [Fact]
    public async Task GeneratedRoutes_ResolveUnderParallelLoad()
    {
        const int callsPerRoute = 200;
        var routes = RouteGenerator.Generate(100);
        var router = new Router.Core.Router();
        foreach (var r in routes)
            router.RegisterRoute(r.Template, r.Handler);

        var sw = Stopwatch.StartNew();
        await Parallel.ForEachAsync(
            Enumerable.Range(0, routes.Count * callsPerRoute),
            new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
            (_, _) =>
            {
                var idx = Random.Shared.Next(routes.Count);
                router.Route(routes[idx].Sample);
                return ValueTask.CompletedTask;
            });
        sw.Stop();

        var total = routes.Count * callsPerRoute;
        var rps = total / Math.Max(sw.Elapsed.TotalSeconds, 0.0001);
        _output.WriteLine($"Обработано {total} вызовов за {sw.ElapsedMilliseconds} мс ({rps:F0} RPS)");
    }
}
