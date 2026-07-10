# Study Materials Encryption Removal

## Summary
Removed encryption/decryption from study materials storage and retrieval. Study materials (Terms & Definitions, Equations) are now stored as plain text files instead of encrypted files.

## Changes Made

### 1. UserStudyMaterialService.cs
- **Removed** `IEncryptionService` dependency from constructor and fields
- **Modified** `UploadMaterialAsync`: Changed from `File.WriteAllBytesAsync(encryptedContent)` to `File.WriteAllTextAsync(content)`
- **Modified** `GetDecryptedContentAsync`: Changed from reading encrypted bytes and decrypting to directly reading plain text with `File.ReadAllTextAsync()`
- Updated log messages to reflect plain text operations instead of encryption/decryption

### 2. Models/UserStudyMaterial.cs
- **Renamed** property from `EncryptedFilePath` to `FilePath` to accurately reflect that files are no longer encrypted

### 3. Services/IUserStudyMaterialService.cs
- Updated documentation for `GetDecryptedContentAsync` method to indicate it returns content (not decrypted content)

## Rationale

Study materials are not sensitive personal information and do not require encryption:
- **Performance**: Eliminates encryption/decryption overhead on every file access
- **Simplicity**: Reduces complexity and removes unnecessary dependency on encryption service
- **Accessibility**: Files can be directly read for debugging or inspection if needed
- **Appropriate Security**: User authentication still protects materials - only the logged-in user can access their own files

## Security Notes

- User credentials and authentication data (in `users.dat`) remain encrypted with AES-256-GCM
- Study materials are still protected by user authentication - access requires login
- Files are stored in user-specific directories: `App_Data/StudyMaterials/{username}/`
- Only the authenticated user can upload, view, or delete their own materials

## File Storage

Study materials are now stored as plain text in:
```
StudyHelper/
└── App_Data/
	└── StudyMaterials/
		└── {username}/
			├── TermsAndDefinitions.md  (plain text)
			├── Equations.md            (plain text)
			└── metadata.json           (plain text)
```

## Testing Recommendations

1. **Upload Test**: Upload new study materials and verify they're saved as plain text
2. **Read Test**: Read uploaded materials and verify no decryption errors occur
3. **Cache Test**: Verify caching still works correctly for performance
4. **Delete Test**: Ensure deletion removes plain text files properly
5. **Migration Test**: If existing encrypted files exist, consider a migration script (optional)

## Migration Consideration

If production already has encrypted study materials from users, you may want to create a one-time migration script to:
1. Read existing encrypted files
2. Decrypt them using the encryption service
3. Write them back as plain text
4. Update metadata file paths

This was not implemented as it depends on whether encrypted files currently exist in production.
