# Super Quiz Question Count Options - Visual Examples

**Feature:** Dynamic question count selection with options: 10, 20, 30, ..., Half, Half+10, Half+20, ..., All

---

## Example 1: 38 Terms Available

### Visual UI Rendering

```
┌──────────────────────────────────────────────────────────────┐
│  🔘 10 Questions (Quick Practice)                   [DEFAULT]│
├──────────────────────────────────────────────────────────────┤
│  ◯ Half (19 Questions) (Balanced Coverage)                   │
├──────────────────────────────────────────────────────────────┤
│  ◯ Half + 10 (29 Questions) (Extended Practice)              │
├──────────────────────────────────────────────────────────────┤
│  ◯ All (38 Questions) (Complete Mastery)                     │
└──────────────────────────────────────────────────────────────┘

┌─────────────────────┐  ┌─────────────────────┐
│   Total Questions   │  │   Estimated Time    │
│        10           │  │     3 minutes       │
└─────────────────────┘  └─────────────────────┘
```

### When User Selects "Half + 10":

```
┌──────────────────────────────────────────────────────────────┐
│  ◯ 10 Questions (Quick Practice)                             │
├──────────────────────────────────────────────────────────────┤
│  ◯ Half (19 Questions) (Balanced Coverage)                   │
├──────────────────────────────────────────────────────────────┤
│  🔘 Half + 10 (29 Questions) (Extended Practice)    [SELECTED]│
├──────────────────────────────────────────────────────────────┤
│  ◯ All (38 Questions) (Complete Mastery)                     │
└──────────────────────────────────────────────────────────────┘

┌─────────────────────┐  ┌─────────────────────┐
│   Total Questions   │  │   Estimated Time    │
│        29           │  │     7 minutes       │
└─────────────────────┘  └─────────────────────┘
```

---

## Example 2: 100 Terms Available

### Visual UI Rendering

```
┌──────────────────────────────────────────────────────────────┐
│  🔘 10 Questions (Quick Practice)                   [DEFAULT]│
│  ◯ 20 Questions (Moderate Practice)                          │
│  ◯ 30 Questions (Moderate Practice)                          │
│  ◯ 40 Questions (Moderate Practice)                          │
│  ◯ Half (50 Questions) (Balanced Coverage)                   │
│  ◯ Half + 10 (60 Questions) (Extended Practice)              │
│  ◯ Half + 20 (70 Questions) (Extended Practice)              │
│  ◯ Half + 30 (80 Questions) (Extended Practice)              │
│  ◯ Half + 40 (90 Questions) (Extended Practice)              │
│  ◯ All (100 Questions) (Complete Mastery)                    │
└──────────────────────────────────────────────────────────────┘

┌─────────────────────┐  ┌─────────────────────┐
│   Total Questions   │  │   Estimated Time    │
│        10           │  │     3 minutes       │
└─────────────────────┘  └─────────────────────┘
```

### When User Selects "Half":

```
┌──────────────────────────────────────────────────────────────┐
│  ◯ 10 Questions (Quick Practice)                             │
│  ◯ 20 Questions (Moderate Practice)                          │
│  ◯ 30 Questions (Moderate Practice)                          │
│  ◯ 40 Questions (Moderate Practice)                          │
│  🔘 Half (50 Questions) (Balanced Coverage)        [SELECTED]│
│  ◯ Half + 10 (60 Questions) (Extended Practice)              │
│  ◯ Half + 20 (70 Questions) (Extended Practice)              │
│  ◯ Half + 30 (80 Questions) (Extended Practice)              │
│  ◯ Half + 40 (90 Questions) (Extended Practice)              │
│  ◯ All (100 Questions) (Complete Mastery)                    │
└──────────────────────────────────────────────────────────────┘

┌─────────────────────┐  ┌─────────────────────┐
│   Total Questions   │  │   Estimated Time    │
│        50           │  │     13 minutes      │
└─────────────────────┘  └─────────────────────┘
```

---

## Example 3: 10 Terms Available (Small Dataset)

### Visual UI Rendering

```
┌──────────────────────────────────────────────────────────────┐
│  🔘 Half (5 Questions) (Balanced Coverage)         [DEFAULT]│
├──────────────────────────────────────────────────────────────┤
│  ◯ All (10 Questions) (Complete Mastery)                     │
└──────────────────────────────────────────────────────────────┘

┌─────────────────────┐  ┌─────────────────────┐
│   Total Questions   │  │   Estimated Time    │
│         5           │  │     1 minutes       │
└─────────────────────┘  └─────────────────────┘
```

**Note:** No fixed increments shown because 10 >= Half (5)

---

## Example 4: 250 Terms Available (Extra Large Dataset)

### Visual UI Rendering

```
┌──────────────────────────────────────────────────────────────┐
│  🔘 10 Questions (Quick Practice)                   [DEFAULT]│
│  ◯ 20 Questions (Moderate Practice)                          │
│  ◯ 30 Questions (Moderate Practice)                          │
│  ◯ 40 Questions (Moderate Practice)                          │
│  ◯ 50 Questions (Moderate Practice)                          │
│  ◯ 60 Questions (Moderate Practice)                          │
│  ◯ 70 Questions (Moderate Practice)                          │
│  ◯ 80 Questions (Moderate Practice)                          │
│  ◯ 90 Questions (Moderate Practice)                          │
│  ◯ 100 Questions (Moderate Practice)                         │
│  ◯ 110 Questions (Moderate Practice)                         │
│  ◯ 120 Questions (Moderate Practice)                         │
│  ◯ Half (125 Questions) (Balanced Coverage)                  │
│  ◯ Half + 10 (135 Questions) (Extended Practice)             │
│  ◯ Half + 20 (145 Questions) (Extended Practice)             │
│  ◯ Half + 30 (155 Questions) (Extended Practice)             │
│  ◯ Half + 40 (165 Questions) (Extended Practice)             │
│  ◯ Half + 50 (175 Questions) (Extended Practice)             │
│  ◯ Half + 60 (185 Questions) (Extended Practice)             │
│  ◯ Half + 70 (195 Questions) (Extended Practice)             │
│  ◯ Half + 80 (205 Questions) (Extended Practice)             │
│  ◯ Half + 90 (215 Questions) (Extended Practice)             │
│  ◯ Half + 100 (225 Questions) (Extended Practice)            │
│  ◯ Half + 110 (235 Questions) (Extended Practice)            │
│  ◯ Half + 120 (245 Questions) (Extended Practice)            │
│  ◯ All (250 Questions) (Complete Mastery)                    │
└──────────────────────────────────────────────────────────────┘
					↓ SCROLLABLE ↓

┌─────────────────────┐  ┌─────────────────────┐
│   Total Questions   │  │   Estimated Time    │
│        10           │  │     3 minutes       │
└─────────────────────┘  └─────────────────────┘
```

**Note:** 26 total options — scrollable radio list

---

## Option Categorization

### Color/Icon Legend (Recommended for UI Enhancement)

```
⚡ Fixed Increments (10, 20, 30, ...)
   - Quick to moderate sessions
   - Fixed predictable counts
   - Color: Blue

📊 Half
   - Midpoint of available material
   - Balanced coverage
   - Color: Orange

📈 Half+ Increments (Half+10, Half+20, ...)
   - Extended practice sessions
   - Progressive mastery
   - Color: Purple

🏆 All
   - Complete mastery
   - All available terms
   - Color: Green
```

---

## User Flow Diagram

```
┌─────────────────┐
│  Load Study     │
│  Materials      │
│  (38 terms)     │
└────────┬────────┘
		 │
		 ▼
┌─────────────────┐
│  Calculate      │
│  HalfCount      │
│  (38 / 2 = 19)  │
└────────┬────────┘
		 │
		 ▼
┌─────────────────┐
│  Generate       │
│  Options:       │
│  - 10           │
│  - Half (19)    │
│  - Half+10 (29) │
│  - All (38)     │
└────────┬────────┘
		 │
		 ▼
┌─────────────────┐
│  Render Radio   │
│  Buttons        │
│  (Default: 10)  │
└────────┬────────┘
		 │
		 ▼
┌─────────────────┐
│  User Selects   │
│  "Half+10"      │
│  (29 questions) │
└────────┬────────┘
		 │
		 ▼
┌─────────────────┐
│  JavaScript     │
│  Updates Preview│
│  Count: 29      │
│  Time: 7 min    │
└────────┬────────┘
		 │
		 ▼
┌─────────────────┐
│  User Submits   │
│  Form           │
│  POST: 29       │
└────────┬────────┘
		 │
		 ▼
┌─────────────────┐
│  Controller     │
│  Validates      │
│  (29 >= 4) ✓    │
└────────┬────────┘
		 │
		 ▼
┌─────────────────┐
│  Service        │
│  Creates Session│
│  with 29 Qs     │
└────────┬────────┘
		 │
		 ▼
┌─────────────────┐
│  Redirect to    │
│  First Question │
└─────────────────┘
```

---

## Data Flow

### Request/Response Cycle

#### 1. GET /SuperQuiz/Start

**Controller:**
```csharp
var sections = await _markdownParserService.ParseMarkdownFilesAsync(username);
var totalTerms = sections.Sum(s => s.TermDefinitions.Count); // 38

var viewModel = new SuperQuizStartViewModel
{
	TotalAvailableTerms = 38,
	SelectedQuestionCount = 10 // Default
};
```

**View Model Processing:**
```csharp
HalfCount = Math.Max(38 / 2, 4) = 19

GetAvailableOptions():
  Loop: 10 (< 19) → Add "10 Questions"
  Loop: 20 (>= 19) → Stop
  Add: "Half (19 Questions)"
  Loop: 29 (< 38) → Add "Half + 10 (29 Questions)"
  Loop: 39 (>= 38) → Stop
  Add: "All (38 Questions)"

Returns: [10, 19, 29, 38]
```

**View Rendering:**
```html
<input name="questionCount" value="10" checked />
<input name="questionCount" value="19" />
<input name="questionCount" value="29" />
<input name="questionCount" value="38" />
```

#### 2. User Interaction

**JavaScript (super-quiz-start.js):**
```javascript
// User clicks "Half + 10 (29 Questions)" radio
const selectedRadio = document.querySelector('input[name="questionCount"]:checked');
const count = parseInt(selectedRadio.value, 10); // 29
const timeMinutes = 29 * 0.25; // 7.25 minutes

document.getElementById('preview-count').textContent = '29';
document.getElementById('preview-time').textContent = '7 minutes';
```

#### 3. POST /SuperQuiz/Start

**Form Submission:**
```
POST /SuperQuiz/Start
Content-Type: application/x-www-form-urlencoded

questionCount=29
__RequestVerificationToken=...
```

**Controller:**
```csharp
public async Task<IActionResult> Start([FromForm] int questionCount)
{
	// questionCount = 29

	if (questionCount < 4) // Validation
	{
		// Error
	}

	var sessionId = await _superQuizService.StartSuperQuizAsync(username, 29);

	return RedirectToAction("Question", new { sessionId });
}
```

**Service:**
```csharp
public async Task<string> StartSuperQuizAsync(string username, int questionCount)
{
	// questionCount = 29

	var sections = await _markdownParserService.ParseMarkdownFilesAsync(username);
	int totalTerms = sections.Sum(s => s.TermDefinitions.Count); // 38

	int targetQuestionCount = (questionCount == -1) ? totalTerms : questionCount; // 29

	// Generate 29 questions
	for (int i = 0; i < 29; i++)
	{
		var question = _questionGeneratorService.GenerateQuestion(sections);
		allQuestions.Add(question);
	}

	// Create session with 29 questions
	var session = new SuperQuizSession
	{
		AllQuestions = allQuestions // 29 questions
	};

	_memoryCache.Set(cacheKey, session);
	return session.SessionId;
}
```

---

## Edge Case Visualizations

### Edge Case 1: Exactly 20 Terms

```
HalfCount = 20 / 2 = 10

Options:
  Loop: 10 (< 10) → FALSE, skip loop entirely
  Add: "Half (10 Questions)"
  Loop: 20 (< 20) → FALSE, skip loop entirely
  Add: "All (20 Questions)"

Result:
┌──────────────────────────────────────────────┐
│  🔘 Half (10 Questions) (Balanced Coverage)  │
│  ◯ All (20 Questions) (Complete Mastery)     │
└──────────────────────────────────────────────┘
```

**Note:** No "10 Questions" option because the loop condition `count < halfCount` is false when `halfCount = 10`.

---

### Edge Case 2: Exactly 5 Terms

```
HalfCount = Math.Max(5 / 2, 4) = Math.Max(2, 4) = 4

Options:
  Loop: 10 (< 4) → FALSE, skip loop entirely
  Add: "Half (4 Questions)"
  Loop: 14 (< 5) → FALSE, skip loop entirely
  Add: "All (5 Questions)"

Result:
┌──────────────────────────────────────────────┐
│  🔘 Half (4 Questions) (Balanced Coverage)   │
│  ◯ All (5 Questions) (Complete Mastery)      │
└──────────────────────────────────────────────┘
```

**Note:** Minimum 4 terms enforced, so Half shows 4 even though 5/2 = 2.

---

## Comparison Table

| Dataset Size | Fixed Options | Half | Half+ Options | All | Total Options |
|--------------|---------------|------|---------------|-----|---------------|
| 10 terms     | -             | 5    | -             | 10  | 2             |
| 20 terms     | -             | 10   | -             | 20  | 2             |
| 38 terms     | 10            | 19   | 29            | 38  | 4             |
| 50 terms     | 10, 20        | 25   | 35, 45        | 50  | 6             |
| 100 terms    | 10-40 (4)     | 50   | 60-90 (4)     | 100 | 10            |
| 200 terms    | 10-90 (9)     | 100  | 110-190 (9)   | 200 | 20            |
| 250 terms    | 10-120 (12)   | 125  | 135-245 (12)  | 250 | 26            |

---

## Preview Animation

### When User Changes Selection

```
Before Selection Change:
┌─────────────────────┐
│   Total Questions   │
│        10           │  ← No animation
└─────────────────────┘

User clicks "Half (19)"
↓
During Animation (300ms):
┌─────────────────────┐
│   Total Questions   │
│        19           │  ← Pulsing/fading effect
└─────────────────────┘

After Animation:
┌─────────────────────┐
│   Total Questions   │
│        19           │  ← Stable display
└─────────────────────┘
```

**CSS Classes:**
- `.preview-updating` — Added during change
- Triggers CSS animation (pulse, scale, or fade)
- Removed after 300ms

---

## Accessibility Features

### Keyboard Navigation

```
Tab → Focuses first radio button (10 Questions)
↓ Arrow → Moves to next option (Half)
↑ Arrow → Moves to previous option (10 Questions)
Space/Enter → Selects focused option
Tab → Moves to "Start Super Quiz" button
```

### Screen Reader Announcements

```
"Radio button, 10 Questions, Quick Practice, checked, 1 of 4"
"Radio button, Half, 19 Questions, Balanced Coverage, not checked, 2 of 4"
"Radio button, Half plus 10, 29 Questions, Extended Practice, not checked, 3 of 4"
"Radio button, All, 38 Questions, Complete Mastery, not checked, 4 of 4"
```

---

## Conclusion

The dynamic option generation provides a flexible, scalable, and user-friendly interface for selecting Super Quiz question counts. The visual examples demonstrate how the system adapts to various dataset sizes while maintaining clarity and usability.

**Status:** ✅ Visual design verified  
**Next:** Manual UI testing with real study materials
