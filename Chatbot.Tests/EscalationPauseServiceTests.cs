using Chatbot.Application.Services;
using Chatbot.Domain;

namespace Chatbot.Tests
{
    public class EscalationPauseServiceTests
    {
        [Fact]
        public void IsBotPaused_EscalatedUnattendedRecent_ReturnsTrue()
        {
            var now = new DateTime (2026, 10, 9, 12, 0, 0);

            var historico = new List<Historico>
            {
                new() {Texto = "Texto", 
                    NumeroWhatsApp = "1599999999", 
                    CriadoEm = now.AddMinutes(-30), 
                    IdGatilho = null, 
                    AtendidoEm = null}
            };

            var escalationPause = new EscalationPauseService();

            var result = escalationPause.IsBotPaused(historico, now, TimeSpan.FromHours(2));

            Assert.True(result);
        }

        [Fact]
        public void IsBotPaused_EscalatedUnattendedNotRecent_ReturnsFalse()
        {
            var now = new DateTime (2026, 10, 9, 12, 0, 0);

            var historico = new List<Historico>
            {
                new() {Texto = "Texto", 
                    NumeroWhatsApp = "1599999999", 
                    CriadoEm = now.AddHours(-3), 
                    IdGatilho = null, 
                    AtendidoEm = null}
            };

            var escalationPause = new EscalationPauseService();

            var result = escalationPause.IsBotPaused(historico, now, TimeSpan.FromHours(2));

            Assert.False(result);
        }

        [Fact]
        public void IsBotPaused_EscalatedAttended_ReturnsFalse()
        {
            var now = new DateTime (2026, 10, 9, 12, 0, 0);

            var historico = new List<Historico>
            {
                new() {Texto = "Texto", 
                    NumeroWhatsApp = "1599999999", 
                    CriadoEm = now.AddMinutes(-30), 
                    IdGatilho = null, 
                    AtendidoEm = now.AddMinutes(-10)}
            };

            var escalationPause = new EscalationPauseService();

            var result = escalationPause.IsBotPaused(historico, now, TimeSpan.FromHours(2));

            Assert.False(result);
        }

        [Fact]
        public void IsBotPaused_RecentUnattendedButMatchedGatilho_ReturnsFalse()
        {
            var now = new DateTime (2026, 10, 9, 12, 0, 0);

            var historico = new List<Historico>
            {
                new() {Texto = "Texto", 
                    NumeroWhatsApp = "1599999999", 
                    CriadoEm = now.AddMinutes(-30), 
                    IdGatilho = 1, 
                    AtendidoEm = null}
            };


            var escalationPause = new EscalationPauseService();

            var result = escalationPause.IsBotPaused(historico, now, TimeSpan.FromHours(2));

            Assert.False(result);
        }

        [Fact]
        public void IsBotPaused_ListEmpty_ReturnsFalse()
        {
            var now = new DateTime (2026, 10, 9, 12, 0, 0);

            var historico = new List<Historico>();


            var escalationPause = new EscalationPauseService();

            var result = escalationPause.IsBotPaused(historico, now, TimeSpan.FromHours(2));

            Assert.False(result);
        }
    }
}