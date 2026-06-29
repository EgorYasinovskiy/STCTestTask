using Xunit;

namespace Router.Tests;

public class AsyncRoutingTests
{
    [Fact]
    public async Task AsyncHandler_AwaitsResult()
    {
        var router = new Router.Core.Router();
        var tcs = new TaskCompletionSource();
        router.RegisterRoute("/slow/{n:int}/", async (int n) =>
        {
            await Task.Delay(10);
            tcs.SetResult();
        });

        await router.RouteAsync("/slow/1/");

        Assert.True(tcs.Task.IsCompleted);
    }

    [Fact]
    public async Task CancellationToken_IsPassedToHandler()
    {
        var router = new Router.Core.Router();
        CancellationToken captured = default;
        router.RegisterRoute("/cancel/{n:int}/", async (int n, CancellationToken ct) =>
        {
            captured = ct;
            await Task.Yield();
        });

        using var cts = new CancellationTokenSource();
        await router.RouteAsync("/cancel/1/", cts.Token);

        Assert.Equal(cts.Token, captured);
    }

    [Fact]
    public async Task CancellationToken_PropagatesToHandler()
    {
        var router = new Router.Core.Router();
        router.RegisterRoute("/cancel/{n:int}/", (int n, CancellationToken ct) =>
        {
            ct.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        });

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => router.RouteAsync("/cancel/1/", cts.Token));
    }

    [Fact]
    public async Task SyncHandler_RouteAsync_RunsSynchronously()
    {
        var router = new Router.Core.Router();
        bool called = false;
        router.RegisterRoute("/foo/", () => called = true);

        await router.RouteAsync("/foo/");

        Assert.True(called);
    }

    [Fact]
    public async Task LongRunningHandler_DoesNotBlockCaller()
    {
        var router = new Router.Core.Router();
        router.RegisterRoute("/long/{ms:int}/", async (int ms) =>
        {
            await Task.Delay(ms);
        });

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var task = router.RouteAsync("/long/200/");
        sw.Stop();

        Assert.True(sw.ElapsedMilliseconds < 50,
            $"RouteAsync вернул задачу слишком поздно ({sw.ElapsedMilliseconds} мс), похоже, что он блокирует.");
        await task;
    }

    [Fact]
    public async Task LongRunningHandlers_RunInParallel()
    {
        var router = new Router.Core.Router();
        var inFlight = 0;
        var maxInFlight = 0;
        var lockObj = new object();

        router.RegisterRoute("/work/{ms:int}/", async (int ms) =>
        {
            lock (lockObj)
            {
                inFlight++;
                if (inFlight > maxInFlight) maxInFlight = inFlight;
            }
            await Task.Delay(ms);
            lock (lockObj) inFlight--;
        });

        var sw = System.Diagnostics.Stopwatch.StartNew();
        await Task.WhenAll(
            router.RouteAsync("/work/200/"),
            router.RouteAsync("/work/200/"),
            router.RouteAsync("/work/200/"),
            router.RouteAsync("/work/200/"));
        sw.Stop();

        Assert.True(maxInFlight > 1, "Параллельные вызовы не должны были сериализоваться.");
        Assert.True(sw.ElapsedMilliseconds < 600,
            $"Четыре длительных вызова по 200 мс заняли {sw.ElapsedMilliseconds} мс — подозрение на блокировку.");
    }

    [Fact]
    public async Task LongRunningHandler_UnderConcurrentLoad_AllComplete()
    {
        var router = new Router.Core.Router();
        var completed = 0;

        router.RegisterRoute("/job/{n:int}/", async (int n) =>
        {
            await Task.Delay(20);
            Interlocked.Increment(ref completed);
        });

        var calls = Enumerable.Range(0, 50).Select(i => router.RouteAsync($"/job/{i}/")).ToArray();
        await Task.WhenAll(calls);

        Assert.Equal(50, completed);
    }

    [Fact]
    public async Task AsyncHandler_ThatThrows_PropagatesException()
    {
        var router = new Router.Core.Router();
        router.RegisterRoute("/exception/{n:int}/", async (int n) =>
        {
            await Task.Delay(5);
            throw new InvalidOperationException("Exception!");
        });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => router.RouteAsync("/exception/1/"));
        Assert.Equal("Exception!", ex.Message);
    }

    [Fact]
    public async Task AsyncHandler_ThatThrows_DoesNotWrapInTargetInvocation()
    {
        var router = new Router.Core.Router();
        router.RegisterRoute("/exception2/{n:int}/", async (int n) =>
        {
            await Task.Yield();
            throw new ArgumentException("Exception!");
        });

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => router.RouteAsync("/exception2/1/"));
        Assert.Equal("Exception!", ex.Message);
    }

    [Fact]
    public async Task CancellationToken_DuringLongRunning_StopsHandler()
    {
        var router = new Router.Core.Router();
        router.RegisterRoute("/cancellable/{ms:int}/", async (int ms, CancellationToken ct) =>
        {
            await Task.Delay(ms, ct);
        });

        using var cts = new CancellationTokenSource(50);
        var sw = System.Diagnostics.Stopwatch.StartNew();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => router.RouteAsync("/cancellable/5000/", cts.Token));
        sw.Stop();

        Assert.True(sw.ElapsedMilliseconds < 500,
            $"Хэндлер не отменился вовремя (заняло {sw.ElapsedMilliseconds} мс).");
    }
}
