# Order Processing System

## Design and storage

`Shared/Models` contains plain domain classes. `Shared/Storage` owns JSON access, seeding, and repositories. `Shared/Logging` serializes coloured console writes. `Phase1` validates orders, `Phase2` prices and renders invoices, `Phase3` runs checkout/payment/email compensation, and `Phase4` contains dashboard and shipping scale-up services.

Every collection is a camel-case, indented JSON array. `JsonFileStore` is the only component that reads or writes collection files. A per-file `SemaphoreSlim` protects each whole read-modify-write transaction. Writes go to a temporary file and are atomically moved over the target, so readers never see half a document. Missing files return an empty list and malformed JSON becomes `DataFileCorruptedException`.

## Measurements from this machine

| Work | Result |
|---|---:|
| Phase 1 async sequential validation | 52.9 s |
| Phase 2 concurrent validation and processing | about 12 s |
| Phase 2 final state | 13 completed, 6 rejected, 1 failed |
| Phase 3 checkout | about 18 s |
| Phase 3 final state | 11 completed, 6 rejected, 2 payment failed, 1 failed |

Machine timings vary because latency is random and CPU-bound work depends on the processor. Phase 4 prints its dashboard, quotes, fraud sequential/parallel timings, thread counts, and email-resend time during each run.

## Answers

### Phase 1

1. Sync and async take nearly the same time sequentially because the next order is not started until the current one finishes; async improves thread use, not the duration of the simulated waits.
2. A continuation may run on a different pool thread. My logs show repository steps moving between thread IDs 5, 7, 8 and 10 after awaits.
3. `Task.Delay` registers a timer and returns the thread to the pool. `Thread.Sleep` keeps that thread blocked.
4. `Task<T>` lets the caller continue without blocking and later await either the value, cancellation, or exception.
5. `ReadAllText` blocks its caller. `ReadAllTextAsync` uses asynchronous file operations and does not reserve a managed thread just to wait for I/O completion.

### Phase 2

1. Calling an async method starts it; awaiting observes its result without blocking the thread. Delaying the await lets independent operations overlap.
2. A task is an abstraction for future completion; it does not imply a dedicated thread. `Task.Delay` uses a timer.
3. `.Result` and `.Wait()` block and wrap exceptions; under a synchronization context they can deadlock by blocking the thread needed by the continuation.
4. An unobserved fault remains attached to its task and can surface through the unobserved-task exception mechanism. More importantly, the operation silently fails from the application's point of view.
5. `Task.Run` fits CPU work such as invoice rendering. Wrapping naturally asynchronous database/file/API calls only wastes a pool thread.
6. `Task` signals completion; `Task<T>` also supplies a result.
7. `lock` is thread-affine, while an await continuation may resume on a different thread. Use an async-compatible gate such as `SemaphoreSlim`.
8. Both unsafe reservations read stock 1 before either wrote, so each believed it succeeded. The fixed method locks the full read-modify-write operation through the store gate.

### Phase 3

1. Async does not shorten one order's fixed external latency. It scales because other orders use the released threads while one waits.
2. Ten blocking workflows require ten threads; ten async workflows spend most of their lives waiting and can share a small pool.
3. A thousand dedicated threads consume large stacks, add scheduling overhead, and can exhaust process resources.
4. Database, file and API work is mostly waiting, so async keeps server threads available for other requests.
5. Await is necessary when later correctness depends on completion or failure. Fire-and-forget is acceptable only when ownership, exception observation, lifetime and retry are explicitly handled by a durable background system.
6. `async void` cannot be awaited and routes exceptions directly to the context; event handlers are the exception because their delegate signature requires `void`.

### Phase 4

1. Live progress uses `Task.WhenEach`, the dashboard uses `Task.WhenAll`, shipping repeatedly uses `Task.WhenAny`, fraud scoring and email resend use `Parallel.ForEachAsync`.
2. After `WhenAll` finishes, inspect each faulted task's `Exception.InnerExceptions` to report all failures. A plain await throws one propagated exception.
3. Without cancellation, losing carriers keep consuming resources after their result is useless.
4. Fraud scoring is CPU-bound and benefits from multiple cores. Email sending is I/O-bound; throughput comes from overlapping waits.
5. Parallel work executes CPU instructions simultaneously across cores. Concurrent async work interleaves operations while I/O is pending.
6. Cancellation releases stock, refunds captured payment, and saves the order as `Pending` with `Cancelled`.

## Problems solved

- Lost JSON updates: protected the complete update and used atomic replacement.
- Keyboard oversell: made stock checking and subtraction one protected operation.
- Partial checkout: tracked stock/payment state and compensated in reverse order.
- Email instability: bounded retries and a durable retry file.
- Interleaved coloured logs: serialized each complete console write.

Before submitting, add the required console screenshots to `screenshots/`.
