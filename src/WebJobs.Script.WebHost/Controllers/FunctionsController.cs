﻿using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Controllers;

namespace WebJobs.Script.WebHost.Controllers
{
    public class FunctionsController : ApiController
    {
        private readonly WebScriptHostManager _scriptHostManager;

        public FunctionsController(WebScriptHostManager scriptHostManager)
        {
            _scriptHostManager = scriptHostManager;
        }

        public override async Task<HttpResponseMessage> ExecuteAsync(HttpControllerContext controllerContext, CancellationToken cancellationToken)
        {
            HttpRequestMessage request = controllerContext.Request;

            string function = _scriptHostManager.GetMappedHttpFunction(request.RequestUri);
            if (string.IsNullOrEmpty(function))
            {
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }

            // TODO: we're assuming the parameter is named "req" - fix this
            // TODO: make "HttpResponse" key name a constant
            HttpResponseMessage response = null;
            try
            {
                await _scriptHostManager.Instance.CallAsync(function, new { req = request }, cancellationToken);
                response = (HttpResponseMessage)request.Properties["HttpResponse"];
            }
            catch
            {
                response = new HttpResponseMessage(HttpStatusCode.InternalServerError);
            }

            return response;
        }
    }
}
