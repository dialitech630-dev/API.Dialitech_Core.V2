using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace API.Dialitech.SecurityTest;

public class SecurityWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("MongoDbSettings:ConnectionString", "mongodb+srv://dialitech630_db_user:Dialitech123456789@cluster0.bjioual.mongodb.net/DialitechDB?retryWrites=true&w=majority&appName=Cluster0");
        builder.UseSetting("MongoDbSettings:DatabaseName", "DialitechDB_Test_Security");
        builder.UseSetting("JwtSettings:SecretKey", "test-key-32-characters-minimum-replace-me");
        builder.UseSetting("JwtSettings:Issuer", "API.Dialitech.Test");
        builder.UseSetting("JwtSettings:Audience", "API.Dialitech.Test");
        builder.UseSetting("JwtSettings:ExpirationInMinutes", "60");
    }
}
