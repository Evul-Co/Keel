# AI Coding Guide: Keel.Infra.Db.Sql

This file serves as a system prompt / instruction manual for AI coding assistants (like Gemini, Copilot, or Cursor) working with the **Keel.Infra.Db.Sql** library.

---

## 1. Core Architecture Reference

`Keel.Infra.Db.Sql` provides a unified entry point for database access: **`IDbLayer`**.
It exposes three database access patterns sharing the exact same connection and transaction context:
* **`Orm`**: Entity Framework Core (`DbContext`).
* **`Dapper`**: Micro-ORM for high-performance raw query mapping.
* **`Ado`**: Direct ADO.NET abstraction returning `DataTable`, `DataSet`, `DataRow`, or cursor readers.

---

## 2. Strict Guidelines for AI Coding

When writing code that uses or modifies this library, you **MUST** follow these rules:

### A. Async Execution (Task Pattern)
* **Never use sync-over-async blocking**: Do not call `.GetAwaiter().GetResult()` or `.Result` on tasks returned by `IDbSharedContextProvider`.
* **Use async APIs for database E/S**: Always prefer `ReadAsync`, `ReadAsync<T>`, and `CreateCommandAsync` over their synchronous counterparts.
* **Use Dapper Async**: When using `.Dapper`, use the async versions: `QueryFirstOrDefaultAsync<T>`, `QueryAsync<T>`, etc. Pass the `CancellationToken` via `CommandDefinition`.

### B. Safe Transaction Management
* **Always wrap transactions in `await using`**: The returned `IDbWrappedTransaction` implements `IAsyncDisposable`. You must use `await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);` to prevent transaction leaks.
* **Avoid manual rollback in happy path**: Let the `await using` block dispose and rollback the transaction automatically if an exception occurs before `transaction.CommitAsync()` is called.

### C. Scalar Casting
* **Do not cast `Task` directly**: `ExecuteScalarAsync` returns a `Task<object?>`. Always `await` it before casting the result.
* **Use `Convert.ChangeType` for primitives**: Use the helper `CastScalar<T>` or `Convert.ChangeType` to support numeric conversions (e.g., `decimal` to `double`, `long` to `int`) and nullable types.

### D. Dependency Injection
* Register the library using:
  ```csharp
  services.EnableDbLayer<TDbContext, TDbLayer>();
  ```
  This extension is under the `Microsoft.EntityFrameworkCore` namespace. Do not use legacy `AddDbLayer` or manual scoped registrations.

---

## 3. Code Examples for AI Reference

### DI Registration
```csharp
using Microsoft.EntityFrameworkCore;

builder.Services.EnableDbLayer<MeuDbContext, MeuDbLayer>();
```

### Performing Transactions Across EF, Dapper, and ADO
```csharp
public async Task ExecutarOperacaoAsync(IDbLayer db, IDbUnitOfWork unitOfWork, CancellationToken cancellationToken)
{
    await using var tx = await unitOfWork.BeginTransactionAsync(cancellationToken);
    
    // 1. EF Core
    db.Orm.Set<Usuario>().Add(new Usuario { Nome = "João" });
    await db.Orm.SaveChangesAsync(cancellationToken);
    
    // 2. Dapper (shares the transaction automatically)
    await db.Dapper.ReadOneAsync<int>(
        "UPDATE Estatisticas SET Total = Total + 1", 
        null, 
        cancellationToken);
        
    await tx.CommitAsync(cancellationToken);
}
```
