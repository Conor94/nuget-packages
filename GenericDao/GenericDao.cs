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
            return (bool)ExecuteCommand($"CREATE TABLE IF NOT EXISTS {tableName} ({columns})", (command) =>
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

            DataRowCollection colMetadata = GetColumnMetadata(tableName);
            for (int i = 0; i < colMetadata.Count; i++)
            {
                DataRow col = colMetadata[i];

                if (!(bool)col[SchemaTableOptionalColumn.IsAutoIncrement])
                {
                    DbParameter parameter = (DbParameter)Activator.CreateInstance(dbParameterType);
                    parameter.ParameterName = $"@{col[SchemaTableColumn.ColumnName]}";
                    parameters.Add(parameter);
                }
            }

            StringBuilder valueBuilder = new StringBuilder();
            PropertyInfo[] propInfo = data.GetType().GetProperties();
            for (int i = 0; i < parameters.Count; i++)
            {
                // Look for a property in the data object that matches the parameter name
                PropertyInfo prop = propInfo.ToList().Find(p => p.Name == parameters[i].ParameterName.Replace("@", ""));
                if (prop != null)
                {
                    parameters[i].Value = prop.GetValue(data);
                }
            }

            string[] valsArray = new string[parameters.Count];
            string[] colsArray = new string[parameters.Count];
            for (int i = 0; i < parameters.Count; i++)
            {
                colsArray[i] = parameters[i].ParameterName.Replace("@", "");
                valsArray[i] = parameters[i].ParameterName;
            }

            return (bool)ExecuteCommand($"INSERT INTO {tableName} ({string.Join(",", colsArray)}) VALUES ({string.Join(",", valsArray)})", (command) =>
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

        public List<T> ReadData<T>(string tableName, Func<DbDataReader, T> converter, string[] columnNames, WhereCondition[] conditions = null, OrderBy orderBy = null)
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

            return (List<T>)ExecuteCommand(commandText, (command) =>
            {
                DbDataReader reader = command.ExecuteReader();

                List<T> data = new List<T>();
                while (reader.Read())
                {
                    data.Add(converter(reader));
                }

                return data;
            }, parameters);
        }

        public List<T> ReadData<T>(string tableName, Func<DbDataReader, T> converter, WhereCondition[] conditions = null, OrderBy orderBy = null)
        {
            return ReadData(tableName, converter, null, conditions, orderBy);
        }

        private object ExecuteCommand(string commandText, Func<DbCommand, object> invoker, List<IDbDataParameter> parameters = null)
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

                    return invoker.DynamicInvoke(command);
                }
            }
        }

        private DataRowCollection GetColumnMetadata(string tableName)
        {
            return (DataRowCollection)ExecuteCommand($"SELECT * FROM {tableName} WHERE 1 = 0", (command) =>
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
