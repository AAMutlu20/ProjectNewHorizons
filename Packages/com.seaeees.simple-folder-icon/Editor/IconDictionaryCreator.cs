using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace SimpleFolderIcon.Editor
{
    public class IconDictionaryCreator : AssetPostprocessor
    {
        private const string AssetsPath = "com.seaeees.simple-folder-icon/Icons";
        internal static Dictionary<string, Texture> IconDictionary;

        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            if (!ContainsIconAsset(importedAssets) &&
                !ContainsIconAsset(deletedAssets) &&
                !ContainsIconAsset(movedAssets) &&
                !ContainsIconAsset(movedFromAssetPaths))
            {
                return;
            }

            BuildDictionary();
        }

        private static bool ContainsIconAsset(string[] assets)
        {
            return assets.Any(str => ReplaceSeparatorChar(Path.GetDirectoryName(str)) == "Packages/" + AssetsPath);
        }

        private static string ReplaceSeparatorChar(string path)
        {
            return path.Replace("\\", "/");
        }

        internal static void BuildDictionary()
        {
            var dictionary = new Dictionary<string, Texture>();

            var appDirPath = Application.dataPath.Replace("Assets","Packages");
            var dir = new DirectoryInfo(appDirPath + "/" + AssetsPath);
            var info = dir.GetFiles("*.png");
            foreach(var f in info)
            {
                var texture = (Texture)AssetDatabase.LoadAssetAtPath($"Packages/{AssetsPath}/{f.Name}", typeof(Texture2D));
                dictionary.Add(Path.GetFileNameWithoutExtension(f.Name),texture);
            }

            var infoSO = dir.GetFiles("*.asset");
            foreach (var f in infoSO) 
            {
                var folderIconSO = (FolderIconSO)AssetDatabase.LoadAssetAtPath($"Packages/{AssetsPath}/{f.Name}", typeof(FolderIconSO));

                if (!folderIconSO) continue;
                Texture texture = folderIconSO.icon;

                foreach (var folderName in folderIconSO.folderNames.Where(folderName => folderName != null))
                {
                    // dictionary.TryAdd(folderName, texture);
                    dictionary.Add(folderName, texture);
                }
            }
            
            IconDictionary = dictionary;
        }
    }
}
