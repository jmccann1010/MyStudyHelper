# Generate-EncryptionKey.ps1
# Generates a secure 256-bit (32-byte) encryption key for the authentication system

Write-Host "Generating encryption key..." -ForegroundColor Cyan
Write-Host ""

$key = [Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(32))

Write-Host "Generated Encryption Key:" -ForegroundColor Green
Write-Host $key -ForegroundColor Yellow
Write-Host ""

Write-Host "To use this key:" -ForegroundColor Cyan
Write-Host "1. Copy the key above" -ForegroundColor White
Write-Host "2. For development: Update appsettings.json -> Authentication:EncryptionKey" -ForegroundColor White
Write-Host "3. For production: Store in User Secrets or Azure Key Vault" -ForegroundColor White
Write-Host ""

Write-Host "User Secrets command:" -ForegroundColor Cyan
Write-Host "dotnet user-secrets set ""Authentication:EncryptionKey"" ""$key""" -ForegroundColor White
Write-Host ""

Write-Host "Environment Variable command:" -ForegroundColor Cyan
Write-Host "`$env:Authentication__EncryptionKey = ""$key""" -ForegroundColor White
Write-Host ""

Write-Host "WARNING: Never commit the encryption key to source control!" -ForegroundColor Red
