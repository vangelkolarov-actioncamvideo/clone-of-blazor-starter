using BlazorApp.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlazorApp.Api
{
    public static class GetMediaByCodeFunction
    {
        [FunctionName("GetMediaByCode")]
        public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req, ILogger log)
        {
            string code = req.Query["code"];

            log.LogTrace("Code is: " + code);
            if (string.IsNullOrEmpty(code))
            {
                var ex = new ArgumentNullException("code");
                log.LogError(ex, "Error on GetMediaByCode function.");
                return new BadRequestObjectResult(ex.Message);
            }

            try
            {
                CloudTableClient tableClient = CloudHelpers.GetTableClient();
                log.LogTrace("tableClient.BaseUri: " + tableClient.BaseUri);
                CloudTable table = tableClient.GetTableReference("CodeIndex");
                log.LogTrace("table.Name: " + table.Name);
                await table.CreateIfNotExistsAsync();
                IEnumerable<CodeIndex> codeIndexes = table.ExecuteQuery(new TableQuery<CodeIndex>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, code)));
                log.LogTrace("codeIndexes.Count: " + codeIndexes.Count());
                CloudTable codeActionTable = await CloudHelpers.GetTable(tableClient, "CodeAction");
                log.LogTrace("codeActionTable.Name: " + codeActionTable.Name);
                var codeAction = new CodeAction
                {
                    RowKey = Guid.NewGuid().ToString(),
                    PartitionKey = code,
                    Timestamp = DateTimeOffset.UtcNow,
                    UserActionType = UserActionType.Visit.ToString(),
                    BrowserUserAgent = req.Headers["User-Agent"].ToString(),
                    IpAddress = req.HttpContext.Connection.RemoteIpAddress.ToString()
                };
                await codeActionTable.ExecuteAsync(TableOperation.Insert(codeAction));
                log.LogTrace("codeAction.RowKey: " + codeAction.RowKey);
                return new OkObjectResult(codeIndexes);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Error on GetMediaByCode function.");
                return new BadRequestObjectResult(ex.Message);
            }
        }
    }
}
