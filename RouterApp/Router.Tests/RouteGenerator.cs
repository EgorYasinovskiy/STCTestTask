namespace Router.Tests;

public static class RouteGenerator
{
    public sealed record GeneratedRoute(string Template, string Sample, Delegate Handler);

    public static IReadOnlyList<GeneratedRoute> Generate(int count, int seed = 42)
    {
        var rng = new Random(seed);
        var routes = new List<GeneratedRoute>(count);
        var fixedTypes = new[] { "int", "string", "guid" };
        for (var i = 0; i < count; i++)
        {
            var segments = new List<string> { "api", "v1" };
            var arity = rng.Next(0, 4);
            for (var p = 0; p < arity; p++)
            {
                segments.Add($"{{p{p}:{fixedTypes[p]}}}");
            }
            segments.Add($"resource{i}");
            var template = "/" + string.Join("/", segments) + "/";

            var sample = "/" + string.Join("/", segments.Select(s =>
            {
                if (s.StartsWith("{"))
                {
                    var typeName = s[(s.IndexOf(':') + 1)..^1];
                    return typeName switch
                    {
                        "int" => "42",
                        "string" => "hello",
                        "guid" => "3f2504e0-4f89-11d3-9a0c-0305e82c3301",
                        _ => "x"
                    };
                }
                return s;
            })) + "/";

            Delegate handler = arity switch
            {
                0 => (Action)(() => { }),
                1 => (Action<int>)(p0 => { }),
                2 => (Action<int, string>)((p0, p1) => { }),
                3 => (Action<int, string, Guid>)((p0, p1, p2) => { }),
                _ => throw new InvalidOperationException()
            };

            routes.Add(new GeneratedRoute(template, sample, handler));
        }
        return routes;
    }
}
