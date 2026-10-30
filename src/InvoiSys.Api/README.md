# InvoiSys.Api — adapter de entrada (HTTP)

ASP.NET **Minimal API**. Camada **fina**: traduz HTTP em chamada de caso de uso ou porta,
e o resultado em DTO. **Nenhuma regra de negócio mora aqui.**

```
InvoiSys.Api/
├── Program.cs              # bootstrap: DI, migração opt-in, /health, modo --healthcheck
├── Auth/
│   ├── ConfiguracaoHttp.cs                 # CORS configurável e rate limit do login (por IP)
│   └── BearerSecuritySchemeTransformer.cs  # esquema Bearer no OpenAPI (botão Authorize)
├── Endpoints/
│   ├── AuthEndpoints.cs           # sessão: login, me, logout
│   ├── ReleaseEndpoints.cs        # rotas /api/v1/releases (listar, detalhe, historias, processar)
│   ├── RevisaoEndpoints.cs        # revisão humana por público (aprovar, reprovar, reabrir)
│   └── ItemComunicadoEndpoints.cs # revisão de item (editar, excluir, reincluir)
├── Contracts/
│   ├── AuthContracts.cs           # DTOs de sessão (LoginIn, LoginOut, UsuarioOut)
│   ├── ReleaseContracts.cs        # DTOs (records) — nunca expor entidade de domínio
│   ├── RevisaoContracts.cs        # DTOs de entrada da revisão
│   └── ItemComunicadoContracts.cs # DTOs de item (ItemRevisaoOut: detalhe e rotas de item)
├── Validation/
│   ├── LoginInValidator.cs        # regras de entrada do login (FluentValidation)
│   └── ValidacaoFiltro.cs         # filtro de endpoint: valida o corpo e devolve 422
└── appsettings*.json
```

---

## Endpoints atuais

| Método | Rota | Handler | Resposta | Erros |
|---|---|---|---|---|
| `GET` | `/health` | inline em `Program.cs` | `{ "status": "ok" }` | — |
| `POST` | `/api/v1/auth/login` | `LoginAsync` → `Autenticacao` | `LoginOut` · corpo `{ "email": "...", "senha": "..." }` | 401, 422, 429 |
| `GET` | `/api/v1/auth/me` | `MeAsync` → `Autenticacao` | `UsuarioOut` (exige Bearer) | 401 |
| `POST` | `/api/v1/auth/logout` | inline | 204 (não há o que revogar no servidor) | — |
| `GET` | `/api/v1/releases?status=` | `ListarReleasesAsync` → `ConsultaReleases` | `ReleaseResumoOut[]` (mais recente primeiro) | 422 (status desconhecido) |
| `GET` | `/api/v1/releases/{chaveRelease}` | `BuscarReleaseAsync` → `ConsultaReleases` | `ReleaseDetalheOut` (todas as versões por público; itens como `ItemRevisaoOut`, com `id`) | 404 |
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

## Autenticação (sessão)

Decisão em [ADR-024](../../docs/decisoes-arquiteturais.md#adr-024--autenticação-jwt-stateless-argon2id-e-admin-inicial).

### Exemplo de resposta de `/auth/login`

```json
{
  "usuario": {
    "id": "0b6a8d3e-4c1f-4a52-9d8e-7f3b2a1c9e10",
    "nome": "Ana Revisora",
    "email": "ana@invoisys.com",
    "papel": "release_manager",
    "avatarUrl": null,
    "criadoEm": "2026-10-11T12:00:00+00:00"
  },
  "token": "eyJhbGciOi...",
  "expiraEm": "2026-10-11T20:00:00+00:00"
}
```

O token vai no cabeçalho `Authorization: Bearer <token>`. Logout é o cliente descartar o
token, e por isso `POST /auth/logout` não faz nada no servidor (e fica anônimo, para
funcionar até com o token expirado).

| Situação | Status |
|---|---|
| e-mail inexistente, senha errada **ou** usuário desativado | **401**, sempre a mesma mensagem |
| e-mail ou senha ausentes, ou longos demais | **422**, com `errors` por campo (FluentValidation) |
| mais de N tentativas de login por minuto, por IP | **429**, com `Retry-After` |
| `/auth/me` sem token, com token inválido ou expirado, ou de usuário desativado | **401** |

### Configuração

| Variável | Padrão | Para quê |
|---|---|---|
| `Cors__Origens__0..N` | nenhuma | origens que podem chamar a API pelo navegador (front: `http://localhost:5173`) |
| `RateLimit__Login__Tentativas` | 5 | tentativas por janela, por IP |
| `RateLimit__Login__JanelaSegundos` | 60 | tamanho da janela |

Sem nenhuma origem configurada o CORS **não libera ninguém**. No Docker o ambiente é
Production, então `Cors__Origens__0` precisa estar no `.env`.

O rate limit conta por IP. Atrás de proxy ou Docker, o IP visto pode ser o do proxy;
`ForwardedHeaders` fica para quando houver proxy reverso na frente.

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
- **O contrato HTTP é definido pelo backend**: campos em português, vocabulário do
  domínio. O front se adapta; o mock do front (`name`, `role`, `password`, `user`) não é
  referência de contrato. As rotas técnicas genéricas (`login`, `me`, `logout`) seguem em
  inglês.
- O login responde **o mesmo 401** para e-mail inexistente, senha errada e usuário
  desativado, e roda `IHashSenha.Verificar` **uma vez em todos os casos**: quando o e-mail
  não existe, verifica contra um hash fictício com os mesmos parâmetros do hash real.
  Assim o tempo de resposta não revela quais e-mails estão cadastrados.

## Detalhes do `Program.cs`

- **Ordem de registro:** `AddOpenApi` → `AddInfrastructure` → `AddApplication` →
  autenticação JWT → `AddProblemDetails` → validadores → CORS → rate limit.
- **Ordem do pipeline:** `UseStatusCodePages` → `UseCors` → `UseRateLimiter` →
  `UseAuthentication` → `UseAuthorization`. O CORS vem antes do rate limit para o 429
  também sair com os cabeçalhos CORS, e `UseStatusCodePages` faz o 401 sem corpo virar
  `ProblemDetails`.
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
- Validação de **formato** do corpo (campo vazio, tamanho) fica em `Validation/`, com
  FluentValidation e `ValidacaoFiltro<T>` no endpoint. Regra de negócio continua no domínio.

## O que ainda falta neste módulo

| Item | Issue / fase |
|---|---|
| Paginação de `GET /api/v1/releases` (hoje lista tudo) | quando o volume pedir |
| ~~Exportar Markdown~~ feito: `POST /{chaveRelease}/exportar` (201; `ReleaseNaoAprovadaException` → 409; público/formato inválido ou html/pdf → 422) | #24 |
| ~~`/auth/login`, `/auth/me`, `/auth/logout`~~ feito (CORS e rate limit do login incluídos). Falta `/auth/forgot-password` e `[Authorize]` nas rotas de revisão | #49 feito; #52 e #50 |
| `GET /api/v1/branding/highlights` (repository já existe) | Fase 3 |
| Endpoint de histórico de execuções (`IExecucaoPipelineRepository`) e de exportações | Fase 4 |
