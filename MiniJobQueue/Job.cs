namespace MiniJobQueue;

public class Job
{
    // No setters since these shouldn't really change after creation
    public long Id { get;  } 
    public string Name { get; }

    public Job(long id, string name)
    {
        Id = id;
        Name = name;
    }

    public async Task ExecuteAsync(long workerId, CancellationToken ct = default)
    {
        var category = $"Worker #{workerId}";

        Logger.LogInfo(category, $"Processing: {Name}");

        if (Name == "Failing Job")
        {
            // Deliberate failure
            throw new InvalidOperationException("Database connection timeout.");
        }

        await Task.Delay(1000, ct);

        Logger.LogSuccess(category, $"Completed: {Name}");
    }
}
