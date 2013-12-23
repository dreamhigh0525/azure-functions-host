﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Reflection;
using System.Threading;

namespace Microsoft.WindowsAzure.Jobs
{
    /// <summary>
    /// Defines properties and methods to locate Job methods and listen to trigger events in order
    /// to execute Job methods.
    /// </summary>
    public class Host
    {
        // Where we log things to. 
        // Null if logging is not supported (this is required for pumping).        
        private readonly string _loggingAccountConnectionString;

        // The user account that we listen on.
        // This is the account that the bindings resolve against.
        private readonly string _userAccountConnectionString;

        private HostContext _hostContext;

        /// <summary>
        /// Initializes a new instance of the Host class, using a Windows Azure Storage connection string located
        /// in the appSettings section of the configuration file.
        /// </summary>
        public Host()
            : this(userAccountConnectionString: null, loggingAccountConnectionString: null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the Host class, using a single Windows Azure Storage connection string for
        /// both reading and writing data as well as logging.
        /// </summary>
        public Host(string userAndLoggingAccountConnectionString)
            : this(userAndLoggingAccountConnectionString, userAndLoggingAccountConnectionString)
        {
        }

        /// <summary>
        /// Initializes a new instance of the Host class, using one Windows Azure Storage connection string for
        /// reading and writing data and another connection string for logging.
        /// </summary>
        public Host(string userAccountConnectionString, string loggingAccountConnectionString)
        {
            _loggingAccountConnectionString = GetConfigSetting(loggingAccountConnectionString, "SimpleBatchLoggingACS");
            _userAccountConnectionString = GetConfigSetting(userAccountConnectionString, "SimpleBatchUserACS");

            ValidateConnectionStrings();

            if (_loggingAccountConnectionString != null)
            {
                _hostContext = GetHostContext();
            }

            WriteAntaresManifest();
        }

        /// <summary>
        /// Gets the storage account name from the connection string.
        /// </summary>
        public string UserAccountName
        {
            get { return Utility.GetAccountName(_userAccountConnectionString); }
        }

        // When running in Antares, write out a manifest file.
        private static void WriteAntaresManifest()
        {
            string filename = Environment.GetEnvironmentVariable("JOB_EXTRA_INFO_URL_PATH");
            if (filename != null)
            {
                const string manifestContents = "/sb";

                File.WriteAllText(filename, manifestContents);
            }
        }

        private static string GetConfigSetting(string overrideValue, string settingName)
        {
            return overrideValue ?? ConfigurationManager.AppSettings[settingName];
        }

        private void ValidateConnectionStrings()
        {
            if (_userAccountConnectionString == null)
            {
                throw new InvalidOperationException("User account connection string is missing. This can be set via the 'SimpleBatchUserACS' appsetting or via the constructor.");
            }
            Utility.ValidateConnectionString(_userAccountConnectionString);
            if (_loggingAccountConnectionString != null)
            {
                if (_loggingAccountConnectionString != _userAccountConnectionString)
                {
                    Utility.ValidateConnectionString(_loggingAccountConnectionString);
                }
            }
        }

        private HostContext GetHostContext()
        {
            var hostContext = new HostContext(_userAccountConnectionString, _loggingAccountConnectionString);
            return hostContext;
        }

        /// <summary>
        /// Runs the jobs on a background thread and return immediately.
        /// The trigger listeners and jobs will execute on the background thread.
        /// </summary>
        public void RunOnBackgroundThread()
        {
            RunOnBackgroundThread(CancellationToken.None);
        }

        /// <summary>
        /// Runs the jobs on a background thread and return immediately.
        /// The trigger listeners and jobs will execute on the background thread.
        /// The thread exits when the cancellation token is signalled.
        /// </summary>
        /// <param name="token">The token to monitor for cancellation requests.</param>
        public void RunOnBackgroundThread(CancellationToken token)
        {
            Thread thread = new Thread(_ => RunAndBlock(token));
            thread.Start();
        }

        /// <summary>
        /// Runs the jobs on the current thread.
        /// The trigger listeners and jobs will execute on the current thread.
        /// </summary>
        public void RunAndBlock()
        {
            RunAndBlock(CancellationToken.None);
        }

        /// <summary>
        /// Runs the jobs on the current thread.
        /// The trigger listeners and jobs will execute on the current thread.
        /// The thread will be blocked until the cancellation token is signalled.
        /// </summary>
        /// <param name="token">The token to monitor for cancellation requests.</param>
        public void RunAndBlock(CancellationToken token)
        {
            //INotifyNewBlobListener fastpathNotify = new NotifyNewBlobViaQueueMessage(Utility.GetAccount(_loggingAccountConnectionString));
            INotifyNewBlobListener fastpathNotify = new NotifyNewBlobViaInMemory();

            using (Worker worker = new Worker(_hostContext.HostName, _hostContext._functionTableLookup, _hostContext._heartbeatTable, _hostContext._queueFunction, fastpathNotify))
            {
                while (!token.IsCancellationRequested)
                {
                    bool handled;
                    do
                    {
                        worker.Poll(token);
                        handled = HandleExecutionQueue(token);
                    }
                    while (handled);

                    Thread.Sleep(2 * 1000);
                    Console.Write(".");
                }
            }
        }

        private bool HandleExecutionQueue(CancellationToken token)
        {
            if (_hostContext._executionQueue != null)
            {
                try
                {
                    bool handled = QueueClient.ApplyToQueue<FunctionInvokeRequest>(request => HandleFromExecutionQueue(request), _hostContext._executionQueue);
                    return handled;
                }
                catch
                {
                    // Poision message. 
                }
            }
            return false;
        }

        private void HandleFromExecutionQueue(FunctionInvokeRequest request)
        {
            // Function was already queued (from the dashboard). So now we just need to activate it.
            //_ctx._queueFunction.Queue(request);
            IActivateFunction activate = (IActivateFunction)_hostContext._queueFunction; // ### Make safe. 
            activate.ActivateFunction(request.Id);
        }

        /// <summary>
        /// Invoke a specific function specified by the method parameter.
        /// </summary>
        /// <param name="method">A MethodInfo representing the job method to execute.</param>
        public void Call(MethodInfo method) {
            Call(method, arguments: null);
        }

        /// <summary>
        /// Invoke a specific function specified by the method parameter.
        /// </summary>
        /// <param name="method">A MethodInfo representing the job method to execute.</param>
        /// <param name="arguments">An object with public properties representing argument names and values to bind to the parameter tokens in the job method's arguments.</param>
        public void Call(MethodInfo method, object arguments)
        {
            if (method == null)
            {
                throw new ArgumentNullException("method");
            }

            if (_loggingAccountConnectionString != null)
            {
                CallWithLogging(method, arguments);
            }
            else
            {
                CallNoLogging(method, arguments);
            }
        }

        // Invoke the function via the SimpleBatch binders. 
        // All invoke is in-memory. Don't log anything to azure storage / function dashboard.
        private void CallNoLogging(MethodInfo method, object arguments = null)
        {
            // This creates with in-memory logging. Create against Azure logging. 
            var lc = new LocalExecutionContext(_userAccountConnectionString, method.DeclaringType);

            Guid guid = lc.Call(method, arguments);

            // If function fails, this should throw
            IFunctionInstanceLookup lookup = lc.FunctionInstanceLookup;
            ExecutionInstanceLogEntity logItem = lookup.LookupOrThrow(guid);

            VerifySuccess(logItem);
        }

        // Invoke the function via the SimpleBatch binders. 
        // Function execution is logged and viewable via the function dashboard.
        private void CallWithLogging(MethodInfo method, object arguments)
        {
            IDictionary<string, string> args2 = ObjectBinderHelpers.ConvertObjectToDict(arguments);

            FunctionDefinition func = ResolveFunctionDefinition(method, _hostContext._functionTableLookup);
            FunctionInvokeRequest instance = Worker.GetFunctionInvocation(func, args2, null);

            instance.TriggerReason = new InvokeTriggerReason
            {
                Message = String.Format("This was function was programmatically called via the host APIs.")
            };

            ExecutionInstanceLogEntity logItem = _hostContext._queueFunction.Queue(instance);

            VerifySuccess(logItem);
        }

        // Throw if the function failed. 
        private static void VerifySuccess(ExecutionInstanceLogEntity logItem)
        {
            if (logItem.GetStatus() == FunctionInstanceStatus.CompletedFailed)
            {
                throw new Exception("Function failed: " + logItem.ExceptionMessage);
            }
        }

        private FunctionDefinition ResolveFunctionDefinition(MethodInfo method, IFunctionTableLookup functionTableLookup)
        {
            foreach (FunctionDefinition func in functionTableLookup.ReadAll())
            {
                MethodInfoFunctionLocation loc = func.Location as MethodInfoFunctionLocation;
                if (loc != null)
                {
                    if (loc.MethodInfo.Equals(method))
                    {
                        return func;
                    }
                }
            }

            string msg = String.Format("'{0}' can't be invoked from simplebatch. Is it missing simple batch bindings?", method);
            throw new InvalidOperationException(msg);
        }
    }
}
