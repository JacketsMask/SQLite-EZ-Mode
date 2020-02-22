using SQLiteEZMode;
using SQLiteEZMode.Attributes;
using System;
using System.Collections.Generic;

namespace SQLite_EZ_Mode_Test
{
    class Program
    {
        [SqliteTableAttribute("Things")]
        public class Thing
        {
            [SqliteCellAttribute(CellDataTypes.INTEGER, isPrimaryId: true)]
            public int Id { get; set; }
            [SqliteCellAttribute(CellDataTypes.TEXT)]
            public string Name { get; set; }
            [SqliteCellAttribute(columnName: "Description", CellDataTypes.TEXT)]
            public string Aaaa { get; set; }
        }

        [SqliteTableAttribute("BetterThings")]
        public class BetterThing
        {
            [SqliteCellAttribute(CellDataTypes.INTEGER, isPrimaryId: true)]
            public int Id { get; set; }
            [SqliteCellAttribute(CellDataTypes.TEXT)]
            public string Name { get; set; }
            [SqliteCellAttribute(columnName: "Description", CellDataTypes.TEXT)]
            public string Aaaa { get; set; }
        }

        public class Tagless
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public List<string> listOfStrings { get; set; }
            public List<int> listofInts { get; set; }
        }

        static void TestTagless()
        {
            //Initialize DB
            EzDb ezDb = new EzDb("Test.db", OperationModes.TAGLESS);

            var tagless = new Tagless()
            {
                Name = "Ihavenotag",
                Description = "A tag have not.",
                listOfStrings = new List<string>() { "one", "two", "three", "four" },
                listofInts = new List<int>() { 1, 2, 3, 4 }
            };

            ezDb.VerifyType<Tagless>();
            ezDb.Insert(tagless);
            var allTagless = ezDb.SelectAll<Tagless>();
        }

        static void TestGeneral()
        {
            var thing = new Thing()
            {
                Name = "Albert",
                Aaaa = "This is thing1."
            };

            var thing2 = new Thing()
            {
                Name = "Ben",
                Aaaa = "This is thing2."
            };

            var thing3 = new Thing()
            {
                Name = "Calamity",
                Aaaa = "This is thing3."
            };

            var betterThing = new BetterThing()
            {
                Name = "BetterAlbert",
                Aaaa = "Better description"
            };

            //Initialize DB
            EzDb ezDb = new EzDb("Test.db", OperationModes.TAGLESS);
            //Test table conditional creation
            ezDb.VerifyType<Thing>();
            ezDb.VerifyType<BetterThing>();

            //Test Insert
            var listOfThings = new List<Thing>();
            listOfThings.Add(thing);
            listOfThings.Add(thing2);
            ezDb.Insert(listOfThings);
            ezDb.Insert(thing3);
            ezDb.Insert(betterThing);

            //Test Select
            Thing selectedThing = ezDb.SelectSingle<Thing>(thing3.Id);
            var allThings = ezDb.SelectAll<Thing>();

            //Test Update
            thing.Aaaa = "this is an updated thing";
            ezDb.Update(thing);

            //Test Delete
            ezDb.Delete(thing);

            //Test RawNonQuery
            ezDb.ExecuteRawNonQuery("UPDATE Things SET Description = 'Ben5' WHERE Name = 'Ben'");

            //Test RawQuery
            IEnumerable<Thing> things = ezDb.ExecuteRawQuery<Thing>("SELECT * FROM Things");
        }

        static void Main(string[] args)
        {
            System.IO.File.Delete("Test.db");

            TestTagless();
            TestGeneral();
        }
    }
}
