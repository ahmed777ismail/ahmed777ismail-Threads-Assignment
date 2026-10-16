# Assignment 04 — Answers

Student name: Ahmed Ismail  
Group / ID: assignment/1-8

## Why Expected and Actual disagree

- `Views++` is a read-modify-write operation, not one indivisible action. Threads can read the same old value and overwrite each other's increments, so updates are lost.

## What you changed

- I store the count privately and use `Interlocked.Increment`. Reads use `Volatile.Read`. Repeated runs now print `Expected: 400000` and `Actual: 400000`.

## When would you protect the update differently

- I would use `lock` when several related values or multiple operations must change as one invariant. For a single integer increment, `Interlocked` is simpler and avoids a blocking lock.
