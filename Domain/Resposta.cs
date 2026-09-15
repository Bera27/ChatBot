namespace Chatbot.Domain
{
    public class Resposta
    {
        public int Id { get; set; }
        public int IdGatilho { get; set; }
        public required string Texto { get; set; }
    }
}