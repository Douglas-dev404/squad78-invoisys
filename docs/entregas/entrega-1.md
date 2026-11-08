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

- **Trava:** dois cliques em "Processar" na mesma Release faziam a IA rodar duas vezes (~10 chamadas pagas cada). A trava usa um *advisory lock* do Postgres por Release: a segunda chamada recebe 409 na hora, antes do Jira e da IA. Não precisa de infraestrutura nova.
- **Observabilidade escolhida:**
  - **Langfuse** (self-hosted) para a IA: um trace por execução, com prompt, resposta, modelo, tokens, custo e latência de cada chamada.
  - **Prometheus + Grafana** para o sistema: métricas de HTTP, do pipeline e da IA, com dashboard pronto.
  - Precisa de containers próprios (Langfuse usa Postgres, Redis e ClickHouse), que o plano gratuito não comporta. A meta é subir na **Entrega 4**.

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
