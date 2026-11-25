# Build / smoke-test checklist

Run in Unity 6.3 LTS before publishing a public release.

- [ ] Project imports without C# errors.
- [ ] Main.unity opens and Play starts at the title menu.
- [ ] New Game starts outside the cabin; title menu cursor is visible.
- [ ] Cabin door enters the interior; front door exits back outside.
- [ ] WASD + mouse movement works in first-person.
- [ ] C toggles the offset third-person camera and the character silhouette.
- [ ] E prompts resolve at the cabin, forest, cliff, mill, gas station, and town.
- [ ] Morning kit panel accepts all four choices and closes.
- [ ] Firewood, fish, provisions, work pay, shop purchase, and fuel purchase update inventory / money.
- [ ] Cooking consumes one available provision; stove consumes one firewood.
- [ ] Radio and cellar drawer dialogue open and close with both buttons and number keys.
- [ ] Night event appears once when the clock reaches 7:30 PM.
- [ ] Deputy conversation becomes available on Day 2 after the night event.
- [ ] Bed advances the day only after 6 PM; Day 6 shows an ending.
- [ ] Save → title → Continue restores day, time, inventory, clues, and gear.
- [ ] Settings can rebind all eight listed controls and persist them.
- [ ] No movement through the cabin interior walls; road and exterior building colliders are reasonable.
- [ ] Windows x64 build launches on a clean Windows machine.
