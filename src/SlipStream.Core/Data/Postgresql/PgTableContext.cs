﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Reflection;
using System.Diagnostics;
using System.Globalization;

using Sandwych.Utility;
using NHibernate.SqlCommand;

using SlipStream.Model;

namespace SlipStream.Data.Postgresql
{
    internal sealed class PgTableContext : ITableContext
    {
        private IDictionary<string, IColumnMetadata> columns = new Dictionary<string, IColumnMetadata>();
        private readonly static IDictionary<OnDeleteAction, string> OnDeleteMapping =
            new Dictionary<OnDeleteAction, string>()
            {
                { OnDeleteAction.SetNull, "SET NULL" },
                { OnDeleteAction.Cascade, "CASCADE" },
                { OnDeleteAction.Restrict, "RESTRICT" },
                { OnDeleteAction.NoAction, "NO ACTION" },
            };

        public PgTableContext(IDataContext db, string tableName)
        {
            if (db == null)
            {
                throw new ArgumentNullException("session");
            }

            if (string.IsNullOrEmpty(tableName))
            {
                throw new ArgumentNullException("tableName");
            }

            if (!NamingRule.IsValidSqlName(tableName))
            {
                throw new ArgumentOutOfRangeException("tableName");
            }

            this.Name = tableName;
            this.LoadColumns(db, tableName);
        }

        public string Name { get; private set; }

        public bool TableExists(IDataContext db, string tableName)
        {
            if (db == null)
            {
                throw new ArgumentNullException("session");
            }

            if (tableName == null)
            {
                throw new ArgumentNullException("tableName");
            }

            if (!NamingRule.IsValidSqlName(tableName))
            {
                throw new ArgumentOutOfRangeException("tableName");
            }

            //检查连接
            var sql = new SqlString(
                "select coalesce(count(table_name), 0) ",
                "from information_schema.tables ",
                "where table_type='BASE TABLE' and table_schema='public' and table_name=",
                Parameter.Placeholder);

            var n = (long)db.QueryValue(sql, tableName);
            return n > 0;
        }

        public void CreateTable(IDataContext db, IModelDescriptor model, string label)
        {
            if (db == null)
            {
                throw new ArgumentNullException("session");
            }

            if (model == null)
            {
                throw new ArgumentNullException("model");
            }

            var tableName = model.TableName.SqlEscape();

            LoggerProvider.EnvironmentLogger.Debug(String.Format("Creating Table [{0}]...", tableName));

            var fieldsWithoutId = model.Fields.Values.Where(f => f.IsColumn);

            var sb = new SqlStringBuilder();
            sb.Add("create table ");
            sb.Add('"' + tableName + '"');
            sb.Add("(");

            var commaNeeded = false;
            foreach (var f in fieldsWithoutId)
            {
                if (commaNeeded)
                {
                    sb.Add(", ");
                }
                commaNeeded = true;

                sb.Add('"' + f.Name + '"');
                sb.Add(" ");
                var sqlType = PgSqlTypeConverter.GetSqlType(f);
                sb.Add(sqlType);
            }

            sb.Add(") without oids");

            var sql = sb.ToSqlString();
            db.Execute(sql);

            SetTableComment(db, tableName, label);
        }

        public void CreateTable(IDataContext db, string tableName, string label)
        {
            if (db == null)
            {
                throw new ArgumentNullException("session");
            }

            if (string.IsNullOrEmpty(tableName))
            {
                throw new ArgumentNullException("tableName");
            }

            if (!NamingRule.IsValidSqlName(tableName))
            {
                throw new ArgumentOutOfRangeException("tableName");
            }

            LoggerProvider.EnvironmentLogger.Debug(String.Format("Creating Table [{0}]...", tableName));

            tableName = tableName.SqlEscape();
            var sb = new SqlStringBuilder();
            sb.Add("create table ");
            sb.Add('"' + tableName + '"');
            sb.Add("(");
            sb.Add("_id");
            sb.Add(" ");
            sb.Add("bigserial not null, ");
            sb.Add("primary key(");
            sb.Add("_id");
            sb.Add(")) without oids");

            var sql = sb.ToSqlString();
            db.Execute(sql);

            SetTableComment(db, tableName, label);
        }

        private static void SetTableComment(IDataContext db, string tableName, string label)
        {
            if (!String.IsNullOrEmpty(label))
            {
                label = label.SqlEscape();
                var sql = String.Format(CultureInfo.InvariantCulture,
                    @"comment on table ""{0}"" is '{1}'", tableName, label.SqlEscape());

                db.Execute(SqlString.Parse(sql));
            }
        }

        public void AddColumn(IDataContext db, IFieldDescriptor field)
        {
            if (db == null)
            {
                throw new ArgumentNullException("session");
            }

            if (field == null)
            {
                throw new ArgumentNullException("field");
            }

            var sqlType = PgSqlTypeConverter.GetSqlType(field);
            var sql = String.Format(CultureInfo.InvariantCulture,
                @"alter table ""{0}"" add column ""{1}"" {2}",
                this.Name, field.Name, sqlType);

            db.Execute(SqlString.Parse(sql));

            this.SetColumnComment(db, field);

            if (field.IsUnique)
            {
                this.AddUniqueConstraint(db, field.Name);
            }
        }

        private void SetColumnComment(IDataContext db, IFieldDescriptor field)
        {
            if (!string.IsNullOrEmpty(field.Label))
            {
                //添加注释
                var commentSql = String.Format(CultureInfo.InvariantCulture,
                     @"comment on column ""{0}"".""{1}"" is '{2}'",
                     this.Name, field.Name, field.Label.SqlEscape());
                db.Execute(SqlString.Parse(commentSql));
            }
        }

        public void DeleteColumn(IDataContext db, string columnName)
        {
            if (db == null)
            {
                throw new ArgumentNullException("session");
            }
            if (string.IsNullOrEmpty(columnName))
            {
                throw new ArgumentNullException("columnName");
            }

            if (!NamingRule.IsValidSqlName(columnName))
            {
                throw new ArgumentOutOfRangeException("columnName");
            }

            var sql = string.Format(CultureInfo.InvariantCulture,
                "alter table \"{0}\" drop column \"{1}\"",
                this.Name, columnName);
            db.Execute(SqlString.Parse(sql));
        }

        public void AlterColumnNullable(IDataContext db, string columnName, bool nullable)
        {
            if (db == null)
            {
                throw new ArgumentNullException("session");
            }

            if (string.IsNullOrEmpty(columnName))
            {
                throw new ArgumentNullException("columnName");
            }

            if (!NamingRule.IsValidSqlName(columnName))
            {
                throw new ArgumentOutOfRangeException("columnName");
            }

            var action = nullable ? "drop not null" : "set not null";
            var sql = string.Format(CultureInfo.InvariantCulture,
                "alter table \"{0}\" alter column \"{1}\" {2}",
                this.Name, columnName, action);
            db.Execute(SqlString.Parse(sql));
        }

        public void AlterColumnType(IDataContext db, string columnName, string sqlType)
        {
            if (db == null)
            {
                throw new ArgumentNullException("session");
            }

            if (string.IsNullOrEmpty(columnName))
            {
                throw new ArgumentNullException("columnName");
            }

            if (!NamingRule.IsValidSqlName(columnName))
            {
                throw new ArgumentOutOfRangeException("columnName");
            }

            var sql = string.Format(CultureInfo.InvariantCulture,
                "alter table \"{0}\" alter \"{1}\" type {2}",
                this.Name, columnName, sqlType);
            db.Execute(SqlString.Parse(sql));
        }

        public bool ColumnExists(string columnName)
        {
            if (string.IsNullOrEmpty(columnName))
            {
                throw new ArgumentNullException("columnName");
            }

            if (!NamingRule.IsValidSqlName(columnName))
            {
                throw new ArgumentOutOfRangeException("columnName");
            }

            return this.columns.ContainsKey(columnName);
        }

        public IColumnMetadata GetColumn(string columnName)
        {
            if (string.IsNullOrEmpty(columnName))
            {
                throw new ArgumentNullException("columnName");
            }

            if (!NamingRule.IsValidSqlName(columnName))
            {
                throw new ArgumentOutOfRangeException("columnName");
            }

            return this.columns[columnName];
        }

        public IColumnMetadata[] GetAllColumns()
        {
            Debug.Assert(this.columns != null);

            return this.columns.Values.ToArray();
        }

        private void LoadColumns(IDataContext db, string tableName)
        {
            if (db == null)
            {
                throw new ArgumentNullException("session");
            }
            if (string.IsNullOrEmpty(tableName))
            {
                throw new ArgumentNullException("tableName");
            }
            if (!NamingRule.IsValidSqlName(tableName))
            {
                throw new ArgumentOutOfRangeException("tableName");
            }

            var sql = new SqlString(
            "select column_name, data_type, is_nullable, character_maximum_length ",
            "from information_schema.columns ",
            "where table_name=", Parameter.Placeholder, " ",
            "order by ordinal_position");

            var records = db.QueryAsDictionary(sql, tableName);
            this.columns.Clear();
            foreach (var r in records)
            {
                var column = new PgColumnMetadata(r);
                this.columns.Add(column.Name, column);
            }

        }

        private void AddUniqueConstraint(IDataContext db, string column)
        {
            Debug.Assert(db != null);
            Debug.Assert(!string.IsNullOrEmpty(column));

            var constraintName = this.Name + "_" + column + "_unique";
            var constraint = string.Format(CultureInfo.InvariantCulture, "UNIQUE(\"{0}\")", column);
            this.AddConstraint(db, constraintName, constraint);
        }


        public void AddConstraint(IDataContext dbctx, string constraintName, string constraint)
        {
            if (dbctx == null)
            {
                throw new ArgumentNullException("_datactx");
            }

            if (string.IsNullOrEmpty(constraintName))
            {
                throw new ArgumentNullException("constraintName");
            }

            if (string.IsNullOrEmpty(constraint))
            {
                throw new ArgumentNullException("constraint");
            }

            var sql = "alter table \"{0}\" add constraint \"{1}\" {2}";
            sql = string.Format(CultureInfo.InvariantCulture, sql, this.Name, constraintName, constraint);
            dbctx.Execute(SqlString.Parse(sql));
        }

        public void DeleteConstraint(IDataContext dbctx, string constraintName)
        {
            if (dbctx == null)
            {
                throw new ArgumentNullException("session");
            }

            if (string.IsNullOrEmpty(constraintName))
            {
                throw new ArgumentNullException("constraintName");
            }

            var sql = string.Format(CultureInfo.InvariantCulture,
                "alter table \"{0}\" drop constraint \"{1}\"",
                this.Name, constraintName);
            dbctx.Execute(SqlString.Parse(sql));
        }


        public bool ConstraintExists(IDataContext db, string constraintName)
        {
            if (db == null)
            {
                throw new ArgumentNullException("session");
            }
            if (string.IsNullOrEmpty(constraintName))
            {
                throw new ArgumentNullException("constraintName");
            }

            var sql = SqlString.Parse(@"
select coalesce(count(constraint_name), 0)
    from information_schema.table_constraints
    where table_catalog=? and constraint_schema = 'public' and constraint_name=?");

            var n = (long)db.QueryValue(sql, db.DatabaseName, constraintName);
            return n > 0;
        }

        public void AddFK(IDataContext db, string columnName, string refTable, OnDeleteAction act)
        {
            if (db == null)
            {
                throw new ArgumentNullException("session");
            }
            if (string.IsNullOrEmpty(columnName))
            {
                throw new ArgumentNullException("columnName");
            }
            if (!NamingRule.IsValidSqlName(columnName))
            {
                throw new ArgumentOutOfRangeException("columnName");
            }
            if (string.IsNullOrEmpty(refTable))
            {
                throw new ArgumentNullException("refTable");
            }

            var onDelete = OnDeleteMapping[act];
            var fkName = this.GenerateFkName(columnName);
            var sql = string.Format(CultureInfo.InvariantCulture,
                "alter table \"{0}\" add constraint \"{1}\" foreign key (\"{2}\") references \"{3}\" on delete {4}",
                this.Name, fkName, columnName, refTable, onDelete);

            db.Execute(SqlString.Parse(sql));
        }


        public void DeleteFK(IDataContext db, string columnName)
        {
            if (db == null)
            {
                throw new ArgumentNullException("session");
            }
            if (string.IsNullOrEmpty(columnName))
            {
                throw new ArgumentNullException("columnName");
            }
            if (!NamingRule.IsValidSqlName(columnName))
            {
                throw new ArgumentOutOfRangeException("columnName");
            }

            var fkName = this.GenerateFkName(columnName);
            this.DeleteConstraint(db, fkName);
        }

        public bool FKExists(IDataContext db, string columnName)
        {
            if (db == null)
            {
                throw new ArgumentNullException("session");
            }
            if (string.IsNullOrEmpty(columnName))
            {
                throw new ArgumentNullException("columnName");
            }
            if (!NamingRule.IsValidSqlName(columnName))
            {
                throw new ArgumentOutOfRangeException("columnName");
            }

            var sql = SqlString.Parse(@"
select coalesce(count(constraint_name), 0)
    from information_schema.key_column_usage 
    where constraint_schema='public' and table_name=? and column_name=?
");
            var n = (long)db.QueryValue(sql, this.Name, columnName);
            return n > 0;
        }

        private string GenerateFkName(string columnName)
        {
            if (string.IsNullOrEmpty(columnName))
            {
                throw new ArgumentNullException("columnName");
            }
            if (!NamingRule.IsValidSqlName(columnName))
            {
                throw new ArgumentOutOfRangeException("columnName");
            }

            return string.Format(CultureInfo.InvariantCulture, "{0}_{1}_fkey", this.Name, columnName);
        }
    }
}
