﻿﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Azure.Jobs.Host.Loggers;
using Microsoft.Azure.Jobs.Host.Protocols;
using Microsoft.Azure.Jobs.Host.Runners;
using Microsoft.Azure.Jobs.Host.Storage;
using Microsoft.Azure.Jobs.Host.Storage.Table;
using Microsoft.Azure.Jobs.Internals;
using Microsoft.WindowsAzure.Storage;

namespace Microsoft.Azure.Jobs
{
    // Create host services that point to a logging account. 
    // This will scan for all functions in-memory, publish them to the function dashboard, 
    // and return a set of services that the host can use for invoking, listening, etc. 
    internal class JobHostContext
    {
        private readonly IExecuteFunction _executeFunction;
        private readonly IFunctionInstanceLogger _functionInstanceLogger;
        private readonly IFunctionTableLookup _functionTableLookup;
        private readonly Guid _hostInstanceId;
        private readonly Guid _hostId;
        private readonly IProcessTerminationSignalReader _terminationSignalReader;
        private readonly IRunningHostTableWriter _heartbeatTable;

        public JobHostContext(string dataConnectionString, string runtimeConnectionString, string serviceBusDataConnectionString, ITypeLocator typeLocator)
        {
            _hostInstanceId = Guid.NewGuid();
            IConfiguration config = RunnerProgram.InitBinders();

            IFunctionTableLookup functionTableLookup;

            var types = typeLocator.GetTypes().ToArray();
            AddCustomerBinders(config, types);
            functionTableLookup = new FunctionStore(dataConnectionString, serviceBusDataConnectionString, config, types);


            // Determine the host name from the function list
            FunctionDefinition[] functions = functionTableLookup.ReadAll();

            FunctionExecutionContext ctx;

            if (runtimeConnectionString != null)
            {
                // Create logging against a live azure account 

                CloudStorageAccount account = CloudStorageAccount.Parse(runtimeConnectionString);
                ICloudTableClient tableClient = new SdkCloudStorageAccount(account).CreateCloudTableClient();
                IHostTable hostTable = new HostTable(tableClient);
                string hostName = GetHostName(functions);
                _hostId = hostTable.GetOrCreateHostId(hostName);
                SetHostId(_hostId, functions);

                IPersistentQueue<PersistentQueueMessage> persistentQueue = new PersistentQueue<PersistentQueueMessage>(account);

                // Publish this to Azure logging account so that a web dashboard can see it. 
                PublishFunctionTable(functionTableLookup, dataConnectionString, serviceBusDataConnectionString,
                    persistentQueue);

                var logger = new WebExecutionLogger(_hostId, _hostInstanceId, account, LogRole);
                ctx = logger.GetExecutionContext();
                _functionInstanceLogger = new CompositeFunctionInstanceLogger(
                    new PersistentQueueFunctionInstanceLogger(persistentQueue), new ConsoleFunctionInstanceLogger());
                ctx.FunctionInstanceLogger = _functionInstanceLogger;

                _terminationSignalReader = new ProcessTerminationSignalReader(account);
                _heartbeatTable = new RunningHostTableWriter(tableClient);
            }
            else
            {
                // No auxillary logging. Logging interfaces are nops or in-memory.

                ctx = new FunctionExecutionContext
                {
                    OutputLogDispenser = new ConsoleFunctionOuputLogDispenser(),
                    FunctionInstanceLogger = new ConsoleFunctionInstanceLogger()
                };

                _terminationSignalReader = new NullProcessTerminationSignalReader();
                _heartbeatTable = new NullRunningHostTableWriter();
            }

            // This is direct execution, doesn't queue up. 
            _executeFunction = new WebSitesExecuteFunction(config, ctx);
            _functionTableLookup = functionTableLookup;
        }

        public IExecuteFunction ExecuteFunction
        {
            get { return _executeFunction; }
        }

        public IFunctionInstanceLogger FunctionInstanceLogger
        {
            get { return _functionInstanceLogger; }
        }

        public IFunctionTableLookup FunctionTableLookup
        {
            get { return _functionTableLookup; }
        }

        public Guid HostId
        {
            get { return _hostId; }
        }

        public Guid HostInstanceId
        {
            get { return _hostInstanceId; }
        }

        public IRunningHostTableWriter RunningHostTableWriter
        {
            get { return _heartbeatTable; }
        }

        public IProcessTerminationSignalReader TerminationSignalReader
        {
            get { return _terminationSignalReader; }
        }

        // Search for any types that implement ICloudBlobStreamBinder<T>
        // When found, automatically add them as binders to our config. 
        internal static void AddCustomerBinders(IConfiguration config, IEnumerable<Type> types)
        {
            // Scan for any binders
            foreach (var type in types)
            {
                try
                {
                    foreach (var ti in type.GetInterfaces())
                    {
                        if (ti.IsGenericType)
                        {
                            var ti2 = ti.GetGenericTypeDefinition();
                            if (ti2 == typeof(ICloudBlobStreamBinder<>))
                            {
                                var tyArg = ti.GetGenericArguments()[0];
                                var tyBinder = typeof(SimpleBinderProvider<>).MakeGenericType(tyArg);

                                var objInner = Activator.CreateInstance(type);
                                var obj = Activator.CreateInstance(tyBinder, objInner);
                                var it = (ICloudBlobBinderProvider)obj;

                                config.BlobBinders.Add(it);
                            }
                        }
                    }
                }
                catch
                {
                }
            }
        }

        private static string GetHostName(FunctionDefinition[] functions)
        {
            // 1. Try to get the assembly name from the first function definition.
            FunctionDefinition firstFunction = functions.FirstOrDefault();

            if (firstFunction != null)
            {
                string hostName = firstFunction.GetAssemblyFullName();

                if (hostName != null)
                {
                    return hostName;
                }
            }

            // 2. If there are no function definitions, try to use the entry assembly.
            Assembly entryAssembly = Assembly.GetEntryAssembly();

            if (entryAssembly != null)
            {
                return entryAssembly.FullName;
            }

            // 3. If there's no entry assembly either, we don't have anything to use.
            return "Unknown";
        }

        // Publish functions to the cloud
        // This lets another site go view them. 
        private void PublishFunctionTable(IFunctionTableLookup functionTableLookup, string dataConnectionString,
            string serviceBusConnectionString, IPersistentQueue<PersistentQueueMessage> logger)
        {
            FunctionDefinition[] functions = functionTableLookup.ReadAll();

            FunctionDescriptor[] functionDescriptors = new FunctionDescriptor[functions.Length];

            for (int index = 0; index < functions.Length; index++)
            {
                functionDescriptors[index] = functions[index].ToFunctionDescriptor();
            }

            HostStartedMessage message = new HostStartedMessage
            {
                HostInstanceId = _hostInstanceId,
                HostId = _hostId,
                StorageConnectionString = dataConnectionString,
                ServiceBusConnectionString = serviceBusConnectionString,
                Functions = functionDescriptors
            };
            logger.Enqueue(message);
        }

        private static void LogRole(TextWriter output)
        {
            output.WriteLine("Local {0}", Process.GetCurrentProcess().Id);
        }

        private static void SetHostId(Guid hostId, FunctionDefinition[] functions)
        {
            Debug.Assert(functions != null);

            foreach (FunctionDefinition function in functions)
            {
                if (function == null)
                {
                    continue;
                }

                function.HostId = hostId;
            }
        }
    }
}
