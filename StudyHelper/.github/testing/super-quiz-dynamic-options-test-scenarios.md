# Super Quiz Dynamic Question Count Options - Test Scenarios

**Feature:** Expanded question count selection with options: 10, 20, 30, ..., Half, Half+10, Half+20, ..., All

**Test Date:** 2025-01-22  
**Status:** ✅ Logic Verified via Code Review

---

## Option Generation Algorithm

The `SuperQuizStartViewModel.GetAvailableOptions()` method generates options as follows:

1. **Fixed increments** (10, 20, 30, ...): Loop from 10 to `HalfCount` in increments of 10
2. **Half**: Always included at `HalfCount` position
3. **Half+ increments** (Half+10, Half+20, ...): Loop from `HalfCount + 10` to `AllCount` in increments of 10
4. **All**: Always included at `AllCount` position

---

## Test Scenarios

### Scenario 1: Small Dataset (10 terms)
**Total Available Terms:** 10  
**Half Count:** 5 (but minimum 4 enforced = 5)

**Expected Options:**
- ❌ No fixed increments (10 >= HalfCount of 5)
- ✅ Half (5 Questions) - Balanced Coverage
- ❌ No Half+ increments (5 + 10 = 15 >= AllCount of 10)
- ✅ All (10 Questions) - Complete Mastery

**Result:** Only Half and All options displayed

---

### Scenario 2: Medium Dataset (38 terms)
**Total Available Terms:** 38  
**Half Count:** 19

**Expected Options:**
- ✅ 10 Questions (Quick Practice) - **DEFAULT**
- ✅ Half (19 Questions) - Balanced Coverage
- ✅ Half + 10 (29 Questions) - Extended Practice
- ✅ All (38 Questions) - Complete Mastery

**Result:** 4 options spanning the full range

---

### Scenario 3: Large Dataset (100 terms)
**Total Available Terms:** 100  
**Half Count:** 50

**Expected Options:**
- ✅ 10 Questions (Quick Practice) - **DEFAULT**
- ✅ 20 Questions (Moderate Practice)
- ✅ 30 Questions (Moderate Practice)
- ✅ 40 Questions (Moderate Practice)
- ✅ Half (50 Questions) - Balanced Coverage
- ✅ Half + 10 (60 Questions) - Extended Practice
- ✅ Half + 20 (70 Questions) - Extended Practice
- ✅ Half + 30 (80 Questions) - Extended Practice
- ✅ Half + 40 (90 Questions) - Extended Practice
- ✅ All (100 Questions) - Complete Mastery

**Result:** 10 options providing fine-grained control

---

### Scenario 4: Extra Large Dataset (250 terms)
**Total Available Terms:** 250  
**Half Count:** 125

**Expected Options:**
- Fixed: 10, 20, 30, 40, 50, 60, 70, 80, 90, 100, 110, 120 (12 options)
- Half: 125 (1 option)
- Half+: 135, 145, 155, 165, 175, 185, 195, 205, 215, 225, 235, 245 (12 options)
- All: 250 (1 option)

**Result:** 26 total options (may be long scrolling list in UI)

---

### Scenario 5: Edge Case - Exactly 20 terms
**Total Available Terms:** 20  
**Half Count:** 10

**Expected Options:**
- ✅ 10 Questions (Quick Practice) - **DEFAULT** (stops before Half = 10)
- ✅ Half (10 Questions) - Balanced Coverage
- ❌ No Half+ increments (10 + 10 = 20 >= AllCount of 20)
- ✅ All (20 Questions) - Complete Mastery

**Result:** 3 options (10, Half (10), All (20))

**Note:** User sees "10 Questions" and "Half (10 Questions)" as separate options with same count but different labels/descriptions.

---

### Scenario 6: Edge Case - Exactly 50 terms
**Total Available Terms:** 50  
**Half Count:** 25

**Expected Options:**
- ✅ 10 Questions (Quick Practice) - **DEFAULT**
- ✅ 20 Questions (Moderate Practice)
- ✅ Half (25 Questions) - Balanced Coverage
- ✅ Half + 10 (35 Questions) - Extended Practice
- ✅ Half + 20 (45 Questions) - Extended Practice
- ✅ All (50 Questions) - Complete Mastery

**Result:** 6 options spanning 10 to 50

---

## Loop Boundary Verification

### Fixed Increments Loop
```csharp
for (int count = 10; count < halfCount; count += 10)
```

- **Start:** 10
- **Condition:** `count < halfCount` (strictly less than, not <=)
- **Increment:** 10
- **Example (halfCount = 25):** Generates 10, 20 (stops at 30 because 30 >= 25)

### Half+ Increments Loop
```csharp
for (int count = halfCount + 10; count < allCount; count += 10)
```

- **Start:** `halfCount + 10`
- **Condition:** `count < allCount` (strictly less than, not <=)
- **Increment:** 10
- **Example (halfCount = 25, allCount = 50):** Generates 35, 45 (stops at 55 because 55 >= 50)

---

## Option Type Categorization

Each option is tagged with `SuperQuizOptionType` for UI styling:

| Type | Usage | Example Labels |
|------|-------|----------------|
| **Fixed** | 10, 20, 30, ... | "10 Questions", "20 Questions" |
| **Half** | Midpoint | "Half (25 Questions)" |
| **HalfPlus** | Half + increments | "Half + 10 (35 Questions)", "Half + 20 (45 Questions)" |
| **All** | Complete set | "All (50 Questions)" |

---

## Minimum Terms Validation

- **Minimum required:** 4 terms (from `SuperQuizStartViewModel.MinimumTermsRequired`)
- **HalfCount calculation:** `Math.Max(TotalAvailableTerms / 2, MinimumTermsRequired)`

### Edge Case: 5 terms
- **Half Count:** Math.Max(5 / 2, 4) = Math.Max(2, 4) = **4**
- **Expected Options:**
  - ❌ No fixed increments (10 >= 4)
  - ✅ Half (4 Questions)
  - ❌ No Half+ increments (4 + 10 = 14 >= 5)
  - ✅ All (5 Questions)

### Edge Case: 8 terms
- **Half Count:** Math.Max(8 / 2, 4) = Math.Max(4, 4) = **4**
- **Expected Options:**
  - ❌ No fixed increments (10 >= 4)
  - ✅ Half (4 Questions)
  - ❌ No Half+ increments (4 + 10 = 14 >= 8)
  - ✅ All (8 Questions)

---

## Default Selection

- **Default option:** First option in the list with `IsDefault = true`
- **Logic:** First fixed increment (10 Questions) is marked as default
- **If no fixed increments exist** (e.g., small dataset): No option is marked as default; first radio button is checked by HTML attribute

---

## UI Considerations

### Potential Issues with Large Datasets
For datasets > 200 terms, the number of options can exceed 20, which may result in:
- Long scrolling radio button list
- Cluttered UI

### Recommended Improvements (Future Work)
1. **Group options:** Collapsible sections for Fixed / Half / Half+ / All
2. **Dropdown/Select:** Use `<select>` instead of radio buttons for >10 options
3. **Custom input:** Add "Custom" option with text input for specific count
4. **Smart defaults:** Show only 5-7 key options, with "Show all" button

---

## Validation Summary

✅ **Algorithm verified:**
- Fixed increments correctly stop before Half
- Half+ increments correctly stop before All
- Minimum terms enforced (4)
- Default selection logic correct

✅ **Build successful:**
- No compilation errors
- All type changes propagated correctly

⚠️ **UI testing required:**
- Manual testing with actual study materials needed
- Verify option rendering for small/medium/large datasets
- Test form submission and session creation with various counts

---

## Next Steps

1. **Manual UI testing** with study materials of varying sizes (10, 38, 100, 250 terms)
2. **JavaScript testing** to verify preview updates work with dynamic options
3. **Help documentation update** to reflect new flexible selection feature
4. **Consider UI improvements** for large option lists (if needed based on testing)

---

**Test Status:** ✅ Algorithm verified via code review  
**Build Status:** ✅ Successful  
**Manual Testing:** ⏳ Pending user acceptance testing
