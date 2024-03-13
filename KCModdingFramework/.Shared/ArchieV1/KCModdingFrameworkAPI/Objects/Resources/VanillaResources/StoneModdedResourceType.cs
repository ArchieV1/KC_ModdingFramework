using KaC_Modding_Engine_API.Objects.Resources;
using static KaC_Modding_Engine_API.Tools.PrivateFieldTools;

namespace KaC_Modding_Engine_API.Shared.ArchieV1.KCModdingFrameworkAPI.Objects.Resources.VanillaResources
{
    public class StoneModdedResourceType : ModdedResourceType
    {
        public StoneModdedResourceType() : base()
        {
            SetPrivateField(this, "DefaultResource", true);
            SetPrivateField(this, "Name", "Stone");
            CaveWitchMustBePlacedXTilesAway = 2;
            ResourceType = ResourceType.Stone;
            DoNotAssignResourceType = true;
            Registered = true;
        }
    }
}
