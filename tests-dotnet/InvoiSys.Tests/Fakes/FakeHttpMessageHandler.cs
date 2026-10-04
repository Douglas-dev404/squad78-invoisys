using System.Net;
using System.Text;

namespace InvoiSys.Tests.Fakes;

public sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly Queue<HttpResponseMessage> _respostas = new();

    public List<string> CorposEnviados { get; } = [];

    public List<Uri?> UrisChamadas { get; } = [];

    public FakeHttpMessageHandler ResponderJson(string json, HttpStatusCode status = HttpStatusCode.OK)
    {
        _respostas.Enqueue(new HttpResponseMessage(status)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        });
        return this;
    }

    public FakeHttpMessageHandler ResponderConteudoLlm(string conteudo)
    {
        var escapado = System.Text.Json.JsonSerializer.Serialize(conteudo);
        return ResponderJson($$"""
        { "choices": [ { "message": { "role": "assistant", "content": {{escapado}} } } ] }
        """);
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        UrisChamadas.Add(request.RequestUri);

        if (request.Content is not null)
        {
            CorposEnviados.Add(await request.Content.ReadAsStringAsync(cancellationToken));
        }

        return _respostas.Count > 0
            ? _respostas.Dequeue()
            : new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("sem resposta enfileirada no fake"),
            };
    }
}
