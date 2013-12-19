﻿namespace Microsoft.WindowsAzure.Jobs
{
    // Bind result. Invoke a cleanup action only if the function runs successfully.
    // Invoked after all other BindResults get OnPostAction
    // This is useful for queuing a cleanup action, like deleting an input blob.
    internal interface IPostActionTransaction
    {
        void OnSuccessAction();
    }
}
