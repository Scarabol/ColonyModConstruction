using System.Collections.Generic;
using Pipliz;
using Pipliz.Chatting;
using Pipliz.JSON;
using Pipliz.Threading;
using BlockTypes.Builtin;

namespace ScarabolMods
{
  [ModLoader.ModManager]
  public static class ScaffoldsModEntries
  {
    public static string SCAFFOLD_ITEM_TYPE = ConstructionModEntries.MOD_PREFIX + "scaffold";
    public static int MAX_PREVIEW_BLOCKS_THRESHOLD = 1000;

    [ModLoader.ModCallback (ModLoader.EModCallbackType.AfterAddingBaseTypes, "scarabol.scaffolds.addrawtypes")]
    public static void AfterAddingBaseTypes (Dictionary<string, ItemTypesServer.ItemTypeRaw> itemTypes)
    {
      itemTypes.Add (SCAFFOLD_ITEM_TYPE, new ItemTypesServer.ItemTypeRaw (SCAFFOLD_ITEM_TYPE, new JSONNode ()
        .SetAs ("onRemove", new JSONNode (NodeType.Array))
        .SetAs ("isSolid", false)
        .SetAs ("mesh", MultiPath.Combine (ConstructionModEntries.AssetsDirectory, "meshes", "scaffold.obj"))
        .SetAs ("sideall", "planks")
        .SetAs ("destructionTime", 100)
      ));
    }

    [ModLoader.ModCallback (ModLoader.EModCallbackType.AfterItemTypesDefined, "scarabol.scaffolds.registertypes")]
    public static void AfterItemTypesDefined ()
    {
      foreach (string blueprintTypename in ManagerBlueprints.Blueprints.Keys) {
        ItemTypesServer.RegisterOnAdd (blueprintTypename, ScaffoldBlockCode.AddScaffolds);
        ItemTypesServer.RegisterOnAdd (blueprintTypename + CapsulesModEntries.CAPSULE_SUFFIX, ScaffoldBlockCode.AddScaffolds);
        ItemTypesServer.RegisterOnRemove (blueprintTypename, ScaffoldBlockCode.RemoveScaffolds);
        ItemTypesServer.RegisterOnRemove (blueprintTypename + CapsulesModEntries.CAPSULE_SUFFIX, ScaffoldBlockCode.RemoveScaffolds);
      }
    }
  }

  public static class ScaffoldBlockCode
  {
    public static void AddScaffolds (Vector3Int position, ushort newtype, Players.Player causedBy)
    {
      string itemtypeFullname = ItemTypes.IndexLookup.GetName (newtype);
      string blueprintBasename = itemtypeFullname.Substring (0, itemtypeFullname.Length - 2);
      ushort bluetype = newtype;
      if (blueprintBasename.EndsWith (CapsulesModEntries.CAPSULE_SUFFIX)) {
        blueprintBasename = blueprintBasename.Substring (0, blueprintBasename.Length - CapsulesModEntries.CAPSULE_SUFFIX.Length);
        bluetype = ItemTypes.IndexLookup.GetIndex (blueprintBasename + itemtypeFullname.Substring (itemtypeFullname.Length - 2));
      }
      List<BlueprintTodoBlock> blocks;
      if (ManagerBlueprints.Blueprints.TryGetValue (blueprintBasename, out blocks)) {
        if (blocks.Count > ScaffoldsModEntries.MAX_PREVIEW_BLOCKS_THRESHOLD) {
          Chat.Send (causedBy, "Blueprint contains too many blocks for preview");
          return;
        }
        ushort scaffoldType = ItemTypes.IndexLookup.GetIndex (ScaffoldsModEntries.SCAFFOLD_ITEM_TYPE);
        foreach (BlueprintTodoBlock block in blocks) {
          if (block.Typename.Equals ("air")) {
            continue;
          }
          Vector3Int realPos = block.GetWorldPosition (blueprintBasename, position, bluetype);
          if (World.TryGetTypeAt (realPos, out ushort wasType) && wasType == BuiltinBlocks.Air) {
            ServerManager.TryChangeBlock (realPos, scaffoldType, causedBy);
          }
        }
        ThreadManager.InvokeOnMainThread (delegate () {
          if (World.TryGetTypeAt (position, out ushort actualType) && actualType == newtype) {
            RemoveScaffolds (position, bluetype, causedBy);
          }
        }, 8.0f);
      }
    }

    public static void RemoveScaffolds (Vector3Int position, ushort wastype, Players.Player causedBy)
    {
      string itemtypeFullname = ItemTypes.IndexLookup.GetName (wastype);
      string blueprintBasename = itemtypeFullname.Substring (0, itemtypeFullname.Length - 2);
      ushort bluetype = wastype;
      if (blueprintBasename.EndsWith (CapsulesModEntries.CAPSULE_SUFFIX)) {
        blueprintBasename = blueprintBasename.Substring (0, blueprintBasename.Length - CapsulesModEntries.CAPSULE_SUFFIX.Length);
        bluetype = ItemTypes.IndexLookup.GetIndex (blueprintBasename + itemtypeFullname.Substring (itemtypeFullname.Length - 2));
      }
      List<BlueprintTodoBlock> blocks;
      if (ManagerBlueprints.Blueprints.TryGetValue (blueprintBasename, out blocks)) {
        if (blocks.Count > ScaffoldsModEntries.MAX_PREVIEW_BLOCKS_THRESHOLD) {
          return;
        }
        ushort scaffoldType = ItemTypes.IndexLookup.GetIndex (ScaffoldsModEntries.SCAFFOLD_ITEM_TYPE);
        foreach (BlueprintTodoBlock block in blocks) {
          if (block.Typename.Equals ("air")) {
            continue;
          }
          Vector3Int realPos = block.GetWorldPosition (blueprintBasename, position, bluetype);
          if (World.TryGetTypeAt (realPos, out ushort wasType) && wasType == scaffoldType) {
            ServerManager.TryChangeBlock (realPos, BuiltinBlocks.Air, causedBy);
          }
        }
      }
    }
  }
}
