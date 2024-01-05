using LSharp.Client.Common;
using LSharp.Client.DataTransfer;
using System.Linq.Dynamic.Core.Tokenizer;

namespace LSharp.Client.Services.User
{
    public class UserService
    {
        private readonly HttpClient httpClient;
        private readonly string userInfoUrl;
        private readonly string setNameUrl;
        private readonly string changePasswordUrl;

        public UserService(IHttpClientFactory clientFactory, 
            IConfiguration configuration) 
        {
            httpClient = clientFactory.CreateClient();
            userInfoUrl = configuration["Urls:User:Info"] ?? throw new ApplicationException("Не задано значение Urls.User.Info в appsettings.json");
            setNameUrl = configuration["Urls:User:SetName"] ?? throw new ApplicationException("Не задано значение Urls.User.SetName в appsettings.json");
            changePasswordUrl = configuration["Urls:User:ChangePassword"] ?? throw new ApplicationException("Не задано значение Urls.User.ChangePassword в appsettings.json");
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

        public async Task<RequestResult> SetUserName(string token, string name)
        {
            HttpResponseMessage response;
            httpClient.DefaultRequestHeaders.Remove("Authorization");
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

            try
            {
                response = await httpClient.GetAsync($"{setNameUrl}?name={name}");
            }
            catch
            {
                return RequestResult.FromError("Сервис недоступен. Попробуйте позже");
            }

            if (response.IsSuccessStatusCode)
            {
                return RequestResult.FromSuccess();
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                var errors = await response.Content.ReadFromJsonAsync<List<string>>();
                return RequestResult.FromErrors(errors);
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                return RequestResult.NoAccess();
            }
            else
            {
                return RequestResult.FromError("Сервис недоступен. Попробуйте позже");
            }
        }

        public async Task<RequestResult> ChangePassword(string token, ChangePasswordDTO changePassword)
        {
            HttpResponseMessage response;
            httpClient.DefaultRequestHeaders.Remove("Authorization");
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

            try
            {
                response = await httpClient.PostAsJsonAsync(changePasswordUrl, changePassword);
            }
            catch
            {
                return RequestResult.FromError("Сервис недоступен. Попробуйте позже");
            }

            if (response.IsSuccessStatusCode)
            {
                return RequestResult.FromSuccess();
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                var errors = await response.Content.ReadFromJsonAsync<List<string>>();
                return RequestResult.FromErrors(errors);
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                return RequestResult.NoAccess();
            }
            else
            {
                return RequestResult.FromError("Сервис недоступен. Попробуйте позже");
            }
        }
        
    }
}
