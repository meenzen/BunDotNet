using Spectre.Console;

namespace BunDotNet.Cli;

public static class ProgressBar
{
    public static async Task<T> RunAsync<T>(Func<Action<BunInstaller.DownloadProgress>, Task<T>> action) =>
        await AnsiConsole
            .Progress()
            .AutoClear(true)
            .HideCompleted(true)
            .Columns(
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new RemainingTimeColumn()
            )
            .StartAsync(async ctx =>
            {
                var configuring = ctx.AddTask("Configuring").IsIndeterminate();
                configuring.StartTask();
                ProgressTask? task = null;
                var result = await action(progress =>
                {
                    configuring.StopTask();
                    if (task is null)
                    {
                        task = ctx.AddTask("Downloading", autoStart: false);
                        task.MaxValue = progress.Total ?? 100;
                        task.IsIndeterminate = progress.Total is null;
                        task.StartTask();
                    }

                    task.Value = progress.Read;
                });
                task?.StopTask();
                return result;
            });
}
