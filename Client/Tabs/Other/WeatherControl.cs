namespace FieldKit
{
    public sealed partial class Plugin
    {
        private static readonly string[] WeatherRainTypeLabels =
        {
            "No Rain",
            "Drizzling",
            "Rain",
            "Heavy Rain",
            "Shower",
            "Custom"
        };

        private void UpdateWeatherServerSync()
        {
            if (_weatherInitialRequestStarted ||
                _weatherRequestInFlight)
                return;

            _weatherInitialRequestStarted = true;
            RefreshWeatherFromServer();
        }

        private void DrawWeatherMenu()
        {
            _weatherMenuScroll = BeginVerticalScrollView(
                _weatherMenuScroll,
                GUILayout.Height(Mathf.Max(300f, MenuHeight - 105f)));
            BeginCategoryColumns();
            DrawWeatherTimeColumn();
            NextCategoryColumn();
            DrawWeatherSettingsColumn();
            EndCategoryColumns();
            EndVerticalScrollView();
        }

        private void DrawWeatherTimeColumn()
        {
            BeginCategoryPanel("Current Server", false);
            DrawCurrentWeatherSummary();
            EndCategoryPanel();
            GUILayout.Space(8f);
            BeginCategoryPanel("Time", false);
            bool timeEnabled = GUILayout.Toggle(
                _weatherTimeEnabled,
                " Force server time");
            if (timeEnabled != _weatherTimeEnabled)
            {
                _weatherTimeEnabled = timeEnabled;
            }
            DrawWeatherSlider(
                "Time of day (" +
                FormatWeatherHour(_weatherHourOfDay) + ")",
                ref _weatherHourOfDay,
                0f,
                23.999f,
                "0.00");
            DrawWeatherSlider(
                "Time speed",
                ref _weatherTimeScale,
                0f,
                8f,
                "0.00x");
            GUILayout.BeginHorizontal();
            GUI.enabled = !_weatherRequestInFlight;
            if (GUILayout.Button("Apply"))
                ApplyTimeToServer();
            if (GUILayout.Button("Reset"))
                ResetTimeOnServer();
            GUILayout.EndHorizontal();
            GUI.enabled = true;
            EndCategoryPanel();
        }

        private void DrawCurrentWeatherSummary()
        {
            if (_weatherHasServerState)
            {
                GUILayout.Label("Time: " +
                    (_serverWeatherTimeEnabled
                        ? FormatWeatherHour(_serverWeatherHourOfDay) + " @ " +
                          _serverWeatherTimeScale.ToString("0.00") + "x"
                        : "SPT generated"));
                GUILayout.Label("Weather: " +
                    (_serverWeatherEnabled ? "FORCED" : "SPT generated"));
                GUILayout.Label(
                    "Cloud " + _serverWeatherCloud.ToString("0.00") +
                    "  |  Fog " + _serverWeatherFog.ToString("0.0000") +
                    "  |  Rain " +
                    (_serverWeatherCustomRainIntensity
                        ? "Custom"
                        : RainTypeName(_serverWeatherRain)) +
                    "/" + _serverWeatherRainIntensity.ToString("0.00"));
                GUILayout.Label(
                    "Wind " + _serverWeatherWindSpeed.ToString("0.0") +
                    "  |  Gust " +
                    _serverWeatherWindGustiness.ToString("0.00") +
                    "  |  Temp " +
                    _serverWeatherTemperature.ToString("0.0") +
                    "  |  Pressure " +
                    _serverWeatherPressure.ToString("0"));
            }
            else
            {
                GUILayout.Label("No server state received yet.");
            }
            GUILayout.Label(
                _weatherRevision > 0
                    ? "Revision " + _weatherRevision
                    : "Not synchronized");
            GUI.enabled = !_weatherRequestInFlight;
            if (GUILayout.Button("Refresh from server"))
                RefreshWeatherFromServer();
            GUI.enabled = true;
            GUILayout.Label(_weatherServerStatus);
        }

        private void DrawWeatherSettingsColumn()
        {
            BeginCategoryPanel("Weather", false);

            bool enabled = GUILayout.Toggle(
                _weatherOverrideEnabled,
                " Force server weather");
            if (enabled != _weatherOverrideEnabled)
            {
                _weatherOverrideEnabled = enabled;
            }

            GUI.enabled = !_weatherRequestInFlight;
            DrawWeatherSlider("Cloud", ref _weatherCloud, -1f, 1.2f, "0.00");
            DrawWeatherSlider("Fog", ref _weatherFog, 0f, 0.02f, "0.0000");
            int rainType = Mathf.Clamp(
                _weatherCustomRainIntensity
                    ? WeatherRainTypeLabels.Length - 1
                    : Mathf.RoundToInt(_weatherRain) - 1,
                0,
                WeatherRainTypeLabels.Length - 1);
            int nextRainType = DrawDropdown(
                "server-weather-rain-type",
                rainType,
                WeatherRainTypeLabels,
                "Discrete rain mode used by EFT weather generation.");
            if (nextRainType != rainType)
            {
                _weatherCustomRainIntensity =
                    nextRainType == WeatherRainTypeLabels.Length - 1;
                if (!_weatherCustomRainIntensity)
                    _weatherRain = nextRainType + 1;
            }
            if (_weatherCustomRainIntensity)
                DrawWeatherSlider(
                    "Rain intensity",
                    ref _weatherRainIntensity,
                    0f,
                    1f,
                    "0.00");
            DrawWeatherSlider(
                "Wind speed",
                ref _weatherWindSpeed,
                0f,
                10f,
                "0.0");
            DrawWeatherSlider(
                "Wind gustiness",
                ref _weatherWindGustiness,
                0f,
                1f,
                "0.00");
            DrawWeatherSlider(
                "Temperature",
                ref _weatherTemperature,
                -50f,
                60f,
                "0.0");
            DrawWeatherSlider(
                "Pressure",
                ref _weatherPressure,
                700f,
                800f,
                "0");

            GUILayout.BeginHorizontal();
            GUI.enabled = !_weatherRequestInFlight;
            if (GUILayout.Button("Apply"))
                ApplyWeatherToServer();
            if (GUILayout.Button("Reset"))
                ResetWeatherOnServer();
            GUILayout.EndHorizontal();
            GUI.enabled = true;
            EndCategoryPanel();
        }

        private void DrawWeatherSlider(
            string label,
            ref float value,
            float minimum,
            float maximum,
            string format)
        {
            GUILayout.Label(label + ": " + value.ToString(format));
            float next = GUILayout.HorizontalSlider(
                value, minimum, maximum);
            if (!Mathf.Approximately(next, value))
            {
                value = next;
            }
        }

        private async void RefreshWeatherFromServer()
        {
            if (_weatherRequestInFlight)
                return;
            _weatherRequestInFlight = true;
            _weatherServerStatus = "Reading server weather...";
            try
            {
                string json = await RequestHandler.PostJsonAsync(
                    "/fieldkit/weather/get", "{}");
                ApplyWeatherServerResponse(json);
                LoadServerWeatherIntoControls();
                ApplyConfirmedEnvironmentToRaid();
                _weatherServerStatus = "Server weather synchronized.";
            }
            catch (Exception exception)
            {
                _weatherServerStatus =
                    "Weather server unavailable: " + exception.Message;
                LogSource.LogWarning(_weatherServerStatus);
            }
            finally
            {
                _weatherRequestInFlight = false;
            }
        }

        private async void ApplyWeatherToServer()
        {
            if (_weatherRequestInFlight)
                return;
            _weatherRequestInFlight = true;
            _weatherServerStatus = "Applying server weather...";
            try
            {
                string request = JsonConvert.SerializeObject(new
                {
                    Enabled = _weatherOverrideEnabled,
                    Cloud = _weatherCloud,
                    Fog = _weatherFog,
                    Rain = _weatherRain,
                    RainIntensity = _weatherRainIntensity,
                    RainCustom = _weatherCustomRainIntensity,
                    WindSpeed = _weatherWindSpeed,
                    WindGustiness = _weatherWindGustiness,
                    Temperature = _weatherTemperature,
                    Pressure = _weatherPressure
                });
                string json = await RequestHandler.PostJsonAsync(
                    "/fieldkit/weather/set", request);
                ApplyWeatherServerResponse(json);
                LoadServerWeatherControls();
                ApplyConfirmedEnvironmentToRaid();
                _weatherServerStatus = "Server accepted weather settings.";
            }
            catch (Exception exception)
            {
                _weatherServerStatus =
                    "Could not apply weather: " + exception.Message;
                LogSource.LogWarning(_weatherServerStatus);
            }
            finally
            {
                _weatherRequestInFlight = false;
            }
        }

        private async void ApplyTimeToServer()
        {
            if (_weatherRequestInFlight)
                return;
            _weatherRequestInFlight = true;
            _weatherServerStatus = "Applying server time...";
            try
            {
                string request = JsonConvert.SerializeObject(new
                {
                    TimeEnabled = _weatherTimeEnabled,
                    HourOfDay = _weatherHourOfDay,
                    TimeScale = _weatherTimeScale
                });
                string json = await RequestHandler.PostJsonAsync(
                    "/fieldkit/weather/set", request);
                ApplyWeatherServerResponse(json);
                LoadServerTimeControls();
                ApplyConfirmedEnvironmentToRaid();
                _weatherServerStatus = "Server accepted time settings.";
            }
            catch (Exception exception)
            {
                _weatherServerStatus =
                    "Could not apply time: " + exception.Message;
                LogSource.LogWarning(_weatherServerStatus);
            }
            finally
            {
                _weatherRequestInFlight = false;
            }
        }

        private async void ResetWeatherOnServer()
        {
            if (_weatherRequestInFlight)
                return;
            _weatherRequestInFlight = true;
            _weatherServerStatus = "Resetting server weather...";
            try
            {
                string json = await RequestHandler.PostJsonAsync(
                    "/fieldkit/weather/reset/weather", "{}");
                ApplyWeatherServerResponse(json);
                LoadServerWeatherControls();
                ApplyConfirmedEnvironmentToRaid();
                _weatherServerStatus = "Server weather reset to SPT generation.";
            }
            catch (Exception exception)
            {
                _weatherServerStatus =
                    "Could not reset weather: " + exception.Message;
                LogSource.LogWarning(_weatherServerStatus);
            }
            finally
            {
                _weatherRequestInFlight = false;
            }
        }

        private async void ResetTimeOnServer()
        {
            if (_weatherRequestInFlight)
                return;
            _weatherRequestInFlight = true;
            _weatherServerStatus = "Resetting server time...";
            try
            {
                string json = await RequestHandler.PostJsonAsync(
                    "/fieldkit/weather/reset/time", "{}");
                ApplyWeatherServerResponse(json);
                LoadServerTimeControls();
                ApplyConfirmedEnvironmentToRaid();
                _weatherServerStatus =
                    "Server time reset to SPT generation.";
            }
            catch (Exception exception)
            {
                _weatherServerStatus =
                    "Could not reset time: " + exception.Message;
                LogSource.LogWarning(_weatherServerStatus);
            }
            finally
            {
                _weatherRequestInFlight = false;
            }
        }

        private void ApplyWeatherServerResponse(string json)
        {
            JToken root = JToken.Parse(json);
            JToken data = root["data"] ?? root;
            _weatherRevision = ReadWeatherLong(data, "revision");
            _weatherHasServerState = true;
            _serverWeatherEnabled =
                ReadWeatherBool(data, "enabled");
            _serverWeatherTimeEnabled =
                ReadWeatherBool(data, "timeEnabled");
            _serverWeatherHourOfDay =
                ReadWeatherFloat(data, "hourOfDay");
            _serverWeatherTimeScale =
                ReadWeatherFloat(data, "timeScale");
            _serverWeatherCloud = ReadWeatherFloat(data, "cloud");
            _serverWeatherFog = ReadWeatherFloat(data, "fog");
            _serverWeatherRain = ReadWeatherFloat(data, "rain");
            _serverWeatherRainIntensity =
                ReadWeatherFloat(data, "rainIntensity");
            _serverWeatherCustomRainIntensity =
                ReadWeatherBool(data, "rainCustom");
            _serverWeatherWindSpeed =
                ReadWeatherFloat(data, "windSpeed");
            _serverWeatherWindGustiness =
                ReadWeatherFloat(data, "windGustiness");
            _serverWeatherTemperature =
                ReadWeatherFloat(data, "temperature");
            _serverWeatherPressure =
                ReadWeatherFloat(data, "pressure");
        }

        private void LoadServerWeatherIntoControls()
        {
            if (!_weatherHasServerState)
                return;
            LoadServerTimeControls();
            LoadServerWeatherControls();
        }

        private void LoadServerTimeControls()
        {
            _weatherTimeEnabled = _serverWeatherTimeEnabled;
            _weatherHourOfDay = _serverWeatherHourOfDay;
            _weatherTimeScale = _serverWeatherTimeScale;
        }

        private void LoadServerWeatherControls()
        {
            _weatherOverrideEnabled = _serverWeatherEnabled;
            _weatherCloud = _serverWeatherCloud;
            _weatherFog = _serverWeatherFog;
            _weatherRain = _serverWeatherRain;
            _weatherRainIntensity = _serverWeatherRainIntensity;
            _weatherCustomRainIntensity =
                _serverWeatherCustomRainIntensity;
            _weatherWindSpeed = _serverWeatherWindSpeed;
            _weatherWindGustiness = _serverWeatherWindGustiness;
            _weatherTemperature = _serverWeatherTemperature;
            _weatherPressure = _serverWeatherPressure;
        }

        private void ApplyConfirmedEnvironmentToRaid()
        {
            EFT.Weather.WeatherController controller =
                EFT.Weather.WeatherController.Instance;
            if (controller != null && controller.WeatherDebug != null)
            {
                EFT.Weather.WeatherDebug debug = controller.WeatherDebug;
                if (_serverWeatherEnabled)
                {
                    debug.CopyParams(controller.WeatherCurve);
                    debug.CloudDensity = _serverWeatherCloud;
                    debug.Fog = Mathf.Max(0.001f, _serverWeatherFog);
                    float rainType = Mathf.InverseLerp(
                        1f, 5f, _serverWeatherRain);
                    debug.Rain = _serverWeatherCustomRainIntensity
                        ? _serverWeatherRainIntensity
                        : (_serverWeatherRain <= 1f ? 0f : rainType);
                    debug.WindMagnitude = Mathf.Clamp01(
                        Mathf.InverseLerp(
                            0f, 5f, _serverWeatherWindSpeed));
                    debug.Temperature = _serverWeatherTemperature;
                    debug.Enabled = true;
                }
                else
                {
                    debug.Enabled = false;
                }
            }

            if (_world == null || _world.GameDateTime == null)
                return;

            GameDateTime gameDateTime = _world.GameDateTime;
            DateTime current = gameDateTime.Calculate();
            if (_serverWeatherTimeEnabled)
            {
                if (!_weatherLiveTimeOverrideApplied)
                {
                    _weatherOriginalLiveTimeScale =
                        gameDateTime.TimeFactor;
                    _weatherOriginalLiveGameTime = current;
                    _weatherOriginalLiveRealTime = DateTime.UtcNow;
                    _weatherLiveTimeOverrideApplied = true;
                }
                int hour = Mathf.FloorToInt(
                    _serverWeatherHourOfDay) % 24;
                int minute = Mathf.FloorToInt(
                    (_serverWeatherHourOfDay -
                     Mathf.Floor(_serverWeatherHourOfDay)) * 60f);
                DateTime desired = new DateTime(
                    current.Year,
                    current.Month,
                    current.Day,
                    hour,
                    minute,
                    0,
                    DateTimeKind.Utc);
                gameDateTime.Reset(
                    DateTime.UtcNow,
                    desired,
                    _serverWeatherTimeScale,
                    true);
            }
            else if (_weatherLiveTimeOverrideApplied)
            {
                DateTime restored = _weatherOriginalLiveGameTime.AddSeconds(
                    (DateTime.UtcNow - _weatherOriginalLiveRealTime)
                    .TotalSeconds * _weatherOriginalLiveTimeScale);
                gameDateTime.Reset(
                    DateTime.UtcNow,
                    restored,
                    _weatherOriginalLiveTimeScale,
                    true);
                _weatherLiveTimeOverrideApplied = false;
            }
        }

        private static string FormatWeatherHour(float hourOfDay)
        {
            int hour = Mathf.FloorToInt(hourOfDay) % 24;
            int minute = Mathf.FloorToInt(
                (hourOfDay - Mathf.Floor(hourOfDay)) * 60f);
            return hour.ToString("00") + ":" + minute.ToString("00");
        }

        private static string RainTypeName(float rawValue)
        {
            int index = Mathf.Clamp(
                Mathf.RoundToInt(rawValue) - 1,
                0,
                WeatherRainTypeLabels.Length - 1);
            return WeatherRainTypeLabels[index];
        }

        private static JToken WeatherProperty(
            JToken data,
            string camelName)
        {
            JToken value = data[camelName];
            if (value != null)
                return value;
            return data[char.ToUpperInvariant(camelName[0]) +
                        camelName.Substring(1)];
        }

        private static float ReadWeatherFloat(
            JToken data,
            string name) =>
            WeatherProperty(data, name)?.Value<float>() ?? 0f;

        private static bool ReadWeatherBool(
            JToken data,
            string name) =>
            WeatherProperty(data, name)?.Value<bool>() ?? false;

        private static long ReadWeatherLong(
            JToken data,
            string name) =>
            WeatherProperty(data, name)?.Value<long>() ?? 0L;
    }
}
