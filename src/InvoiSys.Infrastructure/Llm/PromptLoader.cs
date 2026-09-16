using System.Collections.Concurrent;

namespace InvoiSys.Infrastructure.Llm;

/// <summary>
/// Carrega os arquivos de prompt versionados em "prompts/" e faz a interpolação de
/// placeholders {{chave}}. Único ponto do código que lê arquivo de prompt — mantém a
/// regra de "prompt nunca hardcoded em string C#" verificável num lugar só.
///
/// Interpolação por substituição literal, não string interpolada/composite format:
/// os prompts contêm exemplos de JSON literal no formato de saída esperado
/// (ex: {"titulo": "...", "resumo": "..."}), e um formatador interpretaria essas
/// chaves como placeholders. Por isso o padrão é {{chave}} (Mustache-like), que nunca
/// colide com JSON de exemplo escrito com chave simples.
/// </summary>
public sealed class PromptLoader(string? diretorioPrompts = null)
{
    private readonly string _diretorio = diretorioPrompts ?? LocalizarDiretorioPadrao();
    private readonly ConcurrentDictionary<string, string> _cache = new();

    public string Carregar(string nomeArquivo) =>
        _cache.GetOrAdd(nomeArquivo, nome =>
        {
            var caminho = Path.Combine(_diretorio, nome);
            if (!File.Exists(caminho))
            {
                throw new FileNotFoundException(
                    $"Prompt '{nome}' não encontrado em {_diretorio}. Prompts do pipeline "
                    + "vivem versionados em prompts/, não em string no código.",
                    caminho);
            }

            return File.ReadAllText(caminho);
        });

    /// <summary>
    /// Carrega o prompt e substitui cada {{chave}} pelo valor correspondente.
    /// </summary>
    public string Montar(string nomeArquivo, params (string Chave, string Valor)[] valores)
    {
        var texto = Carregar(nomeArquivo);
        foreach (var (chave, valor) in valores)
        {
            texto = texto.Replace($"{{{{{chave}}}}}", valor, StringComparison.Ordinal);
        }

        return texto;
    }

    /// <summary>
    /// Sobe a partir do diretório da aplicação até achar uma pasta "prompts/". O
    /// binário roda em bin/Debug/netX.0/, então o caminho relativo à raiz do repo
    /// varia com a configuração de build — procurar é mais robusto que fixar "../../..".
    /// </summary>
    private static string LocalizarDiretorioPadrao()
    {
        var atual = new DirectoryInfo(AppContext.BaseDirectory);
        while (atual is not null)
        {
            var candidato = Path.Combine(atual.FullName, "prompts");
            if (Directory.Exists(candidato))
            {
                return candidato;
            }

            atual = atual.Parent;
        }

        throw new DirectoryNotFoundException(
            $"Diretório 'prompts/' não encontrado a partir de {AppContext.BaseDirectory}.");
    }
}
