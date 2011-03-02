﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Diagnostics;
using System.Data;

using ObjectServer.Backend;

namespace ObjectServer.Model
{
    public abstract class ModelBase : ObjectServiceBase
    {
        private readonly IMetaFieldCollection declaredFields =
            new MetaFieldCollection();


        public const string IdFieldName = "id";
        public const string ActiveFieldName = "_field";
        public const string VersionFieldName = "_version";

        protected ModelBase(string name)
            : base(name)
        {
            this.AddInternalFields();
        }

        public override void Initialize(IDatabase db)
        {
            base.Initialize(db);

            //检测此模型是否存在于数据库 core_model 表
            var sql = "SELECT DISTINCT COUNT(\"id\") FROM core_model WHERE name=@0";
            var count = (long)db.DataContext.QueryValue(sql, this.Name);
            if (count <= 0)
            {
                this.CreateModel(db);
            }
        }


        /// <summary>
        /// 注册内部字段
        /// </summary>
        private void AddInternalFields()
        {
            Fields.BigInteger("id").SetLabel("ID").SetRequired();
        }


        private void CreateModel(IDatabase db)
        {
            var rowCount = db.DataContext.Execute(
                "INSERT INTO \"core_model\"(\"name\", \"module\", \"label\") VALUES(@0, @1, @2);",
                this.Name, this.Module, this.Label);

            if (rowCount != 1)
            {
                throw new DataException("Failed to insert record of table core_model");
            }

        }

        public override string[] GetReferencedObjects()
        {
            var query = from f in this.Fields.Values
                        where f.Type == FieldType.ManyToOne
                        select f.Relation;

            //自己不能依赖自己
            query = from m in query
                    where m != this.Name
                    select m;

            return query.Distinct().ToArray();
        }


        public IMetaFieldCollection Fields { get { return this.declaredFields; } }

        protected void VerifyFields(IEnumerable<string> fields)
        {
            Debug.Assert(fields != null);
            var notExistedFields =
                fields.Count(fn => !this.declaredFields.ContainsKey(fn));
            if (notExistedFields > 0)
            {
                throw new ArgumentException("Bad field name", "fields");
            }
        }


    }
}
