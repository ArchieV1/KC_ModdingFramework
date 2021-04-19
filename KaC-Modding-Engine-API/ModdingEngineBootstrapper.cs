// using System;
// using UnityEngine;
// using UnityEngine.Events;
//
// namespace KaC_Modding_Engine_API
// {
//     public class ModdingEngineNames
//     {
//         public static class Methods
//         {
//             /// <summary>
//             /// Send by mods to register themselves, menu returns a ModConfig with all values assigned (The ResourceType they have been assigned):
//             /// Parameter: ModConfigME
//             /// Return value: ModConfigME
//             /// </summary>
//             public static readonly string RegisterMod = "ModdingEngine::Methods::RegisterMod";
//         }
//         public static class Objects
//         {
//             /// <summary>
//             /// The name of the gameobject the modding engine is attached to
//             /// </summary>
//             public static readonly string ModMenuName = "ModdingEngine";
//         }
//     }
//
//     public class ModdingEngineBootstrapper : MonoBehaviour
//     {
//         // Code modified from Zat.ModMenu.API
//         // Have a copy of this file in your mod's files
//
//         public IMCPort port;
//
//         public static void Register(ModConfigME configMe, UnityAction<ModdingEngineProxy, ModConfigME> onRegistered,
//             UnityAction<Exception> onError,
//             int retries = 30, float delay = 1f)
//         {
//             var host = new GameObject($"ModdingEngine::Client::{Guid.NewGuid()}");
//             var bootstrapper = host.AddComponent<ModdingEngineBootstrapper>();
//
//             bootstrapper.InitPort(configMe, onRegistered, onError, retries, delay);
//         }
//
//         private void InitPort(ModConfigME configMe, UnityAction<ModdingEngineProxy, ModConfigME> onRegistered,
//             UnityAction<Exception> onError,
//             int retries = 30, float delay = 1f)
//         {
//             StartCoroutine(WaitForTarget(configMe, onRegistered, onError, retries, delay));
//         }
//
//         private System.Collections.IEnumerator WaitForTarget(ModConfigME configMe,
//             UnityAction<ModdingEngineProxy, ModConfigME> onRegistered,
//             UnityAction<Exception> onError,
//             int retries = 30, float delay = 1f)
//         {
//             port = gameObject.AddComponent<IMCPort>();
//             var _retries = retries;
//             GameObject modMenu = null;
//             do
//             {
//                 yield return new WaitForSeconds(delay);
//                 modMenu = GameObject.Find(ModdingEngineNames.Objects.ModMenuName);
//             } while (_retries-- > 0 && modMenu == null);
//
//             if (modMenu == null)
//             {
//                 onError(new Exception(
//                     $"WaitForTarget: Missing ModMenu GameObject \"{ModdingEngineNames.Objects.ModMenuName}\"!"));
//                 yield break;
//             }
//
//             yield return RegisterOnTarget(configMe, onRegistered, onError, retries, delay);
//         }
//
//         private System.Collections.IEnumerator RegisterOnTarget(ModConfigME configMe,
//             UnityAction<ModdingEngineProxy, ModConfigME> onRegistered,
//             UnityAction<Exception> onError, int retries = 30, float delay = 1f)
//         {
//             yield return new WaitForSeconds(delay);
//             try
//             {
//                 port.RPC<ModConfigME, ModConfigME>(ModdingEngineNames.Objects.ModMenuName,
//                     ModdingEngineNames.Methods.RegisterMod, configMe, 3f, (modConfig) =>
//                     {
//                         var proxy = gameObject.AddComponent<ModdingEngineProxy>();
//                         proxy.port = port;
//                         proxy.configMe = configMe;
//                         proxy.Setup();
//                         onRegistered?.Invoke(proxy, modConfig);
//                         Destroy(this);
//                     },
//                     (ex) =>
//                     {
//                         if (retries > 0)
//                             StartCoroutine(RegisterOnTarget(configMe, onRegistered, onError, retries - 1, delay));
//                         else
//                             onError?.Invoke(ex);
//                     }
//                 );
//             }
//             catch (Exception ex)
//             {
//                 if (retries > 0)
//                     StartCoroutine(RegisterOnTarget(configMe, onRegistered, onError, retries - 1, delay));
//                 else
//                     onError?.Invoke(ex);
//             }
//         }
//
//         /// <summary>
//         /// The ModMenu API client used to interact with the ModMenu
//         /// </summary>
//         public class ModdingEngineProxy : MonoBehaviour
//         {
//             /// <summary>
//             /// IMCPort to communicate through; set by ModdingEngineBootstrapper
//             /// </summary>
//             public IMCPort port;
//
//             /// <summary>
//             /// ModConfigME associated with this proxy, mirrors the state of the configMe in the central ModMenu
//             /// </summary>
//             public ModConfigME configMe;
//
//             /// <summary>
//             /// Called once by ModSettingsBootstrapper to set up various listeners on the port, effectively registering callbacks for events raised by the ModMenu
//             /// </summary>
//             internal void Setup()
//             {
//
//             }
//         }
//     }
// }