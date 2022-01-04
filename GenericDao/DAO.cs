using GenericDAO.Adapters;
using GenericDAO.Enums;
using GenericDAO.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Data.SQLite;
using System.Linq;
using System.Reflection;

namespace GenericDAO
{
    /// <summary>Generic data access object for SQL and SQLite databases.</summary>
    public class DAO
    {
        private readonly string CONN_STR;

        private Type dbConnectionType;
        private Type dbCommandType;
        private Type dbParameterType;

        private WhereAdapter whereAdapter;

        public DAO(string connstr, DatabaseType type)
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
                    dbConnectionType = typeof(SQLiteConnection);
                    dbCommandType = typeof(SQLiteCommand);
                    dbParameterType = typeof(SQLiteParameter);
                    break;
                default:
                    throw new Exception("Unsupported database type.");
            }
        }

        #region Public functions
        /// <summary>Creates a table.</summary>
        /// <param name="tableName">The name of the table.</param>
        /// <param name="columns">The column definitions. Include the name of column, data type, and any constraints.</param>
        /// <param name="createIfNotExists">Specifies whether the IF NOT EXISTS option should be used when creating the table. 
        /// <see langword="true"/> if it should be used, and <see langword="false"/> if it shouldn't.</param>
        public void CreateTable(string tableName, string columns, bool createIfNotExists = true)
        {
            ExecuteCommand<object>($"CREATE TABLE {(createIfNotExists ? "IF NOT EXISTS" : "")} {tableName} ({columns})", (command) =>
            {
                command.ExecuteNonQuery();
                return null;
            });
        }

        /// <summary>Inserts data into a table.</summary>
        /// <typeparam name="T">Type of data that is being inserted.</typeparam>
        /// <param name="tableName">Name of the table that's being inserted into.</param>
        /// <param name="data">The data that's being inserted.</param>
        /// <returns><see langword="true"/> if the any rows were inserted and <see langword="false"/> if no rows were inserted.</returns>
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

        /// <summary>Reads data from the database.</summary>
        /// <typeparam name="TData">The type of data that's being read.</typeparam>
        /// <param name="tableName">Name of the table that data is being read from.</param>
        /// <param name="converter">Function that is used to convert the data from a <see cref="DbDataReader"/> to a <typeparamref name="TData"/> object.</param>
        /// <param name="columnNames">Column names that are being read. If this value is <see langword="null"/>, all columns will be read.</param>
        /// <param name="conditions">Array of objects that represent SQL where statements.</param>
        /// <param name="orderBy">Object that represents a SQL order by statement.</param>
        /// <returns>The data that was read.</returns>
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

        /// <summary>Reads data from the database.</summary>
        /// <typeparam name="TData">The type of data that's being read.</typeparam>
        /// <param name="tableName">Name of the table that data is being read from.</param>
        /// <param name="converter">Function that is used to convert the data from a <see cref="DbDataReader"/> to a <typeparamref name="TData"/> object.</param>
        /// <param name="conditions">Array of objects that represent SQL where statements.</param>
        /// <param name="orderBy">Object that represents a SQL order by statement.</param>
        /// <returns>The data that was read.</returns>
        public List<TData> ReadData<TData>(string tableName, Func<DbDataReader, TData> converter, WhereCondition[] conditions = null, OrderBy orderBy = null)
        {
            return ReadData(tableName, converter, null, conditions, orderBy);
        }

        /// <summary>Updates a record in a table.</summary>
        /// <typeparam name="TData">The type of data that's being updated.</typeparam>
        /// <param name="tableName">Name of the table that's being updated.</param>
        /// <param name="data">The data that is being updated.</param>
        /// <param name="conditions">Array of objects that represent SQL where statements. If this value is <see langword="null"/>, the primary
        /// key for the data is used for the where condition.</param>
        /// <returns><see langword="true"/> if any records were updated and <see langword="false"/> if no records were updated.</returns>
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

        /// <summary>Deletes data from the database.</summary>
        /// <param name="tableName">Name of the table that data is being deleted from.</param>
        /// <param name="conditions">Conditions that define what data will be deleted.</param>
        /// <returns>The number of rows that were deleted.</returns>
        public int DeleteData(string tableName, WhereCondition[] conditions)
        {
            CreateWhereStatement(conditions, out string whereStr, out List<IDbDataParameter> parameters);

            return ExecuteCommand<int>($"DELETE FROM {tableName} " +
                                       $"WHERE {whereStr}", (command) =>
            {
                return command.ExecuteNonQuery();
            }, parameters);
        }
        #endregion

        #region Private functions
        /// <summary>Creates a connection, opens it, and creates a command that uses the connection. This function also adds command text and parameters to the command.</summary>
        /// <typeparam name="TReturn">The type of data returned by the <paramref name="invoker"/>.</typeparam>
        /// <param name="commandText">The SQL statement that will be executed.</param>
        /// <param name="invoker">The function that will invoke the <see cref="DbCommand"/>.</param>
        /// <param name="parameters">The parameters that will be provided to the <see cref="DbCommand"/>.</param>
        /// <returns>The data that is returned by executing the command (e.g. the data returned by the SQL query).</returns>
        private TReturn ExecuteCommand<TReturn>(string commandText, Func<DbCommand, TReturn> invoker, List<IDbDataParameter> parameters = null)
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

        /// <summary>Creates a connection, opens it, and creates a command that uses the connection. The purpose of this overload is to allow the <paramref name="invoker"/>
        /// to provide <see cref="DbCommand.CommandText"/> and <see cref="DbCommand.Parameters"/> to the <see cref="DbCommand"/>.</summary>
        /// <typeparam name="TReturn">The type of data returned by the <paramref name="invoker"/>.</typeparam>
        /// <param name="invoker">The function that will execute the <see cref="DbCommand"/>. For this overloaded function, the invoker is also responsible
        /// for providing <see cref="DbCommand.CommandText"/> and <see cref="DbCommand.Parameters"/>.</param>
        /// <returns>The data that is returned by executing the command (e.g. the data returned by the SQL query).</returns>
        private TReturn ExecuteCommand<TReturn>(Func<DbCommand, TReturn> invoker)
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

        /// <summary>Creates a string and list of parameters for a SQL set statement from the provided table name and data.</summary>
        /// <remarks>A set statement is used to update data.</remarks>
        /// <param name="tableName">Name of the table that the set statement is being created for.</param>
        /// <param name="data">Data that is being used to create the set statement.</param>
        /// <param name="setStatement">The set statement that was prepared by this function.</param>
        /// <param name="parameters">The parameters that were prepared by this function.</param>
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

        /// <summary>Creates a string and list of parameters for a SQL where statement from an array of <see cref="WhereCondition"/>.</summary>
        /// <param name="conditions">Array of objects that are used to create the where string and parameters.</param>
        /// <param name="whereStatement">The where statement that was prepared by this function.</param>
        /// <param name="parameters">The parameters that were prepared by this function.</param>
        private void CreateWhereStatement(WhereCondition[] conditions, out string whereStatement, out List<IDbDataParameter> parameters)
        {
            parameters = new List<IDbDataParameter>();
            whereStatement = "";
            for (int i = 0; i < conditions.Length; i++)
            {
                DbParameter parameter = (DbParameter)Activator.CreateInstance(dbParameterType);
                parameter.ParameterName = $"@where{i}_{conditions[i].LeftSide}";
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

                whereStatement += $"{conditions[i].LeftSide} {comparisonOperator} @where{i}_{conditions[i].LeftSide}";
                if (i != (conditions.Length - 1))
                {
                    whereStatement += " AND ";
                }
            }
        }

        /// <summary>Gets metadata for all columns for a table.</summary>
        /// <param name="tableName">The name of the table.</param>
        /// <returns><see cref="DataRowCollection"/> that holds column metadata. Each <see cref="DataRow"/> in the collection
        /// has metadata for a column in the table.</returns>
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
