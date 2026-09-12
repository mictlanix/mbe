# Mictlanix.BE.Model.Tests

Unit tests for the `Mictlanix.BE.Model` entities. Plain NUnit 3 over POCOs — no
database, no ActiveRecord session.

## Running

```sh
nuget restore Model.Tests/packages.config -PackagesDirectory packages
nuget install NUnit.ConsoleRunner -Version 3.16.3 -OutputDirectory packages
msbuild Model.Tests/Mictlanix.BE.Model.Tests.csproj /p:Configuration=Debug
mono packages/NUnit.ConsoleRunner.3.16.3/tools/nunit3-console.exe \
    Model.Tests/bin/Debug/Mictlanix.BE.Model.Tests.dll
```

## Scope

Only entity members that are pure CLR arithmetic over in-memory collections are
testable this way. Anything reaching `ActiveRecord.Queryable` needs a session and
is out of scope — notably `SalesOrder.Balance`, which queries `CreditNote` and
`CustomerRefund`.
