# Issue #81 — acabamento das telas disponíveis

Data: 05/11/2026. Base: arquivo `squad78-issue81.zip` fornecido por Lucca,
exportado da branch `feature/81-ui-ux-acessibilidade` (commit informado: `42f22d7`).

## Resultado e limites

Esta entrega corrige o front existente nessa base. Não conclui a #81 inteira: faltam
as telas de Releases, detalhe da Release, redefinição de senha e 404, além das integrações
reais. A issue declara dependência de todas as entregas anteriores do front.
Não usar `Closes #81` no PR desta etapa.

Nenhuma regra de aprovação, categoria de comunicado, integração Jira/IA, código C#,
persistência ou contrato do backend foi alterado. Publicação continua sujeita à revisão
humana do domínio. Os serviços simulados existentes continuam simulados.

## Alterações por área

| Área | Correção |
|---|---|
| Estrutura das telas | Menu móvel recolhível, conteúdo sem margem lateral em celular, região principal focável e atalho para o conteúdo |
| Navegação | Rótulos em português, nome acessível nos links recolhidos, título do navegador atualizado e posição de rolagem reiniciada ao trocar de tela |
| Barra superior | Controles compactos; ação de perfil identificada; ações sem implementação não aparentam funcionar |
| Painel | Indicadores com carregamento e valores em pt-BR; tabela com rolagem interna; itens sem destino implementado deixam de aparentar links funcionais |
| Gráfico | Largura adaptável, barras com altura correta e proteção para lista vazia ou máximo zero; valores acessíveis por dia |
| Usuários | Lista adaptável; painel de largura fixa substituído por diálogo com foco contido, Esc e restauração do foco; alterações bloqueadas durante envio |
| Novo usuário | Permissões também desabilitadas durante envio, indicador de progresso e preservação das validações e do cadastro em memória |
| Perfil | Dados com quebra de texto, estado vazio, datas padronizadas e avatar ilustrativo por iniciais |
| Histórico | Tabela focável com rolagem interna; datas numéricas; cabeçalho que quebra em telas estreitas; filtros existentes preservados |
| Recuperação e confirmação | Marca local, endereço solicitado visível, retorno acessível no reenvio e progresso; erros tratados na própria tela |
| Recursos visuais | Mesmas fontes servidas localmente, com licença SIL OFL 1.1; sem novos pacotes no projeto e sem alteração no lockfile |
| Hooks | Carregamento assíncrono com descarte de respostas após desmontagem; permissões iniciais definidas ao selecionar o usuário, sem sincronização de estado em efeito |
| Limpeza | Removidos imports, operação de busca simulada redundante, informações de marca e credencial de exemplo sem consumidores; mocks ativos preservados |

## Matriz tela × checklist

`OK local` indica verificação em navegador com os serviços simulados desta versão.
Não significa comprovação com a API real. `N/A` é ausência de uma operação de leitura
ou de um estado aplicável nessa tela.

| Tela | Carregando/vazio/erro | Textos e formato | 360/768/1280 px | Teclado e identificação | API real |
|---|---|---|---|---|---|
| Login | Formulário e validação local; destaques mantêm os estados existentes | OK local | OK local | OK local | Pendente #64/#66 |
| Recuperar senha | Envio e validação local | OK local | OK local | OK local | Pendente #52/#66 |
| Confirmar envio | Reenvio e confirmação acessível | OK local | OK local | OK local | Pendente #52/#66 |
| Painel | OK local; erro e vazio induzidos no teste | OK local | OK local | OK local | Pendente #74/#77 |
| Histórico | Dados e vazio por filtro; carregamento/erro remotos não existem na prévia | OK local | OK local | OK local | Pendente #75/#79 |
| Usuários | OK local; erro e vazio induzidos no teste | OK local | OK local | OK local, incluindo diálogo | Pendente #78 |
| Novo usuário | Envio, campos obrigatórios e e-mail duplicado; vazio N/A | OK local | OK local | OK local | Pendente #78 |
| Perfil | OK local; erro e vazio induzidos no teste | OK local | OK local | OK local | Pendente #78 |
| Telas de aviso já existentes | N/A | OK no código | Estrutura compartilhada responsiva | Estrutura compartilhada | Não são as funcionalidades finais |
| Redefinir senha | Não existe | Pendente | Pendente | Pendente | #73 |
| Releases | Só aviso, sem lista funcional | Pendente | Pendente | Pendente | #67 |
| Detalhe da Release | Não existe | Pendente | Pendente | Pendente | #69–#72 |
| 404 | Não existe roteamento | Pendente | Pendente | Pendente | #65 |

## Verificações executadas

- Lint estrito: `npm run lint -- --deny-warnings`, sem avisos e sem erros. A base apresentava 11 avisos.
- Build: `npm run build`, concluído sem erro.
- `git diff --check`: sem problemas de whitespace.
- Navegador Chromium: fluxos de login, recuperação, reenvio, painel, busca vazia,
  filtros vazios do histórico, usuários, gravação de permissões, novo usuário e perfil.
- Nas três larguras: ausência de rolagem horizontal na página, campos com nome
  acessível, abertura/fechamento do diálogo, circulação de foco, Esc e retorno ao
  controle que abriu o diálogo. Tabelas podem rolar dentro da própria região.
- Auditoria automatizada de acessibilidade com axe-core, regras WCAG 2 A/AA e
  WCAG 2.1 A/AA, nas nove telas/estados capturados em cada largura: nenhuma violação
  reportada na rodada final. Isso não substitui avaliação manual com leitor de tela.
- Erro → tentar de novo → carregando → vazio exercitado em painel, usuários e perfil
  por respostas induzidas **somente na sessão de teste**, sem adicionar modo de teste ao produto.
- Cadastro com e-mail novo e duplicado; bloqueio dos campos e ações durante envio.
- Menu móvel: conteúdo coberto não recebe foco; Esc fecha; redimensionar para desktop
  libera o conteúdo.
- Formatação: datas válidas, ausentes e inválidas; número 1234 exibido como 1.234.
- Os arquivos JSON da pasta de evidências preservam os resultados e as contagens de cada rodada final.

## Evidências

O pacote de entrega contém `evidencias/` com:

- Antes/depois em 360 px das três telas mais alteradas: painel, usuários e perfil.
- Prints das telas disponíveis em 360, 768 e 1280 px.
- Prints de erro, carregando e vazio induzidos em painel, usuários e perfil.
- Resultados estruturados em `verificacao.json` e `verificacao-estados.json`.

Na captura do estado anterior, as fontes equivalentes foram servidas localmente para
que a restrição de rede do ambiente de teste não interferisse na comparação de layout.
Os serviços de dados foram os originais da base, exceto nas evidências explicitamente
nomeadas como estados induzidos. Na versão final, fontes e ícones são recursos locais.

## O que não foi verificado

- API real, autenticação e autorização reais, falha de rede real, persistência ou e-mail.
- Telas ausentes e fluxos de revisão/aprovação/exportação, pois não existem no arquivo recebido.
- Testes e formatação .NET: SDK .NET indisponível neste ambiente; nenhum C# foi alterado.
- Leitor de tela, navegadores diferentes do Chromium e dispositivos físicos.
- Ambiente Windows do usuário após aplicação do patch e CI remoto do GitHub.

Repetir a matriz com a API de pé e as novas telas integradas antes de fechar a #81.
