using GenericDao.Adapters;
using GenericDao.Enums;
using GenericDao.Models;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;

namespace GenericDao
{
    /// <summary>Generic data access object for SQL and SQLite databases.</summary>
    public class GenericDao
    {
        private readonly string CONN_STR;

        private Type dbConnectionType;
        private Type dbCommandType;
        private Type dbParameterType;

        private WhereAdapter whereAdapter;

        public GenericDao(string connstr, DatabaseType type)
        {
            CONN_STR = connstr;

            whereAdapter = new WhereAdapter();

            switch (type)
            {
                case DatabaseType.Sql:
                    dbConnectionType = typeof(SqlConnection);
                    dbCommandType = typeof(SqlCommand);
                    dbParameterType = typeof(SqlParameter);
                    break;
                case DatabaseType.Sqlite:
                    dbConnectionType = typeof(SqliteConnection);
                    dbCommandType = typeof(SqliteCommand);
                    dbParameterType = typeof(SqliteParameter);
                    break;
                default:
                    throw new Exception("Unsupported database type.");
            }
        }

        #region Public functions
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

        public List<TData> ReadData<TData>(string tableName, Func<DbDataReader, TData> converter, string[] columnNames, WhereCondition[] conditions = null, OrderBy orderBy = null)
        {
            string commandText = $"SELECT {(columnNames == null ? "*" : string.Join(",", columnNames))} FROM {tableName}{(conditions != null ? " WHERE" : "")}";

            List<IDbDataParameter> parameters = null;
            if (conditions != null)
            {
                CreateWhereStatement(conditions, out string where, out parameters);
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

        public bool UpdateData<TData>(string tableName, TData data, WhereCondition[] conditions = null)
        {
            return ExecuteCommand<bool>((command) =>
            {
                PropertyInfo[] propInfo = data.GetType().GetProperties();
                DataRowCollection colMetadata = GetColumnMetadata(tableName);

                // Build the set statement
                CreateSetStatement(tableName, data, out string setStatement, out List<IDbDataParameter> setParameters);

                // Build the where statement
                string whereStr = null;
                List<IDbDataParameter> whereParameters = null;
                if (conditions != null && conditions.Length > 0)
                {
                    CreateWhereStatement(conditions, out whereStr, out whereParameters); // Create a string and parameters for the where statement
                }
                else
                {
                    // Create a where statement for the primary key if where conditions were not provided
                    for (int i = 0; i < colMetadata.Count; i++)
                    {
                        if ((bool)colMetadata[i][SchemaTableOptionalColumn.IsAutoIncrement])
                        {
                            string colName = colMetadata[i][SchemaTableColumn.ColumnName].ToString();
                            PropertyInfo prop = propInfo.ToList().Find(p => p.Name == colName);
                            object val = prop.GetValue(data);

                            CreateWhereStatement(new WhereCondition[]
                            {
                                new WhereCondition(colName, val, WhereOperator.Equal)
                            },
                            out whereStr,
                            out whereParameters);
                        }
                    }
                }

                command.CommandText = $"UPDATE {tableName} " +
                                      $"SET {setStatement} " +
                                      $"WHERE {whereStr}";
                command.Parameters.AddRange(whereParameters.ToArray());
                command.Parameters.AddRange(setParameters.ToArray());

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
        #endregion

        #region Private functions
        // Creates a connection, opens it, and creates a command that uses the connection. This function also adds command text and parameters to the command.
        private TReturn ExecuteCommand<TReturn>(string commandText, Func<DbCommand, object> invoker, List<IDbDataParameter> parameters = null)
        {
            using (DbConnection conn = (DbConnection)Activator.CreateInstance(dbConnectionType))
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
            using (DbConnection conn = (DbConnection)Activator.CreateInstance(dbConnectionType))
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

        private void CreateSetStatement<TData>(string tableName, TData data, out string setStatement, out List<IDbDataParameter> parameters)
        {
            // Initialize returns
            parameters = new List<IDbDataParameter>();
            setStatement = "";

            // Get column and property metadata
            DataRowCollection columnMetadata = GetColumnMetadata(tableName);
            PropertyInfo[] propInfo = data.GetType().GetProperties();

            for (int i = 0; i < columnMetadata.Count; i++)
            {
                if (!(bool)columnMetadata[i][SchemaTableColumn.IsKey]) // Don't add the primary key to the set statement
                {
                    DbParameter parameter = (DbParameter)Activator.CreateInstance(dbParameterType);

                    // Get the column name
                    string colName = columnMetadata[i][SchemaTableColumn.ColumnName].ToString();
                    parameter.ParameterName = $"@set_{colName}";

                    // Get a value for the parameter
                    parameter.Value = propInfo.ToList().Find((prop) => prop.Name == colName).GetValue(data);

                    parameters.Add(parameter);

                    // Append to the set statement string
                    setStatement += $"{colName} = {parameter.ParameterName}";
                    if (i != (columnMetadata.Count - 1))
                    {
                        setStatement += ", ";
                    }
                }
            }
        }
        
        private void CreateWhereStatement(WhereCondition[] conditions, out string whereStatement, out List<IDbDataParameter> parameters)
        {
            parameters = new List<IDbDataParameter>();
            whereStatement = "";
            for (int i = 0; i < conditions.Length; i++)
            {
                DbParameter parameter = (DbParameter)Activator.CreateInstance(dbParameterType);
                parameter.ParameterName = $"@where_{conditions[i].LeftSide}";
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

                whereStatement += $"{conditions[i].LeftSide} {comparisonOperator} @where_{conditions[i].LeftSide}";
                if (i != (conditions.Length - 1))
                {
                    whereStatement += " AND ";
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
        #endregion
    }
}
