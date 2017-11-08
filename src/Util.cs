using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using Pipliz;
using Pipliz.JSON;

namespace ScarabolMods
{
  public static class ModLocalizationHelper
  {
    public static void localize (string localePath, string prefix)
    {
      localize (localePath, prefix, true);
    }

    public static void localize (string localePath, string keyprefix, bool verbose)
    {
      try {
        foreach (string locFilename in new string[] { "types.json", "typeuses.json", "localization.json" }) {
          string[] files = Directory.GetFiles (localePath, locFilename, SearchOption.AllDirectories);
          foreach (string filepath in files) {
            try {
              JSONNode jsonFromMod;
              if (JSON.Deserialize (filepath, out jsonFromMod, false)) {
                string locName = Directory.GetParent (filepath).Name;
                log (string.Format ("Found mod localization file for '{0}' localization", locName), verbose);
                localize (locName, locFilename, jsonFromMod, keyprefix, verbose);
              }
            } catch (Exception exception) {
              log (string.Format ("Exception reading localization from {0}; {1}", filepath, exception.Message), verbose);
            }
          }
        }
      } catch (DirectoryNotFoundException) {
        log (string.Format ("Localization directory not found at {0}", localePath), verbose);
      }
    }

    public static void localize (string locName, string locFilename, JSONNode jsonFromMod, string keyprefix, bool verbose)
    {
      try {
        string patchPath = MultiPath.Combine ("gamedata", "localization", locName, locFilename);
        JSONNode jsonToPatch;
        if (JSON.Deserialize (patchPath, out jsonToPatch, false)) {
          bool changed = false;
          foreach (KeyValuePair<string, JSONNode> entry in jsonFromMod.LoopObject()) {
            string realkey = entry.Key;
            if (!locFilename.Equals ("localization.json")) {
              realkey = keyprefix + entry.Key;
            }
            string val = jsonFromMod.GetAs<string> (entry.Key);
            if (!jsonToPatch.HasChild (realkey)) {
              Pipliz.Log.Write (string.Format ("localization '{0}' => '{1}' added to '{2}'. This will apply AFTER next restart!!!", realkey, val, Path.Combine (locName, locFilename)));
              changed = true;
            } else if (!jsonToPatch.GetAs<string> (realkey).Equals (val)) {
              Pipliz.Log.Write (string.Format ("localization '{0}' => '{1}' changed in '{2}'. This will apply AFTER next restart!!!", realkey, val, Path.Combine (locName, locFilename)));
              changed = true;
            }
            jsonToPatch.SetAs (realkey, val);
          }
          if (changed) {
            JSON.Serialize (patchPath, jsonToPatch);
            log (string.Format ("Patched mod localization file '{0}/{1}' into '{2}'", locName, locFilename, patchPath), verbose);
          }
        } else {
          log (string.Format ("Could not deserialize json from '{0}'", patchPath), verbose);
        }
      } catch (Exception) {
        log (string.Format ("Exception while localizing {0}", Path.Combine (locName, locFilename)), verbose);
      }
    }

    private static void log (string msg, bool verbose)
    {
      if (verbose) {
        Pipliz.Log.Write (msg);
      }
    }
  }

  public static class MultiPath
  {
    public static string Combine (params string[] pathParts)
    {
      StringBuilder result = new StringBuilder ();
      foreach (string part in pathParts) {
        result.Append (part.TrimEnd ('/', '\\')).Append (Path.DirectorySeparatorChar);
      }
      return result.ToString ().TrimEnd (Path.DirectorySeparatorChar);
    }
  }

  public static class TypeHelper
  {
    public static string RotatableToBasetype (string typename)
    {
      if (typename.EndsWith ("x+") || typename.EndsWith ("x-") || typename.EndsWith ("z+") || typename.EndsWith ("z-")) {
        return typename.Substring (0, typename.Length - 2);
      } else {
        return typename;
      }
    }

    public static string GetXZFromTypename (string typename)
    {
      if (typename.EndsWith ("x+") || typename.EndsWith ("x-") || typename.EndsWith ("z+") || typename.EndsWith ("z-")) {
        return typename.Substring (typename.Length - 2);
      } else {
        return "";
      }
    }

    public static Vector3Int RotatableToVector (string typename)
    {
      string xz = GetXZFromTypename (typename);
      if (xz.Equals ("x+")) {
        return new Vector3Int (1, 0, 0);
      } else if (xz.Equals ("x-")) {
        return new Vector3Int (-1, 0, 0);
      } else if (xz.Equals ("y+")) {
        return new Vector3Int (0, 1, 0);
      } else if (xz.Equals ("y-")) {
        return new Vector3Int (0, -1, 0);
      } else if (xz.Equals ("z+")) {
        return new Vector3Int (0, 0, 1);
      } else if (xz.Equals ("z-")) {
        return new Vector3Int (0, 0, -1);
      } else {
        return new Vector3Int (0, 0, 0);
      }
    }

    public static string VectorToXZ (Vector3Int vec)
    {
      if (vec.x == 1) {
        return "x+";
      } else if (vec.x == -1) {
        return "x-";
      } else if (vec.y == 1) {
        return "y+";
      } else if (vec.y == -1) {
        return "y-";
      } else if (vec.z == 1) {
        return "z+";
      } else if (vec.z == -1) {
        return "z-";
      } else {
        Pipliz.Log.WriteError (string.Format ("Malformed vector {0}", vec));
        return "x+";
      }
    }
  }
}
