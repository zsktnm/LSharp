using LSharp.Client.Common;
using LSharp.Client.DataTransfer;
using LSharp.Client.ViewModels;

namespace LSharp.Client.Services.Auth
{
    public class RegistrationService : IRegistrationService
    {
        private readonly HttpClient client;
        private readonly string registrationUrl;

        public RegistrationService(IHttpClientFactory factory, IConfiguration configuration)
        {
            client = factory.CreateClient();
            registrationUrl = configuration["Urls:Auth:Registration"] ??
                throw new ApplicationException("Не задано значение Urls.Auth.Registration в appsettings.json");
        }

        public async Task<RequestResult> RegistrationAsync(RegistrationDTO data)
        {
            var content = JsonContent.Create(data);
            HttpResponseMessage response;
            try
            {
                response = await client.PostAsync(registrationUrl, content);
            }
            catch
            {
                return RequestResult.FromError("Не удалось подключиться к серверу авторизации. Попробуйте позже.");
            }
            if (response.IsSuccessStatusCode)
            {
                return RequestResult.FromSuccess();
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                var errors = await response.Content.ReadFromJsonAsync<List<string>>();
                return RequestResult.FromErrors(errors ?? ["При регистрации произошла ошибка. Проверьте правильность вводимых значений."]);
            }
            else
            {
                return RequestResult.FromError("При регистрации произошла ошибка. Попробуйте попытку позже.");
            }
        }
    }
}
