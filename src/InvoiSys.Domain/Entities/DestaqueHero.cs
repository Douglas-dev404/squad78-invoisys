namespace InvoiSys.Domain.Entities;

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

    public string Icone { get; private set; }

    public int Ordem { get; private set; }

    public bool Ativo { get; private set; }

    public DateTimeOffset CriadoEm { get; private init; }

    public void Desativar() => Ativo = false;
}
