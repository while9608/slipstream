﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ObjectServer.Model;

namespace ObjectServer.Backend
{
    public interface ITableContext
    {
        string Name { get; }

        void AddColumn(IDataContext db, IMetaField field);
        void DeleteColumn(IDataContext db, string columnName);
        void AlterColumnNullable(IDataContext db, string columnName, bool nullable);
        void AlterColumnType(IDataContext db, string columnName, string sqlType);
        bool ColumnExists(string columnName);
        IColumnMetadata GetColumn(string columnName);
        IColumnMetadata[] GetAllColumns();

        bool TableExists(IDataContext db, string tableName);
        void CreateTable(IDataContext db, string tableName, string label);

        void AddConstraint(IDataContext db, string constraintName, string constraint);
        void DeleteConstraint(IDataContext db, string constraintName);

        void AddFK(IDataContext db, string columnName, string refTable, OnDeleteAction refAct);
        void DeleteFK(IDataContext db, string columnName);
        bool FKExists(IDataContext db, string columnName);
    }
}