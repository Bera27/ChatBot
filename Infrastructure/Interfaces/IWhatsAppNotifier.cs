using Twilio;
using Twilio.Rest.Api.V2010.Account;

namespace Chatbot.Infrastructure.Interfaces
{
    public interface IWhatsAppNotifier
    {
        Task<bool> NotificarAsync(string customerNumber);
    }
}