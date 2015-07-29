using System;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RePopCraftingStudio.Db;

namespace RepopCraftingStudioUnitTests
{
    [TestClass]
    public class RepopDBTest
    {
        private static string DB_REPOPDATA_PATH = "D:\\Program Files (x86)\\Steam\\steamapps\\common\\The Repopulation\\repopdata.db3";
        [TestMethod]
        public void TestOpenDB()
        {

            string connectionString = string.Format("Data Source=\"{0}\"", DB_REPOPDATA_PATH);
            RepopDb db = new RepopDb(connectionString);
            db.bootstrapDB();
            Item item = db.GetItemById(165);
            Assert.AreEqual(165, item.Id);
            Assert.AreEqual("Perfect Sarnium Diamond", item.Name);
            Assert.AreEqual("An extremely rare and precious stone.", item.Description);
            Assert.AreEqual(144, item.IconId);

        }
    }
}
