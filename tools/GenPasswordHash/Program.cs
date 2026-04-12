using Microsoft.AspNetCore.Identity;

// 与 Evolver 中 AddIdentityCore<AppUser> 默认 PasswordHasher 一致（PBKDF2-HMAC-SHA256）
var hasher = new PasswordHasher<IdentityUser>();
var hash = hasher.HashPassword(new IdentityUser(), "admin123");
Console.WriteLine(hash);
