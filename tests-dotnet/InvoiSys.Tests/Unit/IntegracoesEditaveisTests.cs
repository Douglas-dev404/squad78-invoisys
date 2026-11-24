using FluentAssertions;
using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Ports;
using InvoiSys.Infrastructure.Configuration;
using InvoiSys.Infrastructure.Integracoes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace InvoiSys.Tests.Unit;

public class IntegracoesEditaveisTests
{
    private static IConfiguration Configuracao(params (string Chave, string Valor)[] extras)
    {
        var valores = new Dictionary<string, string?> { ["Auth:Jwt:Chave"] = ConfiguracaoDeTestes.ChaveJwt };
        foreach (var (chave, valor) in extras)
        {
            valores[chave] = valor;
        }

        return new ConfigurationBuilder().AddInMemoryCollection(valores).Build();
    }

    [Fact]
    public void Cofre_cifra_e_decifra_sem_deixar_o_texto_puro_no_valor_guardado()
    {
        var cofre = new CofreSegredos(Configuracao());

        var guardado = cofre.Cifrar("sk-segredo-123");

        guardado.Should().StartWith("v1:").And.NotContain("sk-segredo-123");
        cofre.Decifrar(guardado).Should().Be("sk-segredo-123");
        cofre.Cifrar("sk-segredo-123").Should().NotBe(guardado, "o nonce muda a cada cifragem");
    }

    [Fact]
    public void Cofre_devolve_null_com_outra_chave_ou_valor_adulterado()
    {
        var guardado = new CofreSegredos(Configuracao()).Cifrar("sk-segredo-123");
        var outraChave = new CofreSegredos(Configuracao(("Configuracoes:ChaveCriptografia", "outra-chave-com-bastante-tamanho-123456")));

        outraChave.Decifrar(guardado).Should().BeNull();
        new CofreSegredos(Configuracao()).Decifrar(guardado[..^4] + "AAAA").Should().BeNull();
        new CofreSegredos(Configuracao()).Decifrar("texto-puro").Should().BeNull();
    }

    [Fact]
    public void Valor_salvo_no_sistema_vale_por_cima_do_ambiente_e_o_segredo_chega_decifrado()
    {
        var configuracao = Configuracao(
            ("Jira:BaseUrl", "https://ambiente.atlassian.net"),
            ("Jira:Email", "ambiente@example.com"),
            ("Jira:ApiToken", "token-do-ambiente"),
            ("Jira:Projeto", "AMB"));
        var cofre = new CofreSegredos(configuracao);
        var emUso = new IntegracoesEmUso(configuracao, cofre, NullLogger<IntegracoesEmUso>.Instance);

        emUso.Jira.BaseUrl.Should().Be("https://ambiente.atlassian.net");

        emUso.Recarregar(
        [
            new ConfiguracaoIntegracao(ChavesIntegracao.JiraBaseUrl, "https://sistema.atlassian.net", false),
            new ConfiguracaoIntegracao(ChavesIntegracao.JiraApiToken, cofre.Cifrar("token-do-sistema"), true),
            new ConfiguracaoIntegracao(ChavesIntegracao.IaProvedor, "OpenAi", false),
            new ConfiguracaoIntegracao(ChavesIntegracao.OpenAiModelo, "gpt-5", false),
        ]);

        emUso.Jira.BaseUrl.Should().Be("https://sistema.atlassian.net");
        emUso.Jira.ApiToken.Should().Be("token-do-sistema");
        emUso.Jira.Email.Should().Be("ambiente@example.com", "o que não foi salvo continua vindo do ambiente");
        emUso.Provedor.Should().Be(ProvedorLlm.OpenAi);
        emUso.ModeloAtivo.Should().Be("gpt-5");
        emUso.ChavesDoSistema.Should().Contain(ChavesIntegracao.JiraApiToken);
    }

    [Fact]
    public void Segredo_que_nao_decifra_cai_para_o_valor_do_ambiente()
    {
        var configuracao = Configuracao(("Jira:ApiToken", "token-do-ambiente"));
        var emUso = new IntegracoesEmUso(
            configuracao, new CofreSegredos(configuracao), NullLogger<IntegracoesEmUso>.Instance);

        emUso.Recarregar([new ConfiguracaoIntegracao(ChavesIntegracao.JiraApiToken, "v1:lixo", true)]);

        emUso.Jira.ApiToken.Should().Be("token-do-ambiente");
        emUso.ChavesDoSistema.Should().NotContain(ChavesIntegracao.JiraApiToken);
    }

    [Fact]
    public async Task Monitor_marca_a_Release_do_fluxo_e_filtra_por_ela_e_por_id()
    {
        var monitor = new MonitorExecucaoEmMemoria(TimeProvider.System);

        using (monitor.IniciarEscopo("2026.10.0"))
        {
            monitor.Publicar(TipoEventoExecucao.Etapa, "primeiro");
            await Task.Yield();
            monitor.Publicar(TipoEventoExecucao.IaChamada, "segundo", prompt: "pergunta");
        }

        using (monitor.IniciarEscopo("2026.11.0"))
        {
            monitor.Publicar(TipoEventoExecucao.Etapa, "outra release");
        }

        monitor.Publicar(TipoEventoExecucao.Etapa, "sem release");

        monitor.Depois(0).Should().HaveCount(4);
        monitor.Depois(0, "2026.10.0").Select(e => e.Mensagem).Should().Equal("primeiro", "segundo");
        monitor.Depois(2).Select(e => e.Mensagem).Should().Equal("outra release", "sem release");
        monitor.Depois(0).Last().Release.Should().BeNull();
    }

    [Fact]
    public void Monitor_guarda_so_os_eventos_mais_recentes_e_corta_textos_enormes()
    {
        var monitor = new MonitorExecucaoEmMemoria(TimeProvider.System);

        for (var i = 0; i < 450; i++)
        {
            monitor.Publicar(TipoEventoExecucao.Etapa, $"evento {i}", resposta: new string('x', 20_000));
        }

        var eventos = monitor.Depois(0);
        eventos.Should().HaveCount(400);
        eventos[0].Mensagem.Should().Be("evento 50");
        eventos[0].Resposta!.Length.Should().BeLessThan(8100);
    }
}
