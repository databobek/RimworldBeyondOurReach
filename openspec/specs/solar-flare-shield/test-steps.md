# Solar Flare Shield Test Steps

Each section can be run on its own from the setup save unless it says otherwise. Save before each section so you can reload.

## Setup

1. Check the log on the main menu after the mod loads. There should be no errors mentioning `SolarFlareShield`.
2. Start a dev quick test colony and turn on god mode.
3. Build a walled, roofed room of about 10x10.
4. Inside it, build a `solar flare shield` and a standing lamp.
5. Outside it, build 10 solarite batteries and a second lamp. Connect everything to one net.
6. Fill the batteries with the `DEV: Fill` gizmo.

To trigger a flare, use the dev action that executes the `SolarFlare` incident. Check the shield's power reading in its inspect pane.

## 1. No flare

The shield reads 0 W and both lamps are on. The info card shows 60,000 W.

## 2. Flare protects the room

Trigger a flare. The lamp inside stays on and the lamp outside goes off. The shield reads 60,000 W.

## 3. Runtime

With nothing else drawing power, the 10 batteries run a lone shield for about one in-game day, slightly under because of self-discharge. Vanilla flares are shorter than this, so lengthen one in dev mode or check the drain rate.

## 4. Heat

Repeat in rooms of about 25, 100 and 900 cells.
- The small and medium rooms climb to 65°C and settle there without overshooting.
- The large room approaches 65°C or plateaus below it, depending on the weather.

## 5. Generators and the comms console

- A chemfuel generator inside the room slows the battery drain.
- The same generator outside the room adds nothing.
- A comms console inside the room stays unusable during the flare.

## 6. Room opened mid-flare

Remove the roof during a flare. Protection and heating stop once the room counts as outdoors, the lamp goes off, and the inspect pane shows "Not shielding". If it rains, the unroofed shield can short-circuit.

## 7. Shield destroyed mid-flare

Destroy the shield during a flare. The room goes dark over the next few seconds, with no log errors.

## 8. Save and reload mid-flare

Save during a flare and reload. The shield still reads 60,000 W, the inside lamp is on, and the log has no errors.

## 9. Two shields in one room

Both draw 60,000 W during a flare. Switching one off keeps the room protected.

## 10. Placement and flare end

- Placing the shield outdoors, or in a walled room that is at least 25% unroofed, is refused with a message asking for an enclosed room with a roof.
- When the flare ends, the shield returns to 0 W, heating stops and outside buildings come back on.

## 11. Running out of power

Empty the batteries with `DEV: Empty` during a flare. Protected consumers and the shield drop off, and the room goes dark.

## 12. Restart on generators alone

On a net with no batteries, put two solarite reactors and a lamp in the shield's room. During a flare, switch the shield off and on. The lamp goes off with the shield, the shield comes back on within a few seconds, and then the lamp follows.

## 13. RT Solar Flare Shield

With RT Solar Flare Shield active, add a powered `magnetic shield` to the setup, on the same net or its own.

- During a flare, every lamp stays on, the RT shield reads 25,000 W and the BOR shield reads 0 W. The room does not heat up from the BOR shield.
- The BOR shield's inspect pane says its shielding is inactive because of interference from the magnetic shield. An Umbral shield on the map says the same.
- Flick the RT shield off during the flare. The message goes away, the BOR shield goes to 60,000 W and only its room stays lit.
- After the flare, the message is gone and the BOR shield reads 0 W.
- Without RT Solar Flare Shield loaded, sections 1 and 2 behave as before, with no log errors.
