﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using RePopCrafting;

namespace RePopCraftingStudio.Db
{
   public class RepopDb
   {
      //private string _connStr = string.Empty;
      public string ConnectionString { get; set; }

      public RepopDb()
         : this( string.Empty )
      { }

      public RepopDb( string connectionString )
      {
         ConnectionString = connectionString;
      }

      public string GetComponentName( EntityWithComponentId entity )
      {
         return (string)GetDataRow( @"select displayName from crafting_components where componentId = {0}", entity.ComponentId ).ItemArray[ 0 ];
      }

      public string GetRecipeName( RecipeResult recipeResult )
      {
         return (string)GetDataRow( @"select displayName from recipes where recipeId = {0}", recipeResult.RecipeId ).ItemArray[ 0 ];
      }

      public string GetCraftingFilterName( long filterId )
      {
         if ( 0 == filterId )
            return string.Empty;
         return (string)GetDataRow( @"select displayName from crafting_filters where filterId = {0}", filterId ).ItemArray[ 0 ];
      }

      public string GetItemName( long itemId )
      {
         if ( 0 == itemId )
            return string.Empty;
         return (string)GetDataRow( @"select displayName from items where itemId = {0}", itemId ).ItemArray[ 0 ];
      }

      public string GetSkillName( long skillId )
      {
         if ( 0 == skillId )
            return string.Empty;
         return (string)GetDataRow( @"select displayName from skills where skillId = {0}", skillId ).ItemArray[ 0 ];
      }

      public IEnumerable<Item> SelectItemsByName( string filter )
      {
         return RowsToEntities(
            GetDataRows( @"select * from items where displayName like '%{0}%'", filter ),
            r => new Item( this, r.ItemArray ) );
      }

      public IEnumerable<Recipe> SelectRecipesForItem( Item item )
      {
         return SelectRecipesForItem( item.ItemId );
      }

      public IEnumerable<Recipe> SelectRecipesForItem( long itemId )
      {
         return RowsToEntities(
            GetDataRows(
               @"select * from recipes where recipeId in (select recipeId from recipe_results where resultId = {0})",
               itemId ),
            r => new Recipe( this, r.ItemArray ) );
      }

      //public bool IsItemCraftComponentUnique( long componentId )
      //{
      //   return 1 == GetDataRows( @"select itemId from item_crafting_components where componentId = {0}", componentId ).Count;
      //}

      public IEnumerable<RecipeAgent> SelectRecipeAgentsForRecipe( Recipe recipe )
      {
         return SelectRecipeAgentsForRecipe( recipe.RecipeId );
      }

      public IEnumerable<RecipeAgent> SelectRecipeAgentsForRecipe( long recipeId )
      {
         return RowsToEntities(
            GetDataRows(
               @"select * from recipe_agents where recipeId = {0}", recipeId ),
            r => new RecipeAgent( this, r.ItemArray ) );
      }

      public IEnumerable<RecipeIngredient> SelectRecipeIngredientsForRecipe( Recipe recipe )
      {
         return SelectRecipeIngredientsForRecipe( recipe.RecipeId );
      }

      public IEnumerable<RecipeIngredient> SelectRecipeIngredientsForRecipe( long recipeId )
      {
         return RowsToEntities(
            GetDataRows(
               @"select * from recipe_ingredients where recipeId = {0}", recipeId ),
            r => new RecipeIngredient( this, r.ItemArray ) );
      }

      public IEnumerable<RecipeResult> SelectRecipeResultsForRecipe( Recipe recipe )
      {
         return SelectRecipeResultsForRecipe( recipe.RecipeId );
      }

      public IEnumerable<RecipeResult> SelectRecipeResultsForRecipe( long recipeId )
      {
         return RowsToEntities(
            GetDataRows(
               @"select * from recipe_results where recipeId = {0}", recipeId ),
            r => new RecipeResult( this, r.ItemArray ) );
      }


      // =============================================================================================

      private IEnumerable<T> RowsToEntities<T>( DataRowCollection rows, Func<DataRow, T> make ) where T : Entity
      {
         IList<T> list = new List<T>();

         foreach ( DataRow row in rows )
         {

            list.Add( make( row ) );
         }
         return list;
      }

      private DataRow GetDataRow( string format, params object[] args )
      {
         return GetDataRows( string.Format( format, args ) )[ 0 ];
      }

      private DataRowCollection GetDataRows( string format, params object[] args )
      {
         return GetDataTable( string.Format( format, args ) ).Rows;
      }

      private DataTable GetDataTable( string format, params object[] args )
      {
         if ( string.IsNullOrEmpty( ConnectionString ) )
            throw new InvalidOperationException( @"Connection string is null or empty." );

         DataTable table = new DataTable();

         using ( SQLiteConnection connection = new SQLiteConnection( ConnectionString ) )
         {
            connection.Open();
            using ( SQLiteCommand command = new SQLiteCommand( connection ) )
            {
               command.CommandText = string.Format( format, args );
               SQLiteDataReader reader = command.ExecuteReader();
               table.Load( reader );
            }
         }

         return table;
      }

      public void BuildManifest( long itemId )
      {
         RecipeResult recipeResult = new RecipeResult( this, GetDataRow( @"select * from recipe_results where resultId = {0} and groupId = 1", itemId ).ItemArray );


         var ingredients = SelectRecipeIngredientsForRecipe( recipeResult.RecipeId );
         foreach ( RecipeIngredient ingredient in ingredients )
         {
            //if ( ingredient.Unique )
            //{
            //   Debug.WriteLine( @"unique." );
            //}
            //else
            //{
            //   Debug.WriteLine( @"not unique." );
            //}
         }


         var agents = SelectRecipeAgentsForRecipe( recipeResult.RecipeId );




      }
   }

   //public class ManifestItem
}