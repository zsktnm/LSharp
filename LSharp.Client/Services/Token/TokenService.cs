using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace LSharp.Client.Services.Token
{
    public class TokenService : ITokenService
    {
        private const string tokenKey = "token";
        private const string refreshTokenKey = "refreshToken";
        private readonly ProtectedLocalStorage storage;

        public TokenService(ProtectedLocalStorage storage)
        {
            this.storage = storage;
        }

        public async Task SetToken(string token)
        {
            await storage.SetAsync(tokenKey, token);
        }

        public async Task SetRefreshToken(string token)
        {
            await storage.SetAsync(refreshTokenKey, token);
        }

        public async Task<string?> GetToken()
        {
            var result = await storage.GetAsync<string>(tokenKey);
            return result.Value;
        }

        public async Task<string?> GetRefreshToken()
        {
            var result = await storage.GetAsync<string>(refreshTokenKey);
            return result.Value;
        }

        public async Task<bool> HasToken()
        {
            var result = await storage.GetAsync<string>(tokenKey);
            return result.Success;
        }



    }
}
