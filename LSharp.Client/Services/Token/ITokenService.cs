
namespace LSharp.Client.Services.Token
{
    public interface ITokenService
    {
        Task<string?> GetRefreshToken();
        Task<string?> GetToken();
        Task<bool> HasToken();
        Task SetRefreshToken(string token);
        Task SetToken(string token);
    }
}