namespace ORION.Infrastructure.AI;

public sealed class OpenAIModeloIAOptions
{
    public string ApiKey { get; init; } = string.Empty;
    public string Model { get; init; } = "gpt-5-mini";
    public string BaseUrl { get; init; } = "https://api.openai.com/v1";
}
