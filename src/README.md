# InvoiSys — Release Notes via IA (.NET)

Fundação arquitetural em .NET 10 do gerador de Release Notes. Só a arquitetura: domínio,
portas, pipeline, adapters e testes. Sem CI/CD, sem banco, sem front — o modelo de dados
está sendo feito em paralelo por outra pessoa.

O projeto Python original segue em [app/](../app/) e não foi tocado.

## Como rodar

```bash
dotnet build                          # na raiz do repositório
dotnet test tests-dotnet/InvoiSys.Tests
dotnet run --project src/InvoiSys.Api
```

A API sobe sem nenhuma configuração: os endpoints que dependem de Jira ou LLM respondem
`501 Not Implemented` com a mensagem do que falta configurar, em vez de falhar de forma
opaca.

### Documentação da API

Com a aplicação rodando em ambiente de desenvolvimento, o contrato OpenAPI 3.1 fica em
`http://localhost:5049/openapi/v1.json` — gerado do próprio código, então não tem como
divergir da implementação. Descreve as três rotas, o schema de cada DTO e as respostas
de erro (501 e 502). Para navegar visualmente, importe essa URL no Swagger Editor,
Postman ou Insomnia.

Quatro testes em [OpenApiTests.cs](../tests-dotnet/InvoiSys.Tests/Integration/OpenApiTests.cs)
garantem que o spec continue completo: sem eles, um handler que devolve `IResult` sem
declarar `.Produces<T>()` esvazia a documentação em silêncio, sem quebrar nada.

### Configuração

Por `appsettings.json` ou variáveis de ambiente (o separador de seção é `__`):

```bash
Jira__BaseUrl=https://suaempresa.atlassian.net
Jira__Email=voce@empresa.com
Jira__ApiToken=...            # API token, não senha

OpenRouter__ApiKey=...
OpenRouter__Modelo=openai/gpt-4o-mini
```

## Camadas

| Projeto | Papel | Depende de |
|---|---|---|
| `InvoiSys.Domain` | Entidades, enums e as **portas** (`IJiraClient`, `ILlmProvider`) | nada |
| `InvoiSys.Application` | `PipelineGeracaoReleaseNote` — orquestra os 5 estágios | Domain |
| `InvoiSys.Infrastructure` | Adapters concretos (Jira REST, OpenRouter) + composition root | Domain |
| `InvoiSys.Api` | Endpoints HTTP e DTOs | Application, Infrastructure |

A dependência aponta só para dentro: o domínio não conhece HTTP, JSON nem ASP.NET.
Trocar o provider de LLM é escrever um adapter novo e mudar uma linha em
[DependencyInjection.cs](InvoiSys.Infrastructure/DependencyInjection.cs) — nada no
domínio ou no pipeline muda.

## Pipeline

1. **Extração e limpeza** — normalização de texto, sem LLM (não gasta token)
2. **Categorização** — nova funcionalidade / melhoria / correção / outros
3. **Agrupamento semântico** — funde histórias que tratam do mesmo assunto
4. **Reescrita** — linguagem técnica vira linguagem de negócio
5. **Título e resumo executivos**

Os prompts vivem versionados em [prompts/](../prompts/), nunca em string no código —
são compartilhados com a versão Python.

## Invariantes que o código garante

- **Revisão humana é obrigatória.** `Release.Aprovar` é o único caminho para o status
  aprovado, e exige itens processados + status `AguardandoRevisao`. O pipeline nunca
  entrega nada publicável direto.
- **Edição humana ganha da IA.** `ItemComunicado.TextoFinal` prioriza o texto revisado.
- **Nenhuma história se perde no agrupamento.** Se o LLM omite uma chave, ela vira um
  grupo próprio, com log de aviso — some do comunicado seria pior.
- **Release Note dedicada tem precedência** sobre a descrição técnica como fonte da IA.

## O que falta

Persistência (não há repositório nem banco), endpoint de aprovação, exportação
(Markdown/HTML/PDF), autenticação e a interface de revisão.
