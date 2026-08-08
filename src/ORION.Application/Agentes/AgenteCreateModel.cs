namespace ORION.Application.Agentes;

public sealed class AgenteCreateModel
{
    public string Identificador { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string Versao { get; set; } = string.Empty;
    public string? Finalidade { get; set; }
}
