using System;
using System.Collections.Generic;
using System.Linq;
using Terminal.Gui;
using NeonTrader.Core.Interfaces;
using NeonTrader.Models;
using NeonTrader.Systems;

namespace NeonTrader.Views;

/// <summary>
/// Character and ship management screen with tabs for Character Sheet,
/// Skills, Reputation, Ship, Equipment, and Save/Load.
/// Implements IRenderable for integration with the game's UI system.
/// </summary>
public sealed class CharacterScreen : IRenderable
{
    private readonly Player _player;
    private readonly SaveSystem? _saveSystem;

    // Root window
    private View _window = null!;

    // Tab view
    private TabView _tabView = null!;
    private TabView.Tab _characterTab = null!;
    private TabView.Tab _skillsTab = null!;
    private TabView.Tab _reputationTab = null!;
    private TabView.Tab _shipTab = null!;
    private TabView.Tab _equipmentTab = null!;
    private TabView.Tab _saveLoadTab = null!;

    // Character tab views
    private ProgressBar _healthBar = null!;
    private ProgressBar _xpBar = null!;
    private Label _nameLabel = null!;
    private Label _backgroundLabel = null!;
    private Label _creditsLabel = null!;
    private Label _levelLabel = null!;
    private Label _skillPointsLabel = null!;
    private Label _locationLabel = null!;
    private Label _playTimeLabel = null!;
    private Label _difficultyLabel = null!;
    private Label _ironmanLabel = null!;

    // Skills tab views
    private ListView _skillsList = null!;
    private Label _skillNameLabel = null!;
    private Label _skillDescLabel = null!;
    private Label _skillLevelLabel = null!;
    private ProgressBar _skillXPBar = null!;
    private Label _skillXPLabel = null!;
    private Label _skillNextPerkLabel = null!;
    private Label _skillUnspentLabel = null!;
    private Button _spendSkillPointBtn = null!;
    private List<SkillDefinition> _skillDefinitions = new();
    private int _selectedSkillIndex = -1;

    // Reputation tab views
    private ListView _factionList = null!;
    private Label _factionNameLabel = null!;
    private Label _factionAlignLabel = null!;
    private Label _factionRepLabel = null!;
    private Label _factionTierLabel = null!;
    private ProgressBar _factionRepBar = null!;
    private Label _factionPriceModLabel = null!;
    private Label _factionMissionModLabel = null!;
    private List<Faction> _factions = new();
    private int _selectedFactionIndex = -1;

    // Ship tab views
    private Label _shipNameLabel = null!;
    private Label _shipClassLabel = null!;
    private ProgressBar _hullBar = null!;
    private Label _hullLabel = null!;
    private ProgressBar _shieldBar = null!;
    private Label _shieldLabel = null!;
    private ProgressBar _fuelBar = null!;
    private Label _fuelLabel = null!;
    private Label _cargoLabel = null!;
    private Label _speedLabel = null!;
    private Label _turnRateLabel = null!;
    private Label _hardpointsLabel = null!;
    private Label _upgradeSlotsLabel = null!;
    private Label _conditionLabel = null!;
    private ListView _upgradesList = null!;
    private List<string> _upgradeDisplayItems = new();

    // Equipment tab views
    private ListView _equipmentList = null!;
    private Label _eqNameLabel = null!;
    private Label _eqTypeLabel = null!;
    private Label _eqSizeLabel = null!;
    private Label _eqMountLabel = null!;
    private Label _eqRarityLabel = null!;
    private Label _eqManufacturerLabel = null!;
    private Label _eqDescLabel = null!;
    private Label _eqMassLabel = null!;
    private Label _eqPowerLabel = null!;
    private Label _eqHeatLabel = null!;
    private ProgressBar _eqConditionBar = null!;
    private Label _eqConditionLabel = null!;
    private Label _eqSlotLabel = null!;
    private FrameView _eqStatsFrame = null!;
    private List<string> _equipmentDisplayItems = new();
    private int _selectedEquipmentIndex = -1;

    // Save/Load tab views
    private ListView _saveSlotList = null!;
    private Label _saveDetailLabel = null!;
    private TextField _saveNameField = null!;
    private Button _saveBtn = null!;
    private Button _loadBtn = null!;
    private Button _deleteBtn = null!;
    private Label _saveStatusLabel = null!;
    private List<SaveSlotInfo> _saveSlots = new();
    private int _selectedSaveSlotIndex = -1;

    // IRenderable implementation
    public View View => _window;
    public int ZIndex => 10;
    public bool IsVisible { get; set; } = true;

    /// <summary>
    /// Creates a new CharacterScreen bound to a player and optional save system.
    /// </summary>
    public CharacterScreen(Player player, SaveSystem? saveSystem = null)
    {
        _player = player ?? throw new ArgumentNullException(nameof(player));
        _saveSystem = saveSystem;
        BuildUI();
    }

    /// <summary>
    /// Builds the complete UI structure.
    /// </summary>
    private void BuildUI()
    {
        _window = new FrameView("CHARACTER & SHIP MANAGEMENT")
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };

        _tabView = new TabView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };

        BuildCharacterTab();
        BuildSkillsTab();
        BuildReputationTab();
        BuildShipTab();
        BuildEquipmentTab();
        BuildSaveLoadTab();

        _tabView.AddTab(_characterTab, andSelect: true);
        _tabView.AddTab(_skillsTab, andSelect: false);
        _tabView.AddTab(_reputationTab, andSelect: false);
        _tabView.AddTab(_shipTab, andSelect: false);
        _tabView.AddTab(_equipmentTab, andSelect: false);
        _tabView.AddTab(_saveLoadTab, andSelect: false);

        _window.Add(_tabView);
    }

    // ──────────────────────────────────────────────
    //  CHARACTER SHEET TAB
    // ──────────────────────────────────────────────

    private void BuildCharacterTab()
    {
        var view = new View
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };

        // Identity section
        var identityFrame = new FrameView("Identity")
        {
            X = 1,
            Y = 1,
            Width = Dim.Fill(2),
            Height = 7
        };

        _nameLabel = new Label("Name: ")
        {
            X = 1,
            Y = 1,
            Width = Dim.Fill(2)
        };
        identityFrame.Add(_nameLabel);

        _backgroundLabel = new Label("Background: ")
        {
            X = 1,
            Y = 2,
            Width = Dim.Fill(2)
        };
        identityFrame.Add(_backgroundLabel);

        _creditsLabel = new Label("Credits: ")
        {
            X = 1,
            Y = 3,
            Width = Dim.Fill(2)
        };
        identityFrame.Add(_creditsLabel);

        _locationLabel = new Label("Location: ")
        {
            X = 1,
            Y = 4,
            Width = Dim.Fill(2)
        };
        identityFrame.Add(_locationLabel);

        _playTimeLabel = new Label("Play Time: ")
        {
            X = 1,
            Y = 5,
            Width = Dim.Fill(2)
        };
        identityFrame.Add(_playTimeLabel);

        view.Add(identityFrame);

        // Status section
        var statusFrame = new FrameView("Status")
        {
            X = 1,
            Y = 9,
            Width = Dim.Fill(2),
            Height = 9
        };

        var healthTitle = new Label("Health")
        {
            X = 1,
            Y = 1,
            Width = 10
        };
        statusFrame.Add(healthTitle);

        _healthBar = new ProgressBar
        {
            X = 12,
            Y = 1,
            Width = Dim.Fill(14),
            Height = 1,
            Fraction = 1.0f,
            ProgressBarFormat = ProgressBarFormat.Simple,
            ProgressBarStyle = ProgressBarStyle.MarqueeBlocks
        };
        statusFrame.Add(_healthBar);

        var healthValue = new Label("100/100")
        {
            X = Pos.Right(_healthBar) + 1,
            Y = 1,
            Width = 12
        };
        _healthBar.Text = healthValue.Text;
        statusFrame.Add(healthValue);

        var levelTitle = new Label("Level")
        {
            X = 1,
            Y = 2,
            Width = 10
        };
        statusFrame.Add(levelTitle);

        _levelLabel = new Label("1")
        {
            X = 12,
            Y = 2,
            Width = Dim.Fill(2)
        };
        statusFrame.Add(_levelLabel);

        var xpTitle = new Label("Experience")
        {
            X = 1,
            Y = 3,
            Width = 10
        };
        statusFrame.Add(xpTitle);

        _xpBar = new ProgressBar
        {
            X = 12,
            Y = 3,
            Width = Dim.Fill(14),
            Height = 1,
            Fraction = 0.0f,
            ProgressBarFormat = ProgressBarFormat.Simple,
            ProgressBarStyle = ProgressBarStyle.MarqueeBlocks
        };
        statusFrame.Add(_xpBar);

        var xpValue = new Label("0/1000")
        {
            X = Pos.Right(_xpBar) + 1,
            Y = 3,
            Width = 12
        };
        _xpBar.Text = xpValue.Text;
        statusFrame.Add(xpValue);

        _skillPointsLabel = new Label("Skill Points: 0")
        {
            X = 1,
            Y = 4,
            Width = Dim.Fill(2)
        };
        statusFrame.Add(_skillPointsLabel);

        _difficultyLabel = new Label("Difficulty: Normal")
        {
            X = 1,
            Y = 5,
            Width = Dim.Fill(2)
        };
        statusFrame.Add(_difficultyLabel);

        _ironmanLabel = new Label("Ironman: OFF")
        {
            X = 1,
            Y = 6,
            Width = Dim.Fill(2)
        };
        statusFrame.Add(_ironmanLabel);

        view.Add(statusFrame);

        // Statistics section
        var statsFrame = new FrameView("Statistics")
        {
            X = 1,
            Y = 19,
            Width = Dim.Fill(2),
            Height = 10
        };

        var statsText = new Label("")
        {
            X = 1,
            Y = 1,
            Width = Dim.Fill(2),
            Height = Dim.Fill(2)
        };
        statsFrame.Add(statsText);

        view.Add(statsFrame);

        _characterTab = new TabView.Tab("Character Sheet", view);
    }

    // ──────────────────────────────────────────────
    //  SKILLS TAB
    // ──────────────────────────────────────────────

    private void BuildSkillsTab()
    {
        var view = new View
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };

        // Skills list (left panel)
        var skillsListFrame = new FrameView("Skills")
        {
            X = 1,
            Y = 1,
            Width = 35,
            Height = Dim.Fill(2)
        };

        _skillsList = new ListView
        {
            X = 1,
            Y = 1,
            Width = Dim.Fill(2),
            Height = Dim.Fill(4),
            AllowsMarking = false
        };
        _skillsList.SelectedItemChanged += OnSkillSelected;
        skillsListFrame.Add(_skillsList);

        _skillUnspentLabel = new Label("Unspent Points: 0")
        {
            X = 1,
            Y = Pos.Bottom(_skillsList) + 1,
            Width = Dim.Fill(2)
        };
        skillsListFrame.Add(_skillUnspentLabel);

        _spendSkillPointBtn = new Button("Spend Point")
        {
            X = 1,
            Y = Pos.Bottom(_skillUnspentLabel) + 1,
            Width = 20
        };
        _spendSkillPointBtn.Clicked += OnSpendSkillPoint;
        skillsListFrame.Add(_spendSkillPointBtn);

        view.Add(skillsListFrame);

        // Skill detail (right panel)
        var skillDetailFrame = new FrameView("Skill Details")
        {
            X = 37,
            Y = 1,
            Width = Dim.Fill(38),
            Height = Dim.Fill(2)
        };

        _skillNameLabel = new Label("Select a skill")
        {
            X = 1,
            Y = 1,
            Width = Dim.Fill(2)
        };
        skillDetailFrame.Add(_skillNameLabel);

        _skillDescLabel = new Label("")
        {
            X = 1,
            Y = 2,
            Width = Dim.Fill(2),
            Height = 3
        };
        skillDetailFrame.Add(_skillDescLabel);

        _skillLevelLabel = new Label("Level: --")
        {
            X = 1,
            Y = 5,
            Width = Dim.Fill(2)
        };
        skillDetailFrame.Add(_skillLevelLabel);

        _skillXPBar = new ProgressBar
        {
            X = 1,
            Y = 6,
            Width = Dim.Fill(2),
            Height = 1,
            Fraction = 0.0f,
            ProgressBarFormat = ProgressBarFormat.Simple,
            ProgressBarStyle = ProgressBarStyle.MarqueeBlocks
        };
        skillDetailFrame.Add(_skillXPBar);

        _skillXPLabel = new Label("XP: --/--")
        {
            X = 1,
            Y = 7,
            Width = Dim.Fill(2)
        };
        skillDetailFrame.Add(_skillXPLabel);

        _skillNextPerkLabel = new Label("Next Perk: --")
        {
            X = 1,
            Y = 8,
            Width = Dim.Fill(2),
            Height = 2
        };
        skillDetailFrame.Add(_skillNextPerkLabel);

        view.Add(skillDetailFrame);

        _skillsTab = new TabView.Tab("Skills", view);
    }

    // ──────────────────────────────────────────────
    //  REPUTATION TAB
    // ──────────────────────────────────────────────

    private void BuildReputationTab()
    {
        var view = new View
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };

        // Faction list (left panel)
        var factionListFrame = new FrameView("Factions")
        {
            X = 1,
            Y = 1,
            Width = 35,
            Height = Dim.Fill(2)
        };

        _factionList = new ListView
        {
            X = 1,
            Y = 1,
            Width = Dim.Fill(2),
            Height = Dim.Fill(2),
            AllowsMarking = false
        };
        _factionList.SelectedItemChanged += OnFactionSelected;
        factionListFrame.Add(_factionList);

        view.Add(factionListFrame);

        // Faction detail (right panel)
        var factionDetailFrame = new FrameView("Faction Details")
        {
            X = 37,
            Y = 1,
            Width = Dim.Fill(38),
            Height = Dim.Fill(2)
        };

        _factionNameLabel = new Label("Select a faction")
        {
            X = 1,
            Y = 1,
            Width = Dim.Fill(2)
        };
        factionDetailFrame.Add(_factionNameLabel);

        _factionAlignLabel = new Label("Alignment: --")
        {
            X = 1,
            Y = 2,
            Width = Dim.Fill(2)
        };
        factionDetailFrame.Add(_factionAlignLabel);

        _factionRepLabel = new Label("Reputation: 0")
        {
            X = 1,
            Y = 3,
            Width = Dim.Fill(2)
        };
        factionDetailFrame.Add(_factionRepLabel);

        _factionTierLabel = new Label("Tier: Neutral")
        {
            X = 1,
            Y = 4,
            Width = Dim.Fill(2)
        };
        factionDetailFrame.Add(_factionTierLabel);

        _factionRepBar = new ProgressBar
        {
            X = 1,
            Y = 5,
            Width = Dim.Fill(2),
            Height = 1,
            Fraction = 0.5f,
            ProgressBarFormat = ProgressBarFormat.Simple,
            ProgressBarStyle = ProgressBarStyle.MarqueeBlocks
        };
        factionDetailFrame.Add(_factionRepBar);

        _factionPriceModLabel = new Label("Price Modifier: 0%")
        {
            X = 1,
            Y = 6,
            Width = Dim.Fill(2)
        };
        factionDetailFrame.Add(_factionPriceModLabel);

        _factionMissionModLabel = new Label("Mission Reward Mod: 1.0x")
        {
            X = 1,
            Y = 7,
            Width = Dim.Fill(2)
        };
        factionDetailFrame.Add(_factionMissionModLabel);

        view.Add(factionDetailFrame);

        _reputationTab = new TabView.Tab("Reputation", view);
    }

    // ──────────────────────────────────────────────
    //  SHIP TAB
    // ──────────────────────────────────────────────

    private void BuildShipTab()
    {
        var view = new View
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };

        // Ship overview (top)
        var overviewFrame = new FrameView("Ship Overview")
        {
            X = 1,
            Y = 1,
            Width = Dim.Fill(2),
            Height = 12
        };

        _shipNameLabel = new Label("Ship: ")
        {
            X = 1,
            Y = 1,
            Width = Dim.Fill(2)
        };
        overviewFrame.Add(_shipNameLabel);

        _shipClassLabel = new Label("Class: ")
        {
            X = 1,
            Y = 2,
            Width = Dim.Fill(2)
        };
        overviewFrame.Add(_shipClassLabel);

        // Hull
        var hullTitle = new Label("Hull")
        {
            X = 1,
            Y = 3,
            Width = 8
        };
        overviewFrame.Add(hullTitle);

        _hullBar = new ProgressBar
        {
            X = 10,
            Y = 3,
            Width = Dim.Fill(12),
            Height = 1,
            Fraction = 1.0f,
            ProgressBarFormat = ProgressBarFormat.Simple,
            ProgressBarStyle = ProgressBarStyle.MarqueeBlocks
        };
        overviewFrame.Add(_hullBar);

        _hullLabel = new Label("100/100")
        {
            X = Pos.Right(_hullBar) + 1,
            Y = 3,
            Width = 12
        };
        overviewFrame.Add(_hullLabel);

        // Shields
        var shieldTitle = new Label("Shields")
        {
            X = 1,
            Y = 4,
            Width = 8
        };
        overviewFrame.Add(shieldTitle);

        _shieldBar = new ProgressBar
        {
            X = 10,
            Y = 4,
            Width = Dim.Fill(12),
            Height = 1,
            Fraction = 1.0f,
            ProgressBarFormat = ProgressBarFormat.Simple,
            ProgressBarStyle = ProgressBarStyle.MarqueeBlocks
        };
        overviewFrame.Add(_shieldBar);

        _shieldLabel = new Label("100/100")
        {
            X = Pos.Right(_shieldBar) + 1,
            Y = 4,
            Width = 12
        };
        overviewFrame.Add(_shieldLabel);

        // Fuel
        var fuelTitle = new Label("Fuel")
        {
            X = 1,
            Y = 5,
            Width = 8
        };
        overviewFrame.Add(fuelTitle);

        _fuelBar = new ProgressBar
        {
            X = 10,
            Y = 5,
            Width = Dim.Fill(12),
            Height = 1,
            Fraction = 1.0f,
            ProgressBarFormat = ProgressBarFormat.Simple,
            ProgressBarStyle = ProgressBarStyle.MarqueeBlocks
        };
        overviewFrame.Add(_fuelBar);

        _fuelLabel = new Label("100/100")
        {
            X = Pos.Right(_fuelBar) + 1,
            Y = 5,
            Width = 12
        };
        overviewFrame.Add(_fuelLabel);

        _cargoLabel = new Label("Cargo: 0/50")
        {
            X = 1,
            Y = 6,
            Width = Dim.Fill(2)
        };
        overviewFrame.Add(_cargoLabel);

        _speedLabel = new Label("Max Speed: --")
        {
            X = 1,
            Y = 7,
            Width = Dim.Fill(2)
        };
        overviewFrame.Add(_speedLabel);

        _turnRateLabel = new Label("Turn Rate: --")
        {
            X = 1,
            Y = 8,
            Width = Dim.Fill(2)
        };
        overviewFrame.Add(_turnRateLabel);

        _hardpointsLabel = new Label("Hardpoints: --")
        {
            X = 1,
            Y = 9,
            Width = Dim.Fill(2)
        };
        overviewFrame.Add(_hardpointsLabel);

        _upgradeSlotsLabel = new Label("Upgrade Slots: --")
        {
            X = 1,
            Y = 10,
            Width = Dim.Fill(2)
        };
        overviewFrame.Add(_upgradeSlotsLabel);

        _conditionLabel = new Label("Condition: 100%")
        {
            X = 1,
            Y = 11,
            Width = Dim.Fill(2)
        };
        overviewFrame.Add(_conditionLabel);

        view.Add(overviewFrame);

        // Installed upgrades (bottom)
        var upgradesFrame = new FrameView("Installed Upgrades")
        {
            X = 1,
            Y = 14,
            Width = Dim.Fill(2),
            Height = Dim.Fill(15)
        };

        _upgradesList = new ListView
        {
            X = 1,
            Y = 1,
            Width = Dim.Fill(2),
            Height = Dim.Fill(2),
            AllowsMarking = false
        };
        upgradesFrame.Add(_upgradesList);

        view.Add(upgradesFrame);

        _shipTab = new TabView.Tab("Ship", view);
    }

    // ──────────────────────────────────────────────
    //  EQUIPMENT TAB
    // ──────────────────────────────────────────────

    private void BuildEquipmentTab()
    {
        var view = new View
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };

        // Equipment list (left panel)
        var eqListFrame = new FrameView("Installed Equipment")
        {
            X = 1,
            Y = 1,
            Width = 35,
            Height = Dim.Fill(2)
        };

        _equipmentList = new ListView
        {
            X = 1,
            Y = 1,
            Width = Dim.Fill(2),
            Height = Dim.Fill(2),
            AllowsMarking = false
        };
        _equipmentList.SelectedItemChanged += OnEquipmentSelected;
        eqListFrame.Add(_equipmentList);

        view.Add(eqListFrame);

        // Equipment detail (right panel)
        var eqDetailFrame = new FrameView("Equipment Details")
        {
            X = 37,
            Y = 1,
            Width = Dim.Fill(38),
            Height = Dim.Fill(2)
        };

        _eqNameLabel = new Label("Select equipment")
        {
            X = 1,
            Y = 1,
            Width = Dim.Fill(2)
        };
        eqDetailFrame.Add(_eqNameLabel);

        _eqTypeLabel = new Label("Type: --")
        {
            X = 1,
            Y = 2,
            Width = Dim.Fill(2)
        };
        eqDetailFrame.Add(_eqTypeLabel);

        _eqSizeLabel = new Label("Size: --")
        {
            X = 1,
            Y = 3,
            Width = Dim.Fill(2)
        };
        eqDetailFrame.Add(_eqSizeLabel);

        _eqMountLabel = new Label("Mount: --")
        {
            X = 1,
            Y = 4,
            Width = Dim.Fill(2)
        };
        eqDetailFrame.Add(_eqMountLabel);

        _eqRarityLabel = new Label("Rarity: --")
        {
            X = 1,
            Y = 5,
            Width = Dim.Fill(2)
        };
        eqDetailFrame.Add(_eqRarityLabel);

        _eqManufacturerLabel = new Label("Manufacturer: --")
        {
            X = 1,
            Y = 6,
            Width = Dim.Fill(2)
        };
        eqDetailFrame.Add(_eqManufacturerLabel);

        _eqSlotLabel = new Label("Slot: --")
        {
            X = 1,
            Y = 7,
            Width = Dim.Fill(2)
        };
        eqDetailFrame.Add(_eqSlotLabel);

        _eqDescLabel = new Label("")
        {
            X = 1,
            Y = 8,
            Width = Dim.Fill(2),
            Height = 2
        };
        eqDetailFrame.Add(_eqDescLabel);

        // Stats sub-frame
        _eqStatsFrame = new FrameView("Stats")
        {
            X = 1,
            Y = 10,
            Width = Dim.Fill(2),
            Height = 6
        };

        _eqMassLabel = new Label("Mass: --")
        {
            X = 1,
            Y = 1,
            Width = Dim.Fill(2)
        };
        _eqStatsFrame.Add(_eqMassLabel);

        _eqPowerLabel = new Label("Power Draw: --")
        {
            X = 1,
            Y = 2,
            Width = Dim.Fill(2)
        };
        _eqStatsFrame.Add(_eqPowerLabel);

        _eqHeatLabel = new Label("Heat: --")
        {
            X = 1,
            Y = 3,
            Width = Dim.Fill(2)
        };
        _eqStatsFrame.Add(_eqHeatLabel);

        var condTitle = new Label("Condition")
        {
            X = 1,
            Y = 4,
            Width = 10
        };
        _eqStatsFrame.Add(condTitle);

        _eqConditionBar = new ProgressBar
        {
            X = 12,
            Y = 4,
            Width = Dim.Fill(14),
            Height = 1,
            Fraction = 1.0f,
            ProgressBarFormat = ProgressBarFormat.Simple,
            ProgressBarStyle = ProgressBarStyle.MarqueeBlocks
        };
        _eqStatsFrame.Add(_eqConditionBar);

        _eqConditionLabel = new Label("100%")
        {
            X = Pos.Right(_eqConditionBar) + 1,
            Y = 4,
            Width = 8
        };
        _eqStatsFrame.Add(_eqConditionLabel);

        eqDetailFrame.Add(_eqStatsFrame);

        view.Add(eqDetailFrame);

        _equipmentTab = new TabView.Tab("Equipment", view);
    }

    // ──────────────────────────────────────────────
    //  SAVE/LOAD TAB
    // ──────────────────────────────────────────────

    private void BuildSaveLoadTab()
    {
        var view = new View
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };

        // Save slots list (left)
        var slotListFrame = new FrameView("Save Slots")
        {
            X = 1,
            Y = 1,
            Width = 40,
            Height = Dim.Fill(2)
        };

        _saveSlotList = new ListView
        {
            X = 1,
            Y = 1,
            Width = Dim.Fill(2),
            Height = Dim.Fill(2),
            AllowsMarking = false
        };
        _saveSlotList.SelectedItemChanged += OnSaveSlotSelected;
        slotListFrame.Add(_saveSlotList);

        view.Add(slotListFrame);

        // Save detail / actions (right)
        var actionFrame = new FrameView("Actions")
        {
            X = 42,
            Y = 1,
            Width = Dim.Fill(43),
            Height = Dim.Fill(2)
        };

        _saveDetailLabel = new Label("Select a save slot")
        {
            X = 1,
            Y = 1,
            Width = Dim.Fill(2),
            Height = 4
        };
        actionFrame.Add(_saveDetailLabel);

        var nameLabel = new Label("Save Name:")
        {
            X = 1,
            Y = 5,
            Width = 12
        };
        actionFrame.Add(nameLabel);

        _saveNameField = new TextField("")
        {
            X = 14,
            Y = 5,
            Width = Dim.Fill(15)
        };
        actionFrame.Add(_saveNameField);

        _saveBtn = new Button("Save Game")
        {
            X = 1,
            Y = 7,
            Width = 18
        };
        _saveBtn.Clicked += OnSaveGame;
        actionFrame.Add(_saveBtn);

        _loadBtn = new Button("Load Game")
        {
            X = 20,
            Y = 7,
            Width = 18
        };
        _loadBtn.Clicked += OnLoadGame;
        actionFrame.Add(_loadBtn);

        _deleteBtn = new Button("Delete Save")
        {
            X = 39,
            Y = 7,
            Width = 18
        };
        _deleteBtn.Clicked += OnDeleteSave;
        actionFrame.Add(_deleteBtn);

        _saveStatusLabel = new Label("")
        {
            X = 1,
            Y = 9,
            Width = Dim.Fill(2),
            Height = 2
        };
        actionFrame.Add(_saveStatusLabel);

        view.Add(actionFrame);

        _saveLoadTab = new TabView.Tab("Save/Load", view);
    }

    // ──────────────────────────────────────────────
    //  REFRESH — Update all UI from player state
    // ──────────────────────────────────────────────

    /// <summary>
    /// Refreshes all tab displays from the current player state.
    /// </summary>
    public void Refresh()
    {
        RefreshCharacterTab();
        RefreshSkillsTab();
        RefreshReputationTab();
        RefreshShipTab();
        RefreshEquipmentTab();
        RefreshSaveLoadTab();
    }

    private void RefreshCharacterTab()
    {
        _nameLabel.Text = $"Name: {_player.Name}";
        _backgroundLabel.Text = $"Background: {_player.Background}";
        _creditsLabel.Text = $"Credits: {_player.Credits:N0} cr";
        _locationLabel.Text = $"Location: {_player.CurrentLocationId}";
        _playTimeLabel.Text = $"Play Time: {FormatPlayTime(_player.TotalPlayTime)}";

        // Health bar
        float healthFrac = _player.MaxHealth > 0 ? (float)_player.Health / _player.MaxHealth : 0f;
        _healthBar.Fraction = healthFrac;
        _healthBar.Text = $"{_player.Health}/{_player.MaxHealth}";

        // Level
        _levelLabel.Text = $"{_player.Level}";

        // XP bar
        var xpForNext = Player.GetXPForLevel(_player.Level + 1);
        var xpForCurrent = Player.GetTotalXPForLevel(_player.Level);
        var xpIntoLevel = _player.Experience - xpForCurrent;
        float xpFrac = xpForNext > 0 ? (float)xpIntoLevel / xpForNext : 1f;
        _xpBar.Fraction = Math.Clamp(xpFrac, 0f, 1f);
        _xpBar.Text = $"{xpIntoLevel}/{xpForNext} XP";

        _skillPointsLabel.Text = $"Skill Points: {_player.SkillPoints}";
        _difficultyLabel.Text = $"Difficulty: {_player.Difficulty}";
        _ironmanLabel.Text = $"Ironman: {(_player.IronmanMode ? "ON" : "OFF")}";

        // Statistics
        var stats = _player.Statistics;
        var statsFrame = FindFrameView(_characterTab.View, "Statistics");
        if (statsFrame != null && statsFrame.Subviews.Count > 0 && statsFrame.Subviews[0] is Label statsLabel)
        {
            statsLabel.Text =
                $"Trades: {stats.TradesCompleted}  |  Profit: {stats.TotalTradeProfit:N0} cr\n" +
                $"Missions: {stats.MissionsCompleted} done / {stats.MissionsFailed} failed\n" +
                $"Enemies Destroyed: {stats.EnemiesDestroyed}  |  Ships Lost: {stats.ShipsLost}\n" +
                $"Distance: {stats.DistanceTraveled:F1} LY  |  Jumps: {stats.JumpsMade}\n" +
                $"Deaths: {stats.Deaths}  |  Locations: {stats.LocationsDiscovered}";
        }
    }

    private void RefreshSkillsTab()
    {
        // Load skill definitions
        _skillDefinitions = SkillRegistry.All
            .OrderBy(s => s.Category)
            .ThenBy(s => s.DisplayOrder)
            .ToList();

        var displayItems = new List<string>();
        foreach (var def in _skillDefinitions)
        {
            var instance = _player.Skills.GetSkill(def.Id);
            var level = instance?.Level ?? 0;
            var category = def.Category.ToString();
            displayItems.Add($"[{category}] {def.Name} (Lv.{level})");
        }

        _skillsList.SetSource(displayItems);
        _skillUnspentLabel.Text = $"Unspent Points: {_player.Skills.UnspentSkillPoints}";

        // Refresh selected skill detail
        if (_selectedSkillIndex >= 0 && _selectedSkillIndex < _skillDefinitions.Count)
        {
            ShowSkillDetail(_selectedSkillIndex);
        }
    }

    private void ShowSkillDetail(int index)
    {
        if (index < 0 || index >= _skillDefinitions.Count) return;

        var def = _skillDefinitions[index];
        var instance = _player.Skills.GetSkill(def.Id);
        var level = instance?.Level ?? 0;
        var currentXP = instance?.CurrentXP ?? 0;

        _skillNameLabel.Text = $"{def.Name} [{def.Category}]";
        _skillDescLabel.Text = def.Description;
        _skillLevelLabel.Text = $"Level: {level}/{def.MaxLevel}";

        // XP progress
        if (level < def.MaxLevel)
        {
            var xpForNext = Skills.GetXPForLevel(level + 1, def);
            float frac = xpForNext > 0 ? (float)currentXP / xpForNext : 0f;
            _skillXPBar.Fraction = Math.Clamp(frac, 0f, 1f);
            _skillXPLabel.Text = $"XP: {currentXP}/{xpForNext}";
        }
        else
        {
            _skillXPBar.Fraction = 1f;
            _skillXPLabel.Text = "MAX LEVEL";
        }

        // Next perk
        var nextPerk = def.GetNextPerk(level);
        if (nextPerk != null)
        {
            _skillNextPerkLabel.Text = $"Next Perk (Lv.{nextPerk.RequiredLevel}):\n{nextPerk.Name} - {nextPerk.Description}";
        }
        else
        {
            _skillNextPerkLabel.Text = "All perks unlocked!";
        }
    }

    private void RefreshReputationTab()
    {
        _factions = FactionRegistry.All
            .OrderBy(f => f.Name)
            .ToList();

        var displayItems = new List<string>();
        foreach (var faction in _factions)
        {
            var rep = _player.Reputation.GetReputation(faction.Id);
            var tier = Reputation.GetTierName(rep);
            displayItems.Add($"{faction.Name}  [{tier}]  ({rep})");
        }

        _factionList.SetSource(displayItems);

        if (_selectedFactionIndex >= 0 && _selectedFactionIndex < _factions.Count)
        {
            ShowFactionDetail(_selectedFactionIndex);
        }
    }

    private void ShowFactionDetail(int index)
    {
        if (index < 0 || index >= _factions.Count) return;

        var faction = _factions[index];
        var rep = _player.Reputation.GetReputation(faction.Id);
        var tier = Reputation.GetTierName(rep);
        var priceMod = _player.Reputation.GetPriceModifier(faction.Id);
        var missionMod = _player.Reputation.GetMissionRewardModifier(faction.Id);

        _factionNameLabel.Text = faction.Name;
        _factionAlignLabel.Text = $"Alignment: {faction.Alignment}";
        _factionRepLabel.Text = $"Reputation: {rep}";
        _factionTierLabel.Text = $"Tier: {tier}";

        // Rep bar: -100 to 100 mapped to 0.0 to 1.0
        float repFrac = (rep + 100f) / 200f;
        _factionRepBar.Fraction = Math.Clamp(repFrac, 0f, 1f);

        _factionPriceModLabel.Text = $"Price Modifier: {priceMod:P1}";
        _factionMissionModLabel.Text = $"Mission Reward Mod: {missionMod:F1}x";
    }

    private void RefreshShipTab()
    {
        _shipNameLabel.Text = $"Ship: {_player.ShipName}";
        _shipClassLabel.Text = $"Class: {_player.ShipId}";

        // Hull
        float hullFrac = _player.ShipMaxHull > 0 ? (float)_player.ShipHull / _player.ShipMaxHull : 0f;
        _hullBar.Fraction = hullFrac;
        _hullLabel.Text = $"{_player.ShipHull}/{_player.ShipMaxHull}";

        // Shields
        float shieldFrac = _player.ShipMaxShields > 0 ? (float)_player.ShipShields / _player.ShipMaxShields : 0f;
        _shieldBar.Fraction = shieldFrac;
        _shieldLabel.Text = $"{_player.ShipShields}/{_player.ShipMaxShields}";

        // Fuel
        float fuelFrac = _player.MaxFuel > 0 ? (float)_player.CurrentFuel / _player.MaxFuel : 0f;
        _fuelBar.Fraction = fuelFrac;
        _fuelLabel.Text = $"{_player.CurrentFuel}/{_player.MaxFuel}";

        // Cargo
        var cargoUsed = _player.GetTotalCargoUsed();
        _cargoLabel.Text = $"Cargo: {cargoUsed}/{_player.CargoCapacity}";

        // Try to get ship details from registry
        var ship = ShipRegistry.Get(_player.ShipId);
        if (ship != null)
        {
            _speedLabel.Text = $"Max Speed: {ship.MaxSpeed:F0} u/s";
            _turnRateLabel.Text = $"Turn Rate: {ship.TurnRate:F0} deg/s";
            _hardpointsLabel.Text = $"Hardpoints: {ship.WeaponHardpoints} weapon / {ship.UtilityHardpoints} utility";
            _upgradeSlotsLabel.Text = $"Upgrade Slots: {ship.UpgradeSlots}";
            _conditionLabel.Text = $"Condition: {ship.Condition * 100:F0}%";
        }
        else
        {
            _speedLabel.Text = "Max Speed: --";
            _turnRateLabel.Text = "Turn Rate: --";
            _hardpointsLabel.Text = "Hardpoints: --";
            _upgradeSlotsLabel.Text = "Upgrade Slots: --";
            _conditionLabel.Text = "Condition: --";
        }

        // Installed upgrades list
        _upgradeDisplayItems.Clear();
        foreach (var upgradeId in _player.InstalledUpgrades)
        {
            _upgradeDisplayItems.Add($"[Upgrade] {upgradeId}");
        }
        foreach (var kvp in _player.InstalledEquipment)
        {
            var eq = EquipmentRegistry.Get(kvp.Value);
            var eqName = eq?.Name ?? kvp.Value;
            _upgradeDisplayItems.Add($"[{kvp.Key}] {eqName}");
        }

        if (_upgradeDisplayItems.Count == 0)
        {
            _upgradeDisplayItems.Add("(No upgrades installed)");
        }

        _upgradesList.SetSource(_upgradeDisplayItems);
    }

    private void RefreshEquipmentTab()
    {
        _equipmentDisplayItems.Clear();
        var equipmentDetails = new List<(string slotId, string eqId, Equipment? eq)>();

        foreach (var kvp in _player.InstalledEquipment)
        {
            var eq = EquipmentRegistry.Get(kvp.Value);
            var eqName = eq?.Name ?? kvp.Value;
            var eqType = eq?.Type.ToString() ?? "Unknown";
            _equipmentDisplayItems.Add($"[{kvp.Key}] {eqName} ({eqType})");
            equipmentDetails.Add((kvp.Key, kvp.Value, eq));
        }

        if (_equipmentDisplayItems.Count == 0)
        {
            _equipmentDisplayItems.Add("(No equipment installed)");
        }

        _equipmentList.SetSource(_equipmentDisplayItems);

        if (_selectedEquipmentIndex >= 0 && _selectedEquipmentIndex < equipmentDetails.Count)
        {
            ShowEquipmentDetail(equipmentDetails[_selectedEquipmentIndex]);
        }
    }

    private void ShowEquipmentDetail((string slotId, string eqId, Equipment? eq) detail)
    {
        var (slotId, eqId, eq) = detail;

        if (eq != null)
        {
            _eqNameLabel.Text = eq.Name;
            _eqTypeLabel.Text = $"Type: {eq.Type}";
            _eqSizeLabel.Text = $"Size: {eq.Size}";
            _eqMountLabel.Text = $"Mount: {eq.MountType}";
            _eqRarityLabel.Text = $"Rarity: {eq.Rarity}";
            _eqManufacturerLabel.Text = $"Manufacturer: {eq.Manufacturer}";
            _eqSlotLabel.Text = $"Slot: {slotId}";
            _eqDescLabel.Text = eq.Description;

            _eqMassLabel.Text = $"Mass: {eq.Mass:F1} tons";
            _eqPowerLabel.Text = $"Power Draw: {eq.PowerDraw:F1} MW";
            _eqHeatLabel.Text = $"Heat: {eq.HeatGeneration:F1}/s";

            float condFrac = (float)eq.Condition;
            _eqConditionBar.Fraction = condFrac;
            _eqConditionLabel.Text = $"{eq.Condition * 100:F0}%";
        }
        else
        {
            _eqNameLabel.Text = eqId;
            _eqTypeLabel.Text = "Type: Unknown";
            _eqSizeLabel.Text = "Size: --";
            _eqMountLabel.Text = "Mount: --";
            _eqRarityLabel.Text = "Rarity: --";
            _eqManufacturerLabel.Text = "Manufacturer: --";
            _eqSlotLabel.Text = $"Slot: {slotId}";
            _eqDescLabel.Text = "(Equipment definition not found)";
            _eqMassLabel.Text = "Mass: --";
            _eqPowerLabel.Text = "Power Draw: --";
            _eqHeatLabel.Text = "Heat: --";
            _eqConditionBar.Fraction = 0f;
            _eqConditionLabel.Text = "--";
        }
    }

    private void RefreshSaveLoadTab()
    {
        if (_saveSystem == null)
        {
            _saveSlotList.SetSource(new List<string> { "(Save system not available)" });
            _saveDetailLabel.Text = "Save/Load is not available.\nThe SaveSystem has not been initialized.";
            _saveBtn.Enabled = false;
            _loadBtn.Enabled = false;
            _deleteBtn.Enabled = false;
            return;
        }

        _saveSlots = _saveSystem.ListSaves();

        var displayItems = new List<string>();
        foreach (var slot in _saveSlots)
        {
            if (slot.IsCorrupted)
            {
                displayItems.Add($"[CORRUPT] {slot.SaveName}");
            }
            else
            {
                displayItems.Add($"{slot.SaveName} | {slot.PlayerName} | {slot.PlayTime:hh\\:mm} | {slot.UpdatedAt:yyyy-MM-dd HH:mm}");
            }
        }

        if (displayItems.Count == 0)
        {
            displayItems.Add("(No save files found)");
        }

        _saveSlotList.SetSource(displayItems);
        _saveBtn.Enabled = true;
        _loadBtn.Enabled = true;
        _deleteBtn.Enabled = true;

        if (_selectedSaveSlotIndex >= 0 && _selectedSaveSlotIndex < _saveSlots.Count)
        {
            ShowSaveSlotDetail(_selectedSaveSlotIndex);
        }
    }

    private void ShowSaveSlotDetail(int index)
    {
        if (index < 0 || index >= _saveSlots.Count) return;

        var slot = _saveSlots[index];
        if (slot.IsCorrupted)
        {
            _saveDetailLabel.Text = $"Slot: {slot.SaveName}\n[CORRUPTED]\n{slot.ErrorMessage}";
        }
        else
        {
            _saveDetailLabel.Text =
                $"Player: {slot.PlayerName}\n" +
                $"Location: {slot.CurrentLocation}\n" +
                $"Credits: {slot.Credits:N0} cr\n" +
                $"Play Time: {slot.PlayTime:hh\\:mm}\n" +
                $"Saved: {slot.UpdatedAt:yyyy-MM-dd HH:mm}\n" +
                $"Version: {slot.GameVersion} (format v{slot.SaveFormatVersion})";
        }

        _saveNameField.Text = slot.SaveName;
    }

    // ──────────────────────────────────────────────
    //  EVENT HANDLERS
    // ──────────────────────────────────────────────

    private void OnSkillSelected(ListViewItemEventArgs args)
    {
        _selectedSkillIndex = args.Item;
        ShowSkillDetail(_selectedSkillIndex);
    }

    private void OnSpendSkillPoint()
    {
        if (_selectedSkillIndex < 0 || _selectedSkillIndex >= _skillDefinitions.Count) return;

        var def = _skillDefinitions[_selectedSkillIndex];
        var success = _player.Skills.SpendSkillPoint(def.Id);

        if (success)
        {
            RefreshSkillsTab();
        }
    }

    private void OnFactionSelected(ListViewItemEventArgs args)
    {
        _selectedFactionIndex = args.Item;
        ShowFactionDetail(_selectedFactionIndex);
    }

    private void OnEquipmentSelected(ListViewItemEventArgs args)
    {
        _selectedEquipmentIndex = args.Item;

        var equipmentDetails = new List<(string slotId, string eqId, Equipment? eq)>();
        foreach (var kvp in _player.InstalledEquipment)
        {
            var eq = EquipmentRegistry.Get(kvp.Value);
            equipmentDetails.Add((kvp.Key, kvp.Value, eq));
        }

        if (_selectedEquipmentIndex >= 0 && _selectedEquipmentIndex < equipmentDetails.Count)
        {
            ShowEquipmentDetail(equipmentDetails[_selectedEquipmentIndex]);
        }
    }

    private void OnSaveSlotSelected(ListViewItemEventArgs args)
    {
        _selectedSaveSlotIndex = args.Item;
        ShowSaveSlotDetail(_selectedSaveSlotIndex);
    }

    private void OnSaveGame()
    {
        if (_saveSystem == null)
        {
            _saveStatusLabel.Text = "Error: Save system not available.";
            return;
        }

        var saveName = _saveNameField.Text?.ToString()?.Trim();
        if (string.IsNullOrEmpty(saveName))
        {
            saveName = $"save_{DateTime.UtcNow:yyyyMMdd_HHmmss}";
            _saveNameField.Text = saveName;
        }

        try
        {
            // Fire-and-forget save (async)
            _ = Task.Run(async () =>
            {
                try
                {
                    await _saveSystem.SaveGameAsync(saveName);
                    Application.MainLoop.Invoke(() =>
                    {
                        _saveStatusLabel.Text = $"Saved successfully to '{saveName}'.";
                        RefreshSaveLoadTab();
                    });
                }
                catch (Exception ex)
                {
                    Application.MainLoop.Invoke(() =>
                    {
                        _saveStatusLabel.Text = $"Save failed: {ex.Message}";
                    });
                }
            });
        }
        catch (Exception ex)
        {
            _saveStatusLabel.Text = $"Save error: {ex.Message}";
        }
    }

    private void OnLoadGame()
    {
        if (_saveSystem == null)
        {
            _saveStatusLabel.Text = "Error: Save system not available.";
            return;
        }

        var saveName = _saveNameField.Text?.ToString()?.Trim();
        if (string.IsNullOrEmpty(saveName))
        {
            _saveStatusLabel.Text = "Please select a save slot or enter a save name.";
            return;
        }

        if (!_saveSystem.SaveExists(saveName))
        {
            _saveStatusLabel.Text = $"Save '{saveName}' not found.";
            return;
        }

        try
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await _saveSystem.LoadGameAsync(saveName);
                    Application.MainLoop.Invoke(() =>
                    {
                        _saveStatusLabel.Text = $"Loaded successfully from '{saveName}'.";
                        Refresh();
                    });
                }
                catch (Exception ex)
                {
                    Application.MainLoop.Invoke(() =>
                    {
                        _saveStatusLabel.Text = $"Load failed: {ex.Message}";
                    });
                }
            });
        }
        catch (Exception ex)
        {
            _saveStatusLabel.Text = $"Load error: {ex.Message}";
        }
    }

    private void OnDeleteSave()
    {
        if (_saveSystem == null)
        {
            _saveStatusLabel.Text = "Error: Save system not available.";
            return;
        }

        var saveName = _saveNameField.Text?.ToString()?.Trim();
        if (string.IsNullOrEmpty(saveName))
        {
            _saveStatusLabel.Text = "Please select a save slot or enter a save name.";
            return;
        }

        if (!_saveSystem.SaveExists(saveName))
        {
            _saveStatusLabel.Text = $"Save '{saveName}' not found.";
            return;
        }

        try
        {
            var deleted = _saveSystem.DeleteSave(saveName);
            if (deleted)
            {
                _saveStatusLabel.Text = $"Deleted save '{saveName}'.";
                _saveNameField.Text = "";
                _selectedSaveSlotIndex = -1;
                RefreshSaveLoadTab();
            }
            else
            {
                _saveStatusLabel.Text = $"Failed to delete '{saveName}'.";
            }
        }
        catch (Exception ex)
        {
            _saveStatusLabel.Text = $"Delete error: {ex.Message}";
        }
    }

    // ──────────────────────────────────────────────
    //  RESIZE
    // ──────────────────────────────────────────────

    /// <summary>
    /// Called when the terminal size changes.
    /// </summary>
    public void OnResize(int width, int height)
    {
        // Terminal.Gui handles layout automatically via Dim.Fill().
        // We just need to ensure the window fills the new size.
        _window.Width = Dim.Fill();
        _window.Height = Dim.Fill();
    }

    // ──────────────────────────────────────────────
    //  HELPERS
    // ──────────────────────────────────────────────

    /// <summary>
    /// Finds a FrameView by title within a parent view.
    /// </summary>
    private static FrameView? FindFrameView(View parent, string title)
    {
        foreach (var subview in parent.Subviews)
        {
            if (subview is FrameView fv && fv.Title == title)
                return fv;
        }
        return null;
    }

    /// <summary>
    /// Formats a TimeSpan for display.
    /// </summary>
    private static string FormatPlayTime(TimeSpan time)
    {
        if (time.TotalDays >= 1)
            return $"{(int)time.TotalDays}d {time.Hours}h {time.Minutes}m";
        if (time.TotalHours >= 1)
            return $"{(int)time.TotalHours}h {time.Minutes}m";
        return $"{time.Minutes}m {time.Seconds}s";
    }
}
