# Neon Trader

A cyberpunk trading simulation TUI game built with C# and Terminal.Gui.

Buy low, jack-in, sell high. Manage heat, upgrade your ship, build reputation with factions. Retire with ¥10M or flatline trying.

## Gameplay

Navigate five distinct sectors, trade commodities across dynamic markets, take on missions, engage in ship combat, and manage your reputation with underground factions — all within a 52-week time limit.

- **5 sectors** with unique economies, specialties, and risk profiles
- **7 commodity categories** with dynamic pricing and supply/demand
- **4 mission types** — delivery, procurement, combat, patrol
- **Ship combat** with weapons, shields, enemy AI, and flee mechanics
- **4 factions** with tiered reputation, price modifiers, and unlocks
- **6 ship upgrade paths** — hull, shields, cargo, weapons, engines, utilities
- **Save/load** with multiple slots and auto-save

## Quick Start

```bash
# Clone and run
git clone https://github.com/superversivesf/neon-trader.git
cd neon-trader
dotnet run
```

**Requirements:** .NET 10 SDK

## Keybindings

| Key | Screen |
|-----|--------|
| F1 | Main — status, cargo, market, navigation |
| F2 | Station — trading, shipyard, missions, equipment, factions |
| F3 | Character — stats, skills, reputation, ship, save/load |
| F5 | Quick-save |
| F9 | Quick-load |
| Esc | Quit |

## Project Structure

```
neon-trader/
├── Core/           # Game loop, event bus, interfaces, game state
├── Models/         # 17 data models (Player, Ship, Commodity, etc.)
├── Systems/        # 6 game systems (Trading, Navigation, Combat, etc.)
├── Views/          # 3 Terminal.Gui screens (Main, Station, Character)
├── Data/           # JSON data files (planets, commodities, ships, etc.)
├── Tests/          # xUnit test project — 1,418 tests
├── Program.cs      # Entry point with DI wiring
└── NeonTrader.csproj
```

## Tech Stack

- **.NET 10** / C#
- **Terminal.Gui 1.18** — TUI framework
- **xUnit 2.9** + **Moq 4.20** — test framework
- **Newtonsoft.Json** — serialization
- **System.Reactive** — event bus
- **Microsoft.Extensions.DependencyInjection** — DI container

## Testing

```bash
dotnet test
```

1,418 tests covering models, systems, and views.

## Design Docs

See `DESIGN_INDEX.md` for the full design document catalog (8 documents, ~5,700 lines) covering game mechanics, economy, sectors, events, UI/UX, save system, and multiplayer planning.

## License

MIT
