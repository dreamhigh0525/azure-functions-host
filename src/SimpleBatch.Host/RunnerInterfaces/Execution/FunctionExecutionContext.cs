﻿namespace Microsoft.WindowsAzure.Jobs
{
    // Various objects needed for execution.
    // @@@ Confirm this can be shared across requests
    internal class FunctionExecutionContext
    {
        public IFunctionOuputLogDispenser OutputLogDispenser { get; set; }

        // Used to update function as its being executed
        public IFunctionUpdatedLogger Logger { get; set; }

        // Mark when a function has finished execution. This will send a message that causes the function's 
        // execution statistics to get aggregated. 
        public IFunctionInstanceLogger Bridge { get; set; }

        // Used to confirm function still exists just prior to execution
        public IFunctionTableLookup FunctionTable { get; set; }
    }
}
