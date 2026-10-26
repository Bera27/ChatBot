using Chatbot.Infrastructure.Data;

namespace Chatbot.Infrastructure
{
    public class HistoricoRetencaoService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<HistoricoRetencaoService> _logger;
        private readonly HistoricoRetencao _historicoRetencao;

        public HistoricoRetencaoService(IServiceScopeFactory scopeFactory, IConfiguration configuration, ILogger<HistoricoRetencaoService> logger, HistoricoRetencao historicoRetencao)
        {
            _scopeFactory = scopeFactory;
            _configuration = configuration;
            _logger = logger;
            _historicoRetencao = historicoRetencao;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await ExecutarLimpezaAsync();

            using var timer = new PeriodicTimer(TimeSpan.FromHours(24));
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await ExecutarLimpezaAsync();
            }
        }

        private async Task ExecutarLimpezaAsync()
        {
            try
            {
                var dias = _configuration.GetValue<int>("Historico:RetencaoDias", 90);

                await using var scope = _scopeFactory.CreateAsyncScope();
                var context = scope.ServiceProvider.GetRequiredService<ChatbotDbContext>();

                var apagados = await _historicoRetencao.LimparAsync(context, DateTime.UtcNow, dias);

                _logger.LogInformation("Foram apagados {Quantidade} registro(s)", apagados);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Falha na limpeza do histórico.");
            }
        }
    }
}