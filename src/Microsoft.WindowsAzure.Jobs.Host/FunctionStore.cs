﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Microsoft.WindowsAzure.Jobs.Internals
{
    internal class FunctionStore : IFunctionTableLookup
    {
        private IndexInMemory _store;
        private string _prefix;

        // userAccountConnectionString - the account that the functions will bind against. 
        // Index all methods in the types provided by the locator
        public FunctionStore(string userAccountConnectionString, IConfiguration config, IEnumerable<Type> types)
        {
            var indexer = Init(userAccountConnectionString, config);
            foreach (Type t in types)
            {
                indexer.IndexType(m => OnApplyLocationInfo(userAccountConnectionString, m), t);
            }
        }
        
        private Indexer Init(string userAccountConnectionString, IConfiguration config)
        {
            _prefix = GetPrefix(userAccountConnectionString);

            _store = new IndexInMemory();
            var indexer = new Indexer(_store);
            indexer.ConfigOverride = config;
            return indexer;
        }

        // Get a prefix for function IDs. 
        // This is particularly important when we delete stale functions. Needs to be specific
        // enough so we don't delete other user's / other assembly functions.
        // Multiple users may share the same backing logging,
        // and a single user may have multiple assemblies. 
        internal static string GetPrefix(string accountConnectionString)
        {
            string appName = "_";
            var a = Assembly.GetEntryAssembly();
            if (a != null)
            {
                appName = Path.GetFileNameWithoutExtension(a.Location);
            }

            if (accountConnectionString == null)
            {
                return "_." + appName;
            }

            //%USERNAME% is too volatile. Instead, get identity based on the user's storage account name.
            //string userName = Environment.GetEnvironmentVariable("USERNAME") ?? "_";
            string accountName = Utility.GetAccountName(accountConnectionString);

            return accountName + "." + appName;
        }

        private FunctionLocation OnApplyLocationInfo(string accountConnectionString, MethodInfo method)
        {
            var loc = new MethodInfoFunctionLocation(method)
            {
                AccountConnectionString = accountConnectionString,
            };

            loc.Id = _prefix + "." + loc.Id;
            return loc;
        }

        public FunctionDefinition Lookup(string functionId)
        {
            IFunctionTableLookup x = _store;
            return x.Lookup(functionId);
        }

        public FunctionDefinition[] ReadAll()
        {
            IFunctionTableLookup x = _store;
            return x.ReadAll();
        }
    }
}
