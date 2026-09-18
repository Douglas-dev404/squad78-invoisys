namespace InvoiSys.Domain.Entities;

/// <summary>
/// Destaque institucional exibido na tela de login (seção Hero) — conteúdo
/// editorial, não regra de negócio do pipeline. Existe como entidade persistida
/// porque o frontend já consome isso de um endpoint dedicado
/// (GET /branding/highlights, ver frontend/src/services/branding.service.js),
/// esperando dados dinâmicos, não texto fixo no bundle do front.
/// </summary>
public sealed class DestaqueHero
{
    public DestaqueHero(string titulo, string descricao, string icone, int ordem)
    {
        Id = Guid.NewGuid();
        Titulo = titulo;
        Descricao = descricao;
        Icone = icone;
        Ordem = ordem;
        Ativo = true;
        CriadoEm = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private init; }

    public string Titulo { get; private set; }

    public string Descricao { get; private set; }

    /// <summary>Nome do ícone (vocabulário do frontend, ex: "auto_awesome").</summary>
    public string Icone { get; private set; }

    /// <summary>Ordem de exibição na lista — menor primeiro.</summary>
    public int Ordem { get; private set; }

    public bool Ativo { get; private set; }

    public DateTimeOffset CriadoEm { get; private init; }

    public void Desativar() => Ativo = false;
}
