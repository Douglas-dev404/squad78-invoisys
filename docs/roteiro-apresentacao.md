# Roteiro de apresentação — InvoiSys Release Notes

Roteiro técnico de fala. Cada bloco tem: **o que falar**, **os arquivos a abrir** e
**perguntas prováveis com resposta pronta**.

Duração alvo: 20–25 min de fala + 10 de perguntas.

---

## Antes de começar

Deixe aberto em abas:

1. `src/InvoiSys.Domain/Entities/Release.cs`
2. `src/InvoiSys.Domain/Entities/VersaoComunicado.cs`
3. `src/InvoiSys.Application/Pipeline/PipelineGeracaoReleaseNote.cs`
4. `src/InvoiSys.Api/Endpoints/ReleaseEndpoints.cs`
5. `docs/erd.png`
6. Terminal na raiz do repositório

Deixe rodando antes da reunião (leva ~40s para ficar `healthy`):

```bash
docker compose up -d --build
curl http://localhost:8080/health
```

---

## Bloco 1 — Arquitetura em 4 projetos (3 min)

### Fala

> O backend é .NET 10, dividido em quatro projetos. A divisão não é organizacional, é
> uma regra de dependência: as setas só apontam para dentro.

```
InvoiSys.Api            →  InvoiSys.Application  →  InvoiSys.Domain
InvoiSys.Infrastructure →  InvoiSys.Domain
```

> `InvoiSys.Domain` não referencia nenhum dos outros. Ele não sabe que existe HTTP, que
> existe PostgreSQL, que existe OpenRouter. Isso não é convenção que a gente combina e
> torce para respeitar — é a direção das referências no `.csproj`. Se alguém tentar
> importar infraestrutura no domínio, o projeto não compila.

> O domínio define **portas** — interfaces do que ele precisa. A infraestrutura
> implementa. A amarração acontece num lugar só.

### Arquivos a mostrar

| Arquivo | O que apontar |
|---|---|
| `src/InvoiSys.Domain/Ports/IJiraClient.cs` | 1 método. O domínio pede "me dê as histórias da Release", não sabe como. |
| `src/InvoiSys.Domain/Ports/ILlmProvider.cs` | 4 métodos, um por estágio de IA que chama o modelo. |
| `src/InvoiSys.Infrastructure/DependencyInjection.cs` | O composition root — **único** lugar onde porta vira implementação concreta. |

### Ponto forte a destacar

Em `DependencyInjection.cs`, nos dois fallbacks (`AddJiraClient` e `AddLlmProvider`):

```csharp
return opcoes.EstaConfigurado
    ? provider.GetRequiredService<JiraRestClient>()
    : new JiraClientPendente();
```

> Sem credencial configurada, o sistema não injeta o adapter real e reza. Ele injeta um
> substituto que responde com uma mensagem clara do que falta configurar. É por isso que
> a API devolve **501 com explicação** em vez de **500 opaco** quando o Jira não está
> configurado. Eu posso demonstrar agora.

**Demonstração** (30s):

```bash
curl -s http://localhost:8080/api/v1/releases/RELEASE-2026-08/historias | head -3
```

Resposta esperada: `501`, com `detail` dizendo exatamente quais variáveis faltam.

---

## Bloco 2 — O fluxo, arquivo por arquivo (6 min)

Este é o miolo. Percorra na ordem em que o código executa.

### Passo 1 — Entra pela API

**Arquivo**: `src/InvoiSys.Api/Endpoints/ReleaseEndpoints.cs`

> Duas rotas hoje. `POST /api/v1/releases/{chave}/processar` é a que dispara tudo.
> Repare que o handler inteiro é instanciar o pipeline, chamar, montar o DTO e traduzir
> exceção em status HTTP — metade dele é bloco `catch`. Nenhuma regra de negócio aqui:
> camada fina de propósito.

Aponte o bloco `catch`:

> Falha do Jira ou do modelo vira **502 Bad Gateway**, não 500. É falha de dependência
> externa, não erro nosso — e quem consome a API precisa conseguir distinguir as duas
> coisas para saber se adianta tentar de novo.

### Passo 2 — Busca no Jira

**Arquivo**: `src/InvoiSys.Infrastructure/Jira/JiraRestClient.cs`

Três coisas a mostrar:

1. **O endpoint** (linha 69): `/rest/api/3/search/jql`
   > O endpoint antigo `/rest/api/3/search` foi removido pela Atlassian. Verificamos o
   > contrato atual na documentação oficial antes de escrever.

2. **A paginação** (linhas 56–88): `nextPageToken`
   > Não é mais `startAt`. O cliente trata a paginação internamente — quem chama recebe
   > a lista completa, não precisa saber que houve três páginas.

3. **A segunda chamada** (`BuscarTextoReleaseNoteAsync`, linha 99)
   > O Jira devolve a subtarefa com só id, chave e título. Para pegar o **texto** da
   > subtarefa Release Note, é preciso uma segunda chamada por subtarefa candidata.
   > Custa mais requisições, mas é o que a API permite.

> O retry não está aqui. Fica na política de resiliência registrada no
> `DependencyInjection.cs`: 3 tentativas, backoff exponencial, e 4xx de cliente propaga
> na primeira — tentar de novo um 401 não muda o resultado.

### Passo 3 — O pipeline

**Arquivo**: `src/InvoiSys.Application/Pipeline/PipelineGeracaoReleaseNote.cs`

É o arquivo mais importante de mostrar. 120 linhas, leitura linear.

```csharp
var historias = await _jira.BuscarHistoriasDaReleaseAsync(chaveRelease, ct);
var release = new Release(chaveRelease, historias);
release.MarcarProcessando();

var execucao = release.RegistrarExecucao(_modeloLlm);   // ← rastreabilidade

try
{
    var historiasLimpas = historias.Select(ExtrairELimpar).ToList();          // estágio 1
    var grupos = await _llm.AgruparSemelhantesAsync(...);                     // estágio 3
    var itens = await ProcessarGruposAsync(...);                              // estágios 2 e 4
    var (titulo, resumo) = await _llm.GerarTituloEResumoAsync(...);           // estágio 5

    release.ConcluirProcessamento(itens, titulo, resumo);
    execucao.MarcarConcluida();
}
catch (Exception exc)
{
    release.MarcarFalha();
    execucao.MarcarFalha(exc.Message);   // ← a falha também deixa rastro
    throw;
}
```

Fala:

> O `RegistrarExecucao` está antes do `try` de propósito. Toda rodada deixa rastro,
> inclusive a que falha — o `catch` registra o erro antes de propagar. Rastreabilidade
> que só funciona no caminho feliz não serve para auditoria.

> O estágio 1, `ExtrairELimpar`, é determinístico: normaliza espaço e quebra de linha.
> Não chama o modelo. Não faz sentido gastar token com `string.Split`.

Aponte o método `ExtrairELimpar` (linha 75):

> Tem uma sutileza aqui que já foi bug: se a história tem subtarefa Release Note, é o
> texto **dela** que precisa ser limpo, não a descrição técnica. Limpar sempre a
> descrição deixaria o texto sujo passar, porque `TextoFonte` prioriza a Release Note.

### Passo 4 — A chamada ao modelo

**Arquivo**: `src/InvoiSys.Infrastructure/Llm/OpenRouterProvider.cs`

> OpenRouter é um gateway: um endpoint, vários modelos, schema compatível com OpenAI.
> Não precisamos de SDK — `HttpClient` direto resolve. Trocar de modelo é mudar uma
> string de configuração, no formato `provedor/modelo`.

Mostre `AgruparSemelhantesAsync` (linha 62) e a validação na linha 81:

```csharp
if (!chavesEntrada.SetEquals(chavesSaida))
{
    var faltando = chavesEntrada.Except(chavesSaida).ToList();
    // cada chave perdida vira um grupo próprio, com log de aviso
}
```

> Esse é o tratamento que eu acho mais importante do arquivo. Se o modelo "esquecer"
> uma história no agrupamento, a gente **não** descarta em silêncio: isola ela como
> grupo próprio e loga aviso. Uma história aparecer sem agrupamento é ruim; uma história
> sumir do comunicado que vai para o cliente é inaceitável.

**Arquivo**: `src/InvoiSys.Infrastructure/Llm/PromptLoader.cs`

> Os prompts vivem em `prompts/*.md`, versionados. A interpolação usa `{{chave}}`, estilo
> Mustache — deliberadamente **não** usa interpolação nativa do C#, porque os prompts
> contêm exemplos de JSON literal que seriam interpretados como placeholder e quebrariam.
> Isso já foi bug uma vez, e está documentado no `AGENTS.md` para não voltar.

Abra `prompts/04_reescrever_linguagem_negocio.md`:

> Ajustar o tom de voz da InvoiSys é editar este arquivo. Não recompila nada, e a
> mudança aparece no diff do pull request.

### Passo 5 — O gate

**Arquivo**: `src/InvoiSys.Domain/Entities/Release.cs`

> O pipeline termina chamando `ConcluirProcessamento`. Repare no que esse método faz e
> no que ele **não** faz:

```csharp
versao.PreencherConteudo(itens, tituloExecutivo, resumoExecutivo);
Status = StatusPipeline.AguardandoRevisao;
```

> Ele nunca deixa a Release em estado publicável. O caminho para "aprovado" é outro
> método, chamado por uma pessoa.

---

## Bloco 3 — Onde os invariantes moram (5 min)

### Fala de abertura

> Toda regra crítica do domínio está dentro do objeto que ela protege, não espalhada em
> `if` pela API ou por um service. A diferença prática: não existe caminho de código que
> esqueça de verificar.

### `VersaoComunicado.Aprovar()`

**Arquivo**: `src/InvoiSys.Domain/Entities/VersaoComunicado.cs` (linha 105)

```csharp
public void Aprovar(string revisadoPor, DateTimeOffset agora)
{
    if (_itens.Count == 0) throw new VersaoSemItensException(...);
    if (ItensPublicaveis.Count == 0) throw new VersaoSemItensException(...);
    if (Status != StatusRevisao.AguardandoRevisao) throw new TransicaoDeStatusInvalidaException(...);

    Status = StatusRevisao.Aprovado;
    RevisadoPor = revisadoPor;
    RevisadoEm = agora;
}
```

> Três guardas. A segunda é a mais sutil: se o revisor excluiu todos os itens, não há
> comunicado a publicar, e aprovar não faz sentido.

> `Status` tem `private set`. Nenhuma camada de fora consegue escrever nele. O único
> caminho é por estes métodos.

### `ComunicadoExportado` — o gate que não dá para burlar

**Arquivo**: `src/InvoiSys.Domain/Entities/ComunicadoExportado.cs` (linha 32)

```csharp
public ComunicadoExportado(VersaoComunicado versao, FormatoExportacao formato, ...)
{
    if (!versao.ProntaParaExportar)
    {
        throw new ReleaseNaoAprovadaException(...);
    }
    ...
}
```

> Isso está no **construtor**. Não existe uma exportação não aprovada nem como objeto em
> memória, quanto mais no banco. Se alguém escrever um serviço de exportação novo daqui
> a seis meses e esquecer de checar aprovação, o código não roda.

### A separação dos dois status

**Arquivos**: `src/InvoiSys.Domain/Enums/StatusPipeline.cs` e `StatusRevisao.cs`

> Dois enums separados de propósito. `StatusPipeline` responde "a IA rodou?".
> `StatusRevisao` responde "a pessoa aprovou?". São eixos independentes.

> Com um enum só era impossível representar uma situação real: a versão do Cliente
> aprovada enquanto a do Suporte ainda está em ajuste. Públicos diferentes são revisados
> por pessoas diferentes, em ritmos diferentes.

---

## Bloco 4 — Modelagem e banco (5 min)

### Abrir `docs/erd.png`

> Oito tabelas. Não vou ler uma por uma — quero apontar a coluna do meio:
> `versoes_comunicado` e, abaixo, `itens_comunicado`.

> Antes, os itens penduravam direto na Release e havia um texto só. Agora eles pertencem
> a uma **versão**, que é o comunicado de um público específico. Título, resumo, itens e
> aprovação são todos por audiência.

### O que destacar no diagrama

| Elemento | Fala |
|---|---|
| Linha vermelha tracejada | `comunicados_exportados → versoes_comunicado` é `ON DELETE RESTRICT`. Todas as outras são CASCADE. Apagar uma versão que já gerou comunicado publicado reescreveria o histórico de auditoria — o banco recusa. |
| `origens` com `IX` verde | Índice GIN num `text[]`. Responde "quais itens vieram da história INV-1234" sem tabela de junção. |
| Bordas tracejadas | `usuarios` e `destaques_hero` ainda não têm FK. Autenticação não está implementada, então quem revisou é texto livre. É pendência conhecida, não esquecimento. |

### Decisões de schema a citar

**Arquivo**: `src/InvoiSys.Infrastructure/Database/Configurations/VersaoComunicadoConfiguration.cs`

```csharp
t.HasCheckConstraint(
    "ck_versoes_comunicado_motivo_reprovacao",
    "status <> 'reprovado' OR motivo_reprovacao IS NOT NULL");
```

> Reprovar exige motivo. O domínio já bloqueia em C#, mas a constraint garante o mesmo
> para escrita via SQL direto, script de correção ou qualquer outro serviço que um dia
> escreva nesse banco.

> Vocabulário fixo (status, categoria, público, formato) usa `varchar` + CHECK, não enum
> nativo do Postgres. Motivo: `ALTER TYPE ADD VALUE` tem implicação de lock. Se o negócio
> pedir uma categoria nova, a migration é trivial.

**Arquivo**: `src/InvoiSys.Infrastructure/Database/Migrations/20260918150448_TriggersAtualizadoEm.cs`

> Os triggers de `atualizado_em` estão no banco, não em código de aplicação. O
> `DEFAULT now()` só age no INSERT — sem trigger, o campo congela na criação e nenhum
> UPDATE o move. Isso era um bug real que a gente achou testando; volto nele daqui a pouco.

---

## Bloco 5 — Testes e validação (3 min)

### Rodar ao vivo

```bash
dotnet test InvoiSys.slnx
```

Resultado: `62 aprovados`.

### Onde estão

| Arquivo | Cobre |
|---|---|
| `tests-dotnet/.../Unit/ReleaseInvariantesTests.cs` | O gate de revisão humana, 7 casos |
| `tests-dotnet/.../Unit/VersaoComunicadoTests.cs` | Revisão por público, 16 casos |
| `tests-dotnet/.../Unit/PipelineGeracaoReleaseNoteTests.cs` | O pipeline com fakes das portas |
| `tests-dotnet/.../Unit/OpenRouterProviderTests.cs` | Parsing defensivo de resposta do modelo, 13 casos |
| `tests-dotnet/.../Integration/ApiReleasesTests.cs` | A API de ponta a ponta, com `WebApplicationFactory` |

> Os testes usam **fakes das portas**, não mocks de biblioteca. `FakeJiraClient` e
> `FakeLlmProvider` implementam as interfaces do domínio. Isso torna o pipeline testável
> sem rede e sem gastar token.

### Os dois bugs — conte esta história

> Vale contar porque mostra o valor de testar contra banco real em vez de confiar em
> "compilou, passou".

**Bug 1 — campos que voltariam vazios**

> Quatro propriedades estavam declaradas como `{ get; }`, sem setter. O EF Core constrói
> a entidade pelo construtor privado, mas depois **não consegue escrever** nessas
> propriedades. Elas voltariam vazias do banco, em silêncio. Nenhum teste em memória
> pegaria isso, porque em memória o construtor preenche tudo.
>
> Achamos escrevendo um teste de ida e volta: grava pelo domínio, lê de volta pelo EF,
> confere campo a campo. 16 verificações. Corrigido com `{ get; private init; }`.

**Bug 2 — a data que nunca mudava**

> A documentação dizia que havia triggers de `atualizado_em`. Não havia — nenhuma
> migration continha o SQL. Testamos: INSERT, espera 1 segundo, UPDATE, e o
> `atualizado_em` continuava igual ao `criado_em`. Campo que mente em silêncio.
>
> Corrigido com migration dedicada, e retestado nas três tabelas.

### O que mais foi validado contra Postgres real

> Subimos o Postgres 16 em container e testamos as constraints **rejeitando** dados
> inválidos: público duplicado na mesma Release, reprovação sem motivo, status fora do
> vocabulário. Os três foram bloqueados pelo banco.
>
> E subimos a stack inteira em container: a API respondeu 200 no `/health`, 501 no
> endpoint do Jira sem token, e o Docker reportou o container como `healthy`.

---

## Bloco 6 — A API está defasada (4 min)

> Preciso ser direto sobre uma dívida que a gente assumiu de propósito.

### O problema, com o arquivo aberto

**Arquivo**: `src/InvoiSys.Api/Endpoints/ReleaseEndpoints.cs`

> Duas rotas: buscar do Jira e processar. Só isso.
>
> Nós implementamos `Aprovar`, `Reprovar` com motivo, `Reabrir`, e exclusão de item na
> revisão. **Nenhum tem rota HTTP.** Dá para gerar o comunicado pela API, mas não dá
> para revisar.

**Arquivo**: `src/InvoiSys.Api/Contracts/ReleaseContracts.cs`

```csharp
public sealed record ReleaseProcessadaOut(
    string ChaveJira,
    string Status,
    string? TituloExecutivo,          // singular
    string? ResumoExecutivo,          // singular
    IReadOnlyList<ItemComunicadoOut> Itens);   // uma lista só
```

> O domínio tem N versões, uma por público. O DTO tem campo único. A API devolve só o
> Cliente — os outros três existem no modelo e são invisíveis por HTTP.

> E o `ItemComunicadoOut` não expõe se o item foi excluído na revisão. Quem consome não
> consegue distinguir um item publicável de um que o revisor tirou.

### Por que continua funcionando

**Arquivo**: `src/InvoiSys.Domain/Entities/Release.cs` (linha 86)

```csharp
public IReadOnlyList<ItemComunicado> Itens => VersaoCliente?.Itens ?? [];
```

> Quando expandimos o domínio, criamos atalhos derivados apontando para a versão
> Cliente. Foi deliberado: fez os 46 testes que já existiam passarem sem alteração
> nenhuma, e manteve a API no ar. Mas é compatibilidade, não a forma final.

### Por que não corrigimos por conta própria

> Mudar a forma da resposta quebra quem consome. Contrato de API é decisão de produto, não
> técnica — preferimos trazer a proposta para esta conversa em vez de decidir sozinhos e
> vocês descobrirem depois.

### A proposta

```
GET   /api/v1/releases/{chave}/versoes
GET   /api/v1/releases/{chave}/versoes/{publico}
POST  /api/v1/releases/{chave}/versoes/{publico}/aprovar
POST  /api/v1/releases/{chave}/versoes/{publico}/reprovar
POST  /api/v1/releases/{chave}/versoes/{publico}/reabrir
PATCH /api/v1/releases/{chave}/versoes/{publico}/itens/{id}
```

Na resposta: lista de versões por público, estado de revisão e quem revisou, `incluido` e
`textoFinal` em cada item, motivo da reprovação quando houver.

> As rotas atuais continuam funcionando — nada que existe hoje quebra.
>
> Estimativa de 1 a 2 dias. A lógica já existe e está coberta por teste; é trabalho de
> exposição, não de regra nova.
>
> **O que eu preciso de vocês**: aval nesse contrato, para o frontend construir a tela de
> revisão em cima dele sem retrabalho.

---

## Bloco 7 — Fechamento (2 min)

> Resumindo o estado: a fundação está fechada — Jira, pipeline, modelagem e
> infraestrutura funcionando e testados. A revisão humana é estrutural, não uma etapa que
> dá para pular. E a API precisa acompanhar o modelo, com proposta pronta.

### Três pedidos concretos

1. **Aval no contrato da API** — destrava a tela de revisão no frontend.
2. **Acesso ao Jira de produção** — sem instância real, o pipeline não é validado de
   ponta a ponta. É o único bloqueador que não depende de nós.
3. **Multi-empresa é requisito?** — hoje assumimos que não e não modelamos. Se for,
   é melhor descobrir agora do que com o banco em produção.

---

## Perguntas prováveis — respostas prontas

**"Por que trocaram Python por .NET no meio do projeto?"**
> A fundação original era Python + FastAPI. A InvoiSys sinalizou preferência por Node ou
> .NET, e o enunciado deixa a escolha livre. Como a arquitetura era hexagonal, a migração
> preservou o desenho — mudou a linguagem, não a estrutura. O código Python está no
> histórico do git antes do commit `3b3a75a`.

**"E se o modelo de IA alucinar e inventar uma funcionalidade que não existe?"**
> Duas defesas. Primeira: o gate de revisão humana — nada é publicado sem uma pessoa
> aprovar, e ela pode editar ou excluir qualquer item. Segunda: cada item guarda em
> `origens` as chaves Jira que o geraram, então dá para conferir a fonte. O que o sistema
> **não** faz é detectar alucinação sozinho; isso é trabalho do revisor, e é por isso que
> o gate é obrigatório.

**"Quanto custa em tokens processar uma Release?"**
> Depende do tamanho. O estágio 1 não gasta token — é limpeza determinística. Os estágios
> 2 e 4 são uma chamada por grupo de histórias; o 3 e o 5, uma chamada cada.
> Não medimos custo real ainda porque não rodamos contra uma Release de produção — é uma
> das coisas que o acesso ao Jira real destrava.

**"Se o Jira cair no meio do processamento, o que acontece?"**
> Há retry com backoff exponencial: 3 tentativas, e erro 4xx de cliente propaga na
> primeira porque tentar de novo um 401 não muda nada. Se ainda assim falhar, a Release
> vai para status `falhou`, a execução registra a mensagem de erro, e a API devolve 502.
> O rastro da tentativa fica em `execucoes_pipeline`.

**"Dá para reprocessar uma Release já aprovada?"**
> Dá. `Reabrir()` devolve a versão para revisão. E, de propósito, **não** apaga os
> comunicados já exportados — o que foi publicado aconteceu, e a auditoria não se
> reescreve por causa de uma correção posterior.

**"Por que não usaram LangChain / framework de agente?"**
> Foi avaliado e descartado. Para um pipeline linear de cinco estágios, sem RAG e sem
> decisão autônoma, o framework seria peso morto — mais dependência, mais camada de
> abstração e menos controle sobre o prompt exato que vai para o modelo. Cada estágio é
> uma função com um prompt versionado, o que é auditável no pull request.

**"Como vocês garantem que o comunicado não vaza informação interna?"**
> É exatamente o papel do campo `incluido` no item. O revisor exclui o que não deve ir
> para o cliente, com motivo registrado. Excluir não apaga: o item continua no banco,
> auditável, mas fora do comunicado publicável. Além disso, o modelo de múltiplos públicos
> permite uma versão Interna com detalhe técnico e uma versão Cliente sem ele.

**"O que falta para colocar em produção?"**
> Na ordem: contrato da API, camada de persistência ligando o pipeline ao banco, tela de
> revisão no frontend, exportação em Markdown e autenticação real. O schema de todos eles
> já existe; o que falta é a lógica e a interface.

---

## Comandos de apoio

```bash
# Subir tudo
docker compose up -d --build

# Health
curl http://localhost:8080/health

# Endpoint sem Jira configurado (mostra o 501 honesto)
curl -s http://localhost:8080/api/v1/releases/RELEASE-2026-08/historias

# Testes
dotnet test InvoiSys.slnx

# Contagem de tabelas no banco
docker compose exec db psql -U invoisys -d invoisys -c "\dt"

# Derrubar e limpar
docker compose down -v
```

---

## Se o tempo apertar

Corte, nesta ordem:

1. Bloco 1 (arquitetura) — resuma em uma frase: "quatro projetos, dependência só para dentro".
2. Passo 2 do Bloco 2 (detalhe do Jira) — diga só "o contrato da API v3 mudou e a gente
   acompanhou".
3. Bloco 5 (testes) — mantenha **só** a história dos dois bugs, que é a parte que convence.

**Não corte**: o gate de revisão (Bloco 3) e a dívida da API (Bloco 6). São o núcleo do
que precisa ser comunicado.
