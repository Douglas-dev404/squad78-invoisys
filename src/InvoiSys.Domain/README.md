# InvoiSys.Domain — o hexágono

O centro do sistema. Aqui vivem as **regras de negócio** e os **contratos** (portas) que
o resto do sistema implementa. Este projeto **não referencia nenhum outro**, nem
ASP.NET, EF Core ou `HttpClient`. Se você precisou de um `using` de infraestrutura aqui,
o desenho está errado (ver [ADR-001](../../docs/decisoes-arquiteturais.md#adr-001--arquitetura-hexagonal-ports--adapters)).

```
InvoiSys.Domain/
├── Entities/   # entidades ricas: estado + invariantes + ciclo de vida
├── Enums/      # vocabulário fixo de negócio, com valor serializado (ParaValor)
└── Ports/      # interfaces que a infraestrutura implementa + contrato de erro
```

---

## Entidades

### Agregado `Release` (raiz)

```mermaid
classDiagram
    direction LR
    Release "1" *-- "N" HistoriaJira : _historias
    Release "1" *-- "0..4" VersaoComunicado : _versoes (1 por público)
    Release "1" *-- "N" ExecucaoPipeline : _execucoes
    VersaoComunicado "1" *-- "N" ItemComunicado : _itens
    ComunicadoExportado ..> VersaoComunicado : nasce de versão aprovada

    class Release {
      Guid Id
      string ChaveJira
      StatusPipeline Status
      MarcarProcessando()
      RegistrarExecucao(modelo) ExecucaoPipeline
      ConcluirProcessamento(itens, titulo, resumo, publico) VersaoComunicado
      MarcarFalha()
      Aprovar(por, agora, publico)
      Reprovar(por, motivo, agora, publico)
      Reabrir(publico)
    }
    class VersaoComunicado {
      PublicoAlvo Publico
      StatusRevisao Status
      string TituloExecutivo
      string ResumoExecutivo
      ItensPublicaveis
      bool ProntaParaExportar
      PreencherConteudo()
      Aprovar() Reprovar() Reabrir()
    }
    class ItemComunicado {
      CategoriaAlteracao Categoria
      string Texto
      string[] Origens
      string TextoFinal
      bool Incluido
      EditarManualmente() Excluir() Reincluir()
    }
```

| Entidade | Papel | Métodos que importam |
|---|---|---|
| [`Release`](Entities/Release.cs) | Raiz do agregado. Dona do ciclo de vida da **geração** (`StatusPipeline`) e porta de entrada para toda mutação de versão/item | `ConcluirProcessamento`, `Aprovar`, `Reprovar`, `Reabrir`, `EditarItem`, `ExcluirItem`, `ReincluirItem` (acham o item em qualquer versão pelo id), `PublicoDoItem`, `RegistrarExecucao`, `VersaoPara(publico)`. Atalhos da versão Cliente: `Itens`, `TituloExecutivo`, `ResumoExecutivo`, `ProntaParaExportar` |
| [`HistoriaJira`](Entities/HistoriaJira.cs) | Dado bruto do Jira. `record` **imutável**: o pipeline gera cópias limpas com `with`, nunca muta | `TextoFonte` (Release Note dedicada **ou** descrição técnica), `PossuiReleaseNoteDedicada` |
| [`VersaoComunicado`](Entities/VersaoComunicado.cs) | O comunicado **para um público**, com revisão própria (`StatusRevisao`). Responde "pode exportar?" | `Aprovar`, `Reprovar(motivo)`, `Reabrir`, `PreencherConteudo`, `EditarItem`/`ExcluirItem`/`ReincluirItem` (recusam versão aprovada, [ADR-021](../../docs/decisoes-arquiteturais.md#adr-021--item-de-versão-aprovada-não-é-editado-sem-reabrir)), `ItensPublicaveis`, `ProntaParaExportar` |
| [`ItemComunicado`](Entities/ItemComunicado.cs) | Um parágrafo do comunicado, originado de 1+ histórias | `TextoFinal` (edição humana vence), `Origens`. `EditarManualmente`, `Excluir(motivo)` e `Reincluir` são `internal`: de fora do domínio, só pela `Release` |
| [`ExecucaoPipeline`](Entities/ExecucaoPipeline.cs) | Log de uma rodada do pipeline (rastreabilidade) | `MarcarConcluida`, `MarcarFalha(erro)` |

### Agregados independentes

| Entidade | Papel |
|---|---|
| [`ComunicadoExportado`](Entities/ComunicadoExportado.cs) | Registro de publicação (append-only). **O construtor lança `ReleaseNaoAprovadaException` se a versão não estiver `ProntaParaExportar`** |
| [`Usuario`](Entities/Usuario.cs) | Quem faz login, revisa e aprova. `Papel` é texto livre até o RBAC existir. Desligar = `Desativar()`, nunca DELETE |
| [`DestaqueHero`](Entities/DestaqueHero.cs) | Conteúdo institucional da tela de login (`GET /branding/highlights`). Sem relação com o pipeline |

### Exceções de regra de negócio

| Exceção | Lançada quando |
|---|---|
| `RevisaoHumanaObrigatoriaException` | aprovar Release com pipeline em `falhou`, transição de revisão inválida, ou reprocessar versão já aprovada sem reabrir (`GarantirQuePodeReprocessar`, [ADR-020](../../docs/decisoes-arquiteturais.md#adr-020--versão-aprovada-não-é-reprocessada-sem-reabrir)) |
| `ReleaseSemItensProcessadosException` | aprovar/reprovar público sem versão gerada, ou versão sem itens |
| `VersaoSemItensException` | (interna à versão) aprovar sem itens ou com todos excluídos |
| `TransicaoDeStatusInvalidaException` | (interna à versão) ex.: reaprovar o que já foi aprovado, ou alterar item de versão aprovada |
| `ItemNaoEncontradoException` | editar/excluir/reincluir um item que não existe na Release |
| `ReleaseNaoAprovadaException` | construir `ComunicadoExportado` de versão não aprovada |
| `ReleaseNaoEncontradaException` | caso de uso aponta para uma chave de Release que não foi persistida |
| `ArgumentException` | reprovar sem motivo, aprovar/reprovar sem identificar o revisor (trilha de auditoria da revisão humana), ou editar item com texto vazio |

> Mapeamento para HTTP das exceções de revisão: ver `MapearFalhaDeRevisao` e `MapearFalhaDeRevisaoDeItem` no
> [README da API](../InvoiSys.Api/README.md#tradução-de-erro--http).

---

## Enums

Todos têm um **valor serializado** separado do nome do membro (`ParaValor()`). Assim o C#
fica em PascalCase e a API e o banco falam `snake_case`, como no contrato herdado do
Python.

| Enum | Valores | Onde |
|---|---|---|
| [`CategoriaAlteracao`](Enums/CategoriaAlteracao.cs) | `nova_funcionalidade` · `melhoria` · `correcao` · `outros`. **Fixos** | item |
| [`PublicoAlvo`](Enums/PublicoAlvo.cs) | `cliente` · `comercial` · `suporte` · `interno`. **Fixos** | versão, exportação |
| [`StatusPipeline`](Enums/StatusPipeline.cs) | `pendente` · `processando` · `aguardando_revisao` · `aprovado` · `falhou` | Release (geração) |
| [`StatusRevisao`](Enums/StatusRevisao.cs) | `aguardando_revisao` · `aprovado` · `reprovado` | versão (revisão) |
| [`StatusExecucaoPipeline`](Enums/StatusExecucaoPipeline.cs) | `iniciada` · `concluida` · `falhou` | execução |
| [`FormatoExportacao`](Enums/FormatoExportacao.cs) | `markdown` · `html` · `pdf` | exportação |

`CategoriaAlteracaoExtensions.TentarConverter` é case-insensitive de propósito, porque a
entrada é resposta de LLM. `StatusPipelineExtensions.TentarConverter` (filtro `?status=` da listagem) e
`PublicoAlvoExtensions.TentarConverter` também, e **não têm
default**: valor ausente não converte (na revisão, assumir Cliente aprovaria o público
errado em silêncio).

---

## Portas

| Porta | Tipo | Implementada por | Para quê |
|---|---|---|---|
| [`IJiraClient`](Ports/IJiraClient.cs) | saída (integração) | `JiraRestClient` / `JiraClientPendente` | `BuscarHistoriasDaReleaseAsync(fixVersion)`: todas as issues, paginação resolvida por dentro |
| [`ILlmProvider`](Ports/ILlmProvider.cs) | saída (integração) | `OpenRouterProvider` / `ProviderPendente` | um método por estágio de IA: `CategorizarAsync`, `AgruparSemelhantesAsync`, `ReescreverLinguagemNegocioAsync`, `GerarTituloEResumoAsync` |
| [`IReleaseRepository`](Ports/IReleaseRepository.cs) | persistência (agregado) | `ReleaseRepository` | `BuscarPorIdAsync`, `BuscarPorChaveJiraAsync` (agregado completo), `SalvarAsync`, `ListarResumosAsync(status?)` (devolve o modelo de leitura `ReleaseResumo`, nunca uma `Release` parcial) |
| [`IHistoriaJiraRepository`](Ports/IHistoriaJiraRepository.cs) | **somente leitura** | `HistoriaJiraRepository` | consultar histórias sem carregar o agregado |
| [`IExecucaoPipelineRepository`](Ports/IExecucaoPipelineRepository.cs) | **somente leitura** | `ExecucaoPipelineRepository` | histórico de execuções (mais recente primeiro) |
| [`IComunicadoExportadoRepository`](Ports/IComunicadoExportadoRepository.cs) | **append-only** | `ComunicadoExportadoRepository` | `AdicionarAsync`, `BuscarPorIdAsync`, `ListarPorReleaseAsync` |
| [`IUsuarioRepository`](Ports/IUsuarioRepository.cs) | persistência (agregado) | `UsuarioRepository` | `BuscarPorIdAsync`, `BuscarPorEmailAsync`, `SalvarAsync` (sem delete) |
| [`IDestaqueHeroRepository`](Ports/IDestaqueHeroRepository.cs) | somente leitura | `DestaqueHeroRepository` | `ListarAtivosAsync` (ordenado) |

**Não existe** porta para `VersaoComunicado` nem `ItemComunicado`, e isso é proposital:
aprovar, editar ou excluir passa pela `Release` carregada ([ADR-009](../../docs/decisoes-arquiteturais.md#adr-009--repository-por-agregado-portas-de-leitura-isoladas)).

### Contrato de erro das integrações

[`Ports/ExcecoesDeIntegracao.cs`](Ports/ExcecoesDeIntegracao.cs). O erro faz parte do
contrato da porta, então mora aqui, não no adapter
([ADR-013](../../docs/decisoes-arquiteturais.md#adr-013--exceções-de-integração-são-contrato-da-porta)):

```
IntegracaoExternaException (abstract) ─► 502
 ├─ JiraApiException
 ├─ LlmApiException
 └─ LlmRespostaInvalidaException
ProviderNaoConfiguradoException ─► 501
```

### Detalhes das portas

- **`null` não é 404 na porta.** Busca que não acha devolve `null` (ou lista vazia), e
  quem chama decide o que isso significa.
- `IJiraClient.BuscarHistoriasDaReleaseAsync` resolve a paginação (`nextPageToken`) por
  dentro e já traz o texto da subtarefa Release Note. Contrato verificado contra a doc
  oficial: [docs/contratos-integracao.md](../../docs/contratos-integracao.md).
- `ILlmProvider` tem **um método por estágio que de fato chama o modelo** (2 a 5). O
  estágio 1 (limpeza) é texto puro e fica na Application. Trocar de provider é escrever
  outro adapter e mudar um binding no composition root, sem tocar no domínio.
- `IHistoriaJiraRepository.BuscarPorChaveAsync` exige o `releaseId`, porque a chave do
  Jira só é única **dentro** de uma Release. As listagens de histórias saem ordenadas
  pela chave, para o resultado ser estável.
- `IExecucaoPipelineRepository` e `IComunicadoExportadoRepository` listam do mais
  recente para o mais antigo, a ordem natural de um log.
- Portas de leitura (`IHistoriaJiraRepository`, `IExecucaoPipelineRepository`) não têm
  escrita: uma porta de escrita deixaria gravar filha do agregado sem passar pela
  `Release`.
- `IUsuarioRepository.BuscarPorEmailAsync` devolve também usuário desativado. Decidir se
  desativado pode entrar é regra da autenticação, não da porta.
- `IDestaqueHeroRepository` é só leitura porque ainda não existe tela nem regra de
  cadastro de destaques.
- Exceções de integração (`ExcecoesDeIntegracao.cs`) têm mensagem **segura para chegar ao
  cliente HTTP**: o corpo da resposta externa vai só para o log do adapter.
  `ProviderNaoConfiguradoException` não é falha externa, é o ambiente sem credencial.

---

## Por que o modelo é assim

**Release**
- Coleções são `IReadOnlyList` e só mudam pelos métodos de ciclo de vida: um `List`
  público deixaria qualquer camada injetar itens sem passar pelo gate de revisão.
- `Status` descreve a **geração** (a IA rodou?). A **revisão** é de cada
  `VersaoComunicado`, porque o Cliente pode estar aprovado enquanto o Suporte ainda está
  em ajuste ([ADR-007](../../docs/decisoes-arquiteturais.md#adr-007--revisão-humana-como-invariante-do-domínio-entidades-ricas),
  [ADR-008](../../docs/decisoes-arquiteturais.md#adr-008--múltiplos-públicos-como-versaocomunicado-geração--revisão)).
- A Release só vira `Aprovado` quando **todas** as versões geradas estão aprovadas.
  Reprovar não é falha técnica: a Release volta a `AguardandoRevisao` e o caminho é
  reprocessar.
- `Execucoes` vivem no agregado para a rastreabilidade ser garantia do domínio, e não da
  aplicação lembrar de gravar o log. Cada rodada vira uma linha, inclusive as que falham.
- `AtualizarHistorias` (reprocessamento) troca as histórias mantendo `Id`, versões e
  execuções. Faz `Clear` + `AddRange` na mesma `List`, porque o EF rastreia aquela
  instância; reatribuir o campo perderia o delete/insert em cascata.
- `GarantirQuePodeReprocessar` roda **antes** de qualquer chamada externa: recusar
  depois de gastar Jira e tokens seria desperdício, e a tentativa recusada não deixa
  rastro (nenhuma execução chegou a rodar).
- Os atalhos `VersaoCliente`, `Itens`, `TituloExecutivo`, `ResumoExecutivo` e
  `ProntaParaExportar` existem porque Cliente é o público obrigatório do MVP. Para os
  outros públicos, use `VersaoPara(publico)`.
- `PublicoDoItem(itemId)` existe porque o item não conhece a própria versão (a FK é
  shadow).

**VersaoComunicado**
- É entidade própria, e não campo do item, porque título, resumo e principalmente a
  **revisão** variam por público.
- `ProntaParaExportar` é o gate único que a exportação checa. `ItensPublicaveis` é o que
  a exportação renderiza, nunca `Itens` cru.
- `PreencherConteudo` não sobrescreve versão aprovada: a IA não desfaz em silêncio uma
  decisão humana.
- `Aprovar` exige itens, ao menos um incluído, status `AguardandoRevisao` e o revisor
  identificado. Sem revisor, a aprovação não serve como trilha de auditoria.
- `EditarItem` recusa texto vazio. Para voltar ao texto da IA, o caminho é reprocessar;
  para tirar o item, é excluir.
- `Reabrir` não apaga `ComunicadoExportado` anteriores: o que já foi publicado
  aconteceu, e auditoria não é reescrita.

**ItemComunicado**
- `Origens` é lista porque um item pode nascer de várias histórias agrupadas (estágio 3).
- `Incluido` nasce `true`: o revisor **exclui** o que não interessa ao público, não
  seleciona o que interessa. É o problema original que o produto resolve.
- Excluir não apaga o registro: o texto da IA e o motivo da exclusão continuam
  auditáveis. `TextoFinal` é a edição humana quando existe; a revisão humana tem a
  última palavra.

**Outras entidades**
- `HistoriaJira` é `record` imutável (o pipeline gera cópias limpas com `with`) e tem um
  `Id` técnico separado da chave do Jira.
- `ComunicadoExportado` tem uma linha por exportação (formato e reexportação), para ter
  histórico completo. `Publico` é redundante de propósito, para facilitar consulta.
  `Conteudo` guarda Markdown/HTML; `CaminhoArquivo`, o binário (PDF). `GeradoPor` é texto
  livre até existir autenticação.
- `Usuario.SenhaHash` guarda só hash (bcrypt/argon2), nunca a senha.
- `DestaqueHero` é persistido porque o frontend consome os destaques de um endpoint,
  esperando conteúdo dinâmico.

**Enums**
- `StatusPipeline` não tem um status por estágio: isso é detalhe de execução do
  orquestrador, não estado relevante para o domínio.
- `StatusRevisao` é separado de `StatusPipeline` porque misturar geração e revisão num
  campo só não representa "Cliente aprovado, Suporte em ajuste".
- `FormatoExportacao`: Markdown é o único obrigatório do MVP.

---

## Regras para quem for mexer aqui

- **Nunca** setar `Status = Aprovado` direto. Aprovação passa por `Aprovar()`.
- Coleções: `IReadOnlyList` público + `List` privado, mutado só por método de ciclo de
  vida.
- Propriedades com `private set`/`private init`. Construtor privado vazio só para o EF.
- `Id = Guid.NewGuid()` no construtor, sempre (e `ValueGeneratedNever()` na
  configuration, [ADR-010](../../docs/decisoes-arquiteturais.md#adr-010--ids-gerados-no-domínio-com-valuegeneratednever)).
- Nomes de negócio em **português** (vocabulário da InvoiSys).
- Testes de referência: `ReleaseInvariantesTests`, `VersaoComunicadoTests`,
  `HistoriaJiraTests`.

## O que ainda falta neste módulo

- Mapear o fluxo de recuperação de senha (token de reset com expiração), se entrar no
  escopo ([P-02](../../docs/decisoes-arquiteturais.md#p-02--desenho-da-autenticação)).
- Porta de renderização da exportação, se a opção escolhida em
  [P-03](../../docs/decisoes-arquiteturais.md#p-03--onde-mora-o-render-da-exportação)
  for essa.
