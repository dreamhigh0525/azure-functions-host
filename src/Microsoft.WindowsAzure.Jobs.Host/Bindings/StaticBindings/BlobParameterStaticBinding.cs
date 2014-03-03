﻿using System;
using System.Collections.Generic;

namespace Microsoft.WindowsAzure.Jobs
{
    // Side-effects are understood. We'll read/write to a specific blob, 
    // for which we can even get a modification timestamp from.
    internal class BlobParameterStaticBinding : ParameterStaticBinding
    {
        public CloudBlobPath Path;
        public bool IsInput;

        // $$$ Ratioanlize these rules with BlobParameterRuntimeBinding
        public override void Validate(IConfiguration config, System.Reflection.ParameterInfo parameter)
        {
            BlobClient.ValidateContainerName(this.Path.ContainerName);

            bool useLease;
            Type type = BlobParameterRuntimeBinding.GetBinderType(parameter, this.IsInput, out useLease);
            ICloudBlobBinder blobBinder = config.GetBlobBinder(type, IsInput);

            BlobParameterRuntimeBinding.VerifyBinder(type, blobBinder, useLease);            
        }

        public override ParameterRuntimeBinding Bind(IRuntimeBindingInputs inputs)
        {
            // Bind to a blob container
            var path = this.Path;

            if (path.BlobName == null)
            {
                // Just a container match. Match to the input blob.
                ITriggerNewBlob trigger = inputs as ITriggerNewBlob;

                if (trigger == null)
                {
                    throw new InvalidOperationException(
                        "Direct calls are not supported for BlobInput methods bound only to a container name.");
                }

                path = new CloudBlobPath(trigger.BlobInput);
            }
            else
            {
                path = path.ApplyNames(inputs.NameParameters);
            }


            return Bind(inputs, path);            
        }

        public override ParameterRuntimeBinding BindFromInvokeString(IRuntimeBindingInputs inputs, string invokeString)
        {
            var path = (string.IsNullOrWhiteSpace(invokeString) && !Path.HasParameters()) ? this.Path : new CloudBlobPath(invokeString);

            return Bind(inputs, path);
        }

        private ParameterRuntimeBinding Bind(IRuntimeBindingInputs inputs, CloudBlobPath path)
        {
            var arg = new CloudBlobDescriptor
            {
                AccountConnectionString = inputs.AccountConnectionString,
                ContainerName = path.ContainerName,
                BlobName = path.BlobName
            };

            BlobClient.ValidateContainerName(arg.ContainerName);

            return new BlobParameterRuntimeBinding { Blob = arg, IsInput = IsInput };
        }

        public override string Description
        {
            get
            {
                if (IsInput)
                {
                    return string.Format("Read from blob: {0}", Path);
                }
                else
                {
                    return string.Format("Write to blob: {0}", Path);
                }
            }
        }

        public override IEnumerable<string> ProducedRouteParameters
        {
            get
            {
                return Path.GetParameterNames();
            }
        }

        public override TriggerDirectionType GetTriggerDirectionType()
        {
            if (this.IsInput)
            {
                return TriggerDirectionType.Input;
            }
            else
            {
                return TriggerDirectionType.Output;
            }
        }
    }
}
