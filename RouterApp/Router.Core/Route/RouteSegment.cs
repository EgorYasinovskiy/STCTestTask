namespace Router.Core.Route;

public class RouteSegment
{
    public string Text { get; set; }
    public RouteSegmentType Type { get; set; }
    public string? DynamicName { get; set; }
    public Type? DynamicType { get; set; }
    
}