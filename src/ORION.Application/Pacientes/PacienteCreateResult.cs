namespace ORION.Application.Pacientes;

public enum PacienteCreateStatus
{
    Criado = 1,
    UsuarioNaoEncontrado = 2,
    UsuarioJaPossuiPaciente = 3,
    CpfDuplicado = 4
}

public sealed record PacienteCreateResult(
    PacienteCreateStatus Status,
    PacienteReadModel? Paciente = null);
