using InvoiSys.Domain.Ports;

namespace InvoiSys.Infrastructure.Integracoes;

public sealed class ModeloIaAtivo(IntegracoesEmUso integracoes) : IModeloIaAtivo
{
    public string? Modelo => integracoes.ModeloAtivo;
}
