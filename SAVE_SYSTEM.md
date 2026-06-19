# Save System Design Document

## Overview

This document specifies the complete save/load system for Neon Trader, including versioned JSON schema, serialization strategies, RNG seed persistence, save slots, auto-save triggers, migration strategy, corruption recovery, and platform-specific considerations.

---

## 1. Save Schema (Versioned JSON)

### 1.1 Versioning Strategy

```
Schema Version: MAJOR.MINOR.PATCH
- MAJOR: Breaking changes (incompatible structure)
- MINOR: New fields added (backward compatible)
- PATCH: Bug fixes, non-structural changes
```

### 1.2 Current Schema: v1.0.0

```json
{
  "schema_version": "1.0.0",
  "save_metadata": {
    "slot_id": "autosave",
    "save_name": "Auto Save - Week 12",
    "timestamp": "2026-06-15T14:30:00Z",
    "timestamp_unix": 1718464200,
    "game_version": "0.1.0",
    "platform": "linux",
    "playtime_seconds": 3600,
    "checksum": "sha256:abc123..."
  },
  "rng_state": {
    "python_random_state": "...",
    "seed": 1234567890
  },
  "player": { /* Player.to_dict() output */ },
  "sectors": { /* Sector.to_dict() output */ },
  "market_state": { /* Market state snapshot */ },
  "game_state": {
    "current_week": 12,
    "max_weeks": 52,
    "current_sector_id": "kowloon_stack",
    "pending_events": [],
    "active_courier_jobs": []
  }
}
```

### 1.3 Complete Schema Definition

```typescript
interface SaveFile {
  schema_version: string;           // "1.0.0"
  save_metadata: SaveMetadata;
  rng_state: RNGState;
  player: PlayerState;
  sectors: Record<string, SectorState>;
  market_state: MarketState;
  game_state: GameState;
}

interface SaveMetadata {
  slot_id: string;                  // "slot_1", "autosave", "quicksave"
  save_name: string;                // Human-readable name
  timestamp: string;                // ISO 8601 UTC
  timestamp_unix: number;           // Unix epoch seconds
  game_version: string;             // Game version that created this save
  platform: Platform;               // "steam" | "epic" | "mobile" | "web" | "linux" | "macos" | "windows"
  playtime_seconds: number;         // Total playtime
  checksum: string;                 // SHA256 of content (excl. checksum field)
  is_autosave: boolean;             // True for auto-saves
  is_corrupted: boolean;            // Set true if recovery was attempted
}

interface RNGState {
  seed: number;                     // Original seed used for deterministic replay
  python_random_state: string;      // Base64-encoded pickle of random.getstate()
  // Note: Python's random module state is ~625 bytes when pickled
}

interface PlayerState {
  cred: number;
  cargo: CargoItem[];
  deck: DeckState;
  reputation: ReputationState;
  current_sector_id: string;
  week: number;
  max_weeks: number;
  heat: number;
  loans: Record<string, LoanState>;
  completed_courier_jobs: number;
  total_cred_earned: number;
  total_heat_gained: number;
}

interface CargoItem {
  payload_id: string;
  quantity: number;
  buy_price: number;
}

interface DeckState {
  icebreaker: number;
  stealth: number;
  cargo: number;
  trace_reducer: number;
  scanner: number;
  auto_fence: number;
}

interface ReputationState {
  fixer: number;
  gangs: number;
  corpsec: number;
  netrunners: number;
}

interface LoanState {
  amount: number;
  interest: number;
  weeks_left: number;
  original_amount: number;
}

interface SectorState {
  id: string;
  name: string;
  description: string;
  specialties: string[];
  base_heat: number;
  corpsec_presence: number;
  gang_presence: number;
  fixer_access: boolean;
  node_access: boolean;
  current_heat: number;
  market_modifiers: Record<string, number>;
  connections?: string[];           // Runtime-only, not in JSON
}

interface MarketState {
  // Cached market listings per sector for instant restore
  listings: Record<string, MarketListing[]>;
}

interface MarketListing {
  payload_id: string;
  buy_price: number;
  sell_price: number;
  stock: number;
  demand: "high" | "normal" | "low";
}

interface GameState {
  current_week: number;
  max_weeks: number;
  current_sector_id: string;
  pending_events: PendingEvent[];
  active_courier_jobs: CourierJobState[];
}

interface PendingEvent {
  event_type: string;
  scheduled_week: number;
  data: Record<string, any>;
}

interface CourierJobState {
  job_id: string;
  payload_id: string;
  quantity: number;
  origin_sector: string;
  destination_sector: string;
  reward: number;
  deadline_week: number;
  picked_up: boolean;
}
```

---

## 2. Player State Serialization

### 2.1 Current Implementation

The `Player.to_dict()` and `Player.from_dict()` methods in `neon_trader/models/player.py` handle serialization. Key points:

- **Cargo**: Serializes as list of `{payload_id, quantity, buy_price}`
- **Deck**: Serializes upgrade levels only (base costs are static)
- **Reputation**: Four faction values (-100 to 100)
- **Loans**: Dictionary with computed fields

### 2.2 Required Extensions

Add to `Player.to_dict()`:
```python
def to_dict(self) -> dict:
    return {
        # ... existing fields ...
        "max_weeks": self.max_weeks,  # Add this
    }
```

Add to `Player.from_dict()`:
```python
@classmethod
def from_dict(cls, data: dict) -> "Player":
    player = cls()
    # ... existing ...
    player.max_weeks = data.get("max_weeks", 52)  # Add this
    return player
```

---

## 3. Sector/Payload/Market State Serialization

### 3.1 Sectors

Current `Sector.to_dict()` includes all necessary fields. Runtime-only `connections` field should be excluded from save (reconstructed from static data on load).

```python
def to_dict(self) -> dict:
    return {
        "id": self.id,
        "name": self.name,
        "description": self.description,
        "specialties": self.specialties,
        "base_heat": self.base_heat,
        "corpsec_presence": self.corpsec_presence,
        "gang_presence": self.gang_presence,
        "fixer_access": self.fixer_access,
        "node_access": self.node_access,
        "current_heat": self.current_heat,
        "market_modifiers": self.market_modifiers,
        # Exclude: connections (runtime only)
    }
```

### 3.2 Payloads

Payloads are loaded from static JSON data files. Save only runtime state:

```json
{
  "payloads": {
    "neural_chip": {
      "current_price": 15000,
      "stock": 3
    }
  }
}
```

### 3.3 Market

Market is regenerated each turn from payloads + sector state. To enable instant restore without recalculation, cache listings:

```python
def market_to_dict(market: Market) -> dict:
    return {
        "listings": {
            pid: {
                "payload_id": listing.payload.id,
                "buy_price": listing.buy_price,
                "sell_price": listing.sell_price,
                "stock": listing.stock,
                "demand": listing.demand
            }
            for pid, listing in market.listings.items()
        }
    }
```

---

## 4. RNG Seed Persistence

### 4.1 Why Critical

Deterministic replay enables:
- Bug reproduction from player saves
- Fair leaderboards (same seed = same market rolls)
- Replay system for streaming/debugging
- Multiplayer sync (future)

### 4.2 Implementation

```python
import random
import pickle
import base64

def get_rng_state() -> dict:
    """Capture complete RNG state for save."""
    state = random.getstate()
    # state is a tuple: (version, tuple of 624 ints, index, ...)
    pickled = pickle.dumps(state)
    return {
        "seed": getattr(random, '_seed', None),  # Custom attribute
        "python_random_state": base64.b64encode(pickled).decode('ascii')
    }

def restore_rng_state(rng_data: dict) -> None:
    """Restore RNG state from save."""
    if "python_random_state" in rng_data:
        pickled = base64.b64decode(rng_data["python_random_state"])
        state = pickle.loads(pickled)
        random.setstate(state)
    if "seed" in rng_data and rng_data["seed"] is not None:
        random.seed(rng_data["seed"])
```

### 4.3 Seed Initialization

```python
# In app.py new_game():
import time
import os

def initialize_rng(save_data: dict = None) -> int:
    if save_data and "rng_state" in save_data:
        restore_rng_state(save_data["rng_state"])
        return save_data["rng_state"]["seed"]
    
    # New game: generate seed from time + entropy
    seed = int.from_bytes(os.urandom(8), 'big') ^ int(time.time() * 1000000)
    random.seed(seed)
    # Store seed for save
    random._seed = seed  # Custom attribute
    return seed
```

---

## 5. Save Slots

### 5.1 Slot Types

| Slot Type | Count | Purpose | Naming |
|-----------|-------|---------|--------|
| Manual | 10 | Player-initiated saves | `slot_1` ... `slot_10` |
| Auto-save | 3 | Rotating automatic saves | `autosave_0`, `autosave_1`, `autosave_2` |
| Quicksave | 1 | Instant save (F5) | `quicksave` |
| Checkpoint | 5 | Milestone saves (boss, victory, etc) | `checkpoint_1` ... `checkpoint_5` |

### 5.2 Slot Management

```python
SAVE_DIR = Path.home() / ".neon_trader" / "saves"

SLOT_CATEGORIES = {
    "manual": {"prefix": "slot_", "count": 10, "max_age_days": 365},
    "autosave": {"prefix": "autosave_", "count": 3, "max_age_days": 7},
    "quicksave": {"prefix": "quicksave", "count": 1, "max_age_days": 1},
    "checkpoint": {"prefix": "checkpoint_", "count": 5, "max_age_days": 30},
}

def get_slot_path(category: str, index: int = 0) -> Path:
    cfg = SLOT_CATEGORIES[category]
    if cfg["count"] == 1:
        return SAVE_DIR / f"{cfg['prefix']}.json"
    return SAVE_DIR / f"{cfg['prefix']}{index}.json"

def rotate_autosave() -> Path:
    """Rotate autosave slots: 0->1, 1->2, 2->delete, new->0"""
    for i in reversed(range(SLOT_CATEGORIES["autosave"]["count"] - 1)):
        src = get_slot_path("autosave", i)
        dst = get_slot_path("autosave", i + 1)
        if src.exists():
            if i + 1 == SLOT_CATEGORIES["autosave"]["count"] - 1:
                src.unlink()  # Delete oldest
            else:
                src.rename(dst)
    return get_slot_path("autosave", 0)
```

### 5.3 Save Listing with Metadata

```python
def list_saves_with_metadata() -> list[dict]:
    """Return saves with parsed metadata for UI."""
    saves = []
    for path in SAVE_DIR.glob("*.json"):
        try:
            with open(path) as f:
                data = json.load(f)
            meta = data.get("save_metadata", {})
            saves.append({
                "slot_id": path.stem,
                "path": str(path),
                "save_name": meta.get("save_name", path.stem),
                "timestamp": meta.get("timestamp"),
                "timestamp_unix": meta.get("timestamp_unix", 0),
                "game_version": meta.get("game_version", "unknown"),
                "platform": meta.get("platform", "unknown"),
                "playtime_seconds": meta.get("playtime_seconds", 0),
                "week": data.get("game_state", {}).get("current_week", 0),
                "cred": data.get("player", {}).get("cred", 0),
                "is_autosave": meta.get("is_autosave", False),
                "is_corrupted": meta.get("is_corrupted", False),
                "checksum_valid": verify_checksum(path, data),
            })
        except Exception as e:
            saves.append({
                "slot_id": path.stem,
                "path": str(path),
                "error": str(e),
                "is_corrupted": True,
            })
    return sorted(saves, key=lambda s: s.get("timestamp_unix", 0), reverse=True)
```

---

## 6. Auto-Save Triggers

### 6.1 Trigger Conditions

| Trigger | Priority | Description |
|---------|----------|-------------|
| Sector Change | High | Player travels to new sector |
| Turn End | High | `advance_turn()` completes |
| Major Transaction | Medium | Buy/sell > 50k cred |
| Heat Milestone | Medium | Heat crosses 25/50/75/90 |
| Level Up | Medium | Deck upgrade purchased |
| Courier Complete | Medium | Courier job delivered |
| Game Event | Low | Random event triggered |
| Periodic | Low | Every 5 minutes real-time |
| Quit/Background | Critical | App close, suspend, focus loss |

### 6.2 Implementation

```python
# In NeonTraderApp
AUTOSAVE_TRIGGERS = {
    "sector_change": True,
    "turn_end": True,
    "major_transaction": 50000,
    "heat_milestones": [25, 50, 75, 90],
    "deck_upgrade": True,
    "courier_complete": True,
    "periodic_seconds": 300,  # 5 minutes
}

def __init__(self, *args, **kwargs):
    # ... existing ...
    self._last_autosave_week = 0
    self._last_autosave_time = time.time()
    self._last_heat_milestone = 0

def maybe_autosave(self, trigger: str, **context) -> bool:
    """Check if autosave should fire for trigger."""
    if not self.player:
        return False
    
    cfg = AUTOSAVE_TRIGGERS
    
    if trigger == "turn_end" and cfg["turn_end"]:
        if self.player.week != self._last_autosave_week:
            self._last_autosave_week = self.player.week
            return self._do_autosave("turn_end", f"Week {self.player.week}")
    
    elif trigger == "sector_change" and cfg["sector_change"]:
        return self._do_autosave("sector_change", f"Entered {context.get('sector_name', 'sector')}")
    
    elif trigger == "transaction" and cfg["major_transaction"]:
        if context.get("amount", 0) >= cfg["major_transaction"]:
            return self._do_autosave("transaction", f"¥{context['amount']:,} trade")
    
    elif trigger == "heat_change" and cfg["heat_milestones"]:
        heat = self.player.heat
        for milestone in cfg["heat_milestones"]:
            if self._last_heat_milestone < milestone <= heat:
                self._last_heat_milestone = milestone
                return self._do_autosave("heat_milestone", f"Heat {milestone}%")
    
    elif trigger == "periodic":
        if time.time() - self._last_autosave_time >= cfg["periodic_seconds"]:
            self._last_autosave_time = time.time()
            return self._do_autosave("periodic", "Periodic autosave")
    
    return False

def _do_autosave(self, trigger: str, description: str) -> bool:
    """Perform autosave with rotation."""
    try:
        slot_path = rotate_autosave()
        save_game(
            self.player, self.sectors, self.payloads,
            slot=slot_path.stem
        )
        # Update metadata with trigger info
        self._update_save_metadata(slot_path, trigger, description)
        self.notify(f"Auto-saved: {description}", severity="info")
        return True
    except Exception as e:
        self.notify(f"Autosave failed: {e}", severity="error")
        return False
```

### 6.3 Platform-Specific Auto-Save

```python
def on_suspend(self) -> None:
    """Called when app loses focus / goes to background."""
    self._do_autosave("suspend", "App suspended")

def on_resume(self) -> None:
    """Called when app regains focus."""
    pass  # Could check for cloud sync conflicts

def on_quit(self) -> None:
    """Called on clean exit."""
    self._do_autosave("quit", "Game quit")
```

---

## 7. Migration Strategy

### 7.1 Version Detection

```python
CURRENT_SCHEMA_VERSION = "1.0.0"

def detect_save_version(save_data: dict) -> str:
    """Detect schema version from save data."""
    if "schema_version" in save_data:
        return save_data["schema_version"]
    
    # Legacy v0 saves (before schema_version field)
    if "version" in save_data:
        return f"0.{save_data['version']}"
    
    # Very old saves (no version field)
    return "0.0.1"
```

### 7.2 Migration Pipeline

```python
MIGRATIONS = {
    "0.0.1": migrate_001_to_010,
    "0.1.0": migrate_010_to_100,
    # Add new migrations here
}

def migrate_save(save_data: dict) -> dict:
    """Apply migrations to bring save to current version."""
    version = detect_save_version(save_data)
    
    if version == CURRENT_SCHEMA_VERSION:
        return save_data
    
    # Sort migrations by version
    migration_versions = sorted(MIGRATIONS.keys(), key=parse_version)
    
    for mv in migration_versions:
        if parse_version(mv) > parse_version(version):
            print(f"Migrating save from {version} to {mv}")
            save_data = MIGRATIONS[mv](save_data)
            version = mv
    
    save_data["schema_version"] = CURRENT_SCHEMA_VERSION
    return save_data

def parse_version(v: str) -> tuple:
    return tuple(map(int, v.split(".")))
```

### 7.3 Example Migrations

```python
def migrate_001_to_010(data: dict) -> dict:
    """Add schema_version, restructure top-level."""
    # Old format: {version, player, sectors, payloads}
    # New format: {schema_version, save_metadata, rng_state, player, sectors, ...}
    return {
        "schema_version": "0.1.0",
        "save_metadata": {
            "slot_id": "migrated",
            "save_name": "Migrated Save",
            "timestamp": datetime.utcnow().isoformat() + "Z",
            "timestamp_unix": int(time.time()),
            "game_version": "0.1.0",
            "platform": "unknown",
            "playtime_seconds": 0,
            "checksum": "",
            "is_autosave": False,
            "is_corrupted": False,
        },
        "rng_state": {"seed": None, "python_random_state": ""},
        "player": data.get("player", {}),
        "sectors": data.get("sectors", {}),
        "market_state": {"listings": {}},
        "game_state": {
            "current_week": data.get("player", {}).get("week", 1),
            "max_weeks": 52,
            "current_sector_id": data.get("player", {}).get("current_sector_id", "kowloon_stack"),
            "pending_events": [],
            "active_courier_jobs": [],
        }
    }

def migrate_010_to_100(data: dict) -> dict:
    """Add deck.max_weeks, market_state listings, courier jobs."""
    # Ensure player has max_weeks
    if "player" in data and "max_weeks" not in data["player"]:
        data["player"]["max_weeks"] = 52
    
    # Ensure market_state exists
    if "market_state" not in data:
        data["market_state"] = {"listings": {}}
    
    # Ensure game_state has courier jobs
    if "game_state" in data:
        data["game_state"].setdefault("active_courier_jobs", [])
    
    return data
```

### 7.4 Migration Testing

```python
# tests/test_save_migrations.py
import pytest
from neon_trader.utils.save_load import migrate_save

def test_migrate_v001_to_v100():
    """Test migration from oldest format."""
    legacy_save = {
        "version": "0.0.1",
        "player": {"cred": 50000, "week": 5},
        "sectors": {},
        "payloads": {}
    }
    migrated = migrate_save(legacy_save)
    assert migrated["schema_version"] == "1.0.0"
    assert migrated["player"]["max_weeks"] == 52
    assert "rng_state" in migrated
    assert "save_metadata" in migrated
```

---

## 8. Corruption Recovery

### 8.1 Detection

```python
import hashlib
import json

def compute_checksum(data: dict) -> str:
    """Compute SHA256 of save content (excluding checksum field)."""
    # Create copy without checksum
    clean = {k: v for k, v in data.items() if k != "save_metadata" or k != "checksum"}
    if "save_metadata" in clean:
        clean["save_metadata"] = {k: v for k, v in clean["save_metadata"].items() if k != "checksum"}
    content = json.dumps(clean, sort_keys=True, separators=(',', ':'))
    return "sha256:" + hashlib.sha256(content.encode()).hexdigest()

def verify_checksum(path: Path, data: dict) -> bool:
    """Verify save file integrity."""
    stored = data.get("save_metadata", {}).get("checksum", "")
    if not stored:
        return False
    computed = compute_checksum(data)
    return stored == computed
```

### 8.2 Recovery Strategies

| Corruption Type | Detection | Recovery |
|-----------------|-----------|----------|
| Truncated JSON | JSONDecodeError | Try loading as much as valid, reconstruct rest from defaults |
| Invalid checksum | Checksum mismatch | Try loading anyway, mark `is_corrupted=true` |
| Missing fields | Validation error | Fill with defaults, mark corrupted |
| Version mismatch | Unknown schema_version | Run migration pipeline |
| RNG state corrupt | pickle.UnpicklingError | Regenerate seed from timestamp |

### 8.3 Recovery Implementation

```python
def load_game_with_recovery(slot: str) -> tuple[dict | None, list[str]]:
    """Load save with corruption recovery. Returns (data, warnings)."""
    warnings = []
    save_path = SAVE_DIR / f"{slot}.json"
    
    if not save_path.exists():
        return None, ["Save file not found"]
    
    # Try normal load
    try:
        with open(save_path) as f:
            raw = f.read()
        data = json.loads(raw)
    except json.JSONDecodeError as e:
        warnings.append(f"JSON parse error: {e}")
        # Try partial recovery
        data = attempt_partial_recovery(raw, warnings)
        if data is None:
            return None, warnings + ["Total corruption - save unrecoverable"]
    
    # Verify checksum
    if not verify_checksum(save_path, data):
        warnings.append("Checksum mismatch - data may be corrupted")
        data.setdefault("save_metadata", {})["is_corrupted"] = True
    
    # Migrate if needed
    original_version = detect_save_version(data)
    if original_version != CURRENT_SCHEMA_VERSION:
        warnings.append(f"Migrated from v{original_version} to v{CURRENT_SCHEMA_VERSION}")
        data = migrate_save(data)
    
    # Validate required structure
    validation_warnings = validate_save_structure(data)
    warnings.extend(validation_warnings)
    
    return data, warnings

def attempt_partial_recovery(raw: str, warnings: list) -> dict | None:
    """Attempt to recover partial JSON."""
    # Strategy 1: Find last complete object
    # Strategy 2: Use regex to extract known fields
    # Strategy 3: Return minimal valid structure
    pass

def validate_save_structure(data: dict) -> list[str]:
    """Validate save has all required fields, return warnings."""
    warnings = []
    required = ["player", "sectors", "game_state", "save_metadata"]
    for field in required:
        if field not in data:
            warnings.append(f"Missing required field: {field}")
            data[field] = get_default_for(field)
    return warnings
```

---

## 9. Cloud Sync Considerations

### 9.1 Conflict Resolution

```python
class CloudSync:
    """Handle cloud save synchronization."""
    
    def __init__(self, local_dir: Path, cloud_dir: Path):
        self.local_dir = local_dir
        self.cloud_dir = cloud_dir
    
    def sync(self) -> SyncResult:
        """Sync local and cloud saves."""
        local_saves = self._scan_saves(self.local_dir)
        cloud_saves = self._scan_saves(self.cloud_dir)
        
        conflicts = []
        for slot_id, local in local_saves.items():
            cloud = cloud_saves.get(slot_id)
            if cloud:
                resolution = self._resolve_conflict(local, cloud)
                conflicts.append(resolution)
            else:
                # Upload new local save
                self._upload(local)
        
        # Download cloud-only saves
        for slot_id, cloud in cloud_saves.items():
            if slot_id not in local_saves:
                self._download(cloud)
        
        return SyncResult(conflicts=conflicts)
    
    def _resolve_conflict(self, local: SaveInfo, cloud: SaveInfo) -> ConflictResolution:
        """Resolve save conflict using timestamp + playtime."""
        # Prefer newer timestamp
        if local.timestamp_unix > cloud.timestamp_unix:
            return ConflictResolution(action="keep_local", reason="newer")
        elif cloud.timestamp_unix > local.timestamp_unix:
            return ConflictResolution(action="keep_cloud", reason="newer")
        
        # Same timestamp - prefer more playtime
        if local.playtime_seconds >= cloud.playtime_seconds:
            return ConflictResolution(action="keep_local", reason="more_playtime")
        return ConflictResolution(action="keep_cloud", reason="more_playtime")
```

### 9.2 Platform-Specific Cloud APIs

| Platform | API | Notes |
|----------|-----|-------|
| Steam | Steamworks Cloud | Automatic, per-user, ~100MB |
| Epic | Epic Online Services | Similar to Steam |
| iOS/macOS | iCloud Drive | Use `NSUbiquitousKeyValueStore` for small data |
| Android | Google Play Games Services | Saved Games API |
| Web | IndexedDB / File System Access API | Browser-based |
| Standalone | None | Local only |

### 9.3 Steam Integration Example

```python
# steam_cloud.py (optional dependency)
try:
    import steamworks
    STEAM_AVAILABLE = True
except ImportError:
    STEAM_AVAILABLE = False

class SteamCloudSync:
    def __init__(self, app_id: int):
        self.app_id = app_id
        self.client = None
    
    def init(self) -> bool:
        if not STEAM_AVAILABLE:
            return False
        try:
            self.client = steamworks.SteamClient(self.app_id)
            return True
        except:
            return False
    
    def sync_saves(self, local_dir: Path) -> SyncResult:
        if not self.client:
            return SyncResult(error="Steam not initialized")
        
        # Steam cloud uses UGC-style file API
        # Files appear in: ~/Steam/userdata/<steam_id>/<app_id>/remote/
        steam_remote = Path.home() / "Steam" / "userdata" / ... / "remote"
        return CloudSync(local_dir, steam_remote).sync()
```

---

## 10. Platform-Specific Paths

### 10.1 Path Resolution

```python
import sys
import os
from pathlib import Path

def get_save_directory() -> Path:
    """Get platform-appropriate save directory."""
    system = sys.platform
    
    if system == "win32":
        # Windows: %LOCALAPPDATA%\NeonTrader\saves
        base = Path(os.environ.get("LOCALAPPDATA", Path.home() / "AppData" / "Local"))
        return base / "NeonTrader" / "saves"
    
    elif system == "darwin":
        # macOS: ~/Library/Application Support/NeonTrader/saves
        # But if App Store sandboxed: ~/Library/Containers/.../Data/Library/...
        base = Path.home() / "Library" / "Application Support"
        return base / "NeonTrader" / "saves"
    
    elif system.startswith("linux"):
        # Linux: ~/.local/share/NeonTrader/saves (XDG)
        # Or ~/.config/NeonTrader/saves (legacy)
        xdg = os.environ.get("XDG_DATA_HOME", Path.home() / ".local" / "share")
        return Path(xdg) / "NeonTrader" / "saves"
    
    else:
        # Fallback
        return Path.home() / ".neon_trader" / "saves"

# Steam Deck
def get_steam_save_directory(app_id: int) -> Path | None:
    """Get Steam cloud save directory for current user."""
    if sys.platform == "win32":
        base = Path(os.environ.get("LOCALAPPDATA", "")) / ".." / "Steam" / "userdata"
    elif sys.platform == "darwin":
        base = Path.home() / "Library" / "Application Support" / "Steam" / "userdata"
    else:
        base = Path.home() / ".steam" / "steam" / "userdata"
    
    # Find user's steam ID directory
    for steam_id in base.glob("*"):
        if steam_id.is_dir():
            cloud_dir = steam_id / str(app_id) / "remote"
            if cloud_dir.exists():
                return cloud_dir
    return None
```

### 10.2 Mobile Considerations

```python
# Mobile (iOS/Android) - use platform-specific APIs
# iOS: Use FileManager.default.urls(for: .documentDirectory, in: .userDomainMask)
# Android: Use context.getFilesDir() or context.getExternalFilesDir()

# For web (PyScript/WASM):
# Use IndexedDB via JavaScript interop, or File System Access API for "save as" dialog
```

### 10.3 Portable Mode

```python
def get_save_directory(portable: bool = False) -> Path:
    """Get save directory, with optional portable mode."""
    if portable:
        # Portable: saves/ next to executable
        return Path(sys.executable).parent / "saves"
    return get_save_directory()
```

---

## 11. Platform-Agnostic Schema

### 11.1 Schema Design Principles

1. **No platform-specific types**: Use JSON-safe types only (string, number, boolean, array, object)
2. **Endianness-neutral**: All integers are JSON numbers (IEEE 754)
3. **Encoding**: UTF-8 only, no BOM
4. **Line endings**: LF only (JSON doesn't care)
5. **Dates**: ISO 8601 UTC strings + Unix timestamp for sorting
6. **Checksums**: Algorithm-prefixed (sha256:...)

### 11.2 Schema Evolution Rules

| Change Type | Compatible? | Action |
|-------------|-------------|--------|
| Add optional field | Yes | Add with default in `from_dict` |
| Remove field | Yes | Ignore in `from_dict` |
| Rename field | No | Migration required |
| Change field type | No | Migration required |
| Add enum value | Yes | Handle unknown in `from_dict` |
| Remove enum value | No | Migration required |

### 11.3 Example: Cross-Platform Save

A save created on Windows Steam can be loaded on Linux standalone, macOS App Store, or Web because:

```json
{
  "schema_version": "1.0.0",
  "save_metadata": {
    "platform": "steam",  // Informational only
    ...
  },
  "player": {
    "cred": 50000,        // Number, not platform-specific
    ...
  }
}
```

---

## 12. Implementation Checklist

### Phase 1: Core Schema (v1.0.0)
- [ ] Add `schema_version` to save files
- [ ] Add `save_metadata` with all fields
- [ ] Add `rng_state` capture/restore
- [ ] Add `market_state` listings cache
- [ ] Add `game_state` with courier jobs
- [ ] Implement checksum computation/verification

### Phase 2: Save Slots & Auto-Save
- [ ] Implement slot categories (manual, auto, quick, checkpoint)
- [ ] Implement autosave rotation
- [ ] Add autosave triggers to `NeonTraderApp`
- [ ] Add periodic autosave timer
- [ ] Add suspend/quit autosave handlers

### Phase 3: Migration & Recovery
- [ ] Implement version detection
- [ ] Build migration pipeline
- [ ] Write migration for v0.1.0 → v1.0.0
- [ ] Implement corruption detection
- [ ] Implement partial recovery
- [ ] Add validation with defaults

### Phase 4: Cloud & Platform
- [ ] Abstract save directory resolution
- [ ] Add platform detection
- [ ] Implement Steam cloud sync (optional)
- [ ] Implement generic cloud sync interface
- [ ] Add conflict resolution

### Phase 5: Testing
- [ ] Unit tests for serialization round-trip
- [ ] Migration tests for each version
- [ ] Corruption recovery tests
- [ ] Cross-platform save compatibility tests
- [ ] RNG determinism tests (same seed = same game)
- [ ] Performance benchmarks (save/load < 100ms)

---

## 13. API Reference

### save_game()

```python
def save_game(
    player: Player,
    sectors: dict[str, Sector],
    payloads: dict[str, Payload],
    market: Market,
    game_state: GameState,
    rng_state: RNGState,
    slot: str = "autosave",
    save_name: str = None,
    is_autosave: bool = False
) -> Path:
    """Save complete game state to slot."""
```

### load_game()

```python
def load_game(slot: str) -> tuple[SaveData | None, list[str]]:
    """Load game state from slot. Returns (data, warnings)."""
```

### SaveData (TypedDict)

```python
class SaveData(TypedDict):
    schema_version: str
    save_metadata: SaveMetadata
    rng_state: RNGState
    player: PlayerState
    sectors: dict[str, SectorState]
    market_state: MarketState
    game_state: GameState
```

---

## 14. Appendix: Complete Save Example

```json
{
  "schema_version": "1.0.0",
  "save_metadata": {
    "slot_id": "slot_1",
    "save_name": "Week 20 - Kowloon Stack",
    "timestamp": "2026-06-15T14:30:00Z",
    "timestamp_unix": 1718464200,
    "game_version": "0.1.0",
    "platform": "linux",
    "playtime_seconds": 7200,
    "checksum": "sha256:a1b2c3d4e5f6...",
    "is_autosave": false,
    "is_corrupted": false
  },
  "rng_state": {
    "seed": 9876543210,
    "python_random_state": "gAAAAABk..."
  },
  "player": {
    "cred": 1250000,
    "cargo": [
      {"payload_id": "neural_chip", "quantity": 5, "buy_price": 12000},
      {"payload_id": "combat_stim", "quantity": 20, "buy_price": 800}
    ],
    "deck": {
      "icebreaker": 2,
      "stealth": 3,
      "cargo": 1,
      "trace_reducer": 2,
      "scanner": 1,
      "auto_fence": 0
    },
    "reputation": {
      "fixer": 45,
      "gangs": -10,
      "corpsec": -30,
      "netrunners": 20
    },
    "current_sector_id": "kowloon_stack",
    "week": 20,
    "max_weeks": 52,
    "heat": 35,
    "loans": {
      "a1b2c3d4": {
        "amount": 100000,
        "interest": 0.15,
        "weeks_left": 4,
        "original_amount": 100000
      }
    },
    "completed_courier_jobs": 3,
    "total_cred_earned": 2500000,
    "total_heat_gained": 1200
  },
  "sectors": {
    "kowloon_stack": {
      "id": "kowloon_stack",
      "name": "Kowloon Stack",
      "description": "Dense vertical sprawl...",
      "specialties": ["wetware", "stims"],
      "base_heat": 15,
      "corpsec_presence": 40,
      "gang_presence": 60,
      "fixer_access": true,
      "node_access": true,
      "current_heat": 42,
      "market_modifiers": {}
    }
  },
  "market_state": {
    "listings": {
      "neural_chip": {
        "payload_id": "neural_chip",
        "buy_price": 14500,
        "sell_price": 11200,
        "stock": 3,
        "demand": "high"
      }
    }
  },
  "game_state": {
    "current_week": 20,
    "max_weeks": 52,
    "current_sector_id": "kowloon_stack",
    "pending_events": [],
    "active_courier_jobs": [
      {
        "job_id": "courier_001",
        "payload_id": "data_packet",
        "quantity": 1,
        "origin_sector": "kowloon_stack",
        "destination_sector": "neo_shanghai",
        "reward": 50000,
        "deadline_week": 22,
        "picked_up": true
      }
    ]
  }
}
```