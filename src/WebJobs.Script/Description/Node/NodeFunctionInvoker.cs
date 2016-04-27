﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using EdgeJs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.Host.Bindings.Runtime;
using Microsoft.Azure.WebJobs.Script.Binding;
using Microsoft.Azure.WebJobs.Script.Diagnostics;
using Newtonsoft.Json;

namespace Microsoft.Azure.WebJobs.Script.Description
{
    // TODO: make this internal
    public class NodeFunctionInvoker : FunctionInvokerBase
    {
        private readonly Collection<FunctionBinding> _inputBindings;
        private readonly Collection<FunctionBinding> _outputBindings;
        private readonly string _script;
        private readonly DictionaryJsonConverter _dictionaryJsonConverter = new DictionaryJsonConverter();
        private readonly BindingMetadata _trigger;
        private readonly IMetricsLogger _metrics;

        private Func<object, Task<object>> _scriptFunc;
        private Func<object, Task<object>> _clearRequireCache;
        private static Func<object, Task<object>> _globalInitializationFunc;
        private static string _functionTemplate;
        private static string _clearRequireCacheScript;
        private static string _globalInitializationScript;

        static NodeFunctionInvoker()
        {
            _functionTemplate = ReadResourceString("functionTemplate.js");
            _clearRequireCacheScript = ReadResourceString("clearRequireCache.js");
            _globalInitializationScript = ReadResourceString("globalInitialization.js");

            Initialize();
        }

        internal NodeFunctionInvoker(ScriptHost host, BindingMetadata trigger, FunctionMetadata functionMetadata, Collection<FunctionBinding> inputBindings, Collection<FunctionBinding> outputBindings)
            : base(host, functionMetadata)
        {
            _trigger = trigger;
            string scriptFilePath = functionMetadata.Source.Replace('\\', '/');
            _script = string.Format(CultureInfo.InvariantCulture, _functionTemplate, scriptFilePath);
            _inputBindings = inputBindings;
            _outputBindings = outputBindings;
            _metrics = host.ScriptConfig.HostConfig.GetService<IMetricsLogger>();

            InitializeFileWatcherIfEnabled();
        }

        /// <summary>
        /// Event raised whenever an unhandled Node exception occurs at the
        /// global level (e.g. an unhandled async exception).
        /// </summary>
        public static event UnhandledExceptionEventHandler UnhandledException;

        private static Func<object, Task<object>> GlobalInitializationFunc
        {
            get
            {
                if (_globalInitializationFunc == null)
                {
                    _globalInitializationFunc = Edge.Func(_globalInitializationScript);
                }
                return _globalInitializationFunc;
            }
        }

        private Func<object, Task<object>> ScriptFunc
        {
            get
            {
                if (_scriptFunc == null)
                {
                    // We delay create the script function so any syntax errors in
                    // the function will be reported to the Dashboard as an invocation
                    // error rather than a host startup error
                    _scriptFunc = Edge.Func(_script);
                }
                return _scriptFunc;
            }
        }

        private Func<object, Task<object>> ClearRequireCacheFunc
        {
            get
            {
                if (_clearRequireCache == null)
                {
                    _clearRequireCache = Edge.Func(_clearRequireCacheScript);
                }
                return _clearRequireCache;
            }
        }

        public override async Task Invoke(object[] parameters)
        {
            object input = parameters[0];
            TraceWriter traceWriter = (TraceWriter)parameters[1];
            IBinderEx binder = (IBinderEx)parameters[2];
            ExecutionContext functionExecutionContext = (ExecutionContext)parameters[3];
            string invocationId = functionExecutionContext.InvocationId.ToString();

            FunctionStartedEvent startedEvent = new FunctionStartedEvent(functionExecutionContext.InvocationId, Metadata);
            _metrics.BeginEvent(startedEvent);

            try
            {
                TraceWriter.Info(string.Format("Function started (Id={0})", invocationId));

                var scriptExecutionContext = CreateScriptExecutionContext(input, binder, traceWriter, TraceWriter, functionExecutionContext);
                var bindingData = (Dictionary<string, string>)scriptExecutionContext["bindingData"];
                bindingData["InvocationId"] = invocationId;

                await ProcessInputBindingsAsync(binder, scriptExecutionContext, bindingData);

                object functionResult = await ScriptFunc(scriptExecutionContext);

                await ProcessOutputBindingsAsync(_outputBindings, input, binder, bindingData, scriptExecutionContext, functionResult);

                TraceWriter.Info(string.Format("Function completed (Success, Id={0})", invocationId));
            }
            catch
            {
                startedEvent.Success = false;
                TraceWriter.Error(string.Format("Function completed (Failure, Id={0})", invocationId));
                throw;
            }
            finally
            {
                _metrics.EndEvent(startedEvent);
            }
        }

        private async Task ProcessInputBindingsAsync(IBinderEx binder, Dictionary<string, object> executionContext, Dictionary<string, string> bindingData)
        {
            var bindings = (Dictionary<string, object>)executionContext["bindings"];

            // create an ordered array of all inputs and add to
            // the execution context. These will be promoted to
            // positional parameters
            List<object> inputs = new List<object>();
            inputs.Add(bindings[_trigger.Name]);

            var nonTriggerInputBindings = _inputBindings.Where(p => !p.Metadata.IsTrigger);
            foreach (var inputBinding in nonTriggerInputBindings)
            {
                string stringValue = null;
                using (MemoryStream stream = new MemoryStream())
                {
                    BindingContext bindingContext = new BindingContext
                    {
                        Binder = binder,
                        BindingData = bindingData,
                        Value = stream
                    };
                    await inputBinding.BindAsync(bindingContext);

                    stream.Seek(0, SeekOrigin.Begin);
                    StreamReader sr = new StreamReader(stream);
                    stringValue = sr.ReadToEnd();
                }

                // if the input is json, try converting to an object or array
                object convertedValue = TryConvertJson(stringValue);

                bindings.Add(inputBinding.Metadata.Name, convertedValue);
                inputs.Add(convertedValue);
            }

            executionContext["inputs"] = inputs;
        }

        private static async Task ProcessOutputBindingsAsync(Collection<FunctionBinding> outputBindings, object input, IBinderEx binder, 
            Dictionary<string, string> bindingData, Dictionary<string, object> scriptExecutionContext, object functionResult)
        {
            if (outputBindings == null)
            {
                return;
            }

            // if the function returned binding values via the function result,
            // apply them to context.bindings
            var bindings = (Dictionary<string, object>)scriptExecutionContext["bindings"];
            IDictionary<string, object> functionOutputs = functionResult as IDictionary<string, object>;
            if (functionOutputs != null)
            {
                foreach (var output in functionOutputs)
                {
                    bindings[output.Key] = output.Value;
                }
            }

            foreach (FunctionBinding binding in outputBindings)
            {
                // get the output value from the script
                // we support primatives (int, string, object) as well as arrays of these
                object value = null;
                if (bindings.TryGetValue(binding.Metadata.Name, out value))
                {
                    // we only support strings, objects, or arrays of those (not primitive types like int)
                    if (value.GetType() == typeof(ExpandoObject) ||
                        value is Array)
                    {
                        value = JsonConvert.SerializeObject(value);
                    }

                    if (!(value is string))
                    {
                        throw new InvalidOperationException(string.Format("Invalid value specified for binding '{0}'", binding.Metadata.Name));
                    }

                    byte[] bytes = Encoding.UTF8.GetBytes((string)value);
                    using (MemoryStream ms = new MemoryStream(bytes))
                    {
                        BindingContext bindingContext = new BindingContext
                        {
                            Input = input,
                            Binder = binder,
                            BindingData = bindingData,
                            Value = ms
                        };
                        await binding.BindAsync(bindingContext);
                    }
                }
            }
        }

        protected override void OnScriptFileChanged(object sender, FileSystemEventArgs e)
        {
            if (_scriptFunc == null)
            {
                // we're not loaded yet, so nothing to reload
                return;
            }

            // The ScriptHost is already monitoring for changes to function.json, so we skip those
            string fileName = Path.GetFileName(e.Name);
            if (string.Compare(fileName, ScriptConstants.FunctionConfigFileName, StringComparison.OrdinalIgnoreCase) != 0)
            {
                // one of the script files for this function changed
                // force a reload on next execution
                _scriptFunc = null;

                // clear the node module cache
                ClearRequireCacheFunc(null).Wait();

                TraceWriter.Info(string.Format(CultureInfo.InvariantCulture, "Script for function '{0}' changed. Reloading.", Metadata.Name));
            }
        }

        private Dictionary<string, object> CreateScriptExecutionContext(object input, IBinderEx binder, TraceWriter traceWriter, TraceWriter fileTraceWriter, ExecutionContext functionExecutionContext)
        {
            // create a TraceWriter wrapper that can be exposed to Node.js
            var log = (Func<object, Task<object>>)(p =>
            {
                string text = p as string;
                if (text != null)
                {
                    traceWriter.Info(text);
                    fileTraceWriter.Info(text);
                } 

                return Task.FromResult<object>(null);
            });

            var bindings = new Dictionary<string, object>();
            var bind = (Func<object, Task<object>>)(p =>
            {
                IDictionary<string, object> bindValues = (IDictionary<string, object>)p;
                foreach (var bindValue in bindValues)
                {
                    bindings[bindValue.Key] = bindValue.Value;
                }
                return Task.FromResult<object>(null);
            });

            var context = new Dictionary<string, object>()
            {
                { "invocationId", functionExecutionContext.InvocationId },
                { "log", log },
                { "bindings", bindings },
                { "bind", bind }
            };

            // This is the input value that we will use to extract binding data.
            // Since binding data extraction is based on JSON parsing, in the
            // various conversions below, we set this to the appropriate JSON
            // string when possible.
            object bindDataInput = input;

            if (input is HttpRequestMessage)
            {
                // convert the request to a json object
                HttpRequestMessage request = (HttpRequestMessage)input;
                string rawBody = null;
                var requestObject = CreateRequestObject(request, out rawBody);
                input = requestObject;

                if (rawBody != null)
                {
                    requestObject["rawBody"] = rawBody;
                    bindDataInput = rawBody;
                }

                // If this is a WebHook function, the input should be the
                // request body
                HttpTriggerBindingMetadata httpBinding = _trigger as HttpTriggerBindingMetadata;
                if (httpBinding != null &&
                    !string.IsNullOrEmpty(httpBinding.WebHookType))
                {
                    input = requestObject["body"];

                    // make the entire request object available as well
                    // this is symmetric with context.res which we also support
                    context["req"] = requestObject;
                }
            }
            else if (input is TimerInfo)
            {
                TimerInfo timerInfo = (TimerInfo)input;
                var inputValues = new Dictionary<string, object>()
                {
                    { "isPastDue", timerInfo.IsPastDue }
                };
                if (timerInfo.ScheduleStatus != null)
                {
                    inputValues["last"] = timerInfo.ScheduleStatus.Last.ToString("s", CultureInfo.InvariantCulture);
                    inputValues["next"] = timerInfo.ScheduleStatus.Next.ToString("s", CultureInfo.InvariantCulture);
                }
                input = inputValues;
            }
            else if (input is Stream)
            {
                Stream inputStream = (Stream)input;
                using (StreamReader sr = new StreamReader(inputStream))
                {
                    bindDataInput = input = sr.ReadToEnd();
                }
            }
            else
            {
                // TODO: Handle case where the input type is something
                // that we can't convert properly
            }

            context["bindingData"] = GetBindingData(bindDataInput, binder);

            if (input is string)
            {
                // if the input is json, try converting to an object or array
                input = TryConvertJson((string)input);
            }

            bindings.Add(_trigger.Name, input);

            return context;
        }

        private Dictionary<string, object> CreateRequestObject(HttpRequestMessage request, out string rawBody)
        {
            rawBody = null;

            // TODO: need to provide access to remaining request properties
            Dictionary<string, object> requestObject = new Dictionary<string, object>();
            requestObject["originalUrl"] = request.RequestUri.ToString();
            requestObject["method"] = request.Method.ToString().ToUpperInvariant();
            requestObject["query"] = request.GetQueryNameValuePairs().ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase);

            Dictionary<string, string> headers = new Dictionary<string, string>();
            foreach (var header in request.Headers)
            {
                // since HTTP headers are case insensitive, we lower-case the keys
                // as does Node.js request object
                headers.Add(header.Key.ToLowerInvariant(), header.Value.First());
            }
            requestObject["headers"] = headers;

            // if the request includes a body, add it to the request object 
            if (request.Content != null && request.Content.Headers.ContentLength > 0)
            {
                string body = request.Content.ReadAsStringAsync().Result;
                rawBody = body;
                MediaTypeHeaderValue contentType = request.Content.Headers.ContentType;
                Dictionary<string, object> jsonObject;
                if (contentType != null && contentType.MediaType == "application/json" &&
                    TryDeserializeJson(body, out jsonObject))
                {
                    // if the content - type of the request is json, deserialize into an object
                    requestObject["body"] = jsonObject;
                }
                else
                {
                    requestObject["body"] = body;
                }
            }

            return requestObject;
        }

        private object TryConvertJson(string input)
        {
            object result = input;

            if (Utility.IsJson(input))
            {
                // if the input is json, try converting to an object or array
                Dictionary<string, object> jsonObject;
                Dictionary<string, object>[] jsonObjectArray;
                if (TryDeserializeJson(input, out jsonObject))
                {
                    result = jsonObject;
                }
                else if (TryDeserializeJson(input, out jsonObjectArray))
                {
                    result = jsonObjectArray;
                }
            }

            return result;
        }

        private bool TryDeserializeJson<TResult>(string json, out TResult result)
        {
            result = default(TResult);

            try
            {
                result = JsonConvert.DeserializeObject<TResult>(json, _dictionaryJsonConverter);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Performs required static initialization in the Edge context.
        /// </summary>
        private static void Initialize()
        {
            var handle = (Func<object, Task<object>>)(err =>
            {
                if (UnhandledException != null)
                {
                    // raise the event to allow subscribers to handle
                    var ex = new InvalidOperationException((string)err);
                    UnhandledException(null, new UnhandledExceptionEventArgs(ex, true));

                    // Ensure that we allow the unhandled exception to kill the process.
                    // unhandled Node global exceptions should never be swallowed.
                    throw ex;
                }
                return Task.FromResult<object>(null);
            });
            var context = new Dictionary<string, object>()
            {
                { "handleUncaughtException", handle }
            };

            GlobalInitializationFunc(context).Wait();
        }

        private static string ReadResourceString(string fileName)
        {
            string resourcePath = string.Format("Microsoft.Azure.WebJobs.Script.Description.Node.Script.{0}", fileName);
            Assembly assembly = Assembly.GetExecutingAssembly();
            using (StreamReader reader = new StreamReader(assembly.GetManifestResourceStream(resourcePath)))
            {
                return reader.ReadToEnd();
            }
        }
    }
}
