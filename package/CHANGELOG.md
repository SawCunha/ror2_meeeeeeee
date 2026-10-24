# Changelog

**Language:** English · [Português (Brasil)](https://github.com/SawCunha/ror2_meeeeeeee/blob/main/package/CHANGELOG.pt-BR.md)

## 0.12.1

- **Call of the Horde** counted the copies on Engineer turrets (which copy their owner's inventory) and on other
  allies. With 4 real items, the enemy spawn reached x12 instead of x3.8, and the count did not drop when a turret
  died. Now only the players' copies count.
- **Communism** no longer shares curses (Hex tier, from DLC4). Before, when someone picked up, for example, Share Items
  with Enemies, the other players got the curse too.
- **Freeze diagnostics:** when a frame takes over 1 s, the log now shows what happened inside it: garbage collector (GC)
  collections and RAM before and after, how many heals and damage events the frame processed, and the monsters alive
  (including `AffixEarthHealerBody`, the Mending elite's healing orb). The `[Vigia]` warning during the freeze also shows
  RAM and GC collections.
- **Main thread sampler:** the first time `[Vigia]` detects a freeze (3 s), it briefly pauses the main thread 20 times
  over 1 s and logs which C# methods it is running, e.g. `[Vigia] thread principal (20 amostras): 18x RoR2.X:Method ← ...`.
  This tells whether the freeze comes from the game, this mod or another mod. It only runs during freezes.
- Three errors from the game itself are now only counted (no longer shown as a "new error"): the Ghost Blades orbit
  turning NaN, `TeleportOnLowHealthBehavior.OnDisable` (Unstable Transmitter, when the body is destroyed by the portal
  or on death) and `GlobalEventManager.OnCrit` (an orb arriving after its attacker died).

## 0.12.0

- **New: `[QoL]`** (idea from WolfoQoL_Gameplay by Wolfo, rewritten and checked against game 1.5.0; on by default,
  each part can be turned off):
  - **Items that fall off the map** (orbs in flight, items on the ground, Command Cube, Void Potential, Aurelionite
    Fragment) come back to the nearest ground within 1 s. Host only.
  - **Dead drones** that fall into a pit or through the ground now leave the broken drone (to repair) on the nearest
    ground. Before, they just vanished. Host only.
  - **Lava** deals 2% of health per touch to drones, turrets and other non-player allies, like common monsters, instead
    of the 10% meant for players. Host only.
  - **Non-damaging skills** (Engi bubble, REX Sonic Boom, Loader hook, Railgunner scopes) no longer put you in combat,
    so out-of-combat items (Red Whip, Cautious Slug...) keep working.

## 0.11.5

- **ProjectileBudget, Acolyte wights:** each kill of a haunted enemy released one wight per Haunt stack, and every wight
  that hit added another stack. With the item multiplier this turned into a chain reaction (over 1,000 wights in 30 s,
  the game freezing for seconds and Unity memory going past 4 GB). Now each kill releases at most `WightsPerKill` (3)
  wights, each one carrying the damage of the cut ones, so the total damage stays the same. Host only.

## 0.11.4

- Dependencies: RoR2BepInExPack 1.48.0 and R2API_ContentManagement 1.1.0.
- **Taxade:** the tax now comes **only out of health**; barrier and shield are left untouched. Before, buying something
  with gold could break the shield and set off the Kinetic Dampener, whose blast hit nearby enemies and triggered on-hit
  items (missiles...).
- **Taxade:** the tax no longer triggers the Razorwire. The game treated the charge as a hit and fired razors at nearby
  enemies, which also triggered on-hit items. The same two fixes apply to the health cost of reviving (Helluva).
- **Tempest, `BlockVoidlingUntilFinalStage`:** the block didn't work: the Deep Void portal is created by the Void Locus
  itself, not by the portal system the mod blocked, and the run could end with the Voidling on stage 5. Now, with the
  key on and before `FinalStage`, the Void Locus portal still appears but leads to the next normal stage. From
  `FinalStage` on it leads to the Voidling again. The key stays off by default.

## 0.11.3

- **Tempest:** protection of new enemies (and on each revive) goes from 10 s to **5 s**. Configs with the old default (3
  or 10) move to 5 on their own (`General.ConfigVersion = 3`); a hand-picked value stays.
- **Tempest, performance:** a hit on a protected enemy is now marked as rejected. Before, the game dropped the damage but
  the attacker still ran every on-hit item effect (missiles, lightning, bubbles...) on the immune enemy, and each effect
  hit another immune enemy again. In a test with ~3,300 items there were 71 thousand hits on protected enemies in a
  single stage and freezes from a few seconds to over 2 minutes.
- **Tempest:** the Void Infestor (released by Void elites on death) no longer gets Dio's. It kills itself when it enters
  its target and, with the extra life, revived and infested again.
- Diagnostics: the NRE from `EnableCustomRenderTextureOnLight.OnDisable` (when quitting the game) is now known noise.

## 0.11.2

- **Tempest:** the Multiplier's boost to monster health and damage now applies only from stage 2 on, like the damage
  reduction. In 0.11.1 it already applied at minute 0 (+220% health and damage with x10) and the team was wiped in under
  a minute on stage 1.
- **Enforcer parry** (`[GameFixes]`): the counterattack aims at whoever dealt the parried hit; when that attacker no
  longer exists (it died or, with Tempest's Dio's, revived in another body), the game threw `NullReferenceException`
  every tick of the parry window and the parry didn't fire. Now only the retaliation for that hit is skipped.
- Diagnostics: the NRE from `HurtBox.SimpleColliderBlockFilter` (the Engineer turret's beam going through the blocking
  bubble with the target already destroyed; only that shot is lost) is now counted as known noise.

## 0.11.1

- **Tougher Tempest:**
  - New enemies' protection goes from 3 s to **10 s**, with the game's golden glow so you can see who is protected.
    Existing configs with the old value (3) move to 10 on their own (`General.ConfigVersion = 2`).
  - Each normal enemy gets 1 **Dio's Best Friend** and each elite gets 2 (`ExtraLivesNormal`, `ExtraLivesElite`).
    Bosses, summons (mines, totems, urchins) and temporary minions get none. Each time it revives, the 10 s protection
    applies again.
  - With the **Multiplier** on (x M), the damage that gets through the reduction is divided by
    `M^MultiplierReductionExponent` (new setting, default 0.5): with x10, stage 2 goes from 20% to 75% and stage 3 from
    40% to 81% (cap 99%). The curves' health and damage are multiplied by the same factor (x3.16 with x10). The
    Multiplier value is read at the start of each stage.
- **Status screen (F8):** the backgrounds (panel, cards, portrait frame) disappeared after stage 1, because the scene
  change unloaded the mod's images; the same happened to the F7 screen. Health no longer gets cut off: on the bar and on
  the panel, the columns have different widths and the value shrinks if needed. In English, short numbers show as
  `12.3K`/`4.5M` (before, always `12,3 mil`).
- **Client-side game fixes** (`[GameFixes]`), seen in a client's log from the test run:
  - Homing projectiles and mines (`ProjectileSteerTowardTarget`, `ProjectileSphereTargetFinder`): the game turns back on,
    on the client, a component that only runs on the host (Huntress' Cyclone return, objects released by the Drifter),
    and every tick threw `NullReferenceException`: about 10,700 errors in one run, costing FPS. The mod now turns it off
    again, as the game itself would.
  - Item-draining cauldron (`HallowedCauldron.DrainingItems`): on the client, without the player who activated it, the
    state threw every tick while draining (251 errors). The client's tick is skipped; the host drives it.
- Tempest diagnostics: the stage summary is now written on the scene change (before, the initial monsters went into a
  fake "summary" and were missing from the right stage) and each phase switch is counted once (it was once per director).
- Diagnostics: the NRE from `AcolyteBodySwapController` and the one from `PlaySoundOnEvent.OnDestroy` (both on the
  scene change), the one from `SendP2PPacket` (when quitting, with Steam already closed), the
  `ActivityManager::CurrentSurvivor` warning and the NRE from `BurnEffectController.Start` (a body catches fire with a part of its model already
  destroyed; only the particles don't show) are now counted as known noise.

## 0.11.0

- **Tempest difficulty** (`[Tempest]`, new R2API.Difficulty dependency): above Monsoon (`ScalingValue` 3.5).
  Monster cap 60 (+10 per extra player), extra director credit with a gate before the cap and gradual resume, swarm
  (cheap monsters, waves of 10) and heavy phases, 3 s of invulnerability for new enemies, damage reduction and the same
  in max health per stage (20/40/55/70/90/99%), health, damage and elite curves, stay pressure, Primordial Teleporter
  only on stage 10 (other endings can be blocked), extra credit cut on low FPS, F8 summary and `DebugLog` for in-game
  validation. Lost Seer's Lenses remove 90% of the remaining health instead of killing. New command `ror2meeeee_portals`. Other difficulties are unchanged.
- Config: `General.ConfigVersion = 1`.
- **Status screen (F8)** redesigned, in a cycle: 1st press shows your stats in a bar above the skills (always up to
  date), 2nd shows every player with portrait, name and stats (and the Tempest summary on the host), 3rd closes. The
  run totals (damage dealt, healing, damage taken...) were removed, and with them the R2API Networking dependency.
- **Languages**: the mod now has an English base with a Brazilian Portuguese translation. With the game set to
  Português (Brasil), everything shows in Portuguese; in any other language, in English. This covers names and
  descriptions of items, artifacts, equipment and elites, chat messages, the F7/F8 screens, on-screen notices and
  markers, console command responses and the mod settings descriptions (Risk of Options). "Taxade" is a term of the
  mod and stays the same in both languages.
- Chat: the host's messages (Tithe, I.M.P. Contract, Grimoire, Tombstone, Drop, Multiplier cap, Progression summary and
  Greater Bazaar) arrive as mod text, and each player reads them in their own language. Player and item names inside
  them come as the host sees them.
- Config: the descriptions in the `.cfg` are now in English; keys and values don't change and existing configs keep
  working. The mod settings screen shows the descriptions in Portuguese when the game is in pt-BR. With an old Risk of
  Options without token support, the mod settings screen doesn't show up and the log warns about it (the mod keeps
  working through the `.cfg`).
- Logs (`LogOutput.log`) and the diagnostic tools (Probe, `ror2meeeee_status`, `ror2meeeee_effects`,
  `ror2meeeee_spend`) stay in Portuguese.
- README and CHANGELOG in English, with the Portuguese versions (`README.pt-BR.md` and `CHANGELOG.pt-BR.md`) and a link
  at the top to choose the language.

## 0.10.4

- Performance: **missiles** (`MissileProjectile`: ATG, Disposable Missile Launcher, Volatile aspect...) join the
  default `[ProjectileBudget]` list (15 per second, damage from those that don't fire added to the next one). In a
  4-player run, the Bazaar, with no enemies at all, had 675 missiles every 30 s. In an existing config, add
  `MissileProjectile` to `Prefabs` by hand.
- Performance: global player projectile cap (`[ProjectileBudget] GlobalMaxPerSecond`) from 80 down to **40** per
  second. In the same run, even with the per-type caps, the total went past 50/s with 1 to 3 monsters alive and the
  average frame reached 34 ms (p95 54 ms). The damage from projectiles that don't fire is added to the next ones of the
  same type. In an existing config, change `GlobalMaxPerSecond` by hand.
- Diagnostics: the `InventorySafekeepController` `NullReferenceException` at the end of the run (the game tries to give
  back each player's stored inventories after the run has already been torn down; no items are lost in a run in
  progress) is now counted as known noise, as are the one from `SetStateOnHurt.GetModifiedDuration` (enemy hit on the
  same frame it spawns, before the game fills in the stun controllers; fixing it would need a hook on every hit, too
  expensive for a rare case), the one from `ParticleSystemColorFromEffectData` (game effect created without going
  through the `EffectManager`; the particle just doesn't get its color), `Unable to SpawnEffect from prefab named
  'GenericHugeFootstepDust'` (game footstep effect that isn't in the effect catalog; only that name),
  `BaseAchievement: localUser is null!` (achievement checked without a local user during a multiplayer unlock), the one
  from `MasterSuicideOnTimer.OnDestroy` (temporary minion destroyed before the game finishes setting it up), the one
  from `BrotherUltUtils.EvaluatePattern` (Mithrix phase 2: the attack aims at whoever killed the enemy, and a turret or
  drone without a direction breaks the calculation; the attack just doesn't happen) and the one from the Captain's
  shock zone achievement (`CaptainSupplyDropFinaleAchievement`), which also shows up as `Failed at InvokeHandler,
  probably malformed packet!`.
- README: credits section with the people who develop and test the mod (SawCunha, Sleeping_tv_, Póker.the.wolf and
  Artie_saito) and the project's GitHub link (top of the page and credits).

## 0.10.3

- Performance: **Fireworks** (`FireworkProjectile`) joins the default `[ProjectileBudget]` list: at most 15 per second,
  all players combined, with the damage from those that don't fire added to the next one (same total damage). In a
  4-player run there were 902 missiles every 30 s before the game froze; `[LimitedCalc]` limits each launcher, but with
  the Multiplier there are many launchers. In an existing config, add `FireworkProjectile` to `Prefabs` by hand.
- Diagnostics: the gold flood warning (`[Diag] enxurrada de eventos de ouro`) now uses 40 events per second **per
  player**; with 3 players, each kill produces 3 events and a normal kill spike already triggered the warning.
- Game fixes (`[GameFixes]`): the **Dunestrider** dying without the "Center" point on its model threw a
  `NullReferenceException` in the death effect **before** destroying the model and the body, and the game didn't try
  again: the dead body stayed on the server until the end of the stage. Now the effect uses the body's position and the
  game's cleanup runs in full (one check per Dunestrider death).
  The **Mimic Chest** without the "Muzzle" point on its model broke when opening and when burrowing; when opening, the
  error skipped the line that marks the mimic as a "fake actor", but leaving the state unmarked it anyway: the counter
  went negative and the mimic's collision layer was wrong. Now the mod handles only that expected error and completes
  the skipped line.
- Diagnostics: these are now counted as known game noise: `CombatSquad has no living members. Triggering defeat...`
  (logged as an error when a boss/event group is defeated), `Unable to set gate state for ...` (map that references a
  passage missing from the navigation graph, when loading the stage) and `PickupDisplay bounds are zero!` (game pickup
  model with zero size, like HelpWake's), plus the network and character preview errors when someone leaves the lobby
  (`host id {0} invoked wrong io operation`, `host id {0} out of bound...`,
  `SurvivorMannequinSlotController.ApplyLoadoutToMannequinInstance`), the Extractor Unit's `NullReferenceException`
  without an animator (`ExtractLunge.OnEnter`; without an animator it doesn't enable its hitbox and doesn't steal items,
  so there's nothing to fix) and the Solus Amalgamator's (`DetatchState.OnEnter`: the game fires the detachments on the
  already dead body, without a model).

## 0.10.2

- **Freeze diagnostics**: the watchdog writes a "last sign of life" every 5 s to `BepInEx/ror2meeeee-heartbeat.txt`
  (stage, frame, monsters, kills, gold, trimmed blades, exceptions, RAM). Closing the game normally deletes the file; if
  it exists on the next launch, the log gets `[Vigia] a sessão anterior não fechou normalmente...` with the state of the
  match at the end. Reason: in a 3-player run the whole process froze (log, Unity and the watchdog itself stopped in the
  same second) and nothing was left to tell what was happening.
- Difficulty: the souls from the **Artifact of Soul** (one per monster killed, thousands per run) no longer get the
  health and damage Boost items; they die in one hit and the Boost only cost a stat recalculation for each one.
- Ghost Blades (`[GhostBlades]`): default cap from 15 down to **10** per second (the run that froze stayed at the cap,
  ~400 blades every 30 s). Applies to new configs; in an existing one, change `MaxPerSecond` by hand.
- **README rewritten for Thunderstore**: each artifact, item, equipment, elite and system in its own section, with what
  it does, how to use it and an example; summary table, commands and FAQ. Package description and website updated.
- Echo elite: if the echoes' projectile comes without a ghost (empty address and reference), the mod uses the game's
  `EchoHunterGhost` so the shots aren't invisible.
- **Greater Bazaar**: the item floating over the chest could be picked up (the game's `Start()` reset the `consumed`
  flag the mod set right after creating it). Now the display uses an infinite wait time, which the game doesn't reset,
  and is never granted. With the **Artifact of Command**, the chest drops the choice cube of the same tier again instead
  of the item directly; only the display ignores the artifact.
- Game fixes (`[GameFixes]`): the Void Barnacle called an empty spawn effect every time it spawned (1.5.0 left the
  effect's address blank), producing an error with a stack trace in the log per barnacle. The mod discards the empty
  effect first; no per-tick cost, one check per spawn.
  The Defective Unit threw a `NullReferenceException` when dying already without a model (the game read the model
  without checking); in that case the mod skips only the death effect part, which would have nowhere to show.
- `ror2meeeee_reload`: the help text said `Enabled` required a restart; it already applies immediately, between
  matches.
- Performance: the Tithe credit (`[Gold]`) no longer builds a diagnostic string on every gold earned (only when the
  flood is detected, once per stage); the Grimoire keeps the teleport effect instead of reloading it on every use.
- Load metric: the `[Carga]` line now lists the Awake steps that took 5 ms or more (registration of each
  item/artifact/elite, interface, config, each feature's `Enable` and the mod settings screen), to find what weighs on
  startup.
- Diagnostics: these are now counted as known game noise (not the mod's): Facepunch's `IndexOutOfRangeException` in the
  lobby list when someone joins, the duplicate `AddPlayer` when a player joins (`already a player at that
  playerControllerId` / `connection has already been set as ready`), the `SteamworksLobbyDataGenerator`
  `NullReferenceException` in the lobby, the one from `ViewableTag.OnDisable` when closing the game, `GenericPool:
  ReleasePools called on non-empty pool` on scene change and `AssignClientAuthority` from game minions. The "new error"
  warning shows the first line of the stack trace and ignores numbers (connection IDs) when deciding whether the error
  has been seen already.

## 0.10.1

- **Echo elite works again on game 1.5.0**: the patch moved the Echo assets from `RoR2/InDev/` to `RoR2/Junk/`
  (`edEcho.asset` and `EchoHunterProjectile.prefab`); in 0.10.0 the elite wasn't registered and the log showed
  `InvalidKeyException ... Key=RoR2/InDev/edEcho.asset`.

## 0.10.0

- **Greater Bazaar** (`[Bazaar]`, new, on; rewrite of BiggerBazaar 1.14.5): the Bazaar Between Time gets up to 6 chests
  with the item on display, bought with the gold left over from the stage (gold doesn't become XP when entering the
  bazaar portal) or with lunar coins (`PayWithLunarCoins`), and a pod next to Newt that trades lunar coins for gold.
  Weight, price, lunar price and stock per tier (10 purchases per chest), no purchase limit per player; trade limit at
  the pod; 10% chance of a temporary items chest.
  Compared to the original: price = small chest of the previous stage × multiplier (the original scaled twice), its own
  roll (doesn't touch the run's RNG), the purchased item drops on the ground and goes through the
  Multiplier/Communism, the Sale Star isn't wasted, the pod doesn't trigger Fireworks, the Recycler doesn't swap the
  displayed item, Command doesn't swap the purchased item, the mod's unique items stay out of the shop and the
  Captain's hack doesn't open the chests. No ShareSuite, no prices for the bazaar's original terminals and no buying
  straight into the inventory.
- **New elites** (`[Elites]`, new, on; rewrite of MoreElites 0.9.5 and BlightedElites 1.2.2): Volatile, Frenzied and
  Empowering (T1, with an Honor version), Blighted and Echo (T2). Game assets only: Frenzied is the game's yellow elite
  and Echo is the game's edEcho, which the director never rolled. Compared to the originals: missiles and teleport only
  on the host (they don't duplicate on clients), the Frenzied aspect used by a player doesn't teleport to (0,0,0), the
  Honor elite has the game's Honor stats, Volatile doesn't give a Brilliant Behemoth that never fires, Empowering doesn't
  erase a real Warbanner, the Echo's projectile is registered (it reached clients without a catalog entry) and the tiers
  come from R2API.Elites, not fixed indexes. New dependency: **R2API_Elites 1.1.3**.
- **Golden elite on AI** (`[GameFixes]`): Golden drones, allies and monsters dropped gold nuggets on every hit on
  non-players, without the cooldown or the chance the player has, until the match froze. Now they follow the player's
  rule (same fix as GildedChunkSpamFix; don't install both).
- **Artifact of Swarms** (`[GameFixes]`): Swarms copies spawned on top of the player or didn't spawn, because the game
  left their spawn distances at zero in the director's normal spawns. Now they get the original spawn's distances (same
  bug SwarmsFix fixes; don't install both).
- **Drop items screen** (F7): stopped creating new text styles on every draw (several per frame, one per item) and only
  looks for icons in the HUD when one is missing, instead of every 0.5 s. Less memory garbage with the screen open.
- **Call of the Horde**: the description said +25% per stack; the default is +50% (`Horde.QuantityPerStack = 0.5`).
- **Progression**: closing the game right after the run ends no longer loses the level or the CSV line (writing runs
  immediately when the game is closing).
- **Projectile cap** (`[ProjectileBudget]`): projectiles outside the `Prefabs` list cut by the global cap lost their
  damage; now it goes to the next shot of the same type, as already happened with the listed ones (total damage kept).

## 0.9.6

- **Projectile cap** (`[ProjectileBudget]`), new defaults measured in a 3-player run (~8k items, FPS dropping to ~11 in
  fights): `DaggerProjectile`, `EngiGrenadeProjectile` and `ShurikenProjectile` join the `Prefabs` list;
  `PerTypeMaxPerSecond` **15** (was 30) and `GlobalMaxPerSecond` **80** (was 300, which was never reached). Total damage
  stays the same: fewer projectiles, each one stronger.
- **Ghost Blades** (`[GhostBlades]`): `MaxPerSecond` **15** (was 30, which was always at the cap).
- **Particles** (`[Particles]`): explosions (`OmniExplosionVFXQuick`) leave the full exemption and get their own higher
  cap: new `Raised` list (default `OmniExplosionVFXQuick`) with `RaisedMaxPerSecond` **60**. Without a cap, they were
  the most frequent effect of the run (190k+). `Exempt` now has only the Mercenary strikes.
- **Game bug fixes** (`[GameFixes]`, new, on): errors that repeated hundreds of times per match (with a stack trace in
  the log, costing FPS) no longer happen:
  - **Runic Lens**: if the attacker dies before the meteor lands, the game repeated the error every tick; that hit's
    meteor is cancelled.
  - **Unstable Transmitter**: a body with the item and no inventory errored every frame; an orb whose owner had already
    died errored when touching enemies; an enemy without a model wasn't teleported and also lost bleed, stun and debuff
    (now it stays where it is and gets the rest of the effect).
  - **War Bonds**: an Engi turret with the item errored when dying.
  - **Equipment**: a body without an inventory with an equipment use request errored every tick.
- **Log**: the per-use lines for Ghor's Tome and Bundle of Fireworks (`[LimitedCalc]`, hundreds per run) only appear
  with `General.VerboseLogging`.
- Existing configs keep their saved values: to get the new defaults, delete the keys (or the `.cfg`) or adjust them on
  the mod settings screen.

## 0.9.5

- **Particles**: default cap reduced to **10/s per effect type** (was 20/s), with default exemptions for the Mercenary
  strikes (`BladeComboSwingEffect`) and explosions (`OmniExplosionVFXQuick`), which stay complete. Existing configs keep
  their saved `MaxPerSecond` and `Exempt` values.

## 0.9.4

- Updated to **Risk of Rain 2 1.5.0#1224** (Steam build 25921972): no more version mismatch warning in the log.
- **Vampiric Aegis** and **Devourer's Aegis** got their own 3D model (round shield with a boss: red and gold / purple
  with a Void glow), on the ground and in the logbook, replacing the Rose Buckler and Safer Spaces.
- **Rusty Nails / Void Spikes**: fixed 6 game asset keys that never loaded (Shard mark icon, nail tracer, detonation
  effect and fallback model). The `InvalidKeyException` at startup is gone.
- Load metric: the log gets `[Carga] Awake em X ms` (item/artifact registration and features/hooks separately).
- **Particles**: the local cut (`Disable`) became a **per-second cap for all effects** (`MaxPerSecond`, default 20/s
  per effect type), except those listed in `Exempt`. It also applies to what each player **sends** to the others, host
  included: whoever has the mod generates and transmits fewer effects. On stage change the log says how many effects the
  cap held back. The old `Disable` key is no longer used.
- **Multiplier**: `Multiplier.StackCap` now defaults to **150** and applies to any multiplied or shared item (before,
  the Limited calculation items were exempt). Hard cap: an item picked up with the stack already at the cap disappears,
  without entering the inventory, with a chat notice; sharing gives nothing to those already at the cap. Printer,
  cauldron and `give_item` don't go through this cap. Existing configs keep the saved value.
- **zzz cervo <3** got its own 3D model (brown deer with bulging eyes, open mouth and thin legs), replacing Paul's Goat
  Hoof.
- **Portal Battery** got its own 3D model (lying battery of worn metal, dark rings and a cyan band/terminal), replacing
  the game's Fuel Cell.

## 0.9.3

- **Particles**: with `Particles.CountSpawns` on, the list of the run's 20 most frequent effects (the same as
  `ror2meeeee_effects`) is printed automatically to the console and `LogOutput.log` on each new stage (`[Particles]
  efeitos mais frequentes após N fase(s)`). Runs on every player, not just the host.

## 0.9.2

- **Ghor's Tome** in the limited calculation (`[LimitedCalc.GhorsTome]`, default `Aggregate`, `Limit = 5`): the chance
  to drop a gold pack stays at most 4% × `Limit` (20%) and each pack is worth proportionally more (up to 5×), keeping the
  expected gold per kill with fewer packs on the map. `Clamp` only cuts the chance; `Off` = as in the game. Applies to all
  players, through the host's config. Log: `[LimitedCalc] GhorsTome Aggregate: pacote X -> Y`.

## 0.9.1

- **Pile of Rusty Nails** and **Pile of Void Spikes** got their own 3D model (pile of rusty / purple nails), on the
  ground and in the logbook, replacing the game's Tri-Tip.
- With `General.VerboseLogging` on, `LogOutput.log` records each nail, Void spike and detonation (`RustyNail: ...`).
- Custom icons for the **Artifact of Communism** in the lobby.

## 0.9.0

- New item **Pile of Rusty Nails** (white): 2% chance per hit (+0.1% per extra stack) to fire a nail dealing 10% of base
  damage (+0.1% per extra stack) that applies bleed. Console: `give_item Ror2MeeeeeRustyNailStack 1`.
- New item **Pile of Void Spikes** (void; corrupts the previous one): 2% chance (+0.5% per extra stack) to fire a spike
  dealing 10% of base damage (+0.5% per extra stack) that marks the target with Void Shard for 3 s. When it ends, a 250%
  detonation and a spike burst in a 12 m radius (150%, +50% per extra stack) that marks those hit. The chain is limited by
  `RustyNail.MaxChain` (default 2). Console: `give_item Ror2MeeeeeVoidSpikeStack 1`.
- Drop items screen (F7): the amount now respects `Drop.MaxPerCommand` (before, the screen let you choose 50, the server
  dropped 10 and the value didn't match) and the count drops immediately. New field to type the amount and −1, +1, +5
  and +10 buttons.
- Sharing became the **Artifact of Communism**: it only works with the artifact active in the lobby (the
  `Sharing.Enabled` option was removed) and now shares every type of item (lunar and void even without multiplying,
  scrap, expansion tiers, temporary and rented items the same way).

## 0.8.6

- Probe: three new columns per 30 s window, to find what weighs in long runs: `heal_events` (how many healing events,
  not just how much), `minions` (players' drones, turrets and other minions) and `player_items` (total copies of players'
  items). Also in the `[Probe] Fase ...` log and in `tools/probe-report.py`. The old CSV is archived automatically.
- Grimoire: when there's no active combat director (Bazaar, end of stage), the chat says there is no wave this time
  instead of saying the contract was charged. Names in chat and log without elite titles.
- New `[ProjectileBudget]`: cap on projectiles from players and minions, on the host. Per type (`Prefabs`, default
  StickyBomb, FireMeatBall (Molten Perforator) and LightningStake; `PerTypeMaxPerSecond` 30) and global
  (`GlobalMaxPerSecond` 300). Above the cap the projectile doesn't fire and its damage goes to the next one: fewer
  projectiles, each one stronger, same total damage. In long runs these items went past 11k projectiles in 30 s (~1 FPS)
  and the garbage produced caused freezes of several seconds. Monster projectiles don't change.
- Call of the Horde is no longer multiplied or shared (1 pickup = 1 copy, like Extra Barrel). With 3-4 players it reached
  60 copies and x43 enemies, with death cycles and FPS drops.
- Ghost Blades: default cap from 100 down to 30 per second (with 100 it was always at the cap, ~2.8k projectiles per
  30 s). Existing configs keep the saved value; adjust `GhostBlades.MaxPerSecond` if you want.

## 0.8.5

- Tombstone: whoever dies with an extra life from the game (Dio's Best Friend or the Void version) doesn't create a
  tombstone; the game already brings them back. Before, the tombstone appeared and vanished 2 s later. Log: `[Revive] X
  morreu com vida extra ... sem lápide`.

## 0.8.4

- Status screen (F8): failed to open ("Expected data type ULong, got data type Double"). The run stats are integers in
  the game; now they're read with the reader that accepts any type.

## 0.8.3

- Tombstone without visuals and invisible I.M.P. marker (since 0.7.0): the prefabs of the mod's effects (tombstone model,
  silent pulse, contract marker) were created in the menu without `DontDestroyOnLoad`; the first scene change destroyed
  them and sending was silently skipped. Now they survive scene changes.
- Heavy Heart/Bastion: max health + shield now stop at 1 billion. Above ~2.1 billion the game's health bar (which uses
  int) overflowed and threw OverflowException every frame (hundreds per minute in the log, a performance cost).
- New status screen (`Stats.MenuKey`, default **F8**): your character's current stats (health, shield, regen, damage,
  attack speed, crit, armor, speed, level) and run totals (damage dealt, minion damage, biggest hit, healing, damage
  taken, kills, deaths, items). The totals come from the game's own stats (those on the end-of-run screen). Shows all
  players: health, damage and run totals for each. The game only sends clients their own totals; the host sends everyone's
  every 2 s (only with a client connected). **New dependency: R2API Networking.**
- F7 (drop items) and F8 (status) screens: they close only with their own key (Esc no longer closes them) and don't open
  on top of each other.

## 0.8.2

- Fixed application order for items that change health, shield, speed and damage (before, each item had its own hook and
  the order depended on who registered first). A single hook per game point (`BodyHooks`), only on while someone holds
  the item:
  - Stats: Heavy Heart (health) -> Bastion (shield over the already multiplied health) -> Cervo (speed into damage) ->
    current health and shield readjusted once, on the host.
  - Damage taken: Aegis rolls the block -> Heavy Heart increases enemy damage (only if not blocked) -> game -> Aegis
    steals health if it blocked.
  Log: `[BodyHooks] hook de stats/dano ligado/desligado`.
- The Taxade tax is always the last step: calculated on the final max health and applied directly, without the Aegis
  block or the Heavy Heart increase. The Aegis also respects BypassBlock now (like the game's Tougher Times).
- Purchases in a fixed order (`PurchaseHub`, a single subscriber): Spend records the spending and Taxade comes last
  (before, the order depended on which feature turned on first, including when re-enabling from the mod settings
  screen). The revive's health cost also goes straight through, like the tax.
- Portal Battery: like the other items, the charge zone hooks only exist while someone has the item. Log:
  `[PortalBattery] hooks de carga ligados/desligados`.

## 0.8.1

- Heavy Heart: the damage taken increase only applies to damage coming from another character. Before, it also
  multiplied health costs (Void Cradle, Shrine of Blood): opening a Void chest left the player with 1 health, and the
  next hit killed. Fall damage and damage without an attacker are no longer increased either.
- Artifact of Taxade: opening a normal chest could kill (with Tax.Lethal off). The "leaves 1 health" limit was
  calculated before other effects multiplied the damage (Heavy Heart, Death Mark). Now the tax damage is NonLethal: the
  game itself guarantees 1 health, whatever the multiplier.

## 0.8.0

- New lunar item **Heavy Heart** (`[HeavyHeart]`): each stack multiplies max health by 1.5 and damage taken by 1.3
  (compounding, no cap: 10 stacks = health x57.7 and damage x13.8). When picking up the item, the extra health comes
  full (like the game's health items); when losing it, health is cut to the new maximum. Model: Pearl.
  Console: `give_item Ror2MeeeeeHeavyHeart 1`.
- New **Artifact of the Void** (`[VoidBounty]`): with it enabled in the lobby, each stage gets +5 Void chests (Void
  Cradle; `VoidBounty.ExtraChests`) and every Void chest rolls among all Void items (white 60%, green 30%, red 8%, boss
  2%). Price: all enemies spawn with +50% health. Needs the Survivors of the Void DLC in the run.
- New green item **Portal Battery** (`[PortalBattery]`): the teleporter and other charge zones charge +10% faster per
  stack, adding up the stacks of the whole team (no cap). Model: Fuel Cell.
  Console: `give_item Ror2MeeeeePortalBattery 1`.
- New red item **Bastion** (`[Bastion]`): shield of 40% of max health per stack (no cap) and the shield starts
  recharging 2 s after the last hit (the game waits 7 s), at the game's recharge speed. Model: Personal Shield
  Generator. Console: `give_item Ror2MeeeeeBastion 1`.
- Stolas' Grimoire: on maps with few nodes (Bazaar) the teleport "to the ally" landed on a random point of the map (the
  game uses a random point when it can't find a free node nearby). Now it only accepts a node within 10 m of the ally;
  otherwise, 2 m behind them, on the ground. The log shows the final distance.

## 0.7.1

- Tombstone without visuals in 0.7.0: the Overlay is always on again (the optimization that turned it off when idle was
  undone) and the Dio's Best Friend model is measured and adjusted to ~2.5 m tall (the pickup model's native scale isn't
  reliable). Log `[Revive] lápide visível nesta máquina: ...` the first time each session.
- All players need this same version (0.7.0 had several packages with different contents).

## 0.7.0

- zzz cervo <3: the hook on `CharacterBody.RecalculateStats` (every body, monsters included) only stays registered while
  someone has the item, as was already the case for Extra Barrel, the Fungi and the Aegises. Log: `[Cervo] hook de stats
  ligado/desligado`.
- New items **Vampiric Aegis** (green) and **Devourer's Aegis** (green void, corrupts the first), `[Aegis]`. Both give a
  5% chance to block (doesn't stack) and react to any block from the game (Tougher Times, Safer Spaces, parry, Captain's
  armor); immunity/invincibility doesn't count. Vampiric: steals 8% of the attacker's current health per stack (up to
  50%) and heals you. Devourer: Void wave that steals 3% of the current health of each enemy within 13 m (+3% and +2 m
  per stack, up to 20% and 30 m) and heals for the total. Models: Rose Buckler and Safer Spaces. Console:
  `give_item Ror2MeeeeeVampiricAegis 1` / `give_item Ror2MeeeeeDevourerAegis 1`.
- 3D models on the ground for the mod's items (before, all of them were the "mystery box"): Call of the Horde = War
  Horn, zzz cervo <3 = Paul's Goat Hoof, Extra Barrel = Brilliant Behemoth, I.M.P. Contract = Death Mark, Stolas'
  Grimoire = Ghor's Tome, Capitalism = Roll of Pennies. If a model doesn't load, it falls back to the mystery box.
- New items **Selfish Fungus** (green) and **Selfish Void Fungus** (green void, corrupts the first), `[SelfishFungus]`:
  they heal **always** (moving or standing still) only you and your drones/turrets/minions, wherever they are; they don't
  heal other players. Selfish: 5 health per second (grows 20% per level) +0.01% of max health per extra stack, with the
  Medkit glow every 3 s. Void: 15/s +0.5% per extra stack, with the Weeping Fungus purple aura. The base is flat (not %)
  so it doesn't scale with the health inflated by the multiplier. Nobody at full health is "healed" (no pointless
  healing events). Console: `give_item Ror2MeeeeeSelfishFungus 1` / `give_item Ror2MeeeeeSelfishFungusVoid 1`.
- Stolas' Grimoire: now teleports near the first living ally (in player list order). Alone, or with everyone else dead,
  it still goes to the teleporter; with no ally and no teleporter (Bazaar, Moon) the use isn't spent.
- I.M.P. Contract: the "new contract" notice and the "CONTRACT TARGET" marker (larger, font 26) show up for **all**
  players, not just the host. The marker disappears on its own in ~1 s when the target dies.
- Tombstone (Artifact of Resurrection): now has the Dio's Best Friend 3D model (2.5x) on the spot, and the "<name>'s
  TOMBSTONE" marker got larger (font 30) and shows up for **all** players, not just the host. The host resends the model
  as an effect every 1 s; on revive, it disappears in ~1 s. New build dependency: `Unity.Addressables`.
- Overlay (notices and markers) and drop items screen are off when there's nothing on screen: Unity no longer calls
  their `OnGUI`/`Update` every frame. The `Drop.MenuKey` key is now read in the game's update.
- Extra Barrel: the firing hooks (`BulletAttack.Fire`, `ProjectileManager.FireProjectile`, `OrbManager.AddOrb`) only
  stay registered while some body has the item. With no holder, no shot in the game (including monsters') goes through
  the mod. The log shows `[CanoExtra] hooks de disparo ligados/desligados` on each change.
- Package icon (r2modman) is now the same art as the in-game mod settings screen (`Assets/mod_icon.png`). Up to 0.6.5
  the zip carried an old `icon.png`, copied from version to version.

## 0.6.5

- New `[GhostBlades]`: cap on Ghost Blade (DLC4 Curse item) spawns/renewals per second (`GhostBlades.MaxPerSecond`,
  default 100; 0 = no cap). In long runs the game reached ~700 per second (20k to 47k projectiles per 30 s), which pushed
  native memory past 3 GB and the frame to 100-300 ms. Host only; `GhostBlades.Enabled` turns it off.
  `ror2meeeee_status` shows how many were skipped.

## 0.6.4

- Drop items screen: the image is now read from the icons the game's own HUD has already drawn (`ItemIcon`), because
  `pickupIconTexture`/`pickupIconSprite` came in null. If an image is still missing, the log lists the `ItemDef` icon
  fields (`[DropMenu] campos de ícone ...`) and the square shows the name.

## 0.6.3

- Drop items screen: the icons were still blank in 0.6.2. In this game version `pickupIconSprite` is obsolete and comes
  in null; the image now comes from `ItemDef.pickupIconTexture` (with `PickupDef.iconTexture` and the old sprites as
  fallback). Without an image, the square still shows the item name.

## 0.6.2

- Drop items screen: the icons were blank. Now they use `PickupDef.iconSprite` (the same as the game's UI), with
  `ItemDef.pickupIconSprite` as fallback; without an image, the square shows the item name in the tier color. The log
  records `[DropMenu] ícones: N de M itens com imagem` on first opening.

## 0.6.1

- Drop items: an item dropped with `Drop` and picked back up is no longer multiplied or shared again. Before, dropping 4
  and picking them back up with a x10 multiplier became 40 (duplication); now 4 come back. The record of dropped items
  resets every stage and every run.
- Drop items screen remade: grid of large icons (scaled by resolution), clicking an item shows its panel already
  selected (name, amount, big buttons), cursor released while open; arrows move, +/- amount, Enter drops.
  New command `ror2meeeee_drop_menu [name]` opens the screen with the item already chosen.

## 0.6.0

- New "Drop items" (`[Drop]`): command `ror2meeeee_drop [name] [amount]` and on-screen menu (key `Drop.MenuKey`, default
  F7; arrows choose, Enter drops). Drops items from your own inventory on the ground to give to an ally;
  `Drop.MaxPerCommand` (default 10).
- Stolas' Grimoire: cap on credit per use (`Grimoire.MaxCreditPerUse`, 600) and a lock by monsters alive
  (`Grimoire.MaxMonsters`, 80); with the lock the use still teleports, it just doesn't give credit.
- I.M.P. Contract: cap on extra gold per contract (`ImpContract.MaxGold`, 20000); the log shows how long the target
  lived.
- Diagnostics always on: `[Identidade]` and `[Config]` at startup (version, DLL build and configs), a `[Estágio]` summary
  per stage (duration, worst frame, kills, monster peak, gold/credit, exceptions, new game errors), `[Vigia]` for freezes
  (game not updating for 3 s or more), classification of Unity errors (known noise counted, new error logged once) and a
  flood detector with a call stack (gold and damage), once per stage.
- Probe: monsters (mean/peak), gold events, credit given/cut, Unity memory change and worst frame per phase (new CSV
  columns; the old one is kept aside) and `ALERTA` when the phase degrades (average frame more than 3x the run's best
  phase, +300 MB of native memory or a frame above 1 s), with the monsters alive by type.
- `ror2meeeee_status` shows the diagnostics (gold, monsters, exceptions, Grimoire uses and sampled cost of the gold
  hook).
- Resurrection: the log shows when the tombstone position was adjusted, each tombstone's lifetime and the time until the
  revive.

## 0.5.0

- Resurrection: the tombstone now shows on the screen of whoever runs the mod (banner and marker with name and distance;
  arrow on the edge when out of view).
- Resurrection: repeated pulses use a silent clone of the effect (the sound plays only once, when the tombstone is
  created); light column visible from afar.
- Resurrection: tombstone created at a safe position (ground below the death; without ground, at the nearest living
  ally), avoiding an unreachable tombstone off the map.
- I.M.P. Contract: the target gets a red on-screen marker and a warning banner.
- Tithe: new cap on credit per second (`Gold.MaxCreditPerSecond`, default 150) and a lock by monsters alive
  (`Gold.MaxMonsters`, default 80), against the snowball gold -> credit -> enemies -> lag.
- Tithe: the log records the call stack on the first flood of gold events (diagnostics).
- New console command `ror2meeeee_demo`: shows the test tombstone and target on screen for 15 s.

## 0.4.0

- Extra Barrel performance: extra orbs (Huntress arrows) cloned without reflection per shot and precomputed spread
  rotations.
- Resurrection: a dead player operating a drone (Remote Operation) now counts as dead and the tombstone revives; an
  operated drone doesn't revive anyone.
- Resurrection: diagnostic logs (artifact active per stage, tombstone created/removed, death without the artifact).
- A single hook on CharacterMaster.GiveMoney for Taxade and Tithe (there were two).
- Purchase (Taxade) and multiplication (Multiplier) logs sampled: the first ones of the run and then 1 every N.

## 0.3.0

- New items (Helluva Boss theme):
  - Lunar equipment "Stolas' Grimoire": teleports to the teleporter; each use gives credit to the director (wave of
    enemies).
  - Red item "I.M.P. Contract": the first elite or boss of the stage becomes a marked target; killing it pays extra gold
    (+100% per stack) and drops a green item.
  - "Artifact of Resurrection": whoever dies leaves a tombstone; an ally near it for 3 s revives the player with half
    health, paying half of their own health.
  - Placeholder icons (solid color).
- New red item "Extra Barrel": +1 shot in a spread on the primary weapon per stack (up to `MaxExtraShots`, default 5;
  global cap per frame). Not multiplied; turrets don't copy it. Zero cost per shot when nobody has the item.
- New green void item "zzz cervo <3": all bonus movement speed becomes damage (1:1, +50% conversion per extra stack).
  Drops from Void Cradles.
- New `[Cleanup]`: corpses disappear quickly (maximum 10, 2 s, even in view) using the game's system; fixes the game bug
  in the Lynx Totem (NullReferenceException every tick when the allies had already died).
- Artifact of Tithe: the director thanks you in chat for the gold received, accumulated, at most once every
  `Gold.ThankIntervalSeconds` (default 60 s) and at the end of the stage.
- Artifact of Taxade: the tax applies to every purchase paid with gold (chests, shrines, drones...) and never to other
  currencies.
- New defaults: almost all features on, x10 multiplier with lunar/void, difficulty +75% health/+50% damage, Horde
  0.5/20, Fireworks in Aggregate (20). Probe stays off.
- Mod settings screen descriptions rewritten: each `Enabled` explains what the feature does and what it depends on.
- Probe: `ram_unity_alloc_mb` column (allocated memory, instead of reserved); the old CSV is kept aside.
- Fix: when closing the game mid-run, the Spend CSV and the Probe's last window were lost; now they're written
  immediately.
- Internal: access to the combat directors centralized in `Common/Directors` (no feature references another).

## 0.2.0

- New: difficulty progression between runs (`[Progression]`), with a run summary in chat and a history CSV. Calculation
  and disk writing off the main thread.
- New: "Capitalism" items (white/green/red): relief from the Taxade tax and gold bonus (+10/+20/+30%); 1 of each per
  player, swapped in the chest for those who already have it.
- `[Tax]` is now on by default.
- Performance: `LimitedCalc` and `Particles` hooks only stay registered when there's something to do; icons without a
  copy in RAM; less allocation in Sharing, Spend and Probe; reflection cache in Difficulty.
- Fix: memory that grew per enemy during the run (Difficulty).

## 0.1.0

- First version: multiplier, limited calculation, difficulty, Call of the Horde item, Probe.
