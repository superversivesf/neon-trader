# SECTORS AND PAYLOADS Reference Guide

Complete reference for Neon Trader sectors, payloads, market dynamics, and sector events.

---

## Table of Contents

1. [Sector Catalog](#sector-catalog)
2. [Sector Connections](#sector-connections)
2. [Payload Taxonomy](#payload-taxonomy)
3. [Payload Class Definitions](#payload-class-definitions)
4. [Sector-Payload Affinity Matrix](#sector-payload-affinity-matrix)
4. [Dynamic Supply & Demand](#dynamic-supply--demand)
5. [Special Sector Events](#special-sector-events)
6. [Market Mechanics Reference](#market-mechanics-reference)
7. [Data Sources](#data-sources)

---

## Sector Catalog

Neon Trader features **5 sectors**, each with unique faction alignment, danger level, specialties, and connectivity.

### 1. Kowloon Stack
| Property | Value |
|----------|-------|
| **ID** | `kowloon_stack` |
| **Name** | Kowloon Stack |
| **Description** | Neon-drenched vertical city. Wetware and stims flow through black clinics. |
| **Faction Alignment** | **Gang-dominated** (80% gang presence) |
| **Danger Level** | **Moderate** (Base Heat: 30%) |
| **CorpSec Presence** | Low (40%) |
| **Gang Presence** | **Very High (80%)** |
| **Specialties** | `wetware`, `stims` |
| **Fixer Access** | ✅ Yes |
| **Node Access** | ✅ Yes |
| **Base Heat** | 30% |
| **Current Heat** | Dynamic (30–100%) |
| **Heat Decay/Week** | -5% (decay), capped at base_heat minimum |

**Special Rules:**
- **Black Clinic Access**: Fixer access enables wetware trading without CorpSec interference
- **Stim Economy**: High gang presence means steady stim demand, but high heat on sales
- **Gang Heat**: Selling in Kowloon Stack generates +5% extra heat from gang attention
- **Travel Risk**: High gang presence = higher trace risk when jacking in/out

---

### 2. Corporate Zone
| Property | Value |
|----------|-------|
| **ID** | `corporate_zone` |
| **Name** | Corporate Zone |
| **Description** | Glass towers, armed drones, and the cleanest cred in the sprawl. High risk, high reward. |
| **Faction Alignment** | **Corporate/CorpSec-dominated** (90% CorpSec) |
| **Danger Level** | **High** (Base Heat: 60%) |
| **CorpSec Presence** | **Extreme (90%)** |
| **Gang Presence** | Low (10%) |
| **Specialties** | `exploits`, `ai_shards` |
| **Fixer Access** | ❌ No |
| **Node Access** | ✅ Yes |
| **Base Heat** | 60% |
| **Current Heat** | Dynamic (60–100%) |
| **Heat Decay/Week** | -3% (slow decay, high base) |

**Special Rules:**
- **No Fixer Access**: Cannot offload hot cargo or access fixer services
- **CorpSec Heat**: All transactions generate +10% extra heat from CorpSec surveillance
- **High-Value Tech**: Exploits and AI Shards command premium prices (1.5–2.5x base)
- **Trace Risk**: Extremely high trace risk when traveling TO/FROM Corporate Zone
- **ICE Upgrade Events**: Global CorpSec boost events hit Corporate Zone hardest (+40% CorpSec)

---

### 3. Industrial Sector
| Property | Value |
|----------|-------|
| **ID** | `industrial_sector` |
| **Name** | Industrial Sector |
| **Description** | Factories, scrap yards, and data brokers. Quiet, low margins, low heat. |
| **Faction Alignment** | **Neutral/Independent** (Balanced) |
| **Danger Level** | **Low** (Base Heat: 15%) |
| **CorpSec Presence** | Low (30%) |
| **Gang Presence** | Moderate (40%) |
| **Specialties** | `credsticks`, `wetware` |
| **Fixer Access** | ✅ Yes |
| **Node Access** | ❌ No |
| **Base Heat** | 15% |
| **Current Heat** | Dynamic (15–100%) |
| **Heat Decay/Week** | -8% (fastest decay) |

**Special Rules:**
- **Low Heat Haven**: Best sector to cool down heat; fastest decay rate
- **Credstick Hub**: Best prices for credsticks (low volatility, steady demand)
- **No Node Access**: Cannot access Darknet/Node activities here
- **Low Trace Risk**: Safest travel connections (low CorpSec + moderate gangs)
- **Volume Discounts**: High-volume credstick trades get better rates

---

### 4. Orbital Nexus
| Property | Value |
|----------|-------|
| **ID** | `orbital_nexus` |
| **Name** | Orbital Nexus |
| **Description** | Zero-g habitat, corporate research stations, and the strangest tech in the system. |
| **Faction Alignment** | **Corporate Research** (70% CorpSec) |
| **Danger Level** | **High** (Base Heat: 45%) |
| **CorpSec Presence** | High (70%) |
| **Gang Presence** | Low (20%) |
| **Specialties** | `ai_shards`, `exploits` |
| **Fixer Access** | ❌ No |
| **Node Access** | ✅ Yes |
| **Base Heat** | 45% |
| **Current Heat** | Dynamic (45–100%) |
| **Heat Decay/Week** | -4% (slow decay) |

**Special Rules:**
- **Zero-G Premium**: AI Shards and Exploits sell at 1.3–1.8x base price
- **Research Premium**: Rare AI Shards (Emergent Code Cluster) have 3x spawn chance here during events
- **No Fixer**: Cannot offload heat or access fixer services
- **Orbital Transit**: Travel to/from Orbital always incurs +15% trace risk
- **Zero-Day Events**: Zero-Day Drop events have 2x spawn chance here

---

### 5. Darknet
| Property | Value |
|----------|-------|
| **ID** | `darknet` |
| **Name** | Darknet |
| **Description** | Encrypted, decentralized, dangerous. Everything has a price. Heat is a way of life. |
| **Faction Alignment** | **Anarchist/Black Market** (No dominant faction) |
| **Danger Level** | **Extreme** (Base Heat: 80%) |
| **CorpSec Presence** | Moderate (50%) |
| **Gang Presence** | High (60%) |
| **Specialties** | `exploits`, `wetware`, `credsticks`, `stims`, `ai_shards` **(ALL CLASSES)** |
| **Fixer Access** | ✅ Yes |
| **Node Access** | ✅ Yes |
| **Base Heat** | 80% |
| **Current Heat** | Dynamic (80–100%) |
| **Heat Decay/Week** | -2% (slowest decay) |

**Special Rules:**
- **Universal Market**: **Only sector buying/selling ALL 5 payload classes**
- **Heat Capital**: Base heat 80% — always near raid threshold
- **Fixer Paradise**: Best fixer access; heat reduction services cheapest here
- **Volatility Capital**: Prices swing ±50–60% due to extreme heat volatility
- **Zero-Day Spawn**: Zero-Day Drop events *only* spawn rare exploits in Darknet
- **Global Connections**: Connected to ALL other sectors (hub topology)
- **Raid Magnet**: Highest raid probability; raids here are most devastating
- **Volume King**: Highest stock volumes for rare/legendary items

---

## Sector Connections

The sectors form a **hub-and-spoke topology** with Darknet as the central hub:

```
                    ┌─────────────────┐
                    │  ORBITAL NEXUS  │
                    │   (CorpSec 70%) │
                    └────────┬────────┘
                             │
                    ┌────────┴────────┐
                    │  CORPORATE ZONE │
                    │   (CorpSec 90%) │
                    └────────┬────────┘
                             │
        ┌────────────────────┼────────────────────┐
        │                    │                    │
┌───────┴───────┐    ┌───────┴───────┐    ┌───────┴───────┐
│ KOWLOON STACK │    │  INDUSTRIAL   │    │    DARKNET    │
│   (Gangs 80%) │    │   SECTOR      │    │  (All Classes)│
└───────┬───────┘    │  (Balanced)   │    │   (Hub: 80%)  │
        │            └───────┬───────┘    └───────┬───────┘
        │                    │                    │
        └────────────────────┴────────────────────┘
                    (All connect to Darknet)
```

### Connection Matrix

| From Sector | Connected To |
|-------------|--------------|
| **Kowloon Stack** | Industrial Sector, Darknet, Corporate Zone |
| **Corporate Zone** | Kowloon Stack, Orbital Nexus, Darknet |
| **Industrial Sector** | Kowloon Stack, Darknet |
| **Orbital Nexus** | Corporate Zone, Darknet |
| **Darknet** | **ALL** (Kowloon, Corporate, Industrial, Orbital) |

### Travel Risk Calculation

```
Trace Risk = (Source CorpSec + Target CorpSec) / 2 + (Source Heat + Target Heat) / 4
Clamped to max 95%
```

**Travel Risk Examples:**
| Route | Base Risk | Typical Heat Factor | Total Risk |
|-------|-----------|---------------------|------------|
| Industrial → Kowloon | 35% | +10% | ~45% |
| Corporate → Orbital | 80% | +25% | ~95% (capped) |
| Industrial → Darknet | 40% | +25% | ~65% |
| Orbital → Darknet | 60% | +30% | ~90% |
| Corporate → Darknet | 70% | +35% | ~95% (capped) |
| Kowloon → Darknet | 65% | +30% | ~95% (capped) |

---

## Payload Taxonomy

### Complete Payload Catalog (15 Payloads × 5 Classes)

| ID | Name | Class | Rarity | Base Price | Price Variance | Size | Heat on Sale | Volume (Stock Range) |
|----|------|-------|--------|------------|----------------|------|--------------|---------------------|
| **zero_day_exploit** | Zero-Day Exploit | exploits | legendary | ¥75,000 | ±60% | 2 | 50% | 0–2 |
| **ai_subroutine** | Autonomous Subroutine | ai_shards | uncommon | ¥18,000 | ±35% | 1 | 28% | 3–15 |
| **ai_shard_fragment** | AI Fragment | ai_shards | rare | ¥35,000 | ±50% | 1 | 40% | 1–5 |
| **rootkit_military** | Mil-Spec Rootkit | exploits | uncommon | ¥8,500 | ±30% | 1 | 18% | 3–15 |
| **logic_bomb** | Logic Bomb | exploits | uncommon | ¥6,200 | ±35% | 1 | 20% | 3–15 |
| **skillsoft_pilot** | Skillsoft: Pilot (Aerodyne) | wetware | rare | ¥15,000 | ±30% | 1 | 12% | 1–5 |
| **combat_wetware** | Combat Reflexes Wetware | wetware | uncommon | ¥7,800 | ±25% | 2 | 15% | 3–15 |
| **memory_expansion** | Memory Expansion Module | wetware | common | ¥3,200 | ±20% | 1 | 8% | 10–50 |
| **credsticks_laundered** | Pre-Laundered Credsticks (x20) | credsticks | uncommon | ¥4,500 | ±20% | 1 | 5% | 3–15 |
| **credsticks_encrypted** | Encrypted Credsticks (x50) | credsticks | common | ¥1,100 | ±15% | 1 | 3% | 10–50 |
| **corporate_scrip** | Corporate Scrip (x100) | credsticks | common | ¥800 | ±10% | 2 | 2% | 10–50 |
| **pain_editor** | Pain Editor Implant | stims | rare | ¥22,000 | ±20% | 1 | 30% | 1–5 |
| **focus_stims** | Hyper-Focus Stims | stims | uncommon | ¥1,200 | ±25% | 1 | 10% | 3–15 |
| **reflex_stims** | Reflex Booster Stims | stims | common | ¥340 | ±30% | 1 | 6% | 10–50 |
| **emergent_code** | Emergent Code Cluster | ai_shards | legendary | ¥75,000 | ±60% | 2 | 50% | 0–2 |

---

### Payload Class Definitions

| Class ID | Display Name | Color | Volatility | Description |
|----------|--------------|-------|------------|-------------|
| **exploits** | Exploits | `#ff006e` (Pink) | **High** | Zero-days, rootkits, logic bombs. High heat, high reward. |
| **wetware** | Wetware | `#00ffff` (Cyan) | **Medium** | Neural implants, skillsofts, memory. Steady demand. |
| **credsticks** | Credsticks | `#ffb000` (Amber) | **Low** | Digital currency. Low risk, low margin, high volume. |
| **stims** | Stims | `#ff0033` (Red) | **Medium** | Combat/performance drugs. Volume-driven. |
| **ai_shards** | AI Shards | `#bb00ff` (Purple) | **Extreme** | AI fragments, subroutines. Extreme volatility, extreme heat. |

---

### Payload Rarity & Stock Mechanics

| Rarity | Base Stock Range | Specialty Multiplier | Restock Variance |
|--------|------------------|---------------------|------------------|
| **Common** | 10–50 units | ×1.5 in specialty sector | ±0% |
| **Uncommon** | 3–15 units | ×1.5 in specialty sector | ±0% |
| **Rare** | 1–5 units | ×1.5 in specialty sector | ±0% |
| **Legendary** | 0–2 units | ×1.5 in specialty sector | ±0% |

**Specialty Bonus**: If payload class matches sector specialty, stock ×1.5

---

## Sector-Payload Affinity Matrix

### Price Modifier Formula
```
Final Price = Base Price × (1 ± Price_Variance) × Specialty_Mod × Heat_Mod
```

Where:
- **Specialty_Mod**: 0.85–1.15x if payload class ∈ sector.specialties (more volatile in specialty)
- **Heat_Mod**: 1.0 + (Sector_Heat / 100) × 0.2 (higher heat = higher prices)

### Affinity Matrix (Sector × Payload Class)

| Sector \ Class | Exploits | Wetware | Credsticks | Stims | AI Shards |
|----------------|:--------:|:-------:|:----------:|:-----:|:---------:|
| **Kowloon Stack** | 1.0x | **1.15x** ♦ | 1.0x | **1.15x** ♦ | 1.0x |
| **Corporate Zone** | **1.15x** ♦ | 1.0x | 1.0x | 1.0x | **1.15x** ♦ |
| **Industrial Sector** | 1.0x | **1.15x** ♦ | **1.15x** ♦ | 1.0x | 1.0x |
| **Orbital Nexus** | **1.15x** ♦ | 1.0x | 1.0x | 1.0x | **1.15x** ♦ |
| **Darknet** | **1.15x** ♦ | **1.15x** ♦ | **1.15x** ♦ | **1.15x** ♦ | **1.15x** ♦ |

**Legend**: ♦ = Sector Specialty (price volatility 0.85–1.15x, stock ×1.5)

### Effective Price Ranges by Sector Specialty

| Payload | Base | Kowloon (Wetware/Stims) | Corporate (Exploits/AI) | Industrial (Wetware/Cred) | Orbital (Exploits/AI) | Darknet (All) |
|---------|------|------------------------|------------------------|--------------------------|----------------------|---------------|
| Zero-Day Exploit | 75,000 | 63,750–86,250 | **63,750–86,250** ♦ | 63,750–86,250 | **63,750–86,250** ♦ | **63,750–86,250** ♦ |
|♦
| AI Fragment | 35,000 | 29,750–40,250 | **29,750–40,250** ♦ | 29,750–40,250 | **29,750–40,250** ♦ | **29,750–40,250** ♦
| Combat Wetware | 7,800 | **6,630–8,970** ♦ | 6,630–8,970 | **6,630–8,970** ♦ | 6,630–8,970 | **6,630–8,970** ♦
| Reflex Stims | 340 | **289–391** ♦ | 289–391 | 289–391 | 289–391 | **289–391** ♦
| Encrypted Credsticks | 1,100 | 935–1,265 | 935–1,265 | **935–1,265** ♦ | 935–1,265 | **935–1,265** ♦

---

## Dynamic Supply & Demand

### Price Generation (Per Turn/Per Sector)

```python
def generate_market_prices(payloads, sector):
    for payload in payloads.values():
        # 1. Base variance
        variance = random.uniform(-payload.price_variance, payload.price_variance)
        
        # 2. Specialty modifier (only if payload class in sector.specialties)
        specialty_mod = 1.0
        if payload.payload_class in sector.specialties:
            specialty_mod = random.uniform(0.85, 1.15)  # MORE volatile in specialty
        
        # 3. Heat modifier (higher heat = higher prices)
        heat_mod = 1.0 + (sector.current_heat / 100.0) * 0.2
        
        # 4. Final price
        payload.current_price = max(1, int(payload.base_price * (1 + variance) * specialty_mod * heat_mod))
        
        # 5. Stock refresh
        base_stock = payload.generate_stock()  # Based on rarity
        if payload.payload_class in sector.specialties:
            base_stock = int(base_stock * 1.5)  # 50% more stock in specialty
        payload.stock = base_stock
```

### Stock Replenishment (Per Turn)
- Each turn/week, ALL payloads refresh stock
- Specialty sectors get 1.5× base stock for matching classes
- Rare/Legendary items may have 0 stock (especially outside specialty sectors)
- Darknet gets full stock for ALL classes every turn

### Heat Impact on Prices
| Sector Heat | Price Multiplier | Effect |
|-------------|-----------------|--------|
| 0–20% | 1.00–1.04x | Baseline |
| 21–40% | 1.04–1.08x | Elevated |
| 41–60% | 1.08–1.12x | High |
| 61–80% | 1.12–1.16x | Very High |
| 81–100% | 1.16–1.20x | Extreme (Darknet baseline) |

### Volume Dynamics by Sector

| Sector | Total Weekly Volume | Specialty Volume | Non-Specialty Volume |
|--------|---------------------|------------------|---------------------|
| **Darknet** | **Highest** (all classes) | N/A (all specialty) | N/A |
| **Kowloon** | High (Wetware/Stims) | 2× | 1× |
| **Corporate** | High (Exploits/AI) | 2× | 1× |
| **Orbital** | Medium (Exploits/AI) | 2× | 1× |
| **Industrial** | Medium (Wetware/Cred) | 2× | 1× |

---

## Special Sector Events

Events trigger at the **start of each turn** (week) based on game state.

### Event Types & Effects

| Event | Trigger Weight | Description | Market Effects | Heat Change | Special |
|-------|----------------|-------------|----------------|-------------|---------|
| **Market Spike** | 20% (base) | `{class} prices surging in {sector}!` | Target class ×2.5 | +10% sector | Random class |
| **Corporate War** | 15% (base) | `{sector_a}` and `{sector_b}` in conflict. Exploits volatile. | Exploits ×1.8, AI Shards ×1.5 | +15% both sectors | Two random sectors |
| **Net Crash** | 10% (base) | Digital payloads crashing. Hardware premium. | Exploits ×0.4, AI Shards ×0.3, Wetware ×1.5, Stims ×1.3 | -5% global | Global |
| **ICE Upgrade** | 10% (base) | CorpSec deployed new countermeasures globally. | None (price) | +20% global | CorpSec +20% all sectors |
| **Zero-Day Drop** | 5% (base) | Rare exploit surfaced in Darknet. | Exploits ×0.6 (cheaper) | +5% Darknet | Spawns 1× Zero-Day Exploit in Darknet |
| **Data Courier** | 15% (base) | Transport `{payload}` from `{origin}` to `{dest}`. Pays ¥{reward}. | Courier job spawned | 0% | Courier mission |

### Dynamic Weight Adjustments

```python
weights = [20, 15, 10, 10, 5, 15]  # Base weights

# High heat (>60% avg) → more crashes & ICE upgrades
if avg_heat > 60:
    weights[2] += 10  # Net Crash
    weights[3] += 10  # ICE Upgrade

# Late game (>week 30) → more Zero-Day Drops
if week > 30:
    weights[4] += 10
```

### Event Impact by Sector

| Event | Most Affected Sectors |
|-------|----------------------|
| **Market Spike** | Sector where event triggers (random) |
| **Corporate War** | Two random sectors (often Corporate + Orbital) |
| **Net Crash** | **All sectors** (global digital crash) |
| **ICE Upgrade** | **All sectors** (global CorpSec boost) |
| **Zero-Day Drop** | **Darknet only** (exclusive spawn) |
| **Data Courier** | Origin → Destination sectors |

---

## Market Mechanics Reference

### Price Calculation Pipeline

```
Base Price
    │
    ├──► Apply Variance (±price_variance) ──────► Base × (1 ± variance)
    │
    ├──► Apply Specialty Modifier ──────────────► ×0.85–1.15 (if specialty)
    │         (more volatile in specialty sectors)
    │
    ├──► Apply Heat Modifier ───────────────────► ×(1.0 + heat/100 × 0.2)
    │         (higher heat = higher prices)
    │
    └──► Apply Event Modifiers ─────────────────► ×event_modifier (if active)
    
    │
    ▼
Final Current Price (integer, min 1)
```

### Heat on Transaction

```python
def calculate_heat_gain(player, sector, payload, quantity, is_selling):
    base_heat = payload.heat_on_sale * quantity
    
    # Sector modifiers
    if sector.id == "kowloon_stack":
        base_heat = int(base_heat * 1.05)  # +5% gang attention
    elif sector.id == "corporate_zone":
        base_heat = int(base_heat * 1.10)  # +10% CorpSec surveillance
    elif sector.id == "darknet":
        base_heat = int(base_heat * 0.9)   # -10% (fixer networks)
    
    # Player heat level modifier
    if player.heat > 50:
        base_heat = int(base_heat * 1.2)
    elif player.heat > 75:
        base_heat = int(base_heat * 1.5)
    
    return base_heat
```

### Raid Trigger Thresholds

| Player Heat | Sector Heat | Raid Chance |
|-------------|-------------|-------------|
| < 30% | Any | 0% |
| 30–50% | > 50% | 10%/turn |
| 51–75% | > 40% | 25%/turn |
| 76–90% | > 30% | 50%/turn |
| > 90% | Any | 90%/turn |

### Raid Consequences

| Raid Severity | Cargo Lost | Cred Lost | Heat Gained |
|---------------|------------|-----------|-------------|
| Minor | 1–2 items | 10–20% | +10% |
| Major | 3–5 items | 30–50% | +25% |
| Catastrophic | All cargo | 60–80% | +50% |

---

## Faction Affinity & Payload Legality

| Payload Class | CorpSec Stance | Gang Stance | Fixer Stance | Typical Legality |
|---------------|----------------|-------------|--------------|------------------|
| **Exploits** | **Illegal** (High priority) | Tolerated | **High Demand** | Illegal |
| **Wetware** | Regulated (Licensed) | **High Demand** | High Demand | Grey Market |
| **Credsticks** | **Legal** (Monitored) | Accepted | **High Volume** | Legal/Grey |
| **Stims** | Regulated (Medical) | **High Demand** | Medium Demand | Grey/Illegal |
| **AI Shards** | **Illegal** (Kill-on-sight) | Feared/Valued | **Extreme Demand** | Highly Illegal |

### Faction Heat Multipliers on Sale

| Sector | CorpSec Mult | Gang Mult | Fixer Discount |
|--------|-------------|-----------|----------------|
| Corporate Zone | **1.5×** | 0.5× | N/A |
| Kowloon Stack | 0.8× | **1.3×** | 0.9× |
| Industrial | 1.0× | 1.0× | 0.95× |
| Orbital Nexus | **1.3×** | 0.7× | N/A |
| Darknet | 1.1× | 1.1× | **0.7×** (best fixer rates) |

---

## Data Sources

This document is derived from the following source files:

| File | Path | Description |
|------|------|-------------|
| **Sectors** | `neon_trader/data/sectors.json` | Sector definitions, connections, base stats |
| **Payloads** | `neon_trader/data/payloads.json` | Payload definitions, classes, pricing, rarity |
| **Sector Model** | `neon_trader/models/sector.py` | Sector class, heat mechanics, travel risk |
| **Payload Model** | `neon_trader/models/payload.py` | Payload class, pricing, stock generation |
| **Market Logic** | `neon_trader/utils/market_logic.py` | Price generation, events, modifiers |
| **Heat System** | `neon_trader/utils/heat_system.py` | Heat calculation, decay, raid mechanics |
| **Market Model** | `neon_trader/models/market.py` | Market listing, buy/sell operations |

---

## Quick Reference Tables

### Best Sector for Each Payload Class

| Payload Class | Best Buy Sector | Best Sell Sector | Reason |
|---------------|----------------|------------------|--------|
| **Exploits** | Industrial/Kowloon (low heat) | Corporate/Orbital/Darknet | High CorpSec demand |
| **Wetware** | Industrial/Corporate | Kowloon/Darknet | Gang/clinic demand |
| **Credsticks** | Any (low variance) | Industrial/Darknet | Volume + fixer access |
| **Stims** | Corporate/Orbital | Kowloon/Darknet | Street demand |
| **AI Shards** | Industrial/Kowloon | Orbital/Corporate/Darknet | Research premium |

### Optimal Trade Routes (Low Risk → High Reward)

| Route | Buy Sector | Sell Sector | Payload Class | Risk | Margin |
|-------|-----------|-------------|---------------|------|--------|
| **Safe Cred Run** | Industrial | Darknet | Credsticks | Low | 15–25% |
| **Wetware Pipeline** | Industrial | Kowloon | Wetware | Low | 20–35% |
| **Stim Run** | Corporate | Kowloon | Stims | Medium | 30–50% |
| **Exploit Flip** | Industrial | Corporate | Exploits | High | 50–100% |
| **AI Shard Run** | Industrial | Orbital | AI Shards | Very High | 80–200% |
| **Darknet Arbitrage** | Any | Darknet | Any (arb) | Extreme | Variable |

---

## Version & Maintenance

| Version | Date | Author | Notes |
|---------|------|--------|-------|
| 1.0 | 2025-06-15 | WildWolf (Swarm Agent) | Initial creation from sectors.json & payloads.json |

---

*Document generated from Neon Trader source data. For game mechanics implementation details, see `neon_trader/utils/market_logic.py` and `neon_trader/utils/heat_system.py`.*