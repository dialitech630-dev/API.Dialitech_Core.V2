using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace API.Dialitech.IntegrationTest;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MongoDbSettings:ConnectionString"] = "mongodb+srv://dialitech630_db_user:Dialitech123456789@cluster0.bjioual.mongodb.net/DialitechDB?retryWrites=true&w=majority&appName=Cluster0",
                ["MongoDbSettings:DatabaseName"] = "DialitechDB_Test",
                ["JwtSettings:SecretKey"] = "test-key-32-characters-minimum-replace-me",
                ["JwtSettings:Issuer"] = "API.Dialitech.Test",
                ["JwtSettings:Audience"] = "API.Dialitech.Test",
                ["JwtSettings:ExpirationInMinutes"] = "60"
            });
        });
    }
}
