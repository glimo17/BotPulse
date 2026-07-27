using System.Collections.Generic;
using System.Threading.Tasks;

namespace BotPulse.Authorization
{
    public interface IAuthorizationService
    {
        Task<IEnumerable<string>> GetPermissionsAsync(System.Guid userId);
    }
}
