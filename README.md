# Blazor AI Bring Your Own Data Search Demo

![dotnet](https://github.com/aherrick/BlazorBYOD/actions/workflows/dotnet.yml/badge.svg)

Showcase for Azure AI Search + Azure OpenAI with PDF upload and search.  

## Configuration

1. **Initialize user secrets** in the Server project folder:

    ```sh
    dotnet user-secrets init
    ```

2. **User secrets JSON structure:**

    ```json
    {
      "AzureAI": {
        "SearchEndpoint": "https://your-search-resource.search.windows.net",
        "SearchApiKey": "your-search-api-key",
        "AIEndpoint": "https://your-openai-resource.openai.azure.com/",
        "AIApiKey": "your-openai-api-key",
        "ChatDeploymentName": "gpt-4o",
        "EmbeddingDeploymentName": "text-embedding-3-small"
      }
    }
    ```

Set your values using `dotnet user-secrets set` commands, or edit the secrets file directly.
