# RepopCraftingStudio
Crafting Studio for The Repopulation MMO.

I haven't seen any changes on the original project in awhile.  I've been hacking away at the code exploring, etc.
I've added a recipe explorer to the controls.  I did that because I couldn't find Hide Boots :)

My current plans are heading towards an MDI application where you can have multiple recipes open and save the selections you have made for the various steps.
I would also love to be able to do a wizard for Fittings.  You want a Type A Armor Fitting with these attributes?  This is how you would build it.
Ideally interactive so that you can select attributes and the points you want.. And it can restrict the remaining attributes to possible combinations.

I have also started going towards loading all the data into memory and searching there.  The existing code is pretty slow.  The DB is fairly small so there shouldn't be a good reason to not load it all in memory.

The existing solver is far too tied to the UI.  In order to do some of the wizard thing, save and load that needs to be broken out.
It also builds as it goes which gives you some pretty ugly unsorted lists.


Here are my random notes on what I feel should be in this tool.

Online tools for reference:
http://repopdb.com/#/craft/1/1406
http://aena.at/craftmap/search.html

Should be able to select any recipe (why are hide boots not being found???)

Parts list should be specific to what is selected
Should be another panel to allow for control items needed / subsistences needed like repopdb.

In that panel the derived attributes for the item should be displayed.

i.e. PE (2), PR (2), etc.

Should be able to save a blueprint build set (all settings) with a name.

should list skill, recipe and recipe book in that panel. (extends repopdb)

Should warn when an attribute will be lost. i.e. copper wires which have no attributes using attributed metal.

List the skills needed to build the item above the materials list.
List the hardness of the recipe from the skill range data.

Expand All
Dependency Graph type view of recipes (i.e. each recipe grouped instead of super nested)

Split Materials into two lists.
Items Needed, Subsistences Needed.
Put check box in to mark as have.
Option to have decimal version vs from scratch version.
i.e. You only need .23 acetice acid vs you have to have to a full recipe.

Be able to specify decimal amounts of value for all the subsistences/items needed.
This would allow for auto costing of things. i.e. I pay 5cpu per A0 Contaminated water.

Images like repopdb would be nice, not sure if it's doable.
i.e. how do you pull the images out of the db3's?

