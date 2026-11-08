# Deploy no Render

O `render.yaml` na raiz descreve os três recursos do ambiente público:

| Recurso | Tipo | O que roda |
|---|---|---|
| `invoisys-db` | Postgres 16 gerenciado (free) | banco da aplicação |
| `invoisys-api` | Web service Docker (free) | a API, com o `Dockerfile` da raiz |
| `invoisys-front` | Static site | o build do `frontend/` (`dist/`) |

Nenhum segredo fica no repositório. Toda variável sensível está como `sync: false` no
`render.yaml`, e o valor é preenchido no painel do Render. A única exceção é a chave do JWT,
que o próprio Render gera (`generateValue: true`).

## Criar do zero

1. No Render: **New → Blueprint**, escolha este repositório e a branch `main`.
2. O Render lê o `render.yaml` e pede os valores das variáveis `sync: false`. Preencha
   conforme a tabela abaixo. Nessa hora a URL do front ainda não existe: use
   `https://invoisys-front.onrender.com` (é o nome do serviço). Se o Render der outro
   nome, corrija depois em **Environment**.
3. Espere o banco ficar **Available**. Em `invoisys-db → Connections`, copie *Hostname*
   (o interno), *Port*, *Database*, *Username* e *Password*, e monte a connection string
   no formato do Npgsql (não use a URL `postgresql://…`, que o Npgsql não aceita):

   ```
   Host=<hostname>;Port=5432;Database=invoisys;Username=invoisys;Password=<password>;SSL Mode=Prefer
   ```

   Cole em `invoisys-api → Environment → ConnectionStrings__Default`.
4. Faça **Manual Deploy** da API. No primeiro boot ela aplica as migrations e cria o admin
   inicial. O log mostra `Applying migration ...`.
5. Faça **Manual Deploy** do front **depois** de preencher `VITE_API_BASE_URL`. O Vite
   embute essa URL no build, então trocar a variável exige rodar o build de novo.

## Variáveis

### API (`invoisys-api`)

| Variável | Valor |
|---|---|
| `ConnectionStrings__Default` | montada no passo 3 |
| `Auth__AdminInicial__Email` / `__Senha` / `__Nome` | admin criado no primeiro boot (só se não existir nenhum usuário) |
| `Auth__RecuperacaoSenha__UrlBase` | `<url do front>/redefinir-senha` (obrigatória: sem ela a API não sobe) |
| `Cors__Origens__0` | `<url do front>`, sem barra no fim. Só essa origem é liberada |
| `Jira__BaseUrl` / `__Email` / `__ApiToken` | credenciais do Jira |
| `Llm__Provedor` | `OpenRouter` ou `OpenAi` |
| `OpenRouter__ApiKey` / `OpenRouter__Modelo` | se o provedor for OpenRouter |
| `OpenAi__ApiKey` / `OpenAi__Modelo` | se o provedor for OpenAI |

Já vêm fixas no `render.yaml`: `PORT=8080` (a porta do container),
`Database__MigrarAoIniciar=true` e `Proxy__ConfiarCabecalhosEncaminhados=true`. Esta última
existe porque o Render coloca um proxy na frente da API: sem ela, todo login chegaria com o
IP do proxy, e o rate limit (5 por minuto) valeria para todo mundo junto.

Sem a chave do provedor de IA, `/processar` responde 501. Isso é esperado (`ProviderPendente`).

### Front (`invoisys-front`)

| Variável | Valor |
|---|---|
| `VITE_API_BASE_URL` | `https://<url da api>/api/v1` |

O rewrite `/* → /index.html` está no `render.yaml`, e por isso F5 em `/releases` não dá 404.

## Conferir

- `GET https://<api>/health` → `200 {"status":"ok"}`
- `https://<api>/swagger` → `404` (o Swagger só existe em Development)
- Abrir o front em aba anônima, logar com o admin inicial e chegar no Dashboard.
- `git grep -nE "sk-|ApiKey=[^$]"` no repositório não encontra nada.

## Antes da apresentação

O plano free **hiberna** a API depois de ~15 minutos sem requisição, e o primeiro acesso
seguinte leva cerca de 1 minuto. Uns 5 minutos antes, abra `https://<api>/health` e espere
o `200`. O banco free expira 30 dias depois de criado.

## Atualizar e voltar versão

- Atualizar: merge na `main` → **Manual Deploy → Deploy latest commit** na API e no front
  (`autoDeploy` está desligado de propósito: nada vai para o ar sem alguém mandar).
- Voltar versão: em cada serviço, **Events → (deploy anterior) → Rollback**. Migration não
  volta sozinha no rollback. Por isso migration destrutiva não entra sem plano próprio.
