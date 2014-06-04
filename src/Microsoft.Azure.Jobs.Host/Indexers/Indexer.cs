﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Azure.Jobs.Host.Bindings;
using Microsoft.Azure.Jobs.Host.Bindings.ConsoleOutput;
using Microsoft.Azure.Jobs.Host.Bindings.Invoke;
using Microsoft.Azure.Jobs.Host.Triggers;
using Microsoft.WindowsAzure.Storage;

namespace Microsoft.Azure.Jobs.Host.Indexers
{
    // Go down and build an index
    internal class Indexer
    {
        private static readonly BindingFlags _publicStaticMethodFlags = BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly;

        private readonly IFunctionTable _functionTable;
        private readonly INameResolver _nameResolver;
        private readonly ITriggerBindingProvider _triggerBindingProvider;
        private readonly IBindingProvider _bindingProvider;
        private readonly CloudStorageAccount _storageAccount;
        private readonly string _serviceBusConnectionString;

        // Account for where index lives
        public Indexer(IFunctionTable functionTable, INameResolver nameResolver, IEnumerable<Type> cloudBlobStreamBinderTypes,
            CloudStorageAccount storageAccount, string serviceBusConnectionString)
        {
            _functionTable = functionTable;
            _nameResolver = nameResolver;
            _triggerBindingProvider = DefaultTriggerBindingProvider.Create(cloudBlobStreamBinderTypes);
            _bindingProvider = DefaultBindingProvider.Create(cloudBlobStreamBinderTypes);
            _storageAccount = storageAccount;
            _serviceBusConnectionString = serviceBusConnectionString;
        }

        public IBindingProvider BindingProvider
        {
            get { return _bindingProvider; }
        }

        public INameResolver NameResolver
        {
            get { return _nameResolver; }
        }

        public void IndexType(Type type)
        {
            // Now register any declaritive methods
            foreach (MethodInfo method in type.GetMethods(_publicStaticMethodFlags))
            {
                IndexMethod(method);
            }

            EnsureNoDuplicateFunctions();
        }

        // Check for duplicate names. Indexing doesn't support overloads.
        private void EnsureNoDuplicateFunctions()
        {
            HashSet<string> locations = new HashSet<string>();

            foreach (FunctionDefinition func in _functionTable.ReadAll())
            {
                var locationKey = func.Id;
                if (!locations.Add(locationKey))
                {
                    // Dup found!
                    string msg = string.Format("Method overloads are not supported. There are multiple methods with the name '{0}'.", locationKey);
                    throw new InvalidOperationException(msg);
                }
            }
        }

        public void IndexMethod(MethodInfo method)
        {
            FunctionDefinition index = CreateFunctionDefinition(method);
            if (index != null)
            {
                _functionTable.Add(index);
            }
        }

        public FunctionDefinition CreateFunctionDefinition(MethodInfo method)
        {
            try
            {
                return CreateFunctionDefinitionInternal(method);
            }
            catch (Exception exception)
            {
                throw IndexException.NewMethod(method.Name, exception);
            }
        }

        private FunctionDefinition CreateFunctionDefinitionInternal(MethodInfo method)
        {
            DescriptionAttribute description = null;
            NoAutomaticTriggerAttribute noAutomaticTrigger = null;

            foreach (var attr in method.GetCustomAttributes())
            {
                description = description ?? (attr as DescriptionAttribute);
                noAutomaticTrigger = noAutomaticTrigger ?? (attr as NoAutomaticTriggerAttribute);
            }

            return CreateFunctionDefinition(method, noAutomaticTrigger != null, description != null);
        }

        private FunctionDefinition CreateFunctionDefinition(MethodInfo method, bool hasNoAutomaticTrigger,
            bool hasDescription)
        {
            ITriggerBinding triggerBinding = null;
            ParameterInfo triggerParameter = null;
            ParameterInfo[] parameters = method.GetParameters();
            foreach (ParameterInfo parameter in parameters)
            {
                ITriggerBinding possibleTriggerBinding = _triggerBindingProvider.TryCreate(new TriggerBindingProviderContext
                {
                    Parameter = parameter,
                    NameResolver = _nameResolver,
                    StorageAccount = _storageAccount,
                    ServiceBusConnectionString = _serviceBusConnectionString
                });

                if (possibleTriggerBinding != null)
                {
                    if (triggerBinding == null)
                    {
                        triggerBinding = possibleTriggerBinding;
                        triggerParameter = parameter;
                    }
                    else
                    {
                        throw new InvalidOperationException("More than one trigger per function is not allowed.");
                    }
                }
            }

            Dictionary<string, IBinding> nonTriggerBindings = new Dictionary<string, IBinding>();
            IReadOnlyDictionary<string, Type> bindingDataContract;

            if (triggerBinding != null)
            {
                bindingDataContract = triggerBinding.BindingDataContract;
            }
            else
            {
                bindingDataContract = null;
            }

            bool hasNonInvokeBinding = false;

            foreach (ParameterInfo parameter in parameters)
            {
                if (parameter == triggerParameter)
                {
                    continue;
                }

                IBinding binding = _bindingProvider.TryCreate(new BindingProviderContext
                {
                    Parameter = parameter,
                    NameResolver = _nameResolver,
                    BindingDataContract = bindingDataContract,
                    StorageAccount = _storageAccount,
                    ServiceBusConnectionString = _serviceBusConnectionString
                });

                if (binding == null)
                {
                    if (triggerBinding != null && !hasNoAutomaticTrigger)
                    {
                        throw new InvalidOperationException("Cannot bind parameter '" + parameter.Name + "' when using this trigger.");
                    }
                    else
                    {
                        // Host.Call-only parameter
                        binding = InvokeBinding.Create(parameter.Name, parameter.ParameterType);
                    }
                }
                else
                {
                    hasNonInvokeBinding = true;
                }

                nonTriggerBindings.Add(parameter.Name, binding);
            }

            if (triggerBinding == null && !hasNonInvokeBinding && !hasNoAutomaticTrigger && !hasDescription)
            {
                // No trigger, binding (other than invoke binding, which always gets created), NoAutomaticTrigger
                // attribute or Description attribute.
                // Ignore this function completely.
                return null;
            }

            // Validation: prevent multiple ConsoleOutputs
            if (nonTriggerBindings.OfType<ConsoleOutputBinding>().Count() > 1)
            {
                throw new InvalidOperationException(
                    "Can't have multiple console output TextWriter parameters on a single function.");
            }

            string triggerParameterName = triggerParameter != null ? triggerParameter.Name : null;

            return new FunctionDefinition
            {
                Id = method.GetFullName(),
                FullName = method.GetFullName(),
                ShortName = method.GetShortName(),
                Method = method,
                TriggerParameterName = triggerParameterName,
                TriggerBinding = triggerBinding,
                NonTriggerBindings = nonTriggerBindings
            };
        }
    }
}
