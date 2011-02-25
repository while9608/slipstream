﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ObjectServer
{
    [Serializable]
    public sealed class ServiceObjectNotFoundException : Exception
    {
        public ServiceObjectNotFoundException(string msg, string objName)
            : base(msg)
        {
            this.ObjectName = objName;
        }

        public string ObjectName { get; private set; }
    }
}