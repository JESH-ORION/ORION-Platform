namespace ORION.Application.Pacientes;

public sealed class PacienteReadModel
{
    public Guid Id { get; set; }
    public Guid UsuarioId { get; set; }
    public string NomeCompleto { get; set; } = string.Empty;
    public string? Cpf { get; set; }
    public string Status { get; set; } = string.Empty;
}
