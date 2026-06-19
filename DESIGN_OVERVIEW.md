# DESIGN_OVERVIEW.md

## Neon Trader — Platform-Agnostic Design Document

**Version:** 1.0  
**Status:** Draft  
**Scope:** Foundation design document — all other design docs reference this

---

## 1. Vision Statement

**Neon Trader** is a turn-based cyberpunk trading simulation where players navigate a neon-drenched sprawl, buying and selling illicit payloads across five distinct sectors while managing heat, upgrading their cyberdeck, and building reputation with underground factions. The game captures the tension of DopeWars-style trading with the atmospheric depth of a Gibsonian sprawl — every transaction is a calculated risk, every sector has its own personality, and every week brings you closer to retirement or flatline.

**Core Fantasy:** "Buy low in Kowloon Stack, jack-in to Corporate Zone, sell high. Manage heat, upgrade your deck, build rep with fixers and gangs. Retire to Panama Cluster with ¥10M or flatline trying."

---

## 2. Design Pillars

### 2.1 Turn-Based Strategic Depth
Every action consumes one **week** (turn). Players must plan routes, anticipate market shifts, and balance risk vs. reward within a 52-week time limit. No real-time pressure — pure decision-making.

### 2.2 Heat as Core Tension Mechanic
Heat is the universal currency of risk. It accumulates from transactions, travel, and events. High heat triggers CorpSec raids (cargo/cred loss), increases future heat gain, and ends the game at 100%. Players must actively manage heat through sector choice, deck upgrades, and faction reputation.

### 2.3 Sector Personality & Asymmetry
Five sectors with distinct specialties, heat baselines, CorpSec/gang presence, and service availability. No sector is "best" — each serves different strategies:
- **Kowloon Stack:** Wetware/Stims hub, high gang presence, fixer access
- **Corporate Zone:** Exploits/AI Shards, extreme CorpSec, no fixers
- **Industrial Sector:** Credsticks/Wetware, low heat, quiet margins
- **Orbital Nexus:** AI Shards/Exploits, research tech, high CorpSec
- **Darknet:** All classes, extreme heat, full services, dangerous

### 2.4 Deck as Progression & Expression
Six upgrade paths (ICEbreaker, Stealth, Cargo, Trace Reducer, Scanner, Auto-Fence) let players specialize. Upgrades are expensive, exponential in cost, and fundamentally change what strategies are viable.

### 2.5 Reputation as Long-Term Investment
Four factions (Fixer, Gangs, CorpSec, Netrunners) with tiered benefits. Reputation gates content (better loan terms, reduced heat, exclusive payloads) and creates emergent narrative — players *become* known in the sprawl.

### 2.6 Platform Agnosticism
Game logic is pure data transformation. The same rules execute identically in Python/Textual, Go/Bubble Tea, or any future port. UI is a thin presentation layer over deterministic simulation.

---

## 3. Scope & Non-Goals

### In Scope (MVP)
- Turn-based trading across 5 connected sectors
- 15 payloads across 5 classes with dynamic pricing
- Heat system with CorpSec raids
- 6 deck upgrades with exponential costs
- 4-faction reputation system with tiers
- Fixer services: loans, money laundering, intel
- Random events (market spikes, corp wars, net crashes, zero-day drops)
- Save/Load (JSON persistence)
- Win condition: ¥10M cred | Lose condition: 100% heat or week 52+

### Explicit Non-Goals
- ❌ Real-time or action gameplay
- ❌ Multiplayer or PvP
- ❌ 3D graphics, sprites, or animations beyond terminal effects
- ❌ Procedural narrative / branching story
- ❌ Modding API (v1)
- ❌ Mobile/touch optimization
- ❌ Sound/music (terminal beep only)
- ❌ Platform-specific features (Steam achievements, etc.)

### Future Expansions (Post-MVP)
- Courier missions with time-sensitive delivery
- Multiple win conditions (reputation, deck maxed, sector control)
- High-score table with seeding
- Additional payload classes (bioware, drones, black ICE)
- Faction warfare dynamic events
- New Game+ with escalating difficulty

---

## 4. Target Audience & Platforms

### Primary Audience
- **Roguelike/Strategy fans** who enjoy DopeWars, Drug Wars, Taipan, Elite Dangerous trading
- **Cyberpunk enthusiasts** seeking atmospheric terminal games
- **TUI enjoyers** who appreciate keyboard-driven, distraction-free interfaces

### Platform Targets (Platform-Agnostic Core)
| Platform | Implementation Status | Notes |
|----------|----------------------|-------|
| **Desktop (Linux/macOS/Windows)** | ✅ Python/Textual, ✅ Go/Bubble Tea | Primary targets |
| **Terminal/SSH** | ✅ Both | Headless server play |
| **WebAssembly (Browser)** | 🔄 Planned | Via Go/WASM or PyScript |
| **Mobile (Termux, ish)** | 🔄 Possible | Terminal-only, no touch UI |

**Key Principle:** The game *is* the simulation. Platforms are just renderers.

---

## 5. High-Level Architecture Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                        SIMULATION CORE                          │
│  (Pure Functions / Deterministic State Transitions)             │
├─────────────────────────────────────────────────────────────────┤
│  • GameState          — Single source of truth                  │
│  • MarketEngine       — Price generation, buy/sell logic        │
│  • HeatSystem         — Heat calc, decay, CorpSec raids         │
│  • EventSystem        — Random events, modifiers                │
│  • TravelSystem       — Sector graph, trace detection           │
│  • ReputationSystem   — Faction tiers, gated content            │
│  • DeckSystem         — Upgrade effects, cargo capacity         │
│  • Persistence        — Save/Load (JSON)                        │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                      PLATFORM ADAPTER LAYER                     │
│  (Thin — Input Mapping → Core → Output Formatting)              │
├─────────────────────────────────────────────────────────────────┤
│  Python: Textual App ──► Widgets ──► Screens ──► Key Bindings   │
│  Go:     Bubble Tea   ──► Components ──► Views ──► Key Map      │
│  Future: WASM/Console ──► Same Core, Different Renderer         │
└─────────────────────────────────────────────────────────────────┘
```

### 5.1 Core Data Models (Shared)

| Model | Responsibility | Key Fields |
|-------|----------------|------------|
| **Player** | Agent state | cred, cargo[], deck, reputation, sector_id, week, heat, loans[] |
| **Sector** | Location node | id, name, specialties[], base_heat, corpsec/gang presence, connections[] |
| **Payload** | Trade good | id, name, class, rarity, base_price, variance, size, heat_on_sale |
| **PayloadClass** | Category metadata | name, color, volatility (low/medium/high/extreme) |
| **Market** | Sector-specific pricing | sector, payloads → Listing{buy_price, sell_price, stock} |
| **Deck** | Upgrade container | 6× Upgrade{type, level, max_level, base_cost, multiplier} |
| **Upgrade** | Progression unit | type, level, cost_for_next_level(), effect_value() |
| **Reputation** | Faction standing | fixer, gangs, corpsec, netrunners (-100 to 100) |
| **Event** | World simulation | type, description, price_modifiers{}, heat_modifiers{} |

### 5.2 Core Systems (Stateless Functions)

| System | Input | Output | Side Effects |
|--------|-------|--------|--------------|
| `generate_market_prices(payloads, sector)` | payloads, sector | Market | Mutates Market listings |
| `calculate_heat_gain(player, sector, payload, qty, selling)` | context | heat_delta | None |
| `apply_sector_heat_decay(sectors)` | sectors | None | Mutates sector heat |
| `check_raid(player, sector)` | player, sector | {raided, cargo_lost, cred_lost, heat_gained} | None |
| `get_random_event(sectors, week)` | sectors, week | event_type | None |
| `apply_event_modifiers(type, payloads, sectors, context)` | event, data | {description, modifiers} | Mutates payload/sector prices |
| `advance_turn(state)` | GameState | GameState | Mutates week, heat, loans, market, events |

### 5.3 Platform Adapter Responsibilities

| Layer | Responsibility | NOT Responsible For |
|-------|----------------|---------------------|
| **Renderer** | Draw state → terminal | Game logic, calculations |
| **Input Mapper** | Key → Action intent | Action execution |
| **Screen/View** | Compose widgets for current mode | State mutation |
| **App Loop** | Mount screens, handle lifecycle | Simulation step (delegates to core) |

---

## 6. Cross-References to Design Docs

This document is the **root** of the design hierarchy. All other design docs **must** reference and align with it.

| Document | Purpose | References This Doc Via |
|----------|---------|------------------------|
| **DATA_MODEL.md** | Canonical data structures, enums, serialization formats | Section 5.1 (Core Data Models) |
| **MARKET_SYSTEM.md** | Price generation, volatility, supply/demand, event modifiers | Section 2.3 (Sector Personality), 5.2 (MarketEngine) |
| **HEAT_SYSTEM.md** | Heat calculation, decay curves, raid mechanics, trace detection | Section 2.2 (Heat as Core Tension), 5.2 (HeatSystem) |
| **DECK_SYSTEM.md** | Upgrade trees, effects, costs, synergy | Section 2.4 (Deck as Progression), 5.1 (Deck/Upgrade) |
| **REPUTATION_SYSTEM.md** | Faction tiers, gated content, reputation gain/loss | Section 2.5 (Reputation), 5.1 (Reputation) |
| **EVENT_SYSTEM.md** | Event types, triggers, modifiers, narrative integration | Section 5.2 (EventSystem) |
| **TRAVEL_SYSTEM.md** | Sector graph, trace mechanics, travel costs | Section 2.3 (Sector Personality), 5.2 (TravelSystem) |
| **PERSISTENCE.md** | Save schema, versioning, migration, load validation | Section 5.1 (all models' to_dict/from_dict) |
| **UI_ARCHITECTURE.md** | Screen hierarchy, widget composition, input mapping | Section 5.3 (Platform Adapter Layer) |
| **PLATFORM_PORTS.md** | Porting guide, platform-specific considerations | Section 4 (Target Platforms), Section 5 (Architecture) |

### Cross-Reference Rules
1. **No circular dependencies** — child docs reference DESIGN_OVERVIEW, not each other
2. **Single source of truth** — if DESIGN_OVERVIEW and child doc conflict, DESIGN_OVERVIEW wins
3. **Version alignment** — all docs share version number; bump together
4. **Implementation references** — Python (`neon_trader/`) and Go (`internal/`) cited only as **existence proofs**, not specifications

---

## 7. Implementation Existence Proofs

Two implementations validate the platform-agnostic design:

### Python/Textual (Original)
- **Location:** `neon_trader/`
- **Entry:** `run.py` → `neon_trader/app.py:NeonTraderApp`
- **Architecture:** Textual App + Screens + Widgets
- **State:** Held in `NeonTraderApp` instance (player, sectors, payloads, market)
- **Turn Loop:** `advance_turn()` called after every buy/sell/travel
- **Screens:** Main, Market, Travel, Fixer, Upgrade, Node, Help

### Go/Bubble Tea (Port)
- **Location:** `internal/` + `cmd/neon-trader/`
- **Entry:** `cmd/neon-trader/main.go`
- **Architecture:** Bubble Tea Model/View/Update + Components
- **State:** `game.GameState` struct (pure data)
- **Turn Loop:** `AdvanceTurn()` method on GameState
- **Components:** Reusable UI components mirroring Python screens

**Both implementations:**
- Load identical `data/sectors.json` and `data/payloads.json`
- Produce identical market prices given same seed/week
- Calculate identical heat for same transaction parameters
- Share the same win/lose conditions
- Use JSON save format compatible across both

---

## 8. Data Flow Summary

```
NEW GAME
    │
    ▼
LOAD STATIC DATA (sectors.json, payloads.json) ──► GameState
    │
    ▼
GENERATE INITIAL MARKET (current sector) ──► Market
    │
    ▼
RENDER MAIN SCREEN (sector info, cargo, deck, heat, cred)
    │
    ├── USER INPUT: BUY/SELL ──► MarketEngine ──► HeatSystem ──► CHECK RAID ──► ADVANCE TURN ──► RERENDER
    ├── USER INPUT: TRAVEL   ──► TravelSystem ──► HeatSystem ──► CHECK RAID ──► ADVANCE TURN ──► RERENDER
    ├── USER INPUT: FIXER    ──► ReputationSystem / LoanSystem ──► ADVANCE TURN ──► RERENDER
    ├── USER INPUT: UPGRADE  ──► DeckSystem ──► ADVANCE TURN ──► RERENDER
    └── USER INPUT: NODE     ──► Launder/Stash ──► ADVANCE TURN ──► RERENDER
    │
    ▼
WIN/LOSS CHECK (every turn) ──► VICTORY / GAME OVER SCREEN
```

---

## 9. Key Invariants (Must Hold Across All Platforms)

1. **Determinism:** Same seed + same inputs = same outputs
2. **Turn Atomicity:** One user action = exactly one `advance_turn()`
3. **Heat Clamping:** Heat always 0-100 inclusive
4. **Cargo Capacity:** `cargo_used ≤ deck.capacity` always
5. **Sector Connectivity:** Travel only along defined connections
6. **Price Bounds:** `sell_price ≤ buy_price` (market spread)
7. **Week Limit:** Game ends at `week > max_weeks` (default 52)
8. **Victory Threshold:** `cred ≥ 10,000,000` (configurable constant)
9. **Upgrade Costs:** Exponential: `cost = base × multiplier^level`
10. **Save Compatibility:** JSON schema versioned; forward/backward compatible

---

## 10. Configuration Constants (Single Source)

| Constant | Value | Location |
|----------|-------|----------|
| `STARTING_CRED` | 50,000 | Player init |
| `MAX_WEEKS` | 52 | Player/GameState |
| `VICTORY_CRED` | 10,000,000 | Player.IsVictory |
| `MAX_HEAT` | 100 | Heat clamping |
| `BASE_CARGO_SLOTS` | 20 | Deck.BASE_CARGO_SLOTS |
| `CARGO_PER_LEVEL` | 10 | Deck.cargo upgrade |
| `RAID_HEAT_THRESHOLD` | 50 | HeatSystem.check_raid |
| `TRACE_DETECTION_HEAT` | 25 | TravelSystem |
| `LOAN_DEFAULT_INTEREST` | 0.15 (15%/week) | Fixer screen |
| `LOAN_DEFAULT_WEEKS` | 4 | Fixer screen |

---

## 11. Glossary

| Term | Definition |
|------|------------|
| **Week** | One game turn; all actions consume 1 week |
| **Sector** | A location node with market, heat, connections |
| **Payload** | Tradable item with class, rarity, size, base price |
| **Listing** | Sector-specific buy/sell price + stock for a payload |
| **Heat** | Risk metric 0-100; triggers raids, increases costs |
| **CorpSec** | Corporate Security; raids high-heat players |
| **Deck** | Player's cyberdeck; holds 6 upgradable systems |
| **ICEbreaker** | Deck upgrade: reduces payload heat on sale |
| **Stealth** | Deck upgrade: reduces trace detection chance |
| **Cargo** | Deck upgrade: increases cargo capacity |
| **Trace Reducer** | Deck upgrade: reduces heat from travel |
| **Scanner** | Deck upgrade: reveals market prices in connected sectors |
| **Auto-Fence** | Deck upgrade: reduces fence fee when selling |
| **Fixer** | Underground broker; loans, laundering, intel |
| **Cred** | Currency (¥) |
| **Flatline** | Game over (heat 100% or time expired) |

---

## 12. Revision History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2026-06-15 | RedForest | Initial platform-agnostic design document |

---

*This document is the authoritative design reference. All implementation decisions should trace back to a pillar, invariant, or system defined herein. When in doubt, ask: "Does this serve the vision? Does it violate an invariant? Is it in scope?"*