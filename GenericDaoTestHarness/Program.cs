﻿using GenericDAO.Enums;
using GenericDAO.Models;
using System;
using System.Collections.Generic;
using System.Configuration;

namespace GenericDAO
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
            new Person() { name = "Terrence Cook", age = 31 },
            new Person() { name = "Eric Ware", age = 27},
            new Person() { name = "Joy Wilks", age = 34 },
            new Person() { name = "Michael Bruce", age = 38 },
            new Person() { name = "Samantha Kane", age = 33 }
        };

        static void Main(string[] args)
        {
            // Test the DAO using a SQLite database
            DAO sqlLiteDao = new DAO(SQLITE_CONN_STR, DatabaseType.Sqlite);

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
            people[1].name = "Eric Ware UPDATE";
            people[1].age = 40;
            Console.WriteLine($" >> {sqlLiteDao.UpdateData("Person", people[1])} records updated");

            if (people.Count >= 6)
            {
                people[5].name = "Samantha Kane";
                people[5].age = 103;
                //Console.WriteLine($" >> {sqlLiteDao.UpdateData("Person", people[5], new WhereCondition[] { new WhereCondition("age", "34", WhereOperator.GreaterThanOrEqual) })} records updated");
                Console.WriteLine($" >> {sqlLiteDao.UpdateData("Person", people[5], new WhereCondition[] { new WhereCondition("id", "1", WhereOperator.GreaterThanOrEqual), new WhereCondition("name", "Samantha Kane") })} records updated");

                people = TestSqliteRead(sqlLiteDao);
                Console.WriteLine("After updating");
                Console.WriteLine("--------------");
                foreach (Person p in people)
                {
                    Console.WriteLine($"{p.id}, {p.name}, {p.age}");
                }
            }

            // Return the number of people with the name "Samantha"
            Console.WriteLine();
            Console.WriteLine($" >> Number of records in Person table = {sqlLiteDao.GetCount("Person", new WhereCondition[] { new WhereCondition("name", "%Samantha%", WhereOperator.Like)})}");
            Console.WriteLine();

            // Testing deleting data
            int rowsDeleted = sqlLiteDao.DeleteData("Person", new WhereCondition[]
            {
                //new WhereCondition("id", 31, WhereOperator.GreaterThanOrEqual),
                //new WhereCondition("id", 35, WhereOperator.LessThanOrEqual),
                new WhereCondition("name", "%W%", WhereOperator.Like),
                //new WhereCondition("name", "('Michael','Samantha')", WhereOperator.In),
                //new WhereCondition("id", "30 AND 40", WhereOperator.Between)
            });

            Console.WriteLine($" >> {rowsDeleted} deleted");

            // Test the DAO using a SQL database
            DAO sqlDao = new DAO(SQL_CONN_STR, DatabaseType.Sql);
            sqlDao.InsertData("Person", new Person() { id = 5, name = "Barney Stinson (That guy's awesome!)" });
            List<Person> sqlPeople = sqlDao.ReadData("Person", (reader) =>
            {
                return new Person()
                {
                    id = Convert.ToInt32(reader["id"]),
                    name = reader["name"].ToString()
                };
            });

            Console.WriteLine("People table from SQL database");
            Console.WriteLine("------------------------------");
            foreach (Person p in sqlPeople)
            {
                Console.WriteLine($"{p.id}, {p.name}");
            }
        }

        private static List<Person> TestSqliteRead(DAO dao)
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
