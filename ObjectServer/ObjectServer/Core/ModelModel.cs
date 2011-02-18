﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Data;
using Npgsql;
using NpgsqlTypes;

using ObjectServer.Utility;
using ObjectServer.Model;

namespace ObjectServer.Core
{
    [ServiceObject]
    public class ModelModel : ModelBase
    {
        public ModelModel() : base()
        {
            this.Automatic = false;
            this.Name = "core.model";
            this.Versioned = false;
        }
    }
}
