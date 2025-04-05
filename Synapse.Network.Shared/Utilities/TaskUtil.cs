namespace Synapse.Network.Shared.Utilities;

public static class TaskUtil {
    public static async Task RunWithTimeout(
        Func<CancellationToken, Task> func,
        Func<CancellationTokenSource, Task> timeoutFunc,
        TimeSpan timeout) {
        CancellationTokenSource tokenSource = new();
        var mainTask = Task.Run(() => func(tokenSource.Token), tokenSource.Token);

        if (await Task.WhenAny(mainTask, Task.Delay(timeout, tokenSource.Token)) == mainTask)
            await mainTask;
        else
            await timeoutFunc(tokenSource);
    }
}