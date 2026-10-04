using System.Collections.Concurrent;

namespace InvoiSys.Infrastructure.Llm;

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

    public string Montar(string nomeArquivo, params (string Chave, string Valor)[] valores)
    {
        var texto = Carregar(nomeArquivo);
        foreach (var (chave, valor) in valores)
        {
            // Substituição literal de {{chave}}: os prompts têm JSON de exemplo que um formatador de string quebraria (ADR-005).
            texto = texto.Replace($"{{{{{chave}}}}}", valor, StringComparison.Ordinal);
        }

        return texto;
    }

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
