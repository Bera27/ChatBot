using Chatbot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Chatbot.Infrastructure
{
    public class HistoricoRetencao
    {
        public async Task<int> LimparAsync(ChatbotDbContext context, DateTime agora, int dias)
        {
            if(dias <= 0)
                return 0;

            var limite = agora - TimeSpan.FromDays(dias);

            return await context.Historicos
                        .Where(x => x.CriadoEm < limite)
                        .ExecuteDeleteAsync();
        }
    }
}