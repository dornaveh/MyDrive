global using GoogleFile = Google.Apis.Drive.v3.Data.File;
using Azure.Identity;
using MyDrive;
using System.Net;

var keyVault = Environment.GetEnvironmentVariable("VaultUri");
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllersWithViews();
builder.Configuration.AddAzureKeyVault(new Uri(keyVault), new DefaultAzureCredential());
builder.Services.AddSingleton<JwtHelper>();
builder.Services.AddSingleton<GoogleProvider>();
builder.Services.AddSingleton<StorageProvider>();
builder.Services.AddSingleton<BackupManager>();
builder.Services.AddSingleton(builder.Configuration.GetMsalConfig());
builder.Services.AddApplicationInsightsTelemetry(o =>
{
    o.ConnectionString = builder.Configuration.GetAppInsightConnectionString();
});
var handler = new HttpClientHandler();
if (handler.SupportsAutomaticDecompression)
{
    handler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
}
builder.Services.AddSingleton(new HttpClient(handler));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller}/{action=Index}/{id?}");

app.MapFallbackToFile("index.html");
app.Run();
