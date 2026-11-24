namespace InvoiSys.Domain.Entities;

/// <summary>Um valor de configuração de integração (Jira, IA) editado pelo admin na tela de Integrações.</summary>
public sealed class ConfiguracaoIntegracao
{
    private ConfiguracaoIntegracao()
    {
        Chave = string.Empty;
        Valor = string.Empty;
    }

    public ConfiguracaoIntegracao(string chave, string valor, bool secreto)
    {
        Chave = chave;
        Valor = valor;
        Secreto = secreto;
        AtualizadoEm = DateTimeOffset.UtcNow;
    }

    public string Chave { get; private init; }

    /// <summary>Texto puro quando não é secreto; texto cifrado (AES-GCM) quando é.</summary>
    public string Valor { get; private set; }

    public bool Secreto { get; private init; }

    public DateTimeOffset AtualizadoEm { get; private set; }

    public void Atualizar(string valor)
    {
        Valor = valor;
        AtualizadoEm = DateTimeOffset.UtcNow;
    }
}
