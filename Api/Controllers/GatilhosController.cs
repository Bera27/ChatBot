using Chatbot.Domain;
using Chatbot.Infrastructure.Data;
using ChatBot.Api.Dtos;
using ChatBot.Api.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ChatBot.Api.Controllers
{
    [ApiController]
    [Route("api/[Controller]")]
    [ApiKey]
    public class GatilhosController : ControllerBase
    {
        private readonly ChatbotDbContext _context;

        public GatilhosController(ChatbotDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAsync()
        {
            var gatilhos = await _context.Gatilhos
                            .AsNoTracking()
                            .Select(x => new GatilhosDto.GatilhoResponse
                            (
                                x.Id,
                                x.Nome,
                                x.Chaves.Select(c => c.Texto).ToList(),
                                x.Resposta.Texto
                            )).ToListAsync();

            return Ok(gatilhos);
        }

        [HttpPost]
        public async Task<IActionResult> PostAsync([FromBody] GatilhosDto.GatilhoRequest model)
        {
            if(model is null ||
               string.IsNullOrWhiteSpace(model.Nome) ||
               model.Chaves is null ||
               !model.Chaves.Any() ||
               model.Chaves.Any(string.IsNullOrWhiteSpace) ||
               string.IsNullOrWhiteSpace(model.Resposta))
            {
                return BadRequest("Dados inválidos. Certifique-se de que o nome, a resposta e todas as chaves foram preenchidos corretamente.");
            }

            var gatilho = new Gatilho
            {
                Nome = model.Nome.Trim(),
                Chaves = [.. model.Chaves.Select(texto => new Chave {Texto = texto.Trim()})],
                Resposta = new Resposta {Texto = model.Resposta.Trim()}
            };

            _context.Gatilhos.Add(gatilho);
            await _context.SaveChangesAsync();

            return Created($"/api/gatilhos/{gatilho.Id}", new GatilhosDto.GatilhoResponse
            (
                gatilho.Id,
                gatilho.Nome,
                gatilho.Chaves.Select(c => c.Texto).ToList(),
                gatilho.Resposta.Texto
            ));
        }
    }
}