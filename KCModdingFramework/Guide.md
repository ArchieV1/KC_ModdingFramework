# Specification
Most specifications will be defined in code and documented in comments. It will also be here though.
*CODE SUPERSEDES COMMENTS WHICH SUPERSEDE THIS DOCUMENT*

# ModdingFrameworkAPI
The modding framework API folder contains the classes and methods needed for your mod to interact with the modding framework.

## ModdingFrameworkNames
Contains strings that are the names of methods and objects needed to execute Remote Procedure Calls (RPCs) on the modding framework.  

Do not:
* Edit
* Add to
* Remove

## ModConfigMF
Contains the details and data from your mod such as assets, mod name and ModObjects.
Will be sent to ModdingFramework using `RegisterMod`

## RegisterMod
`RegisterMod` is the method used to send your mod and all of its data to The Modding Framework.
It will return the same ModConfigMF as was passed to it though depending on what was passed variables will have been set (Eg `Registered=true`, `ExampleResourceTypeBase.ResourceType=12`)

## GeneratorBase
The base class for a generator. A generator is used to generate the World map (Or more accurately to edit the already generated WorldMap).
This allows for new `ResourceTypes` (See `ResourceTypeBase`) to be added to the map or just for editing how the map generates (Eg. Adding twice the amount of iron).
You should implement your own version of this class as follows.  
Most mods will only require a single `GeneratorBase` as multiple may conflict with each other.
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
* `StoneLikeGenerator`
* `WaterLikeGenerator`
* `WoodLikeGenerator`
* `EmptyCaveLikeGenerator`
* `IronDepositLikeGenerator` (Use `StoneLikeGenerator`)
* `WitchHutLikeGenerator`
* `WolfDenLikeGenerator`

Most generators takes 1 `ResourceTypeBase` (The resource to be placed). Any extra `ResourceTypeBase` passed will have no effect.  
The exception is `StoneLikeGenerator` which takes two parameters. The first is the "valuable" `ResourceTypeBase` and the second is the one to be placed like `UnusableStone` around the "valuable" resource. 
If no second value is passed then `UnusableStone` will be placed instead. 


### .Generate(World world)
This is the method that, when implemented, will add new resources to the World map (Or simply edit the map using existing resources).
When the method is called by the modding framework it will be passed the current `World` as a parameter. To copy existing code change use `world.` how `this.` is normally used.

Do:
* Use `World.inst.seed` as a seed for any randomness
* Use `SRand`
* Use `this.RandomStoneState`

Do not:
* Use any random methods without a constant seed (Eg. Do not use `Random random = new Random();` instead, use `Random random = new Random(World.inst.seed);`)

If you do not follow these rules when the map is reloaded from a save the generator will run again and the world will not be the same as it was when it was saved. (Your resources will move around randomly)


## ResourceTypeBase
`ResourceTypeBase` is the base class for new `ResourceType`. It will be sent as part of a `Generator` to the modding framework to be placed in the world.  
It contains information about the resource you want to be created (Such as name and assets).
  
When RegisterMod returns the ModConfigMF each `ResourceTypeBase` within will have had their `ResourceTypeBase.ResourceType` set. This can be used in your code to know if your `ResourceTypeBase` has been selected/mined/built next to etc.

It should be implemented similarly to `GeneratorBase` as follows:
```c#
GoldResource : ModdedResourceType
{
    public override Update()
    {
        // Every update (50x per second) this method will be run
    }
}
```

# Future plans
Future ideas include:
* Test if map saving works properly
* Implement generators
* Implement `Fish` and `Whale` generators
* Tooltips for `ResourceTypeBases` (Like in class `StoneUI`)
    * Check `GameUI.SelectCell`
* Add buildings
    * Extends the `Building` class (Which extends the `Tickable` class so `Tick` must be implemented)
      * Make the build menu expand programatically (Need arrows)
    * Add ability to add new build categories
      * Make new build categories be added programmatically (Need arrows)
* Add translation support
    * Add for the base languages supported by the game
    * Add ability for custom languages using standardised language codes
* Add testing
* Add testing for user made mods
* Make code safer (Less likely to crash)

# To decide
* Instead of applying map edits _after_ the world is generated replace World.GenLand() with my own method
  * This has the advantage of stopping stuff being placed on top of each other as I can keep the coords of waht has been placed easily
    * Pros: More similar to how it works initially
    * Cons: Really hard to implement 
  * Alternative: Create method that scans the map for all resourceTypes so modders can see what other modders have placed down
    * Pros: Simple to create, Simple to use
    * Cons: Modders would need to know what counts as an obstacle in other mods (Eg the game knows that Fertile isnt an issue so will place stone on top. How would modder know that "super fertile" is fine to place Gold on top of? A simple tick inside the resourceTypeBase class would prob fix that)
    * Unknown: Computation cost. Surely cheap?
* Does `WolfDen` implement `Tick`? If yes add that to `ModdedResourceType`
* Implement `ModObject` as something to be loaded that supports many methods to be overrun
* Which methods? All of the ones other things use I guess
* Not sure if it has advantage:
  * Split ModdingEngineAPI into multiple files?
    * API
      * ModdingFrameworkNames
      * ModConfigMF
      * ModObject
    * GeneratorBase, ResourceTypeBase, BuildingBase
    * Tools, WorldTools