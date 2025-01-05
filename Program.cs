using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using AzureDevOpsDashboard.Services;
using AzureDevOpsDashboard.Data;
using Radzen;

var builder = WebApplication.CreateBuilder(args);

// Add URL configuration
builder.WebHost.UseUrls("https://localhost:7234", "http://localhost:5234");

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
builder.Services.AddSingleton<TokenStateService>();

// Add Radzen services
builder.Services.AddScoped<DialogService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<TooltipService>();
builder.Services.AddScoped<ContextMenuService>();

// Add HttpClient
builder.Services.AddHttpClient();

// Configure HTTPS redirection
builder.Services.AddHttpsRedirection(options =>
{
    options.HttpsPort = 5001;
});

// Validate configuration
if (string.IsNullOrEmpty(builder.Configuration["AzureDevOps:Organization"]))
{
    throw new InvalidOperationException("AzureDevOps:Organization configuration is required");
}

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.SetMinimumLevel(LogLevel.Trace);
builder.Logging.AddConsole(options =>
{
    options.FormatterName = "simple";
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