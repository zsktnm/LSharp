using LSharp.Client.Common;
using LSharp.Client.DataTransfer;

namespace LSharp.Client.Services.Auth
{
    public class LoginService : ILoginService
    {
        HttpClient httpClient;
        string loginUrl;
        string refreshUrl;

        public LoginService(IHttpClientFactory clientFactory, IConfiguration configuration)
        {
            httpClient = clientFactory.CreateClient();
            loginUrl = configuration["Urls:Auth:Login"] ?? throw new ApplicationException("Не задано значение Urls.Auth.Login в appsettings.json");
            refreshUrl = configuration["Urls:Auth:Refresh"] ?? throw new ApplicationException("Не задано значение Urls.Auth.Refresh в appsettings.json");
        }

        public async Task<RequestResult<TokensDTO>> GetTokensAsync(string email, string password)
        {
            var login = new
            {
                Email = email,
                Password = password
            };
            var content = JsonContent.Create(login);
            HttpResponseMessage response;
            try
            {
                response = await httpClient.PostAsync(loginUrl, content);

            }
            catch
            {
                return RequestResult<TokensDTO>.FromError("Произошла ошибка соединения. Попробуйте позже.");
            }

            if (response.IsSuccessStatusCode)
            {
                var tokens = await response.Content.ReadFromJsonAsync<TokensDTO>();
                return RequestResult<TokensDTO>.FromSuccess(tokens);
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest || 
                response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                var errors = await response.Content.ReadFromJsonAsync<List<string>>();
                return RequestResult<TokensDTO>.FromErrors(errors);
            }
            else
            {
                return RequestResult<TokensDTO>.FromError("Произошла ошибка соединения. Попробуйте позже.");
            }
        }

        public async Task<RequestResult<TokensDTO>> RefreshToken(string refresToken)
        {
            throw new NotImplementedException();
        }
    }
}
