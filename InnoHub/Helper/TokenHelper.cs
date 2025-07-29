using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

public class TokenHelper
{
    private readonly string _jwtSecret; // The secret used to sign the JWT (from your app settings)

    public TokenHelper(string jwtSecret)
    {
        _jwtSecret = jwtSecret;
    }

    public ClaimsPrincipal GetPrincipalFromToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = System.Text.Encoding.ASCII.GetBytes(_jwtSecret);

        try
        {
            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,  // Ensure token is valid and not expired
                IssuerSigningKey = new SymmetricSecurityKey(key)
            }, out SecurityToken validatedToken);

            return principal;
        }
        catch
        {
            return null;  // Token validation failed
        }
    }
   


    public string GetEmailFromToken(string token)
    {
        var principal = GetPrincipalFromToken(token);
        return principal?.FindFirst(ClaimTypes.Email)?.Value; // Extract email from claims
    }

    public string GetUserIdFromToken(string token)
    {
        var principal = GetPrincipalFromToken(token);
        return principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value; // Extract userId (name identifier) from claims
    }
}
