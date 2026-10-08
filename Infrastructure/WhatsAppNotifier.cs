using Chatbot.Infrastructure.Interfaces;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace Chatbot.Infrastructure
{
    public class WhatsAppNotifier : IWhatsAppNotifier
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<WhatsAppNotifier> _logger;

        public WhatsAppNotifier(IConfiguration configuration, ILogger<WhatsAppNotifier> logger)
            {
                _configuration = configuration;
                _logger = logger;

                var accountSid = _configuration["Twilio:AccountSid"];
                var authToken = _configuration["Twilio:AuthToken"];

                try
                {
                    TwilioClient.Init(accountSid, authToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Falha ao inicializar o cliente Twilio. Verifique Twilio:AccountSid e Twilio:AuthToken");
                }
            }

        public async Task<bool> NotificarAsync(string customerNumber)
        {
            try
            {
                var systemTwilioNumber = _configuration["Twilio:WhatsAppNumber"];
                var agentNumber = _configuration["Notification:AgentNumber"];

                string textBody = $"Atenção: O cliente {customerNumber} precisa de atendimento humano no chat.";

                var message = await MessageResource.CreateAsync(
                    body: textBody,
                    from: new PhoneNumber($"whatsapp:{systemTwilioNumber}"),
                    to: new PhoneNumber($"whatsapp:{agentNumber}")
                );

                return !string.IsNullOrEmpty(message.Sid);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao enviar notificação WhatsApp para o atendente sobre o cliente");
                return false;
            }
        }
    }
}