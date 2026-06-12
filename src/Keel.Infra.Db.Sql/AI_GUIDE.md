# Guia de Codificação para IA: Keel.Infra.Db.Sql

Este arquivo serve como instrução de sistema / manual de orientação para assistentes de codificação baseados em IA (como Gemini, Copilot, Cursor) ao trabalhar com a biblioteca **Keel.Infra.Db.Sql**.

---

## 1. Referência da Arquitetura Central

A biblioteca unifica o acesso a dados por meio da interface **`IDbLayer`**, que expõe três propriedades compartilhando a mesma conexão e transação do banco:
* **`Orm`**: Entity Framework Core (`DbContext`).
* **`Dapper`**: Micro-ORM para mapeamento rápido de objetos.
* **`Ado`**: Abstração direta do ADO.NET (`DataTable`, `DataSet`, cursor readers).

---

## 2. Diretrizes Estritas para Agentes de IA

Ao gerar ou modificar códigos usando esta biblioteca, siga obrigatoriamente as seguintes regras:

### A. Programação Assíncrona (Task-based Asynchronous Pattern)
* **Nunca utilize bloqueios síncronos (Sync-over-Async)**: Evite chamadas a `.GetAwaiter().GetResult()` ou `.Result` em tarefas retornadas.
* **Prefira APIs Assíncronas**: Use `ReadAsync`, `ReadAsync<T>` e `CreateCommandAsync` em vez de suas versões síncronas.
* **Dapper Assíncrono**: Sempre utilize `QueryFirstOrDefaultAsync<T>`, `QueryAsync<T>` e passe o `CancellationToken` via `CommandDefinition`.

### B. Ciclo de Vida e Transações
* **Sempre envolva transações em blocos `await using`**: O retorno de `BeginTransactionAsync` implementa `IAsyncDisposable`. Falhar ao descartar a transação causará vazamento de conexões e transações ativas no banco de dados.

### C. Conversão de Escalares (Cast)
* **Nunca faça cast direto no resultado de `ExecuteScalarAsync`**: Ele retorna `Task<object?>`. Faça `await` do método antes de tentar converter ou unboxar o valor.
* **Use conversão robusta para tipos primitivos**: Utilize a lógica do método `CastScalar<T>` com `Convert.ChangeType` para tratar conversões numéricas do banco de dados (ex: `decimal` -> `double`) e tipos anuláveis (`Nullable<T>`).

---

## 3. Exemplos Avançados de Utilização

### Exemplo 1: Registro de Camada (IoC)
```csharp
using Microsoft.EntityFrameworkCore;

// Registra MeuDbContext, MeuDbLayer e IDbUnitOfWork no contêiner DI
builder.Services.EnableDbLayer<MeuDbContext, MeuDbLayer>();
```

### Exemplo 2: Transações Compartilhadas (EF Core + Dapper)
```csharp
public async Task SalvarPedidoAsync(IDbLayer db, IDbUnitOfWork unitOfWork, Pedido pedido, CancellationToken cancellationToken)
{
    // O descarte e rollback automático ocorrem no fim do bloco se não houver Commit
    await using var tx = await unitOfWork.BeginTransactionAsync(cancellationToken);

    try
    {
        // 1. EF Core (Gravação de registro principal)
        db.Orm.Set<Pedido>().Add(pedido);
        await db.Orm.SaveChangesAsync(cancellationToken);

        // 2. Dapper (Execução de query de contagem usando a mesma transação)
        await db.Dapper.ReadOneAsync<int>(
            "UPDATE Clientes SET Limite = Limite - @Total WHERE Id = @ClienteId",
            new { Total = pedido.ValorTotal, ClienteId = pedido.ClienteId },
            cancellationToken);

        await tx.CommitAsync(cancellationToken);
    }
    catch
    {
        await tx.RollbackAsync(cancellationToken);
        throw;
    }
}
```

### Exemplo 3: Consulta ADO.NET Direta com DataTable e Parâmetros
```csharp
public async Task<DataTable> ObterRelatorioVendasAsync(IDbLayer db, DateTime inicio, CancellationToken cancellationToken)
{
    var parametro = db.Ado.CreateParameter("Inicio", DbType.DateTime, inicio);
    
    return await db.Ado.DataTableAsync(
        "SELECT * FROM Faturamento WHERE DataFaturamento >= @Inicio",
        CommandType.Text,
        cancellationToken,
        parametro);
}
```

### Exemplo 4: Leitura Eficiente com Streaming Assíncrono (`IAsyncEnumerable<T>`)
```csharp
public async Task ProcessarLoginsAsync(IDbLayer db, CancellationToken cancellationToken)
{
    // Executa leitura em modo streaming (cursor) sem carregar toda a lista na memória
    var loginsStream = db.Ado.ReadAsync<string>(
        "SELECT Username FROM LogsAcesso",
        CommandType.Text,
        reader => reader.GetString(0),
        cancellationToken);

    await foreach (var username in loginsStream.WithCancellation(cancellationToken))
    {
        Console.WriteLine($"Processando: {username}");
    }
}
```

### Exemplo 5: Mapeamento Pai-Filho no Dapper (`QueryParentChildAsync`)
```csharp
public async Task<IEnumerable<Pedido>> ObterPedidosComItensAsync(IDbLayer db, CancellationToken cancellationToken)
{
    var connection = await db.GetConnectionAsync(cancellationToken);
    
    return await connection.QueryParentChildAsync<Pedido, ItemPedido, int>(
        "SELECT p.Id, p.Data, i.Id as ID, i.Nome, i.Preco FROM Pedidos p INNER JOIN ItensPedido i ON p.Id = i.PedidoId",
        parent => parent.Id,
        parent => parent.Itens, // Deve retornar IList<ItemPedido>
        splitOn: "ID");
}
```
