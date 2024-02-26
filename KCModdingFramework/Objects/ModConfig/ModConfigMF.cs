using KaC_Modding_Engine_API.Objects.Generators;
using KaC_Modding_Engine_API.Objects.Resources;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KaC_Modding_Engine_API.Objects.ModConfig
{
    /// <summary>
    /// The information for mods being registered.
    /// This should be sent to the KCModdingFramework by modders.
    /// </summary>
    public class ModConfigMF
    {
        /// <summary>
        /// The name of the mod.
        /// </summary>
        public string ModName { get; set; }

        /// <summary>
        /// The author of the mod. Eg greenking2000.
        /// </summary>
        public string Author { get; set; }

        /// <summary>
        /// The Generators this mod uses to add new items to the map.
        /// </summary>
        public ICollection<GeneratorBase> Generators { get; set; }

        /// <summary>
        /// The asset bundles this mod uses for its resources.
        /// </summary>
        public ICollection<AssetBundle> AssetBundles { get; set; }

        /// <summary>
        /// Gets or sets the dependencies of this mod. Defined by the <see cref="ModName"/> of the mods.
        /// </summary>
        public IEnumerable<string> Dependencies { get; set; }

        /// <summary>
        /// If this mod has been registered with the KCModdingFramework.
        /// Should be `false` when it is sent and `true` when received.
        /// </summary>
        public bool Registered { get; set; } = false;

        /// <summary>
        /// Extra ModdedResourceTypes that none of the <see cref="Generators"/> use.
        /// </summary>
        public ICollection<ModdedResourceType> ExtraModdedResourceTypes { get; set; }

        /// <summary>
        /// All of the ModdedResourceTypes this Mod's <see cref="Generators"/> will use. (And <see cref="ExtraModdedResourceTypes"/>)
        /// This is calculated by reading the Generators. A ResourceType can be used by multiple generators.
        /// </summary>
        public IEnumerable<ModdedResourceType> ModdedResourceTypes
        {
            get
            {
                return Generators.SelectMany(g => g.Resources).Union(ExtraModdedResourceTypes).Distinct();
            }
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <returns><inheritdoc/></returns>
        public override string ToString()
        {
            string generatorNames = string.Join(", ", Generators.Select(g => $"{g.Name}[{g.Guid}]"));

            return $"{ModName} by {Author}. Generators included are {generatorNames}";
        }
    }
}
