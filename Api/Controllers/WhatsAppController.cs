using Chatbot.Application.Services;
using Chatbot.Domain;
using Chatbot.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Twilio.AspNet.Core;

namespace Chatbot.Api.Controllers
{
    [ApiController]
    [Route("api/whatsapp")]
    public class WhatsAppController : ControllerBase
    {
        private readonly ChatbotDbContext _context;
        private readonly MatchingService _matching;
        private readonly IConfiguration _configuration;

        public WhatsAppController(ChatbotDbContext context, MatchingService matching, IConfiguration configuration)
        {
            _context = context;
            _matching = matching;
            _configuration = configuration;
        }

        [HttpPost("webhook")]
        [ValidateRequest]
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
                return Content(TransferirChamada(), "application/xml; charset=utf-8");

            var twiml = $"""
                <?xml version="1.0" encoding="UTF-8"?>
                    <Response>
                        <Message>{gatilhoEncontrado.Resposta.Texto}</Message>
                    </Response>
                """;

            return Content(twiml, "application/xml; charset=utf-8");   
        }

        [HttpPatch("historico/{id:int}/atender")]
        public async Task<IActionResult> Atender(int id)
        {
            var key = Request.Headers["X-Api-Key"];
            var apiKey = _configuration["ApiKey"];

            if(key != apiKey)
                return Unauthorized("Acesso Negado!");

            var historico = await _context.Historicos
                            .FirstOrDefaultAsync(x => x.Id == id);

            if(historico == null)
                return NotFound("Histórico de conversa não encontrado. Verifique o ID.");

            historico.AtendidoEm = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            
            return NoContent();
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