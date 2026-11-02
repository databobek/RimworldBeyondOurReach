# Orientis Test Steps

Each section can be run on its own from a fresh dev save unless it says otherwise. Save before each section so you can reload.

## Setup

1. Check the log on the main menu after the mod loads. There should be no errors mentioning `BOR_Orientis`, `Orientis`, `BOR_GoldenScar`, `BOR_Blessing` or `BOR_Meditat`.
2. Start a dev quick test colony and turn on god mode.
3. Stock the map with kwazaari alloy, kwazaari universal parts, gold and silver. Use the dev spawn tool for 1000 of each.

These debug actions are new, and are in the Pawns section of the dev menu:

| Action | What it does |
| --- | --- |
| Force tenebral break | Makes the pawn psychotic if needed, then starts a counted berserk |
| Send Orientis choice | Clears the colony's choice and sends the letter, tied to this pawn |
| Make Orientis | Converts the pawn and grants the ability, without the sequence |

The existing "Grant dark ability..." action is in the same section.

To add tenebral parts and crystallized parts, use the dev add hediff tool on a body part. The hediffs are the tenebral bionics from the Tier 3 defs, and `BOR_Crystallized`.

## 1. Letter timing

1. Use Force tenebral break on a colonist. End the berserk with the dev tools or wait it out. No letter should appear.
2. Do it a second time. No letter should appear.
3. Do it a third time. **Expected:** no letter while the pawn is berserk.
4. End the berserk. **Expected:** within a couple of seconds, a gold letter called "The precipice" arrives.
5. Right-click the letter. **Expected:** it stays.
6. Open it. **Expected:** the text starts "A great shadow looms over the Settlement" and names the pawn, with Path of Light and Path of Dark options.
7. Close it with Escape. **Expected:** it stays in the stack.
8. Save and reload. **Expected:** the letter is still there and still works.

### Letter edge cases

| Setup | Expected |
| --- | --- |
| Third break, pawn has no tenebral parts | Letter arrives when the break ends |
| Third break, kill the pawn while berserk | No letter. Another pawn's third break still sends one |
| Second pawn reaches its third break while a letter is unanswered | No second letter |
| Old save with a pawn already past 3 breaks, and no choice made | Letter arrives when that pawn's next break ends |
| Third break caused by damage or a psycast | Not counted |

## 2. Path of Dark

1. Get the letter and choose Path of Dark.
2. **Expected:** the dialog shows "So be it." The letter is gone after you close it.
3. Force more breaks on this pawn and on others. **Expected:** no letter ever comes again.
4. Keep forcing breaks past the sixth on one pawn. **Expected:** crystallization still works as before.
5. Save and reload, then force another break. **Expected:** still no letter.

## 3. Path of Light

Set up these pawns first:

| Pawn | Setup |
| --- | --- |
| A | Tenebral arm and a tenebral heart, psychotic, with a dark ability |
| B | Crystallized torso and leg, no implants |
| C | Clean colonist, with psychosis only from Force tenebral break |
| D | Clean colonist with nothing |
| E | Umbral, from a Tenebral pod or by adding the `BOR_Umbral` hediff |
| F | Half crystallized, in a Tenebral pod in transformation mode |
| G | Tenebral implant, in a Tenebral pod in stasis mode |
| H | Tenebral part, away in a caravan |
| I | Prisoner with a tenebral part |
| J | Hostile raider with a tenebral part, and a hostile Umbral if you can |

Bring C to three breaks so that C triggers the letter, then choose Path of Light.

**Expected, in order:**

1. The dialog shows "So be it." Close it. The game unpauses if it was paused, and a "The Light descends on the Settlement" message appears. A and B glow gold for about 10 seconds at normal speed, which continues after the letter in step 4. These times are longer if the game runs slowly, so wait for the letter in step 4 before judging the result. C, D, I and J do not glow. F, G and H can't glow, because they aren't on the map.
2. Halfway through the glow, the parts come out. Pod F is destroyed, and F's corpse is on the ground where it stood.
3. A golden beam hits E, and E dies. No nearby pawn or building is damaged. No beam is shown for an Umbral off the map, but that pawn still dies.
4. About 1 second later, a "The light remains" letter arrives, naming the pawn who made the choice, or another Orientis pawn if that one died.

**Check each pawn afterwards:**

| Pawn | Expected |
| --- | --- |
| A | Alive. Natural arm and heart, each with a golden scar. No psychosis, no dark abilities, no bloodlust need. Xenotype Orientis, `orientis` hediff, manifest ability |
| B | Alive. Natural torso and leg with golden scars. Orientis, manifest ability |
| C | Alive with no scars. No psychosis. Orientis, manifest ability, because C triggered the letter |
| D | Unchanged. Not Orientis |
| E | Dead |
| F | Dead |
| G | Still in the pod, stripped and Orientis. Comes out normally when ejected |
| H | Stripped and Orientis when the caravan returns |
| I, J | Unchanged. Still hold their tenebral parts |

**Also check:**

1. Golden scars look like normal scars on the health tab, reduce part health by about 25%, and don't heal.
2. A new Meditate column in the Work tab shows priority 3 for converted pawns and is off for others.
3. Save during the glow, reload, and let it run. **Expected:** the sequence finishes with the same result.
4. Save between the beam and the final letter, reload. **Expected:** same.
5. Repeat without Biotech active. **Expected:** pawns get the `orientis` hediff, the ability, and everything else works.

## 4. Surgery

On a pawn with a tenebral arm and on a pawn with a crystallized leg, at any break count, including 3 or more:

| Operation | Expected |
| --- | --- |
| Remove the tenebral arm | Not offered |
| Remove the shoulder above it | Not offered |
| Install a bionic arm over it | Not offered |
| Remove a tenebral implant | Not offered |
| Remove a crystallized part or its parent | Not offered |
| Install a non-tenebral implant beside a tenebral part, such as on the head when an eye is tenebral | Still offered |
| Tenebral pod operating mode on a part that already has a tenebral part | Part is not listed |

On an Orientis pawn:

| Operation | Expected |
| --- | --- |
| Install a bionic or an implant | Not offered |
| Install a tenebral part | Not offered |
| Install a natural transplant | Still offered |

## 5. Manifest ability

Use Make Orientis on a colonist. Spawn only some of the materials so that some buildings are affordable and some are not.

1. Select the pawn. **Expected:** a manifest gizmo with the placeholder icon, active whether drafted or not.
2. Check the Architect menu. **Expected:** no Orientis buildings anywhere.
3. Click the gizmo. **Expected:** the window opens, the game pauses, and all 8 buildings are listed with image, description and costs as available/needed. Short costs are red.
4. Check that unaffordable buildings have a disabled Place button.
5. Check that descriptions fit in their rows.
6. Choose an affordable building. **Expected:** the window closes and a normal placement ghost follows the mouse. Q and E rotate those that rotate.
7. Try an invalid spot, such as on a wall. **Expected:** rejected with the normal message, nothing is taken.
8. Confirm a valid spot. **Expected:** each stack paid from has a skip effect, nearest stacks are used first, and the finished building appears at once with a big skip effect. Exactly the listed cost is gone.
9. Place a building on items and on a cell a pawn is standing on. **Expected:** items move aside and the pawn is not hurt.
10. Forbid some material, and hide some in unrevealed fog. **Expected:** neither counts as available or gets taken.
11. Select a building, then remove the materials with dev tools before confirming. **Expected:** the placement is refused with "Not enough materials on this map for the...", and nothing is taken or placed.
12. Select two Orientis pawns and click the gizmo. **Expected:** one window.
13. Reload a save with an Orientis pawn. **Expected:** the ability is still there.

## 6. Meditation power

Place a pod, a cache within 12 cells, and a second cache far away. Use an Orientis pawn with Meditate enabled and everything else off.

1. **Expected:** the pawn walks to the pod's interaction cell and meditates, and the near cache fills.
2. Select the cache. **Expected:** a gizmo with the meditation symbol and a bar, and an inspect line with the same numbers.
3. Time one in-game day. **Expected:** about 200 power at 100% strength, and more once the pawn has the bodysuit or focus.
4. Fill the near cache using the DEV: Fill gizmo. **Expected:** the pawn stops when no cache in range has room.
5. Delete all caches near the pod and right-click the pod with the pawn. **Expected:** the prioritize option is disabled, with the "no meditation cache with room nearby" reason.
6. Try a non-Orientis pawn. **Expected:** no meditate job, even with Meditate enabled.
7. Save and reload. **Expected:** the stored amount is the same.
8. Destroy a cache that holds power. **Expected:** no errors, and the map total drops.

## 7. Holy forge

1. Place the forge and fill a cache.
2. Open the forge tab. **Expected:** the 12x12 grid fits on screen and has a smaller cell size. Other forges still use the old size.
3. Have a pawn analyze an item. **Expected:** analysis needs a pawn, as at other forges.
4. Queue the item. **Expected:** no pawn comes to work it, and it finishes on its own, draining the cache. Output appears at the interaction cell.
5. Empty the caches. **Expected:** production stops, and no error is logged.
6. Refill the caches. **Expected:** production resumes.
7. Queue production while an analysis is pending. **Expected:** production waits until the analysis is done.
8. Check the overdrive lever and column format work, with no power connection and no errors.

## 8. Offerings bench

1. Place the bench and fill a cache.
2. Open the Bills tab. **Expected:** only Orientis items are offered, with no ingredient requirement: 4 main melee weapons, 2 crossbows, 4 arquebuses, 4 sabers, 6 armor pieces and the monk bodysuit.
3. Confirm these items are not offered at any other bench.
4. Add a "do 1 time" bill. **Expected:** no pawn comes, the item appears next to the bench, and the bill completes with the normal message.
5. Select the bench while working. **Expected:** an inspect line shows the item and progress.
6. Empty the caches. **Expected:** an inspect line says it is waiting on power, and progress holds.
7. Try "do until you have X" and "do forever" bills, and a suspended bill. **Expected:** each behaves as on a normal bench.
8. Save mid-item and reload. **Expected:** progress is kept.

## 9. Gear

Make one of each from the bench.

**Weapons**

1. Equip each. **Expected:** it renders, has a sensible info card, and can't be smelted.
2. Fire the plasma crossbow at the ground and a target. **Expected:** gold projectile, long charge-up, explosion, can target ground.
3. Fire the laser crossbow and each arquebus. **Expected:** single shots, no ammo, and damage rises with tech level.
4. Melee with each power weapon and saber. **Expected:** hits add burn damage.

**Armor and apparel**

1. Wear each armor and helmet on male, female, thin, fat and hulk body types. **Expected:** it renders from all directions.
2. Check armor values and speed: light has no penalty, medium -0.15, heavy -0.4.
3. Wear the monk bodysuit. **Expected:** meditation strength in the stats panel rises by 25%.

## 10. Meditation bench and mat

Place a bench, a mat and a cache within 12 cells, with no pod. Use an Orientis pawn with Meditate enabled and everything else off.

1. **Expected:** the pawn meditates at the bench's interaction cell, and the cache fills at about half a pod's rate.
2. Occupy the bench or forbid it. **Expected:** the pawn stands on the mat, and the cache fills at about a quarter of a pod's rate.
3. Add a free pod. **Expected:** after its current session, the pawn picks the pod over the bench and mat, even if they're closer.
4. Move or remove the cache so none has room within 12 cells. **Expected:** no job, and right-clicking shows the "no meditation cache" reason.
5. Lower a pawn's recreation need. **Expected:** the bench and mat don't appear as recreation.
6. Try a non-Orientis pawn. **Expected:** no meditate job, even with Meditate enabled.

## 11. Blessing bench

Place the bench.

1. Right-click it with a non-Orientis pawn. **Expected:** a disabled option saying only Orientis pawns can.
2. Right-click with an Orientis pawn, and choose the blessing option. **Expected:** a window listing eyes, ears, jaw, brain, arms, legs, heart, liver, kidneys and lungs.
3. Confirm the Begin button is disabled until a part is checked.
4. Choose the heart and an arm, then begin. **Expected:** the pawn walks over and meditates for about 40 seconds of game time, with a progress bar.
5. When done, **expected:** a message, and a "blessed" hediff on each part showing about 15 days left. Blood pumping and manipulation go up.
6. Open the window again. **Expected:** blessed parts are marked "already blessed".
7. Bless the heart again. **Expected:** the timer restarts with one hediff only, and no extra bonus.
8. Interrupt the job partway. **Expected:** no blessing is applied.
9. Let a blessing run out. **Expected:** the hediff disappears and stats return to normal.
10. Check a pawn with a missing or bionic part. **Expected:** that part isn't listed.
11. Check a blessed part with no scar in the health tab. **Expected:** it shows 320% efficiency, above a kwazaari part at 234% and below a tenebral part at 422%.

## 12. Symbols

| Symbol | Where |
| --- | --- |
| Meditation power | The cache gizmo |
| Manifest | The ability gizmo |
| Orientis sun | The xenotype icon on the pawn's Bio tab, with Biotech |

## 13. Umbral exoskeleton and gene extraction

Use the dev add hediff tool to give an Umbral pawn `BOR_UmbralExoskeleton` and `BOR_UmbralExoskull`.

1. **Expected:** the pawn starts in no mode, with no armor or work bonus, and still looks armored.
2. Choose each mode from the gizmo. **Expected:** the menu shows the durations, and the gizmo and health tab show the time left.
3. Let each mode run out, or check it at about 2 hours for camo, 12 for combat and 24 for utility. **Expected:** it reverts to no mode with a message, and camo invisibility ends.
4. In combat mode, inspect the exoskull. **Expected:** it has armor values of its own, 470% sharp, 210% blunt and 215% heat.
5. In utility mode, check the pawn's stats. **Expected:** global work speed is about 350%, up from 100%.
6. Try to put an Umbral pawn and an Orientis pawn in a gene extractor. **Expected:** both are refused with "genes cannot be extracted".

## 14. Dark buildings after the Path of Light

Before choosing, place an altar, a Tenebral pod with a pawn in stasis, a pacifier turret, an Umbral reactor with something on its grid, an Umbral bench with a bill, and a tenebral blueprint and frame. Use a second map if you can.

1. Choose the Path of Dark on a test save. **Expected:** nothing here changes.
2. Choose the Path of Light. **Expected:** the build menu loses tenebral and Umbral buildings at once, including in god mode.
3. When Umbral pawns are judged, **expected:** each altar gets a golden beam that fades softly at the ground, with a glow, gold dust and sparks where it lands, and turns into a grey "dead dark archotech altar" at once, even with no Umbral pawn present. No tenebrite or components are left behind, including components stored in the altar.
4. As each altar is struck, **expected:** the camera shakes and a gold ring spreads across the whole map, and dark buildings flash and go grey as it reaches them, nearest first, and dark items lying on the map, such as tenebrite, tenebral gear and femtoclusters, flash and burn away. Items worn by pawns who aren't converted, carried or held inside a container don't burn. With no altar on a map, they all go grey at the end of the sequence instead. Either way, the "The light remains" letter waits until the ring has finished. They turn grey in place, with the right footprint and rotation, labeled "dead ...". The pod's pawn is ejected alive, the blueprint and frame are gone, and materials put into the frame come back.
5. Check a husk. **Expected:** no power, no bills, no gizmos, and the turret doesn't shoot. It can be deconstructed, for no materials.
6. Save and reload. **Expected:** husks keep their look and label.
7. Check the log for a "No husk of size" warning. **Expected:** none.

## 15. Meditation shield

Place a pod and a stocked cache, and have an Orientis pawn and a raider with a gun. Add a tenebral part to the raider with dev tools.

1. Check the pod with an empty cache, then with no Orientis pawn on the map. **Expected:** "Enable shield" is greyed out, with the reason.
2. Click "Enable shield" and choose a pawn. **Expected:** the pawn walks to the pod with a progress bar, and a gold dome about 50 tiles across appears.
3. Have a raider shoot from outside the dome, and drop mortar shells from outside. **Expected:** they stop at the edge. Your own shots from inside still go out.
4. Use the dev tools to call a bombardment and a power beam from an enemy, inside the dome. **Expected:** the bombardment doesn't hit and the beam ends at once.
5. Let the raider walk in. **Expected:** the raider starts glowing gold and "burning light" appears within a few seconds, with -35% consciousness, rising 5% each in-game hour and stopping at 95%.
6. Let them leave, and try a non-dark raider. **Expected:** the effect ends on leaving, and the other raider gets nothing.
7. Watch the cache. **Expected:** it drains about 400 per day. At zero the dome falls with a message, and stays down until a pawn raises it again.
8. Save and reload with the shield up and with it down. **Expected:** each stays as it was.

## 16. Light and dark stay apart

Spawn tenebrite, a tenebral weapon, tenebral armor, femtoclusters, an Orientis weapon and an Orientis armor. Have an Orientis pawn and a colonist with a tenebral part.

1. Choose the Path of Light with three colonists wearing tenebral armor and a tenebral weapon: one with tenebral parts, one with none, and one in a caravan. **Expected:** all three glow if on a map, and all lose the gear at the moment parts are stripped, with a gold flash on the map. Nothing drops, and only the one with parts converts. An Umbral pawn keeps its grown exoskeleton until the beam kills it. With the "Make Orientis" debug action, gear drops and stays on the ground, because no sequence is running.
2. Right-click dark gear with the Orientis pawn. **Expected:** equipping is refused with the Orientis reason. It won't haul tenebrite or femtoclusters.
3. Right-click Orientis gear and an Orientis bench with the tenebral colonist. **Expected:** equipping is refused with the dark reason, and it doesn't use or haul them.
4. Check a colonist with neither side's mark. **Expected:** no restriction.
5. When the choice letter opens, **expected:** only Path of Light and Path of Dark are offered, with no jump to location.

## 17. Melee weapon glow

Spawn each Orientis melee weapon and give them to drafted Orientis pawns.

1. Face the pawn in all four directions, by day and at night. **Expected:** the blade has a faint gold glow that lines up with the blade, flipped correctly each way, and the handle has none.
2. Melee something. **Expected:** the glow stays on the blade through the attack.
3. Check the dagger. **Expected:** the glow is scaled down with it.
4. Check an Umbral blade and pole tool. **Expected:** no glow, and the weapon looks as before.
5. With Melee Animation active, draft two pawns and start a melee duel or execution. **Expected:** the weapons animate, sitting properly in the hands, and the blade keeps its glow through the animation. Adjust positions in the mod's tweak editor if they look wrong.
6. Without Melee Animation active, start the game. **Expected:** no errors from `BORMeleeAnimation`, and normal weapon drawing.

## 18. Known limits

Don't file these as bugs:

- All art is placeholder. Armor, weapons and buildings reuse existing textures.
- Costs, rates and stats are unbalanced first guesses.
- With Combat Extended, the new guns and armor have no patch, so expect them not to work properly.
- Exposure to tenebrite can still give an Orientis pawn psychosis.
- Polish has no translation of the new text.
