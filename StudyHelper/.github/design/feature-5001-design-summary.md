# Design Summary: User Custom Study Materials Upload (Feature #5001)

**Feature**: User Custom Study Materials Upload  
**Azure DevOps**: https://dev.azure.com/SchneiderDowns/Jeff/_workitems/edit/5001  
**Branch**: `feature/user-custom-study-materials`  
**Date**: 2026-05-27  
**Architect**: Solutions Architect

---

## Executive Summary

This feature enables users to upload custom `TermsAndDefinitions.md` and `Equations.md` files to personalize their study experience. The system provides secure per-user storage, validates uploads for security and format, and seamlessly integrates custom content with existing flashcard, quiz, and exercise generators while maintaining default content fallback.

---

## User Stories (Feature #5001)

| ID | Title | Priority |
|---|---|---|
| 5002 | Upload custom TermsAndDefinitions.md file | High |
| 5003 | Upload custom Equations.md file | High |
| 5004 | Display default study materials from InputDocuments project | High |
| 5005 | Store uploaded files securely per user account | Critical |
| 5006 | Manage uploaded study materials (view, update, delete) | Medium |
| 5007 | Validate uploaded markdown files for format and security | Critical |

---

## Architecture Decisions

### 1. Storage Strategy
**Decision**: File-based storage with JSON metadata  
**Rationale**:
- Consistent with existing `UserService` file-based approach
- No database schema changes required
- Simple deployment and rollback
- Suitable for expected file volumes

**Location**: `App_Data/StudyMaterials/{username}/`

### 2. Security Approach
**Decision**: Encrypt custom files at rest using existing `IEncryptionService`  
**Rationale**:
- Consistent with existing user data encryption
- Protects user content
- No additional dependencies

### 3. Parser Integration
**Decision**: Inject `IUserStudyMaterialService` into parser services  
**Rationale**:
- Minimal disruption to existing parsers
- Clean separation of concerns
- Easy fallback to defaults
- Supports per-user content resolution

### 4. UI Pattern
**Decision**: Dedicated management page, not inline with settings  
**Rationale**:
- File upload workflows need space
- Clear separation from appearance/theme settings
- Room for future enhancements (preview, versioning)

---

## Component Overview

### New Components

**Models**:
- `UserStudyMaterial` - File metadata
- `StudyMaterialType` - Enum (Terms, Equations)
- `FileValidationResult` - Validation outcome

**Services**:
- `IUserStudyMaterialService` / `UserStudyMaterialService` - File CRUD operations
- `IFileValidationService` / `FileValidationService` - Upload validation

**Controllers**:
- `StudyMaterialsController` - Upload, manage, delete, download templates

**Views**:
- `Views/StudyMaterials/Manage.cshtml` - Main UI

**Assets**:
- `wwwroot/js/study-materials.js` - Client-side validation and UX
- `wwwroot/css/study-materials.css` - Page styling

### Modified Components

**Services** (require constructor changes):
- `TermDefinitionParserService` - Add user file resolution
- `EquationParserService` - Add user file resolution

**Navigation**:
- `Views/Shared/_Layout.cshtml` - Add "Study Materials" link

---

## Data Flow

### Upload Flow
```
User → StudyMaterialsController.UploadTerms()
  → FileValidationService.ValidateMarkdownFileAsync()
  → FileValidationService.ValidateTermsFormatAsync()
  → FileValidationService.ScanForMaliciousContentAsync()
  → EncryptionService.Encrypt()
  → File.WriteAllBytesAsync()
  → UserStudyMaterialService.SaveMetadataAsync()
  → Cache invalidation
  → Redirect with success message
```

### Content Retrieval Flow
```
FlashcardController.Card()
  → TermDefinitionParserService.ParseTermDefinitionsAsync()
	→ UserStudyMaterialService.GetEffectiveFilePathAsync(username, Terms)
	  → Check custom file exists?
		→ Yes: Decrypt and return custom path
		→ No: Return default path from InputDocuments
	→ Parse from resolved path
```

---

## Security Measures

| Risk | Mitigation |
|------|-----------|
| Malicious file upload | Content scanning, format validation, file size limits |
| Path traversal | Username sanitization, use `Path.Combine`, no direct user input |
| Cross-user access | Authorization on all endpoints, username from authenticated identity |
| Data at rest | Encrypt all custom files using `IEncryptionService` |
| XSS in markdown | Scan for script tags and dangerous patterns |
| DoS via large files | 10 MB file size limit enforced client and server side |

---

## Validation Rules

### File Validation
- ✅ Extension must be `.md`
- ✅ File size <= 10 MB
- ✅ File must be readable text
- ✅ No malicious patterns (script tags, eval, etc.)

### Format Validation (Terms)
- ⚠️ Warning if no `**Term** - Definition` patterns found
- ✅ Must have valid markdown structure

### Format Validation (Equations)
- ⚠️ Warning if no LaTeX markers (`$$` or `\[`) found
- ✅ Must have valid markdown structure

---

## UI Design Highlights

### Manage Study Materials Page
- **Card-based layout**: Separate cards for Terms and Equations
- **Status indicators**: Clear visual distinction between "Using Default" and "Using Custom"
- **One-click actions**: Upload, Replace, Delete with confirmation
- **Template downloads**: Sample files to guide users
- **Responsive**: Works on mobile, tablet, desktop
- **Theme-aware**: Uses CSS variables, compatible with all 17 themes

### User Flows
1. **Upload**: Choose file → Upload → Success message
2. **Replace**: Click Replace → File picker → Auto-submit → Success
3. **Delete**: Click Delete → Confirmation modal → Delete → Revert to default
4. **Download template**: Click template link → Download sample file

---

## Configuration

### appsettings.json
```json
{
  "StudyMaterials": {
	"MaxFileSizeBytes": 10485760,
	"StorageFolder": "StudyMaterials",
	"EnableEncryption": true
  }
}
```

---

## Testing Strategy

### Unit Tests (Backend)
- `UserStudyMaterialService`: All CRUD operations
- `FileValidationService`: All validation methods
- Path resolution logic
- Encryption/decryption integration

### Integration Tests
- Full upload workflow end-to-end
- Parser integration with custom files
- Fallback to defaults when custom missing
- Authorization enforcement

### UI Tests
- File upload form submission
- Client-side validation
- Success/error message display
- Modal interactions
- Responsive layout

### Security Tests
- Path traversal attempts
- Malicious content injection
- Cross-user access attempts
- File size limit bypass attempts

**Coverage Target**: >= 80% for all new code

---

## Implementation Phases

### Phase 1: Backend Foundation (User Stories 5005, 5007)
- [ ] Create models: `UserStudyMaterial`, `StudyMaterialType`, `FileValidationResult`
- [ ] Implement `FileValidationService`
- [ ] Implement `UserStudyMaterialService` (upload, delete, retrieve)
- [ ] Unit tests for services

### Phase 2: Backend Integration (User Stories 5002, 5003, 5004)
- [ ] Create `StudyMaterialsController`
- [ ] Modify `TermDefinitionParserService` for user resolution
- [ ] Modify `EquationParserService` for user resolution
- [ ] Integration tests for parser integration

### Phase 3: Frontend (User Story 5006)
- [ ] Create `ManageStudyMaterialsViewModel`
- [ ] Create `Views/StudyMaterials/Manage.cshtml`
- [ ] Create `study-materials.js` and `study-materials.css`
- [ ] Update `_Layout.cshtml` navigation
- [ ] Template download endpoints

### Phase 4: Testing & Polish
- [ ] Security testing
- [ ] Accessibility testing
- [ ] Responsive design testing
- [ ] Theme compatibility testing
- [ ] Performance testing (caching)

---

## Dependencies

### No New NuGet Packages Required
All functionality uses existing .NET 10 libraries and dependencies already in the project.

### Service Registration (Program.cs)
```csharp
builder.Services.AddScoped<IUserStudyMaterialService, UserStudyMaterialService>();
builder.Services.AddScoped<IFileValidationService, FileValidationService>();
```

---

## Performance Considerations

### Caching Strategy
- Cache decrypted custom files in `IMemoryCache`
- Cache key: `{MaterialType}_{Username}`
- Expiration: 1 hour or on update/delete
- Reduces decryption overhead for frequent access

### File I/O
- All operations use async/await
- Stream large files rather than loading into memory
- Limit concurrent uploads (future: rate limiting middleware)

---

## Rollback Plan

If critical issues arise:

1. **Remove DI registrations** for new services in `Program.cs`
2. **Remove navigation link** from `_Layout.cshtml`
3. **Revert parser service constructors** to original signatures
4. **Delete user materials folder** if desired (data cleanup)
5. **Existing users unaffected** - continue using default content

No database migrations to revert.

---

## Deployment Checklist

- [ ] Create `App_Data/StudyMaterials` folder
- [ ] Verify write permissions for application pool identity
- [ ] Update `appsettings.json` with configuration
- [ ] Register new services in `Program.cs`
- [ ] Deploy frontend assets (CSS, JS)
- [ ] Run smoke tests (upload, delete, retrieve)
- [ ] Verify logging and error handling
- [ ] Monitor storage usage

---

## Risks & Mitigations

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|-----------|
| Storage growth over time | Medium | Medium | Monitor usage; implement cleanup policy for inactive users |
| Encryption performance | Low | Medium | Aggressive caching; benchmark decrypt operations |
| Complex parser integration | Low | High | Thorough integration tests; fallback mechanisms |
| Malicious file upload | Medium | High | Multi-layer validation; content scanning; logging |

---

## Open Questions for Product Owner

1. **Versioning**: Should we support versioning of uploaded files? (Recommend: No for MVP)
2. **Templates**: Should sample templates be customizable by admin? (Recommend: Static for MVP)
3. **Rate Limiting**: What is acceptable upload frequency? (Recommend: 5 uploads/hour/user)
4. **Storage Limits**: Should we limit total storage per user? (Recommend: 50 MB total for MVP)
5. **Sharing**: Future feature to share materials between users? (Recommend: Out of scope)

---

## Success Metrics

### Functional
- ✅ Users can upload custom terms and equations
- ✅ Files validated and stored securely
- ✅ Custom content used in flashcards/quizzes/exercises
- ✅ Default content fallback works seamlessly

### Quality
- ✅ >= 80% test coverage
- ✅ Zero critical security vulnerabilities
- ✅ All accessibility requirements met
- ✅ Works on all supported browsers and devices

### Performance
- ✅ Upload completes in < 5 seconds for 10 MB file
- ✅ Cached content retrieval < 50ms
- ✅ No memory leaks from file operations

---

## Documentation Deliverables

### Design Documents
- ✅ Backend Design: `.github/design/feature-5001-backend-design.md`
- ✅ Frontend Design: `.github/design/feature-5001-frontend-design.md`
- ✅ Design Summary: `.github/design/feature-5001-design-summary.md` (this document)

### Implementation Documents (TBD)
- Frontend Implementation Report
- Backend Implementation Report
- Code Review Report
- Security Review Report
- QA Validation Summary
- Technical Documentation (user-facing)

---

## Next Steps

1. **Human Review**: Product owner reviews all design documents
2. **Approval Checkpoint**: Explicit approval required before implementation
3. **Handoff to Engineers**:
   - Backend Engineer: Implement services and controller
   - Frontend Engineer: Implement UI and JavaScript
4. **Code Review**: Review Specialist + Security Specialist
5. **QA Testing**: QA Engineer validates all user stories
6. **Technical Writing**: Technical Writer documents feature
7. **Human Final Review**: Final approval before PR/merge

---

**Design Status**: ✅ Ready for Human Review  
**Documents**: Backend Design, Frontend Design, Design Summary  
**Awaiting**: Human approval to proceed to implementation phase
