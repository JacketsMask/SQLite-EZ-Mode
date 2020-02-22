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
        public string Value { get; set; }
        public bool IsPrimaryId { get; set; }

        internal SqliteCellAttribute CellAttribute { get; set; }
        internal Type OriginalType { get; set; }

        internal MethodInfo GetValueMethod { get; set; }
        internal MethodInfo SetValueMethod { get; set; }

        public MetaSqliteCell(SqliteCellAttribute cellAttribute, PropertyInfo propertyInfo, bool publicOnly)
        {
            this.OriginalType = propertyInfo.PropertyType;

            this.ColumnName = cellAttribute.ColumnName != null ? cellAttribute.ColumnName : propertyInfo.Name;
            this.IsPrimaryId = cellAttribute.IsPrimaryId;

            this.CellAttribute = cellAttribute;

            this.GetValueMethod = propertyInfo.GetGetMethod(nonPublic: !publicOnly);
            this.SetValueMethod = propertyInfo.GetSetMethod(nonPublic: !publicOnly);
        }

        /// <summary>
        /// Given metadata attribute and PropertyInfo.
        /// </summary>
        /// <param name="cellAttribute"></param>
        /// <param name="propertyInfo"></param>
        /// <param name="isPrimaryId"></param>
        /// <param name="publicOnly"></param>
        public MetaSqliteCell(PropertyInfo propertyInfo, CellDataTypes cellDataType, bool publicOnly, bool isPrimaryKey)
        {
            this.OriginalType = propertyInfo.PropertyType;

            this.ColumnName = propertyInfo.Name;
            this.IsPrimaryId = isPrimaryKey;

            this.CellAttribute = new SqliteCellAttribute(propertyInfo.Name, cellDataType, isPrimaryKey);
            
            this.GetValueMethod = propertyInfo.GetGetMethod(nonPublic: !publicOnly);
            this.SetValueMethod = propertyInfo.GetSetMethod(nonPublic: !publicOnly);
        }

        public MetaSqliteCell(MetaSqliteCell metaSqliteCell)
        {
            this.OriginalType = metaSqliteCell.OriginalType;

            this.ColumnName = metaSqliteCell.ColumnName;
            this.IsPrimaryId = metaSqliteCell.IsPrimaryId;

            this.CellAttribute = metaSqliteCell.CellAttribute;

            this.GetValueMethod = metaSqliteCell.GetValueMethod;
            this.SetValueMethod = metaSqliteCell.SetValueMethod;
        }

        public string GetCellCreateInfo()
        {
            return ColumnName + " " + GetCellTypeString() + (this.IsPrimaryId ? " PRIMARY KEY ASC" : "");
        }

        private string GetCellTypeString()
        {
            switch (this.CellAttribute.DataType)
            {
                case CellDataTypes.NULL: return "NULL";
                case CellDataTypes.INTEGER: return "INTEGER";
                case CellDataTypes.TEXT: return "TEXT";
                case CellDataTypes.JSON: return "JSON";
                default: throw new InvalidOperationException("Unsupported data type string conversion.");
            }
        }
    }
}
