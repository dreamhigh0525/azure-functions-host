﻿using System;
using Newtonsoft.Json;

#if PUBLICPROTOCOL
namespace Microsoft.Azure.Jobs.Protocols
#else
namespace Microsoft.Azure.Jobs.Host.Protocols
#endif
{
    /// <summary>Represents a message indicating that a function completed execution.</summary>
    [JsonTypeName("FunctionCompleted")]
#if PUBLICPROTOCOL
    public class FunctionCompletedMessage : FunctionStartedMessage
#else
    internal class FunctionCompletedMessage : FunctionStartedMessage
#endif
    {
        /// <summary>Gets or sets the time the function stopped executing.</summary>
        public DateTimeOffset EndTime { get; set; }

        /// <summary>Gets a value indicating whether the function completed successfully.</summary>
        [JsonIgnore]
        public bool Succeeded
        {
            get { return Failure == null; }
        }

        /// <summary>Gets or sets the details of the function's failure.</summary>
        /// <remarks>If the function succeeded, this value is <see langword="null"/>.</remarks>
        public FunctionFailure Failure { get; set; }
    }
}
