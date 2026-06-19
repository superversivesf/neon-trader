# UI_UX_DESIGN.md

## Neon Trader — Platform-Agnostic UI/UX Design Specification

**Version:** 1.0  
**Status:** Draft  
**Scope:** Defines the complete user interface and experience design for Neon Trader. Serves as the single source of truth for all platform implementations (Python/Textual, Go/Bubble Tea, future ports).

---

## 1. Design Philosophy

### 1.1 Core Aesthetic: Cyberpunk Terminal
Neon Trader embraces a **retro-futuristic terminal aesthetic** inspired by 80s/90s cyberpunk fiction (Gibson, Sterling, Cadigan) and classic terminal applications. The UI is not merely "terminal-like" — it *is* a terminal interface, with all the constraints and affordances that implies:

- **Text-first, graphics-never**: All information conveyed through characters, colors, and layout
- **Keyboard-driven**: Every action accessible without a mouse
- **High information density**: Maximum data in minimum screen real estate
- **Atmospheric but functional**: Visual flair serves gameplay clarity, never obscures it

### 1.2 Design Principles

| Principle | Application |
|-----------|-------------|
| **Clarity over cleverness** | Unambiguous labels, consistent patterns, predictable behavior |
| **Keyboard supremacy** | Every feature reachable via keyboard; gamepad as secondary |
| **Persistent context** | Status bar always visible; current sector, heat, cred never hidden |
| **Progressive disclosure** | Main screen = overview; modals = detail; help = reference |
| **State visibility** | System state (heat, cargo, loans) always surfaced, never buried |
| **Error prevention** | Confirmation dialogs for destructive actions; max bounds enforced |
| **Platform parity** | Identical workflows across Python/Go/WASM implementations |

---

## 2. Screen Inventory

Neon Trader has **7 canonical screens** divided into two categories:

### 2.1 Primary Screen (Always Accessible)

| Screen | Type | Purpose | Access |
|--------|------|---------|--------|
| **Main** | Full-screen, persistent | Core gameplay loop: view market, buy/sell, navigate to sub-screens | Default on launch; `Esc` from any modal returns here |

### 2.2 Modal Screens (Overlay on Main)

| Screen | Type | Purpose | Access from Main |
|--------|------|---------|------------------|
| **Market** | Modal (centered) | Detailed buy/sell transaction for selected payload | `Enter` on market row (Buy/Sell mode) |
| **Travel** | Modal (centered) | Select destination sector, view trace risk | `J` (Jack-In) |
| **Fixer** | Modal (centered) | Loans, laundering, intel, reputation view | `F` |
| **Upgrade** | Modal (centered) | Cyberdeck upgrade purchasing | `U` |
| **Node** | Modal (centered) | Launder cred, stash/retrieve cargo | `N` |
| **Help** | Modal (centered) | Keybindings reference, gameplay basics | `?` or `F1` |

---

### 2.3 Screen Specifications

#### 2.3.1 Main Screen
```
┌─────────────────────────────────────────────────────────────────────────────┐
│ STATUS BAR (3 lines) — CRED | HEAT | WEEK | SECTOR | CARGO | REP | DECK    │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  LEFT PANEL (40%)                    │  RIGHT PANEL (60%)                   │
│  ┌──────────────────────────────┐    │  ┌────────────────────────────────┐  │
│  │ MATRIX RAIN BACKGROUND       │    │  │ STATUS BAR (redundant summary) │  │
│  │ (animated, full height)      │    │  ├────────────────────────────────┤  │
│  │                              │    │  │ MARKET TABLE                   │  │
│  │                              │    │  │ ┌────┬──────┬─────┬────┬────┬──┐│  │
│  │                              │    │  │ │Payl│Class │ Buy│Stck│Dem │Sz││  │
│  │                              │    │  │ │────┼──────┼─────┼────┼────┼──┤│  │
│  │                              │    │  │ │Zero│Explo │¥45k│  12│High│ 3││  │
│  │                              │    │  │ │Neural│Wetw │¥23k│  45│Med │ 2││  │
│  │                              │    │  │ │Stim │Stim  │¥1.2k│ 200│Low │ 1││  │
│  │                              │    │  │ │... │...   │... │... │... │..││  │
│  │                              │    │  │ └────┴──────┴─────┴────┴────┴──┘│  │
│  ├──────────────────────────────┤    │  ├────────────────────────────────┤  │
│  │ DECK DISPLAY                 │    │  │ ACTION BAR                     │  │
│  │ ┌────────────────────────┐   │    │  │ [B]uy [S]ell [J]ack [F]ixer    │  │
│  │ │ CYBERDECK UPGRADES     │   │    │  │ [U]pgr [N]ode [Q]uit [?]Help   │  │
│  │ │ ICEbreaker     2/4 ████░░  │   │    │  └────────────────────────────────┘  │
│  │ │ Stealth        1/3 ███░░░░  │   │                                       │
│  │ │ Cargo          3/4 ██████░░  │   │                                       │
│  │ │ Trace Reducer  0/3 ░░░░░░░░  │   │                                       │
│  │ │ Scanner        1/2 ███░░░░░░  │   │                                       │
│  │ │ Auto-Fence     0/3 ░░░░░░░░░  │   │                                       │
│  │ └────────────────────────┘   │    │                                       │
│  └──────────────────────────────┘    └────────────────────────────────┘       │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

**Key Behaviors:**
- Matrix rain animates continuously in left panel (reduced motion: static)
- Market table sortable by column (click header or keybinding)
- Buy/Sell mode toggles price column and available actions
- Deck display shows all 6 upgrades with progress bars and next costs
- Action bar always visible with hotkeys

#### 2.3.2 Market Screen (Modal)
```
┌──────────────────────────────────────────────┐
│ BUY: Neural Interface              [Esc]     │
├──────────────────────────────────────────────┤
│ Class:    Wetware                            │
│ Rarity:   Rare                               │
│ Size:     2 slots                            │
│ Heat:     8%                                 │
│                                           │
│ Price:    ¥23,450       Available: 45        │
│ Demand:   Medium                              │
│                                           │
│ Max you can buy: 12                          │
│                                           │
│ Quantity: [____________]  (1-12)             │
│                                           │
│ [Buy]          [Cancel]                      │
└──────────────────────────────────────────────┘
```

**Behaviors:**
- Input validates against cargo space, stock, and cred
- `↑/↓` adjusts quantity; `Enter` confirms; `Esc` cancels
- Shows calculated max based on mode (buy: cargo space + stock + cred; sell: owned quantity)

#### 2.3.3 Travel Screen (Modal)
```
┌────────────────────────────────────────────────────────────┐
│ JACK-IN — SELECT DESTINATION                    [Esc]      │
├────────────────────────────────────────────────────────────┤
│ ┌────┬──────┬───────┬──────┬──────┬────┬──────┐            │
│ │Sec │ Heat │CorpSec│Gangs │Fixr │Node│ Risk │            │
│ ├────┼──────┼───────┼──────┼──────┼────┼──────┤            │
│ │Kowl│ 12%  │  35%  │ 78%  │  ●   │ ○  │ 15%  │ ← cursor  │
│ │Corp│ 67%  │  92%  │ 12%  │  ○   │ ●  │ 78%  │            │
│ │Ind │  3%  │  15%  │ 22%  │  ○   │ ○  │  5%  │            │
│ │Orb │ 45%  │  88%  │  8%  │  ○   │ ●  │ 52%  │            │
│ │Dark│ 89%  │  65%  │ 95%  │  ●   │ ●  │ 91%  │            │
│ └────┴──────┴───────┴──────┴──────┴────┴──────┘            │
│                                                            │
│ From: Kowloon Stack                                        │
│ To:   Corporate Zone          ⚠ DETECTION LIKELY          │
│ Trace Risk: 78%                                              │
│ Heat on Arrival: 67%                                        │
│                                                            │
│ [Jack-In]          [Abort]                                 │
└────────────────────────────────────────────────────────────┘
```

**Behaviors:**
- Only connected sectors shown (per sector graph)
- `↑/↓` navigates; `Enter` selects; risk panel updates live
- High risk (>70%) shows warning; detection adds +25 heat

#### 2.3.4 Fixer Screen (Modal)
```
┌────────────────────────────────────────────────────────────┐
█▓▒░ THE FIXER ░▒▓█                                          │
├────────────────────────────────────────────────────────────┤
│ REPUTATION                                                 │
│ Fixer:      ████████████░░░░░░░░░░  +42                    │
│ Gangs:      ████████░░░░░░░░░░░░░░  -15                    │
│ CorpSec:    ░░░░░░░░░░░░░░░░░░░░░░  -88                    │
│ Netrunners: ██████████████░░░░░░░░░  +65                  │
│                                                            │
│ ACTIVE LOANS                                               │
│ ┌────┬──────────┬──────────┬────────┬──────────┐          │
│ │ ID │ Principal│ Int/wk   │ Weeks  │ Total    │          │
│ ├────┼──────────┼──────────┼────────┼──────────┤          │
│ │LN1 │ ¥50,000  │ ¥7,500   │   8    │ ¥110,000 │          │
│ └────┴──────────┴──────────┴────────┴──────────┘          │
│                                                            │
│ SERVICES                                                   │
│ Loan Amount: [________]  Weeks: [___]  [Take Loan]        │
│ [Launder Cred]  [Buy Intel]  [Repay Early]  [Done]        │
└────────────────────────────────────────────────────────────┘
```

**Behaviors:**
- Rep bars color-coded: green (positive), red (negative)
- Loan terms depend on Fixer reputation tier
- Number keys `1-4` quick-access services

#### 2.3.5 Upgrade Screen (Modal)
```
┌────────────────────────────────────────────────────────────────────┐
█▓▒░ DECK UPGRADES ░▒▓█                    CRED: ¥1,234,567         │
├────────────────────────────────────────────────────────────────────┤
│ ┌────┬────────────────┬───────┬──────────────┬──────────┬────┐    │
│ │    │ Upgrade        │ Level │ Effect       │ Next Cost│ Max│    │
│ ├────┼────────────────┼───────┼──────────────┼──────────┼────┤    │
│ │►   │ ICEbreaker     │  2/4  │ 30%          │ ¥180,000 │  4 │    │
│ │    │ Stealth        │  1/3  │ 15%          │ ¥120,000 │  3 │    │
│ │    │ Cargo          │  3/4  │ +30 slots    │ ¥250,000 │  4 │    │
│ │    │ Trace Reducer  │  0/3  │ 0%           │  ¥80,000 │  3 │    │
│ │    │ Scanner        │  1/2  │ 1 sector     │ ¥300,000 │  2 │    │
│ │    │ Auto-Fence     │  0/3  │ 50% disc.    │ ¥150,000 │  3 │    │
│ └────┴────────────────┴───────┴──────────────┴──────────┴────┘    │
│                                                                    │
│ ICEbreaker Suite                                                     │
│ Reduces CorpSec raid chance and trace detection.                    │
│ Each level: -15% raid chance.                                       │
│                                                                    │
│ Current Level: 2/4                                                  │
│ Current Effect: 30%                                                 │
│ Upgrade Cost: ¥180,000 ✓                                            │
│                                                                    │
│ [Upgrade]          [Done]                                          │
└────────────────────────────────────────────────────────────────────┘
```

**Behaviors:**
- `↑/↓` selects row; details panel updates live
- Cost colored green (affordable) or red (insufficient)
- Max level shows "MAX" instead of cost

#### 2.3.6 Node Screen (Modal)
```
┌────────────────────────────────────────────────────────────┐
█▓▒░ OFFSHORE NODE ░▒▓█                                      │
├────────────────────────────────────────────────────────────┤
│ STATUS                                                     │
│ Clean Cred:    ¥1,234,567                                  │
│ Stashed Cred:  ¥500,000                                    │
│ Stashed Cargo: 3 types                                     │
│                                                            │
│ LAUNDER CRED (5% fee)                                      │
│ Amount: [________]  [Launder]                              │
│                                                            │
│ STASH CARGO (safe from raids)                              │
│ Payload: [__________]  Qty: [__]  [Stash]                  │
│                                                            │
│ RETRIEVE                                                   │
│ Payload: [__________]  Qty: [__]  [Retrieve]               │
│                                                            │
│ [Done]                                                     │
└────────────────────────────────────────────────────────────┘
```

**Behaviors:**
- Laundering converts "dirty" cred to stashed (clean) at 5% fee
- Stashed cargo immune to CorpSec raids
- Requires cargo space to retrieve

#### 2.3.7 Help Screen (Modal)
```
┌────────────────────────────────────────────────────────────┐
NEON TRADER — CONTROLS                           [Esc/?]     │
├────────────────────────────────────────────────────────────┤
│ MAIN SCREEN                                                │
│ B        Buy mode                                          │
│ S        Sell mode                                         │
│ ↑/↓      Navigate market list                              │
│ Enter    Confirm buy/sell                                  │
│ J        Jack-in to another sector                         │
│ F        Visit Fixer (loans, services)                     │
│ U        Upgrade cyberdeck                                 │
│ N        Node (launder/stash)                              │
│ Q        Save & Quit                                       │
│ ?        This help                                         │
│                                                            │
│ MARKET SCREEN                                              │
│ ↑/↓      Adjust quantity                                   │
│ Enter    Confirm transaction                               │
│ Esc      Cancel                                            │
│                                                            │
│ GAMEPLAY BASICS                                            │
│ • Buy payloads in one sector, travel to another, sell...  │
│ • Each turn = 1 week. Prices shift, heat decays, events.. │
│ • Heat rises from transactions. High heat = CorpSec raids │
│ • Jack-in has trace risk. Upgrade Trace Reducer to lower  │
│ • Reputation unlocks better prices, loans, protection     │
│ • Win: ¥10,000,000. Lose: 100% heat or 52 weeks           │
│                                                            │
│ PAYLOAD CLASSES                                            │
│ Exploits    High value, high heat, volatile prices         │
│ Wetware     Medium value, steady demand                    │
│ Credsticks  Low risk, low margin, good for laundry         │
│ Stims       Volume trade, consistent demand                │
│ AI Shards   Extreme risk/reward, rare                      │
└────────────────────────────────────────────────────────────┘
```

---

## 3. Navigation Flow & State Transitions

### 3.1 Screen Flow Diagram

```
                          ┌─────────────┐
                          │   MAIN      │
                          │  (ROOT)     │
                          └──────┬──────┘
                                 │
         ┌─────────┬────────┬────┼────┬─────────┬────────┐
         │         │        │    │    │         │        │
         ▼         ▼        ▼    ▼    ▼         ▼        ▼
      ┌──────┐ ┌──────┐ ┌────────┐ ┌──────┐ ┌──────┐ ┌──────┐
      │Market│ │Travel│ │ Fixer  │ │Upgrade│ │ Node │ │ Help │
      └──┬───┘ └──┬───┘ └────┬───┘ └──┬───┘ └──┬───┘ └──┬───┘
         │        │        │        │        │        │
         └────────┴────────┴────────┴────────┴────────┘
                                 │
                          ┌──────▼──────┐
                          │   MAIN      │
                          │ (resume)    │
                          └─────────────┘
```

### 3.2 State Transition Rules

| From | To | Trigger | Data Passed | Turn Consumed |
|------|-----|---------|-------------|---------------|
| Main | Market | `Enter` on row | Listing, mode (buy/sell), player | No (modal) |
| Market | Main | Confirm/Cancel | Quantity, confirmed | **Yes** if confirmed |
| Main | Travel | `J` | Player, sectors, current_sector | No (modal) |
| Travel | Main | Confirm | Target sector, risk, detected | **Yes** |
| Main | Fixer | `F` | Player | No (modal) |
| Fixer | Main | Done/Cancel | Action taken (loan, etc.) | **Yes** if loan taken |
| Main | Upgrade | `U` | Player | No (modal) |
| Upgrade | Main | Done | Upgrades purchased | **Yes** per upgrade |
| Main | Node | `N` | Player | No (modal) |
| Node | Main | Done | Launder/stash actions | **Yes** if action taken |
| Main | Help | `?` / `F1` | None | No (modal) |
| Help | Main | `Esc` / `?` | None | No |

### 3.3 Turn Consumption Rules

**Every modal that mutates game state consumes exactly 1 week (turn):**
- Market: Confirmed buy/sell → `advance_turn()`
- Travel: Confirmed jack-in → `advance_turn()`
- Fixer: Loan taken, laundering, repay → `advance_turn()`
- Upgrade: Each upgrade purchased → `advance_turn()`
- Node: Launder, stash, retrieve → `advance_turn()`

**Modals that only view data do NOT consume turns:**
- Help, viewing loans, viewing upgrades (without buying), viewing travel risk (without confirming)

### 3.4 Data Synchronization

On every screen resume (modal dismissed → Main), the Main screen **must** refresh:
- Market table (prices may have changed from events)
- Status bar (cred, heat, week, cargo, rep, deck)
- Deck display (upgrades may have been purchased)
- Matrix rain continues uninterrupted

---

## 4. Visual Language

### 4.1 Color Palette

#### 4.1.1 Primary Palette (CSS Variables / Design Tokens)

| Token | Hex | RGB | Usage |
|-------|-----|-----|-------|
| `--bg` | `#0a0a0f` | 10,10,15 | Base background (near-black with blue tint) |
| `--panel` | `#1a1a2e` | 26,26,46 | Panel/card backgrounds, modals |
| `--fg` | `#ffffff` | 255,255,255 | Primary text |
| `--matrix` | `#00ff41` | 0,255,65 | **Primary accent** — matrix rain, headers, success |
| `--amber` | `#ffb000` | 255,176,0 | **Secondary accent** — labels, warnings, cred |
| `--cyan` | `#00ffff` | 0,255,255 | **Tertiary accent** — titles, focus, travel |
| `--pink` | `#ff006e` | 255,0,110 | **Quaternary accent** — node, danger, heat critical |
| `--red` | `#ff0033` | 255,0,51 | Error, critical heat, raid warnings |

#### 4.1.2 Semantic Color Mapping

| Semantic Role | Token | States |
|---------------|-------|--------|
| Background | `--bg` | Default |
| Panel/Card | `--panel` | Modals, containers |
| Primary Text | `--fg` | Default text |
| Muted Text | `--fg` @ 50% | Descriptions, disabled |
| **Success/Primary Action** | `--matrix` | Buy buttons, positive heat, matrix rain |
| **Warning/Caution** | `--amber` | Labels, loan interest, medium heat |
| **Info/Focus/Navigation** | `--cyan` | Titles, table headers, travel, cursor |
| **Danger/Heat-Critical** | `--pink` / `--red` | Node, high heat, errors, critical alerts |

#### 4.1.3 Heat Gradient (Dynamic)

Heat values map to colors for immediate visual recognition:

| Heat Range | Color | Class | Visual |
|------------|-------|-------|--------|
| 0-29% | `#00ff41` (matrix) | `heat-low` | ████████░░ |
| 30-59% | `#ffb000` (amber) | `heat-med` | ██████████░░ |
| 60-89% | `#ff0033` (red) | `heat-high` | ████████████░░ |
| 90-100% | `#ff0033` + **blink** | `heat-critical` | ██████████████ **BLINK** |

### 4.2 Typography

#### 4.2.1 Font Stack (Priority Order)

```
1. "JetBrains Mono", "Fira Code", "Cascadia Code", "Source Code Pro"
2. "IBM Plex Mono", "Ubuntu Mono", "DejaVu Sans Mono"
3. "Consolas", "Monaco", "Menlo"
4. monospace (system fallback)
```

**Requirements:**
- Monospace mandatory (alignment, tables, matrix rain)
- Ligatures enabled for code-like readability
- Minimum 12pt / 16px equivalent

#### 4.2.2 Type Scale

| Element | Size | Weight | Color | Example |
|---------|------|--------|-------|---------|
| Modal Title | 1.0rem | Bold | Cyan/Amber/Pink | `█▓▒░ DECK UPGRADES ░▒▓█` |
| Section Title | 0.9rem | Bold | Cyan | `REPUTATION`, `SERVICES` |
| Body Text | 0.9rem | Normal | White | Payload descriptions |
| Labels | 0.9rem | Bold | Amber | `CRED:`, `HEAT:`, `Class:` |
| Values | 0.9rem | Normal | White | `¥1,234,567`, `67%` |
| Table Header | 0.85rem | Bold | Matrix (bg) / Black (fg) | `Payload`, `Buy`, `Stock` |
| Table Row | 0.85rem | Normal | White | `Zero Day Exploit` |
| Action Bar | 0.8rem | Bold keys | Cyan (keys) / Amber (labels) | `[B]uy` |
| Matrix Rain | 0.8rem | Normal | Matrix (dimmed) | Falling katakana |

### 4.3 Visual Effects

#### 4.3.1 Matrix Rain (Background)
- **Characters**: Katakana + alphanumeric + symbols (`アイウエオ...0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ!@#$%^&*()`)
- **Density**: 15% of column count (configurable)
- **Speed**: Variable per column (0.5x-1.5x base speed)
- **Length**: 5-20 characters per column
- **Update Interval**: 150ms (balance smoothness vs CPU)
- **Reduced Motion**: Static character grid, no animation

#### 4.3.2 Focus Indicators
- **Table Cursor**: Amber background (`#ffb000`) with black text
- **Button Focus**: Cyan border, matrix background on hover
- **Input Focus**: Cyan border (`#00ffff`)
- **Modal Focus Trap**: Tab cycles within modal only

#### 4.3.3 Transitions
- **Modal Open**: Fade in 100ms (or instant if reduced motion)
- **Modal Close**: Fade out 100ms
- **Table Sort**: No animation (instant reorder)
- **Status Updates**: No animation (instant)

---

## 5. Information Hierarchy

### 5.1 Global Persistent Elements (Always Visible on Main)

| Priority | Element | Location | Refresh Rate |
|----------|---------|----------|--------------|
| 1 | **Cred** | Status Bar L1, Action Bar | Every turn |
| 2 | **Heat** | Status Bar L1 (color-coded), Deck Display | Every turn |
| 3 | **Week/Max** | Status Bar L1 | Every turn |
| 4 | **Current Sector** | Status Bar L1 | On travel |
| 5 | **Cargo Used/Cargo Used/Max** | Status Bar L2 | Every transaction |
| 6 | **Faction Reps** | Status Bar L2 (4 values) | On rep change |
| 7 | **Deck Summary** | Status Bar L3 (compact) | On upgrade |
| 8 | **Weekly Loan Payment** | Status Bar L3 (if >0) | On loan change |

### 5.2 Main Screen Information Zones

```
┌─────────────────────────────────────────────────────────────────┐
│ ZONE 1: STATUS BAR (3 lines) — Global state, always visible     │
├─────────────────────────────────────────────────────────────────┤
│ ZONE 2: LEFT PANEL — Atmospheric + Player Assets                │
│   ├─ 2A: Matrix Rain (background, non-interactive)              │
│   └─ 2B: Deck Display (upgrades, progress, costs)               │
├─────────────────────────────────────────────────────────────────┤
│ ZONE 3: RIGHT PANEL — Core Gameplay                             │
│   ├─ 3A: Market Table (sortable, selectable, buy/sell mode)     │
│   └─ 3B: Action Bar (hotkey reference, context-aware)           │
└─────────────────────────────────────────────────────────────────┘
```

### 5.3 Modal Information Hierarchy

Each modal follows: **Title → Context → Primary Content → Actions**

| Modal | Title | Context | Primary Content | Actions |
|-------|-------|---------|-----------------|---------|
| Market | `BUY/SELL: <payload>` | Class, rarity, size, heat | Price, stock, demand, max qty | Quantity input, Confirm/Cancel |
| Travel | `JACK-IN — SELECT DESTINATION` | Current sector | Sector table (heat, corpsec, gangs, fixer, node, risk) | Risk panel, Jack-In/Abort |
| Fixer | `█▓▒░ THE FIXER ░▒▓█` | Rep bars (4 factions) | Loans table, Service inputs | 4 service buttons, Done |
| Upgrade | `█▓▒░ DECK UPGRADES ░▒▓█` | Current cred | Upgrade table (level, effect, cost, max) | Detail panel, Upgrade/Done |
| Node | `█▓▒░ OFFSHORE NODE ░▒▓█` | Clean/stashed cred, stashed cargo | 3 service sections (launder, stash, retrieve) | Per-service buttons, Done |
| Help | `NEON TRADER — CONTROLS` | None | Categorized keybindings + basics | Close only |

---

## 6. Input Schemes

### 6.1 Keyboard Bindings (Primary)

#### 6.1.1 Main Screen Bindings

| Key | Action | Context |
|-----|--------|---------|
| `B` | Set mode: Buy | Market table |
| `S` | Set mode: Sell | Market table |
| `↑` / `K` | Cursor up | Market table, Travel table, Upgrade table |
| `↓` / `J` | Cursor down | Market table, Travel table, Upgrade table |
| `Enter` | Open Market modal (buy/sell) | Market table row selected |
| `J` | Open Travel screen | Main |
| `F` | Open Fixer screen | Main |
| `U` | Open Upgrade screen | Main |
| `N` | Open Node screen | Main |
| `?` / `F1` | Open Help screen | Global |
| `Q` | Save & Quit | Main (with confirmation) |
| `Esc` | Close modal / Cancel | Any modal |
| `Tab` | Focus next widget | Within modal |
| `Shift+Tab` | Focus previous widget | Within modal |

#### 6.1.2 Market Modal Bindings

| Key | Action |
|-----|--------|
| `↑` / `↓` | Adjust quantity (±1) |
| `Enter` | Confirm transaction |
| `Esc` | Cancel |

#### 6.1.3 Travel Modal Bindings

| Key | Action |
|-----|--------|
| `↑` / `↓` | Navigate sector list |
| `Enter` | Confirm selection |
| `Esc` | Cancel |

#### 6.1.4 Fixer Modal Bindings

| Key | Action |
|-----|--------|
| `1` | Focus Loan Amount |
| `2` | Focus Launder (placeholder) |
| `3` | Focus Intel (placeholder) |
| `4` | Focus Repay Early |
| `Esc` | Close |

#### 6.1.5 Upgrade Modal Bindings

| Key | Action |
|-----|--------|
| `↑` / `↓` | Navigate upgrade list |
| `Enter` | Purchase selected upgrade |
| `Esc` | Close |

#### 6.1.6 Node Modal Bindings

| Key | Action |
|-----|--------|
| `1` | Focus Launder Amount |
| `2` | Focus Stash Payload |
| `3` | Focus Retrieve Payload |
| `Esc` | Close |

#### 6.1.7 Help Modal Bindings

| Key | Action |
|-----|--------|
| `Esc` / `?` / `F1` | Close |

### 6.2 Gamepad Bindings (Secondary)

| Gamepad Input | Maps To | Notes |
|---------------|---------|-------|
| D-Pad Up / Left Stick Up | `↑` | Navigation |
| D-Pad Down / Left Stick Down | `↓` | Navigation |
| D-Pad Left / Right | `Tab` / `Shift+Tab` | Focus cycling (modals) |
| A (South) / Cross | `Enter` | Confirm/Select |
| B (East) / Circle | `Esc` | Cancel/Back |
| X (West) / Square | `?` | Help |
| Y (North) / Triangle | Contextual | Main: Quick-save; Modal: Primary action |
| Left Shoulder (LB/L1) | `B` | Buy mode |
| Right Shoulder (RB/R1) | `S` | Sell mode |
| Left Trigger (LT/L2) | `J` | Jack-In |
| Right Trigger (RT/R2) | `F` | Fixer |
| Select/Back | `Q` | Quit (with confirm) |
| Start/Options | `?` | Help |

**Gamepad Notes:**
- Analog stick dead zone: 0.3
- Repeat delay: 300ms initial, 50ms repeat
- Vibration: Light pulse on confirmation, heavy on raid/error

---

## 7. Accessibility

### 7.1 Screen Reader Support

#### 7.1.1 Semantic Structure
- All screens use proper heading hierarchy (`h1` → `h2` → `h3`)
- Modal dialogs use `role="dialog"` with `aria-labelledby`
- Tables use `<table>` with `<th scope="col">` headers
- Form inputs have associated `<label>` elements
- Status bar uses `role="status"` with `aria-live="polite"`

#### 7.1.2 Live Regions
| Region | ARIA Live | Content |
|--------|-----------|---------|
| Notifications | `assertive` | Toast messages (success, error, warning) |
| Status Bar | `polite` | Cred, heat, week changes |
| Market Table | `off` | Row selection announced on demand |
| Modal Title | `assertive` | Screen purpose on open |

#### 7.1.3 Announcements
- **On modal open**: "Buy modal, Neural Interface, price 23,450 credits, 45 available"
- **On heat change**: "Heat now 67 percent, high"
- **On raid**: "CorpSec raid detected. Lost 3 cargo, 45000 credits. Heat increased 15 percent"
- **On week advance**: "Week 23 of 52. Market prices updated."

### 7.2 High Contrast Mode

**Trigger:** System preference `prefers-contrast: more` or user setting

**Color Overrides:**
| Standard | High Contrast |
|----------|---------------|
| `#0a0a0f` (bg) | `#000000` |
| `#1a1a2e` (panel) | `#111111` |
| `#00ff41` (matrix) | `#00ff00` |
| `#ffb000` (amber) | `#ffff00` |
| `#00ffff` (cyan) | `#00ffff` |
| `#ff006e` (pink) | `#ff00ff` |
| `#ffffff` (fg) | `#ffffff` |

**Additional Changes:**
- Borders: 2px solid (was 1px)
- Focus outlines: 3px solid cyan
- Text shadows removed
- Matrix rain: Higher contrast chars only (`█▓▒░`)

### 7.3 Reduced Motion

**Trigger:** System preference `prefers-reduced-motion: reduce` or user setting

**Changes:**
- Matrix rain: **Static** — renders single frame, no animation timer
- Modal transitions: **Instant** — no fade in/out
- Table sorting: **Instant** — no transition
- Status updates: **Instant** — no flash/pulse
- Blinking heat-critical: **Solid** — no blink, uses bold + reverse video

### 7.4 Keyboard Navigation

- **Tab order**: Logical, follows visual layout (top-to-bottom, left-to-right)
- **Focus visible**: Always (3px cyan outline in high contrast, 2px cyan standard)
- **Focus trap**: Modals trap focus; `Esc` exits
- **Skip links**: Not needed (no repetitive navigation)
- **Shortcut discovery**: Help screen (`?`) lists all bindings

### 7.5 Color Blindness Safety

**Palette tested against:**
- Protanopia (red-blind)
- Deuteranopia (green-blind)
- Tritanopia (blue-blind)
- Achromatopsia (monochrome)

**Mitigations:**
- Heat uses **position + label + bar** not color alone
- Reputation bars use **fill direction** (left=negative, right=positive) + label
- Demand: Text labels (High/Med/Low) + color
- Buttons: Text labels + variant styling (primary=filled, default=outline)
- Never rely on red/green distinction alone

---

## 8. Responsive Layout Principles

### 8.1 Breakpoints

| Breakpoint | Min Width | Layout Changes |
|------------|-----------|----------------|
| **Desktop** | ≥ 120 cols | Full two-panel Main (40/60 split) |
| **Tablet/Laptop** | 80-119 cols | Main: Stack panels vertically (Matrix top, Deck bottom, Market full-width below) |
| **Mobile/Termux** | < 80 cols | Main: Single column; Market table horizontal scroll; Modals full-width |

### 8.2 Main Screen Responsive Behavior

#### Desktop (≥120 cols)
```
┌──────────────┬──────────────────────────────────────┐
│  Matrix      │  Status Bar                          │
│  (1fr)       │  Market Table (1fr)                  │
│  Deck        │  Action Bar                          │
│  (auto)      │                                      │
└──────────────┴──────────────────────────────────────┘
```

#### Tablet (80-119 cols)
```
┌────────────────────────────────────────────────────┐
│ Matrix (1fr, min 15 rows)                          │
├────────────────────────────────────────────────────┤
│ Deck Display                                       │
├────────────────────────────────────────────────────┤
│ Status Bar                                         │
├────────────────────────────────────────────────────┤
│ Market Table (1fr, horizontal scroll if needed)    │
├────────────────────────────────────────────────────┤
│ Action Bar                                         │
└────────────────────────────────────────────────────┘
```

#### Mobile (<80 cols)
```
┌────────────────────────────┐
│ Matrix (10 rows fixed)     │
├────────────────────────────┤
│ Deck Display (condensed)   │
├────────────────────────────┤
│ Status Bar (wrapped 2 lines)│
├────────────────────────────┤
│ Market Table (scrollable)  │
├────────────────────────────┤
│ Action Bar (wrapped 2 lines)│
└────────────────────────────┘
```

### 8.3 Modal Responsive Behavior

| Screen Width | Modal Width | Max Height |
|--------------|-------------|------------|
| ≥ 100 | 70-85 cols (content-dependent) | 35-40 rows |
| 80-99 | 90% viewport | 80% viewport |
| < 80 | 95% viewport | 90% viewport |

- Modals always centered
- Content scrolls internally if exceeds max-height
- Action buttons always visible (sticky bottom or scroll with content)

### 8.4 Component-Level Responsiveness

| Component | Desktop | Tablet | Mobile |
|-----------|---------|--------|--------|
| Status Bar | 3 lines horizontal | 3 lines horizontal | 4-5 lines wrapped |
| Market Table | 6 columns | 6 columns (h-scroll) | 4 cols priority + h-scroll |
| Deck Display | Horizontal bars | Horizontal bars | Vertical list |
| Rep Bars | Horizontal (20 chars) | Horizontal (15 chars) | Vertical labels + values |
| Action Bar | Single line | Single line | Two lines wrapped |

---

## 9. Widget Library Specification

### 9.1 MatrixRain
**Purpose:** Atmospheric background effect

**Props/Config:**
```python
density: float = 0.15        # 0.0-1.0, columns per width
speed: float = 0.08          # Base fall speed (lower = faster)
chars: str = "..."           # Character set
update_interval: float = 0.15 # Seconds between frames
```

**States:**
- `animating`: Normal operation
- `reduced_motion`: Static render, no timer
- `paused`: Hidden (when modal covers)

**Accessibility:**
- `aria-hidden="true"` (decorative)
- Respects `prefers-reduced-motion`

### 9.2 StatusBar
**Purpose:** Persistent global status display

**Layout:** 3 horizontal lines, each a flex container

**Data Source:** Player object (reactive)

**Sections (Line 1):**
- CRED: `¥{cred:,}` — Amber label, white value
- HEAT: `{heat}%` — Amber label, color-coded value (gradient)
- WEEK: `{week}/{max_weeks}` — Amber label, white value
- SECTOR: `{sector_name}` — Amber label, cyan value

**Sections (Line 2):**
- CARGO: `{used}/{max}` — Amber label, white value
- REP: 4× `Faction: {value:+d}` — Amber label, color-coded value (green/red)

**Sections (Line 3):**
- DECK: `ICE{+}{lvl} STE{+}{lvl} CAR{+}{lvl} TRC{+}{lvl} SCN{+}{lvl} AFC{+}{lvl}` — Compact
- LOANS: `¥{weekly_payment:,}/wk` (only if >0) — Red label, white value

**Responsive:**
- Desktop: All inline
- Mobile: Line 1 wraps to 2 lines; Line 2 wraps to 2 lines

### 9.3 MarketTable
**Purpose:** Sortable, selectable market listings

**Columns (Buy Mode):**
| Column | Key | Width | Align | Sortable |
|--------|-----|-------|-------|----------|
| Payload | `name` | 1fr | Left | Yes |
| Class | `class` | 12 | Center | Yes |
| Buy | `buy_price` | 12 | Right | Yes |
| Stock | `stock` | 8 | Right | Yes |
| Demand | `demand` | 10 | Center | Yes |
| Size | `size` | 6 | Right | Yes |

**Columns (Sell Mode):**
| Column | Key | Width | Align | Sortable |
|--------|-----|-------|-------|----------|
| Payload | `name` | 1fr | Left | Yes |
| Class | `class` | 12 | Center | Yes |
| Sell | `sell_price` | 12 | Right | Yes |
| Owned | `owned_qty` | 8 | Right | Yes |
| Demand | `demand` | 10 | Center | Yes |
| Size | `size` | 6 | Right | Yes |

**Interactions:**
- `↑/↓`: Row navigation
- `Enter`: Open Market modal with current row's listing
- `B`/`S`: Toggle mode (rebuilds columns)
- Click header: Sort (cycle: asc → desc → none)
- Cursor: Amber row highlight

**Styling:**
- Header: Matrix bg, black text, bold
- Cursor: Amber bg, black text
- Zebra stripes: Panel bg / Panel bg + 10% brightness
- Empty state: "No market data" centered, dimmed

### 9.4 DeckDisplay
**Purpose:** Visual upgrade progression

**Layout:** Vertical list, one row per upgrade (6 total)

**Row Structure:**
```
[Name:18] [Level:6] [Bar:20] [Cost:12]
```
Example:
```
ICEbreaker      2/4   ██████████░░░░░░░░  ¥180,000
Stealth         1/3   ████░░░░░░░░░░░░░  ¥120,000
Cargo           3/4   ██████████████░░  ¥250,000
Trace Reducer   0/3   ░░░░░░░░░░░░░░░░░  ¥80,000
Scanner         1/2   ██████████░░░░░░░  ¥300,000
Auto-Fence      0/3   ░░░░░░░░░░░░░░░░░  ¥150,000
```

**Bar Fill:** Matrix green (`#00ff41`)
**Bar Empty:** Dark (`#0a0a0f`) with matrix border
**Cost Color:** Matrix (affordable) / Red (unaffordable) / "MAX" (dimmed)
**Title:** "CYBERDECK UPGRADES" in cyan, bold

**Responsive (Mobile):**
```
ICEbreaker     2/4
██████████░░░░░░░░  ¥180,000
Stealth        1/3
████░░░░░░░░░░░░░  ¥120,000
...
```

### 9.5 Modal Components (Reusable)

#### 9.5.1 ModalFrame
**Base component for all modals**
```css
ModalFrame {
    align: center middle;
    width: 70-85;       /* content-dependent */
    max-height: 35-40;  /* content-dependent */
    background: $panel;
    border: thick $accent;  /* accent color per screen */
    padding: 1;
}
```

#### 9.5.2 ModalTitle
```css
ModalTitle {
    color: $accent;
    text-style: bold;
    text-align: center;
    margin-bottom: 1;
}
```

#### 9.5.3 SectionTitle
```css
SectionTitle {
    color: $cyan;
    text-style: bold;
    margin: 1 0;
}
```

#### 9.5.4 InfoRow
```css
InfoRow { height: 1; layout: horizontal; margin-bottom: 1; }
InfoLabel { width: 16-18; color: $amber; }
InfoValue { width: 1fr; color: $white; }
```

#### 9.5.5 InputRow
```css
InputRow { layout: horizontal; margin: 1 0; }
InputRow Label { width: 18; color: $amber; }
InputRow Input { width: 1fr; }
```

#### 9.5.6 ButtonRow
```css
ButtonRow { layout: horizontal; margin-top: 1; }
ButtonRow Button { margin-right: 1; }
```

#### 9.5.7 DataTable (Styled)
```css
DataTable { height: 15-20; margin: 1 0; }
DataTable > .datatable--header { background: $accent; color: $bg; text-style: bold; }
DataTable > .datatable--cursor { background: $amber; color: $bg; }
```

### 9.6 HelpItem
**Purpose:** Keybinding reference row

```css
HelpItem { height: 1; layout: horizontal; }
HelpKey { width: 8; color: $cyan; text-style: bold; }
HelpDesc { width: 1fr; color: $white; }
```

---

## 10. Platform Implementation Notes

### 10.1 Python/Textual (Reference Implementation)

| Design Spec | Textual Implementation |
|-------------|------------------------|
| Screens | `textual.screen.Screen` / `ModalScreen` |
| Widgets | `textual.widget.Widget` subclasses |
| Layout | CSS Grid/Flex (`Horizontal`, `Vertical`, `Container`) |
| Reactive State | `textual.reactive.reactive` |
| Timers | `set_interval` (MatrixRain) |
| Key Bindings | `BINDINGS` class attribute + `Binding` |
| Colors | CSS custom properties (`$matrix`, `$amber`, etc.) |
| Focus | Automatic + `focus()` calls |

### 10.2 Go/Bubble Tea (Port)

| Design Spec | Bubble Tea Implementation |
|-------------|---------------------------|
| Screens | `tea.Model` per screen, stack-based navigation |
| Widgets | Custom components (struct + `View()` method) |
| Layout | `lipgloss` styling + manual layout math |
| State | Immutable `GameState` struct, `Update(msg)` returns new state |
| Timers | `tea.Tick` messages for MatrixRain |
| Key Bindings | `keyMap` in each model, `tea.KeyMsg` handling |
| Colors | `lipgloss.Color` constants matching design tokens |
| Focus | Manual focus management per model |

### 10.3 Cross-Platform Consistency Checklist

- [ ] All 7 screens implemented with identical layout
- [ ] Color tokens match exactly (hex values)
- [ ] Keybindings identical (including gamepad mappings)
- [ ] Turn consumption rules enforced identically
- [ ] Modal data flow identical (dismiss with result payload)
- [ ] Matrix rain density/speed/config same
- [ ] Heat color thresholds identical
- [ ] Status bar line content identical
- [ ] Market table columns/sorting identical
- [ ] Upgrade cost formula identical (exponential)
- [ ] Reduced motion / high contrast toggles functional
- [ ] Screen reader semantics equivalent

---

## 11. Design Tokens (Machine-Readable)

```json
{
  "colors": {
    "bg": "#0a0a0f",
    "panel": "#1a1a2e",
    "fg": "#ffffff",
    "matrix": "#00ff41",
    "amber": "#ffb000",
    "cyan": "#00ffff",
    "pink": "#ff006e",
    "red": "#ff0033",
    "heatLow": "#00ff41",
    "heatMed": "#ffb000",
    "heatHigh": "#ff0033",
    "heatCritical": "#ff0033"
  },
  "typography": {
    "fontFamily": ["JetBrains Mono", "Fira Code", "Cascadia Code", "monospace"],
    "baseSize": "0.9rem",
    "scale": {
      "modalTitle": "1.0rem",
      "sectionTitle": "0.9rem",
      "body": "0.9rem",
      "label": "0.9rem",
      "value": "0.9rem",
      "tableHeader": "0.85rem",
      "tableRow": "0.85rem",
      "actionBar": "0.8rem",
      "matrixRain": "0.8rem"
    },
    "weights": {
      "normal": 400,
      "bold": 700
    }
  },
  "spacing": {
    "xs": "0.25rem",
    "sm": "0.5rem",
    "md": "1rem",
    "lg": "1.5rem"
  },
  "borders": {
    "thin": "1px solid",
    "thick": "2px solid",
    "radius": 0
  },
  "animation": {
    "modalFade": "100ms",
    "matrixRainInterval": "150ms"
  },
  "breakpoints": {
    "desktop": "120ch",
    "tablet": "80ch",
    "mobile": "0"
  },
  "matrixRain": {
    "density": 0.15,
    "baseSpeed": 0.08,
    "charSet": "アイウエオカキクケコサシスセソタチツテトナニヌネノハヒフヘホマミムメモヤユヨラリルレロワヲン0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ!@#$%^&*()",
    "minLength": 5,
    "maxLength": 20
  }
}
```

---

## 12. Validation Checklist

### Visual Compliance
- [ ] All screens use only defined color tokens
- [ ] No hardcoded hex values in component code
- [ ] Matrix rain respects reduced motion
- [ ] Heat gradient renders correctly at all thresholds
- [ ] High contrast mode passes WCAG AA

### Interaction Compliance
- [ ] Every action reachable via keyboard
- [ ] Gamepad mappings complete
- [ ] Focus management correct in all modals
- [ ] Tab order logical
- [ ] No focus traps outside modals

### Information Architecture
- [ ] Status bar shows all 8 priority items
- [ ] Modal hierarchy: Title → Context → Content → Actions
- [ ] Market table sortable by all columns
- [ ] Deck display shows all 6 upgrades
- [ ] Help screen covers all bindings + basics

### State Management
- [ ] Turn consumption rules enforced
- [ ] Screen resume refreshes all data
- [ ] Modal dismiss returns structured result
- [ ] No stale data visible after actions

### Accessibility
- [ ] Screen reader announcements for all state changes
- [ ] ARIA roles correct on all interactive elements
- [ ] Color not sole information carrier
- [ ] Reduced motion disables all animation
- [ ] High contrast mode functional

---

## 13. Revision History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2026-06-16 | PureStar | Initial platform-agnostic UI/UX design specification |

---

*This document is the authoritative UI/UX reference. All platform implementations must conform to these specifications. When in doubt, the design tokens (Section 11) and screen specifications (Section 2.3) are the final word.*