﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ObjectServer.Model;

namespace ObjectServer.Core
{

    [Resource]
    public sealed class ActionModel : TableModel
    {

        public ActionModel()
            : base("core.action")
        {

        }

    }
}
