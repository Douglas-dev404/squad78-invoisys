# Entrega 1 — 09/11

Período: 26/10 a 08/11. Plano das entregas no épico [#90](https://github.com/Douglas-dev404/squad78-invoisys/issues/90).

**No ar:**

| Peça | Endereço | Onde roda |
|---|---|---|
| Sistema | https://invoisys-front.onrender.com | Render (site estático) |
| API | https://invoisys-api-squad78.azurewebsites.net | Azure App Service B1, sempre ligado |
| Banco | — | Azure PostgreSQL Flexible B1MS |

Como foi montado e como refazer: [deploy.md](../deploy.md).

## O que cada um entregou

| Pessoa | Entrega | Issue | Como entrou |
|---|---|---|---|
| **Anna** (annamelmoura) | Validação de entrada com FluentValidation (422 com erro por campo) | #60 | PR #95 |
| | `GET /branding/highlights`: destaques do login vindos do banco | #63 | PR #94 |
| | Cliente HTTP único do front (token, timeout, ProblemDetails, 401) | #64 | PR #93 |
| | Roteamento com react-router, rotas protegidas e menu em português | #65 | PR #101 |
| | Login, sessão e recuperação de senha na API real | #66 | PR #102 |
| | Destaques do login pela API, com fallback | #68 | PR #103 |
| **Victor** (OldDeux) | Login, `me` e logout, com CORS e limite de tentativas | #49 | PR #97 |
| | Adapter da OpenAI, escolhido por configuração (`Llm__Provedor`) | #61 | PR #100 |
| **Luiz** (Luizynn) | Base da autenticação: Argon2id, JWT e admin inicial (ADR-024) | #48 | PR #56 |
| | Exportação do comunicado em Markdown | #24 | PR #55 |
| | Esqueci a senha: token de uso único, e-mail pendente e limite de envio | #52 | via #105 (do PR #98) |
| | Rotas protegidas; o revisor vem do token, não do corpo | #50 | via #105 (do PR #98) |
| | Telas de Dashboard, Usuários e Perfil (dados de exemplo) | — | PR #59 |
| **Vitor** (vitorRuanDantas) | Tela de Histórico de envios (dados de exemplo) | — | PR #59 |
| **Bianca** (biancarepolho) | Tela de Releases: listar, filtrar e processar (dispara a IA) | #67 | via #105 |
| **Douglas** + Nexus | Integração dos PRs, deploy na Azure e no Render, CI/CD da API, validação real e correções (abaixo) | #62 | PRs #105, #106, #107 |

## O que avaliamos e ajustamos para juntar tudo

Os PRs foram feitos em paralelo, e vários se sobrepunham. A integração (#105) preservou a autoria de cada commit, e os PRs de origem aparecem como mergeados.

| Onde | O que foi encontrado | O que fizemos |
|---|---|---|
| #95 × #97 | Dois filtros de validação (`Api/Validacao` e `Api/Validation`), com formatos de 422 diferentes | Ficou só o `Api/Validacao`; o login usa `.ComValidacao<LoginIn>()` |
| #97 × #98 | Duas configurações de rate limit; o endpoint de auth era registrado duas vezes | Uma configuração só, com as duas políticas (login e esqueci-senha) |
| #97 × #98 | Pastas `Api/Auth` e `Api/Autenticacao`, e um namespace que chocava com a classe `Acesso.Autenticacao` | Unificado em `Api/Auth` |
| #58, #96 e #98 | O #98 já trazia os commits do #58 e do #96, rebaseados | Entrou a versão do #98; #58 e #96 foram fechados como superados |
| #98 | O histórico de revisão (#53) é da Entrega 3 | Entraram só esqueci-senha, rotas protegidas e exportação protegida; o histórico continua no #98 |
| Testes do #98 | Um teste vermelho no CI (revisor gravado pelo nome) e testes de FK esperando a exceção errada | Corrigidos |
| Tela de Releases | O import `./api` não existia, o nome do modal estava errado, a navegação ia para uma tela que ainda não existe, e havia um `BrowserRouter` duplicado | Corrigidos; entraram só os 6 arquivos da tela, com autoria da Bianca |
| ADRs | Numeração repetida entre PRs | Renumeradas: hoje vão de 001 a 030, com a 027 reservada para o histórico do #98 |

## Bugs encontrados na validação real (Jira real + OpenAI `gpt-4o-mini`)

Nenhum deles aparecia nos testes com dados simulados.

| Bug | Efeito | Correção |
|---|---|---|
| Release sem histórias seguia para a IA | Com token do Jira inválido, a busca volta vazia e a IA **inventava** um comunicado | 422 antes de chamar a IA, sem gravar nada ([ADR-030](../decisoes-arquiteturais.md)) |
| Categoria vinha entre crases (`` `correcao` ``) | O processamento quebrava com 502 | Prompt 02 sem crase no exemplo, e o parser tira o invólucro (as 4 categorias continuam fixas) |
| Texto reescrito vinha "entre aspas" | Aspas no Markdown que iria para o cliente | Prompt 04 sem aspas no exemplo, e o adapter tira aspas em volta do parágrafo |
| Rate limit do login atrás de proxy | Na nuvem todos chegam com o IP do proxy: 5 logins no mesmo minuto travariam a equipe inteira | `Proxy__ConfiarCabecalhosEncaminhados` conta o IP real do cliente |
| Botão "Nova Release" | Só navegava; estando na própria tela, não fazia nada | Abre o modal de processar |

**Validado ponta a ponta**, local e em produção: login → processar a Release `2026.10.0` (9 histórias viram 8 itens; a duplicata foi agrupada) → editar e excluir itens → reprovar, reprocessar e aprovar → exportar o Markdown.

A API recusa com 409:
- exportar antes de aprovar;
- reprocessar uma Release aprovada;
- editar uma versão aprovada.

Testes: 349 no backend e 18 no front.

## Pronto, mas ainda fora do ar

**PR #57: trava contra processamento duplicado e observabilidade** (Douglas). Implementado e testado; volta depois do rebase sobre o adapter da OpenAI (#100).

### Trava contra processamento duplicado

**O problema.** Processar uma Release faz **2N + 2 chamadas pagas à IA**, sendo N o número de grupos de histórias: 1 para agrupar, 1 para categorizar e 1 para reescrever cada grupo, e 1 para título e resumo. A `2026.10.0` tem 8 grupos: são 18 chamadas e ~17 s. Se alguém clica duas vezes, ou duas pessoas processam a mesma Release ao mesmo tempo, tudo isso roda em dobro.

O banco já tinha `UNIQUE` em `chave_jira`, mas essa proteção só age no **fim**: a segunda execução gasta as 18 chamadas e falha ao gravar. Ela evita o dado duplicado, não o custo duplicado.

**O tipo de trava.** É uma trava **pessimista**: impede o conflito antes de ele acontecer, em vez de detectá-lo depois. Também é **sem espera** (*try-lock*): quem chega em segundo lugar não entra numa fila, recebe a recusa na hora.

| Pergunta | Resposta |
|---|---|
| Que recurso do banco? | *Advisory lock* de sessão do PostgreSQL (`pg_try_advisory_lock`). É uma trava sobre um **número** escolhido pela aplicação, e não sobre uma linha. Isso importa porque, na primeira execução, a linha da Release ainda nem existe |
| Qual número? | Os 8 primeiros bytes do SHA-256 de `invoisys:processar-release:<CHAVE EM MAIÚSCULAS>`. A mesma Release gera sempre o mesmo número; Releases diferentes geram números diferentes (colisão com chance de ~1 em 2⁶⁴) |
| Quando ela é pega? | No começo do pipeline, **antes** de chamar o Jira e a IA |
| E quem chega em segundo? | `pg_try_advisory_lock` devolve `false` na hora, e a API responde **409** ("Release já está em processamento"). Medido: ~25 ms, contra ~2,7 s da execução que segura a trava (no teste com Jira e IA simulados) |
| Quando é solta? | Ao terminar, com sucesso ou com erro (`await using` + `pg_advisory_unlock`). Se a API cair no meio, a conexão fecha e o PostgreSQL solta a trava sozinho, sem deixar Release presa |
| Funciona com várias instâncias da API? | Sim. A trava mora no banco, que todas as instâncias compartilham. Um `lock` em memória só protegeria uma instância |
| Por que não otimista? | Concorrência otimista (versão/`UNIQUE`) só detecta o conflito na hora de gravar, depois da IA já ter sido paga |
| Custo | Ocupa 1 conexão do pool durante a execução (~20 s). O status "Processando" ainda não aparece no `GET /releases` |

A trava também conta quantas duplicadas barrou: métrica `invoisys.pipeline.bloqueios`, abaixo.

### Observabilidade

**Escolha:** **Langfuse** para enxergar a IA, e **Prometheus + Grafana** para as métricas do sistema. Tudo instrumentado com **OpenTelemetry**, o padrão aberto, para não amarrar o código a um fornecedor. Fica **desligado por padrão** (`Observabilidade__Habilitada=false`); o arquivo `docker-compose.observabilidade.yml` liga tudo localmente.

**Como os dados nascem no código.** O código usa as APIs nativas do .NET: `ActivitySource` para os traces e `Meter` para as métricas. Por isso o `InvoiSys.Domain` não depende de nenhuma biblioteca de observabilidade, e quem exporta é o OpenTelemetry SDK, configurado só na API.

**Langfuse: traces da IA**

| Aspecto | Como funciona |
|---|---|
| O que é registrado | Um **trace** por execução do pipeline, com um **span do tipo `generation`** para cada chamada à IA. Cada span guarda prompt, resposta, modelo, temperatura, tokens de entrada e de saída, custo (quando o provedor informa) e latência |
| Como chega lá | A API **envia** (push) via **OTLP** (HTTP/protobuf) para o endpoint OpenTelemetry do Langfuse, autenticando com a chave pública e a secreta. O Langfuse não tem SDK para .NET; por isso o caminho é OpenTelemetry |
| Para que serve | Abrir uma execução e ver exatamente o que a IA recebeu e devolveu em cada etapa. É assim que se depura um texto ruim, compara modelos e acompanha custo por Release |
| O que roda | Langfuse self-hosted: web + worker + PostgreSQL + ClickHouse (análises) + Redis (fila) + MinIO (armazenamento dos eventos) |

**Prometheus + Grafana: métricas do sistema**

| Aspecto | Como funciona |
|---|---|
| Como chega lá | Também por **push**: a API envia as métricas por OTLP a cada **15 s** para o receptor OTLP do Prometheus (`--web.enable-otlp-receiver`). Não é o *scrape* clássico, então a API não precisa expor um `/metrics` público |
| Métricas da aplicação | `invoisys.pipeline.execucoes` (por resultado), `invoisys.pipeline.duracao`, `invoisys.pipeline.bloqueios` (duplicadas barradas), `invoisys.llm.chamadas` (por etapa, modelo e resultado), `invoisys.llm.duracao`, `invoisys.llm.tokens` |
| Métricas do runtime | .NET (memória, CPU, GC), ASP.NET Core (requisições por rota e status, latência), Kestrel, HttpClient (chamadas ao Jira e à IA) e Npgsql (banco) |
| Grafana | Dashboard pronto, com seções Visão geral, IA (execuções, falhas, duplicadas barradas, chamadas, tokens, p95 de latência por etapa), API HTTP (requisições, erros 5xx, p95 por rota) e Runtime .NET |

**Por que ainda não está no ar.** O Langfuse self-hosted sozinho são 6 containers (ClickHouse incluído), e o plano atual (um App Service B1) não comporta isso. Antes de subir na **Entrega 4**, vale avaliar os **planos gratuitos do Langfuse Cloud e do Grafana Cloud**: o código não muda, só os endpoints e as chaves.

**Pendência técnica do rebase:** no #57, a instrumentação da IA estava no `OpenRouterProvider`. Depois do #100, ela precisa ir para a base comum `ChatCompletionsLlmProvider`, para que as chamadas da OpenAI também apareçam no Langfuse.

## O que cada tela faz

| Tela | Rota | O que faz | Dados |
|---|---|---|---|
| Login | `/login` | Entra com e-mail e senha. Mostra erro de credencial (sem revelar se o e-mail existe), erro por campo (422) e excesso de tentativas (429). À direita, os destaques institucionais | **Reais** (API); destaques vêm do banco, com fallback local |
| Esqueci a senha | `/esqueci-senha` | Pede o link de redefinição pelo e-mail | **Real**. Ainda sem SMTP: o link sai no log da API |
| E-mail enviado | `/email-enviado` | Confirma o envio, mostra a validade do link (30 min) e permite reenviar | **Real** |
| Redefinir senha | `/redefinir-senha` | Placeholder | Tela na Entrega 2 (#73); a API já existe |
| Dashboard | `/dashboard` | KPIs (formalizados, em revisão, enviados), gráfico da semana e últimos cards | **Exemplo**. API na Entrega 2 (#74), tela real na Entrega 3 (#77) |
| Releases | `/releases` | Lista as Releases com status e data; filtra por status (abas na URL); pagina; **Processar Release** abre o modal que dispara Jira + IA (até 3 min de espera, trava contra duplo clique, erro da API em português) | **Real** |
| Detalhe da Release | `/releases/:chave` | Placeholder | Entrega 2 (#69–#72). A API já devolve o comunicado completo |
| Histórico de envios | `/historico` | Filtros e tabela dos comunicados enviados, com prévia | **Exemplo**. API na Entrega 2 (#75), tela real na Entrega 3 (#79) |
| Perfil | `/perfil` | Nome, e-mail, cargo e data de criação do usuário logado | **Real** (`/auth/me`) |
| Usuários | `/usuarios` | Lista, busca, painel do usuário, ativar e desativar. **Só admin** | **Exemplo**. API no PR #99 (Entrega 2), tela real na Entrega 3 (#78) |
| Novo usuário | `/usuarios/novo` | Nome, e-mail, cargo e permissões. **Só admin** | **Exemplo** (some no F5) |

Em todas as telas internas: menu lateral (recolhível), barra superior com o usuário logado e "Sair". Sem sessão, qualquer rota interna volta para o login; um cargo sem permissão vê "Sem permissão". Os botões **Notificações**, **Ajuda** e **Formalizar** ainda não têm função.

## Ficou para as próximas entregas

| Item | Responsável | Entrega |
|---|---|---|
| Cadastro e desativação de usuário pelo admin (PR #99) | Richard | 2 |
| Histórico de revisão (PR #98, restante) | Luiz | 3 |
| Responsividade e acessibilidade (PR #104) | Vitor | 3 |
| Trava + observabilidade (PR #57) | Douglas | 2 (trava) / 4 (observabilidade) |

## Plano da Entrega 2 — 16/11 (PR aberto até quinta, 12/11)

| Issue | O quê | Dono | Observação |
|---|---|---|---|
| #69 | Tela de detalhe da Release: comunicado por público, itens por categoria | Bianca | **Bloqueia #70, #71 e #72.** A API já entrega tudo (`GET /releases/{chave}`) |
| #70 | Editar, excluir e reincluir item | sem dono | API pronta e validada |
| #71 | Aprovar, reprovar e reabrir | sem dono | API pronta. **Falta decidir** como sai uma versão reprovada (abaixo) |
| #72 | Exportar Markdown: prévia, copiar, baixar | sem dono | API pronta e validada |
| #73 | Tela de redefinir senha (link do e-mail) | sem dono | API pronta |
| #74 | `GET /dashboard` (substitui os dados de exemplo) | sem dono | backend novo |
| #75 | `GET /exportacoes` (histórico de envios) | sem dono | backend novo |
| #51 | Cadastro de usuários pelo admin | Richard | revisar e integrar o PR #99 |

**Decisões pendentes antes da Entrega 2:**

1. **Versão reprovada.** O domínio diz que ela volta **reprocessando com a IA**, e é o que o código faz. A #71 pede um botão **"Reabrir"** para versão reprovada, o que a API recusa hoje. Precisa escolher um dos dois antes de alguém pegar a #71.
2. **Resumo executivo desatualizado.** Título e resumo são gerados antes da revisão. Excluir um item não atualiza o resumo, e não há rota para editá-los. Sugestão: issue nova na Entrega 2.
3. **Itens internos.** A IA ainda não marca itens internos (log, ajuste técnico), e o revisor precisa excluí-los à mão. Entra no trabalho de prompts com exemplos reais (#83, Entrega 4).
