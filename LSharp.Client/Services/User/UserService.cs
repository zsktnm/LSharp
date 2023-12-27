using LSharp.Client.Common;
using LSharp.Client.DataTransfer;

namespace LSharp.Client.Services.User
{
    public class UserService
    {
        private readonly HttpClient httpClient;
        private readonly string userInfoUrl;

        public UserService(IHttpClientFactory clientFactory, 
            IConfiguration configuration) 
        {
            httpClient = clientFactory.CreateClient();
            userInfoUrl = configuration["Urls:User:Info"] ?? throw new ApplicationException("Не задано значение Urls.User.Info в appsettings.json");
        }

        public async Task<RequestResult<UserInfoDto>> GetUserInfoAsync(string token)
        {
            HttpResponseMessage response;
            httpClient.DefaultRequestHeaders.Remove("Authorization");
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            try
            {
                response = await httpClient.GetAsync(userInfoUrl);
            }
            catch
            {
                return RequestResult<UserInfoDto>.FromError("Сервис недоступен. Попробуйте позже");
            }

            if (response.IsSuccessStatusCode)
            {
                var userInfo = await response.Content.ReadFromJsonAsync<UserInfoDto>();
                return RequestResult<UserInfoDto>.FromSuccess(userInfo);
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                return RequestResult<UserInfoDto>.NoAccess();
            }
            else
            {
                return RequestResult<UserInfoDto>.FromError("Сервис недоступен. Попробуйте позже");
            }
        }
        
    }
}
