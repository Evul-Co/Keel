# Keel.Infra.Db.Sql

O **Keel.Infra.Db.Sql** é uma biblioteca de infraestrutura unificada para acesso a banco de dados em `.NET 9.0`. Ela foi projetada para expor três abordagens distintas de persistência sob o mesmo contexto de conexão e transação compartilhados:

1. **ORM (Entity Framework Core)**
2. **Micro-ORM (Dapper)**
3. **ADO.NET Direto (Raw SQL / DataTables / DataRows)**

Esta biblioteca é totalmente desacoplada de dependências externas proprietárias, focando na performance assíncrona moderna (Task-based Asynchronous Pattern) e segurança no gerenciamento de transações.

---

## 1. Registro e Configuração (IoC)

A biblioteca expõe o método de extensão `EnableDbLayer` sob o namespace `Microsoft.EntityFrameworkCore` para registrar o contexto de banco de dados e a camada de dados no contêiner de Injeção de Dependência (`IServiceCollection`).

### Passo 1: Defina o seu `DbContext` e a sua Camada de Dados
Crie um contexto que herde de `BaseDbContext` e uma camada que herde de `DbLayer<TDbContext>`:

```csharp
using Keel.Infra.Db.Sql;
using Keel.Infra.Db.Sql.Access;
using Keel.Infra.Db.Sql.Orm;
using Microsoft.EntityFrameworkCore;

namespace MeuApp.Infra;

// 1. Defina o DbContext herdando de BaseDbContext
public class MeuDbContext(DbContextOptions<MeuDbContext> options) : BaseDbContext(options)
{
    // DbSet definitions...
}

// 2. Defina o DbLayer correspondente
public class MeuDbLayer(MeuDbContext context) : DbLayer<MeuDbContext>(context)
{
    // Implemente a criação do ADO.NET direto para o seu provedor (ex: SQL Server)
    protected override DbDirectAccess InternalCreateDirectAccess()
    {
        return new MeuSqlDirectAccess(this);
    }
}
```

### Passo 2: Registrar no Startup (`Program.cs`)
Use o método de extensão `EnableDbLayer` para registrar as instâncias:

```csharp
using Microsoft.EntityFrameworkCore; // Namespace onde reside a extensão
using MeuApp.Infra;

var builder = WebApplication.CreateBuilder(args);

// Adiciona o DbContext ao contêiner
builder.Services.AddDbContext<MeuDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Habilita o Keel DbLayer de forma injetável
builder.Services.EnableDbLayer<MeuDbContext, MeuDbLayer>();
```

O método `EnableDbLayer` efetuará o registro de três interfaces no escopo:
* `IDbLayer` (resolvido a partir de `IDbLayer<MeuDbContext>`)
* `IDbLayer<MeuDbContext>`
* `IDbUnitOfWork` (resolvido a partir de `MeuDbContext`)

---

## 2. Como Utilizar (Injeção de Dependência)

Injete a interface `IDbLayer` no seu serviço ou controlador para obter acesso unificado:

```csharp
using Keel.Infra.Db.Sql;

public class ProdutoService(IDbLayer db)
{
    // Acesso ao EF Core, Dapper e ADO.NET através de 'db'
}
```

---

## 3. Abordagens de Acesso a Dados

### A. EF Core (ORM)
A propriedade `.Orm` retorna a instância tipada ou base do `DbContext`.

```csharp
var produtos = await db.Orm.Set<Produto>()
    .Where(p => p.Ativo)
    .ToListAsync(cancellationToken);
```

---

### B. Dapper (Micro-ORM Assíncrono)
A propriedade `.Dapper` expõe métodos de consulta de alto desempenho parametrizados e assíncronos que rodam sob a mesma conexão e transação do EF Core:

```csharp
// Ler único registro
var produto = await db.Dapper.ReadOneAsync<Produto>(
    "SELECT * FROM Produtos WHERE Id = @Id",
    new { Id = 1 },
    cancellationToken);

// Ler múltiplos registros
var produtos = await db.Dapper.ReadAsync<Produto>(
    "SELECT * FROM Produtos WHERE Ativo = @Ativo",
    new { Ativo = true },
    cancellationToken);

// Consulta usando builder fluente
var ativos = await db.Dapper.QueryAsync<Produto>(builder => builder
    .ForText("SELECT * FROM Produtos WHERE CategoriaId = @CatId")
    .AddInt("CatId", 5)
    .WithTimeout(30),
    cancellationToken);
```

---

### C. ADO.NET Direto (DbDirectAccess)
A propriedade `.Ado` permite executar consultas que necessitem de estruturas tradicionais do ADO.NET (`DataTable`, `DataSet`, `DataRow`) ou leitura via cursor (`DbDataReader`), contendo contrapartes síncronas e assíncronas:

```csharp
// Obter DataTable assíncronamente
DataTable tabela = await db.Ado.DataTableAsync(
    "SELECT * FROM Vendas WHERE Data >= @Inicio",
    CommandType.Text,
    cancellationToken,
    db.Ado.CreateParameter("Inicio", DbType.DateTime, dataInicio));

// Ler via cursor com streaming assíncrono (IAsyncEnumerable)
await foreach (var item in db.Ado.ReadAsync(
    "SELECT Codigo FROM Itens",
    CommandType.Text,
    reader => reader.GetString(0),
    cancellationToken))
{
    Console.WriteLine(item);
}
```

---

## 4. Gerenciamento Seguro de Transações

O `IDbUnitOfWork` permite criar transações unificadas que abrangem alterações feitas pelo EF Core, comandos Dapper e ADO.NET. A transação implementa `IAsyncDisposable`, prevenindo vazamento de conexões ou transações órfãs.

```csharp
using Keel.Infra.Db.Sql.Orm.Transaction;

public class ProcessarPedidoService(IDbLayer db, IDbUnitOfWork unitOfWork)
{
    public async Task ProcessarAsync(Pedido pedido, CancellationToken cancellationToken)
    {
        // Criação segura com bloco 'await using'
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            // 1. Inserir via EF Core (ORM)
            db.Orm.Set<Pedido>().Add(pedido);
            await db.Orm.SaveChangesAsync(cancellationToken);

            // 2. Executar comando via Dapper (mesma transação)
            await db.Dapper.ReadOneAsync<int>(
                "UPDATE Estoque SET Qtd = Qtd - 1 WHERE ProdutoId = @Id",
                new { Id = pedido.ProdutoId },
                cancellationToken);

            // 3. Confirmar a transação
            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception)
        {
            // Reverte automaticamente em caso de falha antes de ser descartado no 'using'
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
```
