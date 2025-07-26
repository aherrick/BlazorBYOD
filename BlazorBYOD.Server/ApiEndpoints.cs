using BlazorBYOD.Server.Helpers;
using BlazorBYOD.Server.Models;
using BlazorBYOD.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel.Connectors.AzureAISearch;
using UglyToad.PdfPig;

namespace BlazorBYOD.Server;

public static class ApiEndpoints
{
    public static void MapApiEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api").DisableAntiforgery();

        api.MapPost(
            "/reset",
            async (AzureAISearchCollection<string, IngestedChunk> aISearchCollection) =>
            {
                await aISearchCollection.EnsureCollectionDeletedAsync();
                await aISearchCollection.EnsureCollectionExistsAsync();
                return Results.Ok();
            }
        );

        api.MapPost(
            "/ingest",
            async (
                HttpRequest request,
                IEmbeddingGenerator<string, Embedding<float>> embeddingClient,
                AzureAISearchCollection<string, IngestedChunk> aISearchCollection
            ) =>
            {
                var form = await request.ReadFormAsync();
                var file = form.Files["file"];
                using var memoryStream = new MemoryStream();
                await file.CopyToAsync(memoryStream);
                memoryStream.Position = 0;

                using var document = PdfDocument.Open(memoryStream);
                foreach (var page in document.GetPages())
                {
                    var vector = await embeddingClient.GenerateAsync(page.Text);

                    var ingestedChunk = new IngestedChunk()
                    {
                        Key = Guid.NewGuid().ToString(),
                        DocumentId = file.FileName,
                        PageNumber = page.Number,
                        Text = page.Text,
                        Vector = vector.Vector,
                    };

                    await aISearchCollection.UpsertAsync(ingestedChunk);
                }

                return Results.Ok();
            }
        );

        api.MapPost("/chat", Chat);
    }

    private static async IAsyncEnumerable<AiResponseDto> Chat(
        IChatClient chatClient,
        List<ChatMessage> messages,
        AISearchToolService searchService,
        [FromBody] string query
    )
    {
        ChatOptions chatOptions = new()
        {
            Tools = [AIFunctionFactory.Create(searchService.SearchAsync)],
        };

        messages.Add(new ChatMessage(ChatRole.User, query));

        var responseText = new TextContent(string.Empty);
        var currentResponseMessage = new ChatMessage(ChatRole.Assistant, [responseText]);

        await foreach (var update in chatClient.GetStreamingResponseAsync(messages, chatOptions))
        {
            responseText.Text += update.Text;
            yield return new AiResponseDto(update.Text);
        }

        messages.Add(currentResponseMessage);
    }
}