﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace ObjectServer.Exceptions
{

    /// <summary>
    /// 跟资源定义、ORM、实体类定义等有关的异常基类，此异常通常是由于程序员的错误抛出
    /// </summary>
    [Serializable]
    public class ResourceException : ApplicationException
    {
        public ResourceException(string msg)
            : base(msg)
        {
        }

        protected ResourceException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

    }
}