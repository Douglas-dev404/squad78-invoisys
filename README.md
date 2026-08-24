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

Em aberto — a InvoiSys usa principalmente .NET na plataforma deles, mas o desafio deixa a
escolha de tecnologias livre para o squad.

## Escopo deste repositório

Foco no **backend**. Frontend é apoio pontual quando o squad precisar, não a frente principal
de trabalho aqui.

## Estrutura

_A definir — decisão em andamento com o squad._
