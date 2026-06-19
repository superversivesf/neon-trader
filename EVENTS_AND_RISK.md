# EVENTS AND RISK SYSTEM DESIGN

**Platform-Agnostic Specification for Neon Trader**

---

## 1. RANDOM EVENT SYSTEM

### 1.1 Overview

Events are the primary source of non-deterministic gameplay variation. Each turn (week), the system selects one event from a weighted pool, applies its effects to markets/sectors, and notifies the player.

### 1.2 Event Selection Algorithm

```python
def get_random_event(sectors: Dict[str, Sector], week: int) -> Optional[str]:
    base_events = ["market_spike", "corp_war", "net_crash", "ice_upgrade", "zero_day_drop", "data_courier"]
    weights = [20, 15, 10, 10, 5, 15]

    # Dynamic weight adjustments
    avg_heat = sum(s.current_heat for s in sectors.values()) / len(sectors)
    if avg_heat > 60:
        weights[2] += 10  # net_crash
        weights[3] += 10  # ice_upgrade

    if week > 30:
        weights[4] += 10  # zero_day_drop

    return random.choices(base_events, weights=weights, k=1)[0]
```

### 1.3 Base Events and Weights

| Event ID | Base Weight | Category | Description |
|----------|-------------|----------|-------------|
| `market_spike` | 20 | Market | Specific payload class prices surge in current sector |
| `corp_war` | 15 | Market/Narrative | Two sectors in conflict; exploit/ai_shard prices volatile |
| `net_crash` | 10 | Market | Digital payloads crash; hardware premium |
| `ice_upgrade` | 10 | Hazard | CorpSec deploys new countermeasures globally |
| `zero_day_drop` | 5 | Opportunity | Rare exploit surfaces in Darknet |
| `data_courier` | 15 | Opportunity | Transport contract between sectors |

**Total Base Weight**: 75

### 1.4 Weight Modifiers

| Condition | Affected Events | Weight Delta |
|-----------|-----------------|--------------|
| Avg sector heat > 60 | `net_crash` +10, `ice_upgrade` +10 | Increases hazard frequency in hot sprawl |
| Week > 30 | `zero_day_drop` +10 | Late-game rare opportunities |

### 1.5 Event Cooldowns (Recommended Addition)

*Not currently implemented — design for future expansion*

```python
EVENT_COOLDOWNS = {
    "market_spike": 3,        # weeks
    "corp_war": 5,
    "net_crash": 4,
    "ice_upgrade": 6,
    "zero_day_drop": 8,
    "data_courier": 2,
}

# Track last occurrence per event
last_occurrence: Dict[str, int] = {}  # event_id -> week

def is_on_cooldown(event_id: str, current_week: int) -> bool:
    last = last_occurrence.get(event_id, -999)
    return (current_week - last) < EVENT_COOLDOWNS.get(event_id, 0)
```

---

## 2. EVENT CATEGORIES

### 2.1 Market Events
Directly modify payload prices, stock, or sector heat.

| Event | Class Modifiers | Heat Change | Special |
|-------|----------------|-------------|---------|
| `market_spike` | `target_class: 2.5x` | +10% | Target class chosen at runtime |
| `corp_war` | `exploits: 1.8x`, `ai_shards: 1.5x` | +15% | Affects two sectors |
| `net_crash` | `exploits: 0.4x`, `ai_shards: 0.3x`, `wetware: 1.5x`, `stims: 1.3x` | -5% | Digital crash, hardware boom |

### 2.2 Narrative Events
Advance world state, modify sector properties, unlock content.

| Event | Effects |
|-------|---------|
| `ice_upgrade` | +20% CorpSec presence globally, +20 heat |
| `zero_day_drop` | Spawns 1 `zero_day_exploit` in Darknet, +5 heat |

### 2.3 Hazard Events
Increase player risk, heat, or apply penalties.

| Event | Hazard Type |
|-------|-------------|
| `ice_upgrade` | Global CorpSec presence increase |
| `net_crash` | Digital asset devaluation (indirect hazard) |

### 2.4 Opportunity Events
Create profitable situations for prepared players.

| Event | Opportunity |
|-------|-------------|
| `zero_day_drop` | Rare high-value payload available |
| `data_courier` | Guaranteed profit transport job |
| `market_spike` | Sell high if holding target class |
| `corp_war` | Arbitrage between warring sectors |

---

## 3. HEAT SYSTEM

### 3.1 Heat Concept

Heat represents attention from CorpSec and rival factions. It exists at two levels:

| Level | Scope | Range | Source |
|-------|-------|-------|--------|
| **Sector Heat** | Per-sector | 0–100 | Transactions, events, travel, time |
| **Player Heat** | Global (max of all sectors) | 0–100 | Aggregated from sectors |

### 3.2 Transaction Heat Calculation

```python
def calculate_heat_gain(player, sector, payload, quantity, is_selling):
    base_heat = payload.heat_on_sale * quantity
    sector_mult = 1.0 + (sector.current_heat / 100.0)
    stealth_reduction = player.deck.get_stealth_bonus()      # 0.15 per level
    gang_protection = player.reputation.get_gang_tier() * 0.05  # 5% per tier
    corpsec_heat = (abs(player.reputation.corpsec) // 25) * 0.1 if player.reputation.corpsec < 0 else 0

    total = int(base_heat * sector_mult * (1 - stealth_reduction - gang_protection + corpsec_heat))
    return max(1, total)
```

#### Heat Gain Factors

| Factor | Formula | Max Impact |
|--------|---------|------------|
| Payload base | `heat_on_sale * quantity` | Varies by payload (2–50) |
| Sector heat multiplier | `1 + sector_heat/100` | 2.0x at 100 heat |
| Stealth upgrade | `-0.15 * level` | -75% at level 5 |
| Gang reputation | `-0.05 * tier` | -20% at tier 4 |
| CorpSec reputation (negative) | `+0.1 * (|corpsec|/25)` | +20% at -100 corpsec |

#### Payload Heat Values (from payloads.json)

| Class | Typical Heat/Sale | Examples |
|-------|-------------------|----------|
| `credsticks` | 2–5 | Low risk, low margin |
| `stims` | 6–30 | Volume-dependent |
| `wetware` | 8–15 | Medium risk |
| `exploits` | 18–25 | High value, high heat |
| `ai_shards` | 28–50 | Extreme risk/reward |

### 3.3 Travel Heat (Trace Risk)

```python
def calculate_travel_risk(player, from_sector, to_sector):
    base_risk = from_sector.get_travel_risk(to_sector)
    trace_reduction = player.deck.get_trace_reduction()  # 0.15 per level
    adjusted = int(base_risk * (1 - trace_reduction))
    adjusted -= player.reputation.get_gang_tier() * 5
    if player.reputation.corpsec < -50:
        adjusted -= 10
    adjusted = max(5, min(95, adjusted))
    detected = random(1,100) <= adjusted
    return adjusted, detected
```

#### Travel Risk Calculation

| Component | Formula |
|-----------|---------|
| Base risk | `(from.corpsec + to.corpsec)/2 + (from.heat + to.heat)/4` |
| Trace reducer | `-0.15 * level` (15% per level) |
| Gang tier | `-5 * tier` |
| CorpSec rep < -50 | `-10` |
| **Clamped** | **5–95%** |

**On Detection**: Player gains **+25 heat** immediately.

### 3.4 Heat Decay (Per Turn)

```python
def apply_sector_heat_decay(sectors):
    for sector in sectors.values():
        if sector.current_heat > sector.base_heat:
            decay = random.randint(2, 5)
            sector.adjust_heat(-decay)
        elif sector.current_heat < sector.base_heat:
            sector.adjust_heat(1)  # Slow return to base
```

| Condition | Decay/Recovery |
|-----------|----------------|
| Above base | -2 to -5 per turn (random) |
| Below base | +1 per turn |
| At base | Stable |

### 3.5 Heat Thresholds & Consequences

| Heat Level | Effect |
|------------|--------|
| 0–30 | Low attention, minimal raid chance |
| 31–60 | Moderate raid chance, price volatility |
| 61–80 | High raid chance, frequent events |
| 81–99 | Near-certain raids, extreme volatility |
| **100** | **GAME OVER** (player.heat >= 100) |

---

## 4. CORPSEC RAID MECHANICS

### 4.1 Raid Trigger Check

Executed after every buy/sell transaction in `check_raid()`:

```python
def check_raid(player, sector):
    raid_chance = sector.current_heat / 2           # 0–50% base
    icebreaker_reduction = player.deck.get_icebreaker_bonus() * 100  # 15% per level
    raid_chance = max(0, raid_chance - icebreaker_reduction)
    gang_tier = player.reputation.get_gang_tier()
    raid_chance -= gang_tier * 5

    raided = random.randint(1, 100) <= raid_chance
    ...
```

### 4.2 Raid Chance Formula

| Factor | Calculation | Max Reduction |
|--------|-------------|---------------|
| Base | `sector.current_heat / 2` | 50% at heat 100 |
| ICEbreaker | `-0.15 * level * 100` | -75% at level 5 |
| Gang tier | `-5 * tier` | -20% at tier 4 |
| **Final** | **Clamped to 0%** | **0% minimum** |

### 4.3 Raid Resolution

If raided, losses are calculated:

```python
# For each cargo item type:
if random.random() < 0.5:  # 50% chance per item TYPE
    lost_qty = max(1, item.quantity // 2)
    cargo_lost += lost_qty
    cred_lost += item.buy_price * lost_qty
    item.quantity -= lost_qty

player.cred = max(0, player.cred - cred_lost)
player.add_heat(20)  # Flat +20 heat penalty
```

#### Raid Consequences Summary

| Loss Type | Calculation |
|-----------|-------------|
| Cargo | 50% chance per **item type** to lose half quantity (min 1) |
| Credits | Buy price × quantity lost |
| Heat | **+20 flat** (added to player heat) |
| Reputation | None directly (but heat increase affects future) |

### 4.4 Raid Mitigation Strategies

| Upgrade/Rep | Effect |
|-------------|--------|
| ICEbreaker (deck) | -15% raid chance per level |
| Gang Reputation | -5% raid chance per tier |
| Stealth (deck) | Reduces heat *gain*, indirectly lowers raid chance |
| Low sector heat | Directly reduces base chance |

---

## 5. EVENT DEFINITIONS (Complete)

### 5.1 EVENT_MODIFIERS Dictionary

```python
EVENT_MODIFIERS = {
    "market_spike": {
        "description": "Market Spike — {class_name} prices surging in {sector}!",
        "class_modifiers": {"target_class": 2.5},
        "heat_change": 10,
    },
    "corp_war": {
        "description": "Corporate War — {sector_a} and {sector_b} in conflict. Exploit prices volatile.",
        "class_modifiers": {"exploits": 1.8, "ai_shards": 1.5},
        "heat_change": 15,
    },
    "net_crash": {
        "description": "Net Crash — Digital payloads crashing. Hardware premium.",
        "class_modifiers": {"exploits": 0.4, "ai_shards": 0.3, "wetware": 1.5, "stims": 1.3},
        "heat_change": -5,
    },
    "ice_upgrade": {
        "description": "ICE Upgrade — CorpSec deployed new countermeasures globally.",
        "class_modifiers": {},
        "heat_change": 20,
        "global_corpsec_boost": 20,
    },
    "zero_day_drop": {
        "description": "Zero-Day Drop — Rare exploit surfaced in Darknet.",
        "class_modifiers": {"exploits": 0.6},
        "heat_change": 5,
        "spawn_rare": "zero_day_exploit",
        "spawn_sector": "darknet",
    },
    "data_courier": {
        "description": "Courier Contract — Transport {payload} from {origin} to {destination}. Pays ¥{reward}.",
        "class_modifiers": {},
        "heat_change": 0,
        "courier_job": True,
    },
}
```

### 5.2 Event Effects Breakdown

#### `market_spike`
- **Trigger**: Random (weight 20)
- **Effect**: One payload class ×2.5 price in current sector
- **Heat**: +10% to current sector
- **Opportunity**: Sell target class if holding
- **Risk**: Holding wrong class = missed opportunity

#### `corp_war`
- **Trigger**: Random (weight 15)
- **Effect**: Exploits ×1.8, AI Shards ×1.5 in two sectors
- **Heat**: +15% to target sectors
- **Opportunity**: Buy exploits/ai_shards in one, sell in other
- **Risk**: High heat = higher raid chance

#### `net_crash`
- **Trigger**: Random (weight 10), more likely at high heat
- **Effect**: Exploits ×0.4, AI Shards ×0.3, Wetware ×1.5, Stims ×1.3
- **Heat**: -5% (relief)
- **Opportunity**: Buy wetware/stims cheap, sell high after
- **Risk**: Digital asset holders take massive losses

#### `ice_upgrade`
- **Trigger**: Random (weight 10), more likely at high heat
- **Effect**: +20 CorpSec presence globally, no price changes
- **Heat**: +20% all sectors
- **Risk**: Dramatically increases raid chances everywhere
- **Mitigation**: ICEbreaker upgrade critical

#### `zero_day_drop`
- **Trigger**: Random (weight 5), more likely late game (week 30+)
- **Effect**: 1 Zero-Day Exploit spawns in Darknet, exploits ×0.6 price
- **Heat**: +5%
- **Opportunity**: Rare payload available for purchase
- **Risk**: Darknet is high-heat (80 base)

#### `data_courier`
- **Trigger**: Random (weight 15)
- **Effect**: Transport contract generated
- **Heat**: 0
- **Opportunity**: Guaranteed profit, rep gain
- **Risk**: Travel heat + trace detection risk

---

## 6. ESCALATION CHAINS

### 6.1 Heat Escalation Chain

```
Transaction → Heat Gain → Sector Heat ↑ → Raid Chance ↑ → Raid → Heat +20 →
  → Higher Sector Heat → More Events (net_crash, ice_upgrade) → More Heat →
  → CorpSec Presence ↑ (ice_upgrade) → Travel Risk ↑ → Detection → Heat +25 →
  → PLAYER HEAT 100 → GAME OVER
```

### 6.2 Event Escalation Chains

| Chain | Sequence | Player Counter |
|-------|----------|----------------|
| **CorpSec Escalation** | High heat → `ice_upgrade` → Global CorpSec+ → More raids → More heat | ICEbreaker, Gang rep, Low profile |
| **Market Crash** | `net_crash` → Digital prices crash → Players dump → More heat from panic sells | Hold wetware/stims, avoid exploits |
| **War Profiteering** | `corp_war` → Price divergence → Arbitrage → High volume → High heat → Raids | Quick in/out, stealth upgrades |
| **Late Game Pressure** | Week 30+ → `zero_day_drop` more frequent → Darknet trips → Extreme heat | Max stealth, ICEbreaker, gang allies |

### 6.3 Sector Heat Feedback Loop

```
Sector Base Heat (fixed)
    ↓
Player Activity (trades, travel)
    ↓
Sector Current Heat ↑
    ↓
Price Volatile prices, higher raid chance
    ↓
Events: ice_upgrade, net_crash more likely
    ↓
Global CorpSec ↑ / Heat modifiers
    ↓
All sectors affected
    ↓
Decay (2-5/turn) fights back
```

---

## 7. RISK/REWARD CALCULUS

### 7.1 Payload Risk/Reward Matrix

| Class | Avg Heat/Sale | Avg Price | Volatility | Risk Rating | Best Sector |
|-------|---------------|-----------|------------|-------------|-------------|
| `credsticks` | 3 | ~2,000 | Low | ⭐ | Industrial |
| `stims` | 10 | ~5,000 | Medium | ⭐⭐ | Kowloon |
| `wetware` | 12 | ~7,000 | Medium | ⭐⭐ | Kowloon/Industrial |
| `exploits` | 22 | ~9,000 | High | ⭐⭐⭐ | Corporate/Orbital |
| `ai_shards` | 38 | ~42,000 | Extreme | ⭐⭐⭐⭐ | Corporate/Orbital |

### 7.2 Sector Risk Profiles

| Sector | Base Heat | CorpSec | Gang | Specialties | Risk | Reward |
|--------|-----------|---------|------|-------------|------|--------|
| Industrial | 15 | 30 | 40 | credsticks, wetware | Low | Low |
| Kowloon Stack | 30 | 40 | 80 | wetware, stims | Medium | Medium |
| Orbital Nexus | 45 | 70 | 20 | ai_shards, exploits | High | High |
| Corporate Zone | 60 | 90 | 10 | exploits, ai_shards | Very High | Very High |
| Darknet | 80 | 50 | 60 | ALL | Extreme | Extreme |

### 7.3 Optimal Strategy by Heat Level

| Heat Range | Strategy |
|------------|----------|
| **0–30** | Aggressive trading, high-volume credsticks/wetware, build rep |
| **31–50** | Selective exploits, avoid Darknet, upgrade stealth |
| **51–70** | Minimize trades, focus courier jobs, upgrade ICEbreaker |
| **71–90** | Emergency mode: sell cargo, pay loans, seek low-heat sectors |
| **91–99** | Final push or escape: victory check, minimize all actions |

### 7.4 Upgrade Investment ROI

| Upgrade | Cost (L1→L5) | Effect | Break-even |
|---------|--------------|--------|------------|
| **Stealth** | 12k → 192k | -15% heat/lvl | ~50 trades |
| **ICEbreaker** | 15k → 240k | -15% raid/lvl | ~8 raids prevented |
| **Trace Reducer** | 10k → 160k | -15% travel risk/lvl | ~20 safe travels |
| **Cargo** | 8k → 64k | +10 slots/lvl | Immediate capacity |
| **Scanner** | 20k → 320k | +1 sector vision/lvl | Strategic value |
| **Auto Fence** | 25k → 100k (L3) | Fence discount | High-volume sellers |

---

## 8. TURN STRUCTURE & EVENT TIMING

### 8.1 Turn Sequence

```
PLAYER ACTION (buy/sell/travel)
    ↓
Heat Applied (transaction + travel)
    ↓
CHECK RAID (immediate)
    ↓
ADVANCE TURN (advance_turn())
    ├─ Player week++
    ├─ Loan payments
    ├─ SECTOR HEAT DECAY (2-5 per sector)
    ├─ RANDOM EVENT (get_random_event + apply_event_modifiers)
    ├─ MARKET REFRESH (generate_market_prices)
    ├─ WIN/LOSS CHECK
    └─ SCREEN UPDATE
```

### 8.2 Event Application Order

1. **Heat decay** — Natural cooling
2. **Random event** — State change
3. **Market refresh** — Prices incorporate event + heat
4. **Notifications** — Player informed

---

## 9. BALANCE PARAMETERS (Tunable)

### 9.1 Heat System Constants

| Parameter | Value | Location |
|-----------|-------|----------|
| Base raid chance divisor | 2 (heat/2) | `heat_system.py:61` |
| Raid cargo loss chance | 50% per type | `heat_system.py:82` |
| Raid cargo loss fraction | 1/2 quantity | `heat_system.py:83` |
| Raid heat penalty | +20 | `heat_system.py:79` |
| Travel detection heat | +25 | `app.py:297` |
| Heat decay range | 2–5 | `heat_system.py:106` |
| Heat recovery (below base) | +1 | `heat_system.py:110` |
| Stealth per level | 0.15 | `deck.py:40` |
| Gang protection per tier | 0.05 | `heat_system.py:28` |
| CorpSec heat per tier | 0.1 | `heat_system.py:31` |

### 9.2 Event Weights

| Event | Base | High Heat | Late Game |
|-------|------|-----------|-----------|
| market_spike | 20 | 20 | 20 |
| corp_war | 15 | 15 | 15 |
| net_crash | 10 | 20 | 10 |
| ice_upgrade | 10 | 20 | 10 |
| zero_day_drop | 5 | 5 | 15 |
| data_courier | 15 | 15 | 15 |

### 9.3 Event Modifiers

| Event | Class Modifier | Heat Delta | Global Effect |
|-------|---------------|------------|---------------|
| market_spike | 2.5x target | +10 | — |
| corp_war | 1.8x/1.5x | +15 | — |
| net_crash | 0.4x/0.3x/1.5x/1.3x | -5 | — |
| ice_upgrade | — | +20 | CorpSec +20 |
| zero_day_drop | 0.6x exploits | +5 | Spawn rare |
| data_courier | — | 0 | Job generated |

---

## 10. IMPLEMENTATION NOTES

### 10.1 Current State (v0.1.0)

- ✅ Heat calculation (transaction + travel)
- ✅ Sector heat decay
- ✅ CorpSec raid check + resolution
- ✅ 6 event types with modifiers
- ✅ Dynamic event weights (heat + week)
- ✅ Event application to markets/sectors

### 10.2 Missing / Planned

- [ ] **Event cooldowns** (prevent repeat spam)
- [ ] **Escalation chains** (multi-turn event sequences)
- [ ] **Player choice events** (branching narratives)
- [ ] **Sector-specific events** (Kowloon gang war, Corporate audit)
- [ ] **Reputation-gated events** (high fixer = better tips)
- [ ] **Event history/log** (player can review)
- [ ] **Event forecasting** (scanner upgrade shows next event)

### 10.3 Integration Points

| System | Calls | Called By |
|--------|-------|-----------|
| `get_random_event` | `advance_turn()` | `app.py:247` |
| `apply_event_modifiers` | `advance_turn()` | `app.py:248-256` |
| `calculate_heat_gain` | `buy_payload`, `sell_payload` | `app.py:177, 209` |
| `check_raid` | `buy_payload`, `sell_payload` | `app.py:184, 216` |
| `calculate_travel_risk` | `travel_to` (via screen) | `TravelScreen` |
| `apply_sector_heat_decay` | `advance_turn()` | `app.py:243` |

---

## 11. TESTING SCENARIOS

### 11.1 Unit Test Cases

```python
# Heat calculation
assert calculate_heat_gain(player, sector, payload, 1, True) >= 1
assert calculate_heat_gain(..., payload_high_heat, 10, True) > calculate_heat_gain(..., payload_low_heat, 10, True)

# Stealth reduces heat
player.deck.stealth.level = 5  # 75% reduction
heat_with_stealth = calculate_heat_gain(...)
player.deck.stealth.level = 0
heat_without = calculate_heat_gain(...)
assert heat_with_stealth < heat_without

# Raid chance decreases with ICEbreaker
player.deck.icebreaker.level = 5  # 75% reduction
chance_with = check_raid(player, sector)["chance"]
player.deck.icebreaker.level = 0
chance_without = check_raid(player, sector)["chance"]
assert chance_with < chance_without

# Heat decay
sector.current_heat = 80
sector.base_heat = 30
apply_sector_heat_decay({sector.id: sector})
assert sector.current_heat < 80
assert sector.current_heat >= 30

# Event weights shift at high heat
# Mock sectors with avg_heat > 60, verify net_crash/ice_upgrade weights increased
```

### 11.2 Integration Test Scenarios

1. **Full turn cycle**: Buy → Raid check → Advance turn → Event → Market refresh
2. **High heat spiral**: Repeated trades in Corporate Zone → heat 80+ → raid → heat 100 → game over
3. **Event chain**: ice_upgrade → global CorpSec+ → travel detected → heat spike → raid
4. **Courier job**: data_courier event → accept → travel → deliver → reward + rep

---

## 12. FUTURE EXTENSIONS

### 12.1 Event Chaining
```python
ESCALATION_CHAINS = {
    "corp_war": {
        "followup": ["mercenary_contracts", "black_market_surge"],
        "delay": (1, 3),  # weeks
        "condition": "sector_heat > 50"
    },
    "ice_upgrade": {
        "followup": ["corpsec_purge", "netrunner_hunt"],
        "delay": (2, 4),
        "condition": "global_corpsec > 70"
    },
}
```

### 12.2 Player-Driven Events
- Fixer tips (cost cred, reveal upcoming event)
- Hack CorpSec database (reduce global CorpSec, high heat)
- Instigate gang war (shift sector control, risky)

### 12.3 Sector Control Mechanics
- Player actions shift sector CorpSec/Gang presence
- Control sectors for reduced heat, better prices
- CorpSec crackdowns as counter-play

---

*Document Version: 1.0*  
*Platform: Agnostic (Python reference implementation)*  
*Last Updated: Based on neon-trader v0.1.0 codebase*