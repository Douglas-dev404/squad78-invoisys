# InvoiSys.Api — adapter de entrada (HTTP)

ASP.NET **Minimal API**. Camada **fina**: traduz HTTP em chamada de caso de uso ou porta,
e o resultado em DTO. **Nenhuma regra de negócio mora aqui.**

```
InvoiSys.Api/
├── Program.cs              # bootstrap: DI, migração opt-in, /health, modo --healthcheck
├── Endpoints/
│   └── ReleaseEndpoints.cs # rotas /api/v1/releases
├── Contracts/
│   └── ReleaseContracts.cs # DTOs (records) — nunca expor entidade de domínio
└── appsettings*.json
```

---

## Endpoints atuais

| Método | Rota | Handler | Resposta | Erros |
|---|---|---|---|---|
| `GET` | `/health` | inline em `Program.cs` | `{ "status": "ok" }` | — |
| `GET` | `/api/v1/releases/{chaveRelease}/historias` | `BuscarHistoriasAsync` → `IJiraClient` | `ReleaseOut` | 501, 502 |
| `POST` | `/api/v1/releases/{chaveRelease}/processar` | `ProcessarReleaseAsync` → `PipelineGeracaoReleaseNote` | `ReleaseProcessadaOut` | 409, 501, 502 |
| `PATCH` | `/api/v1/releases/{chaveRelease}/itens/{itemId}` | `EditarItemAsync` → `Release.EditarItem` | `ItemRevisaoOut` · corpo `{ "texto": "..." }` | 400, 404, 409 |
| `POST` | `/api/v1/releases/{chaveRelease}/itens/{itemId}/excluir` | `ExcluirItemAsync` → `Release.ExcluirItem` | `ItemRevisaoOut` · corpo opcional `{ "motivo": "..." }` | 404, 409 |
| `POST` | `/api/v1/releases/{chaveRelease}/itens/{itemId}/reincluir` | `ReincluirItemAsync` → `Release.ReincluirItem` | `ItemRevisaoOut` | 404, 409 |
| `GET` | `/openapi/v1.json` | gerado (só em Development) | spec OpenAPI 3.1 | — |
| `GET` | `/swagger` | Swagger UI (só em Development) | tela para explorar e testar as rotas | — |

### Exemplo de resposta de `/processar`

```json
{
  "chaveJira": "RELEASE-2026-08",
  "status": "aguardando_revisao",
  "tituloExecutivo": "Emissão de NF-e mais rápida e correções no ICMS",
  "resumoExecutivo": "Esta versão traz ...",
  "itens": [
    {
      "categoria": "correcao",
      "texto": "Corrigimos o cálculo de ICMS interestadual ...",
      "origens": ["INV-1234", "INV-1240"]
    }
  ]
}
```

## Tradução de erro → HTTP

`MapearFalhaDeIntegracao` captura **pelo tipo do domínio**, nunca pelo tipo do adapter
([ADR-013](../../docs/decisoes-arquiteturais.md#adr-013--exceções-de-integração-são-contrato-da-porta)):

| Exceção | Status | Significado |
|---|---|---|
| `RevisaoHumanaObrigatoriaException` (só em `/processar`) | **409** | a versão já foi aprovada; é preciso reabrir a revisão antes de reprocessar |
| `TransicaoDeStatusInvalidaException` (rotas de item) | **409** | item de versão aprovada; é preciso reabrir a revisão antes ([ADR-021](../../docs/decisoes-arquiteturais.md#adr-021--item-de-versão-aprovada-não-é-editado-sem-reabrir)) |
| `ItemNaoEncontradoException` (rotas de item) | **404** | o item não existe nesta Release (inclusive item de outra Release) |
| `ArgumentException` (rotas de item) | **400** | edição com texto vazio |
| `ProviderNaoConfiguradoException` | **501** | falta credencial (Jira ou OpenRouter). O `detail` diz o que configurar |
| `IntegracaoExternaException` (e filhas) | **502** | Jira/LLM falhou ou respondeu fora do formato |
| qualquer outra | 500 | erro interno de verdade |

Respostas de erro seguem `ProblemDetails` (`Results.Problem`).

## Detalhes do `Program.cs`

- **Ordem de registro:** `AddOpenApi` → `AddInfrastructure` → `AddApplication`.
- **Swagger UI** (`Swashbuckle.AspNetCore.SwaggerUI`): só a interface. Lê o mesmo
  `/openapi/v1.json` gerado pelo `Microsoft.AspNetCore.OpenApi`, então nunca diverge do
  contrato. Em Development apenas; `OpenApiTests` garante que não aparece em produção.
- **Migração no startup** só com `Database:MigrarAoIniciar=true` ([ADR-016](../../docs/decisoes-arquiteturais.md#adr-016--migração-de-banco-no-startup-é-opt-in)).
- **`--healthcheck`:** a imagem `aspnet` não tem curl/wget. O `HEALTHCHECK` do Docker
  roda `dotnet InvoiSys.Api.dll --healthcheck`, que chama `/health` e sai com 0/1.
- `public partial class Program` existe para o `WebApplicationFactory` dos testes.

## Regras para quem for mexer aqui

- `Endpoints/` **não pode** ter `using InvoiSys.Infrastructure.*` (só `Program.cs`, que
  é bootstrap).
- Todo endpoint declara `.Produces<T>()` e `.ProducesProblem(...)`. Sem isso o OpenAPI sai
  vazio, e `OpenApiTests` quebra de propósito.
- DTO sempre: enums saem via `ParaValor()` (`"nova_funcionalidade"`), nunca como nome
  PascalCase nem como número.
- Mutação de revisão: carregar a `Release` via `IReleaseRepository` → método de domínio
  → `SalvarAsync`. Nunca setar status direto.

## O que ainda falta neste módulo

| Item | Issue / fase |
|---|---|
| `GET /api/v1/releases` e `GET /api/v1/releases/{chave}` (com versões por público, itens, execuções) | #21 |
| `POST .../{chave}/versoes/{publico}/aprovar` · `/reprovar` · `/reabrir` (forma final a definir no PR) | #22 |
| Exportar Markdown | #24 |
| Mapear exceções de revisão (`RevisaoHumanaObrigatoriaException`, `ReleaseSemItensProcessadosException`, `ReleaseNaoAprovadaException`) para 409/422 | #22–#24 |
| `/auth/login`, `/auth/me`, `/auth/logout`, `/auth/forgot-password` + JWT + `[Authorize]` nas rotas de revisão | Fase 3 |
| `GET /api/v1/branding/highlights` (repository já existe) | Fase 3 |
| **CORS** para o frontend (hoje não configurado) | Fase 3 |
| Endpoint de histórico de execuções (`IExecucaoPipelineRepository`) e de exportações | Fase 4 |
