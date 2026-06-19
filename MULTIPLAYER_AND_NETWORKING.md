# NEON TRADER — Multiplayer & Networking Design

**Version:** 1.0  
**Status:** Draft — Future-Looking Specification  
**Scope:** Multiplayer architecture for post-MVP expansion  
**Dependencies:** DESIGN_OVERVIEW.md, GAME_MECHANICS.md, DATA_MODEL.md

---

## 1. Executive Summary

Neon Trader is currently a deterministic, single-player turn-based simulation. This document specifies how to extend it to multiplayer while preserving **determinism**, **cross-platform compatibility**, and the **core fantasy** (tension, planning, risk management).

### Design Goals
| Goal | Priority | Rationale |
|------|----------|-----------|
| **Preserve determinism** | P0 | Enables replay, anti-cheat, debugging, fair play |
| **Turn-based fits lockstep** | P0 | Natural fit: 1 action = 1 turn = 1 network round |
| **Shared persistent universe** | P1 | Player-driven economy, sector control, reputation |
| **Private instances supported** | P1 | Co-op, competitive, custom rulesets |
| **Cross-platform (desktop/web/mobile)** | P1 | Core design pillar from DESIGN_OVERVIEW |
| **Low bandwidth** | P2 | Terminal-friendly, works on high-latency connections |
| **Anti-cheat via replay verification** | P2 | Deterministic sim makes this tractable |

### Non-Goals (v1 Multiplayer)
- ❌ Real-time action combat
- ❌ Voice/video chat (use Discord)
- ❌ Persistent MMO sharding (single universe, ~50–200 concurrent)
- ❌ Microtransactions / cosmetics

---

## 2. Architecture: Client-Server (Authoritative)

### 2.1 Why Client-Server Over P2P

| Factor | Client-Server | P2P |
|--------|---------------|-----|
| **Determinism guarantee** | Server is single source of truth | Requires consensus, complex |
| **Anti-cheat** | Server validates all inputs | Harder; every peer must verify |
| **NAT traversal** | Single relay/STUN needed | Full mesh or relay mesh |
| **Lobby/matchmaking** | Centralized, simple | Distributed DHT, complex |
| **State persistence** | Server owns save state | Distributed consensus |
| **Mobile/battery** | Thin client, low CPU | High CPU for simulation |
| **Web/WASM** | Native WebSocket support | WebRTC data channels (complex) |

**Decision:** **Authoritative client-server**. The server runs the *exact same simulation core* as single-player. Clients are thin renderers + input senders.

### 2.2 Server Authority Model

```
┌─────────────────────────────────────────────────────────────┐
│                      GAME SERVER                            │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────┐  │
│  │ Simulation  │  │  Lobby &    │  │  Persistence &      │  │
│  │ Core (Go)   │  │  Matchmaking│  │  Replay Storage     │  │
│  │             │  │             │  │                     │  │
│  │ • State     │  │ • Rooms     │  │ • Save/Load         │  │
│  │ • Advance   │  │ • Matchmake │  │ • Replay log        │  │
│  │ • Validate  │  │ • Spectate  │  │ • Leaderboards      │  │
│  └──────┬──────┘  └──────┬──────┘  └──────────┬──────────┘  │
│         │                │                     │             │
│         ▼                ▼                     ▼             │
│  ┌─────────────────────────────────────────────────────┐    │
│  │              NETWORK LAYER (WebSocket / UDP)         │    │
│  └─────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────┘
                            │
          ┌─────────────────┼─────────────────┐
          ▼                 ▼                 ▼
    ┌──────────┐      ┌──────────┐      ┌──────────┐
    │  Python  │      │   Go     │      │   Web    │
    │ Textual  │      │ Bubble   │      │  WASM    │
    │ Client   │      │ Tea      │      │  Client  │
    └──────────┘      └──────────┘      └──────────┘
```

### 2.3 Server Implementation Options

| Option | Pros | Cons | Recommendation |
|--------|------|------|----------------|
| **Go server (reuse internal/game)** | Same core, deterministic, fast | New codebase | **Primary** |
| **Python server (reuse neon_trader)** | Same code, easier dev | Slower, GIL | Fallback |
| **Dedicated game server (Nakama, etc.)** | Features built-in | Not deterministic by default | Avoid for core sim |

**Recommendation:** Build a lightweight Go server in `cmd/neon-server/` reusing `internal/game/`. The simulation core is already platform-agnostic and deterministic.

---

## 3. Synchronization Model: Deterministic Lockstep

### 3.1 Why Lockstep Fits Neon Trader

| Game Property | Lockstep Fit |
|---------------|--------------|
| Turn-based (1 action = 1 week) | Natural round boundary |
| Deterministic simulation | Identical inputs → identical outputs |
| Small state (player + 5 sectors + market) | Low bandwidth |
| No real-time physics | No interpolation needed |
| Cheat-sensitive economy | Server-authoritative validation |

### 3.2 Lockstep Protocol

```
┌─────────────┐     ┌─────────────┐     ┌─────────────┐
│   CLIENT    │     │   SERVER    │     │   CLIENT    │
│  (Player 1) │     │  (Authority)│     │  (Player 2) │
└──────┬──────┘     └──────┬──────┘     └──────┬──────┘
       │                   │                   │
       │  Input: BUY x5    │                   │
       │──────────────────>│                   │
       │                   │  Validate         │
       │                   │  Advance Turn     │
       │                   │  New State        │
       │<──────────────────│                   │
       │  State Delta      │                   │
       │                   │        State Delta│
       │                   │──────────────────>│
       │                   │                   │
       ▼                   ▼                   ▼
  Render new        Authoritative         Render new
  state             state                 state
```

### 3.3 Turn Structure (Networked)

```
SERVER TURN LOOP (runs continuously):
┌────────────────────────────────────────────────────────────┐
│ 1. Collect inputs from all players in current sector/room  │
│    - Timeout: 30s (configurable)                           │
│    - Missing input = "wait" action (no-op, advances turn)  │
│                                                            │
│ 2. Validate each input against server state                │
│    - Cred check, cargo capacity, sector connection         │
│    - Reject invalid → send error, request re-input         │
│                                                            │
│ 3. Deterministic sort: by player_id for reproducibility    │
│                                                            │
│ 4. Apply inputs sequentially:                              │
│    for input in sorted_inputs:                             │
│        execute_action(input)                               │
│        advance_turn()  // Single shared turn counter!      │
│                                                            │
│ 5. Broadcast state delta to all clients                    │
│    - Full state for new joiners                            │
│    - Delta for existing (player diff + sector diff)        │
│                                                            │
│ 6. Check win/loss, persist, log replay                     │
└────────────────────────────────────────────────────────────┘
```

### 3.4 Shared Turn Counter (Critical Design)

**All players in a shared universe share ONE turn counter.**

```
Week 1: Player A buys in Kowloon     → advance_turn() → Week 2
Week 2: Player B travels to Corporate → advance_turn() → Week 3
Week 3: Player C sells in Darknet     → advance_turn() → Week 4
```

**Implications:**
- Market refreshes *once per global turn*, not per player
- Heat decay, events, loan payments happen *once per global turn*
- Creates emergent timing strategy: "Wait for Player A to buy, then I sell"
- Max 52 weeks = game ends for everyone simultaneously

### 3.5 Input Message Format

```json
{
  "type": "player_action",
  "turn": 15,
  "player_id": "p_abc123",
  "action": {
    "kind": "buy",
    "payload_id": "exploit_zero_day",
    "quantity": 3
  },
  "client_seed": 42,           // For deterministic RNG verification
  "ack_turn": 14               // Last confirmed server turn
}
```

### 3.6 State Delta Format

```json
{
  "type": "state_delta",
  "turn": 15,
  "players": {
    "p_abc123": { "cred": 125000, "heat": 23, "cargo": [...], "sector": "corporate_zone", "week": 15 },
    "p_def456": { "cred": 89000, "heat": 45, "cargo": [...], "sector": "kowloon_stack", "week": 15 }
  },
  "sectors": {
    "kowloon_stack": { "heat": 34, "market": { "exploit_zero_day": { "buy": 45000, "sell": 38000, "stock": 2 } } },
    "corporate_zone": { "heat": 67, "market": { ... } }
  },
  "events": [{ "type": "market_spike", "class": "exploits", "sector": "kowloon_stack", "multiplier": 2.5 }],
  "global": { "week": 15, "active_players": 2 }
}
```

### 3.7 Handling Latency & Disconnection

| Scenario | Handling |
|----------|----------|
| **High latency (>500ms)** | 30s input timeout; "wait" action auto-submitted |
| **Disconnect mid-turn** | Player marked AFK; auto-"wait" for 3 turns; then pause |
| **Reconnect** | Full state sync (not delta); replay from last ack if needed |
| **Desync detected** | Server sends full state; client reconciles; log for replay audit |
| **Server restart** | Persisted state + replay log → resume exactly |

---

## 4. Lobby & Matchmaking

### 4.1 Room Types

| Room Type | Max Players | Turn Timer | Persistence | Use Case |
|-----------|-------------|------------|-------------|----------|
| **Shared Universe** | 50–200 | 30s (global) | Persistent | Main mode: persistent economy |
| **Private Instance** | 2–8 | Configurable (15s–5m) | Session-only | Friends, tournaments, testing |
| **Solo (Current)** | 1 | None | Save file | Practice, learning |

### 4.2 Shared Universe Lobby

```
┌────────────────────────────────────────────────────────────┐
│                    SHARED UNIVERSE LOBBY                    │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────┐  │
│  │  Quick Join │  │  Create     │  │  Browse Instances   │  │
│  │  (matchmake)│  │  Private    │  │  (filter: size,     │  │
│  │             │  │  Instance   │  │   ruleset, friends) │  │
│  └─────────────┘  └─────────────┘  └─────────────────────┘  │
│                                                            │
│  Universe Status:  Week 23/52  |  47 pilots online         │
│  Top Sectors:      Darknet (¥2.1M volume)  Corp Zone (high)│
│  Active Events:    Corp War (Kowloon↔Corp), Zero-Day Drop   │
└────────────────────────────────────────────────────────────┘
```

### 4.3 Matchmaking Algorithm (Shared Universe)

```
1. Player clicks "Quick Join"
2. Server checks: player.week, player.cred, player.reputation
3. Match criteria (weighted):
   - Similar week (±5 weeks)          → weight 0.4
   - Similar cred tier (±1 order)     → weight 0.3
   - Complementary sector presence    → weight 0.2
   - Ping < 150ms to server region    → weight 0.1
4. Assign to universe shard (single shard for v1)
5. Spawn at random low-heat sector with fixer access
```

### 4.4 Private Instance Creation

```json
{
  "name": "Neon Syndicate Invitational",
  "max_players": 4,
  "turn_timer_seconds": 60,
  "ruleset": {
    "max_weeks": 52,
    "victory_cred": 10000000,
    "starting_cred": 50000,
    "event_frequency": "normal",
    "raid_lethality": 1.0,
    "loan_interest_base": 0.15
  },
  "invite_only": true,
  "spectator_allowed": true,
  "password_hash": "argon2id$..."
}
```

---

## 5. Shared Universe vs. Private Instances

### 5.1 Shared Universe (Persistent World)

| Property | Specification |
|----------|---------------|
| **Player cap** | ~200 concurrent (single Go process) |
| **Persistence** | Continuous; server saves every 60s + on shutdown |
| **Economy** | Single shared market per sector; player trades affect prices |
| **Sector heat** | Global; all players contribute to sector heat |
| **Events** | Global; affect all players in affected sectors |
| **Reputation** | Persistent across sessions |
| **Leaderboards** | Universe-wide, updated weekly |
| **Join/leave** | Hot-join; new players spawn at week 1, current global week |

**Economy Mechanics:**
- Market prices driven by **aggregate supply/demand** across all players
- Player buy/sell volumes feed into `market_logic.generate_market_prices()`
- High-volume sectors = more volatile, better spreads
- CorpSec presence scales with *total* player heat in sector

### 5.2 Private Instances (Session-Based)

| Property | Specification |
|----------|---------------|
| **Player cap** | 2–8 (configurable) |
| **Persistence** | Session only; save on host disconnect optional |
| **Economy** | Isolated; only instance players affect market |
| **Sector heat** | Instance-local |
| **Events** | Instance-local (can use same weights or custom) |
| **Reputation** | Instance-local or shared (configurable) |
| **Leaderboards** | Instance-only, shown at end |
| **Join/leave** | Invite-only; late join spawns at current week |

### 5.3 Cross-Instance Data (Optional v2)

| Feature | Implementation |
|---------|----------------|
| **Global reputation** | Fixer/Gang/CorpSec/Netrunner rep persists across instances |
| **Cosmetic unlocks** | Deck skins, terminal themes earned in any mode |
| **Statistics** | Lifetime cred earned, sectors visited, raids survived |
| **Achievements** | "First zero-day trade", "Survived CorpSec raid at 90% heat" |

---

## 6. Player-to-Player Trading

### 6.1 Direct Trade (Face-to-Face)

**Requirement:** Both players in same sector, same turn.

```
TURN N:
  Player A: "Propose trade to Player B"  (free action, no turn cost)
  Player B: Receives notification
  
TURN N+1 (or same turn if both online):
  Player B: Accepts / Counters / Declines
  
If Accepted:
  - Cred transferred instantly
  - Cargo swapped instantly
  - Both players: turn NOT advanced (trade is free action)
  - Heat: minimal (+1% for "suspicious activity")
  - Logged for replay/audit
```

**Trade Message:**
```json
{
  "type": "trade_propose",
  "to_player": "p_def456",
  "offer": { "cred": 50000, "cargo": [{ "payload_id": "wetware_neural_boost", "qty": 2 }] },
  "request": { "cred": 0, "cargo": [{ "payload_id": "exploit_buffer_overflow", "qty": 1 }] }
}
```

### 6.2 Market-Mediated Trade (Async)

**No co-location needed.** Uses sector market as intermediary.

```
Player A (Kowloon): Lists "exploit_zero_day" for ¥45,000 (below market)
Player B (Corporate): Sees listing next turn (scanner upgrade reveals)
Player B: Buys from market → Player A receives cred next turn
```

**Implementation:** Extend `MarketListing` with `seller_player_id`. Buy executes against player listing first, then NPC stock.

### 6.3 Trade Restrictions (Anti-Exploit)

| Restriction | Value | Rationale |
|-------------|-------|-----------|
| **Min price** | 50% of current market buy | Prevents cred funneling |
| **Max price** | 200% of current market sell | Prevents laundering |
| **Daily trade limit** | 10 trades/player | Rate limiting |
| **Cred transfer limit** | ¥100K/week between 2 players | Anti-boosting |
| **New player restriction** | No trades week 1–3 | Prevents smurfing |

---

## 7. Leaderboards

### 7.1 Leaderboard Categories

| Category | Metric | Update Frequency | Scope |
|----------|--------|------------------|-------|
| **Cred Kings** | Peak cred achieved | Real-time | Universe / Instance |
| **Speedrunners** | Weeks to ¥10M | On victory | Universe / Instance |
| **Survivors** | Highest week reached | Real-time | Universe / Instance |
| **Heat Masters** | Lowest avg heat at victory | On victory | Universe |
| **Sector Barons** | Total volume traded in sector | Weekly | Universe |
| **Fixer Elite** | Highest fixer reputation | Weekly | Universe |

### 7.2 Leaderboard Implementation

```
Database: Redis sorted sets (score = metric, member = player_id)
           + PostgreSQL for persistence/history

Key: "lb:universe:cred_kings:week_23"
ZADD lb:universe:cred_kings:week_23 12500000 "p_abc123"

Query top 100:
ZREVRANGE lb:universe:cred_kings:week_23 0 99 WITHSCORES

Player rank:
ZREVRANK lb:universe:cred_kings:week_23 "p_abc123" + 1
```

### 7.3 Anti-Cheat on Leaderboards

- **Replay verification required** for top 10 entries
- **Statistical anomaly detection**: actions/turn, cred/turn ratios
- **Manual review flag** for impossible stats (e.g., ¥10M in 5 weeks)
- **Seasonal reset**: New universe = new leaderboard season

---

## 8. Anti-Cheat: Deterministic Replay Verification

### 8.1 Why Deterministic Replay Works

Neon Trader's simulation core is **pure function**: `State' = f(State, Input, Seed)`

| Property | Enables |
|----------|---------|
| **No hidden RNG** | All randomness from seeded PRNG (input + turn) |
| **No floating point** | All math is integer/fixed-point |
| **No system time** | Turn counter only time source |
| **No external input** | No API calls, no filesystem during sim |

### 8.2 Replay Log Format

```json
{
  "version": 1,
  "seed": 1234567890,
  "players": ["p_abc123", "p_def456"],
  "initial_state": { ... },           // Full GameState at turn 0
  "turns": [
    {
      "turn": 1,
      "inputs": [
        { "player": "p_abc123", "action": { "kind": "buy", "payload_id": "stim_combat", "qty": 5 } },
        { "player": "p_def456", "action": { "kind": "travel", "to_sector": "corporate_zone" } }
      ],
      "rng_state": "deadbeef..."       // PRNG state after turn
    }
  ],
  "final_state": { ... },
  "checksum": "sha256:..."             // Of entire replay
}
```

### 8.3 Verification Pipeline

```
┌─────────────────────────────────────────────────────────────┐
│                    VERIFICATION SERVICE                      │
│                                                              │
│  1. Top-10 leaderboard entry submitted                      │
│                                                              │
│  2. Fetch replay log from storage                           │
│                                                              │
│  3. Re-run simulation from initial_state:                   │
│     state = initial_state                                    │
│     for turn in replay.turns:                                │
│         for input in turn.inputs (sorted by player_id):      │
│             state = execute_action(state, input)             │
│             state = advance_turn(state)                      │
│         assert state.rng_state == turn.rng_state             │
│                                                              │
│  4. Compare final_state.hash == replay.final_state.hash     │
│                                                              │
│  5. If mismatch: FLAG for manual review                     │
│                                                              │
│  6. Store verification result (pass/fail + diff)            │
└─────────────────────────────────────────────────────────────┘
```

### 8.4 Cheat Detection Heuristics (Server-Side, Real-Time)

| Heuristic | Threshold | Action |
|-----------|-----------|--------|
| **Impossible cred gain** | >5x market max per turn | Log, flag |
| **Impossible heat reduction** | >20% without upgrade | Log, flag |
| **Invalid state transition** | Any validation fail | Reject input, log |
| **Input timing anomaly** | <50ms human reaction | Shadow ban review |
| **Replay mismatch** | Any field differs | Auto-ban + manual review |

### 8.5 Client-Side Integrity (Defense in Depth)

- **WASM builds**: Compile simulation core to WASM, run in browser sandbox
- **Code signing**: All client binaries signed; server verifies hash on connect
- **Heartbeat challenge**: Server sends random seed; client returns hash(state + seed)
- **Memory scanning** (desktop): Optional, opt-in for competitive play

---

## 9. Cross-Platform Play

### 9.1 Platform Matrix

| Platform | Client Tech | Network | Status |
|----------|-------------|---------|--------|
| Linux/macOS/Windows (Desktop) | Python/Textual or Go/Bubble Tea | WebSocket (TCP) | ✅ Ready |
| Web Browser | Go/WASM + HTML Canvas or PyScript | WebSocket (TCP) | 🔄 Planned |
| Mobile (Termux/ish) | Python/Textual | WebSocket (TCP) | ✅ Works |
| Steam Deck | Python/Textual | WebSocket (TCP) | ✅ Works |

### 9.2 Cross-Platform Considerations

| Challenge | Solution |
|-----------|----------|
| **Terminal capabilities** | Capability negotiation on connect (color, unicode, mouse) |
| **Input latency** | Turn-based = 30s timer hides latency |
| **Screen size** | Responsive TUI layouts (Textual/Bubble Tea both support) |
| **Background/foreground** | Mobile: pause timer when backgrounded (instance only) |
| **Save compatibility** | JSON schema versioned; all platforms read/write same format |

### 9.3 Web/WASM Specifics

```go
// Go/WASM server connection
conn, _ := websocket.Dial("wss://neon-trader.example.com/ws", nil, origin)
// Same protocol as desktop
// Render to <canvas> using termdash or custom cell buffer
// Input: keyboard events → action messages
```

**Fallback:** If WebSocket blocked, HTTPS long-polling (higher latency, acceptable for turn-based).

---

## 10. NAT Traversal

### 10.1 Connection Strategy

```
┌──────────────┐     ┌──────────────┐     ┌──────────────┐
│   CLIENT     │────>│  STUN/TURN   │────>│   SERVER     │
│  (any IP)    │     │   SERVER     │     │  (public IP) │
└──────────────┘     └──────────────┘     └──────────────┘
       │                    │
       │ 1. STUN binding    │
       │    request         │
       │<-------------------│
       │                    │
       │ 2. Direct connect  │
       │    (if both have   │
       │     public IP or   │
       │     cone NAT)      │
       │                    │
       │ 3. TURN relay      │
       │    (if symmetric   │
       │     NAT / carrier) │
```

### 10.2 Implementation

| Layer | Technology | Notes |
|-------|------------|-------|
| **Primary** | WebSocket over TLS (wss://) | Traverses most firewalls/proxies |
| **STUN** | `stun:stun.l.google.com:19302` | Discovers public IP:port |
| **TURN** | Coturn server (self-hosted) | Relay for symmetric NAT; bandwidth cost |
| **ICE** | Built into WebSocket library | Automatic candidate gathering |

**For v1:** **WebSocket over TLS only**. Works for 95%+ of users. Add TURN only if telemetry shows >5% connection failure.

### 10.3 Connection Flow

```go
// Client
func Connect(serverURL string) (*websocket.Conn, error) {
    // 1. Try direct WebSocket
    conn, _, err := websocket.DefaultDialer.Dial(serverURL, nil)
    if err == nil { return conn, nil }
    
    // 2. Try via STUN-discovered reflexive address
    // (handled by OS/browser WebSocket impl)
    
    // 3. Fallback: TURN relay (configured via ICE servers)
    return dialWithICE(serverURL, iceServers)
}
```

---

## 11. Bandwidth Budget

### 11.1 Message Sizes (Estimated)

| Message Type | Frequency | Size (bytes) | Notes |
|--------------|-----------|--------------|-------|
| **Player action** | 1/turn/player | ~200 | JSON, compressed |
| **State delta** | 1/turn | ~2–5 KB | 5 sectors × ~20 payloads + players |
| **Full state sync** | On join/reconnect | ~15–30 KB | All sectors, all players |
| **Chat/emote** | Ad-hoc | ~100 | Optional |
| **Leaderboard query** | On demand | ~2 KB | Top 100 entries |
| **Replay download** | On demand | ~50–200 KB | Full game log |

### 11.2 Bandwidth Calculation (Shared Universe, 50 Players)

```
Per turn (30s):
  50 players × 200B action = 10 KB upstream (to server)
  1 state delta × 5 KB = 5 KB downstream (from server)
  Total: ~15 KB/turn = 0.5 KB/s = ~4 Kbps per client

Per hour: ~1.8 MB
Per day (active 4h): ~7 MB
Per month: ~210 MB
```

**Well within** mobile data limits and satellite internet.

### 11.3 Optimization Techniques

| Technique | Savings | Implementation |
|-----------|---------|----------------|
| **Delta compression** | 80% vs full state | Only send changed fields |
| **MessagePack/Protocol Buffers** | 30–50% vs JSON | Replace JSON for high-frequency messages |
| **Sector interest management** | 60% for large universes | Only send sectors player can scan/access |
| **Turn batching** | N/A (turn-based) | N/A - already batched by turn |
| **Heartbeat only** | When idle | No state updates if no actions |

### 11.4 Bandwidth Tiers

| Tier | Max Players | State Delta Freq | Protocol |
|------|-------------|------------------|----------|
| **Low** (mobile data) | 8 | Every turn | JSON + gzip |
| **Standard** (broadband) | 50 | Every turn | MessagePack |
| **High** (LAN/fiber) | 200 | Every turn | Protocol Buffers |

---

## 12. Server Infrastructure

### 12.1 Single-Process Architecture (v1)

```
┌─────────────────────────────────────────────────────────────┐
│                     neon-server (Go)                         │
│  ┌────────────┐ ┌────────────┐ ┌────────────┐ ┌──────────┐  │
│  │  Universe  │ │  Instance  │ │   Lobby    │ │  Replay  │  │
│  │  Simulator │ │  Manager   │ │  Manager   │ │  Writer  │  │
│  └────────────┘ └────────────┘ └────────────┘ └──────────┘  │
│  ┌────────────┐ ┌────────────┐ ┌────────────┐ ┌──────────┐  │
│  │  WebSocket │ │   HTTP     │ │  Metrics   │ │  Admin   │  │
│  │  Hub       │ │  API       │ │  (Prometheus)│ │  Console │  │
│  └────────────┘ └────────────┘ └────────────┘ └──────────┘  │
└─────────────────────────────────────────────────────────────┘
```

### 12.2 Scaling Path

| Phase | Architecture | Capacity |
|-------|--------------|----------|
| **v1** | Single Go process | 200 concurrent |
| **v2** | Multiple universe shards + lobby coordinator | 2,000 concurrent |
| **v3** | Sector-sharded simulation (each sector on own node) | 20,000+ concurrent |

**v1 is sufficient for years.** Neon Trader's niche = small dedicated community.

### 12.3 Deployment

```yaml
# docker-compose.yml (dev)
services:
  neon-server:
    build: ./cmd/neon-server
    ports:
      - "8080:8080"   # WebSocket
      - "8081:8081"   # HTTP API / metrics
    volumes:
      - ./data:/data  # Saves, replays, leaderboards
    environment:
      - UNIVERSE_SEED=1234567890
      - TURN_SECRET=...

  coturn:
    image: coturn/coturn
    ports:
      - "3478:3478/tcp"
      - "3478:3478/udp"
```

---

## 13. Security Considerations

### 13.1 Threat Model

| Threat | Likelihood | Impact | Mitigation |
|--------|------------|--------|------------|
| **Memory manipulation (desktop)** | Medium | High | Replay verification, code signing |
| **WASM manipulation (web)** | Medium | High | Deterministic core in WASM, server validation |
| **Network injection** | Low | Medium | TLS, message authentication |
| **Replay forgery** | Low | High | Server-signs replays (Ed25519) |
| **DDoS on lobby** | Medium | Medium | Rate limiting, Cloudflare |
| **Cred duping via trade** | Low | High | Server validates all trades, limits |
| **Time manipulation** | Low | Medium | Server-authoritative turn counter |

### 13.2 Cryptographic Guarantees

| Asset | Protection |
|-------|------------|
| **Replay logs** | Ed25519 signed by server private key |
| **Save files** | HMAC-SHA256 (server key) + version |
| **Leaderboard entries** | Signed receipt from verification service |
| **WebSocket messages** | TLS 1.3 (wss://) |
| **TURN credentials** | Short-lived HMAC tokens |

---

## 14. Development Roadmap

### Phase 1: Foundation (4–6 weeks)
- [ ] Extract simulation core to shared `internal/game` (Go) + `neon_trader/core` (Python)
- [ ] Build `cmd/neon-server` with WebSocket hub
- [ ] Implement lockstep protocol (turn collection, validation, broadcast)
- [ ] Add replay logging + verification tool
- [ ] Basic lobby: create/join private instances

### Phase 2: Shared Universe (4–6 weeks)
- [ ] Persistent universe simulator (single process)
- [ ] Shared market economy (aggregate supply/demand)
- [ ] Global events affecting all players
- [ ] Leaderboard service (Redis + PostgreSQL)
- [ ] Hot-join with state sync

### Phase 3: Polish & Scale (ongoing)
- [ ] Player-to-player direct trade
- [ ] Spectator mode (replay live or recorded)
- [ ] Web/WASM client
- [ ] TURN server for NAT traversal
- [ ] Admin console + moderation tools
- [ ] Seasonal universe resets

### Phase 4: Advanced (post-v1)
- [ ] Faction warfare (players align with CorpSec/Gangs)
- [ ] Sector control mechanics
- [ ] Courier missions (player-to-player delivery)
- [ ] Tournament bracket system
- [ ] Cross-instance persistent reputation

---

## 15. API Specification (Server ↔ Client)

### 15.1 WebSocket Message Types

```typescript
// Client → Server
type ClientMessage =
  | { type: "join"; room_id: string; password?: string; player_name: string }
  | { type: "action"; turn: number; action: PlayerAction; client_seed: number }
  | { type: "chat"; channel: "local" | "universe"; message: string }
  | { type: "trade_propose"; to: string; offer: TradeOffer; request: TradeOffer }
  | { type: "trade_respond"; trade_id: string; accept: boolean; counter?: TradeOffer }
  | { type: "ping"; timestamp: number }
  | { type: "request_state"; from_turn?: number }  // For reconnect

// Server → Client
type ServerMessage =
  | { type: "welcome"; player_id: string; room: RoomInfo; state: GameState }
  | { type: "state_delta"; turn: number; delta: StateDelta }
  | { type: "full_state"; turn: number; state: GameState }
  | { type: "action_ack"; turn: number; accepted: boolean; error?: string }
  | { type: "trade_notification"; trade: Trade }
  | { type: "chat"; from: string; channel: string; message: string }
  | { type: "event"; event: GameEvent }
  | { type: "player_joined"; player: PlayerInfo }
  | { type: "player_left"; player_id: string }
  | { type: "leaderboard"; category: string; entries: LeaderboardEntry[] }
  | { type: "error"; code: string; message: string }
  | { type: "pong"; timestamp: number }
```

### 15.2 HTTP API (Lobby, Leaderboards, Admin)

```
GET  /api/v1/rooms                    # List public instances
POST /api/v1/rooms                    # Create private instance
GET  /api/v1/rooms/{id}               # Room details
GET  /api/v1/leaderboards/{category}  # Top 100
GET  /api/v1/leaderboards/{category}/rank/{player_id}  # Player rank
GET  /api/v1/replays/{id}             # Download replay
GET  /api/v1/universe/status          # Current week, player count, events
WS   /ws                              # Game WebSocket
```

---

## 16. Testing Strategy

### 16.1 Determinism Tests

```go
func TestDeterminism(t *testing.T) {
    // Same seed, same inputs → identical state
    seed := int64(42)
    inputs := []Action{Buy("stim", 5), Travel("corporate_zone"), Sell("exploit", 3)}
    
    state1 := RunSimulation(seed, inputs)
    state2 := RunSimulation(seed, inputs)
    
    assert.Equal(t, state1.Hash(), state2.Hash())
}

func TestCrossPlatformDeterminism(t *testing.T) {
    // Go and Python produce identical results
    goState := RunGoSimulation(seed, inputs)
    pyState := RunPythonSimulation(seed, inputs)  // Via subprocess
    
    assert.Equal(t, goState.Hash(), pyState.Hash())
}
```

### 16.2 Network Tests

| Test | Description |
|------|-------------|
| **Lockstep integration** | 4 clients + server, random actions, verify final state |
| **Reconnect resilience** | Kill client mid-game, reconnect, verify state match |
| **Latency simulation** | Add 500ms latency, verify no desync |
| **Packet loss** | 10% drop, verify recovery |
| **Server restart** | Kill server, restart, clients resume from replay |

### 16.3 Load Tests

```
Target: 200 concurrent players, 30s turns
Tool: k6 or custom Go load generator

Scenarios:
- Steady state: all players act every turn
- Burst: 50 players join simultaneously
- Idle: 200 connected, 10 acting
- Replay verify: 1000 games → verify all
```

---

## 17. Appendix: Protocol Buffers Schema (Future)

```protobuf
// messages.proto
syntax = "proto3";

package neontrader;

message PlayerAction {
  string player_id = 1;
  uint32 turn = 2;
  ActionKind kind = 3;
  string payload_id = 4;
  uint32 quantity = 5;
  string target_sector = 6;
  uint64 client_seed = 7;
}

message StateDelta {
  uint32 turn = 1;
  map<string, PlayerState> players = 2;
  map<string, SectorState> sectors = 3;
  repeated GameEvent events = 4;
  GlobalState global = 5;
}

message Replay {
  uint32 version = 1;
  uint64 seed = 2;
  repeated string players = 3;
  GameState initial_state = 4;
  repeated TurnRecord turns = 5;
  GameState final_state = 6;
  bytes checksum = 7;
}
```

---

## 18. Revision History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2026-06-15 | SilverMoon | Initial multiplayer design document |

---

*This document is a living specification. As implementation progresses, update with measured numbers, discovered constraints, and revised decisions. Always trace back to DESIGN_OVERVIEW.md pillars.*