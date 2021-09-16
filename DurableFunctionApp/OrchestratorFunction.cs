using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Models;
using Services;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace DurableFunctionApp
{
    public class OrchestratorFunction
    {
        private RchService RchService { get; set; } = new RchService();
        private CrprService CrprService { get; set; } = new CrprService();

        [FunctionName("OrchestratorFunction")]
        public static async Task<Assessment> RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            try
            {
                var obligor = context.GetInput<string>();
                var assessment = new Assessment(obligor);

                assessment = await context.CallActivityAsync<Assessment>("GetRchData", assessment);

                if (assessment.Evidence.RchData.AssetClass == "CORP")
                {
                    assessment = await context.CallActivityAsync<Assessment>("CalculateRatio", assessment);
                }
                else if (assessment.Evidence.RchData.AssetClass == "PI")
                {
                    assessment.DisqualificationReason = $"Asset class not Corporate";
                    await context.CallActivityAsync<Assessment>("StoreResult", assessment);
                }

                if (assessment.Evidence.Ratio <= 4)
                {
                    assessment.DisqualificationReason = $"Ratio {assessment.Evidence.Ratio} smaller than 4";
                }
                else { 
                    assessment.IsLeveraged = true;
                }

                await context.CallActivityAsync<Evidence>("StoreResult", assessment);

                return assessment;
            }
            catch (FunctionalException e)
            {
                // limitation: this doesn't work. Activity functions always throw FunctionFailedException
                // the innerException would be FunctionalException, but can't read its message anymore 
                throw;
            }
            catch (Exception e)
            {
                // log error
                var a = e.InnerException.GetType();
                var b = e.InnerException.Message; // not the actual message
                throw;
            }
        }

        [FunctionName("GetRchData")]
        public async Task<Assessment> GetRchData([ActivityTrigger] Assessment assessment, ILogger log)
        {
            log.LogInformation("GetRchData called");
            assessment.Evidence.RchData = await RchService.GetAsync(assessment.Evidence.Obligor);
            return assessment;
        }

        [FunctionName("CalculateRatio")]
        public static Assessment CalculateRatio([ActivityTrigger] Assessment assessment, ILogger log)
        {
            log.LogInformation("CalculateRatio called");
            assessment.Evidence.Ratio = 5;
            return assessment;
        }

        [FunctionName("StoreResult")]
        public async Task StoreResult([ActivityTrigger] Assessment assessment, ILogger log)
        {
            log.LogInformation("StoreResult called");

            await CrprService.SaveAsync(assessment);
        }

        [FunctionName("Function1_HttpStart")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            var obligor = "123";

            // Function input comes from the request content.
            string instanceId = await starter.StartNewAsync("OrchestratorFunction", null, obligor);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }
    }
}