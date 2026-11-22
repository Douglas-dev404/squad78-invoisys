# Guia de instalação e execução

Como subir o InvoiSys Release Notes do zero numa máquina de desenvolvimento, rodar os
testes e fazer o primeiro uso ponta a ponta. Para o ambiente de produção (Azure + Render),
veja [deploy.md](deploy.md).

## Índice

1. [Pré-requisitos](#1-pré-requisitos)
2. [Variáveis de ambiente](#2-variáveis-de-ambiente)
3. [Subir com Docker Compose](#3-subir-com-docker-compose)
4. [Rodar sem Docker (API e front locais)](#4-rodar-sem-docker-api-e-front-locais)
5. [Testes](#5-testes)
6. [Primeiro uso, passo a passo](#6-primeiro-uso-passo-a-passo)
7. [Solução de problemas](#7-solução-de-problemas)
8. [Deploy](#8-deploy)
9. [Observabilidade](#9-observabilidade)

---

## 1. Pré-requisitos

| Ferramenta | Versão | De onde vem a exigência |
|---|---|---|
| .NET SDK | **10.0** | `TargetFramework` `net10.0` nos `.csproj`; imagem `mcr.microsoft.com/dotnet/sdk:10.0` no `Dockerfile`; `dotnet-version: "10.0.x"` no CI |
| Node.js | **22** (com npm) | `node-version: "22"` em `.github/workflows/ci.yml` |
| Docker Desktop | versão atual, com Compose v2 (`docker compose`) | `docker-compose.yml`; Testcontainers nos testes de integração |
| Git | qualquer versão recente | clonar o repositório |
| Ferramenta `dotnet-ef` | 10.x (opcional) | só para criar ou aplicar migrations à mão |

O repositório não tem `global.json` nem campo `engines` no `frontend/package.json`: as
versões acima são as que o CI e o `Dockerfile` usam.

Conferir o que está instalado:

```bash
dotnet --version
node --version
npm --version
docker --version
docker compose version
git --version
```

Clonar:

```bash
git clone https://github.com/Douglas-dev404/squad78-invoisys.git
cd squad78-invoisys
```

---

## 2. Variáveis de ambiente

O .NET lê `appsettings.json`, `appsettings.{Ambiente}.json` e variáveis de ambiente, nessa
ordem (a última vence). **Em variável de ambiente o separador de seção é `__` (dois
underscores)**: `Jira__BaseUrl` vira `Jira:BaseUrl`. Nome em outro formato
(`JIRA_BASE_URL`) é ignorado sem aviso.

O modelo pronto para copiar é o [.env.example](../.env.example). O `.env` é ignorado pelo
Git; nunca commite chave real.

### 2.1 API (.NET)

| Variável | Obrigatória? | Exemplo (sem valor real) | Para que serve |
|---|---|---|---|
| `ConnectionStrings__Default` | **sim** (em Development já vem de `appsettings.Development.json`; no compose é sobrescrita com host `db`) | `Host=localhost;Port=5432;Database=invoisys;Username=invoisys;Password=<senha>` | Conexão com o PostgreSQL |
| `Database__MigrarAoIniciar` | não (padrão `false`; o compose liga) | `true` | Aplica as migrations do EF Core no boot da API |
| `Auth__Jwt__Chave` | **sim**: sem ela a API não sobe | `<segredo de 32+ caracteres>` | Chave de assinatura do JWT. Gere com `openssl rand -base64 48` (Bash) ou `$b = [byte[]]::new(48); [Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($b); [Convert]::ToBase64String($b)` (PowerShell). Trocar invalida todos os tokens |
| `Auth__Jwt__Emissor` | não (padrão `invoisys`) | `invoisys` | Emissor (`iss`) do JWT |
| `Auth__Jwt__Audiencia` | não (padrão `invoisys-api`) | `invoisys-api` | Audiência (`aud`) do JWT |
| `Auth__Jwt__ExpiracaoHoras` | não (padrão `8`) | `8` | Validade do token, em horas (maior que zero) |
| `Auth__AdminInicial__Email` | não, mas sem os três campos nenhum admin é criado | `admin@exemplo.com` | E-mail do primeiro administrador |
| `Auth__AdminInicial__Senha` | idem | `<senha inicial>` | Senha do primeiro administrador |
| `Auth__AdminInicial__Nome` | idem | `Administrador` | Nome do primeiro administrador |
| `Auth__RecuperacaoSenha__UrlBase` | **sim**: sem ela a API não sobe | `http://localhost:5173/redefinir-senha` | URL absoluta da tela de redefinição do front; o link enviado é `UrlBase#token=<token>` |
| `Auth__RecuperacaoSenha__CooldownSegundos` | não (padrão `60`) | `60` | Intervalo mínimo entre dois e-mails de recuperação para o mesmo usuário |
| `Auth__RecuperacaoSenha__LimiteRequisicoes` | não (padrão `5`) | `5` | Pedidos de recuperação aceitos por IP na janela |
| `Auth__RecuperacaoSenha__JanelaMinutos` | não (padrão `15`) | `15` | Janela do limite acima, em minutos |
| `Cors__Origens__0`, `__1`… | não, mas sem nenhuma o front em outra origem é bloqueado (em Development já vem `http://localhost:5173`) | `http://localhost:5173` | Origens liberadas no CORS, uma por índice, sem barra no fim |
| `RateLimit__Login__Tentativas` | não (padrão `5`) | `5` | Tentativas de login por IP na janela |
| `RateLimit__Login__JanelaSegundos` | não (padrão `60`) | `60` | Janela do limite de login, em segundos |
| `Proxy__ConfiarCabecalhosEncaminhados` | não (padrão `false`) | `false` | Usa `X-Forwarded-For` para o IP do cliente. Ligue só atrás de proxy reverso |
| `Jira__BaseUrl` | não, mas sem os quatro campos do Jira as rotas que usam o Jira respondem `501` | `https://suaempresa.atlassian.net` | URL do Jira Cloud |
| `Jira__Email` | idem | `voce@empresa.com` | Conta do Jira (Basic Auth) |
| `Jira__ApiToken` | idem | `<api token do Jira>` | API token da conta acima (não é a senha) |
| `Jira__Projeto` | idem | `INV` | Chave do projeto no Jira. A busca é `project = <chave> AND fixVersion = <versão>` |
| `Jira__ReleaseNote__TipoSubtarefa` | não (padrão `Release Note`) | `Release Note` | Tipo de subtarefa que guarda o texto da Release Note |
| `Jira__ReleaseNote__CampoCustomizado` | não | `customfield_10050` | Id do campo da história com o texto da Release Note. Preenchido, tem prioridade sobre a subtarefa |
| `Llm__Provedor` | não (padrão `OpenRouter`) | `OpenRouter` ou `OpenAi` | Qual provedor de IA usar. Valor diferente desses dois impede a API de subir |
| `OpenRouter__ApiKey` | não, mas sem ela (com provedor `OpenRouter`) `/processar` responde `501` | `<chave da OpenRouter>` | Chave da OpenRouter |
| `OpenRouter__Modelo` | não (padrão `openai/gpt-4o-mini`) | `openai/gpt-4o-mini` | Modelo no formato `provedor/modelo` |
| `OpenRouter__ModelosFallback__0`, `__1`… | não | `<provedor/modelo alternativo>` | Modelos alternativos tentados pela própria OpenRouter em erro 5xx/429 |
| `OpenRouter__Endpoint` | não (padrão `https://openrouter.ai/api/v1/chat/completions`) | — | Endpoint da OpenRouter; só mude para apontar a um mock |
| `OpenAi__ApiKey` | não, mas sem ela (com provedor `OpenAi`) `/processar` responde `501` | `<chave da OpenAI>` | Chave da OpenAI |
| `OpenAi__Modelo` | não (padrão `gpt-4o-mini`) | `gpt-4o-mini` | Modelo da OpenAI |
| `OpenAi__Endpoint` | não (padrão `https://api.openai.com/v1/chat/completions`) | — | Endpoint da OpenAI; só mude para apontar a um mock |
| `ASPNETCORE_ENVIRONMENT` | não (o `dotnet run` usa `Development` pelo `launchSettings.json`; o container roda como `Production`) | `Development` | Em `Development` a API publica Swagger e OpenAPI e lê `appsettings.Development.json` |
| `ASPNETCORE_HTTP_PORTS` | não (o `Dockerfile` define `8080`) | `8080` | Porta HTTP da API no container; o `--healthcheck` lê a mesma variável |
| `Logging__LogLevel__Default` | não (padrão `Information`) | `Information` | Nível de log da API |

### 2.2 Docker Compose (só para o `docker-compose.yml`)

| Variável | Obrigatória? | Exemplo | Para que serve |
|---|---|---|---|
| `POSTGRES_USER` | não (padrão `invoisys`) | `invoisys` | Usuário criado no container do Postgres |
| `POSTGRES_PASSWORD` | não (padrão `invoisys`) | `<senha local>` | Senha desse usuário |
| `POSTGRES_DB` | não (padrão `invoisys`) | `invoisys` | Banco criado no container |
| `API_PORT` | não (padrão `8080`) | `8080` | Porta do host mapeada para a API |
| `POSTGRES_PORT` | não (padrão `5432`) | `5432` | Porta do host mapeada para o Postgres |

### 2.3 Front (Vite)

| Variável | Obrigatória? | Exemplo | Para que serve |
|---|---|---|---|
| `VITE_API_BASE_URL` | não (padrão `http://localhost:8080/api/v1`) | `http://localhost:8080/api/v1` | Base da API usada pelo front. Lida quando o `npm run dev` ou o `npm run build` começa: mudou, reinicie |

O modelo do front é o [frontend/.env.example](../frontend/.env.example).

---

## 3. Subir com Docker Compose

Sobe a API (porta `8080`) e o PostgreSQL 16 (porta `5432`). O front roda à parte, com Node
(passo 5 desta seção).

**1. Criar o `.env`** (o compose exige o arquivo: `env_file: .env`).

```bash
cp .env.example .env
```

No PowerShell:

```powershell
Copy-Item .env.example .env
```

**2. Preencher no `.env`, no mínimo:**

- `Auth__Jwt__Chave` com um segredo de 32+ caracteres (`openssl rand -base64 48` no Bash; no
  PowerShell, `$b = [byte[]]::new(48); [Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($b); [Convert]::ToBase64String($b)`);
- `Auth__AdminInicial__Email`, `Auth__AdminInicial__Senha` e `Auth__AdminInicial__Nome`
  para ter com quem entrar;
- `Jira__BaseUrl`, `Jira__Email`, `Jira__ApiToken`, `Jira__Projeto` e `OpenRouter__ApiKey` (ou
  `Llm__Provedor=OpenAi` + `OpenAi__ApiKey`) para processar Releases de verdade. Sem eles a
  API sobe, mas `/processar` responde `501`.

`Auth__RecuperacaoSenha__UrlBase` e `Cors__Origens__0` já vêm prontos para
`http://localhost:5173` no `.env.example`.

**3. Subir:**

```bash
docker compose up --build -d
docker compose ps
docker compose logs -f api
```

O compose espera o Postgres ficar saudável, sobe a API com `Database__MigrarAoIniciar=true`
(aplica as migrations sozinho) e cria o admin inicial se o banco não tiver nenhum usuário.

**4. Conferir a API:**

```bash
curl http://localhost:8080/health
```

Resposta esperada: `{"status":"ok"}`. No Windows PowerShell, use `curl.exe` (o `curl` de lá
é um apelido de `Invoke-WebRequest`) ou abra a URL no navegador.

O container roda em ambiente `Production`, então **não há Swagger em `:8080`**. Para
explorar as rotas no navegador, rode a API com `dotnet run` (seção 4).

**5. Subir o front:**

```bash
cd frontend
npm ci
npm run dev
```

Abra `http://localhost:5173` e entre com o e-mail e a senha de `Auth__AdminInicial__*`.

Login direto pela API, para testar sem o front:

```bash
curl -X POST http://localhost:8080/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"<email do admin>","senha":"<senha do admin>"}'
```

**Parar e limpar:**

```bash
docker compose down          # para os containers, mantém o banco
docker compose down -v       # para e apaga o volume do banco (começa do zero)
```

---

## 4. Rodar sem Docker (API e front locais)

Útil para depurar a API. O Postgres continua precisando existir: o mais simples é subir só
ele pelo compose.

**1. Banco:**

```bash
docker compose up -d db
```

(Ou um PostgreSQL 16 instalado na máquina com usuário, senha e banco `invoisys`, como em
`appsettings.Development.json`.)

**2. Variáveis da API.** O `dotnet run` **não lê o arquivo `.env`**. Em Development a
connection string e o CORS já vêm de `appsettings.Development.json`; faltam as duas
obrigatórias e, se quiser, o admin, o Jira e a IA. Defina no terminal antes de rodar.

Bash:

```bash
export Auth__Jwt__Chave="$(openssl rand -base64 48)"
export Auth__RecuperacaoSenha__UrlBase="http://localhost:5173/redefinir-senha"
export Database__MigrarAoIniciar=true
export Auth__AdminInicial__Email="admin@exemplo.com"
export Auth__AdminInicial__Senha="<senha inicial>"
export Auth__AdminInicial__Nome="Administrador"
# opcional: Jira__BaseUrl, Jira__Email, Jira__ApiToken, Jira__Projeto, OpenRouter__ApiKey
```

PowerShell:

```powershell
$b = [byte[]]::new(48); [Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($b)
$env:Auth__Jwt__Chave = [Convert]::ToBase64String($b)
$env:Auth__RecuperacaoSenha__UrlBase = "http://localhost:5173/redefinir-senha"
$env:Database__MigrarAoIniciar = "true"
$env:Auth__AdminInicial__Email = "admin@exemplo.com"
$env:Auth__AdminInicial__Senha = "<senha inicial>"
$env:Auth__AdminInicial__Nome = "Administrador"
# opcional: $env:Jira__BaseUrl, $env:Jira__Email, $env:Jira__ApiToken, $env:Jira__Projeto, $env:OpenRouter__ApiKey
```

**3. API:**

```bash
dotnet restore InvoiSys.slnx
dotnet build InvoiSys.slnx
dotnet run --project src/InvoiSys.Api
```

A API sobe em `http://localhost:5049` (perfil `http` do `launchSettings.json`).

- Health: `http://localhost:5049/health`
- Swagger UI: `http://localhost:5049/swagger`
- OpenAPI: `http://localhost:5049/openapi/v1.json`

**4. Front apontando para a porta 5049.** O padrão do front é `:8080`. Crie
`frontend/.env.local` (ignorado pelo Git):

```bash
echo "VITE_API_BASE_URL=http://localhost:5049/api/v1" > frontend/.env.local
```

No PowerShell (o `>` do Windows PowerShell grava em UTF-16, que o Vite não lê):

```powershell
Set-Content -Path frontend/.env.local -Value "VITE_API_BASE_URL=http://localhost:5049/api/v1" -Encoding ascii
```

E suba o front:

```bash
cd frontend
npm ci
npm run dev
```

Front em `http://localhost:5173`.

**Migrations à mão** (em vez de `Database__MigrarAoIniciar=true`):

```bash
dotnet tool install --global dotnet-ef
dotnet ef database update --project src/InvoiSys.Infrastructure --startup-project src/InvoiSys.Api
```

---

## 5. Testes

### Backend

```bash
dotnet test InvoiSys.slnx
```

Roda unitários e integração. **Os testes de integração exigem o Docker rodando**: eles
sobem um `postgres:16-alpine` real via Testcontainers (ADR-012). Sem Docker, rode só os
unitários:

```bash
dotnet test InvoiSys.slnx --filter "FullyQualifiedName~Unit"
```

Formatação (o CI reprova se houver diferença):

```bash
dotnet format InvoiSys.slnx --verify-no-changes
dotnet format InvoiSys.slnx
```

O primeiro comando só confere; o segundo corrige.

### Frontend

```bash
cd frontend
npm ci
node --test tests/*.test.js
npm run lint
npm run build
```

São os mesmos passos do job `frontend` do CI: testes do cliente HTTP, lint (oxlint) e
build de produção.

Mais detalhes dos testes em [tests-dotnet/README.md](../tests-dotnet/README.md).

---

## 6. Primeiro uso, passo a passo

Pré-condição: API e front no ar (seção 3 ou 4), com Jira e provedor de IA configurados, e
uma Release (fixVersion) do Jira com histórias.

1. **Login.** Abra `http://localhost:5173`, entre com o e-mail e a senha do admin inicial.
   Você cai no Dashboard.
2. **Processar Release.** Menu **Releases** → **Processar Release** → informe a chave da
   Release, que é o nome da fixVersion no Jira (o pipeline busca `fixVersion = "<chave>"`)
   → confirme. Pode levar até 2 minutos; não feche a janela. Para conferir antes o que vem
   do Jira sem gastar chamada de IA, use `GET /api/v1/releases/{chave}/historias` (no
   Swagger, com a API em `dotnet run`).
3. **Revisar.** Abra a Release na lista. Na aba **Cliente**, leia título, resumo e itens
   por categoria. Em cada item: **Editar** (troca o texto), **Excluir** (tira do
   comunicado, com motivo opcional) ou **Reincluir**. No painel lateral, confira as
   histórias de origem e o histórico de execuções.
4. **Aprovar.** Clique em **Aprovar** e confirme. Para mudar algo depois, **Reabrir** volta
   a versão para revisão. Se o texto não serve, **Reprovar** (com motivo) e
   **Reprocessar com IA**.
5. **Exportar.** Com a versão aprovada, clique em **Exportar**. O modal abre com o
   comunicado em Markdown; o seletor troca para HTML. Há **Copiar** e **Baixar .md** ou
   **.html**. Itens excluídos não aparecem.

Para cadastrar outros usuários: menu **Usuários** → **Novo Usuário** (só admin). Depois do
primeiro login, troque a senha do admin pelo fluxo de recuperação e tire
`Auth__AdminInicial__Senha` do `.env`.

Recuperação de senha em ambiente local: ainda não há SMTP, então o link de redefinição
**sai no log da API** (nível Warning, "Envio de e-mail não configurado"). Com o compose:

```bash
docker compose logs api | grep "redefinição"
```

No PowerShell:

```powershell
docker compose logs api | Select-String "redefinição"
```

---

## 7. Solução de problemas

### 7.1 `dotnet test` falha nos testes de integração

**Sintoma:** os unitários passam, mas os testes de `Integration/` falham ao iniciar, com
erro do Testcontainers dizendo que não encontrou o Docker.

**Causa:** os testes de banco sobem um Postgres real em container (ADR-012). Docker Desktop
desligado derruba todos eles.

**Solução:** ligue o Docker Desktop, espere ficar pronto (`docker ps` responde) e rode de
novo. Para rodar só os unitários enquanto isso:

```bash
dotnet test InvoiSys.slnx --filter "FullyQualifiedName~Unit"
```

### 7.2 A API não sobe: `Auth:Jwt:Chave não configurada` ou `Auth:RecuperacaoSenha:UrlBase não configurada ou inválida`

**Causa:** autenticação não tem modo pendente (ADR-024). Sem `Auth__Jwt__Chave` (ou com
menos de 32 caracteres) ou sem `Auth__RecuperacaoSenha__UrlBase` absoluta, a API recusa o
boot. Acontece muito com `dotnet run`, que não lê o `.env`.

**Solução:** defina as duas variáveis (seção 2 e seção 4). No compose, confira se estão
preenchidas no `.env` e rode `docker compose up -d --force-recreate api`.

### 7.3 `/processar` responde `501` "Nenhum LLM provider configurado" ou "Jira não configurado"

**Causa:** comportamento esperado sem credencial (ADR-014). Sem `OpenRouter__ApiKey` (ou
sem `OpenAi__ApiKey` com `Llm__Provedor=OpenAi`), o composition root usa o
`ProviderPendente`; sem os quatro `Jira__*` obrigatórios, usa o `JiraClientPendente`. Não é bug.

**Solução:** preencha as chaves no `.env` (ou no terminal) e recrie a API. A mensagem do
`501` diz qual variável falta.

### 7.4 Front não fala com a API: erro de CORS ou "Failed to fetch"

**Causas comuns:**

- a origem do front não está em `Cors__Origens__N` (ou foi escrita com barra no fim:
  `http://localhost:5173/` não casa com `http://localhost:5173`);
- `VITE_API_BASE_URL` aponta para a porta errada: a API do compose está em `:8080`, a do
  `dotnet run` em `:5049`;
- `VITE_API_BASE_URL` foi alterada com o `npm run dev` já rodando.

**Solução:** ajuste `Cors__Origens__0` e recrie a API; ajuste `frontend/.env.local` e
reinicie o `npm run dev`.

### 7.5 Variável preenchida, mas a API age como se ela não existisse

**Causa:** nome com um underscore só ou em outro formato (`JIRA_BASE_URL`,
`OpenRouter_ApiKey`). O .NET só reconhece `__` como separador de seção e ignora o resto sem
aviso. Os nomes já tiveram de ser corrigidos uma vez na documentação do projeto.

**Solução:** copie os nomes exatamente do [.env.example](../.env.example) ou da tabela da
seção 2. Para conferir o que o container recebeu:

```bash
docker compose exec api printenv | grep __
```

No PowerShell:

```powershell
docker compose exec api printenv | Select-String "__"
```

Sem Docker (seção 4), o que vale é o ambiente do terminal que rodou o `dotnet run`:
`printenv | grep __` no Bash, `Get-ChildItem Env: | Where-Object Name -like "*__*"` no PowerShell.

### 7.6 `/processar` responde `422` "A Release '...' não tem histórias no Jira"

**Causa:** a busca `fixVersion = "<chave>"` voltou vazia (ADR-030). Ou a chave informada não
é o nome exato da fixVersion, ou a conta de `Jira__Email` não tem acesso ao projeto.

**Solução:** confira o nome da versão no Jira (é sensível ao texto exato) e se a conta do
token enxerga o projeto. Teste sem gastar IA com `GET /api/v1/releases/{chave}/historias`.

Outros casos que aparecem com frequência:

- **Não consigo entrar com o admin inicial:** ele só é criado se o banco não tiver nenhum
  usuário e os três `Auth__AdminInicial__*` estiverem preenchidos. Se o banco já tinha
  usuário, recriar com `docker compose down -v` apaga tudo e começa do zero.
- **`409` ao processar:** a mesma Release já está sendo processada (ADR-031) ou a versão já
  foi aprovada; reabra antes de reprocessar (ADR-020).
- **`429` no login:** limite de tentativas por IP (`RateLimit__Login__*`). Espere o tempo do
  cabeçalho `Retry-After`.

---

## 8. Deploy

Produção (API no Azure App Service, banco no Azure Database for PostgreSQL, front no Render),
App Settings, CI/CD e rollback: [deploy.md](deploy.md).

## 9. Observabilidade

Hoje a observabilidade é o log padrão do ASP.NET Core (console / `docker compose logs api`)
e o endpoint `/health`. O histórico de cada execução do pipeline (modelo, início, fim,
erro) fica persistido e aparece no detalhe da Release. Métricas e tracing (Langfuse,
Prometheus, Grafana) ficaram para depois: ver a nota ao fim da
[ADR-031](decisoes-arquiteturais.md#adr-031--trava-por-release-antes-do-pipeline-advisory-lock-no-postgres).
