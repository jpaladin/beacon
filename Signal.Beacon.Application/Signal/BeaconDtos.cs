using System;

namespace Signal.Beacon.Application.Signal
{
    public record SignalBeaconRegisterRequestDto(string BeaconId);

    public record SignalBeaconRefreshTokenRequestDto(string RefreshToken);

    public record SignalBeaconRefreshTokenResponseDto(string AccessToken, DateTime Expire);
}