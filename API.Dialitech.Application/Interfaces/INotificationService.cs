namespace API.Dialitech.Application.Interfaces;

public interface INotificationService
{
    Task SendHealthAlertAsync(string deviceToken, string title, string body);
}