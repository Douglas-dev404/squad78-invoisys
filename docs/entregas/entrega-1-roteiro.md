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

**Se o link cair:** plano B é rodar local (`docker compose up -d` + `npm run dev` no `frontend/`), com o mesmo fluxo da demo.
