using Router.Core.Helpers;
using Router.Core.Route;

namespace Router.Core;

public static class TemplateParser
{
    public static IReadOnlyList<RouteSegment> ParseTemplate(string template)
    {
        if (string.IsNullOrEmpty(template))
            throw new ArgumentException("Шаблон пути пуст");

        var rawSegments = template.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var result = new List<RouteSegment>(rawSegments.Length);

        foreach (var rawSegment in rawSegments)
        {
            if (rawSegment.Length == 0)
                throw new ArgumentException("Сегмент пуст");

            if (rawSegment[0] == '{' && rawSegment[^1] == '}')
            {
                var inner = rawSegment[1..^1];
                var colon = inner.IndexOf(':');
                string name;
                string typename;
                if (colon == -1)
                {
                    name = inner;
                    typename = "string";
                }
                else
                {
                    name = inner[..colon];
                    typename = inner[(colon + 1)..];
                }

                if (name.Length == 0)
                    throw new ArgumentException("Параметры внутри шаблона должны иметь имя");

                var type = TypeConverters.ResolveType(typename);
                result.Add(new RouteSegment
                {
                    DynamicName = name,
                    DynamicType = type,
                    Type = RouteSegmentType.Dynamic
                });
            }
            else
            {
                result.Add(new RouteSegment
                {
                    Text = rawSegment,
                    Type = RouteSegmentType.Static
                });
            }
        }

        return result;
    }
}
