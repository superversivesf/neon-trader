# ECONOMY AND PROGRESSION DESIGN DOCUMENT
**Neon Trader** — Platform-Agnostic Specification  
Version: 1.0.0 | Status: Design Baseline

---

## TABLE OF CONTENTS
1. [Credit Economy](#1-credit-economy)
2. [Payload Price Curves](#2-payload-price-curves)
3. [Market Volatility](#3-market-volatility)
4. [Upgrade Tree](#4-upgrade-tree)
5. [Reputation Tiers](#5-reputation-tiers)
6. [Unlock Progression](#6-unlock-progression)
7. [Prestige / New Game+](#7-prestige--new-game)
8. [Economy Balance Targets](#8-economy-balance-targets)
9. [Inflation Control](#9-inflation-control)

---

## 1. CREDIT ECONOMY

### 1.1 Core Loop
```
TRAVEL → BUY (low) → TRAVEL → SELL (high) → UPGRADE → REPEAT
  ↓          ↓          ↓          ↓          ↓
  1wk      payload   1wk       profit     credits
```

Every meaningful action consumes **1 week** (turn). Game ends at **week 52** (victory: ¥10M) or **100 heat** (game over).

### 1.2 Credit Sources (Earning)

| Source | Base Range | Scaling | Notes |
|--------|-----------|---------|-------|
| **Payload Arbitrage** | ¥500–¥50k/run | Exponential with deck upgrades | Core loop; buy low in specialty sectors, sell high elsewhere |
| **Courier Contracts** | ¥3k–¥30k | Linear with fixer rep tier | Guaranteed income; heat cost varies |
| **Loan Principal** | ¥10k–¥500k | — | Debt, not income; weekly interest payments |
| **Auto-Fence (emergency)** | 50%→20% of value | Deck upgrade: AutoFence level | Safety valve; always available at discount |
| **Salvage/Raid Recovery** | Variable | — | Rare; from CorpSec raids or events |

### 1.3 Credit Sinks (Spending)

| Sink | Cost Curve | Purpose |
|------|-----------|---------|
| **Payload Purchase** | Base × variance × heat | Core inventory investment |
| **Deck Upgrades** | Exponential (×2/level) | Permanent capability increase |
| **Ship Upgrades** | Exponential (×2.5/level) | Hull, cargo, engine, shield, weapon, sensor |
| **Fuel/Travel** | ¥500–¥5k/jump | Distance × sector heat |
| **Loan Interest** | 5–15%/week | Debt servicing |
| **Bribes/Heat Reduction** | ¥1k–¥50k | Emergency heat management |
| **Market Scanner Intel** | ¥2k–¥20k | Price visibility (scanner level) |

### 1.4 Sink Classification

| Type | Examples | Design Purpose |
|------|----------|----------------|
| **Productive** | Deck upgrades, cargo expansion | Increase earning capacity |
| **Maintenance** | Fuel, loan interest, bribes | Ongoing operational cost |
| **Risk Mitigation** | Stealth, trace reducer, auto-fence | Reduce variance/loss |
| **Progression Gate** | Scanner, quantum drive | Unlock new content |
| **Luxury/Prestige** | Cosmetic, max-level upgrades | Post-victory goals |

---

## 2. PAYLOAD PRICE CURVES

### 2.1 Price Formula
```
CurrentPrice = BasePrice × (1 + Variance) × SpecialtyMod × HeatMod × EventMod
```

| Component | Range | Source |
|-----------|-------|--------|
| **BasePrice** | ¥800–¥75,000 | Static per payload (payloads.json) |
| **Variance** | ±PriceVariance | Random per-turn per-sector |
| **SpecialtyMod** | 0.85–1.15 (specialty), 1.0 (other) | Sector specialties match payload class |
| **HeatMod** | 1.0–1.2 | Sector current_heat / 100 × 0.2 |
| **EventMod** | 0.3–2.5x | Active random events |

### 2.2 Payload Classes & Base Price Tiers

| Class | Volatility | Base Range | Typical Margin | Risk Profile |
|-------|------------|------------|----------------|--------------|
| **Credsticks** | Low (±10–15%) | ¥800–¥4,500 | 15–25% | Safe, low margin, low heat |
| **Stims** | Medium (±25–30%) | ¥340–¥22,000 | 30–50% | Volume play, steady demand |
| **Wetware** | Medium (±20–35%) | ¥3,200–¥15,000 | 40–60% | Specialty-dependent |
| **Exploits** | High (±30–40%) | ¥6,200–¥12,000 | 50–100% | High heat, CorpSec attention |
| **AI Shards** | Extreme (±35–60%) | ¥18,000–¥75,000 | 80–200% | Volatile, legendary rare |

### 2.3 Price Curve Design Principles

1. **Rarity ≠ Linear Price** — Legendary (Emergent Code Cluster) is 75k base but 60% variance = ¥30k–¥120k swing
2. **Size Matters** — 2-slot payloads need ~2× margin to equal 1-slot efficiency
3. **Heat Cost Internalized** — High-heat payloads (AI Shards: 40–50 heat/sale) require price premium
4. **Class Volatility Identity** — Each class has distinct variance "personality" for player learning

### 2.4 Stock Generation
```
BaseStock = RarityWeight × (1 + HeatFactor) × SpecialtyBoost
```
- **Common**: 8–15 units
- **Uncommon**: 3–8 units  
- **Rare**: 1–4 units
- **Legendary**: 0–1 units (event-spawned)
- Specialty sectors: ×1.5 stock for matching classes

---

## 3. MARKET VOLATILITY

### 3.1 Variance Sources (Stacking)

| Source | Magnitude | Frequency | Mitigation |
|--------|-----------|-----------|------------|
| **Base Variance** | ±10–60% | Every turn | Scanner reveals trend |
| **Sector Specialty** | ±15% | Persistent | Learn sector mappings |
| **Sector Heat** | +0–20% | Dynamic | Stealth upgrade, gang rep |
| **Random Events** | -60% to +150% | ~1/turn | Diversify cargo, scanner |
| **Global Events** | Class-wide | Rare | Faction rep, intel |

### 3.2 Random Events (Market Modifiers)

| Event | Trigger Weight | Classes Affected | Price Multiplier | Heat Δ | Notes |
|-------|------------------|------------------|--------|-------|
| **Market Spike** | 20% | Target class 2.5x | +10% | Single class surge |
| **Corp War** | 15% | Exploits 1.8x, AI 1.5x | +15% | Mid-late game |
| **Net Crash** | 10% | Exploits 0.4x, AI 0.3x, Wetware 1.5x, Stims 1.3x | -5% | Digital crash, hardware wins |
| **ICE Upgrade** | 10% | None | +20% | Global CorpSec +20% |
| **Zero-Day Drop** | 5% | Exploits 0.6x | +5% | Spawns rare in Darknet |
| **Data Courier** | 15% | None | 0 | Contract opportunity |

**Weight Adjustments:**
- Avg sector heat > 60: +10 Net Crash, +10 ICE Upgrade
- Week > 30: +10 Zero-Day Drop

### 3.3 Volatility Control Knobs

| Knob | Effect | Default | Tuning Range |
|------|--------|---------|--------------|
| `base_variance` | Payload PriceVariance | Per payload | 0.05–0.7 |
| `specialty_boost` | SpecialtyMod range | 0.85–1.15 | 0.7–1.3 |
| `heat_volatility` | HeatMod coefficient | 0.2 | 0.1–0.5 |
| `event_weight` | Event frequency | 1/turn | 0.5–2/turn |
| `event_magnitude` | Class modifier range | 0.3–2.5x | 0.2–3.0x |

---

## 4. UPGRADE TREE

### 4.1 Deck Upgrades (Cyberdeck Systems)

| Upgrade | Max Level | Base Cost | Cost Multiplier | Effect per Level | Unlock Requirement |
|---------|-----------|-----------|-----------------|------------------|-------------------|
| **ICEbreaker** | 5 | ¥15,000 | 2.0x | -15% raid chance | — |
| **Stealth Protocols** | 5 | ¥12,000 | 2.0x | -15% heat gain | — |
| **Cargo Expansion** | 4 | ¥8,000 | 2.0x | +10 cargo slots (base 20) | — |
| **Trace Reducer** | 5 | ¥10,000 | 2.0x | -15% jack-in trace risk | — |
| **Market Scanner** | 5 | ¥20,000 | 2.0x | +1 sector visible ahead | — |
| **Auto-Fence** | 3 | ¥25,000 | 2.0x | Discount 50%→20% (per level -10%) | — |

**Total Max Cost (all to max):** ~¥1.2M credits

### 4.2 Ship Upgrades (Physical Ship)

| Upgrade | Category | Max Level | Base Cost | Cost Multiplier | Effect |
|---------|----------|-----------|-----------|-----------------|--------|
| **Reinforced Hull** | HULL | 5 | ¥15,000 | 2.5x | +50 max hull integrity/level |
| **Expanded Cargo Bay** | CARGO | 4 | ¥12,000 | 2.5x | +30 cargo capacity/level |
| **Quantum Drive** | ENGINE | 4 | ¥25,000 | 2.5x | +2 jump range, -20% fuel/level |
| **Military Shield** | SHIELD | 4 | ¥30,000 | 2.5x | +100 shield/level |
| **Plasma Cannon** | WEAPON | 3 | ¥20,000 | 2.5x | +2 weapon slots, +50% dmg/level |
| **Long-Range Scanner** | SENSOR | 5 | ¥8,000 | 2.5x | +5 scan range, reveals heat/level |

**Total Max Cost (all to max):** ~¥2.8M credits

### 4.3 Software Upgrades (Deck Modules — Future Expansion)

| Module | Cost | Effect | Prerequisite |
|--------|------|--------|--------------|
| **Price Prediction AI** | ¥50k | 70% accuracy next-turn prices | Scanner Lv.3 |
| **Heat Masking Suite** | ¥40k | -25% heat from all sources | Stealth Lv.3 |
| **Cargo Compression** | ¥30k | Payload size -1 (min 1) | Cargo Lv.3 |
| **CorpSec Spoofer** | ¥60k | -30% trace risk, -10% raid | Trace Lv.3 + ICE Lv.3 |
| **Auto-Trader** | ¥100k | Auto buy/sell at thresholds | All deck Lv.2 |

### 4.4 Upgrade Cost Progression Formula
```
Cost(level) = BaseCost × (Multiplier ^ level)
```

| Level | 2.0x Multiplier | 2.5x Multiplier |
|-------|-----------------|-----------------|
| 1 | 1× | 1× |
| 2 | 2× | 2.5× |
| 3 | 4× | 6.25× |
| 4 | 8× | 15.6× |
| 5 | 16× | 39× |

---

## 5. REPUTATION TIERS

### 5.1 Faction System
Four factions, each tracked -100 to +100 (CorpSec inverted: negative = good).

| Faction | Positive = | Negative = | Primary Interaction |
|---------|------------|------------|---------------------|
| **Fixer** | Trusted | Blacklisted | Loans, contracts, intel, laundry |
| **Gangs** | Protected | Hunted | Heat reduction, sector access, raid protection |
| **CorpSec** | Cooperative | Hostile | Trace risk, raid chance, sector access |
| **Netrunners** | Allied | Targeted | Market intel, exploit prices, node access |

### 5.2 Tier Thresholds (All Factions)

| Tier | Range | Label | Benefits |
|------|-------|-------|----------|
| **0** | < 0 | Hostile / Blacklisted | Maximum penalties |
| **1** | 0–24 | Neutral | Baseline |
| **2** | 25–49 | Friendly | Minor bonuses |
| **3** | 50–74 | Respected | Significant bonuses |
| **4** | 75–100 | Allied / Untouchable | Maximum bonuses |

### 5.3 Per-Faction Tier Benefits

#### Fixer (Loan Shark / Contract Broker)
| Tier | Loan Interest | Contract Reward | Intel Cost | Laundry Rate |
|------|---------------|-----------------|------------|--------------|
| 0 | 15%/wk | ×0.8 | ×2.0 | N/A |
| 1 | 13%/wk | ×1.0 | ×1.5 | N/A |
| 2 | 11%/wk | ×1.2 | ×1.2 | 80% |
| 3 | 9%/wk | ×1.4 | ×1.0 | 90% |
| 4 | 7%/wk | ×1.6 | ×0.8 | 95% |

#### Gangs (Street Protection)
| Tier | Heat Reduction | Raid Protection | Sector Access | Bribe Cost |
|------|----------------|-----------------|---------------|------------|
| 0 | 0% | 0% | Blocked: Industrial, Kowloon | ×2.0 |
| 1 | 5% | 5% | — | ×1.5 |
| 2 | 10% | 10% | Industrial free | ×1.2 |
| 3 | 15% | 15% | Kowloon free | ×1.0 |
| 4 | 20% | 25% | All gang sectors free | ×0.8 |

#### CorpSec (Law Enforcement — Negative is Good)
| Tier (CorpSec) | Trace Risk | Raid Chance | Sector Access | Fine Reduction |
|----------------|------------|-------------|---------------|----------------|
| 0 (>0) | +0% | +0% | Corporate, Orbital blocked | ×1.0 |
| 1 (0 to -24) | -5% | -5% | — | ×0.9 |
| 2 (-25 to -49) | -10% | -10% | Corporate free | ×0.7 |
| 3 (-50 to -74) | -15% | -20% | Orbital free | ×0.5 |
| 4 (-75 to -100) | -25% | -35% | All corp sectors free | ×0.3 |

#### Netrunners (Information Brokers)
| Tier | Scanner Bonus | Exploit Price | Node Access | Event Warning |
|------|---------------|---------------|-------------|---------------|
| 0 | +0 | ×1.0 | Blocked: Darknet, Orbital | None |
| 1 | +1 range | ×0.95 | — | 1-turn warning |
| 2 | +1 range | ×0.9 | Darknet free | 2-turn warning |
| 3 | +2 range | ×0.85 | Orbital free | 3-turn + type |
| 4 | +3 range | ×0.8 | All nodes free | Full event preview |

### 5.4 Reputation Gain/Loss Sources

| Action | Fixer | Gangs | CorpSec | Netrunners |
|--------|-------|-------|---------|------------|
| Complete courier | +5 | +2 | 0 | 0 |
| Sell to faction contact | +10 | +5 | -5 | +3 |
| CorpSec raid survived | -5 | +10 | -15 | +2 |
| Pay loan on time | +3 | 0 | 0 | 0 |
| Default on loan | -20 | -5 | +5 | -3 |
| Buy intel | +2 | 0 | 0 | +5 |
| Hack node successfully | 0 | 0 | -10 | +10 |
| Get caught hacking | 0 | -5 | +15 | -10 |
| Bribe official | -3 | -2 | -10 | 0 |

---

## 6. UNLOCK PROGRESSION

### 6.1 Sector Unlocks (Reputation-Gated)

| Sector | Base Access | Fixer | Gangs | CorpSec | Netrunners |
|--------|-------------|-------|-------|---------|------------|
| Kowloon Stack | ✓ Start | — | Tier 2 | — | — |
| Industrial | ✓ Start | — | Tier 1 | — | — |
| Corporate Zone | Locked | — | — | Tier 2 | — |
| Orbital Nexus | Locked | — | — | Tier 3 | Tier 3 |
| Darknet | Locked | Tier 2 | Tier 3 | — | Tier 2 |

### 6.2 Payload Class Unlocks (Reputation-Gated)

| Class | Base Access | Fixer | Gangs | CorpSec | Netrunners |
|-------|-------------|-------|-------|---------|------------|
| Credsticks | ✓ All | — | — | — | — |
| Stims | ✓ All | — | — | — | — |
| Wetware | ✓ Kowloon, Industrial | Tier 1 | Tier 1 | — | — |
| Exploits | Locked | Tier 2 | — | — | Tier 1 |
| AI Shards | Locked | Tier 3 | — | Tier 3 | Tier 2 |

### 6.3 Fixer Services Unlocks

| Service | Unlock |
|---------|--------|
| Basic Loans | Start |
| Laundering (50%→80%) | Fixer Tier 2 |
| Market Intel (price trends) | Fixer Tier 2 |
| Rare Payload Tips | Fixer Tier 3 |
| CorpSec Blind Eye (heat -10) | Fixer Tier 4 |
| Zero-Interest Bridge Loan | Fixer Tier 4 |

### 6.4 Progression Milestones (Week-Based)

| Week | Milestone | Design Purpose |
|------|-----------|----------------|
| 1–4 | **Tutorial Phase** | Learn basics, first upgrades, low heat |
| 5–12 | **Expansion Phase** | Unlock Corporate/Orbital, deck Lv.2–3 |
| 13–24 | **Mid-Game** | Max 1–2 deck upgrades, gang/CorpSec rep management |
| 25–36 | **Optimization** | Scanner Lv.3+, efficient routes, loan leverage |
| 37–44 | **Endgame Push** | AI Shards accessible, ¥1M+ runs |
| 45–52 | **Victory Lap** | Prestige prep, max upgrades |

---

## 7. PRESTIGE / NEW GAME+

### 7.1 Prestige Trigger
- **Requirement**: Victory (¥10M credits) at week ≤ 52
- **Alternative**: "True Victory" — ¥10M at week ≤ 40 with all deck upgrades maxed

### 7.2 Prestige Rewards (Per Level)

| Prestige Level | Credits Bonus | Unlock | Cosmetic |
|----------------|---------------|--------|----------|
| 1 | +¥50,000 start | **Legacy Ship**: +10% all earnings | Ship skin: "Veteran" |
| 2 | +¥100,000 start | **Fixer Network**: Start at Tier 1 all factions | Ship skin: "Elite" |
| 3 | +¥200,000 start | **Advanced Scanner**: Start at Scanner Lv.2 | Ship skin: "Legend" |
| 4 | +¥400,000 start | **Quantum Drive Mk.0**: Start with Engine Lv.1 | Ship skin: "Mythic" |
| 5+ | ×1.5 multiplier | **Prestige Shop**: Unique upgrades | Title: "Ghost of the Sprawl" |

### 7.3 New Game+ Mechanics

| Modifier | P1 | P2 | P3 | P4 | P5+ |
|----------|----|----|----|----|-----|
| **Max Weeks** | 52 | 50 | 48 | 46 | 44 |
| **Base Heat** | +5 | +10 | +15 | +20 | +25 |
| **CorpSec Presence** | +5% | +10% | +15% | +20% | +25% |
| **Event Weight** | ×1.1 | ×1.2 | ×1.3 | ×1.4 | ×1.5 |
| **Loan Interest Floor** | 5% | 6% | 7% | 8% | 10% |
| **Payload Variance** | +5% | +10% | +15% | +20% | +25% |

### 7.4 Prestige Shop (Unlocks at P3+)

| Item | Prestige Cost | Effect |
|------|---------------|--------|
| **Neural Interface** | 1 | Deck upgrades cost -20% |
| **Smuggler's Hold** | 2 | Cargo capacity +20, no scan detection |
| **Ghost Protocol** | 3 | Heat gain -30%, but no CorpSec rep gain |
| **Market Oracle** | 4 | Scanner shows 2-turn forecast |
| **Singularity Core** | 5 | One free max-level upgrade per run |

---

## 8. ECONOMY BALANCE TARGETS

### 8.1 Target Earning Curves

| Week | Target Credits | Target Cargo Value | Target Deck Level (Avg) |
|------|----------------|--------------------|-------------------------|
| 1 | ¥50,000 (start) | ¥0 | 0 |
| 4 | ¥80,000 | ¥30,000 | 1.0 |
| 8 | ¥150,000 | ¥80,000 | 1.5 |
| 12 | ¥300,000 | ¥150,000 | 2.0 |
| 16 | ¥600,000 | ¥300,000 | 2.5 |
| 20 | ¥1,200,000 | ¥500,000 | 3.0 |
| 26 | ¥2,500,000 | ¥1,000,000 | 3.5 |
| 32 | ¥5,000,000 | ¥2,000,000 | 4.0 |
| 40 | ¥8,000,000 | ¥3,500,000 | 4.5 |
| 52 | ¥10,000,000+ | ¥5,000,000+ | 5.0 |

### 8.2 Run Profit Targets

| Run Type | Investment | Expected Profit | ROI | Risk |
|----------|------------|-----------------|-----|------|
| **Safe Loop** (Credsticks, low heat) | ¥20k | ¥3–5k | 15–25% | Very Low |
| **Standard** (Wetware/Stims, specialty) | ¥50k | ¥15–25k | 30–50% | Low |
| **Aggressive** (Exploits, med heat) | ¥100k | ¥50–100k | 50–100% | Medium |
| **High Roller** (AI Shards, high heat) | ¥300k | ¥200–500k | 60–160% | High |
| **Courier** (Fixer contract) | ¥0 | ¥5–30k | ∞ | Variable |

### 8.3 Upgrade Affordability Gates

| Upgrade | Target Week | Credits Required | % of Net Worth |
|---------|-------------|------------------|----------------|
| First deck upgrade (any) | 3–4 | ¥8–15k | 15–25% |
| Cargo Expansion Lv.2 | 8–10 | ¥24k | 15% |
| Scanner Lv.3 | 15–18 | ¥140k | 25% |
| First ship upgrade | 12–15 | ¥30–50k | 20% |
| Quantum Drive Lv.2 | 22–26 | ¥150k | 20% |
| All deck maxed | 35–40 | ~¥1.2M | 25% |
| All ship maxed | 45–50 | ~¥2.8M | 35% |

### 8.4 Heat Economy Targets

| Playstyle | Avg Heat/Week | Heat Cap Week | Mitigation Investment |
|-----------|---------------|---------------|----------------------|
| **Cautious** (Credsticks, high stealth) | 3–5 | Never | Stealth Lv.3 by wk 12 |
| **Balanced** (Mixed, sector specialty) | 8–12 | 35–40 | Stealth Lv.2 + Gang Tier 2 |
| **Aggressive** (Exploits/AI, Darknet) | 15–25 | 20–25 | Stealth Lv.4 + ICE Lv.3 + bribes |
| **Speedrun** (Max profit, min weeks) | 20–30 | 15–20 | All mitigations maxed |

---

## 9. INFLATION CONTROL

### 9.1 Inflation Sources

| Source | Mechanism | Severity |
|--------|-----------|----------|
| **Compound Interest** | Loans at 15%/wk = 8× in 12 weeks | Critical |
| **Exponential Upgrade Costs** | ×2–2.5/level | High (late game) |
| **Event Cascades** | Corp War → high prices → more credits → more upgrades | Medium |
| **Auto-Fence Abuse** | Emergency sell → buy back cheaper | Low (discount penalty) |
| **Prestige Credit Injection** | +¥50k–400k start per level | Medium (NG+ only) |

### 9.2 Control Mechanisms

#### Hard Caps
| System | Cap | Effect |
|--------|-----|--------|
| **Max Week** | 52 (44 at P5+) | Hard time limit |
| **Max Heat** | 100 | Game over |
| **Max Cargo** | 60 slots (deck 20 + ship 4×30) | Physical limit |
| **Max Scanner** | 5 sectors | Visibility limit |
| **Loan Limit** | 3 concurrent, ¥500k each | Debt ceiling |

#### Soft Caps (Diminishing Returns)
| System | Curve | Intent |
|--------|-------|--------|
| **Upgrade Cost** | Exponential (×2–2.5) | Prevents infinite scaling |
| **Rep Gain** | Logarithmic: tier n requires 25×n points | Slows max-tier rush |
| **Event Weights** | Increase with heat/week | Late-game volatility |
| **Stock Limits** | Per-payload, per-sector | Supply constraint |

#### Active Sinks (Credit Drains)
| Sink | Trigger | Amount | Frequency |
|------|---------|--------|-----------|
| **Loan Interest** | Weekly | 5–15% principal | Every week |
| **Fuel** | Travel | ¥500–5,000 | Every jump |
| **Bribes** | Heat > 70 | ¥5k–50k | As needed |
| **Raid Losses** | CorpSec raid | 10–50% cargo value | Random |
| **Loan Default** | Missed payment | +10 heat | Per loan |

### 9.3 Inflation Monitoring Metrics

**Track per run (telemetry):**
- `credits_earned_per_week` — should follow target curve ±20%
- `credits_spent_upgrades / credits_earned` — target 30–40%
- `heat_gained_per_week` — should trend up, not flat
- `loan_defaults` — should be <2/run for balanced play
- `victory_week` — target 40–52 (lower = difficulty issue)

**Balance Adjustment Levers:**
| Metric Off-Target | Adjustment |
|-------------------|------------|
| Credits too high early | Increase base upgrade costs 10–20% |
| Credits too low late | Increase payload base prices 10%, reduce event weight |
| Heat too low | Increase base heat per sale, reduce stealth effectiveness |
| Heat too high | Increase gang protection, add heat decay events |
| Victory too easy | Reduce max weeks, increase CorpSec presence |
| Victory too hard | Increase specialty modifiers, add more courier contracts |

### 9.4 Prestige Inflation Control

| Prestige | Credit Injection | Countermeasure |
|----------|------------------|----------------|
| P1 | +¥50k | +2 base heat, -2 max weeks |
| P2 | +¥100k | +5 base heat, -2 max weeks, +5% CorpSec |
| P3 | +¥200k | +10 base heat, -2 max weeks, +10% CorpSec, +10% event weight |
| P4 | +¥400k | +15 base heat, -2 max weeks, +15% CorpSec, +20% event weight |
| P5+ | ×1.5 start cred | +25 base heat, -8 max weeks, +25% CorpSec, +50% event weight, loan floor 10% |

**Design Rule**: Every prestige credit bonus is matched by ≥2 difficulty increases.

---

## APPENDIX: QUICK REFERENCE TABLES

### A.1 Payload Quick Reference
| ID | Class | Base | Variance | Size | Heat | Specialty Sectors |
|----|-------|------|----------|------|------|-------------------|
| credsticks_encrypted | Credsticks | 1,100 | 15% | 1 | 3 | Industrial |
| credsticks_laundered | Credsticks | 4,500 | 20% | 1 | 5 | Industrial |
| corporate_scrip | Credsticks | 800 | 10% | 2 | 2 | Industrial |
| reflex_stims | Stims | 340 | 30% | 1 | 6 | Kowloon |
| focus_stims | Stims | 1,200 | 25% | 1 | 10 | Corporate |
| pain_editor | Stims | 22,000 | 20% | 1 | 30 | Kowloon |
| memory_expansion | Wetware | 3,200 | 20% | 1 | 8 | Kowloon, Industrial |
| combat_wetware | Wetware | 7,800 | 25% | 2 | 15 | Kowloon |
| skillsoft_pilot | Wetware | 15,000 | 30% | 1 | 12 | Corporate |
| logic_bomb | Exploits | 6,200 | 35% | 1 | 20 | Corporate, Darknet |
| rootkit_military | Exploits | 8,500 | 30% | 1 | 18 | Corporate, Orbital |
| zero_day_exploit | Exploits | 12,000 | 40% | 1 | 25 | Darknet |
| ai_subroutine | AI Shards | 18,000 | 35% | 1 | 28 | Orbital, Darknet |
| ai_shard_fragment | AI Shards | 35,000 | 50% | 1 | 40 | Orbital, Darknet |
| emergent_code | AI Shards | 75,000 | 60% | 2 | 50 | Darknet (event only) |

### A.2 Sector Quick Reference
| ID | Name | Specialties | Base Heat | CorpSec | Gangs | Fixer | Node |
|----|------|-------------|-----------|---------|-------|-------|------|
| kowloon_stack | Kowloon Stack | Wetware, Stims | 30 | 40 | 80 | ✓ | ✓ |
| corporate_zone | Corporate Zone | Exploits, AI Shards | 60 | 90 | 10 | ✗ | ✓ |
| industrial_sector | Industrial Sector | Credsticks, Wetware | 15 | 30 | 40 | ✓ | ✗ |
| orbital_nexus | Orbital Nexus | AI Shards, Exploits | 45 | 70 | 20 | ✗ | ✓ |
| darknet | Darknet | All | 80 | 50 | 60 | ✓ | ✓ |

### A.3 Upgrade Cost Summary (Max Level)

| Upgrade | Total Cost to Max | Key Benefit at Max |
|---------|-------------------|---------------------|
| ICEbreaker | ¥465,000 | -75% raid chance |
| Stealth | ¥372,000 | -75% heat gain |
| Cargo | ¥120,000 | +40 slots (60 total) |
| Trace Reducer | ¥310,000 | -75% trace risk |
| Scanner | ¥620,000 | 5 sectors ahead |
| Auto-Fence | ¥175,000 | 20% emergency discount |
| **Deck Total** | **~¥2,062,000** | — |
| Hull | ¥1,453,125 | +250 integrity |
| Cargo Bay | ¥453,750 | +120 capacity |
| Quantum Drive | ¥976,562 | +8 range, -80% fuel |
| Shield | ¥1,171,875 | +400 shield |
| Weapon | ¥725,000 | +6 slots, +150% dmg |
| Sensor | ¥248,000 | +25 range |
| **Ship Total** | **~¥5,028,312** | — |

---

## CHANGELOG

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0.0 | 2026-06-15 | CoolForest | Initial design baseline from codebase analysis |

---

*This document is the authoritative specification for Neon Trader's economy and progression systems. All implementation should reference this baseline. Balance adjustments must be documented with rationale and testing results.*