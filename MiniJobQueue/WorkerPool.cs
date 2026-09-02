namespace MiniJobQueue;

public class WorkerPool
{
    public static async Task RunWorkerAsync(long workerId, JobQueue queue, CancellationToken ct = default)
    {
        try
        {
            // Worker interacts strictly with our domain abstraction
            await foreach (var job in queue.ReadAllAsync(ct))
            {
                try
                {
                    await job.ExecuteAsync(workerId, ct);
                }
                catch (OperationCanceledException)
                {
                    Logger.LogWarning($"Worker #{workerId}", "Task cancelled mid-execution. Exiting...");
                    break; // Exit loop on cancellation
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Worker #{workerId}", ex.Message);
                }
            }
        }
        catch (OperationCanceledException)
        {
            Logger.LogWarning($"Worker #{workerId}", "Channel reader cancelled. Exiting worker loop.");
        }
    }
}
