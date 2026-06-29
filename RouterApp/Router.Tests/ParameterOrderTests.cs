using Xunit;

namespace Router.Tests;

public class ParameterOrderTests
{
    [Fact]
    public void DelegateParamOrder_DoesNotMatter()
    {
        var router = new Router.Core.Router();
        int capturedA = 0, capturedB = 0;
        router.RegisterRoute("/foo/bar/{a:int}/{b:int}/",
            (int b, int a) => { capturedB = b; capturedA = a; });

        router.Route("/foo/bar/1/2/");

        Assert.Equal(1, capturedA);
        Assert.Equal(2, capturedB);
    }

    [Fact]
    public void ReverseOrder_ThreeParams()
    {
        var router = new Router.Core.Router();
        int x = 0, y = 0, z = 0;
        router.RegisterRoute("/a/{xParam:int}/{yParam:int}/{zParam:int}/",
            (int zParam, int yParam, int xParam) => { x = xParam; y = yParam; z = zParam; });

        router.Route("/a/10/20/30/");

        Assert.Equal(10, x);
        Assert.Equal(20, y);
        Assert.Equal(30, z);
    }

    [Fact]
    public void ParamNameCaseInsensitive()
    {
        var router = new Router.Core.Router();
        int captured = 0;
        router.RegisterRoute("/foo/{PAGE:int}/", (int page) => captured = page);

        router.Route("/foo/5/");

        Assert.Equal(5, captured);
    }
}
