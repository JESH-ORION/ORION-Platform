namespace ORION.Application.Conversas;

public sealed class ConversaReadModel
{
    public Guid Id { get; set; }
    public Guid? PacienteId { get; set; }
    public Guid? ProfissionalId { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime DataCriacao { get; set; }
    public DateTime DataAtualizacao { get; set; }
}
