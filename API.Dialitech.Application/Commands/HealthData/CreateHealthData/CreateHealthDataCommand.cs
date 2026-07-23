using API.Dialitech.Application.DTOs;
using MediatR;

namespace API.Dialitech.Application.Commands.HealthData.CreateHealthData;

public record CreateHealthDataCommand(
    string UserId,
    int HeartRate,
    double SpO2,
    int ActivityLevel,
    DateTime Timestamp) : IRequest<HealthDataDto>;
