﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Converters;
using Microsoft.Azure.WebJobs.Host.Triggers;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Microsoft.Azure.WebJobs.Host.Blobs.Triggers
{
    internal class BlobTriggerAttributeBindingProvider : ITriggerBindingProvider
    {
        private readonly IBlobArgumentBindingProvider _provider;

        public BlobTriggerAttributeBindingProvider(IEnumerable<Type> cloudBlobStreamBinderTypes)
        {
            _provider = CreateProvider(cloudBlobStreamBinderTypes);
        }

        private static IBlobArgumentBindingProvider CreateProvider(IEnumerable<Type> cloudBlobStreamBinderTypes)
        {
            List<IBlobArgumentBindingProvider> innerProviders = new List<IBlobArgumentBindingProvider>();

            innerProviders.Add(CreateConverterProvider<ICloudBlob, IdentityConverter<ICloudBlob>>());
            innerProviders.Add(CreateConverterProvider<CloudBlockBlob, CloudBlobToCloudBlockBlobConverter>());
            innerProviders.Add(CreateConverterProvider<CloudPageBlob, CloudBlobToCloudPageBlobConverter>());
            innerProviders.Add(new StreamArgumentBindingProvider(defaultAccess: FileAccess.Read));
            innerProviders.Add(new TextReaderArgumentBindingProvider());
            innerProviders.Add(new StringArgumentBindingProvider());

            if (cloudBlobStreamBinderTypes != null)
            {
                foreach (Type cloudBlobStreamBinderType in cloudBlobStreamBinderTypes)
                {
                    innerProviders.Add(new ObjectArgumentBindingProvider(cloudBlobStreamBinderType));
                }
            }

            return new CompositeArgumentBindingProvider(innerProviders);
        }

        private static IBlobArgumentBindingProvider CreateConverterProvider<TValue, TConverter>()
            where TConverter : IConverter<ICloudBlob, TValue>, new()
        {
            return new ConverterArgumentBindingProvider<TValue>(new TConverter());
        }

        public Task<ITriggerBinding> TryCreateAsync(TriggerBindingProviderContext context)
        {
            ParameterInfo parameter = context.Parameter;
            BlobTriggerAttribute blobTrigger = parameter.GetCustomAttribute<BlobTriggerAttribute>(inherit: false);

            if (blobTrigger == null)
            {
                return Task.FromResult<ITriggerBinding>(null);
            }

            string resolvedCombinedPath = context.Resolve(blobTrigger.BlobPath);
            IBlobPathSource path = BlobPathSource.Create(resolvedCombinedPath);

            IArgumentBinding<ICloudBlob> argumentBinding = _provider.TryCreate(parameter, access: null);

            if (argumentBinding == null)
            {
                throw new InvalidOperationException("Can't bind BlobTrigger to type '" + parameter.ParameterType + "'.");
            }

            ITriggerBinding binding = new BlobTriggerBinding(parameter.Name, argumentBinding,
                context.StorageAccount.CreateCloudBlobClient(), path);
            return Task.FromResult(binding);
        }
    }
}
