using Chatbot.Domain;
using Chatbot.Infrastructure.Data;
using ChatBot.Api.Dtos;
using ChatBot.Api.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace ChatBot.Api.Controllers
{
    [ApiController]
    [Route("api/[Controller]")]
    [ApiKey]
    [EnableRateLimiting("painel")]
    public class GatilhosController : ControllerBase
    {
        private readonly ChatbotDbContext _context;
        private const int NomeMax = 80;
        private const int ChavesMax = 30;
        private const int ChaveTamanhoMax = 50;
        private const int RespostaMax = 1600;

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

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetByIdAsync([FromRoute] int id)
        {
            var gatilho = await _context.Gatilhos
                            .AsNoTracking()
                            .Where(x => x.Id == id)
                            .Select(x => new GatilhosDto.GatilhoResponse
                            (
                                x.Id,
                                x.Nome,
                                x.Chaves.Select(c => c.Texto).ToList(),
                                x.Resposta.Texto
                            )).FirstOrDefaultAsync();

            if(gatilho == null)
                return NotFound("Gatilho não encontrado ou inexistente!");

            return Ok(gatilho);
        }

        [HttpPost]
        public async Task<IActionResult> PostAsync([FromBody] GatilhosDto.GatilhoRequest model)
        {
            var erroValidacao = ValidarModel(model);
            if(erroValidacao != null)
            {
                return BadRequest(erroValidacao);
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
                [.. gatilho.Chaves.Select(c => c.Texto)],
                gatilho.Resposta.Texto
            ));
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> PutAsync([FromRoute] int id, [FromBody] GatilhosDto.GatilhoRequest model)
        {
            var erroValidacao = ValidarModel(model);
            if(erroValidacao != null)
            {
                return BadRequest(erroValidacao);
            }

            var gatilho = await _context.Gatilhos
                            .Where(x => x.Id == id)
                            .Include(x => x.Chaves)
                            .Include(x => x.Resposta)
                            .FirstOrDefaultAsync();

            if(gatilho == null)
                return NotFound("Gatilho não encontrado ou inexistente!");

            gatilho.Nome = model.Nome.Trim();
            gatilho.Chaves = [.. model.Chaves.Select(texto => new Chave {Texto = texto.Trim()})];
            gatilho.Resposta.Texto = model.Resposta;

            await _context.SaveChangesAsync();

            return Ok(new GatilhosDto.GatilhoResponse
            (
                gatilho.Id,
                gatilho.Nome,
                [.. gatilho.Chaves.Select(c => c.Texto)],
                gatilho.Resposta.Texto
            ));
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteAsync([FromRoute] int id)
        {
            var gatilho = await _context.Gatilhos
                            .FirstOrDefaultAsync(x => x.Id == id);

            if(gatilho == null)
                return NotFound("Gatilho não encontrado ou inexistente!");
            
            _context.Gatilhos.Remove(gatilho);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private static string? ValidarModel(GatilhosDto.GatilhoRequest model)
        {
            if(model is null ||
               string.IsNullOrWhiteSpace(model.Nome) ||
               model.Chaves is null ||
               !model.Chaves.Any() ||
               model.Chaves.Any(string.IsNullOrWhiteSpace) ||
               string.IsNullOrWhiteSpace(model.Resposta))
            {
                return "Dados inválidos. Certifique-se de que o nome, a resposta e todas as chaves foram preenchidos corretamente.";
            }

            if (model.Nome.Trim().Length > NomeMax)
                return $"O nome pode ter no máximo {NomeMax} caracteres.";

            if (model.Chaves.Count > ChavesMax)
                return $"Use no máximo {ChavesMax} palavras por resposta.";

            if (model.Chaves.Any(c => c.Trim().Length > ChaveTamanhoMax))
                return $"Cada palavra pode ter no máximo {ChaveTamanhoMax} caracteres.";

            if (model.Resposta.Trim().Length > RespostaMax)
                return $"A resposta pode ter no máximo {RespostaMax} caracteres.";

            return null;
        }
    }
}