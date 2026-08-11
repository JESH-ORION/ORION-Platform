using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ORION.Application.IA;

namespace ORION.Infrastructure.AI;

public sealed class OpenAIModeloIAProvider(
    HttpClient httpClient,
    OpenAIModeloIAOptions options) : IModeloIAProvider
{
    private const string Instructions = """
        Você é o agente de triagem de saúde da Plataforma ORION.
        Sua função é coletar e organizar informações relatadas pelo paciente e orientar próximos passos com prudência.
        Não forneça diagnóstico definitivo, não prescreva medicamentos e não substitua avaliação de profissional de saúde.
        Faça perguntas objetivas para esclarecer sintomas, duração, intensidade, fatores associados e sinais de alerta.
        Se houver relato potencialmente grave ou emergencial, oriente procura imediata por atendimento de urgência.
        Responda em português do Brasil, de forma clara, curta e acolhedora.
        """;

    public string NomeModelo => options.Model;

    public async Task<ModeloIAResponse> GerarRespostaAsync(
        ModeloIARequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(options.ApiKey))
            throw new InvalidOperationException("OpenAI:ApiKey não configurada.");

        if (string.IsNullOrWhiteSpace(options.Model))
            throw new InvalidOperationException("OpenAI:Model não configurado.");

        var payload = new
        {
            model = options.Model,
            instructions = Instructions,
            input = request.MensagemUsuario,
            store = false
        };

        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"{options.BaseUrl.TrimEnd('/')}/responses");

        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);
        httpRequest.Content = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json");

        using var response = await httpClient.SendAsync(httpRequest, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"OpenAI Responses API retornou HTTP {(int)response.StatusCode}: {ExtrairErro(responseBody)}");
        }

        var conteudo = ExtrairTexto(responseBody);
        if (string.IsNullOrWhiteSpace(conteudo))
            throw new InvalidOperationException("OpenAI Responses API não retornou conteúdo textual.");

        return new ModeloIAResponse(conteudo.Trim());
    }

    private static string ExtrairTexto(string json)
    {
        using var document = JsonDocument.Parse(json);

        if (!document.RootElement.TryGetProperty("output", out var output) ||
            output.ValueKind != JsonValueKind.Array)
            return string.Empty;

        var textos = new List<string>();

        foreach (var item in output.EnumerateArray())
        {
            if (!item.TryGetProperty("type", out var itemType) ||
                itemType.GetString() != "message" ||
                !item.TryGetProperty("content", out var content) ||
                content.ValueKind != JsonValueKind.Array)
                continue;

            foreach (var part in content.EnumerateArray())
            {
                if (!part.TryGetProperty("type", out var partType) ||
                    partType.GetString() != "output_text" ||
                    !part.TryGetProperty("text", out var text))
                    continue;

                var value = text.GetString();
                if (!string.IsNullOrWhiteSpace(value))
                    textos.Add(value);
            }
        }

        return string.Join(Environment.NewLine, textos);
    }

    private static string ExtrairErro(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.TryGetProperty("error", out var error) &&
                error.TryGetProperty("message", out var message))
                return message.GetString() ?? "erro sem mensagem";
        }
        catch (JsonException)
        {
            // Mantém fallback abaixo.
        }

        return string.IsNullOrWhiteSpace(json) ? "resposta vazia" : json;
    }
}
