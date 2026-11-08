# Deploy

| Peça | Onde | Nome | Por quê |
|---|---|---|---|
| API | Azure App Service (Linux, plano B1, *Always On*) | `invoisys-api-squad78` | não hiberna; deploy de imagem Docker |
| Banco | Azure Database for PostgreSQL Flexible Server (B1MS, PG 16) | `invoisys-pg-squad78` | gratuito por 12 meses no Azure for Students |
| Imagem da API | GitHub Container Registry | `ghcr.io/douglas-dev404/invoisys-api` | pública: não contém segredo, os segredos são App Settings |
| Front | Render, static site | `invoisys-front` (`render.yaml`) | estático não hiberna; deploy a cada commit na `main` |

Grupo de recursos na Azure: `rg-invoisys`, região `chilecentral` (Chile Central: a mais próxima do Brasil entre as permitidas na assinatura de estudante).

Nenhum segredo fica no repositório. Os da API são **App Settings** do App Service. O único
segredo do GitHub é `AZURE_WEBAPP_PUBLISH_PROFILE`, que o workflow usa para fazer o deploy.

## Como o deploy acontece

- **API:** merge na `main` que toque em `src/`, `prompts/`, `Dockerfile` ou no próprio workflow
  dispara o [deploy-api.yml](../.github/workflows/deploy-api.yml). Ele builda a imagem,
  publica no GHCR com a tag do commit, troca a imagem do App Service e espera o
  `/health` responder. Também roda manualmente: **Actions → Deploy API (Azure) → Run workflow**.
- **Front:** o Render observa a `main` e refaz o build a cada commit.
- **Migrations:** `Database__MigrarAoIniciar=true`, aplicadas no boot da API.
  Migration destrutiva não entra sem plano próprio: o rollback da imagem não desfaz schema.

## App Settings da API

| Variável | Valor |
|---|---|
| `ConnectionStrings__Default` | `Host=invoisys-pg-squad78.postgres.database.azure.com;Port=5432;Database=invoisys;Username=<admin>;Password=<senha>;SSL Mode=Require` |
| `Auth__Jwt__Chave` | aleatória, 48+ caracteres |
| `Auth__AdminInicial__Email` / `__Senha` / `__Nome` | admin criado no primeiro boot (só se não houver nenhum usuário) |
| `Auth__RecuperacaoSenha__UrlBase` | `<url do front>/redefinir-senha` (obrigatória: sem ela a API não sobe) |
| `Cors__Origens__0` | `<url do front>`, sem barra no fim |
| `Jira__BaseUrl` / `Jira__Email` / `Jira__ApiToken` | credenciais do Jira (o token só vale para a conta do e-mail) |
| `Llm__Provedor` / `OpenAi__ApiKey` / `OpenAi__Modelo` | `OpenAi` / chave de platform.openai.com / `gpt-4o-mini` |
| `Database__MigrarAoIniciar` | `true` |
| `Proxy__ConfiarCabecalhosEncaminhados` | `true` (o App Service tem proxy na frente; sem isso o rate limit do login vale para todos juntos) |
| `WEBSITES_PORT` | `8080` (porta do container) |

Trocar um App Setting reinicia a API sozinho.

## Recriar do zero (Azure CLI)

```bash
az group create -n rg-invoisys -l chilecentral
az postgres flexible-server create -g rg-invoisys -n invoisys-pg-squad78 -l chilecentral \
  --tier Burstable --sku-name Standard_B1ms --storage-size 32 --version 16 \
  --admin-user <admin> --admin-password '<senha>' --public-access 0.0.0.0
az postgres flexible-server db create -g rg-invoisys -s invoisys-pg-squad78 -d invoisys
az appservice plan create -g rg-invoisys -n plan-invoisys -l chilecentral --is-linux --sku B1
az webapp create -g rg-invoisys -p plan-invoisys -n invoisys-api-squad78 \
  -i ghcr.io/douglas-dev404/invoisys-api:latest
az webapp config set -g rg-invoisys -n invoisys-api-squad78 --always-on true
az webapp config appsettings set -g rg-invoisys -n invoisys-api-squad78 --settings <variáveis acima>
```

`--public-access 0.0.0.0` libera o acesso a partir de serviços da Azure (a API), não da
internet inteira.

Para o deploy pelo GitHub, o publish profile precisa de autenticação básica ligada no SCM:

```bash
az resource update -g rg-invoisys --namespace Microsoft.Web --resource-type basicPublishingCredentialsPolicies \
  --name scm --parent sites/invoisys-api-squad78 --set properties.allow=true
az webapp deployment list-publishing-profiles -g rg-invoisys -n invoisys-api-squad78 --xml \
  | gh secret set AZURE_WEBAPP_PUBLISH_PROFILE
```

Front: no Render, **New → Blueprint**, este repositório, branch `main`.

## Conferir

- `GET https://invoisys-api-squad78.azurewebsites.net/health` → `200 {"status":"ok"}`
- `/swagger` → `404` (só existe em Development)
- Front em aba anônima: login → Releases → Processar Release.

## Voltar versão

- **API:** rodar o workflow de novo a partir do commit anterior, ou
  `az webapp config container set ... --container-image-name ghcr.io/douglas-dev404/invoisys-api:<sha anterior>`.
- **Front:** no Render, **Events → (deploy anterior) → Rollback**.

## Custos

O Flexible Server B1MS é gratuito por 12 meses no Azure for Students. O plano B1 do App
Service consome o crédito de estudante. O static site do Render é gratuito.
