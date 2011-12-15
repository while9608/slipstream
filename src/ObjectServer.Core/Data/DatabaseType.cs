﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace ObjectServer.Data
{
    public enum DatabaseType
    {
        [XmlEnum("mssql")]
        Mssql,

        [XmlEnum("postgres")]
        Postgres,

        //[XmlEnum("oracle")]
        //Oracle,

        //Sqlite
    }
}
