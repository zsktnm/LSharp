using LSharp.Client.ViewModels;

namespace LSharp.Client.Services
{
    public interface IRegistrationService
    {
        Task<RequestResult> RegistrationAsync(RegisterAccountViewModel data);
    }
}
