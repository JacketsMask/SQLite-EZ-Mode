using SQLiteEZMode.Attributes;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace SQLiteEZMode.ReflectiveObjects
{
    internal class MetaSqliteCell
    {

        public string ColumnName { get; set; }
        public CellDataTypes DataType { get; set; }
        public string Value { get; set; }
        public bool IsPrimaryId { get; set; }

        internal SqliteCellAttribute CellAttribute { get; set; }

        internal MethodInfo GetValueMethod { get; set; }
        internal MethodInfo SetValueMethod { get; set; }

        public MetaSqliteCell(SqliteCellAttribute cellAttribute, MethodInfo getValueMethod, MethodInfo setValueMethod)
        {
            this.CellAttribute = cellAttribute;
            this.GetValueMethod = getValueMethod;
            this.SetValueMethod = setValueMethod;
        }

        public string GetCellCreateInfo()
        {
            return ColumnName + " " + GetCellTypeString() + (this.IsPrimaryId ? " PRIMARY KEY ASC" : "");
        }

        private string GetCellTypeString()
        {
            switch (DataType)
            {
                case CellDataTypes.BLOB: return "BLOB";
                case CellDataTypes.INTEGER: return "INTEGER";
                case CellDataTypes.NULL: return "NULL";
                case CellDataTypes.REAL: return "REAL";
                case CellDataTypes.TEXT: return "TEXT";
                default: throw new InvalidOperationException("Unsupported data type string conversion.");
            }
        }
    }
}
