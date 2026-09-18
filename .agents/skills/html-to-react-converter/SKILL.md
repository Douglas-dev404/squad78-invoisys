---
name: html-to-react-converter
description: >-
  Converte código HTML/CSS/JS bruto, protótipos ou templates em aplicações React (JavaScript puro .jsx, sem TypeScript)
  seguindo arquitetura limpa em camadas, Tailwind CSS com tokens extraídos, componentes "burros" acessíveis e desacoplados,
  camada de serviço assíncrona com mocks realistas e preparação total para integração com backend (com // TODOs explicativos).
  Ative esta skill sempre que o usuário fornecer código HTML/CSS/JS para converter em React, pedir para componentizar um layout
  ou solicitar a criação de telas React prontas para receber backend a partir de designs ou snippets HTML.
---

# HTML to React Converter (Clean Architecture & Backend-Ready)

Esta skill orienta o processo de conversão de qualquer layout, template ou código HTML/CSS/JS em uma aplicação React modular, profissional, acessível e 100% preparada para receber APIs e bancos de dados reais sem necessidade de refatoração futura.

---

## 1. Princípios Arquiteturais e Requisitos Técnicos

- **React 18+ & Hooks**: Exclusivamente function components e hooks nativos (`useState`, `useEffect`, `useCallback`, etc.). Sem class components.
- **JavaScript Puro (.jsx)**: Sem TypeScript.
- **Estilização**: Tailwind CSS (extraindo e espelhando fielmente cores, tipografias, espaçamentos e fontes do HTML original em `tailwind.config.js`).
- **Sem Dependências Supérfluas**: Apenas bibliotecas estritamente necessárias (Vite, React, Tailwind, Autoprefixer, PostCSS).
- **Separação Rígida (Smart vs. Dumb)**:
  - **Componentes de UI ("Burros")**: Recebem tudo via `props` (dados, estados visuais, callbacks). Não possuem chamadas de rede, `fetch` ou lógica de negócio interna.
  - **Containers / Páginas ("Smart")**: Consomem custom hooks, orquestram o estado da tela e repassam dados aos componentes de UI.
- **Sem HTML Hardcoded**: Textos variáveis, itens de listas e estados dinâmicos vêm de props ou de serviços mockados.
- **Chaves Estáveis**: Renderização de listas sempre via `.map()` utilizando chaves únicas e estáveis (`key={item.id}`), **nunca** o índice do array.
- **Tratamento dos 3 Estados Obrigatórios**: Todo consumo de dados assíncronos trata e renderiza:
  1. **Carregando** (skeleton screens ou spinners acessíveis);
  2. **Erro** (mensagens acessíveis com ação de nova tentativa / retry);
  3. **Lista Vazia** (estado amigável quando não houver registros).

---

## 2. Estrutura Padrão de Pastas

Toda conversão deve organizar o código na seguinte estrutura dentro de `src/`:

```
src/
├── components/   # UI pura e reutilizável (Header, Button, InputField, Card, Alert, etc.)
├── pages/        # Composição e orquestração de telas (ex: LoginPage, DashboardPage)
├── hooks/        # Custom hooks expondo { data, loading, error, ... }
├── services/     # Funções assíncronas com Promises e assinatura de API final
├── data/         # Mocks realistas com id, timestamps (createdAt), status, etc.
├── config/       # Variáveis de ambiente (API_BASE_URL) e catálogo de rotas/endpoints
└── utils/        # Constantes de texto da UI, helpers e validadores
```

---

## 3. Workflow de Conversão Passo a Passo

### Passo 1: Análise do HTML Original
1. **Identificação de Design Tokens**:
   - Identifique cores de fundo, texto, primárias, bordas, fontes (ex: Google Fonts) e ícones (Material Symbols, Lucide, FontAwesome).
   - Configure o `tailwind.config.js` mapeando essas variáveis no `theme.extend`.
2. **Decomposição Semântica**:
   - Mapeie `<header>`, `<nav>`, `<main>`, `<aside>`, `<section>`, `<footer>`, `<form>`.
   - Identifique elementos repetidos para extração em componentes de lista (ex: cards, itens de tabela, destaques).

### Passo 2: Configuração e Variáveis de Ambiente
1. Crie `src/config/api.config.js`:
   - Centralize `API_BASE_URL = import.meta.env.VITE_API_BASE_URL || '<fallback_url>'`.
   - Mapeie o objeto `ENDPOINTS` com as URLs dos recursos da tela.
2. Forneça o arquivo `.env.example` com as variáveis necessárias.

### Passo 3: Criação de Mocks e Serviços Assíncronos
1. **Mocks (`src/data/*.mock.js`)**:
   - Exporte dados simulados com estrutura idêntica à que a API real retornará (`id`, `createdAt`, `updatedAt`, `status`, etc.).
2. **Serviços (`src/services/*.service.js`)**:
   - Crie funções assíncronas (`async/await`) que retornam Promises com simulação de latência de rede (`setTimeout`).
   - Insira comentários `// TODO:` explicativos com a chamada real via `fetch` ou `axios` pronta para ser descomentada no futuro.

### Passo 4: Custom Hooks com Gestão de Estados
1. Crie hooks em `src/hooks/use[Recurso].js`.
2. O hook deve encapsular `useState` para:
   - `data` (ou entidade específica)
   - `loading` (booleano)
   - `error` (string | null)
3. Exponha callbacks de ação (ex: `refetch`, `login`, `createItem`) e limpeza de erro (`clearError`).

### Passo 5: Construção dos Componentes de UI ("Burros")
1. Crie cada componente em sua respectiva pasta dentro de `src/components/[NomeComponente]/[NomeComponente].jsx`.
2. Garanta acessibilidade total:
   - Inputs com `id`, `name`, `autoComplete`, `htmlFor` no `<label>`, `aria-invalid` e `aria-describedby`.
   - Botões com `type="button|submit"`, `aria-busy` durante loading e spinner SVG acessível.
   - Alertas com `role="alert"`.
   - Ícones com `aria-hidden="true"`.
3. Não insira fetch, hooks de negócio ou manipulação de DOM (`document.querySelector`) dentro de componentes de UI.

### Passo 6: Composição da Página
1. Crie a página em `src/pages/[NomePagina]/[NomePagina].jsx`.
2. Conecte os custom hooks para obter os dados e handlers.
3. Repasse tudo aos componentes de UI via props.
4. Adicione comentários `// TODO:` onde houver navegação ou integração futura (ex: redirecionar com React Router).

### Passo 7: Validação e Entrega
1. Execute `npm run build` para garantir que não há erros de sintaxe ou imports quebrados.
2. Valide no navegador se o design reflete fielmente o HTML original e se os 3 estados (loading, erro, vazio) funcionam.

---

## 4. Formato de Entrega Padrão

Sempre estruture a resposta final para o usuário contendo:
1. **Árvore de arquivos completa** do projeto.
2. **Código completo e funcional de cada arquivo** (sem omissões, snippets cortados ou reticências).
3. **Comentários `// TODO`** em todos os pontos de integração com o backend (serviços, hooks, navegação).
4. **Resumo das decisões de componentização** explicando o desacoplamento e a arquitetura adotada.
