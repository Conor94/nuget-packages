using GenericDao.Enums;
using GenericDao.Models;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Configuration;

namespace GenericDao
{
    class Program
    {
        private static readonly string SQLITE_CONN_STR = ConfigurationManager.ConnectionStrings["SQLiteExampleDb"].ConnectionString;
        private static readonly string SQL_CONN_STR = ConfigurationManager.ConnectionStrings["SQLExampleDb"].ConnectionString;

        private const string PERSON_TABLE_NAME = "Person";
        private const string PERSON_COLUMNS = "id INTEGER PRIMARY KEY AUTOINCREMENT, " +
                                              "name TEXT," +
                                              "age INTEGER";

        private static readonly Person[] samplePeople = new Person[]
        {
            new Person() { name = "Ted Mosby", age = 31 },
            new Person() { name = "Marshall Erickson", age = 27},
            new Person() { name = "Lily Aldrin", age = 34 },
            new Person() { name = "Barney Stinson", age = 38 },
            new Person() { name = "Robin Scherbatsky", age = 33 }
        };

        static void Main(string[] args)
        {
            // Test the DAO using a SQLite database
            GenericDao<SqliteConnection> sqlLiteDao = new GenericDao<SqliteConnection>(SQLITE_CONN_STR);

            // Test creating a table
            sqlLiteDao.CreateTable(PERSON_TABLE_NAME, PERSON_COLUMNS);

            // Test inserting data
            foreach (Person person in samplePeople)
            {
                sqlLiteDao.InsertData("Person", person);
            }

            // Test reading data
            List<Person> people = TestSqliteRead(sqlLiteDao);
            Console.WriteLine("People table from SQLite database");
            Console.WriteLine("---------------------------------");
            foreach (Person p in people)
            {
                Console.WriteLine($"{p.id}, {p.name}, {p.age}");
            }

            // Test updating data
            people[1].name = "Marshall Erickson UPDATE";
            people[1].age = 40;
            Console.WriteLine($" >> {sqlLiteDao.UpdateData("Person", people[1])} records updated");

            people[5].name = "Conor Barr UPDATE 2";
            people[5].age = 19;
            Console.WriteLine($" >> {sqlLiteDao.UpdateData("Person", people[5])} records updated");

            people = TestSqliteRead(sqlLiteDao);
            Console.WriteLine("After updating");
            Console.WriteLine("--------------");
            foreach (Person p in people)
            {
                Console.WriteLine($"{p.id}, {p.name}, {p.age}");
            }

            // Test the DAO using a SQL database
            //GenericDao<SqlConnection> sqlDao = new GenericDao<SqlConnection>(SQL_CONN_STR);
            //sqlDao.InsertData("Person", new Person() { id = 5, name = "Barney Stinson (That guy's awesome!)" });
            //List<Person> sqlPeople = sqlDao.ReadData("Person", (reader) =>
            //{
            //    return new Person()
            //    {
            //        id = Convert.ToInt32(reader["id"]),
            //        name = reader["name"].ToString()
            //    };
            //});

            //Console.WriteLine("People table from SQL database");
            //Console.WriteLine("------------------------------");
            //foreach (Person p in sqlPeople)
            //{
            //    Console.WriteLine($"{p.id}, {p.name}");
            //}
        }

        private static List<Person> TestSqliteRead(GenericDao<SqliteConnection> dao)
        {
            return dao.ReadData("Person", (reader) =>
            {
                return new Person()
                {
                    id = Convert.ToInt32(reader["id"]), // Since this column is excluded, it can't be read
                    name = reader?["name"].ToString(),
                    age = Convert.ToInt32(reader?["age"])
                };
            }, new string[] { "id", "name", "age" }, // Example of excluding a column
            //new WhereCondition[] { new WhereCondition("id", 3, WhereOperator.LessThan) },
            null, // comment out this line and uncomment line above to test WHERE statements
            new OrderBy(new string[] { "id" }));
        }
    }

}
