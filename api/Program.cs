using api.Middleware;
using Microsoft.Extensions.DependencyInjection;
using services;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
	.AddJsonFile("./appsettings.json", true)
	.AddJsonFile("./secrets.json", true)
	.AddJsonFile("/run/secrets/aspsecrets.json", true)
	.AddEnvironmentVariables()
	.Build();

builder.Logging.AddConsole();

builder.Services.AddSingleton<SettingsProviderService>();
builder.Services.AddSingleton<EnvironmentProvider>();

builder.Services.AddSingleton<LoadBalancerService>(sp => new(
	sp.GetRequiredService<SettingsProviderService>().SmtpServiceSettings,
	sp.GetRequiredService<ILogger<LoadBalancerService>>()))
	.AddHostedService(pr => pr.GetRequiredService<LoadBalancerService>());

builder.Services.AddSingleton<SmtpService>(sp => new(
	sp.GetRequiredService<SettingsProviderService>().SmtpServiceSettings,
	sp.GetRequiredService<LoadBalancerService>(),
	sp.GetRequiredService<ILogger<SmtpService>>()));

builder.Services.AddSingleton<MasterAccountsService>();
builder.Services.AddTransient<ApiAuthorizationMiddleware>();

builder.Services.AddControllers();


var app = builder.Build();

app.UseCors(opts => {
	opts.AllowAnyOrigin();
	opts.AllowAnyMethod();
	opts.AllowAnyHeader();
});

app.UseMiddleware<ApiAuthorizationMiddleware>();
app.UseRouting();
app.MapControllers();
app.Run();
