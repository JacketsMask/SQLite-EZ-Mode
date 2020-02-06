# SQLite EZ Mode

This is a C# library that allows you to utilize existing basic class data structures and quickly store them within a SQLite database. The primary goal of this project is to entirely abstract specific knowledge and implementation that would usually be required for operating within SQLite and instead allow you to develop free of those constraints. It is intended as an alternative to writing boilerplate raw SQLite queries and statements and get back to the core functionality of your project. 

The EZ Mode wrapper and converter has several convenient functions to cover basic needs without resorting to raw SQLite syntax, but it supports raw syntax and direct connection accessibility too in order to cover all bases.

---
## "EZ Mode" Functions:
```c#
VerifyType<T>() //Verifies the viability of a provided object and creates a SQLite table for it if it does not exist.
Insert(...) //Looks up the type of passed dynamic items and inserts them into the table.
Select<T>(...) //Given a provided type and primary key value retrieves the row from the database and converts it into the requested type.
SelectAll<T>() //Given a provided type selects all rows and builds objects of the requested type.
Update(...) //Looks up the type of passed items and updates them within the table.
Delete(...) //Given items, deletes their corresponding rows within the table.
```

---

## "Hard Mode"/Advanced Functions:
```C#
ExecuteRawNonQuery(...) //Given a SQLite statement, execute it directly.
ExecuteRawQuery<T>(...) //Given a SQLite query, execute it and convert results to the provided type. 
db.Connection //Get a reference to the SQLite connection directly for full control.
```

## Example Program:

Here's an example program to show the library can be utilized and how easy it is to set up.
```c#
using SQLiteEZMode;
using SQLiteEZMode.Attributes;

    public class Program
    {
        ///This tag allows you to set the name of the table that will hold "Thing" objects
        [SqliteTableAttribute("Things")]
        public class Thing
        {
            //This tag is setting the data type this property corresponds to, and signals that it is a primary key (mandatory)
            [SqliteCellAttribute(CellDataTypes.INTEGER, isPrimaryId: true)]
            public int Id { get; set; }

            //This attribute sets the data type
            [SqliteCellAttribute(CellDataTypes.TEXT)]
            public string Name { get; set; }

            //This attribute overrides the default column name and sets it to "Description" as well as setting the data type
            [SqliteCellAttribute(columnName: "Description", CellDataTypes.TEXT)]
            public string UnncessarilyLongDescriptionName { get; set; }
        }

        static void Main() 
        {
            var thing = new Thing()
            {
                Name = "Albert",
                UnncessarilyLongDescriptionName = "This is thing1."
            };

            var thing2 = new Thing()
            {
                Name = "Ben",
                UnncessarilyLongDescriptionName = "This is thing2."
            };

            var thing3 = new Thing()
            {
                Name = "Calamity",
                UnncessarilyLongDescriptionName = "This is thing3."
            };

            //Initialize DB
            EzDb ezDb = new EzDb("Test.db", OperationModes.EXPLICIT_TAGGING);

            //Test table conditional creation
            ezDb.VerifyType<Thing>();

            //Test Insert
            var listOfThings = new List<Thing>();
            listOfThings.Add(thing);
            listOfThings.Add(thing2);
            ezDb.Insert(listOfThings);
            ezDb.Insert(thing3);

            //Test Select
            Thing selectedThing = ezDb.SelectSingle<Thing>(thing3.Id);
            var allThings = ezDb.SelectAll<Thing>();

            //Test Update
            thing.UnncessarilyLongDescriptionName = "this is an updated thing";
            ezDb.Update(thing);

            //Test Delete
            ezDb.Delete(thing);

            //Test RawNonQuery
            ezDb.ExecuteRawNonQuery("UPDATE Things SET Description = 'Ben5' WHERE Name = 'Ben'");

            //Test RawQuery
            IEnumerable<Thing> things = ezDb.ExecuteRawQuery<Thing>("SELECT * FROM Things");
        }
    }
```