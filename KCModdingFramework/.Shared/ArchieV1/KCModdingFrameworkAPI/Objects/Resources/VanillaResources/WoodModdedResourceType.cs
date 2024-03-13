using KaC_Modding_Engine_API.Objects.Resources;
using static KaC_Modding_Engine_API.Tools.PrivateFieldTools;

namespace KaC_Modding_Engine_API.Shared.ArchieV1.KCModdingFrameworkAPI.Objects.Resources.VanillaResources
{
    public class WoodModdedResourceType : ModdedResourceType
    {
        public WoodModdedResourceType() : base()
        {
            SetPrivateField(this, "DefaultResource", true);
            SetPrivateField(this, "Name", "Wood");
            CaveWitchMustBePlacedXTilesAway = 0;
            ResourceType = ResourceType.Wood;
            DoNotAssignResourceType = true;
            Registered = true;
        }
    }
}
