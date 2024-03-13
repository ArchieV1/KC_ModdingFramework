using KaC_Modding_Engine_API.Objects.Resources;
using static KaC_Modding_Engine_API.Tools.PrivateFieldTools;

namespace KaC_Modding_Engine_API.Shared.ArchieV1.KCModdingFrameworkAPI.Objects.Resources.VanillaResources
{
    public class WitchHutModdedResourceType : ModdedResourceType
    {
        public WitchHutModdedResourceType() : base()
        {
            SetPrivateField(this, "DefaultResource", true);
            SetPrivateField(this, "Name", "WitchHut");
            CaveWitchMustBePlacedXTilesAway = 5;
            NumberTreesRequiredNearby = new TreeRequirement
            {
                Distance = 1,
                NumberTreeTiles = 4,
            };
            ResourceType = ResourceType.WitchHut;
            DoNotAssignResourceType = true;
            Registered = true;
        }
    }
}
