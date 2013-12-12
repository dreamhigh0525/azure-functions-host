﻿using System;
using SimpleBatch;
using System.Diagnostics;

namespace RunnerHost
{
    [DebuggerDisplay("{Guid}")]
    internal class SimpleFunctionToken : IFunctionToken
    {
        public SimpleFunctionToken(Guid g)
        {
            this.Guid = g;
        }

        public Guid Guid { get; private set; }
    }
}