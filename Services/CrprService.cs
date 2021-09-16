using Models;
using System.Threading.Tasks;

namespace Services
{
    public class CrprService
    {
        public Task SaveAsync(Assessment assessment)
        {
            return Task.CompletedTask;
        }
    }
}
