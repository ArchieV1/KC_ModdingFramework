using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KaC_Modding_Engine_API.Objects.Resources;

namespace KaC_Modding_Engine_API.Objects.Generators
{
    public class EmptyCaveLikeGenerator : GeneratorBase
    {
        public EmptyCaveLikeGenerator(IEnumerable<ModdedResourceType> resourceTypeBases) : base(resourceTypeBases)
        {
        }

        public override bool Generate(World world)
        {
            return base.Generate(world);
        }


    }
}
