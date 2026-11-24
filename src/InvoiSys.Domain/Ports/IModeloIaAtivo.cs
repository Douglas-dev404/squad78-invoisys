namespace InvoiSys.Domain.Ports;

/// <summary>Modelo de IA em uso agora (pode mudar na tela de Integrações sem reiniciar a API).</summary>
public interface IModeloIaAtivo
{
    string? Modelo { get; }
}
