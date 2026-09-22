# Squad 78 — InvoiSys

Residência IV. Geração automática de Release Notes via IA, a partir das histórias de uma
Release no Jira.

## Contexto

A InvoiSys é uma empresa especializada em soluções SaaS para gestão de Documentos Fiscais
Eletrônicos (DF-e), atendendo clientes de diversos segmentos do mercado.

A cada nova versão (Release) do produto é criado um card no Jira contendo todas as histórias,
correções, melhorias e novas funcionalidades que serão disponibilizadas aos clientes. Algumas
dessas histórias possuem uma subtarefa específica do tipo Release Note, enquanto outras
possuem apenas a descrição técnica da implementação.

Atualmente, a elaboração dos comunicados enviados aos clientes é um processo manual, exigindo
que um colaborador leia todas as histórias da Release, identifique quais alterações são
relevantes para o cliente, consolide as informações, adapte a linguagem técnica para uma
comunicação de negócio e produza um comunicado final.

## Desafio

Desenvolver uma solução baseada em Inteligência Artificial capaz de automatizar esse processo.

A solução deve consumir as informações da Release (preferencialmente via API do Jira),
interpretar as histórias relacionadas à versão publicada, identificar automaticamente
funcionalidades, correções e melhorias relevantes, e gerar comunicados claros, organizados e
direcionados ao público final.

A IA deve ser capaz de:
- Interpretar descrições técnicas das histórias.
- Utilizar informações existentes nas subtarefas de Release Note quando disponíveis.
- Resumir funcionalidades semelhantes.
- Eliminar informações duplicadas.
- Adaptar o texto para uma linguagem de negócio.
- Organizar os comunicados por categorias (Novas Funcionalidades, Melhorias, Correções, etc.).
- Sugerir títulos e resumos executivos da Release.
- Permitir revisão humana antes da publicação.

### Diferenciais possíveis (não obrigatórios)

- Geração de diferentes versões do comunicado (cliente, equipe interna, comercial e suporte).
- Geração em Markdown, HTML ou PDF.

## Objetivo

Ao final do projeto: um protótipo funcional (MVP) que demonstre a viabilidade da solução.

### Entregáveis esperados

- Aplicação funcional para geração automática de comunicados de Release.
- Integração com a API do Jira (ou mecanismo equivalente para leitura das histórias).
- Processamento das histórias utilizando Inteligência Artificial.
- Geração automática de Release Notes em linguagem voltada ao cliente.
- Organização automática das informações por categorias.
- Interface simples para visualização e revisão do conteúdo gerado.
- Código-fonte completo do projeto.
- Documentação técnica da solução.
- Documentação funcional.
- Guia para instalação e execução.

### Entregáveis opcionais (se houver tempo)

- Exportação em HTML, Markdown e PDF.
- Integração com Microsoft Teams ou e-mail.
- Configuração de diferentes prompts para diferentes públicos.
- Dashboard com métricas de qualidade da geração.

## Stack

- **Backend**: .NET 10, ASP.NET Minimal API, arquitetura hexagonal (Ports & Adapters).
- **Persistência**: PostgreSQL 16 + EF Core 10 (Code-First).
- **IA**: OpenRouter como provider de LLM (gateway único, formato `provedor/modelo`).
- **Frontend**: React + Vite.
- **Infra**: Docker + Docker Compose.

Detalhes de arquitetura, camadas e decisões técnicas em [AGENTS.md](AGENTS.md).

## Escopo deste repositório

Backend é a frente principal, com frontend de apoio para a interface de revisão humana antes
da publicação dos comunicados.

## Estrutura

```
src/
├── InvoiSys.Domain/          # entidades, enums, portas — zero dependência de infra
├── InvoiSys.Application/     # orquestração de casos de uso (pipeline de IA)
├── InvoiSys.Infrastructure/  # adapters concretos (Jira, LLM, EF Core)
└── InvoiSys.Api/             # ASP.NET Minimal API

tests-dotnet/InvoiSys.Tests/  # xUnit — unit + integração
frontend/                     # React + Vite
prompts/                      # prompts do pipeline, versionados como arquivo
docs/                         # modelagem de domínio + ERD + contratos de integração
```

## Rodando o projeto

Backend:

```bash
dotnet restore InvoiSys.slnx
dotnet build InvoiSys.slnx
dotnet test InvoiSys.slnx
dotnet run --project src/InvoiSys.Api
```

Frontend:

```bash
cd frontend
npm ci
npm run dev
```

Tudo junto via Docker: `docker compose up --build` (API em `:8080`, Postgres em `:5432`).

## Contribuindo

Veja [CONTRIBUTING.md](CONTRIBUTING.md) para o fluxo de branches/PR e [AGENTS.md](AGENTS.md)
para as decisões de arquitetura e os invariantes de negócio.
