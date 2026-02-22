using System;
using System.Net.Http;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using MTGCollectionTracker.Client;
using MTGCollectionTracker.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Mount root components
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Register authorization services (enables <AuthorizeView> and [Authorize])
builder.Services.AddAuthorizationCore();

// Register authentication and token management services
builder.Services.AddScoped<ITokenStorageService, TokenStorageService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// Register collection service
builder.Services.AddScoped<ICollectionService, CollectionService>();

// Register card search service (no auth required - card data is public)
builder.Services.AddScoped<ICardService, CardService>();

// Register custom authentication state provider
builder.Services.AddScoped<CustomAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
    sp.GetRequiredService<CustomAuthStateProvider>());

// Configure HttpClient to call our backend API
// In development: https://localhost:5001
// In production: This will be configured via appsettings
var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "https://localhost:5001";
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(apiBaseUrl)
});

await builder.Build().RunAsync();
