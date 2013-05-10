﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SimpleBatch.Client
{
    public interface IFunctionInvoker
    {
        // Invoke the function. 
        // shortName is usually just the function name, although the exact details are determined
        //    by the scope that this invoker is for.
        // Args are either an IDict or anonymous object. 
        // Task is signalled when invocation is complete.
        Task InvokeAsync(string functionShortName, object args = null); // no return value
        
        // Invoke and get the return result.
        //Task<T> InvokeAsync<T>(string function, object args = null);
    }

    public abstract class FunctionInvoker : IFunctionInvoker
    {
        // $$$ Where would we get these from?
        public IDictionary<string, string> InheritedArgs { get; set; }

        // Implements ICall
        // Defers calls to avoid races.
        public Guid QueueCall(string functionShortName, object arguments = null, IEnumerable<Guid> prereqs = null)
        {
            _countQueued++;
            var args = ResolveArgs(arguments);

            var guid = this.InvokeDirect(functionShortName, args, prereqs);
            return guid;
        }       

        private IEnumerable<Guid> NormalizePrereqs(IEnumerable<Guid> prereqs)
        {
            if (prereqs == null)
            {
                return new Guid[0];
            }
            return prereqs;                
        }

        // Invokes, queues an execution. 
        // Function could start running immediately. 
        protected Guid InvokeDirect(string functionShortName, IDictionary<string, string> args, IEnumerable<Guid> prereqs = null)
        {
            Guid guid = MakeWebCall(functionShortName, args, NormalizePrereqs(prereqs));

            return guid;
        }

        // Invoke 
        public Task InvokeAsync(string functionShortName, object arguments = null)
        {
            var args = ResolveArgs(arguments);

            Guid g = InvokeDirect(functionShortName, args);

            // Now wait.
            return WaitOnCall(g);
        }

        public void Invoke(string functionShortName, object arguments = null)
        {
            Task t = InvokeAsync(functionShortName, arguments);
            t.Wait();
        }

        // Arguments is either null (nothing), an IDict, or an object whose properties are the arguments. 
        protected IDictionary<string, string> ResolveArgs(object arguments)
        {
            var args = new Dictionary<string, string>();

            // Start with inhereted, and then overwrite with any explicit arguments.
            if (InheritedArgs != null)
            {
                foreach (var kv in InheritedArgs)
                {
                    args[kv.Key] = kv.Value;
                }
            }

            if (arguments != null)
            {
                var d = ObjectBinderHelpers.ConvertObjectToDict(arguments);
                foreach (var kv in d)
                {
                    args[kv.Key] = kv.Value;
                }
            }

            return args;
        }

        volatile int _countQueued;

        public string GetStatus()
        {
            return string.Format("Queued {0} calls", _countQueued);
        }

        protected abstract Guid MakeWebCall(string functionShortName, IDictionary<string, string> parameters, IEnumerable<Guid> prereqs);

        protected abstract Task WaitOnCall(Guid g);

    }
}