using System;
using System.Collections.Generic;
using System.Text;

namespace SQLiteEZMode.Attributes
{
    /// <summary>
    /// An attribute for decorating a class to allow compatibility with SQLite EZ Mode.
    /// </summary>
    public class SqliteTableAttribute : Attribute
    {
        /// <summary>
        /// The table name that should be used in SQLite for the class this attribute belongs to.
        /// </summary>
        public string TableName { get; private set; }

        /// <summary>
        /// Create an attribute for decorating a class to allow compatibility with SQLite EZ Mode.
        /// </summary>
        /// <param name="tableName">The name of the table in SQLite.</param>
        public SqliteTableAttribute(string tableName)
        {
            this.TableName = tableName;
        }
    }
}
