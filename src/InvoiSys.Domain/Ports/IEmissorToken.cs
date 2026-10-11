using InvoiSys.Domain.Entities;

namespace InvoiSys.Domain.Ports;

public interface IEmissorToken
{
    TokenEmitido Emitir(Usuario usuario);
}

public sealed record TokenEmitido(string Token, DateTimeOffset ExpiraEm);
