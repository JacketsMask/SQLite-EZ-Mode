using System;
using System.Collections.Generic;
using System.Text;

namespace SQLiteEZMode.Attributes
{

    /// <summary>
    /// An attribute for decorating a property to allow compatibility with SQLite EZ Mode.
    /// </summary>
    public class SqliteCellAttribute : Attribute
    {
        /// <summary>
        /// The column name that should be used in SQLite for the property this attribute belongs to.
        /// </summary>
        public string ColumnName { get; private set; }
        /// <summary>
        /// The SQLite data type that should be used in SQLite for the property this attribute belongs to.
        /// </summary>
        public CellDataTypes DataType { get; private set; }
        /// <summary>
        /// True if this is the integer primary key for the property this attribute belongs to.
        /// </summary>
        public bool IsPrimaryId { get; private set; }

        /// <summary>
        /// Create a new attribute to be used to store meta data used by SQLite EZ Mode.
        /// </summary>
        /// <param name="columnName">The column name that should be mapped to this property.</param>
        /// <param name="dataType">The SQLite DataType that this property should be mapped to.</param>
        /// <param name="isPrimaryId">True if this property is an integer that should be mapped as a SQLite primary key.</param>
        public SqliteCellAttribute(string columnName, CellDataTypes dataType, bool isPrimaryId = false)
        {
            this.ColumnName = columnName;
            this.DataType = dataType;
            this.IsPrimaryId = isPrimaryId;
        }

        /// <summary>
        /// Create a new attribute to be used to store meta data used by SQLite EZ Mode.
        /// </summary>
        /// <param name="dataType">The SQLite DataType that this property should be mapped to.</param>
        /// <param name="isPrimaryId">True if this property is an integer that should be mapped as a SQLite primary key.</param>
        public SqliteCellAttribute(CellDataTypes dataType, bool isPrimaryId = false)
        {
            this.DataType = dataType;
            this.IsPrimaryId = isPrimaryId;
        }

    }

    /// <summary>
    /// An enumeration that can be used to map properties to SQLite data types.
    /// </summary>
    public enum CellDataTypes
    {
        NULL,
        INTEGER,
        REAL,
        TEXT,
        BLOB
    }
}
