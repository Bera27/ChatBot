using Chatbot.Application.Services;
using Chatbot.Domain;

namespace Chatbot.Tests
{
    public class MatchingServiceTests
    {
        [Fact]
        public void MatchingService_MessageContainsRegisteredKeyword_ReturnCorrectTrigger()
        {
            var resposta = new Resposta
            {
                Texto = "10h a 19h"
            };

            var chaves = new List<Chave>
            {
                new() {Texto = "Horário"}
            };
            
            var gatilhos = new List<Gatilho>
            {
                new Gatilho
                {
                    Nome = "horario",
                    Resposta = resposta,
                    Chaves = chaves
                }
            };

            var matching = new MatchingService();

            var result = matching.Matching("qual o Horário de voces", gatilhos);

            Assert.NotNull(result);
            Assert.Equal("horario", result.Nome);
        }

        [Fact]
        public void MatchingService_MessageWithoutAccents_ReturnCorrectTrigger()
        {
            var resposta = new Resposta
            {
                Texto = "10h a 19h"
            };

            var chaves = new List<Chave>
            {
                new() {Texto = "Horário"}
            };
            
            var gatilhos = new List<Gatilho>
            {
                new Gatilho
                {
                    Nome = "horario",
                    Resposta = resposta,
                    Chaves = chaves
                }
            };

            var matching = new MatchingService();

            var result = matching.Matching("qual o HORARIO de voces", gatilhos);

            Assert.NotNull(result);
            Assert.Equal("horario", result.Nome);
        }

        [Fact]
        public void MatchingService_MessageWithoutMatchingKeyword_ReturnNull()
        {
            var resposta = new Resposta
            {
                Texto = "10h a 19h"
            };

            var chaves = new List<Chave>
            {
                new() {Texto = "Horário"}
            };
            
            var gatilhos = new List<Gatilho>
            {
                new Gatilho
                {
                    Nome = "horario",
                    Resposta = resposta,
                    Chaves = chaves
                }
            };

            var matching = new MatchingService();

            var result = matching.Matching("bom dia", gatilhos);

            Assert.Null(result);
        }
    }
}