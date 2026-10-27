## O que este PR faz

<!-- Descreva em 2-3 linhas. Se resolve uma issue, referencie (ex: Closes #20). -->

## Tipo de mudança

- [ ] Nova funcionalidade
- [ ] Correção de bug
- [ ] Refatoração (sem mudança de comportamento)
- [ ] Documentação
- [ ] Infraestrutura / CI / build

## Checklist

- [ ] Testes novos/atualizados cobrindo a mudança
- [ ] `dotnet test InvoiSys.slnx` passa localmente
- [ ] `dotnet format InvoiSys.slnx --verify-no-changes` limpo (e `npm run lint` se tocou no front)
- [ ] Nenhum segredo/credencial commitado (`.env`, token, chave de API)
- [ ] Se toca em regra de negócio ou invariante de domínio: documentação atualizada
      (README do projeto que mudou, e `docs/decisoes-arquiteturais.md` se for decisão nova).
      Sem `///` nem comentário explicativo no código: só aviso de 1 linha em armadilha real (ADR-023)
- [ ] Se toca em `InvoiSys.Domain`: nenhuma referência a `InvoiSys.Infrastructure` foi introduzida

## Como testar

<!-- Passos pra quem for revisar rodar localmente e confirmar que funciona. -->

## Notas para o revisor

<!-- Algo que merece atenção redobrada? Decisão que ficou em aberto? -->
