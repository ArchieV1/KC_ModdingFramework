using KaC_Modding_Engine_API.Objects.Resources;
using static KaC_Modding_Engine_API.Tools.PrivateFieldTools;

namespace KaC_Modding_Engine_API.Shared.ArchieV1.KCModdingFrameworkAPI.Objects.Resources.VanillaResources
{
    public class UnusableStoneModdedResourceType : ModdedResourceType
    {
        public UnusableStoneModdedResourceType() : base()
        {
            SetPrivateField(this, "DefaultResource", true);
            SetPrivateField(this, "Name", "UnusableStone");
            CaveWitchMustBePlacedXTilesAway = 2;
            ResourceType = ResourceType.UnusableStone;
            DoNotAssignResourceType = true;
            Registered = true;
        }
    }
}
