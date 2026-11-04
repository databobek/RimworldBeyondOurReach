# Umbral Shield Test Steps

Save before each section so you can reload.

## Setup

1. Check the log on the main menu after the mod loads. There should be no errors mentioning `UmbralShield`.
2. Start a dev quick test colony and turn on god mode. Finish `BOR_Research_UmbralShield` with the dev tools.
3. Spawn a `BOR_UmbralShield` in the base and connect it to a net with at least 1,000,000 W, such as a lesser umbral reactor plus full batteries.

## 1. Dome

A crimson dome 35 tiles in radius is always shown. Check that the art looks right on the 3x3 footprint. Toggling "Show dome" hides only the dome, and the building stays visible.

## 2. Outside fire

- Spawn hostile pawns with guns outside the dome. Their shots stop at the edge.
- Your pawns' shots from inside pass out through it.
- Hostile pawns who walk inside can shoot your pawns.
- Mortar shells fired from outside stop at the dome.
- With the dome hidden, all of this works the same.

## 3. Drop pods

- Trigger a raid with the drop pod arrival mode, including the center drop. Pods land outside the dome.
- Call a trade ship and buy something. The goods land outside the dome.
- Try targeting your own transport pods inside the dome. The target is refused.

## 4. Solar flares

Trigger a flare. A lamp inside the dome stays on and one outside goes off. A generator inside counts and one outside does not. A comms console inside stays unusable.

## 5. Psychosis

- Fast-forward a few days. A colonist inside gains about 0.09 psychosis severity per day.
- A tenebrite building next to a pawn adds nothing extra inside the dome. Outside the dome it emits as usual.
- A pawn who leaves the dome starts to recover.

## 6. Orientis

Spawn an Orientis pawn inside the dome. They gain no psychosis and have the -20 "under the crimson dome" thought. It ends when they leave.

## 7. Umbral only

- A non-Umbral pawn cannot construct it.
- Choosing the Path of Light turns it into a husk.

## 8. Destroyed and reloaded

- Destroy the shield. The dome is gone and every effect stops.
- Save and reload with the dome hidden. It stays hidden.

## 9. Power

- The shield reads 1,000,000 W whether or not there is a flare. It has no off switch.
- Cut the power. The dome disappears, hostile shots pass, pods can land, a pawn inside gains no psychosis from the shield, and an Orientis pawn loses the thought. Nearby tenebrite emits as usual.
- Restore the power. The dome and all its effects come back.
- During a flare on a net with no batteries, cut the wire to the shield and reconnect it, with reactors inside the radius. Consumers under the dome stay off until the shield comes back, then follow it. Reactors outside the radius cannot restart it.
