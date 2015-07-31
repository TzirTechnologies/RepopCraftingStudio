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

        [TestMethod]
        public void TestGetIngredientSlotInfoForRecipeResultAndIngSlot()
        {

            string connectionString = string.Format("Data Source=\"{0}\"", DB_REPOPDATA_PATH);
            RepopDb db = new RepopDb(connectionString);
            db.bootstrapDB();

            Recipe recipe = db.GetRecipeById(136);
            Assert.AreEqual(859, recipe.recipeResultList[0].ResultId);

            IngredientSlotInfo isi = db.GetIngredientSlotInfoForRecipeResultAndIngSlot(recipe.recipeResultList[0], 1);

            Assert.AreEqual("Battery Cell", isi.DisplayName);

            Assert.AreEqual("Cadmium Battery Cell", isi.Items[0].Name);
            Assert.AreEqual("LiCd Battery Cell", isi.Items[1].Name);
            Assert.AreEqual("LiPmCd Battery Cell", isi.Items[2].Name);
            Assert.AreEqual("Lithium Battery Cell", isi.Items[3].Name);
            Assert.AreEqual("LiVCd Battery Cell", isi.Items[4].Name);
            Assert.AreEqual("NiCd Battery Cell", isi.Items[5].Name);
            Assert.AreEqual("NiCeCd Battery Cell", isi.Items[6].Name);
            Assert.AreEqual("Standard Battery Cell", isi.Items[7].Name);
        }
        
    }
}
