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
using System;
using System.Net.Http;

namespace WorkspaceEmulator
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("*** Calling Azure Function ***");
            string responseMessage = string.Empty;
            using (HttpClient functionClient = new HttpClient())
            {
                HttpResponseMessage functionResponse = functionClient.GetAsync($"{Environment.GetEnvironmentVariable("FUNCTION_URL", EnvironmentVariableTarget.Process)}{Environment.GetEnvironmentVariable("FUNCTION_KEY", EnvironmentVariableTarget.Process)}").Result;
                if (functionResponse.IsSuccessStatusCode)
                {
                    string functionResponseJSON = functionResponse.Content.ReadAsStringAsync().Result;
                    responseMessage = functionResponseJSON;
                }
                else
                {
                    responseMessage = $"Call to function failed with response code {functionResponse.StatusCode}";
                }
            }
            Console.WriteLine(responseMessage);
            Console.WriteLine("*** Press any key to exit ***");
            Console.Read();
        }
    }
}
