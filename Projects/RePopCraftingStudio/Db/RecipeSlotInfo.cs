using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

namespace RePopCraftingStudio.Db
{
   public abstract class RecipeSlotInfo
   {
      private Item _specificItem;

      public List<Item> Items { get; set; }
      public CraftingComponent Component { get; set; }

      public Item SpecificItem
      {
         get { return _specificItem ?? Items.First(); }
         set { _specificItem = value; }
      }

      public bool IsSpecific { get { return ( null != _specificItem ) || ( 1 == Items.Count() ); } }

      public string DisplayName { get { return IsSpecific ? SpecificItem.Name : Component.Name; } }

      public Entity Entity { get { return IsSpecific ? (Entity)SpecificItem : Component; } }
   }

   public class AgentSlotInfo : RecipeSlotInfo
   {
   }

   public class IngredientSlotInfo : RecipeSlotInfo, IComparable<IngredientSlotInfo>
   {
      public int IngSlot { get; set; }
      public int CompareTo(IngredientSlotInfo a)
      {
          if (this == a)
          {
              return 0;
          }

          if (!IsSpecific && a.IsSpecific)
          {
              return 1;
          }


          if (IsSpecific && a.IsSpecific)
          {
              int ret = Items.First().Name.CompareTo(a.Items.First().Name);
              if (ret != 0)
              {
                  return ret;
              }
          }

          if (IsSpecific && !a.IsSpecific)
          {
              return -1;
          }

          return 0;
      }
   }

   public static class IngredientSlotInfoExtensions
   {
      public static string GetSpecificItemNames( this IEnumerable<IngredientSlotInfo> infos )
      {
         string names = string.Empty;
         foreach ( IngredientSlotInfo info in infos )
         {
            if ( info.IsSpecific )
               names += info.Items.First().Name + @", ";
         }
         if ( string.IsNullOrEmpty( names ) )
            names = @"Nothing specific";

         return names;
      }
   }
}