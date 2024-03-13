using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KaC_Modding_Engine_API.Objects.Resources;

namespace KaC_Modding_Engine_API.Objects.Generators
{
    public class WitchHutLikeGenerator : GeneratorBase
    {
        private readonly ResourceType witchHut;

        // This is done at the same time as Wolves?
        // Method: DoPlaceCaves
        public WitchHutLikeGenerator(ModdedResourceType resourceTypeBases) : base(new List<ModdedResourceType> { resourceTypeBases })
        {
            witchHut = resourceTypeBases.ResourceType;
        }

        public override bool Generate(World world)
        {
            return base.Generate(world);
        }
    }
}
