using Azure;
using Azure.AI.OpenAI;
using Azure.Search.Documents.Indexes;
using BlazorBYOD.Server.Models;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel.Connectors.AzureAISearch;

namespace BlazorBYOD.Server.Helpers;

public static class ServiceRegistration
{
    public static void AddAI(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. Search client
        services.AddSingleton(_ =>
        {
            var searchEndpoint = configuration["Azure:SearchEndpoint"];
            var searchApiKey = configuration["Azure:SearchApiKey"];
            var searchIndexClient = new SearchIndexClient(
                new Uri(searchEndpoint),
                new AzureKeyCredential(searchApiKey)
            );
            return new AzureAISearchVectorStore(searchIndexClient).GetCollection<
                string,
                IngestedChunk
            >(nameof(IngestedChunk).ToLowerInvariant());
        });

        // 2. Azure OpenAI client
        services.AddSingleton(_ =>
        {
            var openAiEndpoint = configuration["Azure:AIEndpoint"];
            var openAiKey = configuration["Azure:AIApiKey"];
            return new AzureOpenAIClient(
                endpoint: new Uri(openAiEndpoint),
                credential: new AzureKeyCredential(openAiKey)
            );
        });

        // 3. Chat client (deployment name from config)
        services.AddChatClient(serviceProvider =>
        {
            var deploymentName = configuration["Azure:ChatDeploymentName"];
            return serviceProvider
                .GetRequiredService<AzureOpenAIClient>()
                .GetChatClient(deploymentName)
                .AsIChatClient()
                .AsBuilder()
                .UseFunctionInvocation()
                .Build();
        });

        // 4. Embedding generator (deployment name from config)
        services.AddEmbeddingGenerator(serviceProvider =>
        {
            var deploymentName = configuration["Azure:EmbeddingDeploymentName"];
            return serviceProvider
                .GetRequiredService<AzureOpenAIClient>()
                .GetEmbeddingClient(deploymentName)
                .AsIEmbeddingGenerator();
        });

        services.AddSingleton(
            new List<ChatMessage>
            {
                new(
                    ChatRole.System,
                    @"
        You are an assistant who answers questions about information you retrieve.
        Do not answer questions about anything else.
        Use only simple markdown to format your responses.

        Use the search tool to find relevant information. When you do this, end your
        reply with citations in the special XML format:

        <citation filename='string' page_number='number'>exact quote here</citation>

        Always include the citation in your response if there are results.

        The quote must be max 5 words, taken word-for-word from the search result, and is the basis for why the citation is relevant.
        Don't refer to the presence of citations; just emit these tags right at the end, with no surrounding text.
        "
                ),
            }
        );

        services.AddSingleton<AISearchToolService>();
    }
}