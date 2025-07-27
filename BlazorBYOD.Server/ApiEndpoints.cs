using BlazorBYOD.Server.Helpers;
using BlazorBYOD.Server.Models;
using BlazorBYOD.Shared;
using DocumentFormat.OpenXml.Packaging;
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
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

                IEnumerable<(string Text, int PageNumber)> chunks = null;

                if (extension == ".txt")
                {
                    using var reader = new StreamReader(file.OpenReadStream());
                    var text = await reader.ReadToEndAsync();
                    chunks = [(Text: text.Trim(), PageNumber: 1)];
                }
                else if (extension == ".pdf" || extension == ".docx")
                {
                    using var memoryStream = new MemoryStream();
                    await file.CopyToAsync(memoryStream);
                    memoryStream.Position = 0;

                    if (extension == ".pdf")
                    {
                        using var document = PdfDocument.Open(memoryStream);
                        chunks = document.GetPages().Select(p => (p.Text, PageNumber: p.Number));
                    }
                    else // .docx
                    {
                        using var wordDoc = WordprocessingDocument.Open(memoryStream, false);
                        var body = wordDoc.MainDocumentPart?.Document?.Body;

                        var text =
                            body == null
                                ? ""
                                : string.Join(
                                    "\n",
                                    body.Descendants<DocumentFormat.OpenXml.Wordprocessing.Text>()
                                        .Select(t => t.Text)
                                );

                        chunks = [(Text: text.Trim(), PageNumber: 1)];
                    }
                }

                if (chunks != null)
                {
                    foreach (var (text, pageNumber) in chunks)
                    {
                        if (string.IsNullOrWhiteSpace(text))
                            continue;

                        var embedding = await embeddingClient.GenerateAsync(text);

                        var ingestedChunk = new IngestedChunk
                        {
                            Key = Guid.NewGuid().ToString(),
                            DocumentId = file.FileName,
                            PageNumber = pageNumber,
                            Text = text,
                            Vector = embedding.Vector,
                        };

                        await aISearchCollection.UpsertAsync(ingestedChunk);
                    }
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