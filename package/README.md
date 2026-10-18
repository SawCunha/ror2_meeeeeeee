# Ror2Meeeee

**Language:** English · [Português (Brasil)](https://github.com/SawCunha/ror2_meeeeeeee/blob/main/package/README.pt-BR.md)

A **Risk of Rain 2 (1.5.0)** mod that makes the run more chaotic: every item counts for ten, enemies get stronger and
more numerous, and new artifacts, items, an equipment and elites join the game. It also brings tools to keep FPS up
when the inventory explodes.

**Multiplayer:** every player needs the mod, on the **same version**. The **host** is in charge: the gameplay rules run
on their machine and their `.cfg` options apply.

**Source code, bugs and suggestions:** [https://github.com/SawCunha/ror2_meeeeeeee](https://github.com/SawCunha/ror2_meeeeeeee)

## Installation

1. Install with **r2modman** or **Thunderstore Mod Manager** (*Install with Mod Manager* button on this page). The
   dependencies (BepInEx, R2API, HookGenPatcher) come along.
2. Launch the game through the manager. The defaults are ready to play; nothing needs to be configured.
3. Optional: install **Risk of Options** to change the options on the in-game mod settings screen.

## Languages

The mod's texts (items, artifacts, elites, chat, F7/F8 screens and mod settings options) follow the language chosen in
the game: **Portuguese** when the game is set to Português (Brasil) and **English** for any other language. Each player
sees the chat in their own language. "Taxade" is a term of the mod and stays the same in both. The `.cfg` file has its
descriptions in English.

## Summary

| Type | Content |
|---|---|
| Artifacts | Communism, Tithe, Taxade, Resurrection, Void |
| White items | Call of the Horde, Capitalism (white), Pile of Rusty Nails |
| Green items | Capitalism (green), Selfish Fungus, Vampiric Aegis, Portal Battery |
| Red items | Capitalism (red), Extra Barrel, I.M.P. Contract, Bastion |
| Void items | zzz cervo <3, Selfish Void Fungus, Devourer's Aegis, Pile of Void Spikes |
| Lunar item | Heavy Heart |
| Lunar equipment | Stolas' Grimoire |
| Elites | Volatile, Frenzied, Empowering, Blighted, Echo |
| Systems | Item multiplier, Difficulty, Progression between runs, Greater Bazaar, Limited calculation |
| Screens | Drop items (F7), Run status (F8) |
| Performance | Caps on particles, projectiles and Ghost Blades, corpse cleanup, game fixes |

Names in brackets, like `[Multiplier]`, are sections of the configuration file (see *Configuration* at the end).

---

## Tempest difficulty (`[Tempest]`)

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
- `ror2meeeee_portals` lists the stage's portals.

## Artifacts

Artifacts are turned on in the **lobby**, on the artifact selection screen, before the run starts. When off, they
change nothing.

### Artifact of Communism

**What it does:** every item someone picks up also goes to the other living players. Works for items of every type and
expansion, including scrap, temporary and rented items. Equipment and the mod's unique items (Capitalism, Extra Barrel
and Call of the Horde) are not shared.

**How to use:** turn the artifact on in the lobby. It has no options.

**Example:** you open a chest and grab a Soldier's Syringe. Every living ally also gets the Syringe, with no need to
split the chest.

### Artifact of Tithe (`[Gold]`)

**What it does:** part of all gold the players earn becomes credit for the director to spawn more enemies. Every now
and then the director thanks you in chat for how much it received.

**How to use:** turn the artifact on in the lobby. Options:
- `Ratio`: how much of the gold becomes credit (default `0.1`, i.e. 10%).
- `ThankIntervalSeconds`: interval between chat messages (default 60 s; `0` = no messages).
- `MaxCreditPerSecond` (150) and `MaxMonsters` (80): locks so the match doesn't turn into lag; `0` turns each one off.

**Example:** an enemy gives 50 gold. With `Ratio = 0.1`, the director gets 5 credit and spends it on more enemies. With
80 or more monsters alive, the Tithe stops giving credit until the number drops.

### Artifact of Taxade (`[Tax]`)

**What it does:** every purchase paid with **gold** (chest, shrine, drone, printer...) costs health. Purchases with
lunar coins or with items are not taxed. The **Capitalism** items reduce this tax.

**How to use:** turn the artifact on in the lobby. Options:
- `Percent`: health lost per purchase, as % of max health (default 30).
- `Lethal`: off (default), the tax never kills and leaves you with 1 health; on, buying with low health can kill.

**Example:** with 500 max health, opening a chest takes 150 health. With the white Capitalism, the tax drops by 20% and
the same chest takes 120.

### Artifact of Resurrection (`[Revive]`)

**What it does:** whoever dies leaves a tombstone on the ground. An ally who stays near it for a few seconds revives
the player with half health and pays with half of their own health. The tombstone shows on screen with a marker and a
notice. Only makes sense in multiplayer.

**How to use:** turn the artifact on in the lobby. Options:
- `Radius`: distance from the tombstone, in meters (default 4).
- `RequiredSeconds`: consecutive time near it (default 3 s). Walking away resets the count.

**Example:** your friend dies mid-fight. You run to the tombstone, stay 3 s within 4 m of it and they come back with
half health, while you lose half of yours. The `ror2meeeee_demo` command shows how the tombstone looks.

### Artifact of the Void (`[VoidBounty]`)

**What it does:** each stage gets extra Void chests, and all of them roll among **all** Void items. In exchange,
enemies have +50% health.

**How to use:** turn the artifact on in the lobby. Option `ExtraChests`: how many extra chests per stage (default 5).

**Example:** on the first stage, 5 Void chests appear besides the normal ones. One may give a Lysate Cell, another a
Plasma Shrimp, and so on.

---

## Items

The items show up in chests and drops like the game's own. Each one has an `Enabled` key in its `.cfg` section: when
off, the item still exists but does nothing. The `give_item` / `give_equipment` commands in the examples need the
**DebugToolkit** mod and are only for testing.

### Call of the Horde (white)

**What it does:** each stack the team has increases the number of enemies. Needs *Difficulty* on. Not multiplied or
shared.

**How to use:** pick up the item. Options in `[Horde]`: `QuantityPerStack` (default `0.5`, +50% per stack) and
`MaxMultiplier` (cap, default 20×).

**Example:** the team has 4 stacks combined: 1 + 4 × 0.5 = **3× more enemies**.
Test: `give_item Ror2MeeeeeHorde 1`.

### Capitalism (white, green and red)

**What it does:** each one reduces the Taxade tax and increases all gold you earn, even without the artifact:

| Item | Reduces the tax by | Extra gold |
|---|---|---|
| White | 20% | +10% |
| Green | 30% | +20% |
| Red | 50% | +30% |

At most 1 of each per player: they don't multiply and no longer drop from chests for those who already have them.

**How to use:** pick up the item. Needs `[Tax] Enabled` on.

**Example:** with all three, the tax disappears (20 + 30 + 50 = 100%) and you get +60% gold: an enemy that gave 100
gold now gives 160.
Test: `give_item Ror2MeeeeeTaxReliefWhite 1` (or `...Green`, `...Red`).

### Pile of Rusty Nails (white)

**What it does:** each hit has a chance to fire a nail that causes bleeding.

**How to use:** pick up the item. 2% chance with 1 stack (+0.1% per extra stack). The nail deals 10% base damage
(+0.1% per extra stack).

**Example:** with 11 stacks, the chance is 3% per hit. A weapon that hits very fast, like Commando's, fires nails
often.
Test: `give_item Ror2MeeeeeRustyNailStack 1`.

### Pile of Void Spikes (Void, white)

**What it does:** Void version of the Nails. Each hit has a chance to fire a spike that **marks** the target. After
3 s, the marked target suffers a detonation and releases spikes around it (12 m radius), which mark nearby enemies.
Corrupts all Piles of Rusty Nails.

**How to use:** pick up the item. 2% chance (+0.5% per extra stack); spike of 10% base damage (+0.5% per extra stack);
250% detonation; 150% spike burst (+50% per extra stack). Option `[RustyNail] MaxChain`: how many times the burst can
mark again in a chain (default 2; `0` = the burst deals damage but doesn't mark).

**Example:** a spike marks an enemy in the middle of a group; 3 s later it explodes and the spikes mark another 5
enemies, which also explode right after.
Test: `give_item Ror2MeeeeeVoidSpikeStack 1`.

### Selfish Fungus (green)

**What it does:** regenerates 5 health per second, **always** (standing still or not), only on you and your drones,
turrets and minions. The healing scales with level, like the game's regeneration.

**How to use:** pick up the item. Each extra stack adds +0.01% of max health per second.

**Example:** unlike Bustling Fungus, you don't need to stand still: the healing keeps going while you run.
Test: `give_item Ror2MeeeeeSelfishFungus 1`.

### Selfish Void Fungus (Void, green)

**What it does:** like the Selfish Fungus, but with 15 health per second and +0.5% of max health per second per extra
stack. Corrupts all Selfish Fungi.

**Example:** with 10 stacks and 1,000 max health: 15 + 9 × 0.5% × 1,000 = **60 health per second**, at level 1.
Test: `give_item Ror2MeeeeeSelfishFungusVoid 1`.

### Vampiric Aegis (green)

**What it does:** 5% chance to block a hit. On block, steals 8% of the attacker's current health per stack (up to 50%)
and heals you with it.

**How to use:** pick up the item. The block chance is fixed; stacks increase the steal.

**Example:** with 3 stacks, a block against a golem with 2,000 health takes 24% of it (480) and heals you for 480.
Test: `give_item Ror2MeeeeeVampiricAegis 1`.

### Devourer's Aegis (Void, green)

**What it does:** 5% chance to block. On block, releases a Void wave that steals 3% of the current health of **every**
enemy within 13 m (+3% and +2 m per extra stack, up to 20% and 30 m) and heals you for the total. Corrupts all Vampiric
Aegises.

**Example:** surrounded by 10 enemies, a block drains a bit from each one and gives the total back as health.
Test: `give_item Ror2MeeeeeDevourerAegis 1`.

### Portal Battery (green)

**What it does:** the teleporter and other charge zones charge 10% faster per stack. Stacks from the whole team add up.

**Example:** the team has 5 stacks in total: the teleporter charges **50% faster** for everyone.
Test: `give_item Ror2MeeeeePortalBattery 1`.

### Extra Barrel (red)

**What it does:** each stack makes the primary weapon fire **+1 shot** in a spread. Works for bullets, projectiles and
arrows; melee attacks don't change. Not multiplied.

**How to use:** pick up the item. Options in `[CanoExtra]`: `MaxExtraShots` (cap on extra shots, default 5, protects
FPS) and `FanDegrees` (spread angle, default 5°).

**Example:** with 3 stacks, each Huntress shot becomes 4 arrows: the original and others at ±5° and ±10°.
Test: `give_item Ror2MeeeeeCanoExtra 1`.

### I.M.P. Contract (red)

**What it does:** at the start of each stage, the first elite or boss becomes the contract target and gets marked.
Killing it pays double gold (+100% per stack) and drops a green item.

**How to use:** pick up the item and look for the marked target. Option `[ImpContract] MaxGold`: cap on the extra gold
(default 20,000; `0` = no cap).

**Example:** the target would be worth 300 gold. With 1 stack, you get 600 and a green item drops on the ground.
Test: `give_item Ror2MeeeeeImpContract 1`.

### Bastion (red)

**What it does:** grants a shield equal to 40% of max health per stack, and the shield starts recharging **2 s** after
the last hit (7 s in the game).

**Example:** with 1,000 health and 2 stacks, you get 800 shield, which comes back right after leaving the crossfire.
Test: `give_item Ror2MeeeeeBastion 1`.

### zzz cervo <3 (Void, green)

**What it does:** *Haste makes waste.* All **bonus** movement speed becomes damage: +1% speed becomes +1% damage. You
stay at normal speed; slows still apply. Each extra stack adds +50% conversion.

**Example:** with +40% speed from other items and 1 stack, you move at normal speed and get +40% damage. With 2 stacks,
you get +60%.
Test: `give_item Ror2MeeeeeCervo 1`.

### Heavy Heart (lunar)

**What it does:** each stack multiplies max health by 1.5 and damage taken from enemies by 1.3. Stacks multiply with
each other.

**Example:** with 2 stacks, health becomes 2.25× larger and damage taken 1.69× larger.
Test: `give_item Ror2MeeeeeHeavyHeart 1`.

---

## Equipment

### Stolas' Grimoire (lunar)

**What it does:** teleports you near the first living ally. Alone, it takes you to the stage's teleporter. In
exchange, each use gives credit to the director, which sends a wave of enemies. 60 s cooldown.

**How to use:** activate the equipment. Options in `[Grimoire]`:
- `WaveCredit`: credit per use, multiplied by difficulty (default 150, an average wave).
- `MaxCreditPerUse`: cap per use (default 600).
- `MaxMonsters`: with this many monsters alive, the teleport works but doesn't call a wave (default 80).

**Example:** you fell behind on the map. You use the Grimoire, show up next to the friend at the teleporter, and a wave
of enemies arrives right after.
Test: `give_equipment Ror2MeeeeeStolasGrimoire`.

---

## Elites (`[Elites]`)

The director starts rolling five new elites. Tier 1 ones appear from the start; tier 2 ones, from the loop on (after
stage 5). Each has its own `.cfg` key (`Volatile`, `Frenzied`, `Empowering`, `Blighted`, `Echo`). Killing an elite may
drop its aspect, as in the game.

| Elite | Tier | What it does |
|---|---|---|
| **Volatile** | 1 | Every hit explodes in 4 m (60% of the damage) and fires a missile every 1-5 s. |
| **Frenzied** | 1 | More movement and attack speed; teleports close to its target every 10 s. |
| **Empowering** | 1 | 25 m Warbanner aura: +30% attack and movement speed for its allies. |
| **Blighted** | 2 | Gets two random elite affixes at once. |
| **Echo** | 2 | Summons 2 echoes with 10% health that fire homing projectiles and vanish when it dies. |

**Example:** an Empowering Lemurian in the middle of a group makes everyone around it faster. Kill it first.

---

## Game systems

### Item multiplier (`[Multiplier]`)

**What it does:** each item a player picks up yields several copies. Doesn't multiply starting items, items from
monsters, scrap, temporary items, Void conversions or the mod's unique items.

**How to use:** on by default. Options:
- `Value`: total copies per collected item (default 10).
- `IncludeLunar` / `IncludeVoid`: also multiply lunar and Void items (default: yes).
- `StackCap`: cap on copies of the same item per player (default 150; `0` = no cap). An item picked up with the stack
  at the cap disappears, with a chat notice.

**Example:** you pick up 1 Lens-Maker's Glasses and end up with 10. 15 more pickups and you reach 150: the next one
disappears.

### Difficulty (`[Difficulty]`)

**What it does:** stronger, more numerous enemies and a faster difficulty ramp. It's also what makes *Call of the
Horde* work. Only the host needs it.

**How to use:** on by default. Options:
- `Factor`: overall multiplier (1 = no change; 2 = double, split by the gains below).
- `HealthGain`, `DamageGain`, `QuantityGain`, `PaceGain`: how much of `Factor` goes to health, damage, quantity and
  pace.
- `MonsterHealthBonus` (default 75) and `MonsterDamageBonus` (default 50): fixed extra health and damage, in %.

**Example:** with the defaults, every enemy has +75% health and +50% damage. With `Factor = 2`, on top of that: +100%
health, +50% damage, 3× more enemies and difficulty rising 50% faster.

### Progression between runs (`[Progression]`)

**What it does:** at the end of each run, the group gets a score (speed, deaths, damage taken and kills) and the
summary shows up in chat. A victory with a good score goes up one level, and each level gives more enemies next run. A
defeat goes down one level.

**How to use:** on by default. Options:
- `Step`: extra enemies per level (default `0.15`, +15%).
- `MaxMultiplier`: cap (default 3×).
- `RaiseAt`: minimum score to go up (default `0.5`).
- `Par...`: the score references (time to charge the teleporter, deaths, damage and kills).

Commands: `ror2meeeee_progress` shows the level; `ror2meeeee_progress_reset` resets it.

**Example:** you win fast and without dying: level 1 → 2, and the next run starts with +30% enemies. A run abandoned
midway doesn't count.

### Greater Bazaar (`[Bazaar]`)

**What it does:** the Bazaar Between Time gets up to 6 chests with the item on display, bought with the gold left over
from the stage. Next to Newt appears a pod that trades lunar coins for gold. The purchased item drops on the ground and
goes through the Multiplier and Communism. With the **Artifact of Command**, the choice cube of the same tier drops, as
from a game chest. The item floating over the chest is just a display: it can't be picked up.

**How to use:** enter the blue bazaar portal. With `KeepMoney` (on by default), your gold does **not** become XP on
entering: it stays to be spent, and whatever is left becomes XP when leaving. Options:
- `Chests`: number of chests (1 to 6).
- `PayWithLunarCoins`: chests cost lunar coins instead of gold.
- `LunarCoinGold` and `MaxExchangesPerPlayer`: gold per coin at the pod and trades per player.
- For each tier: weight, price, lunar price and stock (`WhiteWeight`, `WhiteCost`, `WhiteStock`...).

**Example:** you reach the bazaar with 400 gold. The chests show two greens and one red; a green costs the price of 2
small chests from the previous stage. Out of gold, you trade 1 lunar coin at the pod for the value of a medium chest.

### Limited calculation (`[LimitedCalc]`)

**What it does:** items that weigh on FPS with many stacks count only up to a limit, without losing the effect. Applies
to Bustling Fungus, Bundle of Fireworks and Ghor's Tome, each with its own section (`[LimitedCalc.BundleOfFireworks]`...).

**How to use:** choose each item's mode:
- `Off`: as in the game.
- `Clamp`: works as if it had at most `Limit` stacks.
- `Aggregate`: fewer effects, each one stronger, with the same total result. Fireworks fires fewer missiles with more
  damage each; Ghor's Tome drops fewer gold packs, each worth more.

Defaults: Fireworks `Aggregate`, up to 20; Ghor's Tome `Aggregate`, up to 5; Fungus `Off`.

**Example:** with 200 Fireworks, opening a chest would fire 804 missiles. In `Aggregate` with a limit of 20, 84 missiles
come out, each with almost 10× the damage. The total is the same and FPS doesn't drop.

---

## Screens

### Drop items (F7, `[Drop]`)

**What it does:** drops items from your inventory on the ground, to give to an ally.

**How to use:** press **F7**, click the item icon, adjust the amount and click **DROP** (or use the arrows, `+`/`-`
and Enter). Also through the console:
- `ror2meeeee_drop`: lists what can be dropped.
- `ror2meeeee_drop <name> <amount>`: drops directly.
- `ror2meeeee_drop_menu <name>`: opens the screen with the item already chosen.

Options: `MenuKey` (key, default `F7`) and `MaxPerCommand` (maximum at a time, default 10).

**Example:** you have 30 Syringes and your friend has none: `ror2meeeee_drop Syringe 10` drops 10 on the ground for
them to pick up.

### Run status (F8, `[Stats]`)

**What it does:** shows your stats (health, speed, damage, regen, armor, level, attack speed) in a bar above the
skills and, on a second press, every player with portrait and the same stats (plus the Tempest summary on the host).

**How to use:** **F8** cycles: your bar → every player → closed. The key is changed in `MenuKey`.

**Example:** leave the bar open to follow your attack speed while stacking Syringes; press again to compare with
your friends.

---

## Performance

On by default, so the match can handle huge inventories. None of them change the total damage.

| Section | What it does | Main options |
|---|---|---|
| `[Particles]` | Limits how many times per second each type of visual effect appears and is sent to others. | `MaxPerSecond` (10), `Raised`/`RaisedMaxPerSecond` (explosions, 60), `Exempt` (never limited; default Mercenary strikes) |
| `[ProjectileBudget]` | Items that fire en masse (Sticky Bomb, Molten Perforator, stakes, Fireworks, missiles...) release fewer projectiles, each one stronger. Player projectiles only. | `Prefabs`, `PerTypeMaxPerSecond` (15), `GlobalMaxPerSecond` (40) |
| `[GhostBlades]` | Limits the Ghost Blades from the Curse item (DLC4), which reached ~700 per second. | `MaxPerSecond` (10) |
| `[Cleanup]` | Corpses disappear quickly, even in view, and fixes a game bug in the Lynx Totem. | `MaxCorpses` (10), `CorpseLifetime` (2 s) |
| `[GameFixes]` | Fixes game errors that repeat hundreds of times per match (Runic Lens, Unstable Transmitter, War Bonds), the Void Barnacle spawn errors and the Defective Unit and Dunestrider death errors, the Mimic Chest, the Swarms artifact and Golden drones/monsters that froze with nuggets. | `Enabled` only |

**Example:** with 100 Sticky Bombs, instead of 100 bombs per second, 15 come out, each with the combined damage of the
others.

---

## Configuration

- **File:** `BepInEx/config/com.sawcunha.ror2meeeee.cfg`. In r2modman: *Config editor* → `com.sawcunha.ror2meeeee`.
- **On the mod settings screen:** with **Risk of Options** installed, the options show up in the game (in Portuguese
  when the game is in Português (Brasil)).
- **Who needs to configure:** only the host.
- **When it applies:** `Enabled` turns things on and off immediately (between matches). The other values are reloaded
  with `ror2meeeee_reload`.

## Console commands

Open the console with **Ctrl + Alt + \`** (or F2, depending on your keyboard).

| Command | What it's for |
|---|---|
| `ror2meeeee_status` | Lists the mod's features, failures and hooks. |
| `ror2meeeee_reload` | Reloads the `.cfg`. |
| `ror2meeeee_drop [name] [amount]` | Drops items (see *Drop items*). |
| `ror2meeeee_drop_menu [name]` | Opens the drop items screen. |
| `ror2meeeee_progress` / `ror2meeeee_progress_reset` | Progression level between runs / reset it. |
| `ror2meeeee_spend` | Purchases and gold spent per player in the run. |
| `ror2meeeee_effects [top]` | Most frequent visual effects (with `[Particles] CountSpawns` on). |
| `ror2meeeee_demo` | Shows the tombstone and the target marker in front of you for 15 s. |

## FAQ

**My friend can't join.** Everyone needs the same version of the mod. Update everyone through the manager.

**I picked up an item and it disappeared.** You reached the `[Multiplier] StackCap` cap (150 copies). The chat tells
you.

**Something went wrong.** `BepInEx/LogOutput.log` has `[Ror2Meeeee]` lines, and `ror2meeeee_status` shows whether some
part turned itself off after errors. Send the log along with the report. (The mod's log lines are in Portuguese.)

**The game froze and I had to close it.** Open the game again and send the `LogOutput.log` from that new launch: the
line `[Vigia] a sessão anterior não fechou normalmente` shows how the match was in the last seconds before freezing.

## Credits

Developed and tested by:

- **SawCunha**
- **Sleeping_tv_**
- **Póker.the.wolf**
- **Artie_saito**

Project on GitHub: [https://github.com/SawCunha/ror2_meeeeeeee](https://github.com/SawCunha/ror2_meeeeeeee)
