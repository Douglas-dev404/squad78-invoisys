## O que este PR faz

<!-- Descreva em 2-3 linhas. Se resolve uma issue/pendência rastreada internamente, referencie. -->

## Tipo de mudança

- [ ] Nova funcionalidade
- [ ] Correção de bug
- [ ] Refatoração (sem mudança de comportamento)
- [ ] Documentação
- [ ] Infraestrutura / CI / build

## Checklist

- [ ] Testes novos/atualizados cobrindo a mudança
- [ ] `dotnet test InvoiSys.slnx` passa localmente
- [ ] `pre-commit run --all-files` limpo (lint + format)
- [ ] Nenhum segredo/credencial commitado (`.env`, token, chave de API)
- [ ] Se toca em regra de negócio ou invariante de domínio: documentação atualizada
      (documentação interna do projeto, ou comentário explicando o porquê no código)
- [ ] Se toca em `InvoiSys.Domain`: nenhuma referência a `InvoiSys.Infrastructure` foi introduzida

## Como testar

<!-- Passos pra quem for revisar rodar localmente e confirmar que funciona. -->

## Notas para o revisor

<!-- Algo que merece atenção redobrada? Decisão que ficou em aberto? -->
