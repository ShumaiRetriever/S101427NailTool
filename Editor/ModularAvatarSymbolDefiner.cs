using UnityEditor;
using System.Linq;

/// プロジェクトにModular Avatarが存在するかを自動検出し、
/// Scripting Define Symbol "MODULAR_AVATAR" を自動で追加・削除する

[InitializeOnLoad]
public class ModularAvatarSymbolDefiner
{
    private const string MODULAR_AVATAR_SYMBOL = "MODULAR_AVATAR";
    private const string MA_TYPE_NAME = "nadena.dev.modular_avatar.core.ModularAvatarMeshSettings";

    static ModularAvatarSymbolDefiner()
    {
        EditorApplication.delayCall += UpdateSymbol;
    }

    private static void UpdateSymbol()
    {
        bool maExists = DoesTypeExist(MA_TYPE_NAME);
        
        var buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
        string definesString = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);
        var allDefines = definesString.Split(';').Where(s => !string.IsNullOrEmpty(s)).ToList();

        if (maExists)
        {
            if (!allDefines.Contains(MODULAR_AVATAR_SYMBOL))
            {
                allDefines.Add(MODULAR_AVATAR_SYMBOL);
                PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, string.Join(";", allDefines.ToArray()));
                UnityEngine.Debug.Log("Nail Tool: Modular Avatarを検出しました。連携機能を有効にします。");
            }
        }
        else
        {
            if (allDefines.Contains(MODULAR_AVATAR_SYMBOL))
            {
                allDefines.Remove(MODULAR_AVATAR_SYMBOL);
                PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, string.Join(";", allDefines.ToArray()));
                UnityEngine.Debug.Log("Nail Tool: Modular Avatarが検出されませんでした。連携機能を無効にします。");
            }
        }
    }

    private static bool DoesTypeExist(string fullTypeName)
    {
        foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            if (assembly.GetType(fullTypeName, false) != null)
            {
                return true;
            }
        }
        return false;
    }
}