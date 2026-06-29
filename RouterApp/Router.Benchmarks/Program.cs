using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

namespace Router.Benchmarks;

[MemoryDiagnoser]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
public class RouterBenchmarks
{
    private Router.Core.Router _router = null!;
    private string[] _samples = null!;

    [GlobalSetup]
    public void Setup()
    {
        var routes = GenerateRoutes(100).ToList();
        _router = new Router.Core.Router();
        foreach (var (template, sample, handler) in routes)
            _router.RegisterRoute(template, handler);

        _router.RegisterRoute("/api/v1/resource0/", (Action)(() => { }));
        _router.RegisterRoute("/foo/bar/{a:int}/", (Action<int>)(a => { }));

        _router.RegisterRoute("/api/v1/async0/", (Func<Task>)(async () => await Task.Yield()));
        _router.RegisterRoute("/api/v1/async1/{a:int}/",
            (Func<int, Task>)(async p0 => await Task.Yield()));
        _router.RegisterRoute("/api/v1/async2/a:int}/{b:string}/",
            (Func<int, string, Task>)(async (a, b) => await Task.Yield()));

        _samples = routes.Select(r => r.Sample).ToArray();
    }

    [Benchmark(Baseline = true)]
    public void Route_StaticOnly()
    {
        for (var i = 0; i < 1000; i++)
            _router.Route("/api/v1/resource0/");
    }

    [Benchmark]
    public void Route_Mixed_100Routes()
    {
        for (var i = 0; i < 1000; i++)
            _router.Route(_samples[i % _samples.Length]);
    }

    [Benchmark]
    public void Route_SingleDynamic_1000Times()
    {
        for (var i = 0; i < 1000; i++)
            _router.Route("/foo/bar/42/");
    }

    [Benchmark]
    public async Task RouteAsync_Static_1000Times()
    {
        for (var i = 0; i < 1000; i++)
            await _router.RouteAsync("/api/v1/resource0/");
    }

    [Benchmark]
    public async Task RouteAsync_SingleDynamic_1000Times()
    {
        for (var i = 0; i < 1000; i++)
            await _router.RouteAsync("/api/v1/async1/42/");
    }

    [Benchmark]
    public async Task RouteAsync_TwoDynamic_1000Times()
    {
        for (var i = 0; i < 1000; i++)
            await _router.RouteAsync("/api/v1/async2/42/hello/");
    }

    [Benchmark]
    public async Task RouteAsync_LongRunning_Handlers_Parallel()
    {
        await Task.WhenAll(
            _router.RouteAsync("/api/v1/async1/1/"),
            _router.RouteAsync("/api/v1/async1/2/"),
            _router.RouteAsync("/api/v1/async1/3/"),
            _router.RouteAsync("/api/v1/async1/4/"),
            _router.RouteAsync("/api/v1/async1/5/"),
            _router.RouteAsync("/api/v1/async1/6/"),
            _router.RouteAsync("/api/v1/async1/7/"),
            _router.RouteAsync("/api/v1/async1/8/"));
    }

    [Benchmark]
    public async Task RouteAsync_Concurrent_1000Calls()
    {
        var tasks = new Task[1000];
        for (var i = 0; i < 1000; i++)
            tasks[i] = _router.RouteAsync("/api/v1/async0/");
        await Task.WhenAll(tasks);
    }

    [Benchmark]
    public void Route_Sync_ConcurrentLoad_1000Calls_ParallelFor()
    {
        Parallel.For(0, 1000, i => _router.Route("/api/v1/resource0/"));
    }

    private static IEnumerable<(string Template, string Sample, Delegate Handler)> GenerateRoutes(int count)
    {
        var rng = new Random(42);
        for (var i = 0; i < count; i++)
        {
            var numOfArgs = rng.Next(0, 4);
            var segments = new List<string> { "api", "v1" };
            var fixedTypes = new[] { "int", "string", "guid" };
            for (var a = 0; a < numOfArgs; a++)
            {
                segments.Add($"{{a{a}:{fixedTypes[a]}}}");
            }
            segments.Add($"resource{i}");

            var template = "/" + string.Join("/", segments) + "/";
            var sample = "/" + string.Join("/", segments.Select(s =>
            {
                if (s.StartsWith("{"))
                {
                    var t = s[(s.IndexOf(':') + 1)..^1];
                    return t switch
                    {
                        "int" => "42",
                        "string" => "hello",
                        "guid" => "3f2504e0-4f89-11d3-9a0c-0305e82c3301",
                        _ => "x"
                    };
                }
                return s;
            })) + "/";

            Delegate handler = numOfArgs switch
            {
                0 => (Action)(() => { }),
                1 => (Action<int>)(a0 => { }),
                2 => (Action<int, string>)((a0, a1) => { }),
                3 => (Action<int, string, Guid>)((a0, a1, a2) => { }),
                _ => throw new InvalidOperationException()
            };

            yield return (template, sample, handler);
        }
    }
}

public static class Program
{
    public static void Main() => BenchmarkRunner.Run<RouterBenchmarks>();
}
