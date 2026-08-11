namespace ORION.Application.Pacientes;

public interface IPacienteRepository
{
    Task<PacienteReadModel?> ObterPorIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<PacienteCreateResult> CriarAsync(
        PacienteCreateModel model,
        CancellationToken cancellationToken = default);
}
