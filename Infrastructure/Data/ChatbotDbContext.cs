using Chatbot.Domain;
using Microsoft.EntityFrameworkCore;

namespace Chatbot.Infrastructure.Data
{
    public class ChatbotDbContext : DbContext
    {
        public DbSet<Chave> Chaves { get; set; }
        public DbSet<Gatilho> Gatilhos { get; set; }
        public DbSet<Historico> Historicos { get; set; }
        public DbSet<Resposta> Respostas { get; set; }

        public ChatbotDbContext(DbContextOptions<ChatbotDbContext> options) : base(options) {}

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Gatilho>()
                .HasMany(g => g.Chaves)
                .WithOne()
                .HasForeignKey(c => c.IdGatilho);

            modelBuilder.Entity<Gatilho>()
                .HasOne(g => g.Resposta)
                .WithOne()
                .HasForeignKey<Resposta>(r => r.IdGatilho);

                modelBuilder.Entity<Gatilho>().HasData(
                    new Gatilho { Id = 1, Nome = "Horário", Resposta = null!});

                modelBuilder.Entity<Resposta>().HasData(
                    new Resposta { Id = 1, IdGatilho = 1, Texto = "Funcionamos de segunda a sexta, das 8h às 18h." });

                modelBuilder.Entity<Chave>().HasData(
                    new Chave { Id = 1, IdGatilho = 1, Texto = "horario" },
                    new Chave { Id = 2, IdGatilho = 1, Texto = "horas" });
        }
    }
}