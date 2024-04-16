# Changelog

| modName | FreeIva                              |
| ------- | ------------------------------------ |
| license | GPL-2                                |
| author  | pizzaoverhead, JonnyOThan, Icecovery |

### Known Issues

* Centrifuge internals are always visible when JSI Advanced Transparent Pods is installed

### Tools used for maintaining this file:

* https://pypi.org/project/yaclog-ksp/
* https://yaclog.readthedocs.io
* https://keepachangelog.com

## 0.4.2.0 - 2024-04-16

There is nothing permanent except change


## 0.4.1.0 - 2024-04-16

There is nothing permanent except change


## 0.4.0.0 - 2024-04-16

There is nothing permanent except change


## 0.3.0.0 - 2024-04-16

There is nothing permanent except change


## 0.2.19.0 - 2024-04-16

* something


## 0.2.18.4 - 2024-03-03

### Notable Changes

* Habtech CBM hatches now have a blocked variant
* Stock mk3 crew cabin now has ventral and dorsal hatches and attachnodes
* Open animation limit can now be configured per hatch
* B9PSConditionalProp can now specify multiple subtypes

### Bug Fixes

* Fixed some missing texture errors
* Fixed a bug where using the seats in the inline docking ports could cause the IVA kerbal to stop spawning
* Fixed a bug preventing requireDeploy in the internal module from working
* Improved error reporting when a cutout target isn't found
* Prevent going EVA from hatches connected to deactivated airlocks
* "Close forever" button on the tutorial window will now keep it closed until the next KSP restart instead of per scene
* Fixed auto-detection system for internal spaces that are only referenced via Reviva configs (fixes hatches in some warbirds cockpits when used with Reviva, and probably others)
* Fixed centrifugal force calculation for spinning parts so that it points towards the axis of rotation rather than center of mass
* Fixed broken camera when boarding from EVA before IVA had ever been used since the last scene switch
* Fixed boarding behavior not working when using a kerbal that had not gone EVA since the last scene switch
* Planetside: Fix hatches not changing properly per subtype in the vertical base node
* Planetside: Fix going EVA from some parts when alternate endcaps were used
* Restock: Fix error message when using mk1 lander
* Restock: Fix inflatable airlock
* Restock: Fixed hatches in mk2landercan
* SSPX: inflatable habs and centrifuges no longer show the internal in the IVA overlay when not inflated/expanded
* SXT: Fixed hatches and cutouts in large passenger cabin
* SXT: Fixed a bad model reference in the 6-way hub
* KNES: Fixed dorsal hatch on French Hermes crew module and docking module
* Tantares: Included v2 parts in support patches
* ACK: Fixed blocked bottom hatch in HLS
* ACK: Fixed wrong cutout target in HLS


## 0.2.18.3 - 2023-11-14

### Bug Fixes

* Fixed a new bug related to SSPX centrifuges
* Fix camera glitch when boarding a part from EVA when QuickIVA is installed
* Fix a bug that would break IVA mode when switching vessels while unbuckled
* Fix exception spew when the internal camera was destroyed


## 0.2.18.2 - 2023-11-09

### Bug Fixes

* Removed ALCOR and ASET hatch configs (these are now in their respective mods)
* Fixed some issues with Artemis Construction Kit


## 0.2.18.1 - 2023-11-06

### Notable Changes

* Turkish localization (thanks @Ferristik!)


## 0.2.18.0 - 2023-11-05 [PRERELEASE]

### Notable Changes

* Added support for Lonesome Robots Collection
* Added support for Tiktaalik Dreaming Props
* Added support for Pathfinder
* French localization (thanks @tony48 !)
* Added support for RealChute's stack chute
* Added support for Cormorant Aeronology
* Centrifugal acceleration from rotating parts is now simulated
* Boarding a part from EVA now starts you unbuckled just inside the hatch. You can press C to immediately return to your seat, or disable this behavior in the settings.cfg file.

### Bug Fixes

* Fix JSI EVA hatches in certain cockpits
* Fixed several issues in OPT
* Fixed several bugs that could result in exception spamming, loss of control, or inability to unbuckle
* Better support for WildBlue props
* Stock 0.625m decoupler and separator are now passable to be consistent with Connected Living Space
* Fixed several issues in Tantares
* Hatches that are set to auto-hide when connected no longer hide their paired hatch if it has an animation
* Hatches in jr docking port and benjee�s docking ports are now set to hide when connected
* Fixed texture on the back of Near Future Props EVA hatch
* Fixed cutout and depth mask in SSPX 3.75m greenhouse
* Added ladder support to Habtech Props bunkbed


## 0.2.17.1 - 2023-09-20

### Bug Fixes

* Fix fuel tank masses when NFLV is installed without CyroTanks or Near Future Construction


## 0.2.17.0 - 2023-09-19

### Notable Changes

* Added support for Heisenberg Airships
* Added support for Wildblue physical props
* Added support for DeepFreeze
* Added support for Near Future Launch Vehicles

### Bug Fixes

* Fix some issues with Mk2Expansion
* Added missing parameters to settings.cfg (jump key, jumpforce, walkable slope)
* Support parts that don't use mesh colliders
* Fix NRE when airlock is missing
* Tourists can no longer go EVA
* Now checks for obstructions before playing the animation on EVA hatches
* Parts without inherent tags now get the freeiva tag and category correctly
* Fixed KPBS landing engine passthrough
* Fixed some issues with Exploration Rover Systems
* Smoothed out roll inputs in zero-g


## 0.2.16.1 - 2023-07-02

### Bug Fixes

* Fixed hatch positions and narrow colliders in KPBS central hub


## 0.2.16.0 - 2023-06-29 [PRERELEASE]

### Notable Changes

* Added support for Exploration Rover System
* Added support for Keridian Dynamics
* Added support for The Martian
* Added support for Malemute Rover
* Added support for Coriolis Space Systems
* Added physical props for Knes, Tantares, and BDB
* Squad fire extinguisher is now a prop (placed in mk1 and mk2 lander cans)
* Cargo bags in mk2Inline are now interactable

### Bug Fixes

* Fixed hatch interactions for hatches without handles
* Fixed hatch interactions for non-prop hatches
* Fixed Mk2Expansion shielded docking port
* Fixed targeting seats when they have a non-identity scale
* Fixed going EVA from stock docking ports
* Fixed some issues with stationhub hatch placement
* Fixed tube scaling when hatches were not aligned
* Fixed Knes ECBM hatches
* Fixed OPT J cockpits
* Fixed restock plus inflatable airlock
* Fixed hatch in habtech2 airlock module
* Fixed BDB Dona pod hatch
* Fixed conflicts with RealismOverhaul


## 0.2.15.2 - 2023-05-15

### Bug Fixes

* Fixed MM syntax error in b9aerospace passthrough patches
* Fixed shell collider in Planetside MMSEV
* Fixed stationhub RPM configuration so that wall labels can work again
* Added physical prop support for the MAS version of ASET beverage props
* Converted cargo bags in mk2 cockpit to physical props
* Prevent grabbing props from flight camera (IVA Overlay)
* Clicking hatch buckle buttons from flight camera (IVA Overlay) now works as expected
* Hatches with hideDoorWhenOpen (most docking ports) now also hide their connected hatch like they used to, so that hatches without animations don't get in the way
* Fixed cutout mesh on NF_HTCH_EVA_BASIC
* Fixed chair configuration in mk3expansion's mk3inline cockpit
* Split BDB content into a separate download


## 0.2.15.1 - 2023-05-03

### Notable Changes

* Animated hatch support. All stock and near future props hatches are now animated. Remember you can click the handle to open it!

### Bug Fixes

* Fix some passthrough issues in Near Future Construction and KNES
* Coffee mug in ProbeControlRoom is now interactable
* Use landed gravity threshold in centrifuges
* Fixed a bug that could prevent opening hatches in centrifuges
* Fixed window depth masks on stock parts
* Fixed NRE spew when going EVA when ThroughTheEyes isn't installed
* Fixed softlock when going EVA from inline docking ports


## 0.2.14.0 - 2023-04-24

### Notable Changes

* Added Near Future Construction support
* Added Cold War Aerospace support
* Added Tundra Exploration support

### Bug Fixes

* Fix hatch cutouts on scaled hatches (introduced in 0.2.13.0)
* Fix missing FreeIva modules on FelineUtilityRover, and the collider on the docking bay
* Fix Kerbal Planetary Base System hatches when RPM is installed


## 0.2.13.0 - 2023-04-19

### Notable Changes

* New physical prop system: alt-left-click to grab or throw a prop. Left-click to use the prop (if appropriate). The ALCOR pod is a great testbed for this. Near Future Spacecraft, Stockalike Station Parts Expanded, Habtech2, and Planetside are also good spaces to explore.
* Added OPT support

### Bug Fixes

* Fixed a fatal bug when a camera collider transform is not found (B9 + RPM)
* Added passthrough variant to 1.8m and 2.5m monoprop tanks (requires B9PartSwitch)


## 0.2.12.0 - 2023-04-08

### Notable Changes

* Added support for NeistAir
* Added support for AirplanePlus
* Added support for SXT
* Added support for Kerbonov
* Added support for Warbirds Cockpits

### Bug Fixes

* Disable autopassthrough system when SimpleFuelSwitch is installed
* Fixed several textures in BDB internals


## 0.2.11.1 - 2023-04-01

### Bug Fixes

* Fixed SOCK middeck ceiling collider


## 0.2.11.0 - 2023-03-29 [PRERELEASE]

### Notable Changes

* Added support for KNES
* Added preliminary support for Bluedog Design Bureau. Note this must be installed manually on top of your BDB install, replacing any files as necessary. Eventually this will be included in BDB itself.
* Added support for Mk3 Mini Expansion
* Added support for Starilex's mk1pod Needle IVA
* Added support for SABS IVAs
* Added support for ReStockPlus
* Added localization support (thanks to @tinygrox !) And thanks to all the translators! If you'd like to translate FreeIva into your language, please get in touch on discord.
* Added passthrough support to Near Future Spacecraft's orbital engine

### Bug Fixes

* Fixed m2x Angler seat config
* Added a bunch of missing textures
* Adjusted the restriction for unbuckling to support probe cores/ProbeControlRoom
* Fixed some visual issues with habtech docking ports
* Changed top hatch configuration for Artemis Construction Kit Orion pod: the docking port should now be attached to the parachute cover (should now work with the included craft file)
* Added EVA support to structural tubes. The 1.25m structural fuselage no longer has an IVA but you can EVA through it.
* Fixed some bugs where other internals would disappear on vessel changed events or switching kerbals
* Fixed planetside adapter part preventing opening hatches on the other side
* Added support for old mk2landerCan internals
* Prevent double-clicking on camera colliders when unbuckled
* Increased the acceleration threshold for orienting to the horizon when not landed

### New Known Issues

* Centrifuge internals are always visible when JSI Advanced Transparent Pods is installed


## 0.2.10.3 - 2023-03-10

### Notable Changes

* The KV2Pod and KV3Pod from Making History now have their external hatch moved to match where the internal hatch is located. This may break certain craft designs if that location is occupied. You can delete GameData/FreeIva/SquadExpansion/PartCfgs/KVPod_Airlocks.cfg to restore the default airlock location.
* Unbuckling in non-supported parts is no longer allowed (underlying code changes made this more difficult). As always, if you'd like to see FreeIva support for your favorite IVA mods, let me know on the forum or discord.

### Bug Fixes

* Fixed dockingPortSr showing up in external view
* Fixed more issues with kerbal portraits in conjunction with ProbeControlRoom
* Fixed broken centrifuge movement
* Fixed a bunch of issues with Tantares


## 0.2.10.2 - 2023-03-05

### Notable Changes

* All 3rd-party mod patches are now marked as `:FOR[FreeIva]` so that if another mod wishes to assume ownership of their FreeIva configs, they should use `:BEFORE[FreeIva]`

### Bug Fixes

* Fixed a function signature change that broke KerbalVR in 0.2.10.1
* Fixed flags becoming transparent in ProbeControlRoom


## 0.2.10.1 - 2023-03-05

### Bug Fixes

* KOSPropMonitor now locks out keys properly
* Pressing M while unbuckled no longer breaks the cameras


## 0.2.10.0 - 2023-03-02 [PRERELEASE]

### Notable Changes

* Added support for Feline Utility Rover
* Added support for Tantares
* Added a ladder collider to NearFutureProps cargo nets
* Added ASET wall labels to the stock station hub. Now you can label your hatches!
* HabTechProps and NearFutureProps hatches are now compatible with KerbalVR

### Bug Fixes

* Fixed shadow casters in HabTech2 parts
* Fixed depth mask on Planetside MMSEV
* Fix border mesh on FLA10 adapter part rendering in the wrong location


## 0.2.9.2 - 2023-02-18

### New Dependencies

* CommunityCategoryKit is recommended to see all FreeIVA-supported parts at once. You can also type "freeiva" into the stock search box, but may get some false positives.

### Notable Changes

* Added "freeiva" tag to all traversable parts so they can be found via stock search box
* Added support for CommunityCategoryKit, which will display all FreeIva-supported parts
* Added support for late additions to HabTech2/benjee10's sharedAssets: The ORB and hybrid docking ports

### Bug Fixes

* Fixed a bug where the gravity direction in ProbeControlRoom would be incorrect if the probe core was rotated relative to the root part


## 0.2.9.1 - 2023-02-13

### Notable Changes

* Added support for the upcoming HabTech2 IVA release
* Allow free movement and orientation in 0.05 m/s/s or less (e.g. Gilly's surface), but still apply subjective gravity
* Add sounds for buckling/unbuckling
* Added a system to auto-detect hatch configurations


## 0.2.8.0 - 2023-02-10

### Notable Changes

* Added support for ProbeControlRoom
* You can now switch to a kerbal by pressing V when aiming at them
* Acceleration forces from flight are fully modeled

### Bug Fixes

* Fixed ExtraDockingPorts support
* Fixed a bunch of (harmless) errors and warnings
* Fixed shadows in stock Mobile Processing Lab
* Fixed ALCOR top hatch
* Prepping for better KerbalVR support


## 0.2.7.0 - 2023-02-01

### Notable Changes

* Added support for Universal Storage II
* Added support for the ALCOR pod (yes it still works in 1.12!)

### Bug Fixes

* Fixed mk2Expansion and mk3Expansion passthrough when CryoTanks is installed
* Better integration with RasterPropMonitor (requires 0.31.10.2 or later) so that you can interact with props after moving to another part


## 0.2.6.1 - 2023-01-30

### Notable Changes

* Disallow unbuclking in mk2Expansion Tuna/fishhead cockpit
* Add lights to SSPX centrifuges


## 0.2.6.0 - 2023-01-28 [PRERELEASE]

### Notable Changes

* Added support for Artemis Construction Kit
* Added support for Shuttle Orbiter Construction Kit. Includes brand-new (very bad) IVAs for the spacelab parts.
* Added support for Mark IV Spaceplane System. Reviva is recommended for support in adapter parts.
* Added support for B9 Aerospace
* Passthrough system is more intelligent: disabling is done on a per-part basis and the part's info box will have details about volume penalties, structural variants, or incompatibilities.

### Bug Fixes

* Fixed shadowing inside hatch tubes
* Fixed some config issues in Mk2Expansion
* Added a light to the large rockomax adapter
* Fixed orientation issues with stack bicoupler and large quadcoupler
* Fixed depth mask and border on inline docking ports
* Fixed tube lengths on docking ports and when parts are offset
* Fixed a bug in shell collider bounds that could break interactions after passing through a docking port


## 0.2.5.2 - 2023-01-24

### Notable Changes

* Added support for the ASET mk1-2 IVA (Use Reviva to use this IVA in the mk1-3 pod).
* Revamped models in all stock multi-adapter parts (Thanks @Icecovery )
* Added passthrough support to 1.875m tanks from Making History

### Bug Fixes

* Disabled fuel tank passthrough configs when ModularFuelTanks or ConfigurableContainers is installed
* Fixed hatch issue in mk1-4 pod from NearFutureSpacecraft
* Fixed hatch and collisions issues in some SSPX parts
* Fixed an issue with "temporary" seats like the one in the inline docking port
* Fixed RPM configuration in inline docking port
* Fixed hatch tubes not using the proper size when the attachnodes were changed by b9ps (e.g. planetside endcaps)
* Changed shadow and lighting configuration in the internal space


## 0.2.5.1 - 2023-01-19

### Bug Fixes

* Improved performance when unbuckled or in VR


## 0.2.5.0 - 2023-01-17 [PRERELEASE]

### New Dependencies

* B9PartSwitch is recommended for passable variants of some of the stock parts.
* RasterPropMonitor and ASET props are recommended for the stock inline docking ports.

### Notable Changes

* Added Mk2 Expansion support, including all of its structural pieces. Reviva is recommended.
* Added Mk3 Expansion support, including all of its structural pieces. Reviva is recommended.
* Added Near Future Spacecraft support, including the Vexarp IVA layouts.
* If B9PartSwitch is installed all stock adapter, Rockomax, and mk3 tanks get an option to add a passable crew tube with a penalty to resource capacity and mass ratio. Slanted adapters also require Reviva.
* Revamped internal models for docking ports, large Rockomax adapter, and FLA10 adapter.
* Inline docking ports now have a dedicated control console with RPM support for docking
* Added an options to disable collisions and the tutorial window (see settings.cfg)

### Bug Fixes

* Habtech door prop gets a window plug when it's blocked
* Fixed some issues in planetside crew tubes
* NFProps Cabin door is now clickable
* Fixed a bug where probe control room internals could be incorrectly created
* Added ability for hatches to use depth masks to make sure you can't see distant internal models through their windows
* Clicking on a hatch handle doesn't bypass connection checks
* Fixed a bug where hatches would not be connected after docking
* Fixed an issue that could block mouse clicks


## 0.2.4 - 2023-01-03

### Notable Changes

* Support for Kerbalism's GravityRing and Greenhouse parts
* SSPX centrifuges no longer break when Kerbalism is installed
* Support for modded mk3 cockpits: KermanTech, Ultimate Shuttle IVA, and Mk3 Shuttle Mid-Deck
* Support for Extra Docking Ports
* Support for SDHI Service Module System
* All stock part EVA hatches now have an attachment node so you can connect other crewed parts there to pass through
* Added tutorial window and additional button prompts

### Bug Fixes

* Raised camera height when standing in gravity
* Internals of distant parts are no longer visible when looking out windows
* Can no longer interact with hatches and seats through walls
* Fixed an issue that would occasionally block mouse clicks while unbuckled
* Fixed some issues related to crew transfers
* Tweaked console collider in mk1pod so you can squeeze under it


## 0.2.3 - 2022-12-17

### New Dependencies

* HabTech Props is required for Kerbal Planetary Base Systems

### Notable Changes

* Support for Kerbal Planetary Base Systems - Requires HabTech Props

### Bug Fixes

* Fix some issues caused by restock changes
* Fixed hatch tubes appearing in unsupported parts
* Other minor stuff


## 0.2.2 - 2022-12-15

### Notable Changes

* Movement in gravity (including crouching, jumping and ladders) is fully supported. See the Controls page for more info
* Full support for Stockalike Station Parts Expansion Redux
* Full support for Buffalo 2
* The centrifuges from SSPX will provide artificial gravity. See the Controls page for more info.
* Faster and more accurate mesh cutter
* Added support for Planetside adapter parts

### Bug Fixes

* Fixed leaking OnCameraChanged event handler
* Fixed jittering in movement
* Fix NREs when leaving IVA mode
* The bottoms of stock docking ports are now solid when not attached to something traversible
* Fixed a rendering order issue with transparent geometry
* Scaled hatch props now scale their tubes properly
* Added an internal model to the mk1 structural fuselage
* Fixed cutout meshes (hexagonal prisms, etc) being left behind when the blocked prop was used

### New Known Issues

* When using a part that swaps internals with Reviva, physics will go crazy on the first launch of the vessel. Reverting to launch should fix it. NOTE this has been fixed in Reviva 0.8
* The internals of distant parts are visible through windows


## 0.2.1 - 2022-11-24

### New Dependencies

* Reviva is recommended if you use the vertical node part from Planetside - it has several different configurations, and Reviva is necessary to alter the internal models to match the external configuration.

### Notable Changes

* All parts from Planetside Exploration Technologies are now supported
* Can now board a part from EVA even if there are no empty seats in it
* Can now go EVA from stock docking ports
* Movement now uses momentum and acceleration
* Hatches that are blocked on the other side will change their appearance
* Adjust inline docking port internals

### Bug Fixes

* Fix hatch open sound playing on spawn
* Fixed kerbal heads re-appearing
* Fix jittery camera at pitch limits
* Ships with loops of parts (self-docking) now work
* Adjusted colliders in mk1 and mk1-3 pods
* Don't spawn ProbeControlRoom IVAs for probes
* Fix a model problem in dockingPortJr
* remove ModuleFreeIva from cloned RP-0 parts
* Fix some non-serious errors

### New Known Issues

* Windows in distant parts do not render correctly


## 0.2 - 2022-11-09

* All stock and DLC crewed parts and docking ports (and several adapters) are supported
* Fixed many buckling/unbuckling issues
* You can now go EVA from most hatches that aren't connected to anything
* Added custom hatch props with clickable handles
* Added custom harness and seatbelt props with clickable buttons
* IVA colliders can now be built in regular MODEL nodes
* Sorted out collision layer issues
* Lots of other stuff


## 0.1.2 - 2021-03-27

* Added placeholder Station Hub IVA.
* Added converter for using part colliders as IVa colliders.
* Added floor colliders for all IVA parts.
* Fixed error on part destroyed.


## 0.1.1 - 2021-03-21

* Rebuilt for KSP 1.11.
* Moved to github.
* Improved gravity mode.
* Added jumping.
* Fixed issues with craft containing multiple instances of the same part.
* GUI cleanup.
* Made sounds configurable.
* Assorted small bugfixes.