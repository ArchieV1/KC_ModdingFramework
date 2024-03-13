using KaC_Modding_Engine_API.Objects.Resources;
using static KaC_Modding_Engine_API.Tools.PrivateFieldTools;

namespace KaC_Modding_Engine_API.Shared.ArchieV1.KCModdingFrameworkAPI.Objects.Resources.VanillaResources
{
    public class IronDepositModdedResourceType : ModdedResourceType
    {
        public IronDepositModdedResourceType() : base() 
        {
            SetPrivateField(this, "DefaultResource", true);
            SetPrivateField(this, "Name", "IronDeposit");
            CaveWitchMustBePlacedXTilesAway = 2;
            ResourceType = ResourceType.IronDeposit;
            DoNotAssignResourceType = true;
            Registered = true;
        }
    }
}
