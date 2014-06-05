﻿namespace Microsoft.Azure.Jobs.Host.Bindings
{
    internal class StaticSelfWatch : ISelfWatch
    {
        private readonly string _status;

        public StaticSelfWatch(string status)
        {
            _status = status;
        }

        public string GetStatus()
        {
            return _status;
        }
    }
}
