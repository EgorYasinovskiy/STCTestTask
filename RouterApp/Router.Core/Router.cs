using System.Reflection;
using System.Runtime.ExceptionServices;
using Router.Core.Helpers;
using Router.Core.Route;

namespace Router.Core;

public class Router : IRouter
{
    private readonly RouteNode _root = new();

    public void RegisterRoute(string template, Delegate action)
    {
        if (action is null) throw new ArgumentNullException(nameof(action));

        var segments = TemplateParser.ParseTemplate(template);

        var segmentNames = segments
            .Where(s => s.Type == RouteSegmentType.Dynamic)
            .Select(s => s.DynamicName!)
            .ToArray();
        var segmentTypes = segments
            .Where(s => s.Type == RouteSegmentType.Dynamic)
            .Select(s => s.DynamicType!)
            .ToArray();

        var endpoint = new Endpoint(action, segmentNames, segmentTypes);

        var current = _root;
        foreach (var segment in segments)
        {
            current = segment.Type == RouteSegmentType.Static
                ? current.GetOrAddStatic(segment.Text)
                : current.GetOrAddDynamic(segment.DynamicName!, segment.DynamicType!);
        }

        if (current.Endpoint is not null)
            throw new InvalidOperationException(
                $"Маршрут '{template}' уже зарегистрирован (предыдущий обработчик: {current.Endpoint.Action.Method.Name})");

        current.Endpoint = endpoint;
    }

    public void Route(string route) => RouteAsync(route, CancellationToken.None).GetAwaiter().GetResult();

    public async Task RouteAsync(string route, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(route))
            throw new ArgumentException("Маршрут пуст", nameof(route));

        var segments = route.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (!TryResolve(_root, segments, 0, out var endpoint, out var values))
            throw new InvalidOperationException($"Маршрут '{route}' не найден");

        var args = endpoint.BuildArgs(values, cancellationToken);

        if (endpoint.IsAsync)
        {
            try
            {
                var task = (Task)endpoint.Action.DynamicInvoke(args)!;
                await task.ConfigureAwait(false);
            }
            catch (TargetInvocationException tie) when (tie.InnerException is not null)
            {
                ExceptionDispatchInfo.Capture(tie.InnerException).Throw();
            }
        }
        else
        {
            try
            {
                endpoint.Action.DynamicInvoke(args);
            }
            catch (TargetInvocationException tie) when (tie.InnerException is not null)
            {
                ExceptionDispatchInfo.Capture(tie.InnerException).Throw();
            }
        }
    }

    private static bool TryResolve(
        RouteNode node,
        string[] segments,
        int index,
        out Endpoint endpoint,
        out object?[] values)
    {
        if (index == segments.Length)
        {
            endpoint = node.Endpoint!;
            values = Array.Empty<object?>();
            return node.Endpoint is not null;
        }

        var segment = segments[index];

        if (node.StaticChildren is not null
            && node.StaticChildren.TryGetValue(segment, out var staticChild)
            && TryResolve(staticChild, segments, index + 1, out endpoint, out values))
        {
            return true;
        }

        if (node.DynamicChildren is not null)
        {
            foreach (var kvp in node.DynamicChildren)
            {
                if (!TypeConverters.TryConvert(segment, kvp.Key, out var parsed) || parsed is null)
                    continue;

                if (TryResolve(kvp.Value, segments, index + 1, out endpoint, out var innerValues))
                {
                    values = new object?[innerValues.Length + 1];
                    values[0] = parsed;
                    Array.Copy(innerValues, 0, values, 1, innerValues.Length);
                    return true;
                }
            }
        }

        endpoint = null!;
        values = null!;
        return false;
    }
}
