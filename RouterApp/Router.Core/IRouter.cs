namespace Router.Core;

public interface IRouter
{
    void RegisterRoute(string template, Delegate action);
    void Route(string route);

    Task RouteAsync(string route, CancellationToken cancellationToken = default);
}
