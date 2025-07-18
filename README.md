# Truinto Potion Craft
Mod for Potion Craft Alchemist Simulator

Index
-----------
* [Disclaimers](#disclaimers)
* [Installation](#installation)
* [Contact](#contact)
* [Content](#content)
* [DefaultPotion](#defaultpotion)
* [RearrangeBookmarks](#rearrangebookmarks)
* [ShareRecipe](#sharerecipe)
* [StackableGarden](#stackablegarden)
* [MagicGarden](#magicgarden)
* [Build](#build)

Disclaimers
-----------
* I do not take any responsibility for broken saves or any other damage. Use this software at your own risk.
* BE AWARE that all mods are WIP and may fail.

Installation
-----------
* You will need [BepInEx](https://github.com/BepInEx/BepInEx/releases).
* Follow the installation procedure.
* Download a release [https://github.com/Truinto/Truinto-PotionCraft/releases](https://github.com/Truinto/Truinto-PotionCraft/releases).
* Extract dll into BepInEx/plugins folder.

Contact
-----------
Start discussion on Github.

Content
-----------
* DefaultPotion: Potion bases all have their own default bottle/sticker setting. These are saved globally and will carry over to any save file.
* RearrangeBookmarks: Press space-bar to align all recipes. Some configuration possible. Works with BookmarkOrganizer.
* ShareRecipe: Adds hotkeys (CTRL+C; CTRL+V) to export/import recipes and (CTRL+D; CTRL+F) to export/import the whole recipe book. Uses clipboard. Works with BookmarkOrganizer.
* StackableGarden: Removes collision from garden plants/crystals.
* MagicGarden: Automatically harvest, water, and fertilize your garden. No lag.

DefaultPotion
-----------
Whenever you set a default for bottle, sticker, or sticker-angle, it will only apply to the potion base of the current map (even when used from the recipe book). When you open the config JSON manually, you may also set a default for lowlander potions (single ingredient). Lowlander can overwrite one or all properties. I usually set a custom sticker for lowlander potions.
![Example DefaultPotion](Resources/screenshot-bottle1.jpg)

RearrangeBookmarks
-----------
When you press space-bar while the recipe book is open, it will rearrange all the bookmarks. Unlike the mod 'SortBookmarks' it will not change the order of recipes. Configuration is saved in the BepInEx config folder. Each rail can be set individually. If a rail is not listed, it will not be modified at all. Alignment can be left, right, or evenly spaced. Offset is useful to indent a row. While Limit can be used to override the normal boundaries. One unit is equal to the width of one bookmark. Empty bookmarks are stacked on top of each other, that behavior can be turned off.
![Example RearrangeBookmarks](Resources/screenshot-recipebook1.jpg)

ShareRecipe
-----------
CTRL+C serializes the currently shown bookmark into your clipboard. CTRL+V deserializes the same data, if the current page is blank. Otherwise no action is done. CTRL+D serializes the whole recipe book into your clipboard. CTRL+F deserializes the same data. Doing so will destroy the recipe book. It also copies the count of empty bookmarks. This can be used for cheating. The data is compressed and turned into a base64 string. Use the hotkeys with shift to get raw output.
![Example RearrangeBookmarks](Resources/screenshot-share.jpg)

StackableGarden
-----------
Removes collision from garden plants/crystals.
![Example RearrangeBookmarks](Resources/screenshot-garden1.jpg)

MagicGarden
-----------
Harvests, waters, and fertilizes all garden plants and crystals. Heavily optimized to prevent lagging. You get a neat result screen. Trigger manually with CTRL+G or automatically starting day 4.
![Example RearrangeBookmarks](Resources/screenshot-magicgarden1.jpg)

Build
-----------
* Clone repo
* Create a copy of Directory.Build.props.default named Directory.Build.props.user
* Open and edit Directory.Build.props.user with your game location
