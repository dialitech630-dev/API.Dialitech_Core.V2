using API.Dialitech.Application.Interfaces;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Logging;

namespace API.Dialitech.Infrastructure.Services;

public class FirebaseNotificationService : INotificationService
{
    private static FirebaseApp? _app;
    private static readonly object Lock = new();
    private readonly ILogger<FirebaseNotificationService> _logger;

    public FirebaseNotificationService(ILogger<FirebaseNotificationService> logger)
    {
        _logger = logger;
    }

    public static bool TryInitialize(string credentialsJson)
    {
        if (_app is not null)
            return true;

        lock (Lock)
        {
            if (_app is null)
            {
                try
                {
                    var credential = CredentialFactory
                        .FromJson<ServiceAccountCredential>(credentialsJson)
                        .ToGoogleCredential();

                    _app = FirebaseApp.Create(new AppOptions
                    {
                        Credential = credential
                    });

                    return true;
                }
                catch (ArgumentException)
                {
                    return false;
                }
                catch (InvalidOperationException)
                {
                    return false;
                }
            }
        }

        return true;
    }

    public async Task SendHealthAlertAsync(string deviceToken, string title, string body)
    {
        try
        {
            var message = new Message
            {
                Token = deviceToken,
                Notification = new Notification
                {
                    Title = title,
                    Body = body
                }
            };

            await FirebaseMessaging.DefaultInstance.SendAsync(message);
        }
        catch (FirebaseMessagingException ex)
        {
            _logger.LogWarning(ex, "FCM delivery failed for token {DeviceToken}", deviceToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "FCM network error for token {DeviceToken}", deviceToken);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "FCM send timed out for token {DeviceToken}", deviceToken);
        }
    }
}

public class NoopNotificationService : INotificationService
{
    public Task SendHealthAlertAsync(string deviceToken, string title, string body)
        => Task.CompletedTask;
}