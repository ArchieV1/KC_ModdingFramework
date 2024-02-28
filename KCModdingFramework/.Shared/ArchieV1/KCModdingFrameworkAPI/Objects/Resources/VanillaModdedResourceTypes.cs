using KaC_Modding_Engine_API.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using static KaC_Modding_Engine_API.Tools.Tools;

namespace KaC_Modding_Engine_API.Objects.Resources
{
    /// <summary>
    /// Used to generate a list of <see cref="ModdedResourceType"/> that mirror those from the Vanilla game.
    /// </summary>
    public class VanillaModdedResourceTypes
    {
        /// <summary>
        /// Generates a list of <see cref="ModdedResourceType"/> that mirrors those from the Vanilla game.
        /// WARNING: Minimise use. This 
        /// </summary>
        public static IEnumerable<ModdedResourceType> GenerateList() 
        { 
            return VanillaResources.List.Select(
            dr =>
            {
                ModdedResourceType rtb = new ModdedResourceType()
                {
                    ResourceType = dr
                };
                PrivateFieldTools.

                                // This sets fields with reflection so that the fields can be left as readonly
                                // Do not want users of the mod setting these so making it awkward will solve that
                                SetPrivateField(rtb, "Name", Enum.GetName(typeof(ResourceType), dr));
                PrivateFieldTools.SetPrivateField(rtb, "DefaultResource", true);

                return rtb;
            });
        }
    }
}
