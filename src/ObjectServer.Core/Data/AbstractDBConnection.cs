﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.Common;
using System.Diagnostics;

using NHibernate.SqlCommand;

using ObjectServer.Model;

namespace ObjectServer.Data
{
    internal abstract class AbstractDBConnection : IDBConnection
    {
        protected DbConnection conn;
        private bool opened;

        public AbstractDBConnection()
        {
            this.opened = false;
        }

        ~AbstractDBConnection()
        {
            this.Dispose(false);
        }

        public void Open()
        {
            if (!this.opened)
            {
                this.conn.Open();
                this.opened = true;
            }
        }

        public void Close()
        {
            if (this.opened)
            {
                this.conn.Close();
                this.opened = false;
            }
        }

        public void Delete(string dbName)
        {
            if (string.IsNullOrEmpty(dbName))
            {
                throw new ArgumentNullException("dbName");
            }

            this.EnsureConnectionOpened();

            var sql = string.Format(
                "DROP DATABASE \"{0}\"", dbName);

            Logger.Debug(() => "SQL: " + sql);

            var cmd = this.conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();
        }

        public string DatabaseName { get; protected set; }

        #region Query methods

        public virtual object QueryValue(SqlString commandText, params object[] args)
        {
            if (commandText == null)
            {
                throw new ArgumentNullException("commandText");
            }

            this.EnsureConnectionOpened();

            Logger.Debug(() => "SQL: " + commandText);

            using (var command = this.CreateCommand(commandText))
            {
                PrepareNamedParameters(command, args);
                var result = command.ExecuteScalar();
                return result;
            }
        }

        public virtual int Execute(SqlString commandText, params object[] args)
        {
            if (commandText == null)
            {
                throw new ArgumentNullException("commandText");
            }

            this.EnsureConnectionOpened();

            Logger.Debug(() => "SQL: " + commandText);

            using (var command = this.CreateCommand(commandText))
            {
                PrepareNamedParameters(command, args);
                var result = command.ExecuteNonQuery();
                return result;
            }
        }

        public virtual DataTable QueryAsDataTable(SqlString commandText, params object[] args)
        {
            if (commandText == null)
            {
                throw new ArgumentNullException("commandText");
            }

            this.EnsureConnectionOpened();

            Logger.Debug(() => ("SQL: " + commandText.ToString()));

            using (var command = this.CreateCommand(commandText))
            {
                PrepareNamedParameters(command, args);
                using (var reader = command.ExecuteReader())
                {
                    var tb = new DataTable();
                    while (reader.Read())
                    {
                        for (int i = 0; i < reader.FieldCount; ++i)
                        {
                            var columnName = reader.GetName(i);
                            if (!tb.Columns.Contains(columnName))
                            {
                                tb.Columns.Add(columnName);
                            }
                        }
                        var row = tb.NewRow();
                        for (int i = 0; i < reader.FieldCount; ++i)
                        {
                            row[i] = reader[i];
                        }

                        tb.Rows.Add(row);

                    }
                    return tb;
                }
            }

        }

        public virtual Dictionary<string, object>[] QueryAsDictionary(
            SqlString commandText, params object[] args)
        {
            if (commandText == null)
            {
                throw new ArgumentNullException("commandText");
            }

            EnsureConnectionOpened();

            Logger.Debug(() => "SQL: " + commandText);

            using (var command = this.CreateCommand(commandText))
            {
                PrepareNamedParameters(command, args);

                var rows = new List<Dictionary<string, object>>();

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var fields = new Dictionary<string, object>();
                        for (int i = 0; i < reader.FieldCount; ++i)
                        {
                            var fieldName = reader.GetName(i);
                            fields[fieldName] = reader.GetValue(i);
                        }
                        rows.Add(fields);
                    }
                }

                return rows.ToArray();
            }
        }

        public virtual dynamic[] QueryAsDynamic(SqlString commandText, params object[] args)
        {
            if (commandText == null)
            {
                throw new ArgumentNullException("commandText");
            }

            var dicts = QueryAsDictionary(commandText, args);

            return dicts.Select(r => new DynamicRecord(r)).ToArray();
        }


        public virtual T[] QueryAsArray<T>(SqlString commandText, params object[] args)
        {
            if (commandText == null)
            {
                throw new ArgumentNullException("commandText");
            }

            this.EnsureConnectionOpened();

            Logger.Debug(() => ("SQL: " + commandText));

            using (var command = this.CreateCommand(commandText))
            {
                PrepareNamedParameters(command, args);
                using (var reader = command.ExecuteReader())
                {
                    var result = new List<T>();
                    while (reader.Read())
                    {
                        result.Add((T)reader[0]);
                    }
                    return result.ToArray();
                }
            }
        }


        #endregion

        #region IDisposable 成员

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.conn.Close();
            }

            this.conn.Dispose();
        }

        #endregion



        protected DbCommand PrepareCommand(string commandText)
        {
            Debug.Assert(!string.IsNullOrEmpty(commandText));

            var command = this.conn.CreateCommand();
            command.CommandText = commandText;

            return command;
        }

        protected static void PrepareCommandParameters(DbCommand command, params object[] args)
        {
            Debug.Assert(command != null);

            if (args == null || args.Length == 0)
            {
                return;
            }

            for (int i = 0; i < args.Length; i++)
            {
                var param = command.CreateParameter();
                param.ParameterName = "@" + i.ToString();
                param.Value = args[i];
                command.Parameters.Add(param);
            }
        }

        protected void EnsureConnectionOpened()
        {
            this.Open();
        }

        public abstract bool IsInitialized();
        public abstract string[] List();
        public abstract void Create(string dbName);
        public abstract void Initialize();
        public abstract ITableContext CreateTableContext(string tableName);
        public abstract void LockTable(string tableName);

        public virtual long NextSerial(string sequenceName)
        {
            if (string.IsNullOrEmpty(sequenceName))
            {
                throw new ArgumentNullException("sequenceName");
            }

            var sql = new SqlString(DataProvider.Dialect.GetSequenceNextValString(sequenceName));
            return (long)this.QueryValue(sql);
        }

        public virtual bool IsValidDatabase()
        {
            throw new NotImplementedException();
        }

        public IDbCommand CreateCommand(SqlString sql)
        {
            var sqlCommand = DataProvider.Driver.GenerateCommand(
                CommandType.Text, sql, new NHibernate.SqlTypes.SqlType[] { });
            sqlCommand.Connection = this.conn;
            return sqlCommand;
        }

        private static void PrepareNamedParameters(IDbCommand sqlCommand, object[] args)
        {
            Debug.Assert(args != null);
            Debug.Assert(sqlCommand != null);

            for (int i = 0; i < args.Length; i++)
            {
                var value = args[i];
                var param = sqlCommand.CreateParameter();
                param.ParameterName = 'p' + i.ToString();
                param.Value = value;
                sqlCommand.Parameters.Add(param);
            }
        }
    }
}
