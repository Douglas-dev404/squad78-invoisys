using FluentAssertions;
using InvoiSys.Application.Autenticacao;
using InvoiSys.Domain.Entities;
using InvoiSys.Tests.Fakes;
using Microsoft.Extensions.Time.Testing;

namespace InvoiSys.Tests.Unit;

public class RecuperacaoSenhaTests
{
    private const string UrlBase = "https://app.invoisys.com/redefinir-senha";

    private readonly FakeTimeProvider _relogio = new(new DateTimeOffset(2026, 10, 26, 12, 0, 0, TimeSpan.Zero));
    private readonly FakeUsuarioRepository _usuarios = new();
    private readonly FakeTokenRedefinicaoSenhaRepository _tokens = new();
    private readonly FakeEnvioEmail _email = new();
    private readonly Usuario _ana = new("Ana", "ana@invoisys.com", "hash:senha-antiga", "release_manager");

    public RecuperacaoSenhaTests() => _usuarios.Usuarios.Add(_ana);

    private RecuperacaoSenha Criar(TimeSpan? cooldown = null) => new(
        _usuarios,
        _tokens,
        new FakeHashSenha(),
        _email,
        new OpcoesRecuperacaoSenha(UrlBase, cooldown ?? TimeSpan.FromSeconds(60)),
        _relogio);

    private string TokenDoUltimoEmail() => _email.Enviados[^1].Link.Split("#token=")[1];

    [Fact]
    public async Task Solicitar_envia_link_com_token_e_guarda_apenas_o_hash()
    {
        await Criar().SolicitarAsync("  ANA@invoisys.com ");

        _email.Enviados.Should().ContainSingle();
        _email.Enviados[0].Email.Should().Be("ana@invoisys.com");
        _email.Enviados[0].Link.Should().StartWith($"{UrlBase}#token=");
        _email.Enviados[0].ExpiraEm.Should().Be(_relogio.GetUtcNow().AddMinutes(30));
        var tokenCru = TokenDoUltimoEmail();
        _tokens.Tokens.Should().ContainSingle();
        _tokens.Tokens[0].TokenHash.Should().NotBe(tokenCru).And.HaveLength(64);
    }

    [Fact]
    public async Task Email_inexistente_nao_envia_nada_nem_grava_token_e_nao_lanca()
    {
        await Criar().SolicitarAsync("ninguem@invoisys.com");

        _email.Enviados.Should().BeEmpty();
        _tokens.Tokens.Should().BeEmpty();
    }

    [Fact]
    public async Task Usuario_desativado_e_tratado_como_inexistente()
    {
        _ana.Desativar();

        await Criar().SolicitarAsync("ana@invoisys.com");

        _email.Enviados.Should().BeEmpty();
        _tokens.Tokens.Should().BeEmpty();
    }

    [Fact]
    public async Task Email_em_branco_e_rejeitado()
    {
        var acao = () => Criar().SolicitarAsync(" ");

        await acao.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Redefinir_troca_a_senha_e_marca_o_token_como_usado()
    {
        var recuperacao = Criar();
        await recuperacao.SolicitarAsync("ana@invoisys.com");

        await recuperacao.RedefinirAsync(TokenDoUltimoEmail(), "senha-nova-123");

        _ana.SenhaHash.Should().Be("hash:senha-nova-123");
        _tokens.Tokens.Should().OnlyContain(t => t.UsadoEm != null);
    }

    [Fact]
    public async Task Token_expirado_depois_de_30_minutos_e_rejeitado_e_nao_troca_a_senha()
    {
        var recuperacao = Criar();
        await recuperacao.SolicitarAsync("ana@invoisys.com");
        var token = TokenDoUltimoEmail();
        _relogio.Advance(TimeSpan.FromMinutes(30));

        var acao = () => recuperacao.RedefinirAsync(token, "senha-nova-123");

        await acao.Should().ThrowAsync<TokenRedefinicaoInvalidoException>();
        _ana.SenhaHash.Should().Be("hash:senha-antiga");
    }

    [Fact]
    public async Task Token_ainda_vale_um_segundo_antes_de_expirar()
    {
        var recuperacao = Criar();
        await recuperacao.SolicitarAsync("ana@invoisys.com");
        var token = TokenDoUltimoEmail();
        _relogio.Advance(TimeSpan.FromMinutes(30).Subtract(TimeSpan.FromSeconds(1)));

        await recuperacao.RedefinirAsync(token, "senha-nova-123");

        _ana.SenhaHash.Should().Be("hash:senha-nova-123");
    }

    [Fact]
    public async Task Token_e_de_uso_unico()
    {
        var recuperacao = Criar();
        await recuperacao.SolicitarAsync("ana@invoisys.com");
        var token = TokenDoUltimoEmail();
        await recuperacao.RedefinirAsync(token, "senha-nova-123");

        var segundoUso = () => recuperacao.RedefinirAsync(token, "outra-senha-456");

        await segundoUso.Should().ThrowAsync<TokenRedefinicaoInvalidoException>();
        _ana.SenhaHash.Should().Be("hash:senha-nova-123");
    }

    [Fact]
    public async Task Token_inventado_ou_vazio_e_rejeitado()
    {
        var recuperacao = Criar();

        await ((Func<Task>)(() => recuperacao.RedefinirAsync("inventado", "senha-nova-123")))
            .Should().ThrowAsync<TokenRedefinicaoInvalidoException>();
        await ((Func<Task>)(() => recuperacao.RedefinirAsync(" ", "senha-nova-123")))
            .Should().ThrowAsync<TokenRedefinicaoInvalidoException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("curta")]
    public async Task Senha_fora_da_politica_e_rejeitada_sem_gastar_o_token(string? senha)
    {
        var recuperacao = Criar();
        await recuperacao.SolicitarAsync("ana@invoisys.com");
        var token = TokenDoUltimoEmail();

        var acao = () => recuperacao.RedefinirAsync(token, senha);

        await acao.Should().ThrowAsync<ArgumentException>();
        _tokens.Tokens[0].UsadoEm.Should().BeNull();
    }

    [Fact]
    public async Task Reenviar_invalida_o_token_anterior_e_emite_um_novo()
    {
        var recuperacao = Criar();
        await recuperacao.SolicitarAsync("ana@invoisys.com");
        var antigo = TokenDoUltimoEmail();
        _relogio.Advance(TimeSpan.FromSeconds(61));

        await recuperacao.ReenviarAsync("ana@invoisys.com");

        _email.Enviados.Should().HaveCount(2);
        var acao = () => recuperacao.RedefinirAsync(antigo, "senha-nova-123");
        await acao.Should().ThrowAsync<TokenRedefinicaoInvalidoException>();
        await recuperacao.RedefinirAsync(TokenDoUltimoEmail(), "senha-nova-123");
    }

    [Fact]
    public async Task Redefinir_invalida_os_outros_tokens_do_usuario()
    {
        _tokens.Tokens.Add(TokenRedefinicaoSenha.Emitir(_ana.Id, "outro-hash-pendente", _relogio.GetUtcNow()));
        var recuperacao = Criar(cooldown: TimeSpan.Zero);
        await recuperacao.SolicitarAsync("ana@invoisys.com");
        var token = TokenDoUltimoEmail();

        await recuperacao.RedefinirAsync(token, "senha-nova-123");

        _tokens.Tokens.Should().OnlyContain(t => t.UsadoEm != null);
    }

    [Fact]
    public async Task Cooldown_impede_segundo_email_em_sequencia_sem_revelar_nada()
    {
        var recuperacao = Criar();
        await recuperacao.SolicitarAsync("ana@invoisys.com");

        await recuperacao.SolicitarAsync("ana@invoisys.com");

        _email.Enviados.Should().ContainSingle();
        _tokens.Tokens.Should().ContainSingle();
    }
}
