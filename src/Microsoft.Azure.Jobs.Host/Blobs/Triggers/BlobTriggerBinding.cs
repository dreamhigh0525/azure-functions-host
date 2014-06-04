﻿using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Azure.Jobs.Host.Bindings;
using Microsoft.Azure.Jobs.Host.Converters;
using Microsoft.Azure.Jobs.Host.Protocols;
using Microsoft.Azure.Jobs.Host.Triggers;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Microsoft.Azure.Jobs.Host.Blobs.Triggers
{
    internal class BlobTriggerBinding : ITriggerBinding<ICloudBlob>
    {
        private readonly IArgumentBinding<ICloudBlob> _argumentBinding;
        private readonly string _containerName;
        private readonly string _blobName;
        private readonly IObjectToTypeConverter<ICloudBlob> _converter;

        public BlobTriggerBinding(IArgumentBinding<ICloudBlob> argumentBinding, CloudStorageAccount account, string containerName, string blobName)
        {
            _argumentBinding = argumentBinding;
            _containerName = containerName;
            _blobName = blobName;
            _converter = CreateConverter(account);
        }

        private static IObjectToTypeConverter<ICloudBlob> CreateConverter(CloudStorageAccount account)
        {
            return new CompositeObjectToTypeConverter<ICloudBlob>(
                new OutputConverter<ICloudBlob>(new IdentityConverter<ICloudBlob>()),
                new OutputConverter<string>(new StringToCloudBlobConverter(account.CreateCloudBlobClient())));
        }

        public IReadOnlyDictionary<string, Type> BindingDataContract
        {
            get
            {
                Dictionary<string, Type> contract = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);

                foreach (string parameterName in new CloudBlobPath(_containerName, _blobName).GetParameterNames())
                {
                    contract.Add(parameterName, typeof(string));
                }

                return contract;
            }
        }

        public string ContainerName
        {
            get { return _containerName; }
        }

        public string BlobName
        {
            get { return _blobName; }
        }

        public string BlobPath
        {
            get { return _containerName + "/" + _blobName; }
        }

        private FileAccess Access
        {
            get
            {
                return typeof(ICloudBlob).IsAssignableFrom(_argumentBinding.ValueType)
                    ? FileAccess.ReadWrite : FileAccess.Read;
            }
        }

        public ITriggerData Bind(ICloudBlob value, ArgumentBindingContext context)
        {
            IValueProvider valueProvider = _argumentBinding.Bind(value, context);
            IReadOnlyDictionary<string, object> bindingData = CreateBindingData(value);

            return new TriggerData(valueProvider, bindingData);
        }

        public ITriggerData Bind(object value, ArgumentBindingContext context)
        {
            ICloudBlob blob = null;

            if (!_converter.TryConvert(value, out blob))
            {
                throw new InvalidOperationException("Unable to convert trigger to ICloudBlob.");
            }

            return Bind(blob, context);
        }

        public ParameterDescriptor ToParameterDescriptor()
        {
            return new BlobTriggerParameterDescriptor
            {
                ContainerName = _containerName,
                BlobName = _blobName,
                Access = Access
            };
        }

        private IReadOnlyDictionary<string, object> CreateBindingData(ICloudBlob value)
        {
            IDictionary<string, string> matches = new CloudBlobPath(_containerName, _blobName).Match(new CloudBlobPath(value));

            if (matches == null)
            {
                return null; // No match
            }

            Dictionary<string, object> bindingData = new Dictionary<string, object>(StringComparer.Ordinal);

            foreach (KeyValuePair<string, string> match in matches)
            {
                bindingData.Add(match.Key, match.Value);
            }

            return bindingData;
        }
    }
}
