using LSharp.Client.ViewModels;

namespace LSharp.Client.Services
{



    public class RegistrationService : IRegistrationService
    {
        private readonly HttpClient client;

        public RegistrationService(IHttpClientFactory factory)
        {
            client = factory.CreateClient();
        }

        public async Task<RequestResult> RegistrationAsync(RegisterAccountViewModel data)
        {
            var content = JsonContent.Create(data);
            HttpResponseMessage response;
            try
            {
                response = await client.PostAsync("https://localhost:7177/registration", content);
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
                return RequestResult.FromErrors(errors);
            }
            else
            {
                return RequestResult.FromError("При регистрации произошла ошибка. Попробуйте попытку позже.");
            }
        }
    }
}
