# Como contribuir

## Branches

- **`main`** — protegida. Sem push direto, sem force-push. Só recebe merge de PR
  vindo de `develop` (ou hotfix crítico, via PR também).
- **`develop`** — branch de integração. Todo trabalho novo nasce daqui.
- **`feature/<nome-curto>`** — uma branch por tarefa, a partir de `develop`.
  Ex: `feature/adapter-llm-openai`, `feature/persistencia-release`.

Fluxo:

```
feature/xyz → PR → develop → (quando pronto pra release) → PR → main
```

## Antes de abrir PR

1. Testes passando localmente: `pytest`
2. Lint/format limpos: `pre-commit run --all-files` (ou deixe o hook de commit fazer
   isso automaticamente — ver `pre-commit install` no [AGENTS.md](AGENTS.md))
3. Se a mudança tocar em regra de negócio, invariante de domínio, ou decisão de
   arquitetura: atualizar a documentação relevante do projeto, ou ao menos deixar
   comentário no código explicando o porquê.

## Abrindo o PR

Use o template — ele carrega automaticamente. Preencha todas as seções, não deixe
checkbox sem marcar sem justificar por quê.

## CI

Todo PR roda automaticamente: lint (ruff), testes (pytest). PR não pode ser mergeado
com CI vermelho.

## Revisão de segurança

Qualquer PR que toque autenticação, autorização, dados sensíveis, ou a integração com
Jira/LLM (onde credenciais transitam) merece atenção redobrada na revisão — confirme
que nenhum segredo foi commitado (`.env`, token, chave de API) antes de aprovar.

## Para agentes de IA trabalhando neste repo

Leia [AGENTS.md](AGENTS.md) primeiro — ele tem o gate de confiança, os invariantes de
negócio e os padrões de arquitetura que qualquer mudança precisa respeitar.
