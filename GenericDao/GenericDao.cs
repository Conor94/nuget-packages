using Microsoft.Data.Sqlite;
using GenericDao.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Text;
using GenericDao.Adapters;

namespace GenericDao
{
    /// <summary>
    /// Generic data access object for SQL and SQLite databases. 
    /// </summary>
    /// <typeparam name="TDatabase"></typeparam>
    public class GenericDao<TDatabase> where TDatabase : IDbConnection
    {
        private readonly string CONN_STR;

        private Type dbCommandType;
        private Type dbParameterType;

        private WhereAdapter whereAdapter;

        public GenericDao(string connstr)
        {
            CONN_STR = connstr;

            whereAdapter = new WhereAdapter();

            if (typeof(TDatabase) == typeof(SqliteConnection))
            {
                dbCommandType = typeof(SqliteCommand);
                dbParameterType = typeof(SqliteParameter);
            }
            else if (typeof(TDatabase) == typeof(SqlConnection))
            {
                dbCommandType = typeof(SqlCommand);
                dbParameterType = typeof(SqlParameter);
            }
            else
            {
                throw new Exception("Unsupported database type.");
            }
        }

        public bool CreateTable(string tableName, string columns)
        {
            return ExecuteCommand<bool>($"CREATE TABLE IF NOT EXISTS {tableName} ({columns})", (command) =>
            {
                if (command.ExecuteNonQuery() > 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            });
        }

        public bool InsertData<T>(string tableName, T data)
        {
            List<IDbDataParameter> parameters = new List<IDbDataParameter>();
            
            PropertyInfo[] propInfo = data.GetType().GetProperties();

            DataRowCollection colMetadata = GetColumnMetadata(tableName);
            for (int i = 0; i < colMetadata.Count; i++)
            {
                DataRow col = colMetadata[i];

                if (!(bool)col[SchemaTableOptionalColumn.IsAutoIncrement])
                {
                    string colName = col[SchemaTableColumn.ColumnName].ToString();

                    DbParameter parameter = (DbParameter)Activator.CreateInstance(dbParameterType);
                    parameter.ParameterName = $"@{colName}";
                    parameters.Add(parameter);

                    PropertyInfo prop = propInfo.ToList().Find(p => p.Name == colName);

                    if (prop != null)
                    {
                        parameter.Value = prop.GetValue(data);
                    }
                }
            }

            string[] valsArray = new string[parameters.Count];
            string[] colsArray = new string[parameters.Count];
            for (int i = 0; i < parameters.Count; i++)
            {
                colsArray[i] = parameters[i].ParameterName.Replace("@", "");
                valsArray[i] = parameters[i].ParameterName;
            }

            return ExecuteCommand<bool>($"INSERT INTO {tableName} ({string.Join(",", colsArray)}) VALUES ({string.Join(",", valsArray)})", (command) =>
            {
                if (command.ExecuteNonQuery() > 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }, parameters);
        }

        public int UpdateData<TData>(string tableName, TData data)
        {
            return ExecuteCommand<int>((command) =>
            {
                int recordsAffected = 0;

                PropertyInfo[] propInfo = data.GetType().GetProperties();
                DataRowCollection colMetadata = GetColumnMetadata(tableName);

                // Need to make sure we have the primary key before doing any updates
                DbParameter primaryKeyParameter = (DbParameter)Activator.CreateInstance(dbParameterType);
                string primaryKeyColName = "";
                for (int i = 0; i < colMetadata.Count; i++)
                {
                    // Get primary key column name so it can be used in the WHERE statement
                    if ((bool)colMetadata[i][SchemaTableOptionalColumn.IsAutoIncrement])
                    {
                        primaryKeyColName = colMetadata[i][SchemaTableColumn.ColumnName].ToString();

                        // Assign column name to parameter
                        primaryKeyParameter.ParameterName = $"@{primaryKeyColName}";

                        // Assign value to parameter
                        PropertyInfo prop = propInfo.ToList().Find(p => p.Name == primaryKeyColName);
                        primaryKeyParameter.Value = prop.GetValue(data);
                    }
                }
                
                // Execute an update statement for each column. Separate update statements are used so that only columns that are
                // actually changed get updated. The way this is being done prevents updating all columns in a single statement
                // (if one column isn't changed, it would prevent all columns from being changed).
                command.Transaction = command.Connection.BeginTransaction(); // Perform all update statements in a single transaction
                for (int i = 0; i < colMetadata.Count; i++)
                {
                    DataRow col = colMetadata[i];

                    string colName = col[SchemaTableColumn.ColumnName].ToString();

                    // Add parameter name and value
                    DbParameter parameter = (DbParameter)Activator.CreateInstance(dbParameterType);
                    parameter.ParameterName = $"@{colName}";
                    PropertyInfo prop = propInfo.ToList().Find(p => p.Name == colName);
                    if (prop != null)
                    {
                        parameter.Value = prop.GetValue(data);
                    }

                    command.CommandText = $"UPDATE {tableName} " +
                                          $"SET {colName} = @{colName} " +
                                          $"WHERE {colName} != @{colName} AND {primaryKeyColName} = @{primaryKeyColName}";
                    command.Parameters.Add(parameter);
                    
                    recordsAffected += command.ExecuteNonQuery();
                }
                command.Transaction.Commit(); // Commit all update statements

                return recordsAffected;
            });


        }

        public List<TData> ReadData<TData>(string tableName, Func<DbDataReader, TData> converter, string[] columnNames, WhereCondition[] conditions = null, OrderBy orderBy = null)
        {
            string commandText = $"SELECT {(columnNames == null ? "*" : string.Join(",", columnNames))} FROM {tableName}{(conditions != null ? " WHERE" : "")}";

            List<IDbDataParameter> parameters = null;
            if (conditions != null)
            {
                CreateWhere(conditions, out string where, out parameters);
                commandText += $" {where}";
            }

            if (orderBy != null)
            {
                commandText += $" ORDER BY {string.Join(",", orderBy.Columns)} {orderBy.Direction}";
            }

            return ExecuteCommand<List<TData>>(commandText, (command) =>
            {
                DbDataReader reader = command.ExecuteReader();

                List<TData> data = new List<TData>();
                while (reader.Read())
                {
                    data.Add(converter(reader));
                }

                return data;
            }, parameters);
        }

        public List<TData> ReadData<TData>(string tableName, Func<DbDataReader, TData> converter, WhereCondition[] conditions = null, OrderBy orderBy = null)
        {
            return ReadData(tableName, converter, null, conditions, orderBy);
        }

        // Creates a connection, opens it, and creates a command that uses the connection. This function also adds command text and parameters to the command.
        private TReturn ExecuteCommand<TReturn>(string commandText, Func<DbCommand, object> invoker, List<IDbDataParameter> parameters = null)
        {
            using (DbConnection conn = (DbConnection)Activator.CreateInstance(typeof(TDatabase)))
            {
                using (DbCommand command = (DbCommand)Activator.CreateInstance(dbCommandType))
                {
                    conn.ConnectionString = CONN_STR;
                    conn.Open();

                    command.Connection = conn;
                    command.CommandText = commandText;
                    if (parameters != null)
                    {
                        command.Parameters.AddRange(parameters.ToArray());
                    }

                    return (TReturn)invoker.DynamicInvoke(command);
                }
            }
        }

        // Creates a connection, opens it, and creates a command that uses the connection.
        private TReturn ExecuteCommand<TReturn>(Func<DbCommand, object> invoker)
        {
            using (DbConnection conn = (DbConnection)Activator.CreateInstance(typeof(TDatabase)))
            {
                using (DbCommand command = (DbCommand)Activator.CreateInstance(dbCommandType))
                {
                    conn.ConnectionString = CONN_STR;
                    conn.Open();

                    command.Connection = conn;

                    return (TReturn)invoker.DynamicInvoke(command);
                }
            }
        }

        private DataRowCollection GetColumnMetadata(string tableName)
        {
            return ExecuteCommand<DataRowCollection>($"SELECT * FROM {tableName} WHERE 1 = 0", (command) =>
            {
                DbDataReader reader = command.ExecuteReader();

                DataTable schemaTable = reader.GetSchemaTable();
                return schemaTable.Rows;
            });
        }

        private void CreateWhere(WhereCondition[] conditions, out string where, out List<IDbDataParameter> parameters)
        {
            parameters = new List<IDbDataParameter>();
            where = "";
            for (int i = 0; i < conditions.Length; i++)
            {
                DbParameter parameter = (DbParameter)Activator.CreateInstance(dbParameterType);
                parameter.ParameterName = $"@{conditions[i].LeftSide}";
                parameter.Value = conditions[i].RightSide;

                parameters.Add(parameter);

                string comparisonOperator = "";
                try
                {
                    comparisonOperator = whereAdapter.ConvertOperator(conditions[i].ComparisonOperator);
                }
                catch (Exception)
                {
                    throw new Exception("Query failed because the comparison operator could not be converted.");
                }

                where += $"{conditions[i].LeftSide} {comparisonOperator} @{conditions[i].LeftSide}";
                if (i != (conditions.Length - 1))
                {
                    where += ",";
                }
            }
        }
    }
}
