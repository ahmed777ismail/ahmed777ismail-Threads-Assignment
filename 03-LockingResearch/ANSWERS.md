# Assignment 03 — Answers

Student name: Ahmed Ismail  
Group / ID: [S1-A8]

1. `Monitor` gives mutually exclusive access to a critical section. It also provides waiting and signalling operations for threads that coordinate around shared state.

2. For a normal reference object, `lock` is compiled to a `try/finally` around `Monitor.Enter` and `Monitor.Exit`, so the monitor is released even when the body throws.

3. `lock` is shorter and automatically releases the monitor. Direct `Monitor` calls expose timed/non-blocking acquisition (`TryEnter`) and condition signalling (`Wait`, `Pulse`, `PulseAll`), but require careful `try/finally` handling.

4. `TryEnter` is useful when a thread must not wait forever. `Wait` and `Pulse` are useful for a condition such as an empty/full producer-consumer buffer, where a thread releases the lock while waiting and is signalled after state changes.

5. Equivalent critical sections:

```csharp
lock (_sync)
{
    _balance += amount;
}

bool lockTaken = false;
try
{
    Monitor.Enter(_sync, ref lockTaken);
    _balance += amount;
}
finally
{
    if (lockTaken) Monitor.Exit(_sync);
}
```

Links:

- https://learn.microsoft.com/dotnet/api/system.threading.monitor
- https://learn.microsoft.com/dotnet/csharp/language-reference/statements/lock
