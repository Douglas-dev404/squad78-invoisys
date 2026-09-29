# Frontend — InvoiSys

Interface web onde o revisor vai **ver, editar, aprovar e exportar** os comunicados
gerados pela IA. Stack: **React 19 + Vite 8 + Tailwind CSS 3**, lint com **oxlint**.

> **Estado atual:** telas de **login**, **recuperação de senha** e **confirmação de
> envio**, ainda sem integração com o backend (serviços com mock e latência simulada).
> A **tela de revisão**, requisito obrigatório do enunciado, ainda não existe (ver
> [roadmap](#o-que-falta-até-a-entrega)).

```bash
npm ci
npm run dev       # http://localhost:5173
npm run lint      # oxlint (o CI roda)
npm run build     # build de produção (o CI roda)
```

Configuração: copie `.env.example` → `.env`.

| Variável | Padrão | Para quê |
|---|---|---|
| `VITE_API_BASE_URL` | `http://localhost:8080/api/v1` | base de todas as chamadas à API |

---

## Arquitetura em camadas

O front segue uma separação estrita, para que trocar mock por API real mexa em **um
arquivo por domínio**:

```
pages/  ──►  hooks/  ──►  services/  ──►  config/api.config.js ──► backend
  │            │              └── hoje: data/*.mock.js (latência simulada)
  └──► components/ (visuais, sem estado de servidor)
```

| Camada | Pasta | Responsabilidade | Exemplo |
|---|---|---|---|
| **Página** | [`src/pages/`](src/pages/) | Orquestra hooks + componentes + validação de formulário. Uma por tela | `LoginPage` usa `useAuth`, `useHeroHighlights`, `validateLoginForm` |
| **Hook** | [`src/hooks/`](src/hooks/) | Estado de servidor: expõe `{ data, loading, error, ...ações }` | `useAuth` → `{ user, loading, error, login, logout, clearError }` |
| **Serviço** | [`src/services/`](src/services/) | Única camada que "fala com a API". **Assinaturas já definitivas**; o corpo hoje é mock, com o `fetch` real comentado como `TODO` | `auth.service.js`: `login`, `requestPasswordReset`, `resendPasswordReset`, `logout`, `getCurrentUser` |
| **Config** | [`src/config/api.config.js`](src/config/api.config.js) | `API_BASE_URL`, timeout e mapa `ENDPOINTS` | `ENDPOINTS.AUTH.LOGIN` |
| **Componente** | [`src/components/`](src/components/) | Visual e reutilizável, sem chamada de rede | `Button`, `InputField`, `Alert`, `LoginForm`, `HeroSection` |
| **Mock** | [`src/data/`](src/data/) | Formato de resposta que o backend deve devolver | `auth.mock.js`, `branding.mock.js` |
| **Utils** | [`src/utils/`](src/utils/) | Constantes de UI (`UI_STRINGS`) e validadores | `isValidEmail`, `validateLoginForm` |

### Fluxo de telas atual

```
LoginPage ──"Esqueci minha senha"──► ForgotPasswordPage ──enviar──► EmailConfirmationPage
    ▲                                        │                              │
    └────────────── voltar ──────────────────┴──────────── voltar ──────────┘
```

Navegação hoje é por estado local em [`App.jsx`](src/App.jsx) (`currentScreen`); há
`TODO` para trocar por `react-router-dom`.

### Contrato que o front já espera do backend

| Endpoint (em `api.config.js`) | Usado por | Existe no backend? |
|---|---|---|
| `POST /auth/login` → `{ user, token }` | `auth.service.login` | ❌ Fase 3 |
| `GET /auth/me` | `getCurrentUser` | ❌ Fase 3 |
| `POST /auth/logout` | `logout` | ❌ Fase 3 |
| `POST /auth/forgot-password` → `{ message, expiresInMinutes }` | `requestPasswordReset` | ❌ Fase 3 |
| `POST /auth/resend-forgot-password` | `resendPasswordReset` | ❌ Fase 3 |
| `GET /branding/highlights` | `branding.service.getHeroHighlights` | ❌ entidade + repository prontos, falta endpoint |

O formato exato esperado está nos mocks de [`src/data/`](src/data/).

---

## O que falta até a entrega

| # | Item | Depende de |
|---|---|---|
| 1 | **CORS** no backend (sem isso o browser bloqueia `:5173` → `:8080`) | backend, Fase 3 |
| 2 | Trocar mocks de `auth.service`/`branding.service` por `fetch` real; guardar token (preferir cookie HttpOnly) | endpoints `/auth/*`, `/branding/highlights` |
| 3 | `react-router-dom` + rota protegida | — |
| 4 | **Lista de Releases** (`GET /releases`) + botão "processar" (`POST /releases/{chave}/processar`) | #21 |
| 5 | **Tela de revisão**: versão por público, itens agrupados por categoria, edição inline (`TextoFinal`), excluir/reincluir com motivo, aprovar/reprovar (motivo obrigatório)/reabrir | #22, #23 |
| 6 | Exportar Markdown (download/preview) | #24 |
| 7 | Dashboard e usuários: **PR #9** (precisa ser refeito sobre a `develop` atual) | — |
| 8 | Histórico de envios: **PR #35** (draft, depende do #9) | #24 |
| 9 | Testes (Vitest + Testing Library; Playwright no fluxo de revisão) | — |

### Convenções para telas novas

- Toda chamada de rede num `*.service.js`, com o endpoint em `api.config.js`.
- Todo estado de servidor num hook `use*` que expõe `data/loading/error`.
- Toda tela trata os quatro estados: **carregando, erro (com "tentar novamente"), vazio,
  sucesso**.
- Textos de UI em `UI_STRINGS`, não literais espalhados.
- Categorias e status chegam da API como `snake_case` (`nova_funcionalidade`,
  `aguardando_revisao`). Mapeie para rótulo de exibição num lugar só.
