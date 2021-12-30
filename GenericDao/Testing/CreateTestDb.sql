
CREATE DATABASE Test

USE [Test]

-- Create a table for testing
CREATE TABLE [Person]
(
	id int IDENTITY(1, 1),
	name nvarchar(100)

	CONSTRAINT PK_Person_id PRIMARY KEY (id)
)

-- Test inserting
INSERT INTO Person (name)
	VALUES ('Harry Potter')
SELECT * FROM Person
