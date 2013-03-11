﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using DaasEndpoints;
using Executor;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using Orchestrator;
using RunnerInterfaces;

namespace WebFrontEnd.Controllers
{
    // HTML helpers for emitting links and things for various interfaces.
    // Another benefit to HTML helpers is that is that the IDE doesn't find property references in CSHTML.
    public static class MoreHtmlHelpers
    {
        public static MvcHtmlString TimeLapse(
            this HtmlHelper htmlHelper,
            DateTime now, DateTime past)
        {
            var span = now - past;

            string s;
            if (span.TotalSeconds < 60)
            {
                s = string.Format("{0:0.0}s ago", span.TotalSeconds);
            }
            else if (span.TotalSeconds < 60 * 60)
            {
                s = string.Format("{0}m {1:0.0}s ago", span.Minutes, span.Seconds);
            }
            else
            {
                s = string.Format("{0} ago", span);
            }
            return MvcHtmlString.Create(s);

        }

        // Emit an HTML link to the log for the given function instance.
        public static MvcHtmlString FunctionInstanceLogLink(
            this HtmlHelper htmlHelper, 
            ExecutionInstanceLogEntity log)
        {
            string name = log.ToString();
            return FunctionInstanceLogLink(htmlHelper, log.FunctionInstance, name);
        }

        public static MvcHtmlString FunctionInstanceLogLink(
            this HtmlHelper htmlHelper,
            FunctionInvokeRequest instance,
            string textLink = null)
        {
            if (textLink == null)
            {
                textLink = instance.ToString();
            }

            return LinkExtensions.ActionLink(
                htmlHelper,
                textLink,
                "FunctionInstance", "Log",
                new { func = instance.Id },
                null);
        }

        // Overload when we have a guid. Includes the resolve to resolve to a nice function name.
        public static MvcHtmlString FunctionInstanceLogLink(
            this HtmlHelper htmlHelper,
            Guid id,
            IFunctionInstanceLookup lookup)
        {
            ExecutionInstanceLogEntity log = lookup.Lookup(id);
            if (log != null)
            {
                return FunctionInstanceLogLink(htmlHelper, log);
            }
            // No entry matching the guid. Just show the raw guid then. 
            return FunctionInstanceLogLink(htmlHelper, id);
        }

        // Overload when we only have a guid, no resolver. 
        public static MvcHtmlString FunctionInstanceLogLink(
            this HtmlHelper htmlHelper,
            Guid? id,
            string textLink = null)
        {
            if (!id.HasValue)
            {
                return MvcHtmlString.Empty;
            }
            if (textLink == null)
            {
                textLink = id.ToString();
            }

            return LinkExtensions.ActionLink(
                htmlHelper,
                textLink,
                "FunctionInstance", "Log",
                new { func = id.Value},
                null);
        }

        // Emit HTML link to the log for the function descriptor.        
        public static MvcHtmlString FunctionLogLink(this HtmlHelper htmlHelper,
            FunctionIndexEntity func)
        {
            return LinkExtensions.ActionLink(
                htmlHelper,
                func.Location.MethodName,
                "Index", "Function", 
                new { func = func.RowKey }, 
                null);
        }

        public static MvcHtmlString FunctionFullNameLink(this HtmlHelper htmlHelper,
            FunctionIndexEntity func)
        {
            return LinkExtensions.ActionLink(
                htmlHelper,
                func.Location.ToString(),
                "Index", "Function",
                new { func = func.RowKey },
                null);
        }

        // Lists the static information about the given function type.
        public static MvcHtmlString FunctionLogLink(this HtmlHelper htmlHelper,
            FunctionLocation func)
        {
            return LinkExtensions.ActionLink(
                htmlHelper,
                func.MethodName,
                "Index", "Function",
                new { func = FunctionIndexEntity.GetRowKey(func) },
                null);
        }

        // Emit HTML link to history of a function. 
        // This can list all instances of that function 
        public static MvcHtmlString FunctionLogInvokeHistoryLink(this HtmlHelper htmlHelper,
            FunctionLocation func)
        {
            return FunctionLogInvokeHistoryLink(htmlHelper, func, null);
        }

        public static MvcHtmlString FunctionLogInvokeHistoryLink(this HtmlHelper htmlHelper,
            FunctionLocation func, string linkText, bool? success = null)
        {
            string msg = linkText ?? string.Format("{0} invoke history", func.MethodName);
            return LinkExtensions.ActionLink(
                htmlHelper,
                msg,
                "ListFunctionInstances", "Log",
                new { 
                    func = FunctionIndexEntity.GetRowKey(func),
                    success = success
                },
                null);
        }


        // Emit link to page describing blob usage and histo
        public static MvcHtmlString BlobLogLink(this HtmlHelper htmlHelper,
            CloudBlobDescriptor blobPath)
        {
            return LinkExtensions.ActionLink(
                htmlHelper,
                linkText: blobPath.GetId(),
                actionName: "Blob",
                routeValues: 
                new { 
                    path = new CloudBlobPath(blobPath).ToString(),
                    accountName = blobPath.GetAccount().Credentials.AccountName
                });
        }

        public static MvcHtmlString ReplayFunctionInstance(this HtmlHelper htmlHelper, ExecutionInstanceLogEntity log)
        {
            return LinkExtensions.ActionLink(
                htmlHelper,
                linkText: "Replay " + log.ToString(),
                actionName: "InvokeFunctionReplay", 
                controllerName: "Function",
                routeValues: new { instance =  log.RowKey },
                htmlAttributes: null                
                );

        }

        // Renders a link to the console output for the given function instance.
        public static MvcHtmlString FunctionOutputLink(this HtmlHelper htmlHelper,
            ExecutionInstanceLogEntity log)
        {
            if (log.OutputUrl == null)
            {
                return MvcHtmlString.Create("No console output available.");
            }
            TagBuilder builder = new TagBuilder("a");
            builder.MergeAttribute("href", log.OutputUrl);
            builder.InnerHtml = "Console output";

            string html = builder.ToString(TagRenderMode.Normal);
            return MvcHtmlString.Create(html);
        }

        // Get an optional link for the parameter value
        public static MvcHtmlString ParamArgValueLink(this HtmlHelper htmlHelper, ParamModel p)
        {
            if (p.ArgBlobLink != null)
            {
                return LinkExtensions.ActionLink(
               htmlHelper,
               linkText: p.ArgInvokeString,
               actionName: "Blob",
               routeValues:
               new
               {
                   path = new CloudBlobPath(p.ArgBlobLink).ToString(),
                   accountName = p.ArgBlobLink.GetAccount().Credentials.AccountName
               });
            }
            return MvcHtmlString.Create(p.ArgInvokeString);
        }

    }
}