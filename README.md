# Keel

A foundational project template designed to support the development of microservices using .NET.

---

## 📦 Keel.Infra.Db.Sql

O **Keel.Infra.Db.Sql** é uma biblioteca de infraestrutura unificada para acesso a banco de dados em `.NET 9.0`. Ela foi projetada para expor três abordagens distintas de persistência sob o mesmo contexto de conexão e transação compartilhados:

1. **ORM (Entity Framework Core)**
2. **Micro-ORM (Dapper)**
3. **ADO.NET Direto (Raw SQL / DataTables / DataRows)**

Esta biblioteca é totalmente desacoplada de dependências externas proprietárias, focando na performance assíncrona moderna (Task-based Asynchronous Pattern) e segurança no gerenciamento de transações.

> [!TIP]
> **Orientação para Agentes de IA**: Se você estiver utilizando assistentes de codificação de IA (como Gemini, Copilot ou Cursor) neste projeto, oriente-os a ler e seguir as diretrizes documentadas em **[AI_GUIDE.md](file:///Users/adrianosepe/x-src/evul/Keel/src/Keel.Infra.Db.Sql/AI_GUIDE.md)** para garantir boas práticas de concorrência e evitar vazamentos de recursos.

### 1. Registro e Configuração (IoC)

A biblioteca expõe o método de extensão `EnableDbLayer` sob o namespace `Microsoft.EntityFrameworkCore` para registrar o contexto de banco de dados e a camada de dados no contêiner de Injeção de Dependência (`IServiceCollection`).

#### Passo 1: Defina o seu `DbContext` e a sua Camada de Dados
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

#### Passo 2: Registrar no Startup (`Program.cs`)
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

### 2. Como Utilizar (Injeção de Dependência)

Injete a interface `IDbLayer` no seu serviço ou controlador para obter acesso unificado:

```csharp
using Keel.Infra.Db.Sql;

public class ProdutoService(IDbLayer db)
{
    // Acesso ao EF Core, Dapper e ADO.NET através de 'db'
}
```

---

### 3. Abordagens de Acesso a Dados

#### A. EF Core (ORM)
A propriedade `.Orm` retorna a instância tipada ou base do `DbContext`.

```csharp
var produtos = await db.Orm.Set<Produto>()
    .Where(p => p.Ativo)
    .ToListAsync(cancellationToken);
```

#### B. Dapper (Micro-ORM Assíncrono)
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
```

#### C. ADO.NET Direto (DbDirectAccess)
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

### 4. Gerenciamento Seguro de Transações

O `IDbUnitOfWork` permite criar transações unificadas que abrangem alterações feitas pelo EF Core, comandos Dapper e ADO.NET. A transação implementa `IAsyncDisposable`, prevenindo vazamento de conexões ou transações órfãs.

```csharp
using Keel.Infra.Db.Sql.Orm.Transaction;

public class ProcessarPedidoService(IDbLayer db, IDbUnitOfWork unitOfWork)
{
    public async Task ProcessarAsync(Pedido pedido, CancellationToken cancellationToken)
    {
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
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
```

---

## 📦 Publishing Packages

Each project within Keel has its own GitHub Actions workflow configured for creating and publishing NuGet packages to both **GitHub Packages** and **NuGet.org**.

### 🚀 How to Publish

You can trigger the publishing process in two ways:

1. **Manual Publish:** Trigger the workflow manually for the specific project via the `Actions` tab in GitHub.
2. **Publish by Tag:** Create and push a Git tag using the format `<PackageId>-v<version>`. 
   * ⚠️ *Important:* The `<version>` value must exactly match the `Version` property evaluated by MSBuild in the project file.
   * ⚠️ *Note:* Push a maximum of three tags at a time. GitHub does not generate tag push events if more than three tags are pushed simultaneously.

#### Tag Examples
- `Keel.Domain.CleanCode-v1.1.0`
- `Keel.Infra.Db-v1.1.0`
- `Keel.Infra.Db.Sql-v1.1.0`
- `Keel.Infra.Db.SqlServer-v1.1.0`
- `Keel.Infra.WebApi-v1.1.0`

### 🌐 Package Destinations

Upon successful execution of the workflow, packages are published to the following registries:
- **GitHub Packages:** `https://nuget.pkg.github.com/evul/index.json`
- **NuGet.org:** `https://www.nuget.org/packages`

### 🔑 Prerequisites for GitHub Actions

To enable publishing to NuGet.org, the following repository secret must be configured:
- `NUGET_API_KEY`: An API key generated on NuGet.org with permissions to push packages.
