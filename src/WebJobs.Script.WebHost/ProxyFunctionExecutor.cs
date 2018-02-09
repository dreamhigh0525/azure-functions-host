﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Dependencies;
using Microsoft.Azure.AppService.Proxy.Client;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Script.Description;
using Microsoft.Azure.WebJobs.Script.WebHost;
using Microsoft.Azure.WebJobs.Script.WebHost.Controllers;
using Microsoft.Azure.WebJobs.Script.WebHost.WebHooks;

namespace Microsoft.Azure.WebJobs.Script.Host
{
    public class ProxyFunctionExecutor : IFuncExecutor
    {
        private readonly WebScriptHostManager _scriptHostManager;
        private readonly ISecretManager _secretManager;

        private WebHookReceiverManager _webHookReceiverManager;

        internal ProxyFunctionExecutor(WebScriptHostManager scriptHostManager, WebHookReceiverManager webHookReceiverManager, ISecretManager secretManager)
        {
            _scriptHostManager = scriptHostManager;
            _webHookReceiverManager = webHookReceiverManager;
            _secretManager = secretManager;
        }

        public async Task ExecuteFuncAsync(string funcName, Dictionary<string, object> arguments, CancellationToken cancellationToken)
        {
            HttpRequestMessage request = arguments[ScriptConstants.AzureFunctionsHttpRequestKey] as HttpRequestMessage;

            FunctionDescriptor function = null;

            // This is a call to the local function app from proxy, in this scenario first match will be against local http triggers then rest of the proxies to avoid infinite redirect for * mappings in proxies.
            function = _scriptHostManager.GetHttpFunctionOrNull(request, proxyRoutesFirst: false);

            var functionRequestInvoker = new FunctionRequestInvoker(function, _secretManager);
            var response = await functionRequestInvoker.PreprocessRequestAsync(request);

            if (response != null)
            {
                request.Properties[ScriptConstants.AzureFunctionsHttpResponseKey] = response;
                return;
            }

            Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> processRequestHandler = async (req, ct) =>
            {
                return await functionRequestInvoker.ProcessRequestAsync(req, ct, _scriptHostManager, _webHookReceiverManager);
            };

            // Local Function calls do not go thru ARR, so implementing the ARR's MAX-FORWARDs header logic here to avoid infinte redirects.
            IEnumerable<string> values = null;
            int redirectCount = 0;
            if (request.Headers.TryGetValues(ScriptConstants.AzureProxyFunctionLocalRedirectHeaderName, out values))
            {
                int.TryParse(values.FirstOrDefault(), out redirectCount);

                if(redirectCount >= ScriptConstants.AzureProxyFunctionMaxLocalRedirects)
                {
                    response = request.CreateErrorResponse(HttpStatusCode.BadRequest, "Infinite loop detected when trying to call a local function or proxy from a proxy.");
                    request.Properties[ScriptConstants.AzureFunctionsHttpResponseKey] = response;
                    return;
                }

                // This is to make sure the header is properly updated. removing it then adding it with updated count.
                request.Headers.Remove(ScriptConstants.AzureProxyFunctionLocalRedirectHeaderName);
            }

            redirectCount++;
            request.Headers.Add(ScriptConstants.AzureProxyFunctionLocalRedirectHeaderName, redirectCount.ToString());

            var resp = await _scriptHostManager.HttpRequestManager.ProcessRequestAsync(request, processRequestHandler, cancellationToken);
            request.Properties[ScriptConstants.AzureFunctionsHttpResponseKey] = resp;
            return;
        }
    }
}
