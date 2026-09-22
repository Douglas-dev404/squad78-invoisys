# InvoiSys — Release Notes via IA (.NET)

Backend em .NET 10 do gerador de Release Notes: domínio, portas, pipeline, adapters,
persistência (EF Core + PostgreSQL) e testes.

Modelagem de dados completa, com ERD e explicação tabela a tabela, em
[docs/modelagem-de-dominio.md](../docs/modelagem-de-dominio.md). O frontend
(React + Vite) vive em [frontend/](../frontend/).

## Como rodar

```bash
dotnet build InvoiSys.slnx            # na raiz do repositório
dotnet test InvoiSys.slnx
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

Os prompts vivem versionados em [prompts/](../prompts/), nunca em string no código.

## Invariantes que o código garante

- **Revisão humana é obrigatória, por público.** `VersaoComunicado.Aprovar` (exposto
  por `Release.Aprovar`) é o único caminho para o status aprovado, e exige itens
  processados, ao menos um item não excluído, e status `AguardandoRevisao`. O pipeline
  nunca entrega nada publicável direto.
- **Exportação sem aprovação é impossível.** O construtor de `ComunicadoExportado`
  rejeita versão não aprovada — não existe caminho de código que burle o gate.
- **Edição humana ganha da IA.** `ItemComunicado.TextoFinal` prioriza o texto revisado.
- **Nenhuma história se perde no agrupamento.** Se o LLM omite uma chave, ela vira um
  grupo próprio, com log de aviso — some do comunicado seria pior.
- **Release Note dedicada tem precedência** sobre a descrição técnica como fonte da IA.

## O que falta

Camada de Repository ligando o pipeline ao banco (o schema existe, o pipeline ainda roda
em memória), endpoints de revisão/aprovação, exportação (Markdown/HTML/PDF), autenticação
JWT e a interface de revisão no frontend. Lista completa em
[docs/modelagem-de-dominio.md](../docs/modelagem-de-dominio.md#pendências-conhecidas).
