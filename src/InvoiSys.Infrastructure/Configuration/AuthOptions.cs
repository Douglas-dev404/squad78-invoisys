namespace InvoiSys.Infrastructure.Configuration;

public sealed class AuthOptions
{
    public const string SecaoConfig = "Auth";

    public const int TamanhoMinimoChave = 32;

    public JwtOptions Jwt { get; set; } = new();

    public AdminInicialOptions AdminInicial { get; set; } = new();

    public void Validar()
    {
        if (string.IsNullOrWhiteSpace(Jwt.Chave))
        {
            throw new InvalidOperationException(
                "Auth:Jwt:Chave não configurada. Defina a variável de ambiente Auth__Jwt__Chave " +
                $"(ou o .env) com um segredo de pelo menos {TamanhoMinimoChave} caracteres. " +
                "Sem ela a API não sobe: não existe modo pendente para autenticação.");
        }

        if (System.Text.Encoding.UTF8.GetByteCount(Jwt.Chave) < TamanhoMinimoChave)
        {
            throw new InvalidOperationException(
                $"Auth:Jwt:Chave curta demais: use pelo menos {TamanhoMinimoChave} caracteres " +
                "(Auth__Jwt__Chave).");
        }

        if (string.IsNullOrWhiteSpace(Jwt.Emissor) || string.IsNullOrWhiteSpace(Jwt.Audiencia))
        {
            throw new InvalidOperationException(
                "Auth:Jwt:Emissor e Auth:Jwt:Audiencia são obrigatórios " +
                "(Auth__Jwt__Emissor, Auth__Jwt__Audiencia).");
        }

        if (Jwt.ExpiracaoHoras <= 0)
        {
            throw new InvalidOperationException(
                "Auth:Jwt:ExpiracaoHoras deve ser maior que zero (Auth__Jwt__ExpiracaoHoras).");
        }
    }
}

public sealed class JwtOptions
{
    public string Chave { get; set; } = string.Empty;

    public string Emissor { get; set; } = "invoisys";

    public string Audiencia { get; set; } = "invoisys-api";

    public double ExpiracaoHoras { get; set; } = 8;
}

public sealed class AdminInicialOptions
{
    public string Email { get; set; } = string.Empty;

    public string Senha { get; set; } = string.Empty;

    public string Nome { get; set; } = string.Empty;

    public bool EstaConfigurado =>
        !string.IsNullOrWhiteSpace(Email)
        && !string.IsNullOrWhiteSpace(Senha)
        && !string.IsNullOrWhiteSpace(Nome);
}
