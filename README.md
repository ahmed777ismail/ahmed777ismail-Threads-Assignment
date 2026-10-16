# Threads Assignment

Student: Ahmed Ismail
Assignment ID: assignment/1-8

The repository contains the five smaller concurrency exercises, the four-phase order-processing console app, and the LinkedIn submission file.

## Run

From `06-OrderProcessing/OrderProcessing`:

```text
dotnet run -- phase1
dotnet run -- phase2
dotnet run -- phase3
dotnet run -- phase4
dotnet run -- all
dotnet run -- phase3 --cancel-after 4000
```

With no argument, the app shows a numbered menu. Runtime JSON is created under `bin/Debug/net10.0/Data`; it is deliberately ignored by Git. Checked sample files belong under `06-OrderProcessing/sample-output`.
