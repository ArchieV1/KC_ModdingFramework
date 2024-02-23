namespace KaC_Modding_Engine_API.Names
{
    /// <summary>
    /// Names of Methods that can be called against the KCModdingFramework object
    /// </summary>
    public static class MethodNames
    {
        /// <summary>
        /// Send by mods to register themselves, menu returns a ModConfig with all values assigned (The ResourceType they have been assigned):
        /// Parameter: ModConfigMF
        /// Return value: ModConfigMF
        /// </summary>
        public static string RegisterMod => "RegisterMod";

        /// <summary>
        /// Get list of all mods loaded by the modding framework
        /// </summary>
        public static string GetLoadedModList => "GetLoadedModList";

        /// <summary>
        /// Returns list of all assigned resource types
        /// </summary>
        public static string GetAssignedResourceTypes => "GetAssignedResourceTypes";
    }
}
