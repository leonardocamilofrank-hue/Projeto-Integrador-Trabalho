# MesaViva 🍞

**Trabalho integrador — Banco de Dados, Análise de Requisitos e Programação Orientada a Objetos (C#)**

**Integrantes:** Welinton e Leonardo  
**Tema:** Combate ao desperdício de alimentos

## 1. Sobre o projeto

O **MesaViva** é um sistema de console desenvolvido em **C#**, com banco de dados **MySQL** acessado pelo **Entity Framework Core (EF Core)**, que conecta estabelecimentos com alimentos excedentes a instituições interessadas em recebê-los.

Os estabelecimentos cadastram ofertas com a quantidade disponível, a validade e os horários de retirada. As instituições consultam essas ofertas, reservam alimentos e confirmam as coletas. O sistema controla as quantidades para evitar reservas acima do disponível.

### Problema

Alimentos que poderiam ser aproveitados acabam desperdiçados, enquanto instituições precisam de doações para atender pessoas em situação de necessidade.

### Objetivo

Organizar a divulgação, a reserva e a retirada de alimentos excedentes de maneira simples, diminuindo desperdícios.

### Público-alvo

- **Estabelecimentos:** padarias, mercados, restaurantes e outros locais que possuem excedentes.
- **Instituições:** ONGs, abrigos e projetos sociais que recebem alimentos.

## 2. Funcionamento do sistema

Na tela inicial, o usuário poderá **fazer login**, **criar uma conta** ou **sair**. Durante o cadastro, ele escolhe entre os perfis **Estabelecimento** e **Instituição**.

Depois do login, aparece o menu correspondente ao tipo de conta. **Não haverá perfil de administrador** nesta versão.

### Menu inicial

```text
========== MESAVIVA ==========
1 - Fazer login
2 - Criar conta
0 - Sair
```

### Menu do estabelecimento

```text
1 - Cadastrar oferta
2 - Consultar minhas ofertas
3 - Atualizar oferta
4 - Excluir oferta
5 - Consultar reservas recebidas
6 - Editar meus dados
0 - Sair da conta
```

### Menu da instituição

```text
1 - Consultar ofertas disponíveis
2 - Reservar alimentos
3 - Consultar minhas reservas
4 - Cancelar reserva
5 - Confirmar coleta
6 - Consultar ofertas urgentes
7 - Editar meus dados
0 - Sair da conta
```

### Exemplo prático

Uma padaria cadastra **12 kg de pães**, com retirada das **16h às 18h**. Uma instituição reserva **8 kg** e outra reserva **4 kg**. Como toda a quantidade foi reservada, o sistema impede novas reservas dessa oferta. Após a retirada, cada instituição confirma sua coleta.

## 3. Requisitos funcionais (RF)

| Código | Descrição |
|---|---|
| RF01 | Permitir cadastro de usuários como estabelecimento ou instituição. |
| RF02 | Permitir login por e-mail e senha. |
| RF03 | Exibir um menu específico para cada tipo de conta. |
| RF04 | Permitir que o usuário consulte e atualize seus dados cadastrais. |
| RF05 | Permitir que estabelecimentos cadastrem ofertas de alimentos. |
| RF06 | Permitir que estabelecimentos consultem suas ofertas. |
| RF07 | Permitir que estabelecimentos atualizem ou excluam suas ofertas, respeitando as regras de negócio. |
| RF08 | Permitir que estabelecimentos consultem as reservas de suas ofertas. |
| RF09 | Permitir que instituições consultem e busquem ofertas disponíveis. |
| RF10 | Permitir que instituições reservem uma quantidade de alimentos. |
| RF11 | Permitir que instituições consultem suas reservas. |
| RF12 | Permitir que instituições cancelem reservas ainda não coletadas. |
| RF13 | Permitir que instituições confirmem a coleta de reservas ativas. |
| RF14 | Exibir ofertas urgentes, próximas ao fim do horário de retirada. |

## 4. Requisitos não funcionais (RNF)

| Código | Descrição |
|---|---|
| RNF01 | O sistema será desenvolvido em C# com .NET. |
| RNF02 | Os dados serão persistidos em um banco MySQL usando Entity Framework Core (EF Core). |
| RNF03 | A interface será de console, com menus interativos. |
| RNF04 | O código aplicará conceitos de POO, como classes, métodos e encapsulamento. |
| RNF05 | O projeto terá separação entre modelos, regras de negócio, DbContext/acesso a dados e menus. |
| RNF06 | As entradas serão validadas e erros serão apresentados com mensagens compreensíveis. |
| RNF07 | Senhas serão armazenadas usando hash seguro, nunca em texto puro. |
| RNF08 | A estrutura do banco será criada e atualizada a partir das classes C# com Code First e Migrations. |
| RNF09 | As consultas ao banco utilizarão LINQ e, quando apropriado, métodos assíncronos com async/await. |

## 5. Regras de negócio (RN)

| Código | Regra |
|---|---|
| RN01 | Apenas usuários autenticados poderão utilizar os menus internos. |
| RN02 | Cada e-mail só poderá estar associado a uma conta. |
| RN03 | Apenas estabelecimentos poderão publicar e gerenciar ofertas. |
| RN04 | Apenas instituições poderão realizar, cancelar e confirmar suas próprias reservas. |
| RN05 | Cada estabelecimento poderá alterar ou excluir somente suas próprias ofertas. |
| RN06 | A quantidade de uma reserva deve ser maior que zero e não pode ultrapassar a quantidade disponível. |
| RN07 | Não será permitido reservar alimentos vencidos ou após o fim do prazo de retirada. |
| RN08 | Cancelar uma reserva ativa libera novamente a quantidade reservada. |
| RN09 | Uma reserva só poderá ter a coleta confirmada uma vez. Reservas canceladas ou já coletadas não poderão ser confirmadas novamente. |
| RN10 | Ofertas com reservas registradas não poderão ser excluídas, para preservar o histórico. |
| RN11 | Uma oferta será urgente quando faltar até 2 horas para encerrar a retirada e ainda houver quantidade disponível. |
| RN12 | Para simplificar, as quantidades de alimentos serão cadastradas em quilogramas (kg). |

**Cálculo de disponibilidade:** quantidade total da oferta menos a soma das reservas com status `RESERVADA` ou `COLETADA`. Reservas `CANCELADA` não ocupam quantidade. Assim, o banco não precisa guardar um campo de disponibilidade que pode ficar desatualizado.

## 6. Diagramas

Os diagramas abaixo usam **Mermaid**, que pode ser renderizado diretamente no README do GitHub.

### 6.1. Diagrama de casos de uso (representação simplificada)

```mermaid
flowchart LR
    E["👤 Estabelecimento"]
    I["👤 Instituição"]

    subgraph Sistema["Sistema MesaViva"]
        UC1(["Criar conta"])
        UC2(["Fazer login"])
        UC3(["Editar meus dados"])
        UC4(["Cadastrar oferta"])
        UC5(["Consultar minhas ofertas"])
        UC6(["Atualizar oferta"])
        UC7(["Excluir oferta"])
        UC8(["Consultar reservas recebidas"])
        UC9(["Consultar ofertas disponíveis"])
        UC10(["Reservar alimentos"])
        UC11(["Consultar minhas reservas"])
        UC12(["Cancelar reserva"])
        UC13(["Confirmar coleta"])
        UC14(["Consultar ofertas urgentes"])
    end

    E --- UC1
    E --- UC2
    E --- UC3
    E --- UC4
    E --- UC5
    E --- UC6
    E --- UC7
    E --- UC8

    I --- UC1
    I --- UC2
    I --- UC3
    I --- UC9
    I --- UC10
    I --- UC11
    I --- UC12
    I --- UC13
    I --- UC14
```

### 6.2. Diagrama entidade-relacionamento (DER)

Este é o **modelo previsto** do MySQL; as tabelas serão criadas a partir das entidades C# com **EF Core Code First e Migrations**, e não por um script SQL manual.

```mermaid
erDiagram
    USUARIOS ||--o{ OFERTAS : publica
    USUARIOS ||--o{ RESERVAS : realiza
    OFERTAS ||--o{ RESERVAS : possui

    USUARIOS {
        int id PK
        varchar nome
        varchar email UK
        varchar senha_hash
        varchar telefone
        varchar endereco
        varchar tipo
    }

    OFERTAS {
        int id PK
        int estabelecimento_id FK
        varchar alimento
        decimal quantidade_total
        datetime validade
        datetime inicio_retirada
        datetime fim_retirada
    }

    RESERVAS {
        int id PK
        int oferta_id FK
        int instituicao_id FK
        decimal quantidade
        varchar status
        datetime data_reserva
        datetime data_coleta
    }
```

**Relacionamentos:** um estabelecimento pode publicar várias ofertas; uma instituição pode realizar várias reservas; e uma oferta pode possuir várias reservas. Os campos `estabelecimento_id` e `instituicao_id` apontam para `USUARIOS`, e o tipo de cada conta será validado pelo sistema. No EF Core, isso será representado com **chaves estrangeiras e propriedades de navegação**.

### 6.3. Diagrama de classes (proposta inicial)

```mermaid
classDiagram
    direction TB

    class Usuario {
        -int Id
        -string Nome
        -string Email
        -string SenhaHash
        -string Telefone
        -string Endereco
        +AtualizarDados()
    }

    class Estabelecimento {
    }

    class Instituicao {
    }

    class Oferta {
        -int Id
        -int EstabelecimentoId
        -string Alimento
        -decimal QuantidadeTotal
        -DateTime Validade
        -DateTime InicioRetirada
        -DateTime FimRetirada
        +CalcularQuantidadeDisponivel()
    }

    class Reserva {
        -int Id
        -int OfertaId
        -int InstituicaoId
        -decimal Quantidade
        -string Status
        +Cancelar()
        +ConfirmarColeta()
    }

    class AppDbContext {
        +DbSet~Usuario~ Usuarios
        +DbSet~Oferta~ Ofertas
        +DbSet~Reserva~ Reservas
    }

    class AuthService {
        +CadastrarUsuario()
        +FazerLogin()
    }

    class OfertaService {
        +CadastrarOferta()
        +ListarOfertas()
        +AtualizarOferta()
        +ExcluirOferta()
        +ListarOfertasUrgentes()
    }

    class ReservaService {
        +CriarReserva()
        +CancelarReserva()
        +ConfirmarColeta()
        +ValidarDisponibilidade()
    }

    Usuario <|-- Estabelecimento
    Usuario <|-- Instituicao
    Estabelecimento "1" --> "0..*" Oferta : publica
    Instituicao "1" --> "0..*" Reserva : realiza
    Oferta "1" --> "0..*" Reserva : recebe
    AppDbContext ..> Usuario
    AppDbContext ..> Oferta
    AppDbContext ..> Reserva
    AuthService ..> Usuario
    OfertaService ..> Oferta
    ReservaService ..> Reserva
    ReservaService ..> Oferta
```

*Os métodos e as classes de serviço poderão ser ajustados quando começarmos a implementação. Se mantivermos a herança de `Usuario`, o EF Core poderá mapear os dois perfis na mesma tabela `USUARIOS` (estratégia TPH, com discriminador de tipo). Como alternativa mais simples, pode-se usar uma única classe `Usuario` com um campo `Tipo`; a decisão será fechada antes de gerar a primeira Migration.*

### 6.4. Fluxo de login e menus

```mermaid
flowchart TD
    A([Início]) --> B{Menu inicial}
    B -->|Criar conta| C[Escolher tipo de conta]
    C --> D[Cadastrar dados e senha]
    D --> B
    B -->|Fazer login| E[Informar e-mail e senha]
    E --> F{Credenciais válidas?}
    F -->|Não| E
    F -->|Sim| G{Tipo de conta}
    G -->|Estabelecimento| H[Menu de ofertas e reservas recebidas]
    G -->|Instituição| I[Menu de ofertas e reservas]
    H --> J[Sair da conta]
    I --> J
    J --> B
    B -->|Sair| K([Fim])
```

## 7. Estrutura sugerida do código

```text
MesaViva/
├── Models/
│   ├── Usuario.cs
│   ├── Estabelecimento.cs
│   ├── Instituicao.cs
│   ├── Oferta.cs
│   └── Reserva.cs
├── Services/
│   ├── AuthService.cs
│   ├── OfertaService.cs
│   └── ReservaService.cs
├── Data/
│   └── AppDbContext.cs
├── Migrations/            # Gerada automaticamente pelo EF Core
├── UI/
│   ├── MenuInicial.cs
│   ├── MenuEstabelecimento.cs
│   └── MenuInstituicao.cs
├── Program.cs
├── MesaViva.csproj
└── README.md
```

**Mudança em relação ao planejamento anterior:** não precisaremos da `ConnectionFactory.cs`, de comandos `MySqlCommand` nem de um arquivo `mesaviva.sql` para criar as tabelas. O **AppDbContext** e o **EF Core** farão o mapeamento das entidades e a persistência no MySQL. Os `Services` manterão as validações e regras do negócio, e os menus cuidarão da interação com o usuário.

## 8. Banco de dados com Entity Framework Core

### 8.1. Como vamos trabalhar

A implementação seguirá a **Aula 11.5 — Entity Framework Core**, do professor Bruno Martins:

1. **Entidades C#:** classes como `Usuario`, `Oferta` e `Reserva` representarão os dados.
2. **AppDbContext:** classe que herda de `DbContext` e reúne os conjuntos `DbSet<Usuario>`, `DbSet<Oferta>` e `DbSet<Reserva>`.
3. **Code First:** definiremos primeiro o modelo em C# e depois geraremos as tabelas no MySQL.
4. **Migrations:** criarão ou atualizarão a estrutura do banco conforme as alterações no modelo.
5. **CRUD com EF Core:** cadastro, consulta, atualização e exclusão de registros sem escrever `INSERT`, `SELECT`, `UPDATE` e `DELETE` manualmente.
6. **LINQ:** consultas com `Where`, `OrderBy`, `FirstOrDefaultAsync`, `ToListAsync` e `Include`.
7. **async/await:** operações como `SaveChangesAsync` e `ToListAsync` para acesso assíncrono aos dados.
8. **Relacionamentos:** chaves estrangeiras e propriedades de navegação entre usuários, ofertas e reservas.

> **EF Core não substitui o MySQL.** Ele é um ORM (mapeamento objeto-relacional) que conecta as classes C# às tabelas do banco.

### 8.2. Pacotes planejados

O material da aula utiliza o provedor **`MySql.EntityFrameworkCore`** para MySQL e o pacote de design do EF Core:

```bash
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet add package MySql.EntityFrameworkCore
```

As versões dos pacotes deverão ser compatíveis com o .NET do projeto. Para conferir os pacotes instalados:

```bash
dotnet list package
```

### 8.3. Comandos de criação e migrations

Depois de configurar o `AppDbContext` e a conexão local com o MySQL, o fluxo previsto é:

```bash
# Criar o projeto (se ainda não existir)
dotnet new console -n MesaViva
cd MesaViva

# Instalar os pacotes do EF Core
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet add package MySql.EntityFrameworkCore

# Instalar a ferramenta de migrations, caso não esteja disponível
dotnet tool install --global dotnet-ef

# Criar a primeira migration
dotnet ef migrations add InitialCreate

# Aplicar a migration ao MySQL
dotnet ef database update

# Executar o sistema
dotnet run
```

**Atenção:** esses comandos fazem parte do **planejamento**, ainda não foram executados. Antes de aplicar a migration, precisaremos configurar a string de conexão no `AppDbContext` (por exemplo, usando `UseMySQL`, como no slide), garantir que o servidor MySQL esteja em funcionamento e não publicar senhas no GitHub.

### 8.4. Exemplo de consulta com LINQ

Quando um estabelecimento visualizar as próprias ofertas, poderemos usar uma consulta parecida com esta:

```csharp
var minhasOfertas = await db.Ofertas
    .Where(o => o.EstabelecimentoId == usuarioLogadoId)
    .ToListAsync();
```

Para salvar alterações, usaremos `await db.SaveChangesAsync()`. A ideia é aplicar os mesmos conceitos de CRUD, LINQ e `async/await` apresentados na aula.

## 9. Tecnologias previstas

- **C# / .NET:** classes, menus e regras de negócio.
- **MySQL:** banco de dados relacional.
- **Entity Framework Core:** ORM e persistência dos dados.
- **MySql.EntityFrameworkCore:** provedor do EF Core para MySQL usado na aula.
- **Code First e Migrations:** criação/evolução das tabelas a partir das entidades.
- **LINQ e async/await:** consultas e operações assíncronas com o banco.
- **Git e GitHub:** versionamento e organização do trabalho.
- **Mermaid/UML:** documentação dos diagramas.

## 10. O que será demonstrado na apresentação

1. Criação de uma conta de estabelecimento e outra de instituição.
2. Login e acesso aos menus específicos.
3. Cadastro, consulta, atualização e exclusão de uma oferta sem reservas.
4. Reserva de parte da quantidade disponível de outra oferta.
5. Tentativa de reservar uma quantidade maior que a disponível (validação).
6. Cancelamento ou confirmação de uma coleta.
7. Consulta às ofertas urgentes.
8. Encerramento e reabertura do programa, comprovando que os dados continuam no MySQL.
9. Explicação dos diagramas, relacionamentos e conceitos de POO aplicados.
10. Demonstração do EF Core com `AppDbContext`, LINQ e Migrations no MySQL.

## 11. Planejamento de 14 dias

| Período | Atividade |
|---|---|
| Dias 1–2 | Fechar requisitos e diagramas. |
| Dias 3–4 | Criar classes, configurar AppDbContext e gerar o banco por Code First/Migrations. |
| Dias 5–7 | Implementar cadastro, login, menus e CRUD de ofertas. |
| Dias 8–10 | Implementar reservas, coletas e validações. |
| Dia 11 | Implementar consulta de ofertas urgentes. |
| Dia 12 | Testar os fluxos e corrigir erros. |
| Dia 13 | Revisar README, documentação e slides. |
| Dia 14 | Ensaiar a demonstração. |

## 12. Status do projeto

- [x] Definição do tema e escopo
- [x] Planejamento dos requisitos
- [x] Propostas iniciais dos diagramas
- [ ] Revisão e aprovação dos diagramas
- [ ] Configuração do EF Core e criação do banco por Migrations
- [ ] Implementação em C#
- [ ] Testes
- [ ] Apresentação final

---

**MesaViva — projeto acadêmico desenvolvido por Welinton e Leonardo.**

*Referência de estudo: Aula 11.5 — Entity Framework Core, Programação Orientada a Objetos, professor Bruno Martins.*
