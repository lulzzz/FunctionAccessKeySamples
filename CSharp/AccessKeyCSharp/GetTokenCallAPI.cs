//===============================================================================
// Microsoft FastTrack for Azure
// Function Access Key Samples
//===============================================================================
// Copyright © Microsoft Corporation.  All rights reserved.
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY
// OF ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT
// LIMITED TO THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
// FITNESS FOR A PARTICULAR PURPOSE.
//===============================================================================
using Azure;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace AccessKeyCSharp
{
    public static class GetTokenCallAPI
    {
        [FunctionName("GetTokenCallAPI")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("GetTokenCallAPI processed a request.");

            string responseMessage = string.Empty;
            string functionKey = string.Empty;
            
            // Get the function key
            if (req.Headers.ContainsKey("x-functions-key"))
            {
                functionKey = req.Headers["x-functions-key"].ToArray()[0];
            }
            else
            {
                IDictionary<string, string> queryParameterDictionary = req.GetQueryParameterDictionary();
                if (queryParameterDictionary.ContainsKey("code"))
                {
                    functionKey = queryParameterDictionary["code"];
                }
            }
            if (string.IsNullOrEmpty(functionKey)) return new UnauthorizedResult();

            using (HttpClient httpClient = new HttpClient())
            {
                // Retrieve the _master host key from Key Vault
                SecretClient secretClient = new SecretClient(new Uri(Environment.GetEnvironmentVariable("KEYVAULT_ENDPOINT")), new DefaultAzureCredential());
                Azure.Response<KeyVaultSecret> secretResponse;
                try
                {
                    log.LogInformation("GetTokenCallAPI Retrieving _master host key.");

                    secretResponse = await secretClient.GetSecretAsync("AccessKeyManagementKey");
                }
                catch (RequestFailedException ex)
                {
                    log.LogInformation($"Failed to retrieve _master host function key from Key Vault with HTTP status code {ex.Status}.");
                    return new ExceptionResult(new ApplicationException($"Failed to retrieve _master host function key from Key Vault with HTTP status code {ex.Status}."), false);
                }
                KeyVaultSecret managementKeySecret = secretResponse.Value;
                string managementKey = managementKeySecret.Value;

                // Retrieve all of the function keys associated with the current function
                string url = $"{Environment.GetEnvironmentVariable("FUNCTION_ADMIN_ENDPOINT", EnvironmentVariableTarget.Process)}{managementKey}";
                HttpResponseMessage response = await httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    // Find the workspace associated with the function key
                    string responseJSON = await response.Content.ReadAsStringAsync();
                    FunctionKeys functionKeys = JsonConvert.DeserializeObject<FunctionKeys>(responseJSON);
                    string workspaceName = string.Empty;
                    foreach (Key key in functionKeys.keys)
                    {
                        if (key.value == functionKey)
                        {
                            workspaceName = key.name;
                            break;
                        }
                    }
                    if (!string.IsNullOrEmpty(workspaceName))
                    {
                        try
                        {
                            // Retrieve Client ID and Client Secret from Key Vault using workspace name
                            secretResponse = await secretClient.GetSecretAsync($"{workspaceName}Secrets");
                            KeyVaultSecret workspaceCredentialsSecret = secretResponse.Value;
                            WorkspaceCredentials workspaceCredentials = JsonConvert.DeserializeObject<WorkspaceCredentials>(workspaceCredentialsSecret.Value);

                            // Get access token from Azure AD
                            AuthenticationContext authContext = new AuthenticationContext(Environment.GetEnvironmentVariable("AUTHORITY", EnvironmentVariableTarget.Process));
                            AuthenticationResult authResult = await authContext.AcquireTokenAsync(Environment.GetEnvironmentVariable("RESOURCE_ID", EnvironmentVariableTarget.Process), new ClientCredential(workspaceCredentials.clientId, workspaceCredentials.clientSecret));

                            // Call external API
                            using (HttpClient apiClient = new HttpClient())
                            {
                                apiClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResult.AccessToken);
                                HttpResponseMessage apiResponse = await apiClient.GetAsync(Environment.GetEnvironmentVariable("API_URL", EnvironmentVariableTarget.Process));
                                if (apiResponse.IsSuccessStatusCode)
                                {
                                    string apiResponseJSON = await apiResponse.Content.ReadAsStringAsync();
                                    responseMessage = apiResponseJSON;
                                }
                            }
                        }
                        catch
                        {
                            return new UnauthorizedResult();
                        }
                    }
                }
                else
                {
                    return new ExceptionResult(new ApplicationException($"Failed to retrieve list of function keys with HTTP status code {response.StatusCode}."), false);
                }
            }

            return new OkObjectResult(responseMessage);
        }
    }
}
