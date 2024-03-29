using blog_API.Migrations;
using blog_API.Models;
using bcrypt = BCrypt.Net.BCrypt;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;


namespace blog_API.Repositories;


public class AuthService : IAuthService
{
    private static PostDbContext? _context;
    private static IConfiguration? _config;

    public AuthService(PostDbContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
    }


    public User SignUp(User user)
    {
        // TODO: Hash Password
        var passwordHash = bcrypt.HashPassword(user.Password);
        user.Password = passwordHash;

        _context!.Add(user);
        _context.SaveChanges();
        return user;
    }

    private string BuildToken(User user)
    {
        var secret = _config.GetValue<String>("TokenSecret");
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));

        // Create Signature using secret signing key
        var signingCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        // Create claims to add to JWT payload
        var claims = new Claim[]
        {
        new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
        new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName ?? "")
        };

        // Create token
        var jwt = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.Now.AddMinutes(60),
            signingCredentials: signingCredentials);

        // Encode token
        var encodedJwt = new JwtSecurityTokenHandler().WriteToken(jwt);


        return encodedJwt;
    }

    //modified SignIn from tutorial:
    // //https://jasonwatmore.com/post/2021/12/14/net-6-jwt-authentication-tutorial-with-example-api#user-cs
    public SignInResponse SignIn(SignInRequest request)
    {

        var user = _context!.Users!.SingleOrDefault(x => x.UserName == request.UserName);
        var verified = false;

        if (user != null)
        {
            verified = bcrypt.Verify(request.Password, user.Password);
        }

        if (user == null || !verified)
        {
            return null!;
        }

        // Create & return JWT
        var token = BuildToken(user)!;

        return new SignInResponse(user, token);
    }

}