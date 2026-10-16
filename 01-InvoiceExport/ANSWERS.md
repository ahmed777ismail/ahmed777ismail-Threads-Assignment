# Assignment 01 — Answers

Student name: Ahmed Ismail  
Group / ID: assignment/1-8

## Problems with the current design

- The original version blocks the current thread during every database, file read, and file write delay.
- It processes five independent orders one at a time, so their waiting time is added together.
- With many orders, this wastes threads and makes total export time grow almost linearly.

## What you changed

- I changed the I/O-shaped methods to return `Task`/`Task<T>` and replaced `Thread.Sleep` with `Task.Delay`.
- I start one export task for every pending order, then await those already-started tasks one by one as required.
- File APIs use `ReadAllTextAsync` and `WriteAllTextAsync`. The invoice text and five output files are unchanged.

Before: the five invoices were written sequentially and the run took about 5.8 seconds.  
After: all five invoices are still produced; a typical run takes about 1.8 seconds.
