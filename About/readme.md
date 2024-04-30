# Beyond the Ambition of the Quasar (BOR)

Welcome to the fan-made expansion to the *Ambition of the Quasar* an excellent mod by **explodoboy**.

## Rationale behind the project

I've always been a fan of the *Science Never Stops* mods, starting back in the Alpha stages of Rimworld. Starting back then as well was my goal to extend Rimworld beyond slapping on dozens or hundreds of unrelated mods atop of it.

Back in A16 I've made a collection of hundreds of mods which I've saved locally and modified to work with each other, increasing the challenge, fixing the logical gaps between them and introducing some concepts of my own.

Since back then there were no XML Patches, I was left with no choice but to keep the changes to myself. Rimworld soon started rapidly updating and the whole thing collapsed unto itself.

Years later, in 1.3, I've noticed that there are quite a few mods that delve into similar topics or are conceptaully similar on meta-level. I've again modified them locally and was quite happy to keep it that way, then discovering the XML Patching system and its power and how easy it is to (mis)use.

I've wanted to share my meta-concept with other gamers, so I've decided ot cobble together a DLC for a mod that kept me entertained for years.

Let's delve *beyond* the Ambition of the Quasar.

## Design decisions
While I didn't really want to change or interfere with the lore of the BOR in any way, I did find the artistic direction lacking. Thus why the retextured assets now all have very distinct look and feel between the "Generations" of the technologies.

*Solar*, the first generation is colorful and quite flashy with blues and oranges sprinkled atop silvery constructs. These buildings do not favor form over function, so we tried to make them both utilitarian and pretty. Think of it a bit of a pre-glitter, approaching Star Trek levels of technology - peak of humanity which people refer to in stories of the civilizations long gone.

*Quasar*, the second generation is monochromatic with gray and dark blue dominating the utilitarian feel of this technology. The aim was to keep it a bit mysterious in a cross between Star Trek, Battlezone and C&C esthetics. These designs favor function immensely, with form being secondary if at all considered. Think of it of the technology reappearing after civilization collapsed, technology held by hidden corporations, vying for dominance in the dark depths of space.

*Enigmatic*, the last generation is shrouded in the black and red tints, which we called the *faux-archotech*. These buildings favor form over function, their function is *tenebral* and they serve as a spite to the golden archotechs. Think of it as the technology of the far future, held by people no longer considering themselves human and yet desperately clinging to the last shreds of their souls. This technology is as ominous as grasping the threads of reality itself.

---
BOR itself offers a plethora of great content, starting with bionics that elegantly extend the gameplay from surviving to becoming technological gods, ending with infinite power sources and fabricators creating resources out of the void itself. However, I've found it misses 2 pieces of the "puzzle".

First thing I didn't like is the obvious lack of automation. These walking gods, knowing everything about everything have to still manually go into the machine to pull out whatever they need - handling the ubiquitous yet mysterious Neutronium with their bare hands?

Second thing I didn't like was that there's dozens of high-tech mods which would very obviously translate into the concepts built by BOR, yet there was no effort to try to integrate them. (This can also be extrapolated to Rimworld modding in general, where most of the mods live in their own disconnected universes).

## Changes and guide
This part aims to tell the new player where the new content fits, how to activate it and how is it useful.

### "Vanilla" Beyond the Ambition of the Quasar
When loading BOR for the first time, you'll want to start a new save. There's a lot of changes here that may break your current save. You've been warned.

First thing you'll notice is that all the buildings, resources and some of the equipment looks different. That's right - all the buildings have been retextured. Hope you like it!

There's also another "first" thing you'll notice, and that's the *UI QOL improvements* I've decided to implements. All of the buildings that relate to each other have now been placed in designator dropdowns - you might remember them from how Bionic tables are handled in base BOR. All the buildings related to BOR are now found in a separate Architect tab called "Beyond" -- this also extend to *integrated mods*.

Please mind that some of the buildings have been resized for balance and looks. Mostly for looks! For example the Portable Reactors are now *high* not *deep*.

New functionalities introduced are mostly related to the Vanilla Expanded team's pipeline network, which is now the main way to interact with the Neutronium, which lore-wise is the center of the technological advancements in BOR.

* Neutronium is produced in the Quasar Fabricator
* You should use it to feed the Neutronium pipeline, which is built using sunsteel (again, lore accurate).
* Atomic Fabricator and the R.I.F.T. Fabricator no longer use Neutronium as ingredients, but use *nothing* as ingredients. This makes them follow the same logic as the ultimate workbench - the Utopia Device - which does not use ingredients either.
* Atomic Fabricator recipes no longer take a constant amount of work to complete, they all vary and the speed is based on the old value of the amount of Neutronium used for a recipe.
* All the recipes should now use the General Labor Speed as the base of the speed of creation.
* Atomic Fabricator, R.I.F.T. Fabricator and their linkables are now connected to the Neutronium network, and they use **tremendous** quantities of it, so expect to have to produce it constantly.
* Quasar Reactor Generation II now needs to be connected to the Neutronium network for fuel.

---

New buildings introduced to the game in the base version are the 3 refineries (one for each generation) that allow you to schedule a resource to be produced from the Neutronium stored in the network. Production takes a lot of time and a lot of power so you'll have to make sure your powernet is stable before trying to use them. For example, the first generation needs an entire year to produce a batch of product - however the batches are very big, which makes it worth it to wait.

Another new building is the Self-sustained Neutronium Generator, which is a pillar-like building allowing you to automatically produce Neutronium in the later stages of the game.

---

There are 3 new resources introduced to the game, very inspired by the *Archotech Expanded* mod. These are the nano-, pico- and femtoclusters. These do not need a lot of resources but a great deal of time to produce. They are required as ingredients for installing bionics.

---
All Mods And DLC ARE OPTIONAL BUT HIGHLY RECOMMENDED
---

### Biotech
With Biotech enabled, the research tree is extended with three new research nodes, allowing you to invent and build the *Solar*, *Quasar* and *Enigmatic* Pollution Pumps. I've felt there's very little of those on the workshop, so here you go! Art tbd

### MedPod
MedPod is one of my favourite mods, because of how unusual it is. While most other *medical* mods introduce new offsets and improve stats, this one takes away a job. Having a MedPod, you no longer need a doctor. I think it fits great with this mod's theme, so when loaded along BOR, it will be integrated:

* MedPods now sit in *Beyond* tab
* MedPods are all under one designator dropdown
* Building MedPods uses BOR's resources
* MedPods need **a lot** of power
* MedPod research is locked behind 2nd Generation's medicine research
* MedPod research takes **a lot of effort**.

### Replimat
Similarly to *MedPod*, Replimat takes a job away from your colony - this time, you don't need cooks. Replimat fits perfectly into BOR's high-tech theme, and is also integrated:
* Replimats now sit in *Beyond* tab
* Replimat buildings are under one designator dropdown
* Building Replimat network uses BOR's resources
* Replimat buildings use 10x more power than original
* Replimat research is locked behind 2nd Generation's food research
* Replimat research takes far more effort than in original.

### RT Solar Flare Shield
One of the main stoppers and your nemesis on the road to eternity will be the solar flare event. Being cut away from all your fancy tech, refineries and fabricators can be super annoying in the later stages of the game. I've integrated the RT Solar Flare Shield:

* Solar Flare Shield now sits in *Beyond* Tab
* Solar Flare Shield uses BOR's resources to be built
* Solar Flare Shield now has **3 tiers**
    * Generation 1 is worse than the original, takes more power and generates more heat
    * Generation 2 unchanged from the original in its function
    * Generation 3 flips the logic upside down. Uses far more power when *inactive* but when Solar Flare strikes, it will *generate power for you*.
* Solar Flare Shields are locked behind each generation's power research and takes more effort than original

### Fire Control Computers
Since I'm the inventor of this mod (developed by legodude17), I couldn't leave it unattended. It's integrated in the following way:
* Ultratech Fire Control Tower is now considered Generation 1 and uses sunsteel to be built
* Quasar Fire Control Tower is now Generation 2 and has a new texture to fit to it
* Enigmatic Fire Control Tower is introduced to Generation 3 as mockery of the Archotech Fire Control Tower (both are the same in function, but Archotech can be received as reward).

You'll have to earn those automated defenses.

### Quantum Cooling Redux
In the theme of removing a problem, Quantum Cooler was an old mod that introduced a cooler without the downside - it did not emit heat. This mod expands on the idea with 4 tiers of the coolers along with some heaters. Integration:

* In *Beyond* tab
* Have their own designator dropdown
* Uses BOR's materials now
* Aside from the *prototype* it's now locked behind BOR's various researches with increased costs
* Patched for use with *Proxy Heat* mod
* Highest tier buildings, the generators have been changed entirely:
    * they produce 250000W, not 2500W
    * they are 10x10, not 3x3 now
    * they emit a lot of heat/cold respectively, but *cannot be controlled*
    * integration with Proxy Heat means that deploying them is highly dangerous (high risk/high reward)

### Rimmunizations
Mod that fits with the meta-concept of removing a problem (illnesses) but doesn't really fit with the high-techness. Very simple integration: you can't have BOR drugs without going through the entire Rimmunization tree first.

### Omni Core Drill
One of my all-time favorites, the Omni Core Drill allows you to drill the planet for the resources, removing the need for mining/trading expeditions. Integration as follows
* Sits in *Beyond* tab
* has new texture!
* Requires a lot of BOR's resources to be built
* Needs 20000W, not 2000W
    * Change also reflected if you use the Re-Powered mod
* Locked behind Advanced sunsteel Production with its custom research project.

### EGI Holograms and Projectors (TBD)
Beautiful addition for the high-tech colonies, the projectors are integrated:
* production uses BOR's materials
* expanded and separated research for the many variants of the items in the mod
* uses more power, because they tend to be OP sometimes.

### Rimputers (also Rimputers (No Dubs Hygiene))
Very nice use of the linkable functionality that fits well with the never-stopping science. Quite cool that you start with the abacus and end with beacons to eternity, non?

* Each Rimputer research now needs the previous Rimputer connected to the research bench
* Each Rimputer sits in a designator dropdown (TBD)
* Quantum Computer is required for each BOR research
* Quantum Computer has a new texture! (tbd)

### Archotech Expanded
In many ways a parallel to the BOR, Archotech Expanded introduces crazy powerful stuff to build with and crazy bionics.

* Nano-, Pico- and Attomachinery is removed entirely
* the above resources are fully replaced with nano-, pico- and femtoclusters
* each Archotech bionic requires 1 femtocluster during installation
* basic archotech workbench is now the Grand Archotech Workbench, uses more work and resources to be created
* grand archotech workbench is 10x10
* archotech workbench gets a new texture! (tbd)
* bionics require a new custom research to be built and installed

### Vanilla
When Core is loaded, Marine Armor and Recon Armor will be available in 3 additional variants (Solar, Quasar and Enigmatic), each 50% better than the last.

### Royalty
When Royalty is loaded, you'll be able to build all the royalty armours in Solar, Quasar and Enigmatic variants, both normal and prestige variants.

All Royalty specific bionics are available in scaled-up versions (Solar, Quasar, Enigmatic), increasing in expenses and efficiency with each tier.

### Simple Learning
When Simple Learning is loaded, Solar, Quasar and Enigmatic PCs can be used for learning with better results, scaling with the tier.

### EPOE Forked
When EPOE Forked is loaded, you can build and install solar, quasar and tenebral tiers of rib implants, which use the BOR drugs and materials to be built.

### Dub's Bad Hygiene
When Bad Hygiene is loaded, the DBH bionics are expanded with three more tiers to allow you to inhumanize your humans even more.

### Star Crafter's Armory
With Star Crafter's Armory, you're getting all the armours in the three BOR tiers.

### Royal Arsenal
With Royal Arsenal, you're getting all the armours in the three BOR tiers.

### Vanilla Armours Expanded
With Vanilla Armours Expanded, you're getting all the armours in the three BOR tiers.

### Rimsenal - Core
With Rimsenal - Core, you're getting all the armours in the three BOR tiers.

### Rimsenal - Augmented Vanilla Pack
With Rimsenal - Augmented Vanilla Pack, you're getting all the armours in the three BOR tiers.

### Combat Extended
All the armours described above have their respective CE patches.

* Core
The 81mm mortar will now be able to use a Neutronium-infused shell, which is approximately 2/3rd the power of an antigrain shell.
Autocannon, anti-materiel calibers are extended with 4 more solar-tech ammo variants.
Charged calibers are extended with 3 more solar-tech variants.
Railgun calibers are extended with an additional Tenebrite-sabot variant.
Turrets are removed, because you can use the better ammo with older turrets instead, and some of those turrets are insanely OP anyway...
Grenades are removed for the time being - I could not get them to work.

Tier 1, Tier 2 and Tier 3 armors are patched for CE. They are respectively roughly 2, 4, 8 times better than original Marine Armor set and take insane amount of work to create.

Tier 1, Tier 2 and Tier 3 guns use improved versions of the improved calibers described above.

* More Vanilla Turrets 1.1+
The Devastator mortar will now be able to use an additional Plasma Neutronium shell, which is approximately 50% stronger than the plasma one.

* Vanilla Vehicles Expanded
Tango can now use the additional autocannon variants on the turret.
Highwayman now uses an additional variant of its caliber on the turret.
Bulldog and Roadkill get two additional calibers to use on their turrets.

* Roaring Tiger
M1A2 and M48H now have 2 additional round variants to use on the main turret each.
M113M APC can use the additional Neutronium shell (also the devastator extra shell if MVT1.1+ is loaded).
M1A2, M2HB MG turrets use 4 additional anti-materiel variants.
M249 and HEMTT's turrets now use the additional ammo variant as well.

* Vanilla XCOM Weapons - Laser, Magnetic, Plasma
All the guns get 4 new (separate) variants of ammo, utilizing solar technology.

## People involved
### SoldierGG1
Created all of the art in the Generation 1, Generation 2, all refineries and pipenets and all the retextured resources as well as new Generation 1 and Generation 2 weaponry.

### ThatBartGuy
Created all of the art in the Generation 3 (the black-red buildings), aside from the refinery and infinite quasar reactor.

### Thadge/Hobbes (number 1 fan)
Moral support and testing.

### Sovereign
Moral support.

### Bill Doors
Helped with understanding the logic of CE patches/Defs.

### Bambaryła
Concept, XML, management and funding of the project.

### Bonible
Creating sounds for the refinery.

## Future of the project
I'm looking forward to hearing what other mods you'd like integrated. I'm also working in the background on getting some new building components that will allow me to do some more fine tuning of the difficulty and challenges in the mod. Refer to research_tree.svg for mostly accurate research tree including all integrations.