﻿using System;
using System.Runtime.Serialization;

namespace KaC_Modding_Engine_API.Exceptions
{
    public class PlacementFailedException : Exception
    {
        public PlacementFailedException()
        {
        }

        public PlacementFailedException(string message) : base(message)
        {
        }

        public PlacementFailedException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected PlacementFailedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
