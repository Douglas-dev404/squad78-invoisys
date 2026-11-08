# Roteiro da apresentação — Entrega 1 (09/11)

Duração: ~10 min. Detalhes do que cada um entregou: [entrega-1.md](entrega-1.md).

**5 min antes:** abrir https://invoisys-front.onrender.com e logar uma vez, para conferir se está tudo de pé.

---

## 1. Onde paramos na última reunião (30 s)

O backend já fazia o fluxo principal: ler a Release no Jira, a IA gerar o comunicado e a API de revisão humana. Faltavam três coisas: **login**, **telas ligadas à API** e o sistema **rodando fora da máquina de quem desenvolve**.

## 2. O que entrou desde então (3 min)

**Sistema publicado, sempre ligado**
- API na **Azure** (App Service, plano que não hiberna) e banco PostgreSQL gerenciado, pelo pacote de estudante do GitHub.
- Front no **Render**, como site estático.
- Deploy automático da API: merge na `main` → GitHub Actions → Azure.
- Nenhuma senha ou chave no repositório.

**Acesso de verdade**
- Login com senha protegida (Argon2id) e token JWT; limite de tentativas contra força bruta.
- Esqueci a senha com link de uso único, que expira em 30 min.
- Toda rota de negócio exige login. Quem aprova um comunicado fica registrado pelo usuário logado, e não por um nome digitado.

**Front ligado à API**
- Rotas protegidas por login e por cargo: só o admin vê "Usuários".
- **Tela de Releases**, com o botão que dispara a IA.

**IA**
- Dá para trocar de provedor (OpenRouter ↔ OpenAI) só por configuração. Hoje roda com a **OpenAI**.

## 3. O que aprendemos testando com dados reais (1 min)

Rodamos o fluxo completo com o **Jira real e a OpenAI real**, e apareceram bugs que nenhum teste simulado pegava:

- Com credencial do Jira errada, a busca voltava vazia e a **IA inventava um comunicado**. Agora o sistema recusa antes de chamar a IA.
- A IA **imitava a formatação dos exemplos** do prompt: crases e aspas apareciam no texto final. Corrigido no prompt e na leitura da resposta.
- Na nuvem, todo mundo chega com o IP do servidor intermediário: **5 logins no mesmo minuto travariam a equipe inteira**. Corrigido.

> Mensagem: testar com dado real faz parte da entrega, não é etapa extra.

## 4. Demo (3 min)

1. Abrir o link: os destaques da tela de login vêm do banco.
2. Errar a senha → "E-mail ou senha inválidos." (não revela se o e-mail existe)
3. Entrar como **gestor** → Dashboard com nome e cargo. O menu **não mostra "Usuários"** (só admin).
4. **Nova Release** → digitar `2026.10.0` → **Processar**.
   - Enquanto roda (~20 s): *"a IA está lendo as 9 histórias dessa versão no Jira, juntando as duplicadas, classificando cada uma e reescrevendo em linguagem de cliente"*.
5. A Release aparece como **"Aguardando revisão"**: nada sai sem um humano aprovar.
6. F5 → continua logado. Sair → tentar abrir `/releases` direto → volta para o login.

**Se perguntarem do texto gerado:** a API já devolve o comunicado completo (título, resumo, itens por categoria). A tela que mostra, edita e aprova é a próxima entrega.

**Não clicar:** Dashboard e Histórico usam **dados de exemplo** (os reais vêm na Entrega 3); Notificações, Ajuda e Formalizar ainda não têm função.

## 5. Pronto, mas ainda fora do ar (1 min)

- **Trava contra processamento duplicado:** dois cliques em "Processar" faziam a IA rodar duas vezes, pagando o dobro. Implementamos uma trava no próprio banco: a segunda chamada é recusada na hora, antes de gastar IA. Está pronta e testada; entra no ar na próxima rodada.
- **Observabilidade escolhida:**
  - **Langfuse**, para enxergar cada chamada da IA: prompt, resposta, tokens, custo e tempo.
  - **Prometheus + Grafana**, para as métricas do sistema.
  - Está implementada, mas precisa de servidores próprios que o plano gratuito não comporta. A meta é subir na **Entrega 4**.

## 6. Próximos passos — Entrega 2, 16/11 (1 min)

- **Telas de revisão:** ver o comunicado gerado, editar e excluir itens, aprovar ou reprovar, e exportar o Markdown. **A API de tudo isso já está pronta e validada com dados reais**; falta só a tela.
- Redefinir senha pelo link do e-mail.
- Cadastro de usuários pelo admin.
- Dashboard e histórico de envios com dados reais (backend).

---

## Perguntas prováveis

**Produto e IA**

- **E se a IA escrever algo errado?** Nada sai sem revisão humana. Cada público (cliente, suporte…) tem a própria aprovação, e a API recusa exportar antes de aprovar (409). Na próxima entrega, o revisor edita ou exclui o item pela tela.
- **A IA pode inventar coisa?** Ela só recebe o texto das histórias do Jira. Se a Release não tem histórias (fixVersion errada, credencial sem acesso), o sistema recusa **antes** de chamar a IA. Esse caso aconteceu de verdade no nosso teste e foi corrigido (ADR-030).
- **De onde vem o texto que a IA usa?** Da subtarefa "Release Note" da história, quando ela existe; senão, da descrição técnica. A Release Note tem prioridade porque é o texto que o time já escreveu pensando no cliente.
- **Como trata histórias repetidas?** A IA agrupa as que falam do mesmo assunto. No teste, INV-3 e INV-4 (o mesmo bug de ICMS-ST) viraram um item só.
- **Quanto tempo leva?** 16 a 20 s para uma Release de 9 histórias com o `gpt-4o-mini`.
- **Quanto custa por Release?** São 2N + 2 chamadas à IA (N = grupos de histórias): 18 na Release de teste. Com o `gpt-4o-mini`, são centavos. O custo exato por execução aparece no Langfuse, quando a observabilidade subir.
- **Por que OpenAI?** O provedor é configurável: OpenAI direto ou OpenRouter, que dá acesso a vários modelos. Hoje usamos `gpt-4o-mini` pelo custo baixo e porque responde bem no formato JSON que o pipeline exige. Trocar de provedor é mudar uma variável, sem mexer no código.
- **Por que não usar LangChain?** O pipeline é linear (5 etapas fixas, sem busca em base de conhecimento). Um `HttpClient` com retry resolve; um framework de orquestração seria peso sem ganho.
- **Os dados do cliente vão para a OpenAI?** O texto das histórias vai, pela API. A política de uso de dados do provedor precisa ser validada com a InvoiSys antes de usar dados reais de clientes.

**Técnicas**

- **E se duas pessoas processarem a mesma Release ao mesmo tempo?** Hoje, o banco impede a Release duplicada (`UNIQUE`), mas as duas execuções pagam a IA. A **trava** do PR #57 resolve isso: é uma trava pessimista no PostgreSQL, por Release, pega antes de chamar a IA. A segunda chamada recebe "já está em processamento" na hora. Ela é solta sozinha se a API cair e funciona com várias instâncias da API. Entra na próxima rodada.
- **Por que trava pessimista e não otimista?** A otimista só descobre o conflito ao gravar, quando a IA já foi paga. Aqui o caro é o processamento, não a gravação.
- **Como vão monitorar a IA?** Langfuse: um trace por execução, com prompt, resposta, tokens, custo e tempo de cada chamada. Prometheus e Grafana cuidam do sistema (requisições, erros, latência, memória, duplicadas barradas). Tudo via OpenTelemetry, enviado pela API. Está pronto no PR #57 e sobe na Entrega 4.
- **Por que a observabilidade não está no ar?** O Langfuse self-hosted precisa de 6 containers (inclui ClickHouse), além do plano atual. Vamos avaliar os planos gratuitos do Langfuse Cloud e do Grafana Cloud: o código não muda.
- **Segurança?**
  - Senhas com Argon2id, o algoritmo recomendado hoje.
  - Token JWT válido por 8 h, e toda rota de negócio exige esse token.
  - Login limitado a 5 tentativas por minuto por IP; CORS só libera o nosso front.
  - Chaves e senhas ficam nas configurações da Azure, nunca no código; o Swagger fica desligado em produção.
- **Quem aprovou fica registrado?** Sim, pelo usuário do token, com chave estrangeira para a tabela de usuários. O histórico completo de quem fez o quê vem na Entrega 3.
- **E a recuperação de senha manda e-mail?** O fluxo está completo (link de uso único, 30 min, reenvio), mas ainda sem servidor de e-mail. Por enquanto, o link vai para o log da API.
- **Como sabem que funciona?** 349 testes no backend (incluindo PostgreSQL real em container, não banco simulado) e 18 no front, todos rodando no CI a cada PR. Além disso, o fluxo foi testado com Jira e IA reais, local e em produção.
- **Por que Azure e Render?** A API precisa estar sempre ligada, e o plano gratuito do Render dorme a cada 15 min. Na Azure usamos o crédito de estudante do GitHub (plano B1, sempre ligado; PostgreSQL grátis por 12 meses). O front é só arquivo estático, e o Render serve isso de graça e sem dormir.
- **Escala?** Dá para aumentar o plano do App Service ou subir mais instâncias. A trava mora no banco, então continua valendo com várias instâncias.
- **O que é aquele dashboard com números?** Dados de exemplo. A API do dashboard é da Entrega 2 e a tela com dados reais, da Entrega 3.

---

**Se o link cair:** plano B é rodar local (`docker compose up -d` + `npm run dev` no `frontend/`), com o mesmo fluxo da demo.
