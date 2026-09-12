# Mictlanix.BE.Web.Tests

Integration tests for the MVC controllers. These talk to a **real database** —
there is no seam between the controllers and Castle ActiveRecord to stub.

## Pointing them at a database

The fixture reads `Web/ActiveRecord.config` (gitignored, so no credentials live
in the repository). Override with an environment variable:

```sh
export MBE_TEST_CONNECTION="Server=/tmp/mysql.sock;Protocol=unix;Database=mbe_test;User Id=developer;Password=;Allow Zero Datetime=True"
```

## Running

```sh
nuget restore Mictlanix.BE.sln -PackagesDirectory packages
msbuild Web.Tests/Mictlanix.BE.Web.Tests.csproj /p:Configuration=Debug
mono packages/NUnit.ConsoleRunner.3.16.3/tools/nunit3-console.exe \
    Web.Tests/bin/Debug/Mictlanix.BE.Web.Tests.dll
```

## Isolation — read this before adding a test

Castle ActiveRecord's `TransactionScope` defaults to `TransactionMode.New`. A
scope opened inside a controller action does **not** join one opened by the test;
it commits on its own. Wrapping a test in a transaction and rolling it back
therefore isolates nothing — this was verified, not assumed.

So every row a test creates has to be deleted by id. `Seed` tracks what it
created and removes it in `Dispose`, children before parents. A test that writes
outside `Seed` must clean up after itself the same way.

Tests only ever create new rows. Stores, customers, employees and points of sale
are read from whatever the target database already holds, and are never modified.
Seeded rows carry the marker `MBE-TEST` so orphans from a crashed run can be found:

```sql
SELECT * FROM sales_order WHERE comment = 'MBE-TEST';
SELECT * FROM customer_payment WHERE reference = 'MBE-TEST';
```
