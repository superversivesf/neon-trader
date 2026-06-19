# PORTING_AND_ACCESSIBILITY.md

## Platform-Agnostic Porting & Accessibility Design Document

**Version:** 1.0.0
**Status:** Draft
**Last Updated:** 2025-06-16

---

## 1. Platform Constraints Matrix

| Platform | Rendering | Input | Storage | Audio | Performance Budget | Distribution |
|----------|-----------|-------|---------|-------|-------------------|--------------|
| **Terminal (Linux/macOS/WSL)** | ANSI/UTF-8, 256-color, truecolor | Keyboard (raw), mouse (SGR) | POSIX filesystem | None (beep) | 60 FPS @ 80×24, <16ms/frame | Direct, Homebrew, Scoop, AUR |
| **Terminal (Windows)** | ConPTY, VT sequences | Keyboard, mouse | Win32 API | None | 60 FPS @ 80×24 | WinGet, Scoop, Chocolatey |
| **Desktop GUI (Linux/macOS/Windows)** | GPU (Vulkan/Metal/DX12), CPU fallback | Keyboard, mouse, gamepad | Platform app dirs | Optional (OpenAL) | 60 FPS @ 1080p, <16ms/frame | Steam, itch.io, Epic, Flatpak, Snap |
| **Mobile (iOS/Android)** | GPU (Metal/Vulkan), CPU fallback | Touch, virtual keyboard, gamepad | App sandbox | OpenAL/SL | 30-60 FPS @ native, <33ms/frame | App Store, Play Store, TestFlight |
| **Web (WASM)** | Canvas2D/WebGL2, DOM fallback | Keyboard, mouse, touch, gamepad | IndexedDB, localStorage | Web Audio API | 60 FPS @ viewport, <16ms/frame | itch.io, GitHub Pages, self-hosted |
| **Console (Switch/PS5/Xbox)** | GPU (NVN/GNM/DX12) | Gamepad (primary), keyboard (secondary) | Platform save data | Platform audio | 30-60 FPS @ 1080p/4K, <16-33ms/frame | eShop, PS Store, Xbox Store |

### Key Constraints Per Platform

| Constraint | Terminal | Desktop GUI | Mobile | Web | Console |
|------------|----------|-------------|--------|-----|---------|
| **Min Resolution** | 80×24 chars | 1024×768 | 320×568 | 320×568 | 1280×720 |
| **Color Depth** | 16/256/truecolor | 32-bit | 32-bit | 32-bit | 32-bit + HDR |
| **Input Latency** | <5ms | <8ms | <16ms | <16ms | <8ms |
| **Memory Limit** | Unlimited | 512MB-2GB | 100-300MB | 200-500MB | 1-4GB |
| **Background Exec** | Yes | Yes | Limited (30s) | No (service worker) | Suspended |
| **File System** | Full | Sandboxed | Sandboxed | Virtual (OPFS) | Sandboxed |
| **Networking** | Full | Full | Full | WebSocket/WebRTC | Platform SDK |
| **Accessibility API** | Limited (screen reader via speak) | Full (AT-SPI, UIA, NSAccessibility) | Full (VoiceOver, TalkBack) | Full (ARIA) | Platform SDK |

---

## 2. Input Abstraction Layer

### 2.1 Action-Based Input Model

```typescript
// Platform-agnostic action definitions
enum GameAction {
  // Navigation
  MENU_UP, MENU_DOWN, MENU_LEFT, MENU_RIGHT,
  MENU_NEXT_TAB, MENU_PREV_TAB,
  MENU_SELECT, MENU_BACK, MENU_CONTEXT,

  // Gameplay
  BUY_PAYLOAD, SELL_PAYLOAD,
  TRAVEL_TO_SECTOR,
  USE_DECK_CARD,
  ACTIVATE_SOFTWARE,

  // Meta
  PAUSE, QUICK_SAVE, QUICK_LOAD,
  TOGGLE_FULLSCREEN, TOGGLE_ACCESSIBILITY,
  SHOW_HELP
}

// Input mapping per platform
interface InputMapping {
  [GameAction]: PlatformInput[];
}

type PlatformInput =
  | { type: 'keyboard'; key: string; modifiers?: string[] }
  | { type: 'gamepad'; button: number; index?: number }
  | { type: 'touch'; gesture: 'tap' | 'swipe' | 'longpress'; zone?: string }
  | { type: 'mouse'; button: number; action: 'click' | 'drag' | 'wheel' };
```

### 2.2 Default Keyboard Mapping (Terminal/Desktop)

| Action | Primary | Secondary | Terminal Notes |
|--------|---------|-----------|----------------|
| MENU_UP | `k` / `Up` | `Ctrl+p` | Vim + arrow |
| MENU_DOWN | `j` / `Down` | `Ctrl+n` | Vim + arrow |
| MENU_LEFT | `h` / `Left` | `Shift+Tab` | Vim + shift-tab |
| MENU_RIGHT | `l` / `Right` | `Tab` | Vim + tab |
| MENU_NEXT_TAB | `]` | `Ctrl+]` | Bracket nav |
| MENU_PREV_TAB | `[` | `Ctrl+[` | Bracket nav |
| MENU_SELECT | `Enter` / `Space` | `l` | Context-aware |
| MENU_BACK | `Esc` / `q` / `Backspace` | `h` | Esc = universal back |
| MENU_CONTEXT | `x` / `RightClick` | `Ctrl+x` | Context menu |

### 2.3 Gamepad Mapping (Standard Layout)

| Action | Xbox/Generic | PlayStation | Switch Pro |
|--------|--------------|-------------|------------|
| MENU_UP | D-Pad Up / L-Stick Up | D-Pad Up / L-Stick Up | D-Pad Up / L-Stick Up |
| MENU_DOWN | D-Pad Down / L-Stick Down | D-Pad Down | D-Pad Down / L-Stick Down | D-Pad Down / L-Stick Down |
| MENU_LEFT | D-Pad Left / L-Stick Left | D-Pad Left / L-Stick Left | D-Pad Left / L-Stick Left |
| MENU_RIGHT | D-Pad Right / L-Stick Right | D-Pad Right / L-Stick Right | D-Pad Right / L-Stick Right |
| MENU_SELECT | A (South) | Cross (South) | A (South) |
| MENU_BACK | B (East) | Circle (East) | B (East) |
| MENU_CONTEXT | Y (North) / Right Stick | Triangle (North) / R3 | X (North) / R3 |
| MENU_NEXT_TAB | RB / R1 | R1 | R |
| MENU_PREV_TAB | LB / L1 | L1 | L |
| PAUSE | Start / Menu | Options | + |

### 2.4 Touch Mapping (Mobile/Web)

| Action | Gesture | Zone |
|--------|---------|------|
| MENU_UP | Swipe up | List area |
| MENU_DOWN | Swipe down | List area |
| MENU_LEFT | Swipe left | Content area |
| MENU_RIGHT | Swipe right | Content area |
| MENU_SELECT | Tap | Item |
| MENU_BACK | Swipe from left edge / Back button | Global |
| MENU_CONTEXT | Long press (500ms) | Item |
| BUY_PAYLOAD | Double tap | Market row |
| TRAVEL_TO_SECTOR | Tap → Confirm dialog | Travel screen |

### 2.5 Input System Architecture

```
┌─────────────────────────────────────────────────────────┐
│                    Game Logic                           │
│  (Receives GameAction events, knows nothing about       │
│   keyboard/gamepad/touch)                               │
└─────────────────────────┬───────────────────────────────┘
                          │ GameAction events
                          ▼
┌─────────────────────────────────────────────────────────┐
│                 Input Manager                           │
│  - Holds current InputMapping                           │
│  - Translates PlatformInput → GameAction                │
│  - Handles remapping, conflicts, dead zones            │
└─────────────────────────┬───────────────────────────────┘
                          │ PlatformInput events
                          ▼
┌─────────────────────────────────────────────────────────┐
│              Platform Input Adapters                    │
│  ┌─────────────┐ ┌────────────┐ ┌─────────┐ ┌────────┐ │
│  │  Terminal   │ │  Desktop   │ │ Mobile  │ │  Web   │ │
│  │  (crossterm/│ │  (SDL/GLFW/│ │ (touch  │ │ (DOM   │ │
│  │   bubbletea)│ │   winit)   │ │  events)│ │ events)│ │
│  └─────────────┘ └────────────┘ └─────────┘ └────────┘ │
└─────────────────────────────────────────────────────────┘
```

---

## 3. Rendering Abstraction Layer

### 3.1 Renderer Interface

```typescript
interface Renderer {
  // Lifecycle
  init(config: RenderConfig): Promise<void>;
  shutdown(): Promise<void>;
  resize(width: number, height: number): void;

  // Frame
  beginFrame(): void;
  endFrame(): void;

  // Primitives
  drawText(x: number, y: number, text: string, style: TextStyle): void;
  drawRect(x: number, y: number, w: number, h: number, style: RectStyle): void;
  drawLine(x1: number, y1: number, x2: number, y2: number, style: LineStyle): void;
  drawImage(x: number, y: number, image: ImageHandle, opts?: DrawImageOpts): void;

  // Advanced
  pushScissor(x: number, y: number, w: number, h: number): void;
  popScissor(): void;
  pushLayer(opacity: number): void;
  popLayer(): void;

  // Metrics
  measureText(text: string, style: TextStyle): TextMetrics;
  getDPI(): number;
  getColorSpace(): ColorSpace;
}
```

### 3.2 Renderer Implementations

| Renderer | Backend | Features | Fallback |
|----------|---------|----------|----------|
| **TerminalRenderer** | ANSI/UTF-8, crossterm/bubbletea | Text, box drawing, 256-color, truecolor, mouse | N/A (base) |
| **GPURenderer** | Vulkan/Metal/DX12/WebGPU | Text atlas, SDF fonts, shaders, post-FX | CPURenderer |
| **CPURenderer** | Software raster (pixman/skia) | Full 2D, anti-aliased text | TerminalRenderer |
| **CanvasRenderer** | Canvas2D/WebGL2 | DOM overlay for text, WebGL for FX | Canvas2D only |
| **DOMRenderer** | HTML/CSS | Full accessibility tree, SEO | CanvasRenderer |

### 3.3 Visual Language Tokens (Platform-Agnostic)

```typescript
interface VisualTokens {
  // Colors (semantic, not literal)
  colors: {
    // Base
    background: ColorToken;        // #0a0a0f / terminal default
    surface: ColorToken;           // #12121a
    surfaceElevated: ColorToken;   // #1a1a24
    border: ColorToken;            // #2a2a3a
    borderFocus: ColorToken;       // #00ff88 (neon green)

    // Text
    textPrimary: ColorToken;       // #e8e8f0
    textSecondary: ColorToken;     // #888899
    textMuted: ColorToken;         // #555566
    textInverse: ColorToken;       // #0a0a0f

    // Semantic
    success: ColorToken;           // #00ff88
    warning: ColorToken;           // #ffaa00
    danger: ColorToken;            // #ff3366
    info: ColorToken;              // #00aaff

    // Faction
    corp: ColorToken;              // #0066ff (blue)
    syndicate: ColorToken;         // #ff0066 (magenta)
    freeport: ColorToken;          // #00ffaa (cyan)
    fringe: ColorToken;            // #ff8800 (orange)
    blacksite: ColorToken;         // #aa00ff (purple)

    // Heat gradient
    heatLow: ColorToken;           // #00ff88
    heatMed: ColorToken;           // #ffaa00
    heatHigh: ColorToken;          // #ff5500
    heatCritical: ColorToken;      // #ff0033

    // Matrix rain
    matrixHead: ColorToken;        // #ffffff
    matrixBody: ColorToken;        // #00ff88
    matrixTail: ColorToken;        // #004422
  };

  // Typography
  typography: {
    fontMono: FontToken;           // "JetBrains Mono", "Fira Code", terminal
    fontUI: FontToken;             // "IBM Plex Sans", system UI
    fontDisplay: FontToken;        // "Orbitron", "Rajdhani" (cyberpunk)
    scale: {
      xs: 0.75;    // 12px / 9pt
      sm: 0.875;   // 14px / 10.5pt
      base: 1.0;   // 16px / 12pt
      lg: 1.125;   // 18px / 13.5pt
      xl: 1.25;    // 20px / 15pt
      '2xl': 1.5;  // 24px / 18pt
      '3xl': 2.0;  // 32px / 24pt
    };
    weights: {
      normal: 400;
      medium: 500;
      semibold: 600;
      bold: 700;
    };
  };

  // Spacing (base unit = 4px / 1 char cell)
  spacing: {
    0: 0;
    1: 4;    // 1 unit
    2: 8;    // 2 units
    3: 12;
    4: 16;
    5: 20;
    6: 24;
    8: 32;
    10: 40;
    12: 48;
    16: 64;
  };

  // Effects
  effects: {
    matrixRainDensity: number;     // 0.0-1.0
    matrixRainSpeed: number;       // chars/sec
    scanlineOpacity: number;       // 0.0-1.0
    vignetteStrength: number;      // 0.0-1.0
    crtCurvature: number;          // 0.0-1.0
    glowBloom: boolean;            // neon glow
    reducedMotion: boolean;        // accessibility
  };
}
```

### 3.4 Terminal-Specific Rendering

| Feature | Implementation | Fallback |
|---------|----------------|----------|
| **Matrix Rain** | Character buffer, per-column offset, trail decay | Static noise pattern |
| **Box Drawing** | Unicode box-drawing chars (─│┌┐└┘├┤┬┴┼) | ASCII (+-|) |
| **Colors** | Truecolor (24-bit) → 256 → 16 → mono | Grayscale chars (@%#*+=-:. ) |
| **Fonts** | N/A (terminal font) | N/A |
| **Animations** | Frame-based (16ms ticks) | Instant state changes |

---

## 4. Asset Pipeline

### 4.1 Asset Categories

| Category | Formats | Pipeline | Platform Variants |
|----------|---------|----------|-------------------|
| **Fonts** | TTF, OTF, WOFF2 | Subset → SDF atlas (GPU) / bitmap (CPU) | Mono/UI/Display per platform |
| **UI Icons** | SVG → PNG/WebP | Rasterize @ 1x, 2x, 3x | Terminal: Unicode/CP437 |
| **Portraits** | PNG, WebP | Compress, mipmap | Terminal: ASCII art / ANSI art |
| **Backgrounds** | PNG, WebP | Compress, tiling variants | Terminal: matrix rain only |
| **SFX** | OGG, WAV | Encode @ 44.1kHz/48kHz | Web: WebM/MP3 fallback |
| **Music** | OGG, FLAC | Loop points, adaptive layers | Terminal: None |
| **Shaders** | GLSL/WGSL/MSL/HLSL | Cross-compile (spirv-cross) | Terminal: N/A |
| **Localization** | JSON, PO, XLIFF | Extract → translate → compile | All platforms |

### 4.2 Build Pipeline

```
Source Assets
     │
     ├── Fonts ──► subsetter ──► SDF Atlas (GPU) / Bitmap (CPU/Terminal)
     ├── SVGs  ──► svgr/rsvg  ──► PNG/WebP @ 1x,2x,3x / Unicode (Terminal)
     ├── Images ──► mozjpeg/oxipng ──► Compressed + mipmaps
     ├── Audio ──► opus/ogg ──► Multi-quality (low/med/high)
     ├── Shaders ──► glslang/spirv-cross ──► SPIR-V + per-backend
     └── Locales ──► gettext/intl ──► Compiled .mo / JSON bundles
     │
     ▼
Asset Manifest (JSON) ──► Platform-specific bundles ──► Distribution
```

### 4.3 Terminal Asset Strategy

- **No external assets required** - pure code rendering
- **Unicode/CP437 graphics** for icons, portraits, borders
- **ANSI art** for splash screens, sector backgrounds
- **Color palette** defined in 16/256/truecolor variants
- **Fonts** - rely on terminal emulator font configuration

---

## 5. Localization (i18n)

### 5.1 Supported Languages (Launch + Roadmap)

| Phase | Languages | Script | RTL |
|-------|-----------|--------|-----|
| **Launch** | English (en) | Latin | No |
| **v1.1** | Spanish (es), French (fr), German (de), Portuguese (pt), Russian (ru), Chinese Simplified (zh-CN), Japanese (ja), Korean (ko) | Latin, Cyrillic, Han, Hangul | No |
| **v1.2** | Arabic (ar), Hebrew (he) | Arabic, Hebrew | **Yes** |
| **v1.3** | Polish (pl), Turkish (tr), Italian (it), Dutch (nl) | Latin | No |

### 5.2 Localization Architecture

```typescript
// Message format (ICU MessageFormat)
interface Message {
  id: string;                    // "market.buy.confirm"
  default: string;               // "Buy {count} {item} for {price}¥?"
  context?: string;              // "market buy confirmation"
  placeholders: PlaceholderDef[]; // [{name: "count", type: "number"}]
  notes?: string;                // Translator notes
}

// Per-locale compiled bundle
interface LocaleBundle {
  locale: string;                // "ja"
  messages: Map<string, string>; // Compiled ICU patterns
  pluralRules: PluralRuleFunc;   // CLDR plural categories
  numberFormat: NumberFormat;    // Locale-specific formatting
  dateFormat: DateFormat;        // Locale-specific dates
  rtl: boolean;                  // Right-to-left
  fontFallback: string[];        // ["Noto Sans JP", "Noto Sans CJK SC"]
}
```

### 5.3 RTL Support

| Aspect | Implementation |
|--------|----------------|
| **Layout** | Flexbox/Grid with `dir="rtl"` (web), mirrored coordinates (native) |
| **Text** | ICU BiDi algorithm, explicit LRM/RLM marks |
| **Icons** | Mirror directional icons (arrows, chevrons), keep universal icons |
| **Input** | Cursor navigation reversed, gamepad unchanged |
| **Terminal** | Limited - use visual ordering, logical for screen readers |

### 5.4 Terminal Localization Constraints

- **Monospace fonts required** - CJK needs double-width, emoji variable width
- **Line breaking** - ICU break iterator, no hyphenation
- **Input** - IME composition support (crossterm/bubbletea have partial support)
- **Fallback** - English if glyph missing, show missing-glyph indicator

---

## 6. Accessibility (WCAG 2.1 AA)

### 6.1 Conformance Target

| Level | Criteria | Target |
|-------|----------|--------|
| **A** | 30 criteria | ✅ Required |
| **AA** | 20 criteria | ✅ Required |
| **AAA** | 28 criteria | 🎯 Aspirational (selected) |

### 6.2 Per-Platform Accessibility Mapping

| WCAG Criterion | Terminal | Desktop GUI | Mobile | Web | Console |
|----------------|----------|-------------|--------|-----|---------|
| **1.1.1 Non-text Content** | Text alt for ASCII art | Alt text, ARIA labels | Alt text, content descriptions | Alt, ARIA | Platform SDK |
| **1.3.1 Info & Relationships** | Semantic structure via text | Heading hierarchy, landmarks | Heading hierarchy, landmarks | Semantic HTML, ARIA | Platform SDK |
| **1.4.3 Contrast (Min)** | 16-color palette tuned | 4.5:1 minimum | 4.5:1 minimum | 4.5:1 minimum | Platform cert |
| **1.4.4 Resize Text** | Terminal zoom | 200% zoom, no horizontal scroll | Dynamic Type, pinch zoom | 200% zoom, rem units | System zoom |
| **1.4.10 Reflow** | N/A (fixed grid) | 320px width / 256px height | 320px width | 320 CSS pixels | N/A |
| **1.4.11 Non-text Contrast** | Border chars 3:1 | UI components 3:1 | UI components 3:1 | UI components 3:1 | Platform cert |
| **1.4.13 Content on Hover/Focus** | Status line | Tooltips, focus visible | N/A (touch) | Tooltips, focus visible | Platform SDK |
| **2.1.1 Keyboard** | Full keyboard nav | Full keyboard nav | Virtual keyboard + BT keyboard | Full keyboard nav | Gamepad mapping |
| **2.1.2 No Keyboard Trap** | Esc always exits | Tab cycle, Esc exits | Swipe gestures + back | Tab cycle, Esc exits | Home button |
| **2.1.4 Character Key Shortcuts** | Single-key shortcuts | Remappable | N/A | Remappable | Gamepad remap |
| **2.2.1 Timing Adjustable** | Turn-based (no timers) | Turn-based | Turn-based | Turn-based | Turn-based |
| **2.2.2 Pause/Stop/Hide** | Pause menu | Pause menu | Pause menu | Pause menu | Home button |
| **2.3.1 Three Flashes** | No flashing | No flashing | No flashing | No flashing | Platform cert |
| **2.4.3 Focus Order** | Logical tab order | Logical tab order | Logical swipe order | DOM order = visual | Platform SDK |
| **2.4.7 Focus Visible** | Highlight + status | 3px outline + glow | Focus ring | 3px outline + glow | Platform SDK |
| **2.5.3 Label in Name** | Status line text | ARIA labels match visible | Accessibility labels | ARIA labels | Platform SDK |
| **2.5.5 Target Size** | N/A (text) | 44×44px min | 48×48dp min | 44×44 CSS px | Platform cert |
| **3.1.1 Language of Page** | N/A | lang attribute | locale | lang attribute | Platform SDK |
| **3.2.1 On Focus** | No auto-action | No auto-action | No auto-action | No auto-action | Platform SDK |
| **3.3.2 Labels/Instructions** | Inline help screen help screen | Inline + help | Inline + help | Inline + help | Platform SDK |
| **4.1.2 Name/Role/Value** | Text representation | ARIA roles/states | Accessibility traits | ARIA roles/states | Platform SDK |

### 6.3 Screen Reader Support

| Platform | API | Implementation |
|----------|-----|----------------|
| **Terminal** | speak/tts (espeak, say, narrator) | Announce screen changes, status line updates, menu position |
| **Desktop** | AT-SPI (Linux), UIA (Windows), NSAccessibility (macOS) | Full widget tree, live regions for dynamic content |
| **Mobile** | VoiceOver (iOS), TalkBack (Android) | Semantic labels, rotor/gestures, live announcements |
| **Web** | ARIA live regions, roles, properties | `role="status" aria-live="polite"`, `role="log"` |
| **Console** | Platform accessibility APIs | Platform-certified implementation |

### 6.4 Color Blindness Support

| Type | Prevalence | Palette Adjustment |
|------|------------|---------------------|
| **Deuteranopia** (green-blind) | 6% male | Avoid red/green distinction; use blue/orange |
| **Protanopia** (red-blind) | 2% male | Avoid red/green; use blue/yellow |
| **Tritanopia** (blue-blind) | <0.01% | Avoid blue/yellow; use red/green |

**Implementation:**
- Color-blind safe palette as default (CVD-friendly)
- Pattern/icon reinforcement for all color-coded info
- User-selectable palette: Default / Deuteranopia / Protanopia / Tritanopia / Monochrome
- Terminal: Use shape + text labels, not color alone

### 6.5 Motor Accessibility

| Feature | Implementation |
|---------|----------------|
| **Remappable Controls** | Full remap for keyboard/gamepad; saved per profile |
| **Hold-to-Repeat** | Configurable delay/rate for navigation |
| **Sticky Keys** | Modifier keys toggle (Shift, Ctrl, Alt, Meta) |
| **One-Handed Mode** | All actions reachable via single hand (terminal: Vim keys) |
| **Auto-Action** | Optional auto-buy/sell/travel for reduced input |
| **Input Buffering** | Queue inputs during animation/transition |
| **Gamepad Dead Zones** | Configurable per-stick, per-trigger |

### 6.6 Cognitive Accessibility

| Feature | Implementation |
|---------|----------------|
| **Reduced Motion** | Disable matrix rain, scanlines, transitions, parallax |
| **High Contrast** | Force 7:1 contrast, remove gradients, bold borders |
| **Simplified UI** | Hide advanced stats, compact mode, larger hit targets |
| **Text Scaling** | 100%-300% in 25% steps, respects system setting |
| **Reading Assist** | Dyslexia-friendly font option (OpenDyslexic, Lexend) |
| **Pause Anywhere** | Instant pause, no penalty, state preserved |
| **Tutorial/Tooltips** | Contextual, dismissible, replayable |
| **Consistent Navigation** | Same keys/buttons everywhere, documented in Help |

---

## 7. Performance Budgets

### 7.1 Frame Budget Allocation (60 FPS = 16.67ms)

| System | Terminal | Desktop GPU | Mobile | Web (WASM) |
|--------|----------|-------------|--------|------------|
| **Input** | <0.5ms | <0.5ms | <1ms | <1ms |
| **Simulation** | <2ms | <2ms | <3ms | <3ms |
| **UI Layout** | <1ms | <1ms | <2ms | <2ms |
| **Rendering** | <4ms | <6ms | <8ms | <8ms |
| **Audio** | <0.1ms | <1ms | <2ms | <2ms |
| **GC/Overhead** | <1ms | <2ms | <4ms | <2ms |
| **Total** | **<8.6ms** | **<12.5ms** | **<20ms** | **<18ms** |
| **Headroom** | 8ms | 4ms | -3ms* | -1ms* |

*Mobile/Web need 30 FPS fallback (33ms budget) or quality scaling

### 7.2 Memory Budgets

| Asset Type | Terminal | Desktop | Mobile | Web |
|------------|----------|---------|--------|-----|
| **Code** | 5MB | 20MB | 15MB | 10MB (WASM) |
| **Fonts** | 0 (system) | 8MB (SDF atlas) | 6MB | 4MB (WOFF2) |
| **Textures** | 0 | 64MB | 32MB | 24MB |
| **Audio** | 0 | 32MB | 16MB | 12MB (streamed) |
| **Save Data** | 1MB | 1MB | 1MB | 5MB (IndexedDB) |
| **Total** | **~6MB** | **~125MB** | **~70MB** | **~55MB** |

### 7.3 Scaling Strategies

| Tier | Terminal | Desktop | Mobile | Web |
|------|----------|---------|--------|-----|
| **Ultra** | N/A | 4K, all FX, 60fps | N/A | N/A |
| **High** | Truecolor, matrix rain | 1080p, all FX, 60fps | 1080p, reduced FX, 60fps | 1080p, reduced FX, 60fps |
| **Medium** | 256-color, rain | 1080p, some FX, 60fps | 720p, minimal FX, 30fps | 720p, minimal FX, 30fps |
| **Low** | 16-color, no rain | 720p, no FX, 60fps | 540p, no FX, 30fps | 540p, no FX, 30fps |
| **Potato** | Mono, no rain | 480p, no FX, 30fps | 480p, no FX, 30fps | 480p, no FX, 30fps |

---

## 8. Distribution Channels

### 8.1 Platform-Specific Requirements

| Platform | Binary Format | Signing/Notarization | Store Requirements | Update Mechanism |
|----------|---------------|----------------------|-------------------|------------------|
| **Linux** | ELF (AppImage, .deb, .rpm, Flatpak, Snap) | GPG sign, Flatpak/Snap auto | Flathub, Snapcraft | Package manager, Flatpak, Snap |
| **macOS** | Mach-O (Universal .app, .dmg) | Apple Notarization (Developer ID) | Mac App Store (optional), Homebrew | Sparkle, Homebrew |
| **Windows** | PE/COFF (.exe, .msi, .msix) | Authenticode + EV cert, MSIX | Microsoft Store, WinGet, Scoop | MSIX auto, WinGet, custom |
| **Steam Deck** | ELF (Linux) | Steamworks | Steam Deck Verified | Steam auto-update |
| **iOS** | Mach-O (IPA) | Apple App Store cert | App Store review, guidelines | App Store auto |
| **Android** | ELF (APK, AAB) | Play App Signing | Play Store review, target API | Play Store auto |
| **Web** | WASM + JS/HTML/CSS | HTTPS, CSP, COOP/COEP | itch.io, GitHub Pages, self-host | Service worker, manual |
| **Switch** | NRO/NSO | Nintendo SDK cert | Nintendo lotcheck | eShop |
| **PS5** | ELF (PKG) | Sony SDK cert | Sony certification | PSN |
| **Xbox** | PE (XVC) | Microsoft SDK cert | Xbox certification | Xbox Live |

### 8.2 Build Matrix

| Target | CI Job | Artifacts | Tests |
|--------|--------|-----------|-------|
| Linux x86_64 | `build-linux` | AppImage, .deb, .rpm, tar.gz | Unit, integration, terminal render |
| Linux ARM64 | `build-linux-arm64` | tar.gz | Unit, terminal render |
| macOS x86_64 | `build-macos-x64` | .dmg, .app.tar.gz | Unit, integration |
| macOS ARM64 | `build-macos-arm64` | .dmg, .app.tar.gz | Unit, integration |
| Windows x86_64 | `build-windows` | .exe, .msi, .msix | Unit, integration |
| Web (WASM) | `build-web` | .wasm, .js, .html, assets | Unit, browser (playwright) |
| Android | `build-android` | .aab, .apk | Unit, instrumented |
| iOS | `build-ios` | .ipa (TestFlight) | Unit, UI tests |

### 8.3 Versioning & Release

```
Version: MAJOR.MINOR.PATCH[-PRERELEASE+BUILD]
Example: 1.2.3-beta.1+20250616.abc123

Channels:
  - stable:   Semantic version, full QA
  - beta:     Pre-release, opt-in testers
  - nightly:  Daily build, dev only
  - canary:   Per-commit, internal only

Per-platform version suffix:
  - Linux:     1.2.3
  - Steam:     1.2.3-steam
  - iOS:       1.2.3 (build 42)
  - Android:   1.2.3 (versionCode 42)
  - Web:       1.2.3-web.20250616
```

---

## 9. Abstraction Layer Summary

### 9.1 Core Interfaces to Implement Per Platform

```rust
// Platform abstraction trait (Rust example)
trait Platform {
    type Renderer: Renderer;
    type Input: InputAdapter;
    type Storage: Storage;
    type Audio: Audio;
    type Network: Network;
    type Clipboard: Clipboard;
    type Notification: Notification;

    fn create_renderer(&self) -> Self::Renderer;
    fn create_input(&self) -> Self::Input;
    fn create_storage(&self) -> Self::Storage;
    fn create_audio(&self) -> Self::Audio;
    fn create_network(&self) -> Self::Network;
    fn create_clipboard(&self) -> Self::Clipboard;
    fn create_notification(&self) -> Self::Notification;

    // Platform info
    fn platform_id(&self) -> PlatformId;
    fn capabilities(&self) -> PlatformCapabilities;
    fn locale(&self) -> Locale;
    fn prefers_reduced_motion(&self) -> bool;
    fn prefers_high_contrast(&self) -> bool;
    fn color_scheme(&self) -> ColorScheme; // light/dark
}
```

### 9.2 Shared Core (Platform-Agnostic)

```
neon-trader-core/
├── game/
│   ├── simulation/      # Pure logic, deterministic
│   ├── mechanics/       # Formulas, rules
│   ├── economy/         # Prices, upgrades, reputation
│   ├── sectors/         # Sector/payload data
│   ├── events/          # Event system
│   └── save/            # Save schema, migration
├── ui/
│   ├── screens/         # Screen definitions (data-driven)
│   ├── widgets/         # Widget logic (data → layout)
│   ├── navigation/      # Screen graph, transitions
│   └── theme/           # Visual tokens → platform renderer
├── input/
│   ├── actions.rs       # GameAction enum
│   ├── mapping.rs       # Default mappings per platform
│   └── manager.rs       # Input → Action translation
└── platform/
    ├── traits.rs        # Platform trait definitions
    ├── config.rs        # PlatformConfig, UserSettings
    └── error.rs         # PlatformError types
```

### 9.3 Platform Adapters (Per-Platform Crates)

```
neon-trader-terminal/     # crossterm / bubbletea
neon-trader-desktop/      # winit + vulkano / gfx-hal
neon-trader-mobile/       # winit + vulkano (Android) / metal (iOS)
neon-trader-web/          # wasm-bindgen + web-sys / canvas
neon-trader-console/      # Platform SDKs (private repos)
```

---

## 10. Cross-References

| This Doc Section | References |
|------------------|------------|
| **Platform Matrix** | DESIGN_OVERVIEW.md (Architecture), GAME_MECHANICS.md (Performance) |
| **Input Abstraction** | UI_UX_DESIGN.md (Navigation, Keyboard Schemes) |
| **Rendering Abstraction** | UI_UX_DESIGN.md (Visual Language, Widget Library) |
| **Asset Pipeline** | UI_UX_DESIGN.md (Asset List), SECTORS_AND_PAYLOADS.md (Portraits) |
| **Localization** | DESIGN_OVERVIEW.md (Scope), ECONOMY_AND_PROGRESSION.md (Terminology) |
| **Accessibility** | UI_UX_DESIGN.md (Accessibility Section), DESIGN_INDEX.md (Traceability) |
| **Performance Budgets** | GAME_MECHANICS.md (Turn Timing), DESIGN_OVERVIEW.md (Invariants) |
| **Distribution** | MULTIPLAYER_AND_NETWORKING.md (Cross-Platform Play), SAVE_SYSTEM.md (Cloud Sync) |

---

## 11. Revision History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0.0 | 2025-06-16 | Swarm Agent | Initial comprehensive draft |

---

## 12. Appendix: Platform-Specific Gotchas

### Terminal
- **Windows ConPTY** requires Windows 10 1809+, fallback to win32 console API
- **SSH** may strip ANSI sequences - detect and degrade gracefully
- **Tmux/Screen** intercept keys - document prefix key conflicts
- **Font fallback** varies wildly - test with Nerd Fonts, system fonts

### Desktop GUI
- **HiDPI** - fractional scaling on Linux (GTK/Qt), Windows (DPI awareness v2), macOS (backing scale)
- **VSync** - present modes: Mailbox (low latency) vs FIFO (standard)
- **Exclusive fullscreen** - required for consoles, optional desktop

### Mobile
- **Thermal throttling** - monitor, reduce quality automatically
- **Battery** - offer "battery saver" mode (30fps, reduced FX)
- **Orientation** - portrait primary, landscape supported
- **Notches/safe area** - inset UI, especially on iOS

### Web
- **WASM memory** - 2GB limit (32-bit), 4GB (64-bit), use memory64 when stable
- **Threading** - SharedArrayBuffer requires COOP/COEP headers
- **Audio** - Autoplay policy requires user gesture first
- **IndexedDB** - quota varies, handle QuotaExceededError

### Console
- **Certification** - 3-6 month lead time, strict requirements
- **Save data** - platform encryption, cloud sync via platform
- **Achievements/trophies** - integrate early
- **Suspend/resume** - must handle instant suspend/resume correctly