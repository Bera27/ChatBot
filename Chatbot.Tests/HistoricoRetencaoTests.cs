using Chatbot.Domain;
using Chatbot.Infrastructure;
using Chatbot.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Chatbot.Tests
{
    public class HistoricoRetencaoTests
    {
        [Fact]
        public async Task HistoricoRetencao_OldAndRecentRecords_DeletesOnlyOldOne()
        {
            using var conexao = new SqliteConnection("Data Source=:memory:");
            conexao.Open();

            using var context = CriaContext(conexao);

            var agora = new DateTime(2026, 10, 23);
            var retencao = new HistoricoRetencao();

            var historicoVelho = new Historico
            {
                Texto = "Alguma coisa",
                NumeroWhatsApp = "99999999999",
                CriadoEm = agora.AddDays(-100)
            };

            var historicoRecente = new Historico
            {
                Texto = "Alguma coisa",
                NumeroWhatsApp = "99999999999",
                CriadoEm = agora.AddDays(-10)
            };

            context.Historicos.AddRange(historicoVelho, historicoRecente);
            await context.SaveChangesAsync();

            var result = await retencao.LimparAsync(context, agora, 90);

            Assert.Equal(1, result);

            var restantes = await context.Historicos
                        .AsNoTracking()
                        .ToListAsync();

            Assert.Single(restantes);
            Assert.Equal(agora.AddDays(-10), restantes[0].CriadoEm);
        }

        [Fact]
        public async Task HistoricoRetencao_ZeroDays_DoNotDeleteAnything()
        {
            using var conexao = new SqliteConnection("Data Source=:memory:");
            conexao.Open();

            using var context = CriaContext(conexao);

            var agora = new DateTime(2026, 10, 23);
            var retencao = new HistoricoRetencao();


            var historico = new Historico
            {
                Texto = "Alguma coisa",
                NumeroWhatsApp = "99999999999",
                CriadoEm = agora.AddDays(-100)
            };
        
            context.Historicos.Add(historico);
            await context.SaveChangesAsync();

            var result = await retencao.LimparAsync(context, agora, 0);

            Assert.Equal(0, result);

            var restantes = await context.Historicos
                        .AsNoTracking()
                        .ToListAsync();

            Assert.Single(restantes);
            Assert.Equal(agora.AddDays(-100), restantes[0].CriadoEm);
        }

        [Fact]
        public async Task HistoricoRetencao_RegistrationAtTheExactLimit_DoNotDeleteAnything()
        {
            using var conexao = new SqliteConnection("Data Source=:memory:");
            conexao.Open();

            using var context = CriaContext(conexao);

            var agora = new DateTime(2026, 10, 23);
            var retencao = new HistoricoRetencao();


            var historico = new Historico
            {
                Texto = "Alguma coisa",
                NumeroWhatsApp = "99999999999",
                CriadoEm = agora.AddDays(-90)
            };
        
            context.Historicos.Add(historico);
            await context.SaveChangesAsync();

            var result = await retencao.LimparAsync(context, agora, 90);

            Assert.Equal(0, result);

            var restantes = await context.Historicos
                        .AsNoTracking()
                        .ToListAsync();

            Assert.Single(restantes);
            Assert.Equal(agora.AddDays(-90), restantes[0].CriadoEm);
        }

        private static ChatbotDbContext CriaContext(SqliteConnection conexao)
        {
            var opcoes = new DbContextOptionsBuilder<ChatbotDbContext>()
                        .UseSqlite(conexao)
                        .Options;

            var context = new ChatbotDbContext(opcoes);
            context.Database.EnsureCreated();
            return context;
        }   
    }
}