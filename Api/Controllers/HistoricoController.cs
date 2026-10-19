using Chatbot.Infrastructure.Data;
using ChatBot.Api.Dtos;
using ChatBot.Api.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ChatBot.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [ApiKey]
    public class HistoricoController : ControllerBase
    {
        private readonly ChatbotDbContext _context;

        public HistoricoController(ChatbotDbContext context)
        {
            _context = context;
        }

        [HttpGet("pendentes")]
        public async Task<ActionResult> ObterPendentes()
        {
            var historicoPendentes = await _context.Historicos
                            .AsNoTracking()
                            .Where(x => x.AtendidoEm == null && x.IdGatilho == null)
                            .OrderBy(x => x.CriadoEm)
                            .Select(x => new HistoricoResponseDTO.HistoricoResponse
                            (
                                x.Id,
                                x.NumeroWhatsApp,
                                x.Texto,
                                x.CriadoEm
                            )).ToListAsync();

            return Ok(historicoPendentes);
        }
    }
}