namespace ORION.Application.Mensagens;

public sealed class MensagemReadModel
{
    public Guid Id { get; set; }
    public Guid ConversaId { get; set; }
    public Guid? UsuarioId { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string Conteudo { get; set; } = string.Empty;
    public DateTime DataEnvio { get; set; }
    public DateTime? LidaEm { get; set; }
}
