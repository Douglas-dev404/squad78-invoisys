# Frontend — InvoiSys

React 19, Vite 8 e Tailwind CSS 3. O lint usa oxlint. Instale com o lockfile existente:

```bash
cd frontend
npm ci
npm run dev
npm run lint -- --deny-warnings
npm run build
```

O servidor de desenvolvimento abre em `http://localhost:5173`. O CI usa Node 22;
para reproduzir o ambiente do CI, prefira essa versão. Não execute `npm audit fix`
como parte do acabamento visual: atualização de dependências é uma mudança separada.

## Telas e navegação nesta versão

| Tela | Como acessar | Dados |
|---|---|---|
| Login | Inicialização | Autenticação simulada |
| Recuperação de senha | Esqueci minha senha | Serviço simulado |
| Confirmação de envio | Enviar um e-mail válido no formulário | Serviço simulado |
| Painel | Entrar no login | Indicadores, gráfico e busca simulados |
| Histórico de envios | Menu Histórico de envios | Prévia com filtros e paginação locais |
| Usuários | Menu Usuários | Lista e edição simuladas em memória |
| Novo usuário | Usuários → Novo usuário | Cadastro simulado em memória |
| Perfil | Botão Meu perfil, na barra superior | Dados simulados |
| Releases, registros e configurações | Menu lateral | Aviso de funcionalidade pendente |

O `App.jsx` ainda controla telas por estado local. **Esta versão não tem rotas reais**:
abrir `/perfil` ou atualizar o navegador volta ao login. O roteamento pertence à #65.
A tabela a seguir descreve os caminhos planejados, não implementados nesta entrega:

| Caminho planejado | Tela/dependência |
|---|---|
| `/login`, `/esqueci-senha`, `/email-enviado` | Roteamento #65 e sessão #66 |
| `/redefinir-senha` | #73 |
| `/dashboard`, `/historico`, `/usuarios`, `/usuarios/novo`, `/perfil` | #65 e integrações correspondentes |
| `/releases` | #67 |
| `/releases/:chave` | #69 |
| Caminho desconhecido → 404 | #65 |

## Configuração da API

Copie `.env.example` para `.env` dentro de `frontend/` e configure:

```dotenv
VITE_API_BASE_URL=http://localhost:8080/api/v1
```

Reinicie o Vite após alterar variáveis. A configuração é lida em
`src/config/api.config.js`. **Alterar essa URL não ativa integrações**: nesta base,
os serviços ainda retornam mocks. As issues #64 e #66 substituem essa fundação pelo
cliente HTTP e autenticação reais. Os contratos definidos pelo backend têm precedência
sobre os formatos antigos dos mocks. Não envie credenciais no repositório.

## Organização e manutenção

- Páginas compõem componentes e validação; serviços concentram o acesso aos dados.
- `useAsyncResource` mantém carregamento, resultado, erro e nova tentativa. O efeito
  só atualiza estado após a resposta e ignora respostas de componentes desmontados.
- A busca do painel filtra o conjunto simulado carregado. Isso evita respostas de buscas
  antigas sobrescreverem resultados atuais. A API real será integrada na #77.
- Permissões em edição são iniciadas ao selecionar um usuário, não em efeito.
- Mocks ainda consumidos por telas foram preservados. `MOCK_BRAND_INFO` e sua função
  exclusiva sem consumidores foram removidos. O histórico continua sendo uma prévia.
- Datas válidas usam `dd/mm/aaaa HH:mm`, no fuso do navegador; números usam `pt-BR`.
- O gráfico aceita lista vazia e valores zero sem dividir por zero.

## Acessibilidade e responsividade

A navegação lateral recolhe abaixo de 768 px. O botão da barra superior abre o menu;
Esc fecha e devolve o foco ao botão. Em telas maiores, o menu mantém a opção de minimizar.
O link “Ir para o conteúdo” fica visível ao receber foco por teclado. O foco é destacado
em controles, e a preferência por movimento reduzido é respeitada.

As tabelas rolam dentro de uma região identificada e focável, sem ampliar a página.
O painel de usuário usa `<dialog>` nativo: foco contido, fechamento por Esc e retorno
ao controle que o abriu. Campos e ações de alteração ficam bloqueados durante o envio.
Os textos dos selos continuam identificando status independentemente da cor.

## Validação da issue #81

Ver `docs/issue81-validacao.md` na raiz do repositório para a matriz de cobertura,
evidências e pendências. A revisão final da #81 depende das telas e integrações que
não estão presentes nesta base. Esta entrega não deve fechar a issue automaticamente.

Revisão manual:

1. Rodar login → recuperação → confirmação e reenvio → voltar ao login.
2. Entrar com o usuário de demonstração já definido em `auth.service.js`.
3. Abrir painel, buscar um termo inexistente e limpar a busca.
4. Percorrer histórico e testar filtros com e sem resultado.
5. Abrir Usuários, selecionar uma pessoa, alterar permissões, salvar, fechar e reabrir.
6. No diálogo, percorrer Tab/Shift+Tab e Esc; confirmar restauração do foco.
7. Abrir Novo usuário; testar campos vazios, envio e e-mail duplicado.
8. Abrir Perfil e voltar; usar o menu móvel com teclado.
9. Repetir em 360, 768 e 1280 px, verificando rolagem horizontal e legibilidade.
10. Após integração das dependências, repetir com a API real, incluindo falha de rede,
    estados vazios e novas tentativas. Testes contra mocks não comprovam integração.

### Recursos visuais locais

As mesmas fontes Inter e Material Symbols usadas pela interface estão em `public/fonts`,
com suas licenças (SIL Open Font License 1.1). Nenhum pacote foi adicionado
ao projeto. Isso evita que indisponibilidade do Google Fonts transforme ícones em palavras
e quebre o cabeçalho móvel. A marca nas telas de recuperação usa o mesmo símbolo do login;
os avatares ilustrativos usam iniciais, sem depender de URLs temporárias externas.
