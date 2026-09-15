namespace Chatbot.Domain
{
    public class Gatilho
    {
        public int Id { get; set; }
        public required Resposta Resposta { get; set; }
        public required string Nome { get; set; }

        public List<Chave> Chaves { get; set; } = [];
    }
}