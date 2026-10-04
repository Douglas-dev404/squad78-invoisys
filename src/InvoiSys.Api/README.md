# InvoiSys.Api — adapter de entrada (HTTP)

ASP.NET **Minimal API**. Camada **fina**: traduz HTTP em chamada de caso de uso ou porta,
e o resultado em DTO. **Nenhuma regra de negócio mora aqui.**

```
InvoiSys.Api/
├── Program.cs              # bootstrap: DI, migração opt-in, /health, modo --healthcheck
├── Endpoints/
│   ├── ReleaseEndpoints.cs # rotas /api/v1/releases (historias, processar)
│   └── RevisaoEndpoints.cs # rotas de revisão humana (aprovar, reprovar, reabrir)
├── Contracts/
│   ├── ReleaseContracts.cs # DTOs (records) — nunca expor entidade de domínio
│   └── RevisaoContracts.cs # DTOs de entrada da revisão
└── appsettings*.json
```

---

## Endpoints atuais

| Método | Rota | Handler | Resposta | Erros |
|---|---|---|---|---|
| `GET` | `/health` | inline em `Program.cs` | `{ "status": "ok" }` | — |
| `GET` | `/api/v1/releases/{chaveRelease}/historias` | `BuscarHistoriasAsync` → `IJiraClient` | `ReleaseOut` | 501, 502 |
| `POST` | `/api/v1/releases/{chaveRelease}/processar` | `ProcessarReleaseAsync` → `PipelineGeracaoReleaseNote` | `ReleaseProcessadaOut` | 409, 501, 502 |
| `POST` | `/api/v1/releases/{chaveRelease}/aprovar` | `AprovarAsync` → `RevisaoComunicado` | 204 | 404, 409, 422 |
| `POST` | `/api/v1/releases/{chaveRelease}/reprovar` | `ReprovarAsync` → `RevisaoComunicado` | 204 | 404, 409, 422 |
| `POST` | `/api/v1/releases/{chaveRelease}/reabrir` | `ReabrirAsync` → `RevisaoComunicado` | 204 | 404, 409, 422 |
| `PATCH` | `/api/v1/releases/{chaveRelease}/itens/{itemId}` | `EditarItemAsync` → `RevisaoComunicado` | `ItemRevisaoOut` · corpo `{ "texto": "..." }` | 404, 409, 422 |
| `POST` | `/api/v1/releases/{chaveRelease}/itens/{itemId}/excluir` | `ExcluirItemAsync` → `RevisaoComunicado` | `ItemRevisaoOut` · corpo opcional `{ "motivo": "..." }` | 404, 409 |
| `POST` | `/api/v1/releases/{chaveRelease}/itens/{itemId}/reincluir` | `ReincluirItemAsync` → `RevisaoComunicado` | `ItemRevisaoOut` | 404, 409 |
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
| `ProviderNaoConfiguradoException` | **501** | falta credencial (Jira ou OpenRouter). O `detail` diz o que configurar |
| `IntegracaoExternaException` (e filhas) | **502** | Jira/LLM falhou ou respondeu fora do formato |
| qualquer outra | 500 | erro interno de verdade |

Nas rotas de revisão, `MapearFalhaDeRevisao` (em `RevisaoEndpoints`):

| Exceção | Status | Significado |
|---|---|---|
| `ReleaseNaoEncontradaException` | **404** | não há Release persistida com essa chave |
| `ReleaseSemItensProcessadosException`, `RevisaoHumanaObrigatoriaException`, `TransicaoDeStatusInvalidaException` | **409** | o estado atual impede a operação (sem comunicado, já aprovada, reabrir o que não foi aprovado) |
| `ArgumentException` | **422** | revisor ou motivo vazio |
| público ausente ou desconhecido | **422** | validado na conversão do DTO, sem default para Cliente |

Nas rotas de item, `MapearFalhaDeRevisaoDeItem` (em `ItemComunicadoEndpoints`), mesmo
contrato ([ADR-022](../../docs/decisoes-arquiteturais.md#adr-022--endpoint-só-chama-caso-de-uso-erros-de-revisão-padronizados)):

| Exceção | Status | Significado |
|---|---|---|
| `ReleaseNaoEncontradaException`, `ItemNaoEncontradoException` | **404** | Release inexistente, ou item que não existe nela (inclusive item de outra Release) |
| `TransicaoDeStatusInvalidaException` | **409** | item de versão aprovada; é preciso reabrir a revisão antes ([ADR-021](../../docs/decisoes-arquiteturais.md#adr-021--item-de-versão-aprovada-não-é-editado-sem-reabrir)) |
| `ArgumentException` | **422** | edição com texto vazio |

Respostas de erro seguem `ProblemDetails` (`Results.Problem`).

## Contratos: por que são assim

- DTO sempre, nunca entidade de domínio na resposta: o contrato HTTP fica estável mesmo
  que o domínio mude. Enums saem no formato herdado do contrato original
  (`nova_funcionalidade`, `aguardando_revisao`), e quem consome a API não precisa saber
  que o backend trocou de linguagem.
- Os campos dos DTOs de entrada da revisão são **anuláveis** porque é isso que o binder
  JSON entrega quando o campo falta. A obrigatoriedade (revisor, motivo, texto) é
  validada no domínio, não pela anotação do tipo.
- `aprovadoPor`/`revisadoPor` são texto livre enquanto não há autenticação. Quando houver,
  saem do corpo e passam a vir do usuário autenticado.
- `ItemRevisaoOut` traz o `id` (alvo das rotas de item), o texto da IA e a edição humana
  lado a lado, o `textoFinal` e se o item está incluído: o bastante para a tela atualizar
  a linha sem buscar a Release inteira de novo.
- As rotas de item não pedem público: o `itemId` é global, e a Release acha a versão
  dona do item. Item de outra Release dá 404.
- `GET .../historias` só consulta o Jira, nunca roda o pipeline, então o `status` da
  resposta é sempre `pendente`.

## Detalhes do `Program.cs`

- **Ordem de registro:** `AddOpenApi` → `AddInfrastructure` → `AddApplication`.
- **Swagger UI** (`Swashbuckle.AspNetCore.SwaggerUI`): só a interface. Lê o mesmo
  `/openapi/v1.json` gerado pelo `Microsoft.AspNetCore.OpenApi`, então nunca diverge do
  contrato. Em Development apenas; `OpenApiTests` garante que não aparece em produção.
- **Migração no startup** só com `Database:MigrarAoIniciar=true` ([ADR-016](../../docs/decisoes-arquiteturais.md#adr-016--migração-de-banco-no-startup-é-opt-in)).
- **`--healthcheck`:** a imagem `aspnet` não tem curl/wget. O `HEALTHCHECK` do Docker
  roda `dotnet InvoiSys.Api.dll --healthcheck`, que chama `/health` e sai com 0/1. A porta vem
  de `ASPNETCORE_HTTP_PORTS`, para o check não mentir se a porta mudar, e API fora do ar
  ou lenta (4 s) vira saída 1, não crash.
- `public partial class Program` existe para o `WebApplicationFactory` dos testes.

## Regras para quem for mexer aqui

- `Endpoints/` **não pode** ter `using InvoiSys.Infrastructure.*` (só `Program.cs`, que
  é bootstrap).
- Todo endpoint declara `.Produces<T>()` e `.ProducesProblem(...)`. Sem isso o OpenAPI sai
  vazio, e `OpenApiTests` quebra de propósito.
- DTO sempre: enums saem via `ParaValor()` (`"nova_funcionalidade"`), nunca como nome
  PascalCase nem como número.
- Endpoint não fala com repository nem decide nada: converte o DTO, chama o caso de uso
  em `InvoiSys.Application` e traduz o resultado/erro em HTTP (ADR-001). Mutação de
  revisão passa por `RevisaoComunicado`; nunca setar status direto.
- Um arquivo de endpoints por recurso (`ReleaseEndpoints`, `RevisaoEndpoints`...), não
  todas as rotas num arquivo só.

## O que ainda falta neste módulo

| Item | Issue / fase |
|---|---|
| `GET /api/v1/releases` e `GET /api/v1/releases/{chave}` (com versões por público, itens, execuções) | #21 |
| Exportar Markdown (mapear `ReleaseNaoAprovadaException` para 409) | #24 |
| `/auth/login`, `/auth/me`, `/auth/logout`, `/auth/forgot-password` + JWT + `[Authorize]` nas rotas de revisão | Fase 3 |
| `GET /api/v1/branding/highlights` (repository já existe) | Fase 3 |
| **CORS** para o frontend (hoje não configurado) | Fase 3 |
| Endpoint de histórico de execuções (`IExecucaoPipelineRepository`) e de exportações | Fase 4 |
