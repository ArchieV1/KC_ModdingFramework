using HarmonyLib;
using KaC_Modding_Engine_API.Tools;
using static KaC_Modding_Engine_API.Tools.Tools;

namespace KaC_Modding_Engine_API.HarmonyPatches
{
    [HarmonyPatch(typeof(KCModHelper.ModLoader), "SendScenePreloadSignal")]
    class SendScenePreloadSignal_Patch
    {
        public static void Postfix()
        {
            ModdingFramework.Inst.Helper.Log("SendScenePreloadSignal postfix");
            ModdingFramework.Inst.Helper.Log(LoggingTools.GetCallingMethodsAsString());
            ModdingFramework.Inst.Helper.Log("Finished postfixing");

            // Initialise my mod in PreloadSignal
            // That way it is the last thing loaded
            // Wants other mods to register in SceneLoaded or before
        }
    }
}
