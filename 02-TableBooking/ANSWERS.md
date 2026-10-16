# Assignment 02 — Answers

Student name: Ahmed Ismail  
Group / ID: assignment/1-8

## What is wrong

- Checking `TablesLeft`, decrementing it, and incrementing `Booked` form one business operation, but the original code lets threads interleave those steps. Several customers can all pass the check while the value is still positive, producing negative availability and too many bookings.

## How you fixed it

- I protect the complete check-and-update operation with one private lock. This makes the invariant `Booked + TablesLeft == 5` hold throughout the run. Repeated fixed runs finish with `Booked=5, TablesLeft=0`.
