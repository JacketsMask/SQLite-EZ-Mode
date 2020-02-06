using Microsoft.Data.Sqlite;
using SQLiteEZMode.Attributes;
using SQLiteEZMode.ReflectiveObjects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace SQLiteEZMode
{
    /// <summary>
    /// A wrapper for SQLite that allows utilizing standard class data structures and handles interaction with the SQLite database layer.
    /// </summary>
    public class EzDb : IDisposable
    {
        /// <summary>
        /// The open SQLite connection reference to be used for manual manipulation if EZ Mode functionality falls short.
        /// </summary>
        public SqliteConnection Connection { get; private set; }

        /// <summary>
        /// The OperationMode that EZ Mode should operate within.
        /// </summary>
        public OperationModes OperationMode { get; private set; }

        private Dictionary<Type, MetaSqliteRow> rowSchemaMap;

        /// <summary>
        /// Create a new Sqlite EZ Mode object and open a connection to the passed database, or prepare to create it if it doesn't yet exist.
        /// </summary>
        /// <example>
        /// <code>
        ///     var db = new EzDb("storage.db", OperationModes.EXPLICIT_TAGGING);
        /// </code>
        /// </example>
        /// <param name="dbFileName">The path to the database.</param>
        /// <param name="operationMode">The OperationMode that should be utilized for analysing and processing data structures.</param>
        public EzDb(string dbFileName, OperationModes operationMode)
        {
            this.OperationMode = operationMode;
            // Set the connection and prepare create the DB if it doesn't exist
            SqliteConnectionStringBuilder sb = new SqliteConnectionStringBuilder()
            {
                DataSource = dbFileName,
                Mode = SqliteOpenMode.ReadWriteCreate

            };
            this.Connection = new SqliteConnection(sb.ToString());
            this.Connection.Open();

            this.rowSchemaMap = new Dictionary<Type, MetaSqliteRow>();
        }

        /// <summary>
        /// Select a single object of type <typeparamref name="T"/> from the database with the given primary key.
        /// </summary>
        /// <typeparam name="T">The type of the object to retrieve from the database.</typeparam>
        /// <param name="primaryKeyId">The primary key of the row to retrieve from the database.</param>
        /// <returns>An object of type <typeparamref name="T"/> with the given primary key.</returns>
        /// <example>
        /// <code>
        ///     var person = db.SelectSingle<Person>(15);
        /// </code>
        /// </example>
        public T SelectSingle<T>(int primaryKeyId)
        {
            //Create object of proper type to store result
            var returnObject = (T)Activator.CreateInstance(typeof(T));
            MetaSqliteRow metaSqliteRow = GetOrCreateCachedSqliteRow(typeof(T));

            SqliteCommand command = Connection.CreateCommand();
            command.CommandText = metaSqliteRow.GetSelectStatementWithId(primaryKeyId);
            var reader = command.ExecuteReader();
            reader.Read();

            int index = 0;
            foreach (MetaSqliteCell sqliteCell in metaSqliteRow.MetaSqliteCells)
            {
                var value = reader.GetValue(index++);
                if (Type.GetTypeCode(value.GetType()) == TypeCode.Int64)
                {
                    sqliteCell.SetValueMethod.Invoke(returnObject, new object[] { Convert.ToInt32(value) });
                }
                else
                {
                    sqliteCell.SetValueMethod.Invoke(returnObject, new object[] { value });
                }
            }

            return returnObject;
        }

        /// <summary>
        /// Select all objects of type <typeparamref name="T"/> from the appropriate SQLite table.
        /// </summary>
        /// <typeparam name="T">The type of the object to retrieve from the database.</typeparam>
        /// <returns>All rows of type <typeparamref name="T"/> from the appropriate SQLite table.</returns>
        public IEnumerable<T> SelectAll<T>()
        {
            List<T> results = new List<T>();
            MetaSqliteRow metaSqliteRow = GetOrCreateCachedSqliteRow(typeof(T));
            SqliteCommand command = Connection.CreateCommand();
            command.CommandText = metaSqliteRow.GetSelectStatement();
            var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var returnObject = (T)Activator.CreateInstance(typeof(T));
                int index = 0;
                foreach (MetaSqliteCell sqliteCell in metaSqliteRow.MetaSqliteCells)
                {
                    var value = reader.GetValue(index++);
                    if (Type.GetTypeCode(value.GetType()) == TypeCode.Int64)
                    {
                        sqliteCell.SetValueMethod.Invoke(returnObject, new object[] { Convert.ToInt32(value) });
                    }
                    else
                    {
                        sqliteCell.SetValueMethod.Invoke(returnObject, new object[] { value });
                    }
                }
                results.Add(returnObject);
            }
            return results;
        }


        /// <summary>
        /// Inserts the passed items to the underlying SQLite Database, and updates the integer primary key of each object.
        /// </summary>
        /// <param name="items">An IEnumerable of items to be updated.</param>
        public void Insert(IEnumerable<dynamic> items)
        {
            if (!items.Any())
            {
                return;
            }
            List<MetaSqliteRow> sqliteRow = new List<MetaSqliteRow>();

            //Create intermediary rows for each object
            foreach (var item in items)
            {
                var intermediaryRow = GetOrCreateCachedSqliteRow(item);
                sqliteRow.Add(intermediaryRow);
            }

            Type type = items.First().GetType();

            //Create statement similar to "INSERT INTO TABLE (column1, column2, column3)", excluding the primary key 
            var firstRow = sqliteRow.First();
            string columnNames = string.Join(",", firstRow.MetaSqliteCells.Where(x => !x.IsPrimaryId).Select(x => x.ColumnName));
            StringBuilder sb = new StringBuilder($"INSERT INTO {firstRow.TableName}({columnNames})");
            sb.AppendLine();
            sb.AppendLine("VALUES");

            //Create line for each row similar to:
            //VALUES(value1,value2,value3),
            //      (value1,value2,value3)
            foreach (var row in sqliteRow)
            {
                //Get each non-primary key column
                string values = string.Join(",", row.MetaSqliteCells.Where(x => !x.IsPrimaryId).Select(x => '\'' + x.Value + '\''));
                sb.AppendLine($"({values}),");
            }
            //Remove the final newline and comma
            sb.Remove(sb.Length - 3, 3);
            var transaction = Connection.BeginTransaction();
            SqliteCommand insertCommand = Connection.CreateCommand();
            insertCommand.Transaction = transaction;
            insertCommand.CommandText = sb.ToString();
            //Update the records in the database
            insertCommand.ExecuteNonQuery();

            //Grab the last inserted id so we can properly update the base objects
            SqliteCommand lastInsertIdSelect = Connection.CreateCommand();
            //Use a transaction so that we can get an accurate id for the new rows
            lastInsertIdSelect.Transaction = transaction;
            lastInsertIdSelect.CommandText = $"SELECT last_insert_rowid() FROM {firstRow.TableName}";
            //Note that this only supports Int32 instead of SQLite's default Int64
            int lastId = Convert.ToInt32(lastInsertIdSelect.ExecuteScalar());

            //Start from the first id and populate the property on each object
            int nextId = lastId - items.Count() + 1;
            MetaSqliteRow metaSqliteRow = GetOrCreateCachedSqliteRow(type);
            foreach (var item in items)
            {
                MethodInfo primaryKeySetter = metaSqliteRow.MetaSqliteCells.Where(x => x.IsPrimaryId).First().SetValueMethod;
                primaryKeySetter.Invoke(item, new object[] { nextId++ });
            }

            transaction.Commit();
        }

        /// <summary>
        /// Inserts the passed items to the matching SQLite table, and updates the integer primary key of the object.
        /// </summary>
        /// <param name="items">The object to be inserted within SQLite.</param>
        public void Insert(dynamic item)
        {
            Insert(new List<dynamic> { item });
        }

        /// <summary>
        /// Updates the passed items within the matching SQLite table, and updates the interger primary keys of the objects.
        /// </summary>
        /// <param name="items">And IEnumerable of objects to be updated within SQLite.</param>
        public void Update(IEnumerable<dynamic> items)
        {
            if (!items.Any())
            {
                return;
            }
            SqliteTransaction transaction = Connection.BeginTransaction();
            foreach (var item in items)
            {
                MetaSqliteRow sqliteRow = GetOrCreateCachedSqliteRow(item);
                string updateStatement = sqliteRow.GetUpdateStatement();
                SqliteCommand command = Connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = updateStatement;
                command.ExecuteNonQuery();
            }
            transaction.Commit();
        }

        /// <summary>
        /// Updates the passed item within the matching SQLite table, and then sets the object's primary key.
        /// </summary>
        /// <param name="item">The item to be updated within SQLite.</param>
        public void Update(dynamic item)
        {
            Update(new List<dynamic> { item });
        }

        /// <summary>
        /// Delete the passed items from the matching SQLite table.
        /// </summary>
        /// <param name="items">An IEnumerable of items to be deleted.</param>
        public void Delete(IEnumerable<dynamic> items)
        {
            if (!items.Any())
            {
                return;
            }
            SqliteTransaction transaction = Connection.BeginTransaction();
            foreach (var item in items)
            {
                MetaSqliteRow sqliteRow = GetOrCreateCachedSqliteRow(item);
                string deleteStatement = sqliteRow.GetDeleteStatement();
                SqliteCommand command = Connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = deleteStatement;
                command.ExecuteNonQuery();
            }
            transaction.Commit();
        }

        /// <summary>
        /// Delete the passed item from the matching SQLite table.
        /// </summary>
        /// <param name="item">The item to be deleted.</param>
        public void Delete(dynamic item)
        {
            Delete(new List<dynamic> { item });
        }

        /// <summary>
        /// Execute a raw SQLite statement expecting no results.
        /// </summary>
        /// <param name="statement">The statement to be executed.</param>
        public void ExecuteRawNonQuery(string statement)
        {
            SqliteCommand command = Connection.CreateCommand();
            command.CommandText = statement;
            command.ExecuteNonQuery();
        }

        /// <summary>
        /// Execute a raw SQLite query expecting results from type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type of results to expect.</typeparam>
        /// <param name="query">The query to be executed.</param>
        /// <returns>An IEnumerable of type <typeparamref name="T"/> populated with results.</returns>
        public IEnumerable<T> ExecuteRawQuery<T>(string query)
        {
            List<T> results = new List<T>();

            MetaSqliteRow metaSqliteRow = GetOrCreateCachedSqliteRow(typeof(T));

            SqliteCommand command = Connection.CreateCommand();
            command.CommandText = query;
            var reader = command.ExecuteReader();

            while (reader.Read())
            {
                var nextResult = (T)Activator.CreateInstance(typeof(T));
                int index = 0;
                foreach (MetaSqliteCell sqliteCell in metaSqliteRow.MetaSqliteCells)
                {
                    var value = reader.GetValue(index++);
                    if (Type.GetTypeCode(value.GetType()) == TypeCode.Int64)
                    {
                        sqliteCell.SetValueMethod.Invoke(nextResult, new object[] { Convert.ToInt32(value) });
                    }
                    else
                    {
                        sqliteCell.SetValueMethod.Invoke(nextResult, new object[] { value });
                    }
                }
                results.Add(nextResult);
            }
            return results;
        }

        /// <summary>
        /// Verify the table format for a given type and create the table if it doesn't exist in SQLite already.
        /// </summary>
        /// <typeparam name="T">The type to analyze and verify for SQLite EZ Mode compatibility.</typeparam>
        public void VerifyType<T>()
        {
            var intermediaryRow = GetOrCreateCachedSqliteRow(typeof(T));
            //Confirm that the table exists
            var command = Connection.CreateCommand();
            command.CommandText = $"SELECT name from sqlite_master WHERE type='table' AND name='{intermediaryRow.TableName}'";
            var reader = command.ExecuteReader();
            if (!reader.Read())
            {
                //Table can't be found, create table
                string createStatement = intermediaryRow.GetCreateStatement();
                command = Connection.CreateCommand();
                command.CommandText = createStatement;
                command.ExecuteNonQuery();
            }
        }

        private MetaSqliteRow GetOrCreateCachedSqliteRow(Type type)
        {
            return GetOrCreateCachedSqliteRow(Activator.CreateInstance(type));
        }

        private MetaSqliteRow GetOrCreateCachedSqliteRow(dynamic targetObject)
        {
            if (OperationMode == OperationModes.EXPLICIT_TAGGING)
            {
                Type rowType = targetObject.GetType();
                //Check if reflection has already been cached
                if (rowSchemaMap.ContainsKey(rowType))
                { //Used cached values to save on reflection
                    MetaSqliteRow newRow = new MetaSqliteRow();
                    MetaSqliteRow cachedRow = rowSchemaMap[rowType];

                    newRow.TableName = cachedRow.TableName;
                    //We need to update values based off of the object parameter, but many properties can be set from cached values
                    foreach (MetaSqliteCell sqliteCellAttribute in cachedRow.MetaSqliteCells)
                    {
                        MetaSqliteCell cachedCell = cachedRow.MetaSqliteCells.Where(x => x.ColumnName.Equals(sqliteCellAttribute.ColumnName)).FirstOrDefault();
                        object rawValue = sqliteCellAttribute.GetValueMethod.Invoke(targetObject, new object[] { });
                        string value = rawValue == null ? null : rawValue.ToString();
                        newRow.MetaSqliteCells.Add(new MetaSqliteCell(cachedCell.CellAttribute, cachedCell.GetValueMethod, cachedCell.SetValueMethod)
                        {
                            //DataType will be the same as the cached property
                            DataType = sqliteCellAttribute.DataType,
                            //ColumnName will be the same as the cached property
                            ColumnName = sqliteCellAttribute.ColumnName,
                            //Invoke the cached getter method for the value
                            Value = value,
                            IsPrimaryId = sqliteCellAttribute.IsPrimaryId
                        });
                    }
                    return newRow;
                }
                else
                {
                    MetaSqliteRow newCacheRow = new MetaSqliteRow();
                    //Get table name
                    SqliteTableAttribute tableAttribute = ((SqliteTableAttribute[])targetObject.GetType().GetCustomAttributes(typeof(SqliteTableAttribute), inherit: false)).FirstOrDefault();
                    if (tableAttribute == null)
                    {
                        throw new NotSupportedException("Expected Class attribute \"SqliteTable\"");
                    }
                    newCacheRow.TableName = tableAttribute.TableName;
                    var propertyInfoList = targetObject.GetType().GetProperties();
                    //Process properties
                    foreach (PropertyInfo propertyInfo in propertyInfoList)
                    {
                        SqliteCellAttribute cellAttribute = ((SqliteCellAttribute[])propertyInfo.GetCustomAttributes(typeof(SqliteCellAttribute), inherit: false)).FirstOrDefault();
                        if (cellAttribute == null)
                        {
                            //Skip attributes that aren't tagged with EZ-Mode data types
                            continue;
                        }
                        var cellData = new MetaSqliteCell(cellAttribute, propertyInfo.GetGetMethod(), propertyInfo.GetSetMethod())
                        {
                            DataType = cellAttribute.DataType,
                            //Get the column name from the attribute first, then the property as a backup
                            ColumnName = cellAttribute.ColumnName != null ? cellAttribute.ColumnName : propertyInfo.Name,
                            Value = propertyInfo.GetValue(targetObject, null)?.ToString(),
                            IsPrimaryId = cellAttribute.IsPrimaryId,
                        };
                        newCacheRow.MetaSqliteCells.Add(cellData);
                    }
                    //Add the row to the schema map for easier traversal in the future
                    rowSchemaMap.Add(rowType, newCacheRow);
                    return newCacheRow;
                }
            }
            throw new NotSupportedException("Unsupported operation mode for caching.");
        }

        public void Dispose()
        {
            this.Connection.Close();
        }
    }
}
