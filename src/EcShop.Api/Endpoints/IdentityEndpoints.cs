using EcShop.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace EcShop.Api.Endpoints;

public static class IdentityEndpoints
{
    private static readonly PasswordHasher<ShopUser> PasswordHasher = new();
    public static IEndpointRouteBuilder MapIdentityEndpoints(this IEndpointRouteBuilder routes)
    {
        var api = routes.MapGroup("/api/v1");
        api.MapPost("/auth/register", RegisterAsync); api.MapPost("/auth/login", LoginAsync); api.MapPost("/auth/logout", LogoutAsync); api.MapGet("/me", MeAsync); api.MapPatch("/me", UpdateMeAsync); api.MapGet("/me/addresses", AddressesAsync); api.MapPost("/me/addresses", AddAddressAsync); api.MapPatch("/me/addresses/{id:long}", UpdateAddressAsync); return routes;
    }
    private static async Task<IResult> RegisterAsync(RegisterRequest request, EcShopDbContext db, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Email) || request.Password?.Length < 12 || !request.AgreementAccepted) return Bad("username, email, a 12-character password, and agreement_accepted are required");
        if (await db.Users.AnyAsync(x => x.UserName == request.Username || x.Email == request.Email, ct)) return Results.Conflict(new { code = "conflict", message = "username or email already exists" });
        var user = new ShopUser { UserName = request.Username.Trim(), Email = request.Email.Trim() }; user.PasswordHash = PasswordHasher.HashPassword(user, request.Password!); db.Users.Add(user); await db.SaveChangesAsync(ct); return Results.Created($"/api/v1/me", new { id = user.UserId, username = user.UserName, access_token = await CreateSessionAsync(user.UserId, db, ct) });
    }
    private static async Task<IResult> LoginAsync(LoginRequest request, EcShopDbContext db, CancellationToken ct)
    {
        var user = await db.Users.SingleOrDefaultAsync(x => x.UserName == request.Username, ct); if (user is null || PasswordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password ?? string.Empty) == PasswordVerificationResult.Failed) return Results.Unauthorized();
        return Results.Ok(new { id = user.UserId, username = user.UserName, access_token = await CreateSessionAsync(user.UserId, db, ct) });
    }
    private static async Task<IResult> LogoutAsync(HttpRequest request, EcShopDbContext db, CancellationToken ct) { var token = Token(request); if (token is not null) { var s = await db.Sessions.FindAsync([Hash(token)], ct); if (s is not null) { db.Sessions.Remove(s); await db.SaveChangesAsync(ct); } } return Results.NoContent(); }
    private static async Task<IResult> MeAsync(HttpRequest request, EcShopDbContext db, CancellationToken ct) { var u = await CurrentUserAsync(request, db, ct); return u is null ? Results.Unauthorized() : Results.Ok(new { id = u.UserId, username = u.UserName, email = u.Email }); }
    private static async Task<IResult> UpdateMeAsync(UpdateProfileRequest request, HttpRequest http, EcShopDbContext db, CancellationToken ct) { var u = await CurrentUserAsync(http, db, ct); if (u is null) return Results.Unauthorized(); if (!string.IsNullOrWhiteSpace(request.Email) && request.Email != u.Email) { if (await db.Users.AnyAsync(x => x.Email == request.Email && x.UserId != u.UserId, ct)) return Results.Conflict(); u.Email = request.Email; await db.SaveChangesAsync(ct); } return Results.Ok(new { id = u.UserId, username = u.UserName, email = u.Email }); }
    private static async Task<IResult> AddressesAsync(HttpRequest http, EcShopDbContext db, CancellationToken ct) { var u = await CurrentUserAsync(http, db, ct); if (u is null) return Results.Unauthorized(); return Results.Ok((await db.Addresses.Where(x => x.UserId == u.UserId).OrderByDescending(x => x.IsDefault).ThenBy(x => x.AddressId).ToListAsync(ct)).Select(Address)); }
    private static async Task<IResult> AddAddressAsync(AddressRequest r, HttpRequest http, EcShopDbContext db, CancellationToken ct) { var u = await CurrentUserAsync(http, db, ct); if (u is null) return Results.Unauthorized(); if (string.IsNullOrWhiteSpace(r.Consignee) || string.IsNullOrWhiteSpace(r.Address)) return Bad("consignee and address are required"); if (r.IsDefault) await db.Addresses.Where(x => x.UserId == u.UserId && x.IsDefault).ExecuteUpdateAsync(x => x.SetProperty(y => y.IsDefault, false), ct); var a = new UserAddress { UserId = u.UserId, Consignee = r.Consignee, Address = r.Address, Mobile = r.Mobile ?? "", Zipcode = r.Zipcode ?? "", CountryId = r.CountryId, ProvinceId = r.ProvinceId, CityId = r.CityId, DistrictId = r.DistrictId, IsDefault = r.IsDefault }; db.Addresses.Add(a); await db.SaveChangesAsync(ct); return Results.Created($"/api/v1/me/addresses/{a.AddressId}", Address(a)); }
    private static async Task<IResult> UpdateAddressAsync(long id, AddressRequest r, HttpRequest http, EcShopDbContext db, CancellationToken ct) { var u = await CurrentUserAsync(http, db, ct); if (u is null) return Results.Unauthorized(); var a = await db.Addresses.SingleOrDefaultAsync(x => x.AddressId == id && x.UserId == u.UserId, ct); if (a is null) return Results.NotFound(); a.Consignee = r.Consignee ?? a.Consignee; a.Address = r.Address ?? a.Address; a.Mobile = r.Mobile ?? a.Mobile; a.Zipcode = r.Zipcode ?? a.Zipcode; if (r.IsDefault) { await db.Addresses.Where(x => x.UserId == u.UserId && x.AddressId != id).ExecuteUpdateAsync(x => x.SetProperty(y => y.IsDefault, false), ct); a.IsDefault = true; } await db.SaveChangesAsync(ct); return Results.Ok(Address(a)); }
    public static async Task<ShopUser?> CurrentUserAsync(HttpRequest request, EcShopDbContext db, CancellationToken ct) { var token = Token(request); return token is null ? null : await (from s in db.Sessions join u in db.Users on s.UserId equals u.UserId where s.TokenHash == Hash(token) select u).SingleOrDefaultAsync(ct); }
    private static async Task<string> CreateSessionAsync(long userId, EcShopDbContext db, CancellationToken ct) { var raw = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)); db.Sessions.Add(new UserSession { TokenHash = Hash(raw), UserId = userId }); await db.SaveChangesAsync(ct); return raw; }
    private static string? Token(HttpRequest request) { var value = request.Headers.Authorization.ToString(); return value.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? value[7..].Trim() : null; }
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    private static object Address(UserAddress a) => new { id = a.AddressId, consignee = a.Consignee, country_id = a.CountryId, province_id = a.ProvinceId, city_id = a.CityId, district_id = a.DistrictId, address = a.Address, mobile = a.Mobile, zipcode = a.Zipcode, is_default = a.IsDefault };
    private static IResult Bad(string d) => Results.Problem(statusCode: 400, title: "validation_error", detail: d);
}
public sealed record RegisterRequest(string? Username, string? Email, string? Password, bool AgreementAccepted); public sealed record LoginRequest(string? Username, string? Password); public sealed record UpdateProfileRequest(string? Email); public sealed record AddressRequest(string? Consignee, long CountryId, long ProvinceId, long CityId, long DistrictId, string? Address, string? Mobile, string? Zipcode, bool IsDefault);
