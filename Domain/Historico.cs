namespace Chatbot.Domain
{
    public class Historico
    {
        public int Id { get; set; }
        public int? IdGatilho { get; set; }
        public required string Texto { get; set; }
        public required string NumeroWhatsApp { get; set; }
        public DateTime CriadoEm { get; set; }
        public DateTime? AtendidoEm { get; set; }
    }
}