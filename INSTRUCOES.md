# Sistema de Vendas de Doces - Instruções de Configuração

## 📋 Pré-requisitos

1. **.NET SDK 8.0** - Baixe em: https://dotnet.microsoft.com/download
2. **MySQL Server** - Já instalado (você tem o database_script.sql)
3. **Visual Studio Code** ou **Visual Studio 2022**

## 🚀 Passos para Configurar o Projeto

### 1. Restaurar os Pacotes NuGet

Abra o terminal na pasta do projeto e execute:

```bash
dotnet restore
```

Isso vai baixar todas as dependências necessárias (Entity Framework Core, MySQL, etc.)

### 2. Configurar a String de Conexão

Abra o arquivo `appsettings.json` e altere a senha do MySQL:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=vendas_doces;User=root;Password=SUA_SENHA_AQUI;"
}
```

### 3. Criar o Banco de Dados

Você tem duas opções:

**Opção A: Usar o script SQL que você já tem**
- Execute o arquivo `database_script.sql` no MySQL Workbench

**Opção B: Usar Migrations do Entity Framework (Recomendado para aprender)**
```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

> **Nota:** Se o comando `dotnet ef` não funcionar, instale a ferramenta:
> ```bash
> dotnet tool install --global dotnet-ef
> ```

### 4. Executar o Projeto

```bash
dotnet run
```

Ou pressione **F5** no Visual Studio/VS Code

O projeto vai abrir em: `https://localhost:5001` ou `http://localhost:5000`

## 📁 Estrutura do Projeto Criada

```
Projeto Pri/
├── Controllers/          # Controladores MVC (lógica das páginas)
│   ├── HomeController.cs
│   ├── ProdutosController.cs
│   └── ClientesController.cs
├── Models/              # Modelos de dados (classes que representam tabelas)
│   ├── Produto.cs
│   ├── Cliente.cs
│   ├── Venda.cs
│   └── ItemVenda.cs
├── Data/                # Contexto do banco de dados
│   └── ApplicationDbContext.cs
├── Views/               # VOCÊ PRECISARÁ CRIAR (próximo passo)
├── wwwroot/             # Arquivos estáticos (CSS, JS, imagens)
├── Program.cs           # Arquivo principal da aplicação
├── appsettings.json     # Configurações (connection string)
└── SistemaVendasDoces.csproj  # Arquivo do projeto
```

## 🎯 Próximos Passos (Para Você Aprender)

### 1. Criar as Views (Páginas HTML)

Você precisará criar as pastas e arquivos de Views:

```
Views/
├── Home/
│   └── Index.cshtml
├── Produtos/
│   ├── Index.cshtml
│   ├── Create.cshtml
│   ├── Edit.cshtml
│   ├── Details.cshtml
│   └── Delete.cshtml
├── Clientes/
│   ├── Index.cshtml
│   ├── Create.cshtml
│   ├── Edit.cshtml
│   ├── Details.cshtml
│   └── Delete.cshtml
└── Shared/
    ├── _Layout.cshtml
    └── _ViewImports.cshtml
```

### 2. Estudar os Conceitos

- **Models:** Representam as tabelas do banco (Produto, Cliente, etc.)
- **Controllers:** Controlam a lógica (buscar dados, salvar, etc.)
- **Views:** As páginas HTML que o usuário vê
- **Entity Framework:** Ferramenta que conecta C# com o banco de dados

### 3. Comandos Úteis

```bash
# Ver se o projeto compila
dotnet build

# Executar o projeto
dotnet run

# Criar uma nova migration (quando alterar os Models)
dotnet ef migrations add NomeDaMigracao

# Aplicar migrations no banco
dotnet ef database update

# Ver as rotas disponíveis
dotnet run --urls="http://localhost:5000"
```

## 📚 Recursos para Aprender

1. **Documentação Oficial Microsoft:**
   - https://learn.microsoft.com/pt-br/aspnet/core/

2. **Tutorial MVC:**
   - https://learn.microsoft.com/pt-br/aspnet/core/tutorials/first-mvc-app/

3. **Entity Framework Core:**
   - https://learn.microsoft.com/pt-br/ef/core/

## ⚠️ Problemas Comuns

### Erro: "Unable to connect to database"
- Verifique se o MySQL está rodando
- Confira a senha no `appsettings.json`

### Erro: "dotnet ef command not found"
```bash
dotnet tool install --global dotnet-ef
```

### Erro ao compilar
```bash
dotnet clean
dotnet restore
dotnet build
```

## 💡 Dicas de Aprendizado

1. **Comece simples:** Primeiro faça funcionar, depois melhore
2. **Teste cada parte:** Teste Produtos primeiro, depois Clientes, depois Vendas
3. **Use o Debugger:** Coloque breakpoints (F9) para ver o código executando
4. **Leia os erros:** As mensagens de erro geralmente dizem o que está errado
5. **Pergunte:** Quando travar, me chame que eu te ajudo!

---

**Criado para ajudar você a aprender ASP.NET Core passo a passo! 🚀**
