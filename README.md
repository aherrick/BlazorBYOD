# Blazor AI PDF Search Demo

Showcase for Azure AI Search + Azure OpenAI with PDF upload and search.  
Minimal UI, backend tech demo only.

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
        "OpenAIEndpoint": "https://your-openai-resource.openai.azure.com/",
        "OpenAIApiKey": "your-openai-api-key",
        "ChatDeploymentName": "gpt-4o",
        "EmbeddingDeploymentName": "text-embedding-3-small"
      }
    }
    ```

Set your values using `dotnet user-secrets set` commands, or edit the secrets file directly.
