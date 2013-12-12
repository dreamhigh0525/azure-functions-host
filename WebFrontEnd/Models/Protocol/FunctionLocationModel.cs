﻿using RunnerInterfaces;

namespace WebFrontEnd.Models.Protocol
{
    public class FunctionLocationModel
    {
        internal FunctionLocation UnderlyingObject { get; private set; }

        internal FunctionLocationModel(FunctionLocation underlyingObject)
        {
            UnderlyingObject = underlyingObject;
        }

        public string GetId()
        {
            return UnderlyingObject.GetId();
        }

        public string GetShortName()
        {
            return UnderlyingObject.GetShortName();
        }

        public override string ToString()
        {
            return UnderlyingObject.ToString();
        }
    }
}