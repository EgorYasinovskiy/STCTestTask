namespace Router.Core.Route;

public class RouteNode
{
    public Dictionary<string, RouteNode>? StaticChildren { get; private set; }
    public Dictionary<Type, RouteNode>? DynamicChildren { get; private set; }

    public string? DynamicName { get; set; }
    public Endpoint? Endpoint { get; set; }

    public RouteNode GetOrAddStatic(string segment)
    {
        if (StaticChildren is null)
        {
            StaticChildren = new Dictionary<string, RouteNode>(StringComparer.OrdinalIgnoreCase);
        }

        if (!StaticChildren.TryGetValue(segment, out var node))
        {
            node = new RouteNode();
            StaticChildren[segment] = node;
        }

        return node;
    }

    public RouteNode GetOrAddDynamic(string name, Type type)
    {
        if (DynamicChildren is null)
        {
            DynamicChildren = new Dictionary<Type, RouteNode>();
        }

        if (!DynamicChildren.TryGetValue(type, out var node))
        {
            node = new RouteNode
            {
                DynamicName = name
            };
            DynamicChildren[type] = node;
        }
        else if (!string.Equals(node.DynamicName, name, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                $"Конфликт имён динамического сегмента для типа {type.Name}: '{node.DynamicName}' != '{name}'");
        }

        return node;
    }
}
