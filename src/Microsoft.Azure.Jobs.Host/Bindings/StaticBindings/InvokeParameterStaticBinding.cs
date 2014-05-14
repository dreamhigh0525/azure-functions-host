﻿using System;
using System.Reflection;

namespace Microsoft.Azure.Jobs
{
    // Represents binding to a parameter that must be supplied by the caller.
    internal class InvokeParameterStaticBinding : ParameterStaticBinding
    {
        public override ParameterRuntimeBinding Bind(IRuntimeBindingInputs inputs)
        {
            string value;
            if (inputs.NameParameters != null)
            {
                if (inputs.NameParameters.TryGetValue(Name, out value))
                {
                    return new LiteralStringParameterRuntimeBinding { Name = Name, Value = value };
                }
            }
            return new BindingErrorParameterRuntimeBinding { Name = Name };
        }

        public override ParameterRuntimeBinding BindFromInvokeString(IRuntimeBindingInputs inputs, string invokeString)
        {
            return new LiteralStringParameterRuntimeBinding { Name = Name, Value = invokeString };
        }

        public override string Description
        {
            get
            {
                return string.Format("Caller-supplied value");
            }
        }

        public override string Prompt
        {
            get
            {
                return "Enter the value";
            }
        }

        public override string DefaultValue
        {
            get { return null; }
        }

        private class BindingErrorParameterRuntimeBinding : ParameterRuntimeBinding
        {
            public override string ConvertToInvokeString()
            {
                return null;
            }

            public override BindResult Bind(IConfiguration config, IBinderEx bindingContext, ParameterInfo targetParameter)
            {
                string msg = String.Format("Can't bind parameter '{0}' to type '{1}'. Are you missing a custom model binder or binding attribute ([BlobInput], [BlobOutput], [QueueInput], [QueueOutput], [Table])?", targetParameter.Name, targetParameter.ParameterType);
                throw new InvalidOperationException(msg);
            }
        }
    }
}
