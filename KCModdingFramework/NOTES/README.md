# Specification
Most specifications will be defined in code and documented in comments. It will also be here though

# ModdingFrameworkAPI
The modding framework API contains the classes and methods needed for your mod to interact with the modding framework.

## ModdingFrameworkNames
Contains strings that are the names of methods and objects needed to execute Remote Procedure Calls (RPC) on the modding framework.  

Do not:
* Edit
* Add to
* Remove

## ModConfigMF
Contains the details and data from your mod such as assets, mod name and ModObjects.
Will be sent to ModdingFramework using `RegisterMod`

## RegisterMod
`RegisterMod` is the method used to pass your mod and all of its data to the modding framework.
It will return the same ModConfigMF as was passed to it though depending on what was passed variables will have been set (Eg `Registered=true`, `ResourceType=12`)

## GeneratorBase
The base class for a generator. A generator is used to generate the World map (Or more accurately to edit the already generated WorldMap).
This allows for new ResourceTypes (See ResourceTypeBase) to be added to the map.
You should implement your own version of this class as follows.
```c#
MyGenerator : GeneratorBase
{
    public override Generate()
    {
        // Code here edits the world map
    }
}
```

## Examples generators
Some example generators are built in if you do not want to create your own.
The example generators are:  
* StoneLikeGenerator
* WaterlikeGenerator
* WoodlikeGenerator
* EmptyCavelikeGenerator
* IronDepositlikeGenerator*
* WitchHutlikeGenerator
* WolfDenlikeGenerator

Every generator except IronDepositlikeGenerator takes 1 `ResourceTypeBase` (The resource to be placed). Any extra `ResourceTypeBase` passed will have no effect.  
Unlike the others IronDepositlikeGenerator takes two parameters. The first is the "valuable" ResourceTypeBase and the second is the one to be placed like `UnusableStone` around the "valuable" resource. 
If no second value is passed then UnusableStone will be placed instead. 


### .Generate(World world)
This is the method that, when implemented, will add new resources to the World map (Or simply edit the map using existing resources).
When the method is called by the modding framework it will be passed the current `World` as a parameter.

Do:
* Use `World.inst.seed` as a seed for any randomness
* Use `SRand`
* Use `this.RandomStoneState`

Do not:
* Use any random methods without a seed

If you do not follow these rules when the map is reloaded from a save the generator will run again and the world will not be the same as it was when it was saved.


## ResourceTypeBase
`ResourceTypeBase` is the base class for new ResourceTypes. It will be sent as part of a `Generator` to the modding framework to be placed in the world.  
It contains information about the resource you want to be created (Such as name and assets).
  
When RegisterMod returns the ModConfigMF the `ResourceTypeBases` within will have had their `ResourceType` set. This can be used in your code to know if your `ResourceTypeBase` has been selected/mined/built next to etc.

It should be implemented similarly to `GeneratorBase` as follows:
```c#
GoldResource : BaseResourceType
{
    public override Update()
    {
        // Every update (50x per second) this method will be run
    }
}
```

# Future plans
Future ideas include:  
* Add buildings
    * Extends the `Building` class with extends the `Tickable` class
    * Make the build menu expand programatically
    * Add ability to add new build categories
* Add translation support
    * Add for the base languages supported by the game
    * Add ability for custom languages using official language codes
* Add testing
* Add testing for user made mods
* Improve `Tools`
* Make code safer

# TODO
* Does `WolfDen` implement `Tick`? If yes add that to `ResourceTypeBase`
* Does mod load order matter? If yes create queue and load alphabetically
  * If doing this then all mods have to be able to receive messages no?
* Tooltips for `ResourceTypeBases` (Like in class `StoneUI`)
* Test if map saving works properly
* Implement `Fish` and `Whale` generators?
* Decide whether to make all base classes extend ModObject?
* Split ModdingEngineAPI into multiple files
  * API
    * ModdingFrameworkNames
    * ModConfigMF
    * ModObject
  * GeneratorBase, ResourceTypeBase, BuildingBase
  * Tools, WorldTools