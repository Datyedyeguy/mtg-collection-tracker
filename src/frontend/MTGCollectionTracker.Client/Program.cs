using System;
using System.Net.Http;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using MTGCollectionTracker.Client;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Mount root components
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configure HttpClient to call our backend API
// In development: https://localhost:5001
// In production: This will be configured via appsettings
var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "https://localhost:5001";
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(apiBaseUrl)
});

await builder.Build().RunAsync();
