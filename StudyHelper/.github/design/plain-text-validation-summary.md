# Plain Text Validation for Study Materials

**Date**: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
**Feature**: ASCII/Plain Text Validation for Uploaded Study Materials
**Status**: ✅ Implemented and Build Verified

## Summary

Added validation to ensure that uploaded study materials (TermsAndDefinitions.md and Equations.md) contain only plain text/ASCII characters. This prevents encoding issues and ensures compatibility with the markdown parsers.

## Changes Made

### 1. Interface Update - `IFileValidationService.cs`

Added new method signature:
```csharp
/// <summary>
/// Validate that content contains only plain text/ASCII characters.
/// </summary>
Task<FileValidationResult> ValidatePlainTextAsync(string content);
```

### 2. Implementation - `FileValidationService.cs`

**New Method**: `ValidatePlainTextAsync(string content)`

#### Validation Rules
- **Allowed Characters**:
  - ASCII printable characters: 32-126 (space through tilde)
  - Tab character: ASCII 9
  - Line feed: ASCII 10 (newline)
  - Carriage return: ASCII 13

- **Rejected Characters**:
  - Control characters (ASCII 0-31, except tab/LF/CR)
  - Extended ASCII (128-255)
  - Unicode characters (256+)
  - Any non-text binary data

#### Error Reporting
- Provides line number and character position for each invalid character
- Groups errors by character type
- Logs character code (ASCII value) for debugging
- User-friendly error message at the top summarizing the issue

#### Example Error Output
```
File contains 2 type(s) of non-ASCII/non-text characters. Please save as plain text (ASCII) encoding.
Invalid non-ASCII character 'é' (code 233) found at line 5, position 12
Invalid control character (ASCII 3) found at line 10, position 1
```

### 3. Service Integration - `UserStudyMaterialService.cs`

**Upload Flow Updated**:
1. Validate file extension and readability
2. Read file content
3. **NEW**: Validate plain text/ASCII encoding ← Added here
4. Validate content format (Terms or Equations structure)
5. Scan for malicious patterns
6. Encrypt and save file
7. Update metadata
8. Invalidate cache

The plain text validation runs before format validation to catch encoding issues early.

### 4. Validation Sequence

```
Upload File
	↓
Basic File Validation (extension, size, readability)
	↓
Read Content
	↓
Plain Text Validation ← NEW
	↓
Format Validation (Terms/Equations structure)
	↓
Security Scan (malicious patterns)
	↓
Encrypt & Save
```

## Technical Details

### Character Validation Logic

```csharp
// Allow:
- Tab (ASCII 9)
- Newline (ASCII 10, handled separately)
- Carriage Return (ASCII 13)
- Printable ASCII (32-126): space through tilde

// Reject everything else:
- Binary data
- Control characters
- Extended ASCII
- Unicode/UTF-8 characters beyond ASCII range
```

### Why ASCII-Only?

1. **Parser Compatibility**: Markdown parsers expect ASCII text
2. **Cross-Platform**: ASCII is universally supported
3. **No Encoding Issues**: Eliminates UTF-8/UTF-16/ANSI confusion
4. **Security**: Prevents hidden Unicode characters or encoding exploits
5. **Simplicity**: Clear, unambiguous text representation

### Acceptable Content Examples

✅ **Valid Plain Text**:
```markdown
# Terms and Definitions

**Accounting**: The process of recording financial transactions.
**GAAP**: Generally Accepted Accounting Principles.

Equation: Assets = Liabilities + Equity
```

❌ **Invalid (Non-ASCII)**:
```markdown
# Términos y Definiciones  ← Contains 'é'

**Café**: A place for coffee  ← Contains 'é'
**Résumé**: A document  ← Contains 'é' and 'é'

Equation: π = 3.14159  ← Contains Greek letter π
```

## User Experience

### Upload Success Flow
1. User uploads `.md` file
2. File passes all validations
3. Success message displayed
4. File encrypted and stored
5. Ready for use in flashcards/quizzes/exercises

### Upload Failure Flow (Non-ASCII)
1. User uploads `.md` file with non-ASCII characters
2. Plain text validation fails
3. **Error message shows**:
   - Main message: "File contains non-ASCII characters"
   - Specific locations: "Line 5, position 12"
   - Character details: "Invalid character 'é' (code 233)"
4. User sees TempData error alert
5. User must re-save file as plain ASCII text

### How Users Can Fix Issues

#### In Notepad (Windows)
1. Open the `.md` file
2. Click **File → Save As**
3. Change **Encoding** dropdown to **ANSI** or **UTF-8**
4. Click **Save**

#### In Notepad++ (Recommended)
1. Open the `.md` file
2. Click **Encoding → Convert to ANSI**
3. Click **File → Save**

#### In VS Code
1. Open the `.md` file
2. Click encoding in bottom-right corner
3. Select **Save with Encoding**
4. Choose **Western (Windows 1252)** or **UTF-8**
5. Replace non-ASCII characters manually

#### General Approach
- Replace accented characters: `café` → `cafe`
- Replace special symbols: `π` → `pi` or `3.14159`
- Remove smart quotes: `""` → `""`
- Remove em-dashes: `—` → `-` or `--`

## Testing Recommendations

### Manual Testing

#### Test Case 1: Valid ASCII File
```markdown
# Test Terms
**Term1**: Definition1
**Term2**: Definition2
```
**Expected**: Upload succeeds

#### Test Case 2: UTF-8 with Accents
```markdown
**Café**: A coffee shop
**Résumé**: A document
```
**Expected**: Upload fails with specific line/position errors

#### Test Case 3: Unicode Characters
```markdown
**Pi**: π = 3.14159
**Sigma**: Σ (sum)
```
**Expected**: Upload fails with character code errors

#### Test Case 4: Smart Quotes
```markdown
**Quote**: "This is a smart quote"
```
**Expected**: Upload fails (smart quotes are non-ASCII)

#### Test Case 5: Tab and Newline
```markdown
**Term**:	Definition with tab
Another line

With blank line above
```
**Expected**: Upload succeeds (tabs and newlines allowed)

#### Test Case 6: Control Characters
```
File with embedded Ctrl+C or other control characters
```
**Expected**: Upload fails with control character error

### Automated Testing Needed

- [ ] Unit test for `ValidatePlainTextAsync` with valid ASCII
- [ ] Unit test for UTF-8 characters (should fail)
- [ ] Unit test for control characters (should fail)
- [ ] Unit test for Unicode emoji (should fail)
- [ ] Unit test for tab/newline (should pass)
- [ ] Integration test for full upload flow with non-ASCII rejection

## Error Messages

### Controller Level (StudyMaterialsController)
When upload returns `false`, the controller sets:
```csharp
TempData["ErrorMessage"] = "Upload failed. Please ensure the file is a valid markdown file in plain text format.";
```

### Service Level Logging
- `LogWarning`: Lists all detected invalid characters
- `LogDebug`: Confirms when plain text validation passes

### User-Facing Errors (via TempData)
Current generic message. Could be enhanced to show validation details:
```
"Upload failed. File contains non-ASCII characters at line 5. 
Please save as plain text (ASCII) encoding and try again."
```

## Future Enhancements

### Possible Improvements
1. **Detailed Error Display**: Show validation errors in UI instead of generic message
2. **Auto-Conversion**: Attempt to convert common Unicode to ASCII equivalents
3. **Character Preview**: Show problematic characters in upload UI
4. **Encoding Detection**: Detect file encoding and suggest correct one
5. **Client-Side Check**: Add JavaScript validation before upload

### Configuration Options
Could add app setting for strictness:
```json
{
  "StudyMaterials": {
	"EncodingValidation": "Strict",  // "Strict", "Lenient", "None"
	"AllowExtendedAscii": false,
	"AllowBasicUnicode": false
  }
}
```

## Build Status

✅ **Build Successful**
- No compilation errors
- All validations integrated correctly
- Existing functionality preserved

## Known Limitations

1. **No Auto-Fix**: Users must manually fix encoding issues
2. **Error Message Indirection**: Validation errors don't flow directly to UI
3. **No Preview**: Users can't see problematic characters before upload
4. **Strict ASCII**: Even common extended ASCII (e.g., ©, ®) is rejected

## Rollback Procedure

If issues arise:
1. Remove call to `ValidatePlainTextAsync` in `UserStudyMaterialService.cs`
2. Remove method from `FileValidationService.cs`
3. Remove method signature from `IFileValidationService.cs`
4. Rebuild

---

**Implementation Status**: ✅ Complete  
**Build Status**: ✅ Verified  
**Testing Status**: ⚠️ Manual testing recommended
