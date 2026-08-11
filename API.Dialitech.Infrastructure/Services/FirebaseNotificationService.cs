using API.Dialitech.Application.Interfaces;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;

namespace API.Dialitech.Infrastructure.Services;

public class FirebaseNotificationService : INotificationService
{
    private static FirebaseApp? _app;
    private static readonly object Lock = new();

    public static void Initialize(string credentialsJson)
    {
        if (_app is not null)
            return;

        lock (Lock)
        {
            if (_app is null)
            {
                _app = FirebaseApp.Create(new AppOptions
                {
                    Credential = GoogleCredential.FromJson(credentialsJson)
                });
            }
        }
    }

    public async Task SendHealthAlertAsync(string deviceToken, string title, string body)
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
}

public class NoopNotificationService : INotificationService
{
    public Task SendHealthAlertAsync(string deviceToken, string title, string body)
        => Task.CompletedTask;
}