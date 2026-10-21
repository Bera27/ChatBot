using System.Threading.RateLimiting;
using Chatbot.Api.Controllers;
using Chatbot.Application.Services;
using Chatbot.Infrastructure;
using Chatbot.Infrastructure.Data;
using Chatbot.Infrastructure.Interfaces;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Twilio.AspNet.Core;

var builder = WebApplication.CreateBuilder(args);

ConfigureServices(builder);

var app = builder.Build();

app.UseForwardedHeaders();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ChatbotDbContext>();
    db.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseRateLimiter();
app.UseHttpsRedirection();
app.UseStaticFiles();

app.MapGet("/admin", (IWebHostEnvironment env) =>
    Results.File(Path.Combine(env.WebRootPath, "admin", "index.html"), "text/html"));

app.MapControllers();


app.Run();

void ConfigureServices(WebApplicationBuilder builder)
{
    builder.Services.AddOpenApi();

    builder.Services.AddDbContext<ChatbotDbContext>(options =>
        options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

    builder.Services.AddSingleton<MatchingService>();
    builder.Services.AddSingleton<EscalationPauseService>();
    builder.Services.AddSingleton<IWhatsAppNotifier, WhatsAppNotifier>();
    builder.Services.AddControllers();
    builder.Services.AddTwilioRequestValidation();

    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.AddPolicy("painel", context =>
            RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "desconhecido",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 60,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                }));
    });

    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.All;
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
    });
}
