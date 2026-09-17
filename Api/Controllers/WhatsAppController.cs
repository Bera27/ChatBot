using Chatbot.Application.Services;
using Chatbot.Domain;
using Chatbot.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Chatbot.Api.Controllers
{
    [ApiController]
    [Route("api/whatsapp")]
    public class WhatsAppController : ControllerBase
    {
        private readonly ChatbotDbContext _context;
        private readonly MatchingService _matching;

        public WhatsAppController(ChatbotDbContext context, MatchingService matching)
        {
            _context = context;
            _matching = matching;
        }

        [HttpPost("webhook")]
        public async Task<IActionResult> Webhook()
        {
            var form = Request.Form;
            var body = form["body"].ToString();
            var sender = form["From"];

            var gatilhos = await _context.Gatilhos
                            .Include(x => x.Chaves)
                            .Include(x => x.Resposta)
                            .ToListAsync();

            var gatilhoEncontrado = _matching.Matching(body, gatilhos);
            
            var historico = new Historico
            {
                IdGatilho = gatilhoEncontrado?.Id,
                Texto = body,
                NumeroWhatsApp = sender,
                CriadoEm = DateTime.UtcNow
            };

            _context.Historicos.Add(historico);
            await _context.SaveChangesAsync();

            if(gatilhoEncontrado == null)
                return Content(TransferirChamada(), "application/xml");

            var twiml = $"""
                <?xml version="1.0" encoding="UTF-8"?>
                    <Response>
                        <Message>{gatilhoEncontrado.Resposta.Texto}</Message>
                    </Response>
                """;

            return Content(twiml, "application/xml");   
        }

        private string TransferirChamada()
        {
            var transfirir = $"""
                            <?xml version="1.0" encoding="UTF-8"?>
                            <Response>
                                <Message>Não entendi sua mensagem, aguarde um momento enquanto transfiro sua chamada para um especialista.</Message>
                            </Response>
                            """;

            return transfirir;
        }
    }
}