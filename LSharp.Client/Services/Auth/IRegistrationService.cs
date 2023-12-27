using LSharp.Client.Common;
using LSharp.Client.DataTransfer;

namespace LSharp.Client.Services.Auth
{
    public interface IRegistrationService
    {
        Task<RequestResult> RegistrationAsync(RegistrationDTO data);
    }
}
