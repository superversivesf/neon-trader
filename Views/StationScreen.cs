using Terminal.Gui;
using NeonTrader.Core;
using NeonTrader.Core.Interfaces;
using NeonTrader.Models;
using NeonTrader.Systems;

namespace NeonTrader.Views;

/// <summary>
/// Station/Docking screen - the main hub interface when docked at a station.
/// Provides Market, Shipyard, Missions, Equipment, and Factions tabs.
/// Implements IRenderable for integration with the game's UI system.
/// </summary>
public sealed class StationScreen : IRenderable
{
    // ─── Dependencies ─────────────────────────────────────────────────────
    private readonly GameState _gameState;
    private readonly TradingSystem _tradingSystem;
    private readonly MissionSystem _missionSystem;
    private readonly Reputation? _reputation;

    // ─── Root Views ───────────────────────────────────────────────────────
    private readonly View _window;
    private readonly TabView _tabView;
    private readonly Label _statusLabel;
    private readonly Label _creditsLabel;
    private readonly Label _locationLabel;
    private readonly Label _cargoLabel;

    // ─── Market Tab ───────────────────────────────────────────────────────
    private TabView.Tab? _marketTab;
    private ListView? _marketListView;
    private Label? _marketDetailLabel;
    private Button? _buyButton;
    private Button? _sellButton;
    private TextField? _quantityField;
    private List<CommodityMarketInfo> _marketItems = new();

    // ─── Shipyard Tab ─────────────────────────────────────────────────────
    private TabView.Tab? _shipyardTab;
    private ListView? _shipyardListView;
    private Label? _shipyardDetailLabel;
    private Label? _currentShipLabel;
    private Button? _buyShipButton;
    private Button? _repairButton;
    private List<ShipClass> _shipyardItems = new();

    // ─── Missions Tab ─────────────────────────────────────────────────────
    private TabView.Tab? _missionsTab;
    private ListView? _missionsListView;
    private Label? _missionDetailLabel;
    private Label? _activeMissionLabel;
    private Button? _acceptMissionButton;
    private Button? _abandonMissionButton;
    private List<MissionInfo> _missionItems = new();

    // ─── Equipment Tab ────────────────────────────────────────────────────
    private TabView.Tab? _equipmentTab;
    private ListView? _equipmentListView;
    private Label? _equipmentDetailLabel;
    private Button? _buyEquipmentButton;
    private List<Equipment> _equipmentItems = new();

    // ─── Factions Tab ─────────────────────────────────────────────────────
    private TabView.Tab? _factionsTab;
    private ListView? _factionsListView;
    private Label? _factionDetailLabel;
    private List<Faction> _factionItems = new();

    // ─── IRenderable Implementation ──────────────────────────────────────
    public View View => _window;
    public int ZIndex => 10;
    public bool IsVisible
    {
        get => _window.Visible;
        set => _window.Visible = value;
    }

    // ─── Constructor ─────────────────────────────────────────────────────
    public StationScreen(
        GameState gameState,
        TradingSystem tradingSystem,
        MissionSystem missionSystem,
        Reputation? reputation = null)
    {
        _gameState = gameState ?? throw new ArgumentNullException(nameof(gameState));
        _tradingSystem = tradingSystem ?? throw new ArgumentNullException(nameof(tradingSystem));
        _missionSystem = missionSystem ?? throw new ArgumentNullException(nameof(missionSystem));
        _reputation = reputation;

        // ── Root Window ───────────────────────────────────────────────────
        _window = new FrameView("Neon Trader — Station Services")
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
        };

        // ── Status Bar (bottom) ──────────────────────────────────────────
        var statusBar = new FrameView("Status")
        {
            X = 0,
            Y = Pos.AnchorEnd(3),
            Width = Dim.Fill(),
            Height = 3,
        };

        _creditsLabel = new Label("Credits: 0")
        {
            X = 1,
            Y = 0,
            Width = 25,
        };
        _locationLabel = new Label("Location: ---")
        {
            X = 27,
            Y = 0,
            Width = 30,
        };
        _cargoLabel = new Label("Cargo: 0/0")
        {
            X = 58,
            Y = 0,
            Width = 25,
        };
        _statusLabel = new Label("Ready.")
        {
            X = 1,
            Y = 1,
            Width = Dim.Fill() - 2,
        };

        statusBar.Add(_creditsLabel, _locationLabel, _cargoLabel, _statusLabel);
        _window.Add(statusBar);

        // ── Tab View ─────────────────────────────────────────────────────
        _tabView = new TabView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill() - 3,
        };

        BuildMarketTab();
        BuildShipyardTab();
        BuildMissionsTab();
        BuildEquipmentTab();
        BuildFactionsTab();

        _window.Add(_tabView);

        // Initial data load (deferred — PushScreen calls Refresh after systems init)
    }

    // ─── Tab Builders ────────────────────────────────────────────────────

    private void BuildMarketTab()
    {
        var view = new View { Width = Dim.Fill(), Height = Dim.Fill() };

        // Left panel: commodity list
        var listFrame = new FrameView("Commodities")
        {
            X = 0,
            Y = 0,
            Width = 40,
            Height = Dim.Fill(),
        };

        _marketListView = new ListView
        {
            X = 1,
            Y = 1,
            Width = Dim.Fill() - 2,
            Height = Dim.Fill() - 2,
            AllowsMarking = false,
        };
        _marketListView.SelectedItemChanged += OnMarketItemSelected;
        listFrame.Add(_marketListView);

        // Right panel: detail + actions
        var detailFrame = new FrameView("Trade")
        {
            X = 40,
            Y = 0,
            Width = Dim.Fill() - 40,
            Height = Dim.Fill(),
        };

        _marketDetailLabel = new Label("Select a commodity to view details.")
        {
            X = 1,
            Y = 1,
            Width = Dim.Fill() - 2,
            Height = 6,
        };

        var quantityLabel = new Label("Quantity:")
        {
            X = 1,
            Y = 8,
            Width = 10,
        };
        _quantityField = new TextField("1")
        {
            X = 12,
            Y = 8,
            Width = 8,
        };

        _buyButton = new Button("Buy")
        {
            X = 1,
            Y = 10,
        };
        _buyButton.Clicked += OnBuyClicked;

        _sellButton = new Button("Sell")
        {
            X = 12,
            Y = 10,
        };
        _sellButton.Clicked += OnSellClicked;

        detailFrame.Add(_marketDetailLabel, quantityLabel, _quantityField, _buyButton, _sellButton);

        view.Add(listFrame, detailFrame);
        _marketTab = new TabView.Tab("Market", view);
        _tabView.AddTab(_marketTab, false);
    }

    private void BuildShipyardTab()
    {
        var view = new View { Width = Dim.Fill(), Height = Dim.Fill() };

        // Current ship info at top
        _currentShipLabel = new Label("Current Ship: ---")
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = 3,
        };

        // Left panel: ship list
        var listFrame = new FrameView("Available Ships")
        {
            X = 0,
            Y = 3,
            Width = 40,
            Height = Dim.Fill() - 3,
        };

        _shipyardListView = new ListView
        {
            X = 1,
            Y = 1,
            Width = Dim.Fill() - 2,
            Height = Dim.Fill() - 2,
            AllowsMarking = false,
        };
        _shipyardListView.SelectedItemChanged += OnShipyardItemSelected;
        listFrame.Add(_shipyardListView);

        // Right panel: detail + actions
        var detailFrame = new FrameView("Ship Details")
        {
            X = 40,
            Y = 3,
            Width = Dim.Fill() - 40,
            Height = Dim.Fill() - 3,
        };

        _shipyardDetailLabel = new Label("Select a ship to view details.")
        {
            X = 1,
            Y = 1,
            Width = Dim.Fill() - 2,
            Height = 8,
        };

        _buyShipButton = new Button("Buy Ship")
        {
            X = 1,
            Y = 10,
        };
        _buyShipButton.Clicked += OnBuyShipClicked;

        _repairButton = new Button("Repair Hull (100 cr/point)")
        {
            X = 16,
            Y = 10,
        };
        _repairButton.Clicked += OnRepairClicked;

        detailFrame.Add(_shipyardDetailLabel, _buyShipButton, _repairButton);

        view.Add(_currentShipLabel, listFrame, detailFrame);
        _shipyardTab = new TabView.Tab("Shipyard", view);
        _tabView.AddTab(_shipyardTab, false);
    }

    private void BuildMissionsTab()
    {
        var view = new View { Width = Dim.Fill(), Height = Dim.Fill() };

        // Active mission info at top
        _activeMissionLabel = new Label("Active Mission: None")
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = 3,
        };

        // Left panel: mission list
        var listFrame = new FrameView("Mission Board")
        {
            X = 0,
            Y = 3,
            Width = 45,
            Height = Dim.Fill() - 3,
        };

        _missionsListView = new ListView
        {
            X = 1,
            Y = 1,
            Width = Dim.Fill() - 2,
            Height = Dim.Fill() - 2,
            AllowsMarking = false,
        };
        _missionsListView.SelectedItemChanged += OnMissionItemSelected;
        listFrame.Add(_missionsListView);

        // Right panel: detail + actions
        var detailFrame = new FrameView("Mission Details")
        {
            X = 45,
            Y = 3,
            Width = Dim.Fill() - 45,
            Height = Dim.Fill() - 3,
        };

        _missionDetailLabel = new Label("Select a mission to view details.")
        {
            X = 1,
            Y = 1,
            Width = Dim.Fill() - 2,
            Height = 8,
        };

        _acceptMissionButton = new Button("Accept Mission")
        {
            X = 1,
            Y = 10,
        };
        _acceptMissionButton.Clicked += OnAcceptMissionClicked;

        _abandonMissionButton = new Button("Abandon Mission")
        {
            X = 20,
            Y = 10,
        };
        _abandonMissionButton.Clicked += OnAbandonMissionClicked;

        detailFrame.Add(_missionDetailLabel, _acceptMissionButton, _abandonMissionButton);

        view.Add(_activeMissionLabel, listFrame, detailFrame);
        _missionsTab = new TabView.Tab("Missions", view);
        _tabView.AddTab(_missionsTab, false);
    }

    private void BuildEquipmentTab()
    {
        var view = new View { Width = Dim.Fill(), Height = Dim.Fill() };

        // Left panel: equipment list
        var listFrame = new FrameView("Equipment Shop")
        {
            X = 0,
            Y = 0,
            Width = 40,
            Height = Dim.Fill(),
        };

        _equipmentListView = new ListView
        {
            X = 1,
            Y = 1,
            Width = Dim.Fill() - 2,
            Height = Dim.Fill() - 2,
            AllowsMarking = false,
        };
        _equipmentListView.SelectedItemChanged += OnEquipmentItemSelected;
        listFrame.Add(_equipmentListView);

        // Right panel: detail + actions
        var detailFrame = new FrameView("Equipment Details")
        {
            X = 40,
            Y = 0,
            Width = Dim.Fill() - 40,
            Height = Dim.Fill(),
        };

        _equipmentDetailLabel = new Label("Select equipment to view details.")
        {
            X = 1,
            Y = 1,
            Width = Dim.Fill() - 2,
            Height = 8,
        };

        _buyEquipmentButton = new Button("Buy & Install")
        {
            X = 1,
            Y = 10,
        };
        _buyEquipmentButton.Clicked += OnBuyEquipmentClicked;

        detailFrame.Add(_equipmentDetailLabel, _buyEquipmentButton);

        view.Add(listFrame, detailFrame);
        _equipmentTab = new TabView.Tab("Equipment", view);
        _tabView.AddTab(_equipmentTab, false);
    }

    private void BuildFactionsTab()
    {
        var view = new View { Width = Dim.Fill(), Height = Dim.Fill() };

        // Left panel: faction list
        var listFrame = new FrameView("Factions")
        {
            X = 0,
            Y = 0,
            Width = 40,
            Height = Dim.Fill(),
        };

        _factionsListView = new ListView
        {
            X = 1,
            Y = 1,
            Width = Dim.Fill() - 2,
            Height = Dim.Fill() - 2,
            AllowsMarking = false,
        };
        _factionsListView.SelectedItemChanged += OnFactionItemSelected;
        listFrame.Add(_factionsListView);

        // Right panel: detail
        var detailFrame = new FrameView("Faction Details")
        {
            X = 40,
            Y = 0,
            Width = Dim.Fill() - 40,
            Height = Dim.Fill(),
        };

        _factionDetailLabel = new Label("Select a faction to view details.")
        {
            X = 1,
            Y = 1,
            Width = Dim.Fill() - 2,
            Height = Dim.Fill() - 2,
        };

        detailFrame.Add(_factionDetailLabel);

        view.Add(listFrame, detailFrame);
        _factionsTab = new TabView.Tab("Factions", view);
        _tabView.AddTab(_factionsTab, false);
    }

    // ─── IRenderable Methods ─────────────────────────────────────────────

    /// <summary>
    /// Refresh all tab data from current game state.
    /// </summary>
    public void Refresh()
    {
        UpdateStatusBar();
        RefreshMarketTab();
        RefreshShipyardTab();
        RefreshMissionsTab();
        RefreshEquipmentTab();
        RefreshFactionsTab();
    }

    /// <summary>
    /// Handle terminal resize.
    /// </summary>
    public void OnResize(int width, int height)
    {
        // Terminal.Gui handles layout automatically via Dim.Fill().
        // Force a refresh to re-render content.
        Application.Refresh();
    }

    // ─── Status Bar ──────────────────────────────────────────────────────

    private void UpdateStatusBar()
    {
        _creditsLabel.Text = $"Credits: {_gameState.Credits:N0} cr";
        _locationLabel.Text = $"Location: {_gameState.CurrentLocation}";
        var used = _gameState.GetTotalCargoUsed();
        _cargoLabel.Text = $"Cargo: {used}/{_gameState.CargoCapacity} t";
    }

    // ─── Market Tab Logic ────────────────────────────────────────────────

    private void RefreshMarketTab()
    {
        var marketInfo = _tradingSystem.GetCurrentMarketInfo();
        if (marketInfo == null)
        {
            _marketItems.Clear();
            _marketListView?.SetSource(new List<string> { "No market data available." });
            return;
        }

        _marketItems = marketInfo.Commodities
            .Where(c => c.IsAvailable)
            .OrderBy(c => c.Category)
            .ThenBy(c => c.CommodityName)
            .ToList();

        var displayItems = _marketItems.Select(c =>
        {
            var trend = c.Trend > 0.05 ? "↑" : c.Trend < -0.05 ? "↓" : "→";
            return $"{c.CommodityName,-20} {c.Price,8:N0} cr {trend}  S:{c.Supply,4} D:{c.Demand,4}";
        }).ToList();

        if (displayItems.Count == 0)
            displayItems.Add("No commodities available at this location.");

        _marketListView?.SetSource(displayItems);
    }

    private void OnMarketItemSelected(ListViewItemEventArgs args)
    {
        var idx = _marketListView?.SelectedItem ?? -1;
        if (idx < 0 || idx >= _marketItems.Count)
        {
            _marketDetailLabel!.Text = "Select a commodity to view details.";
            return;
        }

        var item = _marketItems[idx];
        var playerQty = _gameState.GetCargoQuantity(item.CommodityId);
        var trendText = item.Trend > 0.05 ? "Rising" : item.Trend < -0.05 ? "Falling" : "Stable";

        _marketDetailLabel!.Text =
            $"Commodity: {item.CommodityName}\n" +
            $"Category:  {item.Category}\n" +
            $"Price:     {item.Price:N0} cr/unit\n" +
            $"Supply:    {item.Supply} | Demand: {item.Demand}\n" +
            $"Trend:     {trendText} ({item.Trend:+0.00;-0.00})\n" +
            $"In Cargo:  {playerQty} units";
    }

    private void OnBuyClicked()
    {
        var idx = _marketListView?.SelectedItem ?? -1;
        if (idx < 0 || idx >= _marketItems.Count) return;

        if (!int.TryParse(_quantityField?.Text?.ToString() ?? "1", out var qty) || qty <= 0)
        {
            SetStatus("Invalid quantity.", isError: true);
            return;
        }

        var commodityId = _marketItems[idx].CommodityId;
        var result = _tradingSystem.BuyFromMarket(commodityId, qty);

        if (result.Success)
        {
            SetStatus($"Bought {result.Quantity}x {result.CommodityName} for {Math.Abs(result.TotalCredits):N0} cr.");
        }
        else
        {
            SetStatus($"Buy failed: {result.ErrorMessage}", isError: true);
        }

        Refresh();
    }

    private void OnSellClicked()
    {
        var idx = _marketListView?.SelectedItem ?? -1;
        if (idx < 0 || idx >= _marketItems.Count) return;

        if (!int.TryParse(_quantityField?.Text?.ToString() ?? "1", out var qty) || qty <= 0)
        {
            SetStatus("Invalid quantity.", isError: true);
            return;
        }

        var commodityId = _marketItems[idx].CommodityId;
        var result = _tradingSystem.SellToMarket(commodityId, qty);

        if (result.Success)
        {
            SetStatus($"Sold {result.Quantity}x {result.CommodityName} for {result.TotalCredits:N0} cr.");
        }
        else
        {
            SetStatus($"Sell failed: {result.ErrorMessage}", isError: true);
        }

        Refresh();
    }

    // ─── Shipyard Tab Logic ──────────────────────────────────────────────

    private void RefreshShipyardTab()
    {
        // Current ship info
        var ship = ShipRegistry.Get(_gameState.ShipId);
        var shipClass = ship?.GetShipClass();

        if (ship != null && shipClass != null)
        {
            _currentShipLabel!.Text =
                $"Current Ship: {ship.Name} ({shipClass.Name}) | " +
                $"Hull: {ship.CurrentHull}/{ship.MaxHull} | " +
                $"Shield: {ship.CurrentShield}/{ship.MaxShield} | " +
                $"Fuel: {ship.CurrentFuel}/{ship.FuelCapacity} | " +
                $"Condition: {ship.Condition:P0}";
        }
        else
        {
            _currentShipLabel!.Text = "Current Ship: --- (no ship data)";
        }

        // Available ships
        _shipyardItems = ShipClassRegistry.All
            .Where(sc => sc.IsPlayerPurchasable)
            .OrderBy(sc => sc.BasePrice)
            .ToList();

        var displayItems = _shipyardItems.Select(sc =>
        {
            var owned = ship?.ShipClassId == sc.Id ? " [OWNED]" : "";
            return $"{sc.Name,-22} {sc.Type,-12} {sc.Size,-8} {sc.BasePrice,10:N0} cr{owned}";
        }).ToList();

        if (displayItems.Count == 0)
            displayItems.Add("No ships available for purchase.");

        _shipyardListView?.SetSource(displayItems);
    }

    private void OnShipyardItemSelected(ListViewItemEventArgs args)
    {
        var idx = _shipyardListView?.SelectedItem ?? -1;
        if (idx < 0 || idx >= _shipyardItems.Count)
        {
            _shipyardDetailLabel!.Text = "Select a ship to view details.";
            return;
        }

        var sc = _shipyardItems[idx];
        var ship = ShipRegistry.Get(_gameState.ShipId);
        var owned = ship?.ShipClassId == sc.Id;

        _shipyardDetailLabel!.Text =
            $"Class:       {sc.Name}\n" +
            $"Type:        {sc.Type} | Size: {sc.Size}\n" +
            $"Manufacturer: {sc.Manufacturer}\n" +
            $"Price:       {sc.BasePrice:N0} cr\n" +
            $"\n" +
            $"Hull:        {sc.BaseHullIntegrity} | Shield: {sc.BaseShieldCapacity}\n" +
            $"Cargo:       {sc.BaseCargoCapacity} t | Fuel: {sc.BaseFuelCapacity}\n" +
            $"Speed:       {sc.BaseMaxSpeed:F0} | Turn: {sc.BaseTurnRate:F0}°/s\n" +
            $"Hardpoints:  {sc.HardpointCount}W / {sc.UtilityHardpointCount}U\n" +
            $"Upgrades:    {sc.UpgradeSlotCount} slots\n" +
            (owned ? "\n[You already own this ship class]" : "");
    }

    private void OnBuyShipClicked()
    {
        var idx = _shipyardListView?.SelectedItem ?? -1;
        if (idx < 0 || idx >= _shipyardItems.Count) return;

        var sc = _shipyardItems[idx];
        var ship = ShipRegistry.Get(_gameState.ShipId);

        if (ship?.ShipClassId == sc.Id)
        {
            SetStatus("You already own this ship class.", isError: true);
            return;
        }

        if (_gameState.Credits < sc.BasePrice)
        {
            SetStatus($"Insufficient credits. Need {sc.BasePrice:N0}, have {_gameState.Credits:N0}.", isError: true);
            return;
        }

        // Create new ship from class
        var newShip = new Ship
        {
            Id = $"ship_{Guid.NewGuid():N}",
            Name = sc.Name,
            ShipClassId = sc.Id,
            CurrentHull = sc.BaseHullIntegrity,
            MaxHull = sc.BaseHullIntegrity,
            CurrentShield = sc.BaseShieldCapacity,
            MaxShield = sc.BaseShieldCapacity,
            ShieldRechargeRate = sc.BaseShieldRecharge,
            CargoCapacity = sc.BaseCargoCapacity,
            FuelCapacity = sc.BaseFuelCapacity,
            CurrentFuel = sc.BaseFuelCapacity,
            FuelConsumption = sc.BaseFuelConsumption,
            MaxSpeed = sc.BaseMaxSpeed,
            Acceleration = sc.BaseAcceleration,
            TurnRate = sc.BaseTurnRate,
            WeaponHardpoints = sc.HardpointCount,
            UtilityHardpoints = sc.UtilityHardpointCount,
            UpgradeSlots = sc.UpgradeSlotCount,
            Condition = 1.0,
            HomePort = _gameState.CurrentLocation,
            PurchaseDate = DateTime.UtcNow,
        };

        // Install default equipment
        foreach (var eqId in sc.DefaultEquipment)
        {
            var eq = EquipmentRegistry.Get(eqId);
            if (eq != null)
            {
                var slotId = $"slot_{eq.Type}_{Guid.NewGuid():N[..6]}";
                newShip.InstallEquipment(slotId, eqId);
            }
        }

        // Deduct credits
        _gameState.Credits -= sc.BasePrice;

        // Register and assign
        ShipRegistry.Register(newShip);
        _gameState.ShipId = newShip.Id;
        _gameState.CargoCapacity = newShip.CargoCapacity;
        _gameState.FuelCapacity = newShip.FuelCapacity;
        _gameState.CurrentFuel = newShip.CurrentFuel;

        SetStatus($"Purchased {sc.Name} for {sc.BasePrice:N0} cr. Welcome aboard, Captain!");
        Refresh();
    }

    private void OnRepairClicked()
    {
        var ship = ShipRegistry.Get(_gameState.ShipId);
        if (ship == null)
        {
            SetStatus("No ship to repair.", isError: true);
            return;
        }

        var missing = ship.MaxHull - ship.CurrentHull;
        if (missing <= 0)
        {
            SetStatus("Hull is already at full integrity.");
            return;
        }

        var cost = missing * 100L;
        if (_gameState.Credits < cost)
        {
            // Partial repair
            var affordable = (int)(_gameState.Credits / 100);
            if (affordable <= 0)
            {
                SetStatus("Insufficient credits for repairs (100 cr per hull point).", isError: true);
                return;
            }
            var repaired = ship.RepairHull(affordable);
            _gameState.Credits -= repaired * 100L;
            SetStatus($"Partial repair: +{repaired} hull for {repaired * 100} cr. Hull: {ship.CurrentHull}/{ship.MaxHull}");
        }
        else
        {
            var repaired = ship.RepairHull(missing);
            _gameState.Credits -= cost;
            SetStatus($"Full repair: +{repaired} hull for {cost} cr. Hull: {ship.CurrentHull}/{ship.MaxHull}");
        }

        Refresh();
    }

    // ─── Missions Tab Logic ──────────────────────────────────────────────

    private void RefreshMissionsTab()
    {
        // Active mission
        var active = _gameState.ActiveMission;
        if (active != null)
        {
            var timeLeft = active.ExpiryTime - _gameState.GameTime;
            var timeStr = timeLeft.TotalHours > 0
                ? $"{timeLeft.TotalHours:F0}h remaining"
                : "EXPIRED";

            _activeMissionLabel!.Text =
                $"Active: {active.Title} | " +
                $"Type: {active.Type} | " +
                $"Reward: {active.Reward:N0} cr | " +
                $"{timeStr}";
        }
        else
        {
            _activeMissionLabel!.Text = "Active Mission: None";
        }

        // Available missions
        _missionItems = _missionSystem.GetAvailableMissions()
            .OrderBy(m => m.ExpiryTime)
            .ToList();

        var displayItems = _missionItems.Select(m =>
        {
            var timeLeft = m.ExpiryTime - _gameState.GameTime;
            var timeStr = timeLeft.TotalHours > 0
                ? $"{timeLeft.TotalHours:F0}h"
                : "EXP";

            var typeStr = m.Type switch
            {
                MissionType.Delivery => "DEL",
                MissionType.Procurement => "PRC",
                MissionType.Combat => "CMB",
                MissionType.Exploration => "EXP",
                _ => "???"
            };

            return $"[{typeStr}] {m.Title,-40} {m.Reward,8:N0} cr  {timeStr,5}";
        }).ToList();

        if (displayItems.Count == 0)
            displayItems.Add("No missions available. Check back later.");

        _missionsListView?.SetSource(displayItems);
    }

    private void OnMissionItemSelected(ListViewItemEventArgs args)
    {
        var idx = _missionsListView?.SelectedItem ?? -1;
        if (idx < 0 || idx >= _missionItems.Count)
        {
            _missionDetailLabel!.Text = "Select a mission to view details.";
            return;
        }

        var m = _missionItems[idx];
        var timeLeft = m.ExpiryTime - _gameState.GameTime;
        var timeStr = timeLeft.TotalHours > 0
            ? $"{timeLeft.TotalHours:F1} hours"
            : "EXPIRED";

        _missionDetailLabel!.Text =
            $"Title:       {m.Title}\n" +
            $"Type:        {m.Type}\n" +
            $"Description: {m.Description}\n" +
            $"\n" +
            $"From:        {m.SourceLocation}\n" +
            $"To:          {m.DestinationLocation}\n" +
            $"Reward:      {m.Reward:N0} cr\n" +
            $"Expires:     {timeStr}\n" +
            $"Status:      {m.Status}";
    }

    private void OnAcceptMissionClicked()
    {
        var idx = _missionsListView?.SelectedItem ?? -1;
        if (idx < 0 || idx >= _missionItems.Count) return;

        if (_gameState.ActiveMission != null)
        {
            SetStatus("You already have an active mission. Abandon it first.", isError: true);
            return;
        }

        var missionId = _missionItems[idx].MissionId;
        var success = _missionSystem.AcceptMission(missionId);

        if (success)
        {
            SetStatus($"Mission accepted: {_missionItems[idx].Title}");
        }
        else
        {
            SetStatus("Failed to accept mission. Check reputation requirements.", isError: true);
        }

        Refresh();
    }

    private void OnAbandonMissionClicked()
    {
        if (_gameState.ActiveMission == null)
        {
            SetStatus("No active mission to abandon.");
            return;
        }

        var success = _missionSystem.AbandonMission();

        if (success)
        {
            SetStatus("Mission abandoned. Reputation penalty applied.");
        }
        else
        {
            SetStatus("Failed to abandon mission.", isError: true);
        }

        Refresh();
    }

    // ─── Equipment Tab Logic ─────────────────────────────────────────────

    private void RefreshEquipmentTab()
    {
        var ship = ShipRegistry.Get(_gameState.ShipId);
        var shipClass = ship?.GetShipClass();

        _equipmentItems = EquipmentRegistry.All
            .Where(e => !e.IsInstalled)
            .OrderBy(e => e.Type)
            .ThenBy(e => e.BasePrice)
            .ToList();

        var displayItems = _equipmentItems.Select(e =>
        {
            var compat = shipClass != null && e.MinimumShipSize <= shipClass.Size ? "✓" : "✗";
            return $"{compat} {e.Name,-22} {e.Type,-14} {e.Size,-8} {e.BasePrice,8:N0} cr";
        }).ToList();

        if (displayItems.Count == 0)
            displayItems.Add("No equipment available.");

        _equipmentListView?.SetSource(displayItems);
    }

    private void OnEquipmentItemSelected(ListViewItemEventArgs args)
    {
        var idx = _equipmentListView?.SelectedItem ?? -1;
        if (idx < 0 || idx >= _equipmentItems.Count)
        {
            _equipmentDetailLabel!.Text = "Select equipment to view details.";
            return;
        }

        var e = _equipmentItems[idx];
        var ship = ShipRegistry.Get(_gameState.ShipId);
        var shipClass = ship?.GetShipClass();
        var compatible = shipClass != null && e.MinimumShipSize <= shipClass.Size;

        var mods = e.StatModifiers.Count > 0
            ? string.Join("\n", e.StatModifiers.Select(kvp => $"  {kvp.Key}: {kvp.Value:+0.##;-0.##}"))
            : "  (none)";

        _equipmentDetailLabel!.Text =
            $"Name:        {e.Name}\n" +
            $"Type:        {e.Type} | Size: {e.Size}\n" +
            $"Mount:       {e.MountType} | Rarity: {e.Rarity}\n" +
            $"Manufacturer: {e.Manufacturer}\n" +
            $"Price:       {e.BasePrice:N0} cr\n" +
            $"\n" +
            $"Mass:        {e.Mass:F1} t | Power: {e.PowerDraw:F1} MW\n" +
            $"Heat:        {e.HeatGeneration:F1}/s\n" +
            $"Min Ship:    {e.MinimumShipSize}\n" +
            $"Stat Mods:\n{mods}\n" +
            $"\n" +
            $"Description: {e.Description}\n" +
            (compatible ? "\n[Compatible with your ship]" : "\n[NOT compatible - ship too small]");
    }

    private void OnBuyEquipmentClicked()
    {
        var idx = _equipmentListView?.SelectedItem ?? -1;
        if (idx < 0 || idx >= _equipmentItems.Count) return;

        var eq = _equipmentItems[idx];
        var ship = ShipRegistry.Get(_gameState.ShipId);
        var shipClass = ship?.GetShipClass();

        if (ship == null)
        {
            SetStatus("No ship to install equipment on.", isError: true);
            return;
        }

        if (shipClass != null && eq.MinimumShipSize > shipClass.Size)
        {
            SetStatus($"Equipment requires minimum ship size {eq.MinimumShipSize}. Your ship is {shipClass.Size}.", isError: true);
            return;
        }

        if (_gameState.Credits < eq.BasePrice)
        {
            SetStatus($"Insufficient credits. Need {eq.BasePrice:N0}, have {_gameState.Credits:N0}.", isError: true);
            return;
        }

        // Check hardpoint availability
        if (eq.Type == EquipmentType.Weapon && ship.GetFreeWeaponHardpoints() <= 0)
        {
            SetStatus("No free weapon hardpoints available.", isError: true);
            return;
        }

        if (eq.Type != EquipmentType.Weapon && ship.GetFreeUtilityHardpoints() <= 0)
        {
            SetStatus("No free utility hardpoints available.", isError: true);
            return;
        }

        // Install
        var slotId = $"slot_{eq.Type}_{Guid.NewGuid():N[..6]}";
        if (!ship.InstallEquipment(slotId, eq.Id))
        {
            SetStatus("Failed to install equipment. Slot may be occupied.", isError: true);
            return;
        }

        _gameState.Credits -= eq.BasePrice;
        eq.OnInstalled(ship, slotId);

        SetStatus($"Purchased and installed {eq.Name} for {eq.BasePrice:N0} cr.");
        Refresh();
    }

    // ─── Factions Tab Logic ──────────────────────────────────────────────

    private void RefreshFactionsTab()
    {
        _factionItems = FactionRegistry.All
            .OrderBy(f => f.IsMajorFaction ? 0 : 1)
            .ThenBy(f => f.Name)
            .ToList();

        var displayItems = _factionItems.Select(f =>
        {
            var rep = _reputation?.GetReputation(f.Id) ?? 0;
            var tier = Reputation.GetTierName(rep);
            var major = f.IsMajorFaction ? "★" : " ";
            return $"{major} {f.Name,-22} {f.Alignment,-14} Rep: {rep,+4} ({tier})";
        }).ToList();

        if (displayItems.Count == 0)
            displayItems.Add("No faction data available.");

        _factionsListView?.SetSource(displayItems);
    }

    private void OnFactionItemSelected(ListViewItemEventArgs args)
    {
        var idx = _factionsListView?.SelectedItem ?? -1;
        if (idx < 0 || idx >= _factionItems.Count)
        {
            _factionDetailLabel!.Text = "Select a faction to view details.";
            return;
        }

        var f = _factionItems[idx];
        var rep = _reputation?.GetReputation(f.Id) ?? 0;
        var tier = Reputation.GetTierName(rep);
        var priceMod = _reputation?.GetPriceModifier(f.Id) ?? 0m;
        var missionMod = _reputation?.GetMissionAvailabilityModifier(f.Id) ?? 1.0f;
        var rewardMod = _reputation?.GetMissionRewardModifier(f.Id) ?? 1.0f;

        var territory = f.TerritorySystems.Count > 0
            ? string.Join(", ", f.TerritorySystems.Take(5))
            : "(none)";

        var relations = f.FactionRelations.Count > 0
            ? string.Join("\n", f.FactionRelations.Take(5).Select(kvp =>
            {
                var otherFaction = FactionRegistry.Get(kvp.Key);
                var otherName = otherFaction?.Name ?? kvp.Key;
                return $"  {otherName}: {kvp.Value:+0;-0}";
            }))
            : "  (no relations)";

        _factionDetailLabel!.Text =
            $"Faction:     {f.Name} {(f.IsMajorFaction ? "[MAJOR]" : "")}\n" +
            $"Alignment:   {f.Alignment}\n" +
            $"Description: {f.Description}\n" +
            $"\n" +
            $"Your Rep:    {rep} ({tier})\n" +
            $"Price Mod:   {priceMod:+0.0%;-0.0%}\n" +
            $"Mission Avail: {missionMod:P0}\n" +
            $"Reward Mod:  {rewardMod:P0}\n" +
            $"\n" +
            $"Territory:   {territory}\n" +
            $"Joinable:    {(f.IsJoinable ? "Yes" : "No")}\n" +
            $"Black Market: {(f.OffersBlackMarket ? "Yes" : "No")}\n" +
            $"\n" +
            $"Relations:\n{relations}";
    }

    // ─── Helpers ──────────────────────────────────────────────────────────

    private void SetStatus(string message, bool isError = false)
    {
        _statusLabel.Text = message;
        _statusLabel.ColorScheme = isError
            ? new ColorScheme { Normal = new Terminal.Gui.Attribute(Color.Red, Color.Black) }
            : new ColorScheme { Normal = new Terminal.Gui.Attribute(Color.Green, Color.Black) };

        // Reset color after 5 seconds via a simple timer
        Application.MainLoop?.AddTimeout(TimeSpan.FromSeconds(5), (_) =>
        {
            _statusLabel.ColorScheme = new ColorScheme
            {
                Normal = new Terminal.Gui.Attribute(Color.White, Color.Black)
            };
            return false;
        });
    }
}
