using LSharp.Client.Common;
using LSharp.Client.DataTransfer;

namespace LSharp.Client.Services.Auth
{
    public interface ILoginService
    {
        Task<RequestResult<TokensDTO>> GetTokensAsync(string email, string password);
        Task<RequestResult<TokensDTO>> RefreshToken(string refresToken);
    }
}
