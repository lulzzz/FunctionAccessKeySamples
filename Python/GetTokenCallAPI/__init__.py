#===============================================================================
# Microsoft FastTrack for Azure
# Function Access Key Samples
#===============================================================================
# Copyright © Microsoft Corporation.  All rights reserved.
# THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY
# OF ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT
# LIMITED TO THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
# FITNESS FOR A PARTICULAR PURPOSE.
#===============================================================================
import os
import json
import requests
import logging
import socket

import azure.functions as func
from azure.keyvault.secrets import SecretClient
from azure.identity import DefaultAzureCredential
from adal import AuthenticationContext

def main(req: func.HttpRequest) -> func.HttpResponse:
    logging.info('Python GetTokenCallAPI processed a request.')

    # Get the function key
    code = req.params.get('code')
    if not code:
        code = req.headers.get('x-functions-key')

    if code:
        credential = DefaultAzureCredential()
        KVUri = os.environ["KEYVAULT_ENDPOINT"]
        client = SecretClient(vault_url=KVUri, credential=credential)

        # Retrieve the _master host key from Key Vault
        try:
            retrieved_secret = client.get_secret("AccessKeyManagementKeyPY")
        except:
            return func.HttpResponse(
             "Failed to retrieve _master host function key from Key Vault.",
             status_code=500
            )

        # Retrieve all of the function keys associated with the current function
        hostname = socket.gethostname()    
        IPAddr = socket.gethostbyname(hostname)
        logging.info(f'{IPAddr}')
        function_admin_endpoint = os.environ["FUNCTION_ADMIN_ENDPOINT"]
        management_url = f"{function_admin_endpoint}{retrieved_secret.value}"
        keys_response = requests.get(management_url)
        if keys_response.status_code == 200:
            # Find the workspace associated with the function key
            function_keys = keys_response.json()
            workspace_name = ""
            for key in function_keys["keys"]:
                if key["value"] == code:
                    workspace_name = key["name"]
                    break
            titles = ""
            if workspace_name != "":
                try:
                    # Retrieve Client ID and Client Secret from Key Vault using workspace name
                    workspace_secret = client.get_secret(f"{workspace_name}Secrets")
                    workspace_client_credentials = json.loads(workspace_secret.value)
                    client_id = workspace_client_credentials["clientId"]
                    client_secret = workspace_client_credentials["clientSecret"]

                    # Get access token from Azure AD
                    authority = os.environ["AUTHORITY"]
                    auth_context = AuthenticationContext(authority)
                    resource_id = os.environ["RESOURCE_ID"]
                    auth_result = auth_context.acquire_token_with_client_credentials(resource_id, client_id, client_secret)
                    token = auth_result["accessToken"]

                    # Call external API
                    request_session = requests.Session()
                    request_session.headers.update({'Authorization': "Bearer " + token})
                    api_endpoint = os.environ["API_URL"]
                    titles_response = request_session.get(api_endpoint)
                    if titles_response.status_code == 200:
                        titles = titles_response.text
                except:
                    func.HttpResponse("Unauthorized", status_code=401)
            return func.HttpResponse(titles, status_code=200)
        else:
            return func.HttpResponse("Failed to retrieve list of function keys", status_code=500)
    else:
        return func.HttpResponse(
             "Pass a function key in the query string or in the request headers.",
             status_code=401
        )
