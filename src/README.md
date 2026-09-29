# Backend — visão das camadas

Backend .NET 10 do gerador de Release Notes, com arquitetura hexagonal em quatro projetos.
Cada um tem o próprio README com classes, métodos principais e pendências:

| Projeto | Papel | Depende de | README |
|---|---|---|---|
| `InvoiSys.Domain` | Entidades ricas, enums, **portas** e contrato de erro | nada | [→](InvoiSys.Domain/README.md) |
| `InvoiSys.Application` | Caso de uso `PipelineGeracaoReleaseNote` (5 estágios) | Domain | [→](InvoiSys.Application/README.md) |
| `InvoiSys.Infrastructure` | Adapters (Jira, OpenRouter, EF Core/Postgres) + composition root | Domain | [→](InvoiSys.Infrastructure/README.md) |
| `InvoiSys.Api` | Minimal API: endpoints, DTOs, erro → HTTP | Application, Infrastructure | [→](InvoiSys.Api/README.md) |

```
Api ──► Application ──► Domain ◄── Infrastructure
 └────────── Program.cs registra a DI ──────┘
```

A dependência aponta só para dentro. O domínio não conhece HTTP, JSON, EF nem ASP.NET.
Trocar o provider de LLM é escrever um adapter novo e mudar um binding em
[DependencyInjection.cs](InvoiSys.Infrastructure/DependencyInjection.cs). Nada no domínio
ou no pipeline muda.

## Um request `/processar`, atravessando as camadas

```
HTTP POST /api/v1/releases/RELEASE-2026-08/processar
  Api            ReleaseEndpoints.ProcessarReleaseAsync
  Application      PipelineGeracaoReleaseNote.ExecutarAsync
  Domain             IJiraClient ──────────────► Infrastructure JiraRestClient ──► Jira
  Domain             Release.MarcarProcessando / RegistrarExecucao
  Domain             ILlmProvider (×2G+2) ─────► Infrastructure OpenRouterProvider ──► OpenRouter
  Domain             Release.ConcluirProcessamento → VersaoComunicado(Cliente)
  Domain             IReleaseRepository (#20)  ► Infrastructure ReleaseRepository ──► Postgres
  Api            Release → ReleaseProcessadaOut (DTO)
HTTP 200 / 501 / 502
```

A explicação completa do fluxo, do percurso do dado e do roadmap está no
[README principal](../README.md). As decisões estão em
[docs/decisoes-arquiteturais.md](../docs/decisoes-arquiteturais.md).

## Como rodar

```bash
dotnet build InvoiSys.slnx            # na raiz do repositório
dotnet test InvoiSys.slnx             # precisa de Docker (Testcontainers)
dotnet run --project src/InvoiSys.Api # http://localhost:5049
```

A API sobe sem configuração de Jira/LLM: os endpoints que dependem deles respondem
`501 Not Implemented` com a mensagem do que falta configurar. Swagger UI em
`http://localhost:5049/swagger` e o spec em `http://localhost:5049/openapi/v1.json`
(Development), gerado do código, então não tem
como divergir da implementação.
