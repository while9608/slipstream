﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.IO;

using ObjectServer.Model;
using ObjectServer.Runtime;
using ObjectServer.Backend;

namespace ObjectServer.Core
{
    /// <summary>
    /// </summary>
    [Resource]
    public sealed class ModelDataModel : AbstractTableModel
    {
        public const string ModelName = "core.model_data";

        public ModelDataModel()
            : base(ModelName)
        {
            this.AutoMigration = false;

            Fields.Chars("name").SetLabel("Key").Required().SetSize(128);
            Fields.Chars("module").SetLabel("Module").Required().SetSize(64);
            Fields.Chars("model").SetLabel("Model").Required().SetSize(64);
            Fields.BigInteger("ref_id").SetLabel("Referenced ID").Required();
            Fields.Text("value").SetLabel("Value");
        }

        internal static void Create(IDataContext dbctx, string module, string model, string key, long resId)
        {
            var sql =
                "INSERT INTO core_model_data(name, module, model, ref_id) VALUES(@0, @1, @2, @3)";
            var rows = dbctx.Execute(sql, key, module, model, resId);
            if (rows != 1)
            {
                throw new DataException("Failed to insert row of table 'core_model_data'");
            }
        }

        internal static long? TryLookupResourceId(IDataContext dbctx, string model, string key)
        {
            if (dbctx == null)
            {
                throw new ArgumentNullException("dbctx");
            }

            if (string.IsNullOrEmpty(model))
            {
                throw new ArgumentNullException("model");
            }

            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException("model");
            }

            var sql = "SELECT ref_id FROM core_model_data WHERE model = @0 AND name = @1";
            var rows = dbctx.QueryAsArray(sql, model, key);
            if (rows.Length == 0)
            {
                return null;
            }

            return (long)rows[0];
        }

        internal static void UpdateResourceId(IDataContext dbctx, string model, string key, long refId)
        {
            if (dbctx == null)
            {
                throw new ArgumentNullException("dbctx");
            }

            var sql = "UPDATE core_model_data SET ref_id = @0 WHERE model = @1 AND name = @2";
            var rowCount = dbctx.Execute(sql, refId, model, key);

            if (rowCount != 1)
            {
                throw new InvalidDataException("More than one record");
            }
        }
    }
}