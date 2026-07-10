# Card Title Dark Theme Fix

## Issue
The `.card-title` Bootstrap class was hard to read on dark theme backgrounds in the quiz (and other pages using cards) because Bootstrap's default dark text color was being used instead of the theme's light text color.

## Solution
Updated `wwwroot/css/themes/theme-base.css` to add CSS overrides for card title and text elements.

### Changes Made

Added two new CSS rules after the card styles (around line 630):

```css
.card-title {
	color: var(--theme-text-primary) !important;
}

.card-text {
	color: var(--theme-text-secondary);
}
```

## Technical Details

### Why This Works
- Bootstrap's `.card-title` class uses a default dark color that doesn't adapt to themes
- By overriding with `var(--theme-text-primary)`, card titles now use the theme's primary text color
- Dark themes have white/near-white primary text colors
- Light themes have dark primary text colors
- The `!important` flag ensures this override takes precedence over Bootstrap's defaults

### Color Values by Theme

#### Dark Themes (now white/bright)
- **Dark Mode**: `#f7fafc` (near white)
- **Midnight Blue**: `#e3f2fd` (bright blue-white)
- **Cyberpunk**: `#e0e0e0` (bright gray)
- **Espresso**: `#f5f5dc` (bright beige)
- **Crimson Night**: `#ffe0eb` (bright pink)
- **High Contrast**: `#ffffff` (pure white)

#### Light Themes (appropriate dark colors)
- **Default**: `#2d3748` (dark gray)
- **Ocean Blue**: `#002952` (dark blue)
- **Warm Sunset**: `#5c0000` (dark red)
- And all other light themes maintain appropriate contrast

## Impact

### Pages Affected
All pages using `.card-title` now have proper theme-aware colors:
- ✅ **Quiz pages** (question cards)
- ✅ **Flashcard pages** (term/equation cards)
- ✅ **Exercise pages** (problem cards)
- ✅ **Graded pages** (result cards)
- ✅ **Home page** (feature cards)
- ✅ **Study materials management** (upload cards)
- ✅ **Settings pages** (preference cards)

### Components Improved
1. **`.card-title`**: Now uses `var(--theme-text-primary)` for maximum visibility
2. **`.card-text`**: Now uses `var(--theme-text-secondary)` for proper hierarchy

## Before & After

### Before
```html
<!-- Bootstrap default: dark text on all backgrounds -->
<div class="card">
  <div class="card-body">
	<h5 class="card-title">Question 1</h5>
	<p class="card-text">What is the accounting equation?</p>
  </div>
</div>
<!-- Dark themes: dark gray text on dark background = hard to read ❌ -->
```

### After
```html
<!-- Theme-aware: adapts to background -->
<div class="card">
  <div class="card-body">
	<h5 class="card-title">Question 1</h5>
	<p class="card-text">What is the accounting equation?</p>
  </div>
</div>
<!-- Dark themes: white text on dark background = easy to read ✅ -->
<!-- Light themes: dark text on light background = easy to read ✅ -->
```

## Benefits

1. **Accessibility**: Card titles are now readable in all themes
2. **Consistency**: Matches the fix applied to `.text-muted` earlier
3. **Maintainability**: Single CSS variable controls card title color across all themes
4. **Comprehensive**: Also added `.card-text` for proper text hierarchy
5. **No Breaking Changes**: All themes continue to work with appropriate contrast

## Related Fixes

This fix complements the earlier `.text-muted` fix, creating a comprehensive solution for Bootstrap text utilities in themed environments:
- `.text-muted` → uses `var(--theme-text-muted)`
- `.card-title` → uses `var(--theme-text-primary)`
- `.card-text` → uses `var(--theme-text-secondary)`

## Testing

- ✅ Build successful
- ✅ All themes now use appropriate card title colors
- ✅ Dark theme card titles are white/near-white for maximum visibility
- ✅ Light theme card titles remain appropriately dark for good contrast
- ✅ No visual regressions in existing pages

## Files Modified

- `wwwroot/css/themes/theme-base.css`
  - Added `.card-title` CSS override (line ~633)
  - Added `.card-text` CSS override (line ~637)

## User Experience Improvement

**Quiz Experience**: Users selecting dark themes can now easily read question titles, making the quiz more usable and reducing eye strain. The same improvement applies to all other pages using card layouts throughout the application.
