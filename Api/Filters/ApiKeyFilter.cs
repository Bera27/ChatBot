using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ChatBot.Api.Filters
{
    public class ApiKeyFilter : IAuthorizationFilter
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ApiKeyFilter> _logger;

        public ApiKeyFilter(IConfiguration configuration,ILogger<ApiKeyFilter> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }
        
        private static bool ValidarKeys(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right)
        {
            return CryptographicOperations.FixedTimeEquals(left, right);
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var key = context.HttpContext.Request.Headers["X-Api-Key"].ToString();
            var apiKey = _configuration["ApiKey"];

            var ip = context.HttpContext.Connection.RemoteIpAddress;
            var method = context.HttpContext.Request.Method;
            var path = context.HttpContext.Request.Path;

            if (string.IsNullOrEmpty(apiKey))
            {
                _logger.LogError("ApiKey não configurada no servidor.");
                context.Result = new UnauthorizedObjectResult("Acesso Negado!");
                return;
            }

            if(string.IsNullOrEmpty(key))
            {
                _logger.LogWarning("Tentativa de acesso sem chave de API. IP: {Ip}, Método: {Method}, Caminho: {Path}", ip, method, path);
                context.Result = new UnauthorizedObjectResult("Acesso Negado!");
                return;
            }

            var keyInByte = Encoding.UTF8.GetBytes(key);
            var apiKeyInByte = Encoding.UTF8.GetBytes(apiKey);

            if (!ValidarKeys(keyInByte, apiKeyInByte))
            {
                _logger.LogWarning("Tentativa de acesso com chave de API inválida. IP: {Ip}, Método: {Method}, Caminho: {Path}", ip, method, path);
                var req = context.HttpContext.Request;
                _logger.LogWarning("DIAGNOSTICO IP: Original-For={OriginalFor} | Forwarded-For={ForwardedFor} | Real-IP={RealIp} | Remoto={Ip}",
                    req.Headers["X-Original-For"].ToString(),
                    req.Headers["X-Forwarded-For"].ToString(),
                    req.Headers["X-Real-IP"].ToString(),
                    ip);
                context.Result = new UnauthorizedObjectResult("Acesso Negado!");
            }
        }
    }
}