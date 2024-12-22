using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using AzureDevOpsDashboard.Services;
using AzureDevOpsDashboard.Data;
using Radzen;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// Add custom services
builder.Services.AddSingleton<ITokenService>(serviceProvider => 
{
    var logger = serviceProvider.GetRequiredService<ILogger<TokenService>>();
    var tokenService = new TokenService(logger);
    logger.LogInformation("TokenService initialized as singleton");
    return tokenService;
});
builder.Services.AddScoped<IAzureDevOpsService, AzureDevOpsService>();

// Add Radzen services
builder.Services.AddScoped<DialogService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<TooltipService>();
builder.Services.AddScoped<ContextMenuService>();

// Add HttpClient
builder.Services.AddHttpClient();

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.SetMinimumLevel(LogLevel.Trace);
builder.Logging.AddConsole(options =>
{
    options.IncludeScopes = true;
    options.TimestampFormat = "[HH:mm:ss] ";
});
builder.Logging.AddDebug();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();