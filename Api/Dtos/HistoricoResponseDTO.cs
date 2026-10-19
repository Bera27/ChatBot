namespace ChatBot.Api.Dtos
{
    public class HistoricoResponseDTO
    {
        public record HistoricoResponse(int Id, string NumeroWhatsApp, string Texto, DateTime CriadoEm);
    }
}