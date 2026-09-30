using InvoiSys.Api.Contracts;
using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;
using InvoiSys.Domain.Ports;
using Microsoft.AspNetCore.Mvc;

namespace InvoiSys.Api.Endpoints;

/// <summary>
/// Revisão item a item do comunicado: editar o texto, excluir e reincluir. Camada fina:
/// carrega a Release, chama o método do agregado e salva. A regra de quando um item
/// pode mudar (versão aprovada não pode) mora no domínio, não aqui.
///
/// O <c>itemId</c> é global (uuid), então a rota não precisa do público-alvo: a Release
/// acha a versão dona do item. Item que existe mas é de outra Release é 404.
/// </summary>
public static class ItemComunicadoEndpoints
{
    public static IEndpointRouteBuilder MapItemComunicadoEndpoints(this IEndpointRouteBuilder rotas)
    {
        var grupo = rotas.MapGroup("/api/v1/releases/{chaveRelease}/itens/{itemId:guid}")
            .WithTags("itens");

        grupo.MapPatch("", EditarItemAsync)
            .WithName("EditarItem")
            .WithSummary("Troca o texto de um item pela versão revisada por um humano.")
            .Produces<ItemRevisaoOut>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        grupo.MapPost("/excluir", ExcluirItemAsync)
            .WithName("ExcluirItem")
            .WithSummary("Tira o item do comunicado publicado, sem apagar o registro.")
            .Produces<ItemRevisaoOut>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        grupo.MapPost("/reincluir", ReincluirItemAsync)
            .WithName("ReincluirItem")
            .WithSummary("Devolve ao comunicado um item excluído.")
            .Produces<ItemRevisaoOut>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return rotas;
    }

    private static Task<IResult> EditarItemAsync(
        string chaveRelease,
        Guid itemId,
        [FromBody] EditarItemIn corpo,
        [FromServices] IReleaseRepository repositorio,
        CancellationToken cancellationToken) =>
        AlterarItemAsync(
            chaveRelease,
            repositorio,
            release => release.EditarItem(itemId, corpo.Texto ?? string.Empty),
            cancellationToken);

    private static Task<IResult> ExcluirItemAsync(
        string chaveRelease,
        Guid itemId,
        [FromBody] ExcluirItemIn? corpo,
        [FromServices] IReleaseRepository repositorio,
        CancellationToken cancellationToken) =>
        AlterarItemAsync(
            chaveRelease,
            repositorio,
            release => release.ExcluirItem(itemId, corpo?.Motivo),
            cancellationToken);

    private static Task<IResult> ReincluirItemAsync(
        string chaveRelease,
        Guid itemId,
        [FromServices] IReleaseRepository repositorio,
        CancellationToken cancellationToken) =>
        AlterarItemAsync(
            chaveRelease,
            repositorio,
            release => release.ReincluirItem(itemId),
            cancellationToken);

    /// <summary>
    /// O caminho de toda mutação de revisão: Release via repository → método do
    /// agregado → SalvarAsync. As exceções do domínio viram HTTP aqui, nunca 500.
    /// </summary>
    private static async Task<IResult> AlterarItemAsync(
        string chaveRelease,
        IReleaseRepository repositorio,
        Func<Release, ItemComunicado> alteracao,
        CancellationToken cancellationToken)
    {
        var release = await repositorio.BuscarPorChaveJiraAsync(chaveRelease, cancellationToken);

        if (release is null)
        {
            return Results.Problem(
                detail: $"Release {chaveRelease} não encontrada.",
                statusCode: StatusCodes.Status404NotFound);
        }

        ItemComunicado item;

        try
        {
            item = alteracao(release);
        }
        catch (ItemNaoEncontradoException exc)
        {
            return Results.Problem(detail: exc.Message, statusCode: StatusCodes.Status404NotFound);
        }
        catch (TransicaoDeStatusInvalidaException exc)
        {
            // Versão aprovada: o estado atual do recurso impede a operação, e o caminho
            // é reabrir a revisão — mesmo 409 do reprocessamento de versão aprovada.
            return Results.Problem(detail: exc.Message, statusCode: StatusCodes.Status409Conflict);
        }
        catch (ArgumentException exc)
        {
            return Results.Problem(detail: exc.Message, statusCode: StatusCodes.Status400BadRequest);
        }

        await repositorio.SalvarAsync(release, cancellationToken);

        var publico = release.Versoes.First(v => v.Itens.Contains(item)).Publico;

        return Results.Ok(new ItemRevisaoOut(
            item.Id,
            publico.ParaValor(),
            item.Categoria.ParaValor(),
            item.Texto,
            item.TextoEditadoManualmente,
            item.TextoFinal,
            item.Incluido,
            item.MotivoExclusao,
            item.Origens));
    }
}
