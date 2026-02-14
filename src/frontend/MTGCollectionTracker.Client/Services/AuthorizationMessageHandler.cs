using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace MTGCollectionTracker.Client.Services;

/// <summary>
/// HTTP message handler that automatically adds JWT token to outgoing requests.
/// </summary>
public class AuthorizationMessageHandler : DelegatingHandler
{
    private readonly ITokenStorageService _tokenStorage;

    public AuthorizationMessageHandler(ITokenStorageService tokenStorage)
    {
        _tokenStorage = tokenStorage ?? throw new ArgumentNullException(nameof(tokenStorage));
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Get the access token from storage
        var token = await _tokenStorage.GetAccessTokenAsync();

        // If we have a token, add it to the Authorization header
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        // Continue with the request
        return await base.SendAsync(request, cancellationToken);
    }
}
