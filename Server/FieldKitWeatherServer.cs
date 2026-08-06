using HarmonyLib;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Controllers;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Weather;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Spt.Weather;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Routers;
using SPTarkov.Server.Core.Utils;

namespace FieldKit.Server;

[Injectable(InjectionType.Singleton)]
public sealed class FieldKitWeatherService
{
    private readonly object _sync = new();
    private FieldKitWeatherState _state = new();
    private FieldKitWeatherState _original = new();
    private DateTime _originalCapturedUtc = DateTime.UtcNow;
    private static FieldKitWeatherService? _instance;
    private static bool _patched;

    public FieldKitWeatherService()
    {
        _instance = this;
        if (_patched)
            return;

        new Harmony("com.hysocs.fieldkit.server.weather")
            .PatchAll(typeof(FieldKitWeatherService).Assembly);
        _patched = true;
    }

    public FieldKitWeatherState Get()
    {
        lock (_sync)
            return _state with { };
    }

    public FieldKitWeatherState Set(FieldKitWeatherSetRequest request)
    {
        lock (_sync)
        {
            _state = _state with
            {
                Revision = _state.Revision + 1,
                Enabled = request.Enabled ?? _state.Enabled,
                TimeEnabled = request.TimeEnabled ?? _state.TimeEnabled,
                HourOfDay = Clamp(
                    request.HourOfDay,
                    _state.HourOfDay,
                    0d,
                    23.999d),
                TimeScale = Clamp(
                    request.TimeScale,
                    _state.TimeScale,
                    0d,
                    8d),
                Cloud = Clamp(request.Cloud, _state.Cloud, -1d, 1.2d),
                Fog = Clamp(request.Fog, _state.Fog, 0d, 0.1d),
                Rain = Clamp(request.Rain, _state.Rain, 1d, 5d),
                RainIntensity = Clamp(
                    request.RainIntensity,
                    _state.RainIntensity,
                    0d,
                    1d),
                RainCustom = request.RainCustom ?? _state.RainCustom,
                WindSpeed = Clamp(
                    request.WindSpeed,
                    _state.WindSpeed,
                    0d,
                    10d),
                WindGustiness = Clamp(
                    request.WindGustiness,
                    _state.WindGustiness,
                    0d,
                    1d),
                Temperature = Clamp(
                    request.Temperature,
                    _state.Temperature,
                    -50d,
                    60d),
                Pressure = Clamp(
                    request.Pressure,
                    _state.Pressure,
                    700d,
                    800d)
            };
            return _state with { };
        }
    }

    public FieldKitWeatherState Reset()
    {
        lock (_sync)
        {
            _state = new FieldKitWeatherState
            {
                Revision = _state.Revision + 1
            };
            return _state with { };
        }
    }

    public FieldKitWeatherState ResetTime()
    {
        lock (_sync)
        {
            _state = _state with
            {
                Revision = _state.Revision + 1,
                TimeEnabled = false,
                HourOfDay = CalculateOriginalHour(),
                TimeScale = _original.TimeScale
            };
            return _state with { };
        }
    }

    public FieldKitWeatherState ResetWeather()
    {
        lock (_sync)
        {
            _state = _state with
            {
                Revision = _state.Revision + 1,
                Enabled = false,
                Cloud = _original.Cloud,
                Fog = _original.Fog,
                Rain = _original.Rain,
                RainIntensity = _original.RainIntensity,
                RainCustom = false,
                WindSpeed = _original.WindSpeed,
                WindGustiness = _original.WindGustiness,
                Temperature = _original.Temperature,
                Pressure = _original.Pressure
            };
            return _state with { };
        }
    }

    private void CaptureOriginal(Weather weather, double? acceleration)
    {
        lock (_sync)
        {
            double hour = ParseHour(weather.Time, _original.HourOfDay);
            _original = _original with
            {
                HourOfDay = hour,
                TimeScale = acceleration ?? _original.TimeScale,
                Cloud = weather.Cloud ?? _original.Cloud,
                Fog = weather.Fog ?? _original.Fog,
                Rain = weather.Rain ?? _original.Rain,
                RainIntensity =
                    weather.RainIntensity ?? _original.RainIntensity,
                RainCustom = false,
                WindSpeed = weather.WindSpeed ?? _original.WindSpeed,
                WindGustiness =
                    weather.WindGustiness ?? _original.WindGustiness,
                Temperature =
                    weather.Temperature ?? _original.Temperature,
                Pressure = weather.Pressure ?? _original.Pressure
            };
            _originalCapturedUtc = DateTime.UtcNow;
        }
    }

    private double CalculateOriginalHour()
    {
        double elapsedGameHours =
            (DateTime.UtcNow - _originalCapturedUtc).TotalSeconds *
            _original.TimeScale / 3600d;
        double hour = (_original.HourOfDay + elapsedGameHours) % 24d;
        return hour < 0d ? hour + 24d : hour;
    }

    private static double ParseHour(string? value, double fallback)
    {
        if (string.IsNullOrWhiteSpace(value) ||
            !DateTime.TryParse(value, out DateTime parsed))
            return fallback;
        return parsed.Hour + parsed.Minute / 60d + parsed.Second / 3600d;
    }

    public static void ApplyOverride(Weather? weather)
    {
        FieldKitWeatherService? service = _instance;
        if (service is null || weather is null)
            return;

        service.CaptureOriginal(weather, null);
        FieldKitWeatherState state = service.Get();
        ApplyState(weather, state);
    }

    private static void ApplyState(
        Weather weather,
        FieldKitWeatherState state)
    {
        if (state.Enabled)
        {
            weather.Cloud = state.Cloud;
            weather.Fog = state.Fog;
            weather.Rain = state.RainCustom ? 3d : state.Rain;
            weather.RainIntensity = state.RainIntensity;
            weather.WindSpeed = state.WindSpeed;
            weather.WindGustiness = state.WindGustiness;
            weather.Temperature = state.Temperature;
            weather.Pressure = state.Pressure;
        }
        ApplyTimeOverride(weather, state);
    }

    public static void ApplyOverride(WeatherData? weatherData)
    {
        FieldKitWeatherService? service = _instance;
        if (service is null || weatherData is null)
            return;
        if (weatherData.Weather is null)
            return;
        service.CaptureOriginal(
            weatherData.Weather,
            Convert.ToDouble(weatherData.Acceleration));
        FieldKitWeatherState state = service.Get();
        ApplyState(weatherData.Weather, state);
        if (!state.TimeEnabled)
            return;
        weatherData.Acceleration = state.TimeScale;
        weatherData.Time = FormatTime(state.HourOfDay);
    }

    private static void ApplyTimeOverride(
        Weather weather,
        FieldKitWeatherState state)
    {
        if (!state.TimeEnabled)
            return;
        string time = FormatTime(state.HourOfDay);
        string date = weather.Date ?? DateTime.UtcNow.ToString("yyyy-MM-dd");
        weather.Time = date + " " + time;
    }

    private static string FormatTime(double hourOfDay)
    {
        int hours = (int)Math.Floor(hourOfDay) % 24;
        int minutes = (int)Math.Floor(
            (hourOfDay - Math.Floor(hourOfDay)) * 60d);
        return $"{hours:00}:{minutes:00}:00";
    }

    private static double Clamp(
        double? requested,
        double current,
        double minimum,
        double maximum) =>
        requested.HasValue
            ? Math.Clamp(requested.Value, minimum, maximum)
            : current;
}

[Injectable]
public sealed class FieldKitWeatherRouter : StaticRouter
{
    public FieldKitWeatherRouter(
        JsonUtil jsonUtil,
        FieldKitWeatherCallback callback,
        FieldKitWeatherService weatherService)
        : base(
            jsonUtil,
            [
                new RouteAction<EmptyRequestData>(
                    "/fieldkit/weather/get",
                    async (url, info, sessionId, output, cancellationToken) =>
                        await callback.Get()),
                new RouteAction<FieldKitWeatherSetRequest>(
                    "/fieldkit/weather/set",
                    async (url, info, sessionId, output, cancellationToken) =>
                        await callback.Set(info)),
                new RouteAction<EmptyRequestData>(
                    "/fieldkit/weather/reset",
                    async (url, info, sessionId, output, cancellationToken) =>
                        await callback.Reset()),
                new RouteAction<EmptyRequestData>(
                    "/fieldkit/weather/reset/time",
                    async (url, info, sessionId, output, cancellationToken) =>
                        await callback.ResetTime()),
                new RouteAction<EmptyRequestData>(
                    "/fieldkit/weather/reset/weather",
                    async (url, info, sessionId, output, cancellationToken) =>
                        await callback.ResetWeather())
            ])
    {
        _ = weatherService;
    }
}

[Injectable]
public sealed class FieldKitWeatherCallback(
    FieldKitWeatherService weatherService,
    HttpResponseUtil httpResponseUtil)
{
    public ValueTask<string> Get() =>
        ValueTask.FromResult(
            httpResponseUtil.NoBody(weatherService.Get()));

    public ValueTask<string> Set(FieldKitWeatherSetRequest request) =>
        ValueTask.FromResult(
            httpResponseUtil.NoBody(weatherService.Set(request)));

    public ValueTask<string> Reset() =>
        ValueTask.FromResult(
            httpResponseUtil.NoBody(weatherService.Reset()));

    public ValueTask<string> ResetTime() =>
        ValueTask.FromResult(
            httpResponseUtil.NoBody(weatherService.ResetTime()));

    public ValueTask<string> ResetWeather() =>
        ValueTask.FromResult(
            httpResponseUtil.NoBody(weatherService.ResetWeather()));
}

public sealed record FieldKitWeatherSetRequest : IRequestData
{
    public bool? Enabled { get; init; }
    public bool? TimeEnabled { get; init; }
    public double? HourOfDay { get; init; }
    public double? TimeScale { get; init; }
    public double? Cloud { get; init; }
    public double? Fog { get; init; }
    public double? Rain { get; init; }
    public double? RainIntensity { get; init; }
    public bool? RainCustom { get; init; }
    public double? WindSpeed { get; init; }
    public double? WindGustiness { get; init; }
    public double? Temperature { get; init; }
    public double? Pressure { get; init; }
}

public sealed record FieldKitWeatherState
{
    public long Revision { get; init; }
    public bool Enabled { get; init; }
    public bool TimeEnabled { get; init; }
    public double HourOfDay { get; init; } = 12d;
    public double TimeScale { get; init; } = 1d;
    public double Cloud { get; init; } = -1d;
    public double Fog { get; init; } = 0.0013d;
    public double Rain { get; init; } = 1d;
    public double RainIntensity { get; init; }
    public bool RainCustom { get; init; }
    public double WindSpeed { get; init; }
    public double WindGustiness { get; init; }
    public double Temperature { get; init; } = 20d;
    public double Pressure { get; init; } = 770d;
}

[HarmonyPatch(typeof(WeatherController), nameof(WeatherController.Generate))]
internal static class FieldKitCurrentWeatherPatch
{
    private static void Postfix(WeatherData __result) =>
        FieldKitWeatherService.ApplyOverride(__result);
}

[HarmonyPatch(
    typeof(WeatherController),
    nameof(WeatherController.GenerateLocal))]
internal static class FieldKitLocalWeatherPatch
{
    private static void Postfix(GetLocalWeatherResponseData __result)
    {
        if (__result?.Weather is null)
            return;

        foreach (Weather weather in __result.Weather)
            FieldKitWeatherService.ApplyOverride(weather);
    }
}
