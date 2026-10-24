# Ror2Meeeee

**Language:** English · [Português (Brasil)](README.pt-BR.md)

A **Risk of Rain 2** mod (1.5.0#1224, Steam build 25921972) made with BepInEx 5 and R2API. It makes the game more
chaotic: multiplies collected items, makes enemies stronger and more numerous, adds new items, an equipment and
artifacts, and includes tools to keep FPS up when the inventory explodes.

- **Current version:** 0.12.0 (`src/Ror2Meeeee.Core/ModInfo.cs`; history in [`package/CHANGELOG.md`](package/CHANGELOG.md))
- **GUID:** `com.sawcunha.ror2meeeee` · **Package:** `Sawcunha-Ror2Meeeee`
- **GitHub:** [https://github.com/SawCunha/ror2_meeeeeeee](https://github.com/SawCunha/ror2_meeeeeeee) · **Thunderstore:** [thunderstore.io/c/riskofrain2/p/SawCunhaOS/Ror2Meeeee](https://thunderstore.io/c/riskofrain2/p/SawCunhaOS/Ror2Meeeee/)
- **Multiplayer:** every player needs the same mod on the same version. Gameplay logic runs **only on the host**, so
  the host's `.cfg` options apply.

---

## Installation

### From Thunderstore (players)

The mod is published at [thunderstore.io/c/riskofrain2/p/SawCunhaOS/Ror2Meeeee](https://thunderstore.io/c/riskofrain2/p/SawCunhaOS/Ror2Meeeee/):
use *Install with Mod Manager* (r2modman or Thunderstore Mod Manager). The guide to each item, artifact and system,
with examples, is in [`package/README.md`](package/README.md), which is the mod's page on the site (in Portuguese:
[`package/README.pt-BR.md`](package/README.pt-BR.md)).

### From the local zip (r2modman)

1. Build or download `Sawcunha-Ror2Meeeee-<version>.zip` (see [Build](#build-and-development)).
2. In r2modman: **Settings → Import local mod** and pick the zip.
3. The dependencies (`package/manifest.json`) are installed along with it:
   BepInExPack, HookGenPatcher, RoR2BepInExPack 1.48.0 and the R2API Core, ContentManagement, Difficulty, Elites, Items and Language
   modules.

Optional: **Risk of Options** shows every option on an in-game mod settings screen.

### Manual installation

Copy `Ror2Meeeee.dll` and `Ror2Meeeee.Core.dll` to `BepInEx/plugins/Sawcunha-Ror2Meeeee/`. Use **only one folder**
with the plugin: two copies with the same GUID make BepInEx pick one of them, and `Core.dll` may come from the wrong
one.

---

## Languages

**English** base, with a **Brazilian Portuguese** translation. With the game set to Português (Brasil), items,
artifacts, elites, chat, the F7/F8 screens, notices, command responses and the mod settings screen show in Portuguese;
in any other language, in English. "Taxade" is a term of the mod and stays the same in both. The `.cfg` descriptions
are in English; logs and diagnostic tools stay in Portuguese.

---

## Items

Every item can be spawned from the console with `give_item <internal name> <amount>`.
Turning off the item's section in the config keeps the item in the game, but without effect.

| Item | Rarity | Effect | Internal name |
|---|---|---|---|
| **Call of the Horde** | White | +50% enemy spawn budget per stack (`Horde.QuantityPerStack`, cap in `Horde.MaxMultiplier`) | `Ror2MeeeeeHorde` |
| **Capitalism** | White | Taxade tax −20% and +10% of all gold earned. At most 1 per player | `Ror2MeeeeeTaxReliefWhite` |
| **Pile of Rusty Nails** | White | 2% chance per hit (+0.1% per stack) to fire a nail dealing 10% base damage (+0.1% per stack) that applies bleed | `Ror2MeeeeeRustyNailStack` |
| **Capitalism** | Green | Tax −30% and +20% gold. At most 1 per player | `Ror2MeeeeeTaxReliefGreen` |
| **Selfish Fungus** | Green | Heals 5 health/s always (+% of health per extra stack), only you and your drones, turrets and minions | `Ror2MeeeeeSelfishFungus` |
| **Vampiric Aegis** | Green | 5% chance to block a hit; on block, steals health from the attacker | `Ror2MeeeeeVampiricAegis` |
| **Portal Battery** | Green | Teleporter and other charge zones charge +10% faster per team stack | `Ror2MeeeeePortalBattery` |
| **Capitalism** | Red | Tax −50% and +30% gold. At most 1 per player | `Ror2MeeeeeTaxReliefRed` |
| **Extra Barrel** | Red | Each stack makes the primary weapon fire +1 shot in a spread (bullets, projectiles and arrows; not melee). Up to 5 extra. Not multiplied | `Ror2MeeeeeCanoExtra` |
| **I.M.P. Contract** | Red | The first elite or boss of the stage becomes a marked target; killing it pays extra gold (+100% per stack, up to `ImpContract.MaxGold`) and drops a green item | `Ror2MeeeeeImpContract` |
| **Bastion** | Red | Shield of 40% of max health per stack, which starts recharging 2 s after the last hit (7 s in the game) | `Ror2MeeeeeBastion` |
| **Heavy Heart** | Lunar | Each stack multiplies max health by 1.5 and damage taken from enemies by 1.3 (compounding) | `Ror2MeeeeeHeavyHeart` |
| **Pile of Void Spikes** | Void (white) | Corrupts the Nails. 2% chance (+0.5% per stack) of a spike dealing 10% base damage (+0.5% per stack) that marks the target for 3 s. When it ends: 250% detonation and a spike burst in a 12 m radius (150%, +50% per stack) that marks those hit, chaining up to `RustyNail.MaxChain` | `Ror2MeeeeeVoidSpikeStack` |
| **Selfish Void Fungus** | Void (green) | Corrupts the Selfish Fungus. Heals 15 health/s (+% of health per extra stack), only you and your minions | `Ror2MeeeeeSelfishFungusVoid` |
| **Devourer's Aegis** | Void (green) | Corrupts the Vampiric Aegis. 5% chance to block; on block, steals health from all nearby enemies | `Ror2MeeeeeDevourerAegis` |
| **zzz cervo <3** | Void (green) | *Haste makes waste.* All bonus movement speed becomes damage (1:1; +50% conversion per extra stack). Corrupts no item; drops from Void Cradles and Potentials | `Ror2MeeeeeCervo` |

The Capitalism items don't duplicate and stop dropping from chests for those who already have the same color.

### Equipment

| Equipment | Type | Effect | Internal name |
|---|---|---|---|
| **Stolas' Grimoire** | Lunar (60 s cooldown) | Teleports near the first living ally (alone: to the stage's teleporter). Each use gives credit to the director, which sends a wave of enemies | `Ror2MeeeeeStolasGrimoire` |

Console: `give_equipment Ror2MeeeeeStolasGrimoire`.

---

## Artifacts

They only take effect if enabled in the lobby.

| Artifact | Effect |
|---|---|
| **Communism** | Every item a player picks up also goes to the other living players: all types, from all expansions, including scrap, temporary and rented items. Equipment, curses (Hex tier) and the mod's unique items are not shared. No config option: the artifact is the switch |
| **Tithe** | Part of all gold earned (`Gold.Ratio`, 10%) becomes credit for the director to spawn more enemies, and the director thanks you in chat every `ThankIntervalSeconds` (60 s). Limits: `MaxCreditPerSecond` (150) and `MaxMonsters` (80 alive) |
| **Taxade** | Every purchase paid with gold (chest, shrine, drone...) costs health: 30% of max health by default (`Tax.Percent`). Purchases with other currencies don't pay. With `Tax.Lethal = false` (default), the tax never kills: it leaves 1 health. The Capitalism items reduce the tax |
| **Void** | +5 Void chests per stage (`VoidBounty.ExtraChests`), each rolling among all Void items. Enemies with +50% health |
| **Resurrection** | Whoever dies leaves a tombstone. An ally who stays within 4 m of it for 3 s revives the player with half health, paying half of their own health. Only makes sense in multiplayer; `ror2meeeee_demo` shows the on-screen markers |

---

## Gameplay systems

### Pickup multiplier (`[Multiplier]`)

Each eligible item collected yields **N copies** (`Value`, default **10**).

- Lunar and Void items also multiply (`IncludeLunar` and `IncludeVoid`, on by default).
- `StackCap` (default **150**) is the cap on copies of the same item per player, for any item: multiplying and sharing
  stop at the cap, and an item picked up with the stack already at the cap **disappears** (chat notice). Printer,
  cauldron and `give_item` don't go through this cap. 0 = no cap.
- These don't multiply: starting items, conversions (Void, scrap), items from monsters, temporary items and Extra
  Barrel.
- Dead players don't get the extra copies.

### Difficulty (`[Difficulty]`)

Makes enemies stronger and more numerous. Everything runs on the host.

- **Fixed bonus:** `MonsterHealthBonus` (+75% health) and `MonsterDamageBonus` (+50% damage), applied with the game's
  own `BoostHp`/`BoostDamage` items, once per enemy.
- **Single factor** (`Factor`, 1.0 = no change), split by separate gains:
  - `QuantityGain` (2): more credits for the director, i.e. more enemies;
  - `PaceGain` (0.5): difficulty rises faster over time;
  - `HealthGain` (1) and `DamageGain` (0.5): health and damage.
- Also applies the effect of the **Call of the Horde** item.

### Tempest difficulty (`[Tempest]`)

New lobby difficulty, above Monsoon (`ScalingValue` 3.5). Everything runs on the host and only in runs with it.

- **Hordes:** the cap of monsters alive goes from 40 to 60 (+10 per extra player, counted at the start of the run). The
  director gets extra credit that grows with time and stages, stops 5 slots before the cap and comes back gradually (no burst).
- **Swarm and heavy phases:** each stage starts with 45 s of swarm (the director uses cheap monsters, waves of 10) and
  alternates with 75 s of heavy phase (expensive monsters and elites, as in the game).
- **Protection:** every new enemy is invulnerable for 5 s (damage, knockback, damage over time and on-hit effects), with
  the game's golden glow. Once per enemy, and again each time it revives. It still moves and attacks.
- **Extra lives:** each normal enemy gets 1 Dio's Best Friend (`ExtraLivesNormal`) and each elite gets 2
  (`ExtraLivesElite`). Bosses, summons and temporary minions get none.
- **Damage reduction and health per stage** (`DamageReductionSteps`): 20/40/55/70/90/99% from stage 2 on, and the same
  value in max health (hidden item, +1% per stack). Stacks with the `[Difficulty]` bonus.
- **With the Multiplier on** (x M): the damage that gets through the reduction is divided by `M^MultiplierReductionExponent`
  (default 0.5; with x10, stage 2 goes from 20% to 75%, cap 99%), and the curves' health and damage are multiplied by
  the same factor (x3.16 with x10), also from stage 2 on.
- **Curves:** health, damage and elite frequency grow per minute and per stage (`<Axis>PerMinute`, `PerStage`, `Max`).
  Staying long in a stage raises the pressure (frozen while the teleporter charges).
- **Ending on stage 10** (`FinalStage`): before it the Primordial Teleporter leads to the next loop. The other endings
  can be blocked until then (`Block*UntilFinalStage`, off by default).
- **Lost Seer's Lenses:** they don't kill instantly in Tempest; a proc (same chance as the game) removes 90% of the
  enemy's remaining health instead. Bosses are still immune.
- **Performance:** with the average frame above 33 ms for 10 s the extra credit stops, then comes back gradually.
- **Validation:** `DebugLog` (toggles immediately) logs stacks given, health before/after, damage before/after the
  reduction, hits ignored by the protection and the director state every 10 s. The F8 screen shows a summary on the host.
- `ror2meeeee_portals` lists the stage's portals. Specification (Portuguese): [`docs/especificacao_mod_risk_of_rain_2.md`](docs/especificacao_mod_risk_of_rain_2.md).

### Progression between runs (`[Progression]`)

At the end of each run, the group gets a score based on speed, deaths, damage taken and kills. The reference targets
are in the `Par*` options. The summary shows up in chat (`ShowInChat`).

- A victory with a score from `RaiseAt` (0.5) goes up one level; a defeat goes down one.
- Each level changes the director's spawn budget for the next run by `Step` (15%), up to `MaxMultiplier` (3×).
- A run abandoned midway doesn't count.
- The level is in `BepInEx/config/ror2meeeee-progress.txt` and the history in `BepInEx/ror2meeeee-runs.csv`.
- The calculation runs off the main thread.

### Greater Bazaar (`[Bazaar]`)

The Bazaar Between Time becomes a shop. Rewrite of BiggerBazaar 1.14.5 (MagnusMagnuson). Everything runs on the host.

- When entering the bazaar portal, gold does **not** become XP (`KeepMoney`): it stays to be spent. Whatever is left
  becomes XP when leaving.
- Up to 6 chests (`Chests`) with the item on display. Each chest's tier is rolled by weight (`<Tier>Weight`; default
  55% white, 30% green, 15% red; boss, lunar and equipment off). Gold price = `<Tier>Cost` small chests of the previous
  stage (1 / 2 / 16); with `PayWithLunarCoins`, `<Tier>LunarCost` lunar coins.
- Each chest sells `<Tier>Stock` times (10 on every tier; -1 = no limit); no purchase limit per player.
- `TemporaryChance` (10%): the chest sells `TemporaryAmount` (5) temporary copies per purchase.
- Pod next to Newt: 1 lunar coin = `LunarCoinGold` (50, scales with difficulty), up to `MaxExchangesPerPlayer` (3)
  times.
- The purchased item drops on the ground and goes through the Multiplier and Communism like any pickup; with the
  Artifact of Command, the choice cube of the same tier drops. The displayed item is just a showcase (can't be picked
  up). The mod's unique items don't appear in the shop; the Captain's hack doesn't open the chests.

### New elites (`[Elites]`)

Five more elites in the director's roll. Rewrite of MoreElites 0.9.5 (Nuxlar) and BlightedElites 1.2.2 (Moffein), using
only game assets. Everything runs on the host; each elite has its own key (`Volatile`, `Frenzied`, `Empowering`,
`Blighted`, `Echo`), which applies to the next spawns.

- **Volatile** (T1): every hit explodes (like a Brilliant Behemoth) and fires a missile every 1 to 5 s.
- **Frenzied** (T1): +2 base speed and +50% attack speed (the yellow elite the game has and doesn't use); every 10 s it
  teleports close to its target. The aspect used by a player teleports 25 m in the direction of movement.
- **Empowering** (T1): 25 m Warbanner for allies. On clients, the ring shows with the Celestine's visuals.
- **Blighted** (T2): two different random T1 affixes (Blazing, Glacial, Overloaded, Mending). 16× health, 4× damage.
- **Echo** (T2): the Echo elite that already exists in the game, but which it never rolls: 2 echoes with 10% health
  that fire homing projectiles.

Stats equal to the game's elites (T1, Honor T1 and Malachite). The aspects drop with the game's chance (1 in 4000).

### Drop items (`[Drop]`)

Drops items from your inventory on the ground to give to an ally.

- **Menu:** **F7** key (`Drop.MenuKey`). Click the icon, adjust the amount (field, −1/+1/+5/+10, arrows or +/−) and
  confirm with **DROP** or Enter.
- **Console:** `ror2meeeee_drop [name] [amount]` (with no arguments, lists what can be dropped) and
  `ror2meeeee_drop_menu [name]`.
- At most `MaxPerCommand` (10) at a time.

### Status screen (F8)

Press once: your stats (health, speed, damage, regen, armor, level, attack speed) in a bar above the skills,
always up to date. Press again: every player with portrait and the same stats (and the Tempest summary on the host).
Third press closes. Key in `Stats.MenuKey`.

---

## Performance

With the multiplier at 10×, the inventory grows very fast. These systems hold FPS up without removing the items'
effect:

| Section | What it does | Default |
|---|---|---|
| `[LimitedCalc.BustlingFungus]` | Treats Bustling Fungus as if it had at most `Limit` stacks (`Mode = Clamp`) | `Off`, limit 10 |
| `[LimitedCalc.BundleOfFireworks]` | `Aggregate`: fewer missiles, each one stronger, same total damage | `Aggregate`, limit 20 |
| `[LimitedCalc.GhorsTome]` | `Aggregate`: fewer gold packs on the ground, each worth more, same expected gold | `Aggregate`, limit 5 |
| `[ProjectileBudget]` | Cap on projectiles from players and minions (Sticky Bomb, Molten Perforator, stakes, Fireworks, missiles...): fewer projectiles, each one stronger, same total damage. Acolyte wights: at most 3 per haunted kill, with the damage of the cut ones. Monster projectiles don't change | 15/s per type, 40/s total, 3 wights per kill |
| `[GhostBlades]` | Limits how many Ghost Blades (DLC4 Curse item) the game spawns or renews per second | 10/s |
| `[Cleanup]` | Corpses disappear quickly, even in view, and fixes a game bug in the Lynx Totem that produced hundreds of exceptions | 10 bodies, 2 s |
| `[GameFixes]` | Fixes game errors that repeated hundreds of times per match and cost FPS: Runic Lens, Unstable Transmitter, War Bonds and equipment on bodies without an inventory; also fixes the Swarms artifact and the Golden drones/monsters that froze the match dropping gold nuggets (same fix as GildedChunkSpamFix) | on |
| `[QoL]` | Items and dead drones that fall off the map come back to the nearest ground; lava deals 2% to drones and turrets (like monsters) instead of 10%; Engi bubble, REX Sonic Boom, Loader hook and Railgunner scope don't put you in combat. Idea from WolfoQoL_Gameplay | on |
| `[Particles]` | Per-second cap (`MaxPerSecond`) for **every** type of effect, except those in the `Exempt` list; those in the `Raised` list have their own higher cap (`RaisedMaxPerSecond`). Applies to what you show **and to what you send** to others (host and clients with the mod). Doesn't change damage | 10/s per type; explosions 60/s; exception: Mercenary strikes |

---

## Configuration

The file is `BepInEx/config/com.sawcunha.ror2meeeee.cfg` (only the host needs to adjust it), or the Risk of Options
screen.

- The defaults are ready to play: almost everything on. Only the Probe (measurement) comes off.
- Each section has an `Enabled` explaining what it does. `Enabled` applies between matches.
- The other values can be reloaded with `ror2meeeee_reload`.

### `[General]`

| Option | Default | Use |
|---|---|---|
| `VerboseLogging` | `false` | Extra debug logs in `LogOutput.log` (e.g. each RustyNail nail, spike and detonation). Costs FPS |
| `MaxFailures` | 50 (minimum) | How many errors a feature can have before it turns itself off |
| `ProbeEnabled` | `false` | Turns on the measurement Probe |
| `ProbeAutoWindowSeconds` | 30 | Size of the Probe's automatic window |

---

## Console commands

Open the console with **Ctrl+Alt+`** (or F2, depending on the keyboard).

| Command | What it does |
|---|---|
| `ror2meeeee_status` | Lists the mod's features, failures and hooks |
| `ror2meeeee_portals` | Lists the current stage's portal spawners (card, chance, conditions) |
| `ror2meeeee_reload` | Reloads the `.cfg` (`Enabled` already applies immediately, between matches) |
| `ror2meeeee_drop [name] [amount]` | Drops items from your inventory |
| `ror2meeeee_drop_menu [name]` | Opens the drop items screen |
| `ror2meeeee_demo` | Shows the test markers (tombstone and target) for 15 s |
| `ror2meeeee_progress` / `ror2meeeee_progress_reset` | Shows or resets the progression level between runs |
| `ror2meeeee_spend` | Purchases and gold spent per player in the run |
| `ror2meeeee_effects [top]` | Most frequent visual effects (needs `Particles.CountSpawns`); with it on, the list is also printed to the console/log on each new stage |
| `ror2meeeee_probe_label <name>` | Labels the Probe's following automatic windows |
| `ror2meeeee_probe_begin <name>` / `ror2meeeee_probe_end` | Starts and ends a measurement phase (writes CSV) |

---

## Diagnostics and measurement

- **Always on:** `[Identidade]`, `[Carga]` (time of the mod's `Awake`), `[Config]`, `[Estágio]`, `[Vigia]` lines and
  new game errors in `LogOutput.log` (log lines are in Portuguese).
- **Isolated failure:** each feature has a kill-switch. If it hits `MaxFailures` errors, it turns itself off and the
  rest of the mod keeps going. `ror2meeeee_status` shows which one fell.
- **Spending per player** (`[Spend]`): counts purchases and gold spent and writes `BepInEx/ror2meeeee-spend.csv` at the
  end of the run.
- **Probe** (`[General] ProbeEnabled`): measures FPS, monsters, healing events, minions and items per 30 s window, with
  degradation alerts. Writes a CSV to `BepInEx/`; `tools/probe-report.py` summarizes the file.
- Useful lines in the log: `[Multiplier]`, `[Difficulty]`, `[Progression]`, `[Tax]`, `[Probe]`, `[ProjectileBudget]`,
  `[LimitedCalc]`.

---

## Credits

Developed and tested by:

- **SawCunha**
- **Sleeping_tv_**
- **Póker.the.wolf**
- **Artie_saito**

---

## Build and development

### Requirements

- .NET SDK 8 (the plugin compiles to `netstandard2.1`; tests run on `net8.0`).
- Risk of Rain 2 installed. The project references the local DLLs from `Risk of Rain 2_Data/Managed`, not the
  `RiskOfRain2.GameLibs` NuGet.
- An r2modman profile with the dependencies installed (the R2API and MMHOOK DLLs come from there).

The paths are in [`Directory.Build.props`](Directory.Build.props) and can be overridden with `-p:` or an environment
variable:

| Property | Default |
|---|---|
| `GameDir` | `~/.steam/debian-installation/steamapps/common/Risk of Rain 2` |
| `R2modmanProfile` | `~/.config/r2modmanPlus-local/RiskOfRain2/profiles/Default` |

### Commands

```bash
dotnet build -c Debug     # builds the plugin and Core
dotnet test               # Ror2Meeeee.Core tests (xunit.v3)
tools/pack.sh             # Release build + dist/Sawcunha-Ror2Meeeee-<version>.zip for r2modman
```

`pack.sh` checks that the version in `ModInfo.cs` matches `package/manifest.json` before packaging.

### Releasing a version

1. Update the version in `src/Ror2Meeeee.Core/ModInfo.cs` and in `package/manifest.json`.
2. Add the entry to `package/CHANGELOG.md` (English) and `package/CHANGELOG.pt-BR.md` and, if something changed for the
   player, to `package/README.md` and `package/README.pt-BR.md`.
3. Run `dotnet test` and `tools/pack.sh`.

### Structure

```
src/
  Ror2Meeeee.Core/     pure logic (rules, curves, multiplier pipeline). Never references the game; 100% testable
  Ror2Meeeee/          the BepInEx plugin
    Plugin.cs          entry point: registers items, artifacts and features
    Common/            Feature base (kill-switch + isolated failure), Cfg, Loc (languages), ModItem, diagnostics, F7/F8 screens
    Features/<Name>/   one folder per mechanic (MMHOOK On.* hooks)
    Debug/             console commands and Probe
    Assets/            icons embedded in the DLL (artifacts/ = artifact icons, 128×128)
tests/Ror2Meeeee.Core.Tests/
package/               manifest.json, README and CHANGELOG (English + .pt-BR.md) of the Thunderstore/r2modman package
tools/                 pack.sh, probe-report.py
docs/                  original art and test and measurement reports
_bmad-output/          planning (PRD, GDD, architecture, stories)
```

### Code rules

The full rules are in [`_bmad-output/project-context.md`](_bmad-output/project-context.md) and
[`_bmad-output/game-architecture.md`](_bmad-output/game-architecture.md). The ones that break most:

- Every `On.*` hook calls `orig`.
- Gameplay logic only with `NetworkServer.active` (host).
- No per-tick allocation on the hot path.
- `Ror2Meeeee.Core` never references the game.
- Game behavior is confirmed by decompiling `RoR2.dll` (e.g. `ilspycmd`) and measuring, not by assumption.
- New items and artifacts use `ModItem.Register` and `TitheArtifact.LoadIcon` (icons embedded as `EmbeddedResource`).
- Player-visible text always in both languages, through `Loc` (`Common/Loc.cs`): `Loc.Add(token, en, pt)` for game
  tokens, `Loc.T(en, pt)` for local screens, `Loc.Broadcast(token, args)` for chat and `Cfg.Bind(..., en, pt)` for
  config. Logs stay in Portuguese.
