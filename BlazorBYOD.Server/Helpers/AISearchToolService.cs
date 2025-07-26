using System.ComponentModel;
using BlazorBYOD.Server.Models;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Connectors.AzureAISearch;

namespace BlazorBYOD.Server.Helpers;

public class AISearchToolService(
    AzureAISearchCollection<string, IngestedChunk> searchCollection,
    IEmbeddingGenerator<string, Embedding<float>> embeddingClient
)
{
    [Description("Searches for information using a phrase or keyword")]
    public async Task<IEnumerable<string>> SearchAsync(
        [Description("The phrase to search for.")] string query
    )
    {
        var queryVector = await embeddingClient.GenerateAsync(query);

        var results = await searchCollection
            .SearchAsync(queryVector, top: 5, new VectorSearchOptions<IngestedChunk>())
            .ToListAsync();

        return results.Select(result =>
            $"<result filename=\"{result.Record.DocumentId}\" page_number=\"{result.Record.PageNumber}\">{result.Record.Text}</result>"
        );
    }
}