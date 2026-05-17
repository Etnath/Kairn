# Kairn — Color System
**Version 1.0 · May 2026**

---

## Concept

The Kairn color palette is drawn from the cairn stone — the stacked trail markers that give the product its name. Each ramp maps to something you would find on a mountain path: the weathered granite of the stones themselves, the lichen that grows between them, the cool blue-gray of a shaded slate face, the warm amber light at the summit, and the rust-red paint blazed on markers to signal the way.

---

## Ramps

Each ramp runs five tonal stops. Use **50** for backgrounds and subtle fills, **200** for borders and stripes, **500** for interactive elements and chart series, **700** for text on light fills, and **900** for deep dark surfaces.

---

### Stone — Granite Gray

> Structural neutral. Backgrounds, surfaces, borders, body text. The literal color of stacked trail stones.

| Stop | Hex | Primary use |
|---|---|---|
| 50 | `#F5F4F1` | App background |
| 200 | `#D6D3CA` | Borders, dividers, table lines, draft badge background |
| 500 | `#8C8980` | Muted labels, placeholders, disabled text |
| 700 | `#4A4843` | Icons (default state) |
| 900 | `#1F1E1C` | Body text, headings |

---

### Lichen — Forest Green

> Primary brand color. Navigation, buttons, positive states, and success indicators. The green growth found on high-altitude stones.

| Stop | Hex | Primary use |
|---|---|---|
| 50 | `#E8F4ED` | Nav hover, selected table row, success badge background |
| 200 | `#A8D9BC` | Light green fills, chart area fills |
| 500 | `#3A9463` | Primary button, nav active state, chart series 1, focus ring |
| 700 | `#1F6040` | Positive P&L values, paid invoice status, button hover |
| 900 | `#0D3322` | Deep green text on light fills |

---

### Slate — Mountain Blue-Gray

> Secondary accent. The cooler, bluer tone of shaded stone faces. Used for chart series, info states, and links.

| Stop | Hex | Primary use |
|---|---|---|
| 50 | `#EAF0F6` | Sent badge background, info state background |
| 200 | `#AABFD4` | Light slate fills |
| 500 | `#4D7A9E` | Chart series 2, info badge, links |
| 700 | `#2A4F6E` | Link hover, info text on light fills |
| 900 | `#112840` | Deep slate text |

---

### Summit — Warm Amber

> Warning and attention states. The golden light at the top of the climb. Budget alerts, thresholds, and anything that needs attention without being alarming.

| Stop | Hex | Primary use |
|---|---|---|
| 50 | `#FBF2E0` | Warning badge background |
| 200 | `#F2CF84` | Light amber fills |
| 500 | `#D4920F` | Warning badge, budget alert indicator |
| 700 | `#8F6008` | Warning text on light fills |
| 900 | `#4A3004` | Deep amber text |

---

### Signal — Rust Red

> Danger and error states. The rust-red paint blazed on cairn markers to warn hikers. Overdue invoices, negative P&L, destructive actions.

| Stop | Hex | Primary use |
|---|---|---|
| 50 | `#FAECE8` | Danger badge background, overdue row tint |
| 200 | `#F0B5A6` | Light red fills |
| 500 | `#C2492A` | Danger badge, delete button, overdue status |
| 700 | `#7E2A14` | Danger text on light fills, negative P&L values |
| 900 | `#3E1208` | Deep rust text |

---

## Semantic Mapping

These are the canonical pairings between UI concepts and palette values.

### Surfaces & Structure

| Element | Color | Token |
|---|---|---|
| App background | `#F5F4F1` | Stone 50 |
| Card surface | `#FFFFFF` | White |
| Card border | `#D6D3CA` | Stone 200 |
| Table row stripe | `#E8F4ED` | Lichen 50 |
| Divider / rule | `#D6D3CA` | Stone 200 |

### Typography

| Element | Color | Token |
|---|---|---|
| Body text | `#1F1E1C` | Stone 900 |
| Muted label / placeholder | `#8C8980` | Stone 500 |
| Icon (default) | `#4A4843` | Stone 700 |
| Link | `#4D7A9E` | Slate 500 |
| Link hover | `#2A4F6E` | Slate 700 |

### Interactive Elements

| Element | Color | Token |
|---|---|---|
| Primary button background | `#3A9463` | Lichen 500 |
| Primary button hover | `#1F6040` | Lichen 700 |
| Focus ring | `#3A9463` | Lichen 500 |
| Nav active state | `#3A9463` | Lichen 500 |
| Nav hover background | `#E8F4ED` | Lichen 50 |

### Status Badges

| Status | Background | Text |
|---|---|---|
| Draft | Stone 200 `#D6D3CA` | Stone 700 `#4A4843` |
| Sent | Slate 50 `#EAF0F6` | Slate 700 `#2A4F6E` |
| Paid | Lichen 50 `#E8F4ED` | Lichen 700 `#1F6040` |
| Overdue | Signal 50 `#FAECE8` | Signal 700 `#7E2A14` |
| Warning | Summit 50 `#FBF2E0` | Summit 700 `#8F6008` |

### Financial Values

| Context | Color | Token |
|---|---|---|
| Positive P&L / profit | `#1F6040` | Lichen 700 |
| Negative P&L / loss | `#7E2A14` | Signal 700 |
| Neutral / zero | `#8C8980` | Stone 500 |
| Budget warning threshold | `#D4920F` | Summit 500 |

### Charts

| Series | Color | Token |
|---|---|---|
| Series 1 (primary) | `#3A9463` | Lichen 500 |
| Series 2 (secondary) | `#4D7A9E` | Slate 500 |
| Series 3 (tertiary) | `#D4920F` | Summit 500 |
| Negative / danger area | `#C2492A` | Signal 500 |
| Grid lines | `#D6D3CA` | Stone 200 |
| Axis labels | `#8C8980` | Stone 500 |

---

## Accessibility Notes

- All text-on-background pairings in the semantic mapping above meet **WCAG 2.1 AA** contrast requirements (minimum 4.5:1 for body text, 3:1 for large text and UI components).
- Never use Stone 500 (`#8C8980`) as text on a white background for critical content — it passes AA for large text only. Reserve it for placeholders and decorative labels.
- Badge pairings (50 background + 700 text) all achieve at least 4.5:1 contrast.
- Do not introduce colors outside this system without verifying contrast ratios.

---

## MudBlazor Theme Configuration

The following maps the Kairn palette to a MudBlazor `MudTheme` in C#.

```csharp
var KairnTheme = new MudTheme
{
    Palette = new PaletteLight
    {
        // Brand
        Primary         = "#3A9463",   // Lichen 500
        PrimaryDarken   = "#1F6040",   // Lichen 700
        PrimaryLighten  = "#E8F4ED",   // Lichen 50

        // Secondary
        Secondary       = "#4D7A9E",   // Slate 500
        SecondaryDarken = "#2A4F6E",   // Slate 700
        SecondaryLighten= "#EAF0F6",   // Slate 50

        // Semantic
        Success         = "#3A9463",   // Lichen 500
        Warning         = "#D4920F",   // Summit 500
        Error           = "#C2492A",   // Signal 500
        Info            = "#4D7A9E",   // Slate 500

        // Surfaces
        Background      = "#F5F4F1",   // Stone 50
        Surface         = "#FFFFFF",
        DrawerBackground= "#1F6040",   // Lichen 700 (nav drawer)
        DrawerText      = "#E8F4ED",   // Lichen 50
        DrawerIcon      = "#A8D9BC",   // Lichen 200

        // Text
        TextPrimary     = "#1F1E1C",   // Stone 900
        TextSecondary   = "#8C8980",   // Stone 500
        TextDisabled    = "#D6D3CA",   // Stone 200

        // Lines
        Divider         = "#D6D3CA",   // Stone 200
        TableLines      = "#D6D3CA",   // Stone 200
        TableStriped    = "#E8F4ED",   // Lichen 50
    },
    PaletteDark = new PaletteDark
    {
        Primary         = "#3A9463",   // Lichen 500
        PrimaryDarken   = "#A8D9BC",   // Lichen 200
        PrimaryLighten  = "#0D3322",   // Lichen 900
        Background      = "#1F1E1C",   // Stone 900
        Surface         = "#2C2B29",
        TextPrimary     = "#F5F4F1",   // Stone 50
        TextSecondary   = "#8C8980",   // Stone 500
        DrawerBackground= "#0D3322",   // Lichen 900
        DrawerText      = "#A8D9BC",   // Lichen 200
    }
};
```

---

## Tailwind CSS Custom Colors

Add the following to `tailwind.config.js` to make the Kairn palette available as utility classes.

```js
module.exports = {
  theme: {
    extend: {
      colors: {
        stone: {
          50:  '#F5F4F1',
          200: '#D6D3CA',
          500: '#8C8980',
          700: '#4A4843',
          900: '#1F1E1C',
        },
        lichen: {
          50:  '#E8F4ED',
          200: '#A8D9BC',
          500: '#3A9463',
          700: '#1F6040',
          900: '#0D3322',
        },
        slate: {
          50:  '#EAF0F6',
          200: '#AABFD4',
          500: '#4D7A9E',
          700: '#2A4F6E',
          900: '#112840',
        },
        summit: {
          50:  '#FBF2E0',
          200: '#F2CF84',
          500: '#D4920F',
          700: '#8F6008',
          900: '#4A3004',
        },
        signal: {
          50:  '#FAECE8',
          200: '#F0B5A6',
          500: '#C2492A',
          700: '#7E2A14',
          900: '#3E1208',
        },
      },
    },
  },
}
```

---

*Kairn — navigate your finances with confidence.*
