using Chatbot.Application.Services;
using Chatbot.Domain;
using Chatbot.Infrastructure.Data;
using Chatbot.Infrastructure.Interfaces;
using ChatBot.Api.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
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
        private readonly IWhatsAppNotifier _whatsAppNotifier;
        private readonly EscalationPauseService _escalation;

        public WhatsAppController(ChatbotDbContext context, MatchingService matching, IConfiguration configuration, IWhatsAppNotifier whatsAppNotifier, EscalationPauseService escalation)
        {
            _context = context;
            _matching = matching;
            _configuration = configuration;
            _whatsAppNotifier = whatsAppNotifier;
            _escalation = escalation;
        }

        [HttpPost("webhook")]
        [ValidateRequest]
        public async Task<IActionResult> Webhook()
        {
            var form = Request.Form;
            var body = form["body"].ToString();
            var sender = form["From"].ToString();

            var agora = DateTime.UtcNow;
            var horas = _configuration.GetValue<double>("Escalation:PauseHours", 2);
            var duracao = TimeSpan.FromHours(horas);
            var limite = agora - duracao;

            var buscaHistorico = await _context.Historicos
                                    .Where(x => x.NumeroWhatsApp == sender &&
                                            x.CriadoEm > limite)
                                    .ToListAsync();

            var pause = _escalation.IsBotPaused(buscaHistorico, agora, duracao);

            if(pause)
                return Content("<Response></Response>", "application/xml; charset=utf-8");

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
                CriadoEm = agora
            };

            _context.Historicos.Add(historico);
            await _context.SaveChangesAsync();

            if(gatilhoEncontrado == null)
            {
                await _whatsAppNotifier.NotificarAsync(sender);
                return Content(TransferirChamada(), "application/xml; charset=utf-8");
            }

            var twiml = $"""
                <?xml version="1.0" encoding="UTF-8"?>
                    <Response>
                        <Message>{gatilhoEncontrado.Resposta.Texto}</Message>
                    </Response>
                """;

            return Content(twiml, "application/xml; charset=utf-8");   
        }

        [HttpPatch("historico/{id:int}/atender")]
        [ApiKey]
        public async Task<IActionResult> Atender(int id)
        {
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