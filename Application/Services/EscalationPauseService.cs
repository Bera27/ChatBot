using Chatbot.Domain;

namespace Chatbot.Application.Services
{
    public class EscalationPauseService
    {
        public bool IsBotPaused(List<Historico> historicos, DateTime now, TimeSpan pauseDuration)
        {
            return historicos.Any(x => x.IdGatilho == null &&
                                x.AtendidoEm == null &&
                                now - x.CriadoEm < pauseDuration);
        }
    }
}