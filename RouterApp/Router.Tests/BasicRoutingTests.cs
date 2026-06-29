using Xunit;

namespace Router.Tests;

public class BasicRoutingTests
{
    [Fact]
    public void StaticRoute_InvokesHandler()
    {
        var router = new Router.Core.Router();
        var called = false;
        router.RegisterRoute("/foo/bar/", () => called = true);

        router.Route("/foo/bar/");

        Assert.True(called);
    }

    [Fact]
    public void StaticRoute_CaseInsensitive()
    {
        var router = new Router.Core.Router();
        var called = false;
        router.RegisterRoute("/Foo/Bar/", () => called = true);

        router.Route("/foo/BAR/");

        Assert.True(called);
    }

    [Fact]
    public void DynamicInt_ParsesAndPasses()
    {
        var router = new Router.Core.Router();
        int captured = 0;
        router.RegisterRoute("/foo/bar/{p:int}/", (int p) => captured = p);

        router.Route("/foo/bar/123/");

        Assert.Equal(123, captured);
    }

    [Fact]
    public void DynamicString_DefaultsToString()
    {
        var router = new Router.Core.Router();
        string? captured = null;
        router.RegisterRoute("/foo/{name}/", (string name) => captured = name);

        router.Route("/foo/alice/");

        Assert.Equal("alice", captured);
    }

    [Fact]
    public void DynamicGuid_Parses()
    {
        var router = new Router.Core.Router();
        var guid = Guid.NewGuid();
        Guid? captured = null;
        router.RegisterRoute("/users/{id:guid}/", (Guid id) => captured = id);

        router.Route($"/users/{guid}/");

        Assert.Equal(guid, captured);
    }

    [Fact]
    public void DynamicDateTime_Parses()
    {
        var router = new Router.Core.Router();
        var dt = new DateTime(2024, 5, 12, 0, 0, 0, DateTimeKind.Utc);
        DateTime? captured = null;
        router.RegisterRoute("/datetimeroute/{at:datetime}/", (DateTime at) => captured = at);

        router.Route($"/datetimeroute/{dt:o}/");

        Assert.Equal(dt, captured);
    }

    [Fact]
    public void UnknownRoute_Throws()
    {
        var router = new Router.Core.Router();
        router.RegisterRoute("/foo/", () => { });

        Assert.Throws<InvalidOperationException>(() => router.Route("/bar/"));
    }

    [Fact]
    public void EmptyRoute_Throws()
    {
        var router = new Router.Core.Router();
        Assert.Throws<ArgumentException>(() => router.Route(""));
    }

    [Fact]
    public void MixedStaticAndDynamicNotInOrder_RoutesCorrectly()
    {
        var router = new Router.Core.Router();
        int a = 0, b = 0;
        router.RegisterRoute("/api/v1/users/{id:int}/posts/{postId:int}/",
            (int postId, int id) => { a = id; b = postId; });

        router.Route("/api/v1/users/7/posts/42/");

        Assert.Equal(7, a);
        Assert.Equal(42, b);
    }
}
