using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ChatBot.Api.Filters
{
    public class ApiKeyFilter : IAuthorizationFilter
    {
        private readonly IConfiguration _configuration;

        public ApiKeyFilter(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        
        private static bool ValidarKeys(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right)
        {
            return CryptographicOperations.FixedTimeEquals(left, right);
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var key = context.HttpContext.Request.Headers["X-Api-Key"].ToString();
            var apiKey = _configuration["ApiKey"];

            if (string.IsNullOrEmpty(apiKey))
            {
                context.Result = new UnauthorizedObjectResult("Acesso Negado!");
                return;
            }

            var keyInByte = Encoding.UTF8.GetBytes(key);
            var apiKeyInByte = Encoding.UTF8.GetBytes(apiKey);

            if (!ValidarKeys(keyInByte, apiKeyInByte))
                context.Result = new UnauthorizedObjectResult("Acesso Negado!");
        }
    }
}