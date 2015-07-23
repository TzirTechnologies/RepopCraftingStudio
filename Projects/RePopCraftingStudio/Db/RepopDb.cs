using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;

namespace RePopCraftingStudio.Db
{
   // Jim's request for credit:
   // I want to be referenced as "The dude that was once too stoned to make Mac & CHeese"
   // Misugumi, Towan Navarr, Treyonna Sullivan

    public class RepopDb
    {
        Dictionary<long, Skill> skillDict = new Dictionary<long, Skill>();
        Dictionary<long, Recipe> recipeDict = new Dictionary<long, Recipe>();
        Dictionary<EntityTypes, Dictionary<long, IList<RecipeResult>>> recipeResultByResultIdTypeIdDict = new Dictionary<EntityTypes, Dictionary<long, IList<RecipeResult>>>();
        Dictionary<long, IList<ItemCraftingComponent>> itemCraftingComponentDict = new Dictionary<long, IList<ItemCraftingComponent>>();
        Dictionary<long, IList<ItemCraftingComponent>> itemCraftingComponentReverseDict = new Dictionary<long, IList<ItemCraftingComponent>>();

        private Control _parent;
        public string ConnectionString { get; set; }

        // C-tors
        public RepopDb(Control parent)
            : this(parent, string.Empty)
        {
        }

        public RepopDb(Control parent, string connectionString)
        {
            _parent = parent;
            ConnectionString = connectionString;
        }

        // Agent Slot Info
        public IEnumerable<AgentSlotInfo> GetAgentSlotInfosForRecipeResult(RecipeResult recipeResult)
        {
            IList<AgentSlotInfo> slotInfos = new List<AgentSlotInfo>();
            Recipe recipe = recipeDict[recipeResult.RecipeId];
            foreach (RecipeAgent recipeAgent in recipe.recipeAgentList)
            {
                IList<Item> items = new List<Item>();

                IEnumerable<ItemCraftingComponent> itemList = itemCraftingComponentReverseDict[recipeAgent.ComponentId];
                foreach (ItemCraftingComponent itemCraftingComponent in itemList)
                {
                    items.Add(GetItemById(itemCraftingComponent.ItemId));
                }

                AgentSlotInfo slot = new AgentSlotInfo
                {
                    Component = SelectCraftingComponentById(recipeAgent.ComponentId),
                    Items = items,
                };
                slotInfos.Add(slot);

            }

            return slotInfos;
        }

        // "crafting_components" table access
        public CraftingComponent SelectCraftingComponentById(long componentId)
        {
            return new CraftingComponent(this, GetDataRow(@"select * from crafting_components where componentId = {0}", componentId).ItemArray);
        }

        // "Fittings" table access
        public IEnumerable<Fitting> SelectFittingsByName(string filter)
        {
            return RowsToEntities(
               GetDataRows(@"select * from fittings where displayName like '%{0}%'", filter),
               r => new Fitting(this, r.ItemArray));
        }

        public IEnumerable<Entity> SelectFittingEntitiessByName(string filter)
        {
            return SelectFittingsByName(filter).OfType<Entity>();
        }

        // Ingredient Slot Info 
        public IEnumerable<IngredientSlotInfo> GetIngredientSlotsInfoForRecipeResult(RecipeResult recipeResult)
        {
            IList<IngredientSlotInfo> slotInfos = new List<IngredientSlotInfo>();
            for (int ingSlot = 1; ingSlot < 5; ingSlot++)
            {
                IngredientSlotInfo slotInfo = GetIngredientSlotInfoForRecipeResultAndIngSlot(recipeResult, ingSlot);
                if (null != slotInfo)
                    slotInfos.Add(slotInfo);
            }

            return slotInfos;
        }

        public IngredientSlotInfo GetIngredientSlotInfoForRecipeResultAndIngSlot(RecipeResult recipeResult, int ingSlot)
        {
            IList<Item> items = new List<Item>();
            var rows = GetDataRows(@"select * from item_crafting_filters
	         inner join item_crafting_components on (item_crafting_components.itemid = item_crafting_filters.itemid)
	         where item_crafting_components.componentid in
		         (select componentid from recipe_ingredients where recipeid={0} and ingslot={1})
	         and item_crafting_filters.filterid = {2}",
               recipeResult.RecipeId, ingSlot, recipeResult.GetFilterId(ingSlot));
            if (0 == rows.Count)
            {
                rows = GetDataRows(@"select * from item_crafting_components where componentid in
		            (select componentid from recipe_ingredients where recipeid={0} and ingslot={1})",
                   recipeResult.RecipeId, ingSlot);
            }
            if (0 == rows.Count)
                return null;

            foreach (DataRow row in rows)
            {
                items.Add(GetItemById((long)row[@"itemId"]));
            }
            return new IngredientSlotInfo
            {
                IngSlot = ingSlot,
                Items = items,
                Component = SelectCraftingComponentById((long)rows[0][@"componentId"]),
            };
        }

        // "Items" table access
        public string GetItemName(long itemId)
        {
            if (0 == itemId)
                return string.Empty;
            try
            {
                return (string)GetDataRow(@"select displayName from items where itemId = {0}", itemId).ItemArray[0];
            }
            catch (Exception ex)
            {
                return string.Empty;
            }
        }

        public Item GetItemById(long itemId)
        {
            if (0 == itemId)
                return null;
            return new Item(this, GetDataRow(@"select * from items where itemId = {0}", itemId).ItemArray);
        }

        public IEnumerable<Item> SelectItemsByName(string filter)
        {
            return RowsToEntities(
               GetDataRows(@"select * from items where displayName like '%{0}%'", filter),
               r => new Item(this, r.ItemArray));
        }

        public IEnumerable<Entity> SelectItemEntitiesByName(string filter)
        {
            return SelectItemsByName(filter).OfType<Entity>();
        }

        // "recipes" table access
        public IEnumerable<Recipe> SelectRecipesByResultIdandType(long id, EntityTypes type)
        {
            List<Recipe> recipes = new List<Recipe>();
            if (type == EntityTypes.Recipe)
            {
                recipes.Add(recipeDict[id]);
                return recipes;
            }
            else
            {
                //Dictionary<EntityTypes, Dictionary<long, IList<RecipeResult>>> recipeResultByResultIdTypeIdDict = new Dictionary<EntityTypes, Dictionary<long, IList<RecipeResult>>>();
                Dictionary<long, IList<RecipeResult>> recipeResultByResultIdDict = null;
                if (recipeResultByResultIdTypeIdDict.TryGetValue(type, out recipeResultByResultIdDict) == true)
                {
                    IList<RecipeResult> recipeResultList = null;
                    if (recipeResultByResultIdDict.TryGetValue(id, out recipeResultList) == true)
                    {
                        foreach (RecipeResult recipeResult in recipeResultList)
                        {
                            //if (recipeResult.GroupId == 1)
                            //{
                                recipes.Add(recipeDict[recipeResult.RecipeId]);
                            //}
                        }
                    }
                }
                    

                return recipes;
            }
        }

        // "recipe_results" table access
        public IEnumerable<RecipeResult> SelectRecipeResultsForRecipeAndResult(long recipeId, long resultId)
        {
            Recipe recipe = recipeDict[recipeId];
            if (resultId != 0)
            {
                IList<RecipeResult> results = new List<RecipeResult>();
                foreach (RecipeResult result in recipe.recipeResultList)
                {
                    if (result.ResultId == resultId)
                    {
                        results.Add(result);
                    }
                }
                return results;
            }
            return recipe.recipeResultList;
        }

        // "Skills" table access
        public string GetSkillName(long skillId)
        {
            if (0 == skillId)
                return string.Empty;
            Skill skill = skillDict[skillId];
            return skill.DisplayName;
        }

        // "Structures" table access
        public IEnumerable<Blueprint> SelectBlueprintsByName(string filter)
        {
            return RowsToEntities(
               GetDataRows(@"select * from structures where displayName like '%{0}%'", filter),
               r => new Blueprint(this, r.ItemArray));
        }

        public IEnumerable<Entity> SelectBlueprintEntitiesByName(string filter)
        {
            return SelectBlueprintsByName(filter).OfType<Entity>();
        }


        // "Recipe" table access
        public IEnumerable<Recipe> SelectRecipesByName(string filter)
        {
            IList<Recipe> list = new List<Recipe>();
            foreach (Recipe recipe in recipeDict.Values)
            {
                if (stringLike(recipe.Name, filter) == true) {
                    list.Add(recipe);
                }
            }
            return list;
        }

        public IEnumerable<Entity> SelectRecipeEntitiesByName(string filter)
        {
            return SelectRecipesByName(filter).OfType<Entity>();
        }

        private IEnumerable<T> RowsToEntities<T>(DataRowCollection rows, Func<DataRow, T> make) where T : Entity
        {
            IList<T> list = new List<T>();

            foreach (DataRow row in rows)
            {
                list.Add(make(row));
            }
            return list;
        }

        public DataRow GetDataRow(string format, params object[] args)
        {
            return GetDataRows(string.Format(format, args))[0];
        }

        public DataRowCollection GetDataRows(string format, params object[] args)
        {
            return GetDataTable(string.Format(format, args)).Rows;
        }

        private DataTable GetDataTable(string format, params object[] args)
        {
            string sql = string.Empty;

            try
            {
                try
                {
                    _parent.Cursor = Cursors.WaitCursor;

                    sql = string.Format(format, args);
                    Debug.WriteLine(sql);
                    if (string.IsNullOrEmpty(ConnectionString))
                        throw new InvalidOperationException(@"Connection string is null or empty.");

                    DataTable table = new DataTable();

                    using (SQLiteConnection connection = new SQLiteConnection(ConnectionString))
                    {
                        connection.Open();
                        using (SQLiteCommand command = new SQLiteCommand(connection))
                        {
                            command.CommandText = sql;
                            SQLiteDataReader reader = command.ExecuteReader();
                            table.Load(reader);
                            return table;
                        }
                    }
                }
                finally
                {
                    _parent.Cursor = Cursors.Default;
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(string.Format("Error executing SQL statement.\nSQL:\n\n{0}", sql), ex);
            }
        }

        private void LogInfo(string format, params object[] args)
        {
            Debug.WriteLine(string.Format(format, args));
        }

        public void bootstrapDB()
        {
            bootstrapSkills();
            bootstrapRecipes();
            bootstrapRecipeResults();
            bootstrapRecipeAgents();
            bootstrapRecipeIngredient();
            bootstrapRecipeSkillRange();
            bootstrapItemCraftingComponent();
        }

        private void bootstrapSkills()
        {
            IEnumerable<Skill> skillList = RowsToEntities(
                   GetDataRows(@"select * from skills"),
                   r => new Skill(this, r.ItemArray));
            foreach (Skill skill in skillList)
            {
                skillDict.Add(skill.SkillId, skill);
            }
        }

        private void bootstrapRecipes()
        {
            IEnumerable<Recipe> recipeList = RowsToEntities(
                   GetDataRows(@"select * from recipes"),
                   r => new Recipe(this, r.ItemArray));
            foreach (Recipe recipe in recipeList)
            {
                recipeDict.Add(recipe.RecipeId, recipe);
            }
        }

        private void bootstrapRecipeResults()
        {
            IEnumerable<RecipeResult> recipeResultList = RowsToEntities(
                   GetDataRows(@"select * from recipe_results"),
                   r => new RecipeResult(this, r.ItemArray));
            foreach (RecipeResult recipeResult in recipeResultList)
            {
                Recipe recipe = recipeDict[recipeResult.RecipeId];
                recipe.recipeResultList.Add(recipeResult);

                Dictionary<long, IList<RecipeResult>> recipeResultTypeIdDict = null;
                if (recipeResultByResultIdTypeIdDict.TryGetValue(recipeResult.Type, out recipeResultTypeIdDict) == false)
                {
                    recipeResultTypeIdDict = new Dictionary<long, IList<RecipeResult>>();
                    recipeResultByResultIdTypeIdDict[recipeResult.Type] = recipeResultTypeIdDict;
                }

                IList<RecipeResult> recipeResultGroupList = null;
                if (recipeResultTypeIdDict.TryGetValue(recipeResult.ResultId, out recipeResultGroupList) == false)
                {
                    recipeResultGroupList = new List<RecipeResult>();
                    recipeResultTypeIdDict[recipeResult.ResultId] = recipeResultGroupList;
                }

                recipeResultGroupList.Add(recipeResult);
            }
        }

        private void bootstrapRecipeAgents()
        {
            IEnumerable<RecipeAgent> recipeAgentList = RowsToEntities(
                   GetDataRows(@"select * from recipe_agents"),
                   r => new RecipeAgent(this, r.ItemArray));
            foreach (RecipeAgent recipeAgent in recipeAgentList)
            {
                Recipe recipe = recipeDict[recipeAgent.RecipeId];
                recipe.recipeAgentList.Add(recipeAgent);
                //recipeResultDict[recipeResult.Type][recipeResult.ResultId][recipeResult.GroupId].Add(recipeResult);
            }
        }

        private void bootstrapRecipeIngredient()
        {
            IEnumerable<RecipeIngredient> recipeIngredientList = RowsToEntities(
                   GetDataRows(@"select * from recipe_ingredients"),
                   r => new RecipeIngredient(this, r.ItemArray));
            foreach (RecipeIngredient recipeIngredient in recipeIngredientList)
            {
                Recipe recipe = null;
                if (recipeDict.TryGetValue(recipeIngredient.RecipeId, out recipe) == true)
                {
                    recipe.recipeIngredientList.Add(recipeIngredient);
                    //recipeResultDict[recipeResult.Type][recipeResult.ResultId][recipeResult.GroupId].Add(recipeResult);
                }
            }
        }

        private void bootstrapRecipeSkillRange()
        {
            IEnumerable<RecipeSkillRange> recipeSkillRangeList = RowsToEntities(
                   GetDataRows(@"select * from recipe_skill_range"),
                   r => new RecipeSkillRange(this, r.ItemArray));
            foreach (RecipeSkillRange recipeSkillRange in recipeSkillRangeList)
            {
                Recipe recipe = null;
                if (recipeDict.TryGetValue(recipeSkillRange.RecipeId, out recipe) == true)
                {
                    recipe.recipeSkillRangeList.Add(recipeSkillRange);
                    Debug.WriteLine(GetSkillName(recipe.SkillId) + "," + recipe.Name + "," + recipeSkillRange.Level + "," + recipeSkillRange.MinF + "," + recipeSkillRange.MinD + "," + recipeSkillRange.MinC + "," + recipeSkillRange.MinB + "," + recipeSkillRange.MinA + "," + recipeSkillRange.MinAA + "," + recipeSkillRange.Over1 + "," + recipeSkillRange.Over2);
                }
            }
        }

        private void bootstrapItemCraftingComponent()
        {
            IEnumerable<ItemCraftingComponent> itemCraftingComponentList = RowsToEntities(
                   GetDataRows(@"select * from item_crafting_components"),
                   r => new ItemCraftingComponent(this, r.ItemArray));
            foreach (ItemCraftingComponent itemCraftingComponent in itemCraftingComponentList)
            {
                IList<ItemCraftingComponent> componentIdList = null;
                if (itemCraftingComponentDict.TryGetValue(itemCraftingComponent.ItemId, out componentIdList) == false)
                {
                    componentIdList = new List<ItemCraftingComponent>();
                    itemCraftingComponentDict[itemCraftingComponent.ItemId] = componentIdList;
                }
                componentIdList.Add(itemCraftingComponent);

                IList<ItemCraftingComponent> itemIdList = null;
                if (itemCraftingComponentReverseDict.TryGetValue(itemCraftingComponent.ComponentId, out itemIdList) == false)
                {
                    itemIdList = new List<ItemCraftingComponent>();
                    itemCraftingComponentReverseDict[itemCraftingComponent.ComponentId] = itemIdList;
                }
                itemIdList.Add(itemCraftingComponent);
                //recipeResultDict[recipeResult.Type][recipeResult.ResultId][recipeResult.GroupId].Add(recipeResult);
            }
        }

        private bool stringLike(string toSearch, string toFind)
        {
            return toSearch.ToLower().Contains(toFind.ToLower());
            //return new Regex(@"\A" + new Regex(@"\.|\$|\^|\{|\[|\(|\||\)|\*|\+|\?|\\").Replace(toFind, ch => @"\" + ch).Replace('_', '.').Replace("%", ".*") + @"\z", RegexOptions.Singleline).IsMatch(toSearch);
        }
    }
}