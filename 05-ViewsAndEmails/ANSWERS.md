# Assignment 05 — Answers

Student name: Ahmed Ismail  
Group / ID: assignment/1-8

## Failures you observed

- Repeated original runs lose view increments because dictionary lookup and update are separate operations.
- The shared `Dictionary` and `Queue` are modified while other threads access them, which is unsupported and can cause incorrect counts or queue exceptions.

## What you changed

- I replaced the dictionary with `ConcurrentDictionary` and use `AddOrUpdate` for an atomic per-key increment.
- I replaced the queue with `ConcurrentQueue` and use `TryDequeue`, which combines the empty check and removal safely.
- The sent list remains protected by its lock. Fixed runs report 50,000 views and 20 unique/total emails.

## Collection safety vs business-rule safety

- A thread-safe collection protects its own individual operations. It cannot make a larger rule atomic. For example, `ContainsKey` followed by assignment still has a gap where another thread can change the same key. The business operation must use an atomic collection API or an external lock around all related steps.
