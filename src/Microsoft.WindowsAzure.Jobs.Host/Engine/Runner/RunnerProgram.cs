using System;
using System.IO;
using System.Reflection;
using Microsoft.WindowsAzure.Jobs.Azure20SdkBinders;
using Microsoft.WindowsAzure.StorageClient;
using Newtonsoft.Json;

namespace Microsoft.WindowsAzure.Jobs
{
    // Used for launching an instance
    internal class RunnerProgram
    {
        public static FunctionExecutionResult MainWorker(FunctionInvokeRequest descr)
        {
            Console.WriteLine("running in pid: {0}", System.Diagnostics.Process.GetCurrentProcess().Id);
            Console.WriteLine("Timestamp:{0}", DateTime.Now.ToLongTimeString());

            _parameterLogger = descr.ParameterLogBlob; // optional 

            FunctionExecutionResult result = new FunctionExecutionResult();

            try
            {
                Invoke(descr);
                // Success
                Console.WriteLine("Success");
            }
            catch (Exception e)
            {
                // both binding errors and user exceptions from the function will land here. 
                result.ExceptionType = e.GetType().FullName;
                result.ExceptionMessage = e.Message;

                // Failure. 
                Console.WriteLine("Exception while executing:");
                WriteExceptionChain(e);
                Console.WriteLine("FAIL");
            }

            return result;
        }

        // $$$ Merge with above
        public static FunctionExecutionResult MainWorker(FunctionInvokeRequest descr, IConfiguration config)
        {
            Console.WriteLine("running in pid: {0}", System.Diagnostics.Process.GetCurrentProcess().Id);
            Console.WriteLine("Timestamp:{0}", DateTime.Now.ToLongTimeString());

            _parameterLogger = descr.ParameterLogBlob; // optional 

            FunctionExecutionResult result = new FunctionExecutionResult();

            try
            {
                Invoke(descr, config);
                // Success
                Console.WriteLine("Success");
            }
            catch (Exception e)
            {
                // both binding errors and user exceptions from the function will land here. 
                result.ExceptionType = e.GetType().FullName;
                result.ExceptionMessage = e.Message;

                // Failure. 
                Console.WriteLine("Exception while executing:");
                WriteExceptionChain(e);
                Console.WriteLine("FAIL");
            }

            return result;
        }

        // Write an exception and inner exceptions
        public static void WriteExceptionChain(Exception e)
        {
            Exception e2 = e;
            while (e2 != null)
            {
                Console.WriteLine("{0}, {1}", e2.GetType().FullName, e2.Message);

                // Write bonus information for extra diagnostics
                var se = e2 as StorageClientException;
                if (se != null)
                {
                    var nvc = se.ExtendedErrorInformation.AdditionalDetails;

                    foreach (var key in nvc.AllKeys)
                    {
                        Console.WriteLine("  >{0}: {1}", key, nvc[key]);
                    }
                }

                Console.WriteLine(e2.StackTrace);
                Console.WriteLine();
                e2 = e2.InnerException;
            }
        }

        public static void Invoke(FunctionInvokeRequest invoke, IConfiguration config)
        {
            MethodInfo method = GetLocalMethod(invoke);
            IRuntimeBindingInputs inputs = new RuntimeBindingInputs(invoke.Location);
            Invoke(config, method, invoke.Id, inputs, invoke.Args);
        }

        public static void Invoke(FunctionInvokeRequest invoke)
        {
            MethodInfo method = GetLocalMethod(invoke);

            // Get standard config. 
            // Use an ICall that binds against the WebService provided by the local function instance.
            IConfiguration config = InitBinders();

            ApplyHooks(method, config); // Give user hooks higher priority than any cloud binders

            // Don't bind ICall if we have no WebService URL. 
            // ### Could bind ICall other ways
            if (invoke.ServiceUrl != null)
            {
                throw new NotImplementedException();
                //                ICall inner = GetWebInvoker(invoke);
                //                CallBinderProvider.Insert(config, inner); // binds ICall
            }

            Invoke(invoke, config);
        }

        private static MethodInfo GetLocalMethod(FunctionInvokeRequest invoke)
        {
            // For a RemoteFunctionLocation, we could download it and invoke. But assuming caller already did that. 
            // (Caller can cache the downloads and so do it more efficiently)
            var localLocation = invoke.Location as LocalFunctionLocation;
            if (localLocation != null)
            {
                return localLocation.GetLocalMethod();
            }

            var methodLocation = invoke.Location as MethodInfoFunctionLocation;
            if (methodLocation != null)
            {
                var method = methodLocation.MethodInfo;
                if (method != null)
                {
                    return method;
                }
            }

            throw new InvalidOperationException("Can't get a MethodInfo from function location:" + invoke.Location.ToString());

        }

        // $$$ get rid of static fields.
        static CloudBlobDescriptor _parameterLogger;

        public static IConfiguration InitBinders()
        {
            Configuration config = new Configuration();

            AddDefaultBinders(config);
            return config;

        }

        public static void AddDefaultBinders(IConfiguration config)
        {
            // Blobs
            config.BlobBinders.Add(new CloudBlobBinderProvider());
            config.BlobBinders.Add(new BlobStreamBinderProvider());
            config.BlobBinders.Add(new TextReaderProvider());
            config.BlobBinders.Add(new TextWriterProvider());
            config.BlobBinders.Add(new StringBlobBinderProvider());

            // Tables
            config.TableBinders.Add(new TableBinderProvider());
            config.TableBinders.Add(new StrongTableBinderProvider());
            config.TableBinders.Add(new DictionaryTableBinderProvider());

            // Other
            config.Binders.Add(new QueueOutputBinderProvider());
            config.Binders.Add(new CloudStorageAccountBinderProvider());

            config.Binders.Add(new BinderBinderProvider()); // for IBinder
            config.Binders.Add(new ContextBinderProvider()); // for IContext

            // Hook in optional binders for Azure 2.0 data types. 
            var azure20sdkBinderProvider = new Azure20SdkBinderProvider();
            config.Binders.Add(azure20sdkBinderProvider);
            config.BlobBinders.Add(azure20sdkBinderProvider);
        }

        private static void ApplyHooks(MethodInfo method, IConfiguration config)
        {
            // Find a hook based on the MethodInfo, and if found, invoke the config
            // Look for Initialize(IConfiguration c) in the same type?

            var t = method.DeclaringType;
            ApplyHooks(t, config);
        }

        public static void ApplyHooks(Type t, IConfiguration config)
        {
            var methodInit = t.GetMethod("Initialize",
                BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public, null,
                new Type[] { typeof(IConfiguration) }, null);
            if (methodInit != null)
            {
                if (methodInit.IsStatic && methodInit.IsPublic)
                {
                    try
                    {
                        methodInit.Invoke(null, new object[] { config });
                    }
                    catch (TargetInvocationException ex)
                    {
                        // This will lose original callstack. Hopefully message is complete enough. 
                        if (ex.InnerException is InvalidOperationException)
                        {
                            throw ex.InnerException;
                        }
                    }
                }
            }
        }

        // Have to still pass in IRuntimeBindingInputs since methods can do binding at runtime. 
        private static void Invoke(IConfiguration config, MethodInfo m, FunctionInstanceGuid instance, IRuntimeBindingInputs inputs, ParameterRuntimeBinding[] argDescriptors)
        {
            int len = argDescriptors.Length;
                        
            INotifyNewBlob notificationService = new NotifyNewBlobViaInMemory();


            IBinderEx bindingContext = new BindingContext(config, inputs, instance, notificationService);

            BindResult[] binds = new BindResult[len];
            ParameterInfo[] ps = m.GetParameters();
            for (int i = 0; i < len; i++)
            {
                var p = ps[i];
                try
                {
                    binds[i] = argDescriptors[i].Bind(config, bindingContext, p);
                }
                catch (Exception e)
                {
                    string msg = string.Format("Error while binding parameter #{0} '{1}':{2}", i, p, e.Message);
                    throw new InvalidOperationException(msg, e);
                }
            }

            bool success = false;
            Console.WriteLine("Parameters bound. Invoking user function.");
            Console.WriteLine("--------");

            SelfWatch fpStopWatcher = null;
            try
            {
                fpStopWatcher = InvokeWorker(m, binds, ps);
                success = true;
            }
            finally
            {
                // Process any out parameters, do any cleanup
                // For update, do any cleanup work. 

                try
                {
                    Console.WriteLine("--------");

                    for (int i = 0; i < len; i++)
                    {
                        var bind = binds[i];
                        try
                        {
                            // This could invoke user code and do complex things that may fail. Catch the exception 
                            bind.OnPostAction();
                        }
                        catch (Exception e)
                        {
                            // This 
                            string msg = string.Format("Error while handling parameter #{0} '{1}' after function returned:", i, ps[i]);
                            throw new InvalidOperationException(msg, e);
                        }
                    }

                    if (success)
                    {
                        foreach (var bind in binds)
                        {
                            var a = bind as IPostActionTransaction;
                            if (a != null)
                            {
                                a.OnSuccessAction();
                            }
                        }
                    }

                }
                finally
                {
                    // Stop the watches last. PostActions may do things that should show up in the watches.
                    // PostActions could also take a long time (flushing large caches), and so it's useful to have
                    // watches still running.                
                    if (fpStopWatcher != null)
                    {
                        fpStopWatcher.Stop();
                    }
                }
            }
        }

        public static SelfWatch InvokeWorker(MethodInfo m, BindResult[] binds, ParameterInfo[] ps)
        {
            SelfWatch fpStopWatcher = null;
            if (_parameterLogger != null)
            {
                CloudBlob blobResults = _parameterLogger.GetBlob();
                fpStopWatcher = new SelfWatch(binds, ps, blobResults);
            }

            // Watchers may tweak args, so do those second.
            object[] args = Array.ConvertAll(binds, bind => bind.Result);

            try
            {
                m.Invoke(null, args);
            }
            catch (TargetInvocationException e)
            {
                // $$$ Beware, this loses the stack trace from the user's invocation
                // Print stacktrace to console now while we have it.
                Console.WriteLine(e.InnerException.StackTrace);

                throw e.InnerException;
            }
            finally
            {
                // Copy back any ref/out parameters
                for (int i = 0; i < binds.Length; i++)
                {
                    binds[i].Result = args[i];
                }
            }

            return fpStopWatcher;
        }
    }
}
