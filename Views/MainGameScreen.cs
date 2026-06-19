using System.Data;
using Terminal.Gui;
using NeonTrader.Core;
using NeonTrader.Core.Events;
using NeonTrader.Core.Interfaces;
using NeonTrader.Models;
using NeonTrader.Systems;

namespace NeonTrader.Views;

/// <summary>
/// Main game screen implementing IRenderable.
/// Layout: StatusBar (bottom), Cargo Inventory (left-top), Navigation Panel (left-bottom),
/// Market Table (right-top), Action Buttons (right-bottom), Log Window (above status bar).
/// Wires to TradingSystem and NavigationSystem APIs for live data.
/// </summary>
public sealed class MainGameScreen : IRenderable, IDisposable
{
    private readonly GameState _gameState;
    private readonly IEventBus _eventBus;
    private readonly TradingSystem _tradingSystem;
    private readonly NavigationSystem _navigationSystem;

    // Event subscriptions
    private IDisposable? _refreshSubscription;
    private IDisposable? _creditsSubscription;
    private IDisposable? _cargoSubscription;
    private IDisposable? _locationSubscription;
    private IDisposable? _marketSubscription;
    private IDisposable? _tradeSubscription;

    // Terminal.Gui views
    private readonly View _window;
    private readonly FrameView _cargoFrame;
    private readonly ListView _cargoList;
    private readonly List<string> _cargoItems;
    private readonly FrameView _navFrame;
    private readonly ListView _navList;
    private readonly List<string> _navItems;
    private readonly FrameView _marketFrame;
    private readonly TableView _marketTable;
    private DataTable _marketDataTable;
    private readonly FrameView _actionFrame;
    private readonly Button _buyButton;
    private readonly Button _sellButton;
    private readonly Button _jumpButton;
    private readonly Button _refuelButton;
    private readonly Button _missionButton;
    private readonly FrameView _logFrame;
    private readonly ListView _logList;
    private readonly List<string> _logMessages;
    private readonly Label _statusBar;

    // Cached data
    private List<JumpDestination> _currentDestinations = new();
    private MarketInfo? _currentMarketInfo;
    private bool _disposed;

    // Layout constants
    private const int StatusBarHeight = 1;
    private const int LogHeight = 4;
    private const double LeftColumnRatio = 0.35;
    private const double MarketHeightRatio = 0.65;

    // IRenderable implementation
    public View View => _window;
    public int ZIndex => 0;
    public bool IsVisible
    {
        get => _window.Visible;
        set => _window.Visible = value;
    }

    public MainGameScreen(
        GameState gameState,
        IEventBus eventBus,
        TradingSystem tradingSystem,
        NavigationSystem navigationSystem)
    {
        _gameState = gameState ?? throw new ArgumentNullException(nameof(gameState));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _tradingSystem = tradingSystem ?? throw new ArgumentNullException(nameof(tradingSystem));
        _navigationSystem = navigationSystem ?? throw new ArgumentNullException(nameof(navigationSystem));

        _cargoItems = new List<string>();
        _navItems = new List<string>();
        _logMessages = new List<string>();
        _marketDataTable = new DataTable();

        // Build the UI hierarchy
        _window = new FrameView("Neon Trader")
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };

        // --- Status Bar (bottom) ---
        _statusBar = new Label("")
        {
            X = 0,
            Y = Pos.AnchorEnd(StatusBarHeight),
            Width = Dim.Fill(),
            Height = StatusBarHeight,
            ColorScheme = Colors.Menu,
            TextAlignment = TextAlignment.Left
        };

        // --- Log Window (above status bar) ---
        _logFrame = new FrameView("Log")
        {
            X = 0,
            Y = Pos.AnchorEnd(StatusBarHeight + LogHeight),
            Width = Dim.Fill(),
            Height = LogHeight
        };
        _logList = new ListView(_logMessages)
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            CanFocus = false
        };
        _logFrame.Add(_logList);

        // --- Cargo Inventory (left-top) ---
        _cargoFrame = new FrameView("Cargo Hold")
        {
            X = 0,
            Y = 0,
            Width = Dim.Percent(35),
            Height = Dim.Percent(50)
        };
        _cargoList = new ListView(_cargoItems)
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            CanFocus = true
        };
        _cargoFrame.Add(_cargoList);

        // --- Navigation Panel (left-bottom) ---
        _navFrame = new FrameView("Navigation")
        {
            X = 0,
            Y = Pos.Percent(50),
            Width = Dim.Percent(35),
            Height = Dim.Fill() - (StatusBarHeight + LogHeight)
        };
        _navList = new ListView(_navItems)
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            CanFocus = true
        };
        _navList.SelectedItemChanged += OnNavSelectionChanged;
        _navFrame.Add(_navList);

        // --- Market Table (right-top) ---
        _marketFrame = new FrameView("Market")
        {
            X = Pos.Percent(35),
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Percent(65)
        };
        _marketTable = new TableView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            FullRowSelect = true,
            MultiSelect = false
        };
        BuildMarketTable();
        _marketTable.Table = _marketDataTable;
        _marketTable.CellActivated += OnMarketCellActivated;
        _marketFrame.Add(_marketTable);

        // --- Action Buttons (right-bottom) ---
        _actionFrame = new FrameView("Actions")
        {
            X = Pos.Percent(35),
            Y = Pos.Percent(65),
            Width = Dim.Fill(),
            Height = Dim.Fill() - (StatusBarHeight + LogHeight)
        };

        _buyButton = new Button("Buy")
        {
            X = 1,
            Y = 1
        };
        _buyButton.Clicked += OnBuyClicked;

        _sellButton = new Button("Sell")
        {
            X = Pos.Right(_buyButton) + 1,
            Y = 1
        };
        _sellButton.Clicked += OnSellClicked;

        _jumpButton = new Button("Jump")
        {
            X = 1,
            Y = Pos.Bottom(_buyButton) + 1
        };
        _jumpButton.Clicked += OnJumpClicked;

        _refuelButton = new Button("Refuel")
        {
            X = Pos.Right(_jumpButton) + 1,
            Y = Pos.Bottom(_buyButton) + 1
        };
        _refuelButton.Clicked += OnRefuelClicked;

        _missionButton = new Button("Missions")
        {
            X = 1,
            Y = Pos.Bottom(_jumpButton) + 1
        };
        _missionButton.Clicked += OnMissionClicked;

        _actionFrame.Add(_buyButton, _sellButton, _jumpButton, _refuelButton, _missionButton);

        // Assemble the window
        _window.Add(
            _cargoFrame,
            _navFrame,
            _marketFrame,
            _actionFrame,
            _logFrame,
            _statusBar
        );

        // Initial data load (deferred — PushScreen calls Refresh after systems init)
        AddLogMessage("Welcome to Neon Trader. Ready for departure.");
    }

    /// <summary>
    /// Build the market data table with columns for commodity, price, supply, demand, trend, and player cargo.
    /// </summary>
    private void BuildMarketTable()
    {
        _marketDataTable = new DataTable();

        _marketDataTable.Columns.Add(new DataColumn("Commodity", typeof(string)));
        _marketDataTable.Columns.Add(new DataColumn("Price", typeof(string)));
        _marketDataTable.Columns.Add(new DataColumn("Supply", typeof(string)));
        _marketDataTable.Columns.Add(new DataColumn("Demand", typeof(string)));
        _marketDataTable.Columns.Add(new DataColumn("Trend", typeof(string)));
        _marketDataTable.Columns.Add(new DataColumn("Cargo", typeof(string)));

        // Set column widths
        var style = _marketTable.Style;
        style.ColumnStyles.Add(_marketDataTable.Columns[0], new TableView.ColumnStyle { MinWidth = 14, MaxWidth = 20 });
        style.ColumnStyles.Add(_marketDataTable.Columns[1], new TableView.ColumnStyle { MinWidth = 10, MaxWidth = 14 });
        style.ColumnStyles.Add(_marketDataTable.Columns[2], new TableView.ColumnStyle { MinWidth = 8, MaxWidth = 12 });
        style.ColumnStyles.Add(_marketDataTable.Columns[3], new TableView.ColumnStyle { MinWidth = 8, MaxWidth = 12 });
        style.ColumnStyles.Add(_marketDataTable.Columns[4], new TableView.ColumnStyle { MinWidth = 6, MaxWidth = 10 });
        style.ColumnStyles.Add(_marketDataTable.Columns[5], new TableView.ColumnStyle { MinWidth = 7, MaxWidth = 10 });
    }

    /// <summary>
    /// Subscribe to relevant game events for automatic UI updates.
    /// </summary>
    private void SubscribeToEvents()
    {
        _refreshSubscription = _eventBus.Subscribe<RefreshUIEvent>(OnRefreshUI);
        _creditsSubscription = _eventBus.Subscribe<CreditsChangedEvent>(OnCreditsChanged);
        _cargoSubscription = _eventBus.Subscribe<CargoChangedEvent>(OnCargoChanged);
        _locationSubscription = _eventBus.Subscribe<LocationChangedEvent>(OnLocationChanged);
        _marketSubscription = _eventBus.Subscribe<MarketUpdatedEvent>(OnMarketUpdated);
        _tradeSubscription = _eventBus.Subscribe<TradeExecutedEvent>(OnTradeExecuted);
    }

    // --- Event Handlers ---

    private void OnRefreshUI(RefreshUIEvent evt)
    {
        // Only refresh if this is a full refresh or relevant region
        if (evt.Region == null || evt.Region == "all" || evt.Region == "main")
        {
            Refresh();
        }
    }

    private void OnCreditsChanged(CreditsChangedEvent evt)
    {
        UpdateStatusBar();
    }

    private void OnCargoChanged(CargoChangedEvent evt)
    {
        RefreshCargoList();
        RefreshMarketTable();
        UpdateStatusBar();
    }

    private void OnLocationChanged(LocationChangedEvent evt)
    {
        AddLogMessage($"Arrived at {evt.NewLocation}.");
        Refresh();
    }

    private void OnMarketUpdated(MarketUpdatedEvent evt)
    {
        RefreshMarketTable();
    }

    private void OnTradeExecuted(TradeExecutedEvent evt)
    {
        var direction = evt.IsBuy ? "Bought" : "Sold";
        AddLogMessage($"{direction} {evt.CommodityId} x{evt.Quantity} @ {evt.PricePerUnit:F0} cr = {evt.TotalCost:N0} cr");
        Refresh();
    }

    // --- IRenderable Methods ---

    /// <summary>
    /// Refresh all UI elements with current game state data.
    /// </summary>
    public void Refresh()
    {
        UpdateStatusBar();
        RefreshCargoList();
        RefreshNavList();
        RefreshMarketTable();
        UpdateButtonStates();
    }

    /// <summary>
    /// Called when the terminal size changes. Re-layouts all child views.
    /// </summary>
    public void OnResize(int width, int height)
    {
        // Terminal.Gui's Dim.Fill() and Pos.Percent() handle most resizing automatically.
        // Force a layout pass on the window.
        _window.SetNeedsDisplay();
    }

    // --- Status Bar ---

    /// <summary>
    /// Update the status bar with credits, fuel, location, and hull.
    /// </summary>
    private void UpdateStatusBar()
    {
        var locationName = PlanetRegistry.Get(_gameState.CurrentLocation)?.Name ?? _gameState.CurrentLocation;
        var hullDisplay = $"{_gameState.Health}/{_gameState.MaxHealth}";

        _statusBar.Text = $" Credits: {_gameState.Credits:N0} | " +
                          $"Fuel: {_gameState.CurrentFuel}/{_gameState.FuelCapacity} | " +
                          $"Location: {locationName} | " +
                          $"Hull: {hullDisplay} ";
    }

    // --- Cargo List ---

    /// <summary>
    /// Refresh the cargo inventory list from GameState.Cargo.
    /// </summary>
    private void RefreshCargoList()
    {
        _cargoItems.Clear();

        if (_gameState.Cargo.IsEmpty)
        {
            _cargoItems.Add("(empty)");
        }
        else
        {
            var totalUsed = _gameState.GetTotalCargoUsed();
            _cargoItems.Add($"--- {totalUsed}/{_gameState.CargoCapacity} tons used ---");

            foreach (var kvp in _gameState.Cargo.OrderBy(k => k.Key))
            {
                var commodity = CommodityRegistry.Get(kvp.Key);
                var name = commodity?.Name ?? kvp.Key;
                _cargoItems.Add($"  {name}: {kvp.Value}");
            }
        }

        _cargoList.SetSource(_cargoItems);
        _cargoFrame.Title = $"Cargo Hold ({_gameState.GetTotalCargoUsed()}/{_gameState.CargoCapacity})";
    }

    // --- Navigation List ---

    /// <summary>
    /// Refresh the navigation panel with available jump destinations.
    /// </summary>
    private void RefreshNavList()
    {
        _navItems.Clear();
        _currentDestinations = _navigationSystem.GetAvailableDestinations();

        if (_currentDestinations.Count == 0)
        {
            _navItems.Add("(no destinations available)");
        }
        else
        {
            var currentPlanet = PlanetRegistry.Get(_gameState.CurrentLocation);
            var currentName = currentPlanet?.Name ?? _gameState.CurrentLocation;
            _navItems.Add($"From: {currentName}");

            foreach (var dest in _currentDestinations)
            {
                var canJump = _navigationSystem.CanJumpTo(dest.PlanetId);
                var marker = canJump ? " " : "!";
                var facilities = "";
                if (dest.HasMarket) facilities += "M";
                if (dest.HasShipyard) facilities += "S";
                if (dest.HasOutfitter) facilities += "O";
                if (facilities.Length > 0) facilities = $" [{facilities}]";

                _navItems.Add($"{marker} {dest.PlanetName} ({dest.SystemName}) - " +
                              $"{dest.DistanceLY:F1} LY, {dest.FuelCost} fuel{facilities}");
            }
        }

        _navList.SetSource(_navItems);
    }

    // --- Market Table ---

    /// <summary>
    /// Refresh the market table with current location's market data.
    /// </summary>
    private void RefreshMarketTable()
    {
        _currentMarketInfo = _tradingSystem.GetCurrentMarketInfo();

        // Clear existing rows
        _marketDataTable.Rows.Clear();

        if (_currentMarketInfo == null || _currentMarketInfo.Commodities.Count == 0)
        {
            _marketFrame.Title = "Market (no data)";
            return;
        }

        _marketFrame.Title = $"Market - {_currentMarketInfo.LocationName} " +
                             $"[{_currentMarketInfo.EconomyType}]";

        foreach (var commodity in _currentMarketInfo.Commodities.OrderBy(c => c.Category).ThenBy(c => c.CommodityName))
        {
            var row = _marketDataTable.NewRow();

            row["Commodity"] = commodity.CommodityName;
            row["Price"] = $"{commodity.Price:N0} cr";
            row["Supply"] = commodity.Supply.ToString();
            row["Demand"] = commodity.Demand.ToString();

            // Trend indicator
            var trendSymbol = commodity.Trend switch
            {
                > 0.2 => "▲",
                < -0.2 => "▼",
                _ => "─"
            };
            row["Trend"] = trendSymbol;

            // Player cargo quantity
            var playerQty = _gameState.GetCargoQuantity(commodity.CommodityId);
            row["Cargo"] = playerQty > 0 ? playerQty.ToString() : "-";

            _marketDataTable.Rows.Add(row);
        }

        _marketTable.Update();
    }

    // --- Button State ---

    /// <summary>
    /// Enable/disable buttons based on current game state.
    /// </summary>
    private void UpdateButtonStates()
    {
        var hasMarketSelection = _marketTable.SelectedRow >= 0 &&
                                 _marketTable.SelectedRow < _marketDataTable.Rows.Count;
        var hasNavSelection = _navList.SelectedItem > 0; // Item 0 is the "From:" header

        _buyButton.Enabled = hasMarketSelection && _tradingSystem.IsRunning;
        _sellButton.Enabled = hasMarketSelection && _tradingSystem.IsRunning;
        _jumpButton.Enabled = hasNavSelection && !_navigationSystem.IsTraveling;
        _refuelButton.Enabled = true; // Always available as a UI action
        _missionButton.Enabled = true;
    }

    // --- Button Click Handlers ---

    private void OnBuyClicked()
    {
        var commodity = GetSelectedCommodity();
        if (commodity == null)
        {
            AddLogMessage("Select a commodity in the market table first.");
            return;
        }

        var quantity = PromptForQuantity("Buy", commodity.CommodityName, commodity.Price);
        if (quantity <= 0) return;

        var result = _tradingSystem.BuyFromMarket(commodity.CommodityId, quantity);
        if (!result.Success)
        {
            AddLogMessage($"Buy failed: {result.ErrorMessage}");
        }
        // Success is logged via TradeExecutedEvent handler
    }

    private void OnSellClicked()
    {
        var commodity = GetSelectedCommodity();
        if (commodity == null)
        {
            AddLogMessage("Select a commodity in the market table first.");
            return;
        }

        var playerQty = _gameState.GetCargoQuantity(commodity.CommodityId);
        if (playerQty <= 0)
        {
            AddLogMessage($"You have no {commodity.CommodityName} to sell.");
            return;
        }

        var quantity = PromptForQuantity("Sell", commodity.CommodityName, commodity.Price, playerQty);
        if (quantity <= 0) return;

        var result = _tradingSystem.SellToMarket(commodity.CommodityId, quantity);
        if (!result.Success)
        {
            AddLogMessage($"Sell failed: {result.ErrorMessage}");
        }
        // Success is logged via TradeExecutedEvent handler
    }

    private void OnJumpClicked()
    {
        var dest = GetSelectedDestination();
        if (dest == null)
        {
            AddLogMessage("Select a destination in the navigation panel first.");
            return;
        }

        if (!_navigationSystem.CanJumpTo(dest.PlanetId))
        {
            AddLogMessage($"Cannot jump to {dest.PlanetName}: insufficient fuel or already traveling.");
            return;
        }

        var confirmed = MessageBox.Query("Jump", 
            $"Jump to {dest.PlanetName}?\n" +
            $"Distance: {dest.DistanceLY:F1} LY\n" +
            $"Fuel cost: {dest.FuelCost}\n" +
            $"Travel time: {dest.TravelTimeHours:F1} hours\n" +
            $"Danger: {dest.DangerLevel}/100",
            "Jump", "Cancel");

        if (confirmed == 0) // 0 = first button (Jump)
        {
            var success = _navigationSystem.StartJump(dest.PlanetId);
            if (success)
            {
                AddLogMessage($"Jumping to {dest.PlanetName}... ETA: {dest.TravelTimeHours:F1} hours.");
                Refresh();
            }
            else
            {
                AddLogMessage($"Failed to initiate jump to {dest.PlanetName}.");
            }
        }
    }

    private void OnRefuelClicked()
    {
        var needed = _gameState.FuelCapacity - _gameState.CurrentFuel;
        if (needed <= 0)
        {
            AddLogMessage("Fuel tanks are already full.");
            return;
        }

        // Refuel cost: 10 credits per fuel unit
        var costPerUnit = 10L;
        var maxAffordable = (int)(_gameState.Credits / costPerUnit);
        var maxRefuel = Math.Min(needed, maxAffordable);

        if (maxRefuel <= 0)
        {
            AddLogMessage($"Not enough credits to refuel. Need {costPerUnit} cr per unit.");
            return;
        }

        var amountStr = $"How many fuel units? (max {maxRefuel}, {costPerUnit} cr/unit)";
        var input = MessageBox.Query("Refuel", amountStr, "Full", "Partial", "Cancel");

        int amount;
        if (input == 0) // Full
        {
            amount = maxRefuel;
        }
        else if (input == 1) // Partial
        {
            var qtyStr = PromptTextInput("Refuel", $"Enter quantity (1-{maxRefuel}):");
            if (!int.TryParse(qtyStr, out amount) || amount < 1 || amount > maxRefuel)
            {
                AddLogMessage("Invalid quantity.");
                return;
            }
        }
        else
        {
            return; // Cancel
        }

        var totalCost = amount * costPerUnit;
        _gameState.Credits -= totalCost;
        _gameState.CurrentFuel += amount;

        AddLogMessage($"Refueled {amount} units for {totalCost:N0} cr. Fuel: {_gameState.CurrentFuel}/{_gameState.FuelCapacity}.");
        _eventBus.Publish(new CreditsChangedEvent
        {
            PreviousCredits = _gameState.Credits + totalCost,
            NewCredits = _gameState.Credits,
            Delta = -totalCost
        });
        Refresh();
    }

    private void OnMissionClicked()
    {
        if (_gameState.AvailableMissions.Count == 0)
        {
            AddLogMessage("No missions available at this location.");
            return;
        }

        var missionNames = _gameState.AvailableMissions
            .Select(m => $"{m.Title} ({m.Type}) - Reward: {m.Reward:N0} cr")
            .ToList();

        var selected = MessageBox.Query("Mission Board",
            $"Available Missions ({missionNames.Count}):\n" +
            string.Join("\n", missionNames.Take(5).Select((n, i) => $"  {i + 1}. {n}")) +
            (missionNames.Count > 5 ? $"\n  ... and {missionNames.Count - 5} more" : ""),
            "Accept", "Cancel");

        if (selected == 0 && _gameState.AvailableMissions.Count > 0)
        {
            // Accept the first mission for now (simplified UI)
            var mission = _gameState.AvailableMissions[0];
            _gameState.ActiveMission = mission;
            _gameState.AvailableMissions.RemoveAt(0);
            AddLogMessage($"Accepted mission: {mission.Title}. Reward: {mission.Reward:N0} cr.");
            Refresh();
        }
    }

    // --- Selection Helpers ---

    private void OnNavSelectionChanged(ListViewItemEventArgs args)
    {
        UpdateButtonStates();
    }

    private void OnMarketCellActivated(TableView.CellActivatedEventArgs args)
    {
        UpdateButtonStates();
    }

    /// <summary>
    /// Get the currently selected commodity from the market table.
    /// </summary>
    private CommodityMarketInfo? GetSelectedCommodity()
    {
        if (_currentMarketInfo == null) return null;
        if (_marketTable.SelectedRow < 0 || _marketTable.SelectedRow >= _marketDataTable.Rows.Count)
            return null;

        var row = _marketDataTable.Rows[_marketTable.SelectedRow];
        var commodityName = row["Commodity"]?.ToString();
        if (string.IsNullOrEmpty(commodityName)) return null;

        return _currentMarketInfo.Commodities
            .FirstOrDefault(c => c.CommodityName == commodityName);
    }

    /// <summary>
    /// Get the currently selected jump destination from the navigation list.
    /// </summary>
    private JumpDestination? GetSelectedDestination()
    {
        var selectedIndex = _navList.SelectedItem;
        // Item 0 is the "From:" header, destinations start at index 1
        if (selectedIndex <= 0 || selectedIndex - 1 >= _currentDestinations.Count)
            return null;

        return _currentDestinations[selectedIndex - 1];
    }

    // --- Input Helpers ---

    /// <summary>
    /// Prompt the user for a trade quantity.
    /// </summary>
    private int PromptForQuantity(string action, string commodityName, decimal pricePerUnit, int maxQuantity = int.MaxValue)
    {
        var input = PromptTextInput($"{action} {commodityName}",
            $"Price: {pricePerUnit:N0} cr/unit\nEnter quantity (max {maxQuantity}):");

        if (string.IsNullOrWhiteSpace(input)) return 0;
        if (!int.TryParse(input.Trim(), out var quantity) || quantity <= 0)
        {
            AddLogMessage("Invalid quantity.");
            return 0;
        }
        if (quantity > maxQuantity)
        {
            AddLogMessage($"Quantity exceeds maximum ({maxQuantity}).");
            return 0;
        }

        return quantity;
    }

    /// <summary>
    /// Show a simple text input dialog and return the entered text.
    /// </summary>
    private static string PromptTextInput(string title, string message)
    {
        // Terminal.Gui 1.18.0 doesn't have a built-in text input dialog,
        // so we use MessageBox.Query with preset options or a simple approach.
        // For numeric input, we use a workaround: show the prompt and let the
        // user type in a subsequent interaction.
        //
        // Since Terminal.Gui 1.18.0 MessageBox.Query only supports button choices,
        // we create a minimal Dialog with a TextField for input.

        var dialog = new Dialog(title, 60, 8);
        var label = new Label(message)
        {
            X = 1,
            Y = 1,
            Width = Dim.Fill() - 2
        };
        var textField = new TextField("")
        {
            X = 1,
            Y = 3,
            Width = Dim.Fill() - 2
        };
        var okButton = new Button("OK")
        {
            X = Pos.Center(),
            Y = 5,
            IsDefault = true
        };

        string result = string.Empty;
        okButton.Clicked += () =>
        {
            result = textField.Text?.ToString() ?? string.Empty;
            Application.RequestStop();
        };

        dialog.Add(label, textField, okButton);
        Application.Run(dialog);

        return result;
    }

    // --- Logging ---

    /// <summary>
    /// Add a message to the log window. Keeps the most recent 100 messages.
    /// </summary>
    private void AddLogMessage(string message)
    {
        var timestamp = _gameState.GameTime.ToString("HH:mm");
        _logMessages.Add($"[{timestamp}] {message}");

        // Trim to last 100 messages
        while (_logMessages.Count > 100)
        {
            _logMessages.RemoveAt(0);
        }

        _logList.SetSource(_logMessages);
        // Scroll to bottom
        _logList.SelectedItem = _logMessages.Count - 1;
    }

    // --- IDisposable ---

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _refreshSubscription?.Dispose();
        _creditsSubscription?.Dispose();
        _cargoSubscription?.Dispose();
        _locationSubscription?.Dispose();
        _marketSubscription?.Dispose();
        _tradeSubscription?.Dispose();

        _buyButton.Clicked -= OnBuyClicked;
        _sellButton.Clicked -= OnSellClicked;
        _jumpButton.Clicked -= OnJumpClicked;
        _refuelButton.Clicked -= OnRefuelClicked;
        _missionButton.Clicked -= OnMissionClicked;
        _navList.SelectedItemChanged -= OnNavSelectionChanged;
        _marketTable.CellActivated -= OnMarketCellActivated;

        _window.Dispose();
    }
}
