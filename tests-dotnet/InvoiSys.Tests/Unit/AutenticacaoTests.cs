using System.Diagnostics;
using FluentAssertions;
using InvoiSys.Application.Acesso;
using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Ports;
using InvoiSys.Infrastructure.Auth;
using InvoiSys.Tests.Fakes;

namespace InvoiSys.Tests.Unit;

public class AutenticacaoTests
{
    private const string Email = "ana@invoisys.com";
    private const string Senha = "Senha-Correta-123";

    private sealed class HashContador(IHashSenha real) : IHashSenha
    {
        public int Verificacoes { get; private set; }

        public string Gerar(string senha) => real.Gerar(senha);

        public bool Verificar(string senha, string hash)
        {
            Verificacoes++;
            return real.Verificar(senha, hash);
        }
    }

    private sealed class EmissorFalso : IEmissorToken
    {
        public TokenEmitido Emitir(Usuario usuario) =>
            new("token-de-teste", DateTimeOffset.UtcNow.AddHours(8));
    }

    private static async Task<(Autenticacao Caso, HashContador Hash)> Montar(bool ativo = true)
    {
        var real = new Argon2idHashSenha();
        var usuario = new Usuario("Ana", Email, real.Gerar(Senha), "release_manager");
        if (!ativo)
        {
            usuario.Desativar();
        }

        var repositorio = new FakeUsuarioRepository();
        await repositorio.SalvarAsync(usuario);

        var hash = new HashContador(real);
        return (new Autenticacao(repositorio, hash, new EmissorFalso()), hash);
    }

    [Fact]
    public async Task Email_inexistente_tambem_verifica_um_hash()
    {
        var (caso, hash) = await Montar();

        (await caso.AutenticarAsync("ninguem@invoisys.com", Senha)).Should().BeNull();
        hash.Verificacoes.Should().Be(1);
    }

    [Fact]
    public async Task Senha_errada_verifica_um_hash_e_nao_autentica()
    {
        var (caso, hash) = await Montar();

        (await caso.AutenticarAsync(Email, "errada")).Should().BeNull();
        hash.Verificacoes.Should().Be(1);
    }

    [Fact]
    public async Task Usuario_desativado_nao_autentica_nem_com_a_senha_certa()
    {
        var (caso, hash) = await Montar(ativo: false);

        (await caso.AutenticarAsync(Email, Senha)).Should().BeNull();
        hash.Verificacoes.Should().Be(1);
    }

    [Fact]
    public async Task Credenciais_certas_devolvem_a_sessao_com_token()
    {
        var (caso, _) = await Montar();

        var sessao = await caso.AutenticarAsync("  ANA@invoisys.com ", Senha);

        sessao.Should().NotBeNull();
        sessao!.Token.Token.Should().Be("token-de-teste");
    }

    [Fact]
    public void Hash_ficticio_tem_os_mesmos_parametros_e_tamanhos_do_hash_real()
    {
        var real = new Argon2idHashSenha().Gerar("qualquer").Split('$');
        var ficticio = Autenticacao.HashFicticio.Split('$');

        ficticio.Take(4).Should().Equal(real.Take(4));
        ficticio[4].Length.Should().Be(real[4].Length);
        ficticio[5].Length.Should().Be(real[5].Length);
    }

    [Fact]
    public async Task Tempo_de_resposta_nao_revela_se_o_email_existe()
    {
        var (caso, _) = await Montar();
        await caso.AutenticarAsync(Email, "aquecimento");
        await caso.AutenticarAsync("ninguem@invoisys.com", "aquecimento");

        var existente = new List<double>();
        var inexistente = new List<double>();
        for (var i = 0; i < 9; i++)
        {
            existente.Add(await Medir(caso, Email));
            inexistente.Add(await Medir(caso, "ninguem@invoisys.com"));
        }

        var a = Mediana(existente);
        var b = Mediana(inexistente);

        (Math.Abs(a - b) / Math.Max(a, b)).Should().BeLessThan(
            0.5, $"e-mail existente levou {a:F1} ms e inexistente {b:F1} ms");
    }

    private static async Task<double> Medir(Autenticacao caso, string email)
    {
        var inicio = Stopwatch.GetTimestamp();
        await caso.AutenticarAsync(email, "senha-errada");
        return Stopwatch.GetElapsedTime(inicio).TotalMilliseconds;
    }

    private static double Mediana(List<double> valores)
    {
        var ordenados = valores.Order().ToList();
        return ordenados[ordenados.Count / 2];
    }
}