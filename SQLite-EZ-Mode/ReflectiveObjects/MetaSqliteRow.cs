using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Reflection;

namespace SQLiteEZMode.ReflectiveObjects
{
    internal class MetaSqliteRow
    {

        public string TableName { get; set; }
        public List<MetaSqliteCell> MetaSqliteCells { get; }

        public MetaSqliteRow()
        {
            this.MetaSqliteCells = new List<MetaSqliteCell>();
        }

        public string GetCreateStatement()
        {
            return $"CREATE TABLE {this.TableName}({string.Join(",", this.MetaSqliteCells.Select(x => x.GetCellCreateInfo())) })";
        }

        public string GetSelectStatementWithId(int primaryKeyId)
        {
            string primaryKeyName = MetaSqliteCells.Where(x => x.IsPrimaryId).FirstOrDefault().ColumnName;
            return $"SELECT {string.Join(",", MetaSqliteCells.Select(x => x.ColumnName))} FROM {this.TableName} WHERE {primaryKeyName} = {primaryKeyId}";
        }

        public string GetSelectStatement()
        {
            return $"SELECT {string.Join(",", MetaSqliteCells.Select(x => x.ColumnName))} FROM {this.TableName}";
        }

        public string GetUpdateStatement()
        {
            string updateClause = string.Join(",", MetaSqliteCells.Where(x=>!x.IsPrimaryId).Select(x => x.ColumnName + " = '" + x.Value + "'"));
            string whereClause = MetaSqliteCells.Where(x => x.IsPrimaryId).Select(x => x.ColumnName + " = " + x.Value).Single();
            return $"UPDATE {this.TableName} SET {updateClause} WHERE {whereClause}";
        }

        public string GetDeleteStatement()
        {
            string whereClause = MetaSqliteCells.Where(x => x.IsPrimaryId).Select(x => x.ColumnName + " = " + x.Value).Single();
            return $"DELETE FROM {this.TableName} WHERE {whereClause}";
        }
    }
}
