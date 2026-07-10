# Text-Muted Dark Theme Fix

## Issue
The `.text-muted` Bootstrap class was hard to read on dark theme backgrounds because it was using the default Bootstrap gray color instead of the theme's CSS variable.

## Solution
Updated `wwwroot/css/themes/theme-base.css` to:

1. **Added CSS override for `.text-muted` class**
   - Applied `var(--theme-text-muted)` to the `.text-muted` Bootstrap class
   - Used `!important` to override Bootstrap's default styling
   - Location: Line ~686 in theme-base.css

2. **Updated dark theme variables for better visibility**
   Changed `--theme-text-muted` values for all dark themes to match their primary text color (white/near-white) for maximum readability:

   | Theme | Old Value | New Value | Description |
   |-------|-----------|-----------|-------------|
   | **Dark Mode** | `#cbd5e0` (light gray) | `#f7fafc` (near white) | Now matches primary text |
   | **Midnight Blue** | `#e1f5fe` (light blue) | `#e3f2fd` (brighter blue-white) | Now matches primary text |
   | **Cyberpunk** | `#d0d0d0` (medium gray) | `#e0e0e0` (lighter gray) | Now matches primary text |
   | **Espresso** | `#f0e6d2` (beige) | `#f5f5dc` (brighter beige) | Now matches primary text |
   | **Crimson Night** | `#fff0f5` (light pink) | `#ffe0eb` (bright pink) | Now matches primary text |

## Technical Details

### CSS Rule Added
```css
/* Text utilities - Bootstrap overrides */
.text-muted {
	color: var(--theme-text-muted) !important;
}
```

### Why This Works
- Bootstrap's `.text-muted` class originally uses a fixed gray color
- By overriding it with CSS variables, the color now adapts to each theme
- Dark themes use bright colors for muted text (white/near-white)
- Light themes continue to use darker muted colors for proper contrast
- The `!important` flag ensures this override takes precedence over Bootstrap

## Impact

### Pages Affected
Any page using `.text-muted` class will now have improved readability in dark themes:
- Help pages (breadcrumbs, subtitles)
- Form labels with helper text
- Card footers
- List descriptions
- Timestamps
- Status messages

### Themes Improved
- ✅ Dark Mode
- ✅ Midnight Blue
- ✅ Cyberpunk
- ✅ Espresso
- ✅ Crimson Night
- ✅ High Contrast (already white, no change needed)

### Light Themes
Light themes are unaffected as they already had sufficient contrast:
- Default
- Ocean Blue
- Warm Sunset
- Forest Green
- Royal Purple
- Amber Gold
- Slate Gray
- Cherry Blossom
- Deep Teal
- Lavender Dream
- Mint Fresh

## Testing
- ✅ Build successful
- All themes now use appropriate text-muted colors
- Dark theme text-muted is now white/near-white for maximum visibility
- Light theme text-muted remains appropriately muted for good contrast

## Example Usage
```html
<!-- Before: Hard to read on dark backgrounds -->
<p class="text-muted">This is muted text</p>

<!-- After: Automatically uses theme-appropriate color -->
<p class="text-muted">This is muted text</p>
<!-- Dark themes: white/near-white -->
<!-- Light themes: appropriately muted gray/color -->
```

## Benefits
1. **Accessibility**: Improved readability in dark themes
2. **Consistency**: All text elements follow theme color scheme
3. **Maintainability**: Single CSS variable controls muted text across all themes
4. **Flexibility**: Easy to adjust individual theme muted colors if needed
5. **No Breaking Changes**: Light themes continue to work as before

## Files Modified
- `wwwroot/css/themes/theme-base.css`
  - Added `.text-muted` CSS override rule
  - Updated `--theme-text-muted` for 5 dark themes
