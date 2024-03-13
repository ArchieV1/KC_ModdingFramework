using KaC_Modding_Engine_API.Objects.Resources;
using static KaC_Modding_Engine_API.Tools.PrivateFieldTools;

namespace KaC_Modding_Engine_API.Shared.ArchieV1.KCModdingFrameworkAPI.Objects.Resources.VanillaResources
{
    public class NoneModdedResourceType : ModdedResourceType
    {
        public NoneModdedResourceType() : base()
        {
            SetPrivateField(this, "DefaultResource", true);
            SetPrivateField(this, "Name", "None");
            CaveWitchMustBePlacedXTilesAway = 0;
            ResourceType = ResourceType.None;
            DoNotAssignResourceType = true;
            Registered = true;
        }
    }
}
