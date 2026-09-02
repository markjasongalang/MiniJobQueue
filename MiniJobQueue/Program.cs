using System.Diagnostics;
using MiniJobQueue;

public class Program
{
    // Producer
    private static async Task SeedJobsAsync(JobQueue queue, int count)
    {
        for (var i = 1; i <= count; i++)
        {
            string jobName = i % 25 == 0 ? "Failing Job" : $"Job #{i}";
            await queue.EnqueueAsync(new Job(i, jobName));
        }

        queue.Complete(); // Signal completion (jobs are all enqueued)
    }

    public static async Task Main(string[] args)
    {
        var queue = new JobQueue();

        await SeedJobsAsync(queue, count: 100);

        // Graceful cancellation
        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (sender, eventArgs) => // Event handler
        {
            Logger.LogWarning("System", "Shutdown requested. Cancelling pending work...");
            eventArgs.Cancel = true; // Prevents process from terminating immediately
            cts.Cancel(); // Signals workers to wrap up cleanly
        };

        var stopWatch = new Stopwatch();
        stopWatch.Restart();

        int workerCount = 2;

        // Spin up multiple workers
        Task[] workerTasks = Enumerable.Range(1, workerCount)
            .Select(id => WorkerPool.RunWorkerAsync(id, queue, cts.Token))
            .ToArray(); // .ToArray() materializes the collection so execution starts immediately

        // Await all workers concurrently
        await Task.WhenAll(workerTasks);

        stopWatch.Stop();

        TimeSpan ts = stopWatch.Elapsed;
        string elapsedTime = $"{ts.Hours:00}:{ts.Minutes:00}:{ts.Seconds:00}.{ts.Milliseconds / 10:00}";

        Logger.LogInfo("RunTime", elapsedTime);
    }
}
