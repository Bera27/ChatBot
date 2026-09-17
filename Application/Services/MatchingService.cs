using System.Globalization;
using System.Text;
using Chatbot.Domain;

namespace Chatbot.Application.Services
{
    public class MatchingService
    {
        public Gatilho? Matching(string mensagem, List<Gatilho>  gatilhos)
        {
            var bodyNormalizado = NormalizarTexto(mensagem);

            var gatilhoEncontrado = gatilhos.FirstOrDefault(x => x.Chaves.Any(chave => bodyNormalizado.Contains(NormalizarTexto(chave.Texto))));

            return gatilhoEncontrado;
        }

        private string NormalizarTexto(string mensagem)
        {
            var textoNormalizado = mensagem.Normalize(NormalizationForm.FormD).ToLower();

            StringBuilder stringBuilder = new();

            foreach (var c in textoNormalizado)
            {
                if(CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    stringBuilder.Append(c);
            }

            return stringBuilder.ToString();
        }
    }
}