namespace ORION.Application.Agentes;

public sealed class AgenteReadModel
{
    public Guid Id { get; set; }
    public string Identificador { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string Versao { get; set; } = string.Empty;
    public string? Finalidade { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime DataCriacao { get; set; }
    public DateTime DataAtualizacao { get; set; }
}
