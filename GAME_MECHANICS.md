# NEON TRADER — Game Mechanics Reference

**Version:** 1.0  
**Platform:** Agnostic (Python/Textual implementation)  
**Last Updated:** 2026-06-15

---

## 1. CORE TURN STRUCTURE

### 1.1 Turn = 1 Week
Every **player action** that advances time counts as **1 turn = 1 week**:
- Buying payload from market
- Selling payload to market  
- Traveling (jack-in) to another sector
- Taking a loan from fixer
- Laundering cred at node
- Stashing/retrieving cargo at node

### 1.2 Actions That Do NOT Advance Time
- Viewing market/inventory/deck
- Checking reputation/loans
- Opening help screen
- Browsing upgrade options (without purchasing)
- Changing buy/sell mode on market table

### 1.3 Turn Sequence (advance_turn)
When a turn advances, the following occurs **in order**:
1. `player.advance_week()` — increment week counter, process loan payments
2. `apply_sector_heat_decay(sectors)` — natural heat reduction per sector
3. `get_random_event()` → `apply_event_modifiers()` — random world event
4. `generate_market_prices()` — refresh current sector market
5. Check win/loss conditions
6. Update all screens

---

## 2. ACTION ECONOMY

### 2.1 Primary Actions (Cost: 1 Turn)
| Action | Function | Turn Cost |
|--------|----------|-----------|
| Buy | Purchase payload from market | 1 week |
| Sell | Sell payload to market | 1 week |
| Travel | Jack-in to connected sector | 1 week |
| Loan | Take loan from fixer | 1 week |
| Launder | Clean dirty cred at node (5% fee) | 1 week |
| Stash | Move cargo to secure offline storage | 1 week |
| Retrieve | Pull cargo from stash | 1 week |

### 2.2 Free Actions (Cost: 0 Turns)
| Action | Description |
|--------|-------------|
| View market | Browse current sector listings |
| View deck | Check upgrades, cargo capacity |
| View reputation | Check faction standings |
| View loans | Check active debt |
| Change mode | Toggle buy/sell view |
| Help | Open help screen |

---

## 3. CREDIT (¥) FORMULAS

### 3.1 Market Pricing
**Base Price Generation (per turn):**
```
current_price = base_price × (1 + variance) × specialty_mod × heat_mod

where:
  variance = random.uniform(-price_variance, +price_variance)
  specialty_mod = 0.85–1.15 if payload_class in sector.specialties else 1.0
  heat_mod = 1.0 + (sector.current_heat / 100) × 0.2
```

**Market Listing Prices:**
```
buy_price  = current_price × BASE_BUY_MARKUP (1.15) × specialty_mod × heat_mod
sell_price = current_price × BASE_SELL_MARKDOWN (0.85) / specialty_mod / heat_mod

where:
  specialty_mod = 0.8 if payload_class in sector.specialties else 1.0
  heat_mod = 1.0 + (sector.current_heat / 100) × 0.3
```

### 3.2 Transaction Heat Cost
```
heat_gain = payload.heat_on_sale × quantity × sector_mult × (1 - stealth - gang_protection + corpsec_heat)

where:
  sector_mult = 1.0 + (sector.current_heat / 100)
  stealth = deck.stealth.level × 0.15
  gang_protection = player.reputation.get_gang_tier() × 0.05
  corpsec_heat = (abs(player.reputation.corpsec) // 25) × 0.1  (only if corpsec < 0)
  
Minimum heat gain = 1
```

### 3.3 Loans
```
weekly_payment = loan.amount × loan.interest
interest_rate = 0.15 - (fixer_tier × 0.02)  // minimum 0.05 (5%)
fixer_tier = 0–4 based on fixer reputation (0, 25, 50, 75)
```

### 3.4 Laundering (Node)
```
clean_cred = dirty_cred × 0.95  // 5% fee
minimum = ¥1,000
```

### 3.5 Auto-Fence (Upgrade)
```
sell_discount = 0.5 - (auto_fence.level × 0.1)  // 50% → 20% of market sell price
```

---

## 4. HEAT SYSTEM

### 4.1 Heat Sources
| Source | Heat Gain |
|--------|-----------|
| Buy payload | `payload.heat_on_sale × qty × modifiers` |
| Sell payload | `payload.heat_on_sale × qty × modifiers` |
| Detected travel | +25% |
| CorpSec raid | +20% |
| Loan default | +10% |
| ICE Upgrade event | +20% globally |

### 4.2 Heat Decay (per turn)
```
if sector.current_heat > sector.base_heat:
    decay = random(2, 5)
elif sector.current_heat < sector.base_heat:
    decay = -1  // slow return to base
sector.current_heat = clamp(sector.current_heat + decay, 0, 100)
```

### 4.3 Player Heat
```
player.heat = max(sector.current_heat for all sectors)
clamped to 0–100
```

### 4.4 Heat Effects
- **Market prices**: Higher heat → more volatile prices
- **Raid chance**: `sector.current_heat / 2` (0–50% base)
- **Travel risk**: Included in trace calculation
- **Game over**: Player heat ≥ 100%

---

## 5. REPUTATION SYSTEM

### 5.1 Faction Scales (-100 to +100)
| Faction | Positive Effect | Negative Effect |
|---------|----------------|-----------------|
| **Fixer** | Lower loan interest (-2%/tier) | Higher interest |
| **Gangs** | Heat reduction (-5%/tier), travel risk -5/tier | No protection |
| **CorpSec** | Negative is good: travel risk -10 if ≤ -50, heat reduction | Higher raid chance |
| **Netrunners** | (Future: better exploit prices, scanner range) | — |

### 5.2 Tier Thresholds
| Tier | Fixer/Gangs | Effect |
|------|-------------|--------|
| 0 | < 0 | None |
| 1 | 0–24 | Base |
| 2 | 25–49 | +1 tier |
| 3 | 50–74 | +2 tier |
| 4 | ≥ 75 | +3 tier |

### 5.3 CorpSec Reputation (Inverted)
| Value | Tier | Effect |
|-------|------|--------|
| 0 to -24 | 0 | None |
| -25 to -49 | 1 | Heat +10% per tier |
| -50 to -74 | 2 | Travel risk -10 |
| -75 to -100 | 3–4 | Maximum benefits |

---

## 6. WIN / LOSS CONDITIONS

### 6.1 Victory
```
player.cred ≥ 10,000,000  (¥10 million)
```
**Trigger:** Checked at end of every turn via `player.is_victory()`

### 6.2 Game Over (Loss)
```
player.heat ≥ 100  OR  player.week > player.max_weeks (52)
```
**Trigger:** Checked at end of every turn via `player.is_game_over()`

---

## 7. GAME LOOP PHASES

```
┌─────────────────────────────────────────────────────────────┐
│                    PLAYER TURN                              │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐         │
│  │   ACTION    │──│  VALIDATE   │──│  EXECUTE    │         │
│  │ (buy/sell/  │  │ (cred/heat/ │  │ (state     │         │
│  │  travel)    │  │  capacity)  │  │  changes)   │         │
│  └─────────────┘  └─────────────┘  └──────┬──────┘         │
│                                           │                │
│                                           ▼                │
│  ┌─────────────────────────────────────────────────────┐  │
│  │              ADVANCE_TURN (1 week)                  │  │
│  │  1. player.week += 1                                │  │
│  │  2. Process loan payments                           │  │
│  │  3. apply_sector_heat_decay()                       │  │
│  │  4. get_random_event() → apply_event_modifiers()    │  │
│  │  5. generate_market_prices(current_sector)          │  │
│  │  6. Check win/loss                                  │  │
│  │  7. update_all_screens()                            │  │
│  └─────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
```

---

## 8. MARKET REFRESH MECHANICS

### 8.1 When Markets Refresh
- Every turn (`advance_turn()`)
- When player travels to new sector (`travel_to()`)
- When event modifies prices (`apply_event_modifiers()`)

### 8.2 Refresh Process
```
for each payload in all_payloads:
    if payload.stock > 0:
        // 1. Generate new price with variance
        variance = random(-price_variance, +price_variance)
        specialty_mod = 0.85–1.15 if specialty else 1.0
        heat_mod = 1.0 + (sector.heat/100) × 0.2
        payload.current_price = base_price × (1+variance) × specialty_mod × heat_mod
        
        // 2. Generate stock
        base_stock = rarity_based_stock()
        if specialty: base_stock *= 1.5
        payload.stock = base_stock
        
        // 3. Create market listing
        listing = MarketListing(
            buy_price = current_price × 1.15 × specialty_mod × heat_mod
            sell_price = current_price × 0.85 / specialty_mod / heat_mod
            stock = payload.stock (×1.5 if specialty)
            demand = "high"/"normal"/"low"
        )
```

### 8.3 Sector Specialties (from sectors.json)
| Sector | Specialties |
|--------|-------------|
| Kowloon Stack | wetware, stims |
| Corporate Zone | exploits, ai_shards |
| Industrial Sector | credsticks, wetware |
| Orbital Nexus | ai_shards, exploits |
| Darknet | ALL classes |

---

## 9. SECTOR TRAVEL RULES

### 9.1 Connections (from sectors.json)
```
kowloon_stack     → industrial_sector, darknet, corporate_zone
corporate_zone    → kowloon_stack, orbital_nexus, darknet
industrial_sector → kowloon_stack, darknet
orbital_nexus     → corporate_zone, darknet
darknet           → ALL other sectors
```

### 9.2 Travel Risk Calculation
```
base_risk = (from.corpsec_presence + to.corpsec_presence) // 2
heat_factor = (from.current_heat + to.current_heat) // 4
base_risk = min(95, base_risk + heat_factor)

// Apply player modifiers:
trace_reduction = deck.trace_reducer.level × 0.15
adjusted_risk = base_risk × (1 - trace_reduction)
adjusted_risk -= gang_tier × 5
if corpsec_rep ≤ -50: adjusted_risk -= 10
adjusted_risk = clamp(adjusted_risk, 5, 95)

// Detection roll:
detected = random(1, 100) ≤ adjusted_risk

if detected: player.add_heat(25)
```

### 9.3 Travel Effects
- Always advances 1 week
- Moves player to target sector
- Refreshes market for new sector
- If detected: +25% heat, notification

---

## 10. PAYLOAD CAPACITY / WEIGHT

### 10.1 Cargo System
```
cargo_used = sum(item.quantity for item in cargo)
cargo_capacity = deck.BASE_CARGO_SLOTS (20) + (deck.cargo.level × 10)
cargo_free = cargo_capacity - cargo_used

// Payload size varies (1–2 slots):
can_carry(payload, qty) = cargo_free ≥ (payload.size × qty)
```

### 10.2 Payload Sizes (from payloads.json)
| Size | Payloads |
|------|----------|
| 1 | Most exploits, wetware, credsticks, stims, ai_shards |
| 2 | Skillsoft: Pilot, Corporate Scrip, Emergent Code Cluster |

### 10.3 Cargo Item Structure
```python
CargoItem:
    payload_id: str
    quantity: int
    buy_price: int  # Price paid per unit (for profit calc)
```

---

## 11. DECK / CARGO MANAGEMENT

### 11.1 Deck Upgrades
| Upgrade | Base Cost | Max Level | Effect per Level |
|---------|-----------|-----------|------------------|
| **Icebreaker** | ¥15,000 | 5 | -15% raid chance |
| **Stealth** | ¥12,000 | 5 | -15% transaction heat |
| **Cargo** | ¥8,000 | 4 | +10 cargo slots |
| **Trace Reducer** | ¥10,000 | 5 | -15% travel detection |
| **Scanner** | ¥20,000 | 5 | +1 sector scan range |
| **Auto-Fence** | ¥25,000 | 3 | Sell discount 50%→20% |

### 11.2 Upgrade Cost Formula
```
cost(level) = base_cost × (cost_multiplier ^ level)
cost_multiplier = 2.0 (all upgrades)
```

### 11.3 Stash System (Node)
- Secure offline storage, immune to raids
- Unlimited capacity (separate from deck cargo)
- Move cargo between active cargo ↔ stash (1 turn each)

---

## 12. CORPSEC RAID TRIGGERS

### 12.1 Raid Check
Triggered after **every buy/sell action** via `check_raid()`:
```
raid_chance = sector.current_heat / 2  // 0–50% base

// Reductions:
icebreaker_reduction = deck.icebreaker.level × 0.15  // 15% per level
raid_chance -= icebreaker_reduction × 100
gang_tier = player.reputation.get_gang_tier()
raid_chance -= gang_tier × 5

raid_chance = max(0, raid_chance)
raided = random(1, 100) ≤ raid_chance
```

### 12.2 Raid Consequences
```
if raided:
    for each cargo item (50% chance per type):
        lost_qty = max(1, item.quantity // 2)
        cargo_lost += lost_qty
        cred_lost += item.buy_price × lost_qty
        item.quantity -= lost_qty
        if item.quantity == 0: remove from cargo
    
    player.cred = max(0, player.cred - cred_lost)
    player.add_heat(20)
```

---

## 13. RANDOM EVENTS

### 13.1 Event Types & Weights
| Event | Base Weight | Description |
|-------|-------------|-------------|
| market_spike | 20 | One class surges 2.5x in current sector |
| corp_war | 15 | Two sectors conflict; exploits +80%, ai_shards +50% |
| net_crash | 10 | Digital payloads crash; hardware premium |
| ice_upgrade | 10 | Global CorpSec +20%, heat +20% |
| zero_day_drop | 5 | Rare exploit appears in Darknet |
| data_courier | 15 | Transport job (not yet implemented) |

### 13.2 Dynamic Weight Adjustments
```
if avg_sector_heat > 60:
    net_crash weight += 10
    ice_upgrade weight += 10

if week > 30:
    zero_day_drop weight += 10
```

### 13.3 Event Application
```
for each affected payload class:
    payload.current_price = int(payload.current_price × modifier)
    
for each target sector:
    sector.adjust_heat(heat_change)
    
if global_corpsec_boost:
    for all sectors: corpsec_presence += boost (max 100)
    
if spawn_rare:
    payloads[rare_id].stock += 1 in target sector
```

---

## 14. DIFFICULTY SCALING

### 14.1 Implicit Scaling Factors
| Factor | Early Game (Week 1–15) | Mid Game (Week 16–35) | Late Game (Week 36–52) |
|--------|------------------------|----------------------|------------------------|
| **Base heat** | Low sectors accessible | Must manage heat | High-heat sectors needed for margins |
| **Event frequency** | Standard | More ICE upgrades | More zero-day drops |
| **Loan availability** | Easy (low interest) | Moderate | Expensive (high interest) |
| **Target cred** | ¥100K | ¥1M | ¥10M |
| **Raid lethality** | Low (low heat) | Moderate | High (high sector heat) |

### 14.2 Explicit Difficulty (Not Yet Implemented)
Future: Difficulty settings could modify:
- `max_weeks` (default 52)
- `victory_cred` (default 10M)
- `base_heat` multipliers
- `raid_chance` multipliers
- `loan_interest` base rate
- `event_weights` bias

---

## 15. FORMULAS QUICK REFERENCE

### 15.1 Transaction Heat
```
heat = max(1, payload.heat_on_sale × qty × (1 + sector_heat/100) × 
           (1 - stealth×0.15 - gang_tier×0.05 + corpsec_tier×0.1))
```

### 15.2 Travel Detection
```
risk = clamp(5, 95, 
    ((from_corpsec + to_corpsec)//2 + (from_heat + to_heat)//4) 
    × (1 - trace_reducer×0.15) 
    - gang_tier×5 
    - (corpsec≤-50 ? 10 : 0)
)
detected = rand(1,100) ≤ risk
```

### 15.3 Raid Chance
```
chance = max(0, sector_heat/2 - icebreaker×15 - gang_tier×5)
raided = rand(1,100) ≤ chance
```

### 15.4 Market Buy Price
```
buy = current_price × 1.15 × (specialty ? 0.8 : 1.0) × (1 + sector_heat/100 × 0.3)
```

### 15.5 Market Sell Price
```
sell = current_price × 0.85 / (specialty ? 0.8 : 1.0) / (1 + sector_heat/100 × 0.3)
```

### 15.6 Loan Interest
```
interest = max(0.05, 0.15 - fixer_tier × 0.02)
weekly_payment = principal × interest
```

### 15.7 Cargo Capacity
```
capacity = 20 + cargo_level × 10
```

---

## 16. DATA FILES REFERENCE

### 16.1 sectors.json Fields
```json
{
  "id": "string",
  "name": "string",
  "description": "string",
  "specialties": ["payload_class", ...],
  "base_heat": 0-100,
  "corpsec_presence": 0-100,
  "gang_presence": 0-100,
  "fixer_access": boolean,
  "node_access": boolean
}
```

### 16.2 payloads.json Fields
```json
{
  "id": "string",
  "name": "string",
  "class": "payload_class_id",
  "rarity": "common|uncommon|rare|legendary",
  "base_price": integer,
  "price_variance": 0.0-1.0,
  "size": 1-2,
  "heat_on_sale": 0-50,
  "description": "string"
}
```

### 16.3 Payload Classes
| Class | Color | Volatility |
|-------|-------|------------|
| exploits | #ff006e | high |
| wetware | #00ffff | medium |
| credsticks | #ffb000 | low |
| stims | #ff0033 | medium |
| ai_shards | #bb00ff | extreme |

### 16.4 Rarity Stock Ranges
| Rarity | Stock Range |
|--------|-------------|
| common | 10–50 |
| uncommon | 3–15 |
| rare | 1–5 |
| legendary | 0–2 |

---

## 17. IMPLEMENTATION NOTES

### 17.1 Key Modules
| Module | Responsibility |
|--------|----------------|
| `app.py` | Main game loop, turn advancement, screen management |
| `models/player.py` | Player state, cargo, reputation, loans, win/loss |
| `models/sector.py` | Sector data, heat, travel risk |
| `models/payload.py` | Payload definitions, price/stock generation |
| `models/market.py` | Market listings, buy/sell pricing |
| `models/deck.py` | Upgrades, cargo capacity, bonuses |
| `utils/heat_system.py` | Heat calc, raid check, travel risk |
| `utils/market_logic.py` | Price generation, events |
| `utils/save_load.py` | Persistence |

### 17.2 State Flow
```
NeonTraderApp (singleton)
├── player: Player
├── sectors: Dict[str, Sector]
├── payloads: Dict[str, Payload]
├── current_sector: Sector
├── market: Market
└── screens: Dict[str, Screen]
```

All screens receive updates via `on_screen_resume` → `update_all_screens()`.

---

*End of GAME_MECHANICS.md*