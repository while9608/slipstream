﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ObjectServer.Model;

namespace ObjectServer.Backend
{
    public interface ISqlTypeConverter
    {
        string FieldToColumn(IField field);
    }
}