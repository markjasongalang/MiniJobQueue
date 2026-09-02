# MiniJobQueue

A minimal, zero-dependency, in-memory job queue built with C# and .NET 10. Demonstrates the **Producer-Consumer** pattern using `System.Threading.Channels` and async streams.

## Features

- Thread-safe async queue via `System.Threading.Channels`
- Configurable worker pool with concurrent job execution
- Graceful shutdown via `CancellationToken` (Ctrl+C)
- Per-job error handling — one bad job doesn't crash the workers
- Zero-allocation `ValueTask` writes
- Color-coded, thread-safe console logging
- Deliberate failure injection for demonstration purposes

## Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download)

## Build & Run

```bash
# Build
dotnet build

# Run
dotnet run --project MiniJobQueue/MiniJobQueue.csproj
```

Press **Ctrl+C** at any time to trigger graceful cancellation.

## Project Structure

```
MiniJobQueue/
├── Program.cs       # Entry point — seeds jobs, starts workers, handles shutdown
├── Job.cs           # Immutable job entity with ExecuteAsync
├── JobQueue.cs      # Thread-safe channel-based queue (producer API)
├── WorkerPool.cs    # Consumer loop — pulls jobs and executes them
└── Logger.cs        # Thread-safe, color-coded console logger
```

## Architecture Sequence Diagram

```plantuml
@startuml MiniJobQueue_Architecture

autonumber
skinparam Style strictuml
skinparam SequenceMessageAlignment center

actor "User / OS" as OS
participant "Program (Main)" as Main
participant "JobQueue" as Queue
participant "WorkerPool\n(Worker #1)" as W1
participant "WorkerPool\n(Worker #2)" as W2
participant "Job" as Job

== 1. Job Enqueuing (Producer) ==
activate Main
loop 100 times
    Main -> Queue : EnqueueAsync(job, ct)
    note right: Zero-allocation ValueTask\n(unbounded channel write)
    Queue --> Main : complete
end
Main -> Queue : Complete()
note right: Signals no more jobs will be added

== 2. Concurrent Worker Execution ==
Main -> W1 ** : RunWorkerAsync(id: 1, queue, ct)
activate W1
Main -> W2 ** : RunWorkerAsync(id: 2, queue, ct)
activate W2

par Worker Processing Loop
    W1 -> Queue : ReadAllAsync(ct)
    Queue --> W1 : job #1
    W1 -> Job : ExecuteAsync(workerId: 1, ct)
    activate Job
    Job --> W1 : completed
    deactivate Job
else
    W2 -> Queue : ReadAllAsync(ct)
    Queue --> W2 : job #2 (Failing Job)
    W2 -> Job : ExecuteAsync(workerId: 2, ct)
    activate Job
    Job --x W2 : throws InvalidOperationException
    deactivate Job
    note over W2: Inner try/catch logs error\nand keeps worker loop alive!
end

== 3. Graceful Cancellation (Ctrl+C) ==
OS -> Main : Console.CancelKeyPress (SIGINT)
note over Main: eventArgs.Cancel = true\ncts.Cancel()
Main -> W1 : CancellationToken triggered
W1 -> W1 : Catch OperationCanceledException & exit loop
deactivate W1
Main -> W2 : CancellationToken triggered
W2 -> W2 : Catch OperationCanceledException & exit loop
deactivate W2

Main -> OS : Log Total RunTime & Exit
deactivate Main

@enduml
```

## How It Works

1. **Producer** — `Program.SeedJobsAsync` enqueues 100 jobs into the channel. Every 25th job is a `"Failing Job"` that deliberately throws on execution.
2. **Workers** — Two workers run concurrently. Each iterates over the async stream (`await foreach`) and executes jobs as they become available.
3. **Cancellation** — Pressing Ctrl+C triggers a `CancellationToken`, causing workers to catch `OperationCanceledException` and exit their loops cleanly.

## Design Decisions

| Decision | Rationale |
|---|---|
| `System.Threading.Channels` over `BlockingCollection<T>` | Modern async-first API; integrates naturally with `await foreach` |
| Unbounded channel | Simplicity; no backpressure needed for a demo |
| `ValueTask` on `EnqueueAsync` | Unbounded channel writes complete synchronously — avoids heap allocation |
| Immutable `Job` properties | Safer for concurrent access; no accidental mutation |
| Per-worker try/catch | Fault tolerance — one bad job doesn't terminate the worker |
| `IAsyncEnumerable<T>` over `.Reader` directly | Encapsulates channel internals; exposes a clean domain abstraction |
