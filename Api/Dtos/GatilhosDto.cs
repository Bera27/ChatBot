namespace ChatBot.Api.Dtos
{
    public class GatilhosDto
    {
        public record GatilhoRequest(string Nome,
                                    List<string> Chaves,
                                    string Resposta);

        public record GatilhoResponse(int Id,
                                    string Nome,
                                    List<string> Chaves,
                                    string Resposta);
    }
}