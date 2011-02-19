﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

using ObjectServer.Model;

namespace ObjectServer.Backend
{
    internal sealed class PgSqlTypeConverter : ISqlTypeConverter
    {

        private static readonly Dictionary<FieldType, Func<IField, string>> mapping =
            new Dictionary<FieldType, Func<IField, string>>()
            {
                { FieldType.Boolean, f => "boolean" },
                { FieldType.Integer, f => "int4"  },
                { FieldType.BigInteger, f => "int8"  },
                { FieldType.DateTime, f => "timestamp" },
                { FieldType.Float, f => "float8" },
                { FieldType.Money, f => "money" },
                { FieldType.Text, f => "text" },
                { FieldType.Binary, f =>  "bytea" },
                { FieldType.ManyToOne, f => "int8" },
                { FieldType.Chars, f => string.Format("varchar({0})", f.Size) },
            };

        #region ISqlTypeConverter 成员

        public string FieldToColumn(IField field)
        {
            //mapping[
            throw new NotImplementedException();
        }

        #endregion


        public static string GetSqlType(IField field)
        {
            Debug.Assert(field != null);

            return mapping[field.Type](field);
        }
    }
}
