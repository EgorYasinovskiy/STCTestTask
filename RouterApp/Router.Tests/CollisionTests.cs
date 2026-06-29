using Xunit;

namespace Router.Tests;

public class CollisionTests
{
    [Fact]
    public void DuplicateRoute_Throws()
    {
        var router = new Router.Core.Router();
        router.RegisterRoute("/foo/bar/", () => { });

        Assert.Throws<InvalidOperationException>(() =>
            router.RegisterRoute("/foo/bar/", () => { }));
    }

    [Fact]
    public void StaticRoute_ShadowsDynamic_ByExactMatch()
    {
        var router = new Router.Core.Router();
        bool staticHit = false, dynHit = false;
        router.RegisterRoute("/foo/static/", () => staticHit = true);
        router.RegisterRoute("/foo/{name:string}/", (string name) =>
        {
            dynHit = true;
            Assert.Equal("static", name);
        });

        router.Route("/foo/static/");
        Assert.True(staticHit);
        Assert.False(dynHit);
    }

    [Fact]
    public void DynamicDynamic_DifferentTypes_Coexist()
    {
        var router = new Router.Core.Router();
        int? intHit = null;
        Guid? guidHit = null;
        router.RegisterRoute("/x/{v:int}/", (int v) => intHit = v);
        router.RegisterRoute("/x/{v:guid}/", (Guid v) => guidHit = v);

        router.Route("/x/123/");
        Assert.Equal(123, intHit);
        Assert.Null(guidHit);

        var g = Guid.NewGuid();
        router.Route($"/x/{g}/");
        Assert.Equal(g, guidHit);
    }

    [Fact]
    public void DynamicString_AcceptsNumericString_AsString()
    {
        var router = new Router.Core.Router();
        string? s = null;
        router.RegisterRoute("/x/{v:string}/", (string v) => s = v);

        router.Route("/x/123/");

        Assert.Equal("123", s);
    }

    [Fact]
    public void DynamicNameConflict_Throws()
    {
        var router = new Router.Core.Router();
        router.RegisterRoute("/x/{a:int}/", (int a) => { });

        Assert.Throws<ArgumentException>(() =>
            router.RegisterRoute("/x/{b:int}/", (int b) => { }));
    }

    [Fact]
    public void DelegateParamMissing_Throws()
    {
        var router = new Router.Core.Router();

        Assert.Throws<ArgumentException>(() =>
            router.RegisterRoute("/x/{a:int}/{b:int}/", (int a) => { }));
    }

    [Fact]
    public void TemplateSegmentMissingDelegateParam_Throws()
    {
        var router = new Router.Core.Router();

        Assert.Throws<ArgumentException>(() =>
            router.RegisterRoute("/x/{a:int}/", (int a, int b) => { }));
    }

    [Fact]
    public void TypeMismatch_Throws()
    {
        var router = new Router.Core.Router();

        Assert.Throws<ArgumentException>(() =>
            router.RegisterRoute("/x/{a:int}/", (string a) => { }));
    }
}
