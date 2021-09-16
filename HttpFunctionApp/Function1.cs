using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Models;
using Services;
using System;
using System.Threading.Tasks;

namespace HttpFunctionApp
{
    public class Function1
    {
        private RchService RchService { get; set; } = new RchService();
        private CrprService CrprService { get; set; } = new CrprService();

        [FunctionName("Function1")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            try
            {
                var obligor = "123";
                var assessment = new Assessment(obligor);

                assessment = await ExecutePhase(() => GetRchData(assessment));
                assessment = await ExecutePhase(() => CalculateRatio(assessment));

                if (!assessment.Disqualified)
                {
                    assessment.IsLeveraged = true;
                }

                await StoreResult(assessment);

                assessment.Evidence = null;
                return new OkObjectResult(assessment);
            }
            catch (FunctionalException e)
            {
                log.LogError("Functional error occured", e);

                return new UnprocessableEntityObjectResult(new ProblemDetails
                {
                    Status = StatusCodes.Status422UnprocessableEntity,
                    Title = e.Message,
                });
            }
            catch (Exception e)
            {
                log.LogError("Technical error occured", e);                
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }

        private async Task<Assessment> ExecutePhase(Func<Task<Assessment>> deleg)
        {
            var assessment = await deleg();

            if (assessment.Disqualified)
            {
                await StoreResult(assessment);
            }

            return assessment;
        }

        private async Task<Assessment> GetRchData(Assessment assessment)
        {
            assessment.Evidence.RchData = await RchService.GetAsync(assessment.Evidence.Obligor);

            if (assessment.Evidence.RchData.AssetClass == "PI")
            {
                assessment.DisqualificationReason = $"Asset class not Corporate";
            }

            return assessment;
        }

        private static async Task<Assessment> CalculateRatio(Assessment assessment)
        {
            assessment.Evidence.Ratio = 3;

            if (assessment.Evidence.Ratio <= 4)
            {
                assessment.DisqualificationReason = $"Ratio {assessment.Evidence.Ratio} smaller than 4";
            }

            return await Task.FromResult<Assessment>(assessment);
        }

        private async Task StoreResult(Assessment assessment)
        {
            await CrprService.SaveAsync(assessment);
        }
    }
}
