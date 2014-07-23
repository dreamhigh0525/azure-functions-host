﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.Jobs.Host.Converters;
using Microsoft.Azure.Jobs.Host.Protocols;
using Microsoft.WindowsAzure.Storage;

namespace Microsoft.Azure.Jobs.Host.Bindings.StorageAccount
{
    internal class CloudStorageAccountBinding : IBinding
    {
        private static readonly IObjectToTypeConverter<CloudStorageAccount> _converter =
            new CompositeObjectToTypeConverter<CloudStorageAccount>(
                new OutputConverter<CloudStorageAccount>(new IdentityConverter<CloudStorageAccount>()),
                new OutputConverter<string>(new StringToCloudStorageAccountConverter()));

        private readonly string _parameterName;
        private readonly string _accountName;

        public CloudStorageAccountBinding(string parameterName, CloudStorageAccount account)
        {
            _parameterName = parameterName;
            _accountName = StorageClient.GetAccountName(account);
        }

        public bool FromAttribute
        {
            get { return false; }
        }

        private Task<IValueProvider> BindAsync(CloudStorageAccount account, FunctionBindingContext context)
        {
            IValueProvider provider = new CloudStorageAccountValueProvider(account);
            return Task.FromResult(provider);
        }

        public Task<IValueProvider> BindAsync(object value, FunctionBindingContext context)
        {
            CloudStorageAccount account = null;

            if (!_converter.TryConvert(value, out account))
            {
                throw new InvalidOperationException("Unable to convert value to CloudStorageAccount.");
            }

            return BindAsync(account, context);
        }

        public Task<IValueProvider> BindAsync(BindingContext context)
        {
            return BindAsync(context.StorageAccount, context.FunctionContext);
        }

        public ParameterDescriptor ToParameterDescriptor()
        {
            return new CloudStorageAccountParameterDescriptor
            {
                Name = _parameterName,
                AccountName = _accountName
            };
        }
    }
}
