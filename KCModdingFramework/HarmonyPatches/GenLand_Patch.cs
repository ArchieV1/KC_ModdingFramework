using HarmonyLib;
using System;
using static KaC_Modding_Engine_API.Tools.Tools;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KaC_Modding_Engine_API.Objects.Generators;
using KaC_Modding_Engine_API.Objects.ModConfig;
using KaC_Modding_Engine_API.Tools;

namespace KaC_Modding_Engine_API.HarmonyPatches
{
    [HarmonyPatch(typeof(World), "GenLand")]
    class GenLand_Patch
    {
        /// <summary>
        /// Runs after map has been generated and adds every resource with a Generate() method
        /// </summary>
        /// <param name="__instance"></param>
        public static void Postfix(ref World __instance)
        {
            KCModHelper helper = ModdingFramework.Inst.Helper;
            if (helper == null) return;

            helper.Log($"POSTFIXING \"GenLand\" with seed: {__instance.seed}");
            helper.Log("Calling methods: " + LoggingTools.GetCallingMethodsAsString());

            if (ModdingFramework.Inst.RegisteredModConfigs == null) return;
            foreach (ModConfigMF modConfig in ModdingFramework.Inst.RegisteredModConfigs)
            {
                if (modConfig.Generators == null) continue;
                helper.Log($"Mod {modConfig.ModName} contains {modConfig.Generators.Count()} generators");
                foreach (GeneratorBase generator in modConfig.Generators)
                {
                    helper.Log($"Generator: {generator}");
                    helper.Log($"Contains:");
                    if (generator.Resources == null) continue;
                    foreach (var resource in generator.Resources)
                    {
                        helper.Log($"\t{resource.Name}");
                    }

                    // Go and use this GeneratorBase's Generate method using its assigned ResourceTypes (AV00, AV01 etc)
                    bool implemented = false;
                    try
                    {
                        implemented = generator.Generate(__instance);
                    }
                    catch (Exception e)
                    {
                        helper.Log(e.ToString());
                        helper.Log($"Failed to generate: {generator}");
                    }

                    helper.Log($"{(implemented ? "Used" : "Didn't use")} {generator}");
                }
            }
            helper.Log("Finished postfixing");
        }
    }
}
