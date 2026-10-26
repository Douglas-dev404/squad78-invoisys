namespace InvoiSys.Domain.Ports;

public interface IHashSenha
{
    string Gerar(string senha);

    bool Verificar(string senha, string hash);
}
