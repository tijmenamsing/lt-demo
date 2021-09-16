using Models;
using System.Threading.Tasks;

namespace Services
{
    public class RchService
    {
        public Task<RchData> GetAsync(string obligor)
        {
            //throw new System.Exception("error");
            //throw new FunctionalException("LT-101", "obligor not found");
            return Task.FromResult(new RchData
            {
                AssetClass = obligor == "123" ? "CORP" : "PI"
            });
        }
    }
}
