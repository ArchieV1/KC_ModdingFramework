using KaC_Modding_Engine_API.Shared.ArchieV1.KCModdingFrameworkAPI.Objects.Resources.VanillaResources;
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
        /// </summary>
        public static IEnumerable<ModdedResourceType> GenerateList() 
        {
            return new List<ModdedResourceType>()
            {
                new EmptyCaveModdedResourceType(),
                new IronDepositModdedResourceType(),
                new NoneModdedResourceType(),
                new StoneModdedResourceType(),
                new UnusableStoneModdedResourceType(),
                new WaterModdedResourceTypeBase(),
                new WitchHutModdedResourceType(),
                new WolfDenModdedResourceType(),
                new WoodModdedResourceType(),
            };
        }
    }
}
