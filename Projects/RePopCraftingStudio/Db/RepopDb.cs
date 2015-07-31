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

        Dictionary<long, Item> itemDict = new Dictionary<long, Item>();
        Dictionary<long, IList<ItemCraftingComponent>> itemCraftingComponentReverseDict = new Dictionary<long, IList<ItemCraftingComponent>>();
        Dictionary<long, IList<ItemCraftingFilter>> itemCraftingFilterReverseDict = new Dictionary<long, IList<ItemCraftingFilter>>();

        public string ConnectionString { get; set; }

        // C-tors
        public RepopDb()
            : this(string.Empty)
        {
        }

        public RepopDb(string connectionString)
        {
            ConnectionString = connectionString;
        }

        // Agent Slot Info
        public IEnumerable<AgentSlotInfo> GetAgentSlotInfosForRecipeResult(RecipeResult recipeResult)
        {
            IList<AgentSlotInfo> slotInfos = new List<AgentSlotInfo>();
            Recipe recipe = recipeDict[recipeResult.RecipeId];
            foreach (RecipeAgent recipeAgent in recipe.recipeAgentList)
            {
                List<Item> items = new List<Item>();

                IEnumerable<ItemCraftingComponent> itemList = itemCraftingComponentReverseDict[recipeAgent.ComponentId];
                foreach (ItemCraftingComponent itemCraftingComponent in itemList)
                {
                    items.Add(GetItemById(itemCraftingComponent.ItemId));
                }

                items.Sort();

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
            List<IngredientSlotInfo> slotInfos = new List<IngredientSlotInfo>();
            for (int ingSlot = 1; ingSlot < 5; ingSlot++)
            {
                IngredientSlotInfo slotInfo = GetIngredientSlotInfoForRecipeResultAndIngSlot(recipeResult, ingSlot);
                if (null != slotInfo)
                    slotInfos.Add(slotInfo);
            }

            slotInfos.Sort();

            return slotInfos;
        }

        public IngredientSlotInfo GetIngredientSlotInfoForRecipeResultAndIngSlot2(RecipeResult recipeResult, int ingSlot)
        {
            List<Item> items = new List<Item>();
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
        public IngredientSlotInfo GetIngredientSlotInfoForRecipeResultAndIngSlot(RecipeResult recipeResult, int ingSlot)
        {
            List<Item> baseItems = new List<Item>();
            long firstComponentId = 0;

            IEnumerable<long> componentIds = GetComponentIdsForRecipeResultAndIngSlot(recipeResult, ingSlot);
            foreach (long componentId in componentIds)
            {
                IEnumerable<Item> componentItems = GetItemsForComponent(componentId);
                foreach (Item item in componentItems)
                {
                    if (baseItems.Contains(item) == false)
                    {
                        baseItems.Add(item);
                        if (firstComponentId == 0)
                        {
                            firstComponentId = componentId;
                        }
                    }
                }
            }

            long filterId = recipeResult.GetFilterId(ingSlot);

            List<Item> filterItems = new List<Item>();
            foreach (Item item in baseItems)
            {
                if (CheckItemAgainstFilter(item, filterId) == true)
                {
                    filterItems.Add(item);
                }
            }

            List<Item> items = null;

            if (filterItems.Count > 0)
            {
                items = filterItems;
            }
            else if (baseItems.Count > 0)
            {
                items = baseItems;
            } else 
            {
                return null;
            }

            items.Sort();

            return new IngredientSlotInfo
            {
                IngSlot = ingSlot,
                Items = items,
                Component = SelectCraftingComponentById(firstComponentId),
            };
        }

        public IEnumerable<long> GetComponentIdsForRecipeResultAndIngSlot(RecipeResult recipeResult, int ingSlot)
        {
            List<long> componentIds = new List<long>();
            
            Recipe recipe = recipeDict[recipeResult.RecipeId];
            foreach (RecipeIngredient recipeIngredient in recipe.recipeIngredientList)
            {
                if (recipeIngredient.IngSlot == ingSlot)
                {
                    if (componentIds.Contains(recipeIngredient.ComponentId) == false)
                    {
                        componentIds.Add(recipeIngredient.ComponentId);
                    }
                }
            }
            return componentIds;
        }

        public bool CheckItemAgainstFilter(Item item, long filterId)
        {
            foreach (ItemCraftingFilter itemCraftingFilter in item.itemCraftingFilterList)
            {
                if (itemCraftingFilter.FilterId == filterId)
                {
                    return true;
                }
            }

            return false;
        }

        public IEnumerable<Item> GetItemsForComponent(long componentId)
        {
            List<Item> items = new List<Item>();

            IEnumerable<ItemCraftingComponent> itemCraftingComponents = itemCraftingComponentReverseDict[componentId];

            foreach (ItemCraftingComponent itemCraftingComponent in itemCraftingComponents)
            {
                Item item = itemDict[itemCraftingComponent.ItemId];
                if (items.Contains(item) == false)
                {
                    items.Add(item);
                }
            }

            return items;
        }

        // "Items" table access
        public string GetItemName(long itemId)
        {
            if (0 == itemId)
                return string.Empty;
            Item item = GetItemById(itemId);
            if (item == null)
            {
                return string.Empty;
            }
            return item.Name;
        }

        public Item GetItemById(long itemId)
        {
            if (0 == itemId)
                return null;

            Item item = null;
            itemDict.TryGetValue(itemId, out item);
            return item;
        }

        public IEnumerable<Item> SelectItemsByName(string filter)
        {
            IList<Item> list = new List<Item>();
            foreach (Item item in itemDict.Values)
            {
                if (stringLike(item.Name, filter) == true)
                {
                    list.Add(item);
                }
            }
            return list;
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
                List<RecipeResult> results = new List<RecipeResult>();
                foreach (RecipeResult result in recipe.recipeResultList)
                {
                    if (result.ResultId == resultId)
                    {
                        results.Add(result);
                    }
                }

                results.Sort();

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
        public Recipe GetRecipeById(long recipeId)
        {
            return recipeDict[recipeId];
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
            bootstrapItems();
            bootstrapItemCraftingComponent();
            bootstrapItemCraftingFilter();
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
                    //Debug.WriteLine(GetSkillName(recipe.SkillId) + "," + recipe.Name + "," + recipeSkillRange.Level + "," + recipeSkillRange.MinF + "," + recipeSkillRange.MinD + "," + recipeSkillRange.MinC + "," + recipeSkillRange.MinB + "," + recipeSkillRange.MinA + "," + recipeSkillRange.MinAA + "," + recipeSkillRange.Over1 + "," + recipeSkillRange.Over2);
                }
            }
        }

        private void bootstrapItems()
        {
            IEnumerable<Item> itemList = RowsToEntities(
                   GetDataRows(@"select * from items"),
                   r => new Item(this, r.ItemArray));
            foreach (Item item in itemList)
            {
                itemDict.Add(item.Id, item);
            }
        }

        private void bootstrapItemCraftingComponent()
        {
            IEnumerable<ItemCraftingComponent> itemCraftingComponentList = RowsToEntities(
                   GetDataRows(@"select * from item_crafting_components"),
                   r => new ItemCraftingComponent(this, r.ItemArray));
            foreach (ItemCraftingComponent itemCraftingComponent in itemCraftingComponentList)
            {
                Item item = itemDict[itemCraftingComponent.ItemId];
                item.itemCraftingComponentList.Add(itemCraftingComponent);

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
        private void bootstrapItemCraftingFilter()
        {
            IEnumerable<ItemCraftingFilter> itemCraftingFilterList = RowsToEntities(
                   GetDataRows(@"select * from item_crafting_filters"),
                   r => new ItemCraftingFilter(this, r.ItemArray));
            foreach (ItemCraftingFilter itemCraftingFilter in itemCraftingFilterList)
            {
                Item item = itemDict[itemCraftingFilter.ItemId];
                item.itemCraftingFilterList.Add(itemCraftingFilter);

                IList<ItemCraftingFilter> itemIdList = null;
                if (itemCraftingFilterReverseDict.TryGetValue(itemCraftingFilter.FilterId, out itemIdList) == false)
                {
                    itemIdList = new List<ItemCraftingFilter>();
                    itemCraftingFilterReverseDict[itemCraftingFilter.FilterId] = itemIdList;
                }
                itemIdList.Add(itemCraftingFilter);
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