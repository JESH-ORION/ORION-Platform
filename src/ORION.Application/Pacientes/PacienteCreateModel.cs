namespace ORION.Application.Pacientes;

public sealed class PacienteCreateModel
{
    public Guid UsuarioId { get; set; }
    public string NomeCompleto { get; set; } = string.Empty;
    public string? Cpf { get; set; }
}
