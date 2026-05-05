// Copyright 2025 Takuto Nakamura
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//
//        http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.

using System.Diagnostics;
using System.Globalization;
using RunCat365.Properties;

namespace RunCat365
{
    struct TemperatureInfo
    {
        internal float AverageCelsius { get; set; }
        internal float MaximumCelsius { get; set; }
    }

    internal static class TemperatureInfoExtension
    {
        private static readonly bool usesFahrenheit = UsesFahrenheit();

        internal static string GetDescription(this TemperatureInfo temperatureInfo)
        {
            return $"{Strings.SystemInfo_Temperature}: {temperatureInfo.MaximumCelsius.ToLocalizedTemperatureText()}";
        }

        internal static List<string> GenerateIndicator(this TemperatureInfo temperatureInfo)
        {
            return [
                TreeFormatter.CreateRoot($"{Strings.SystemInfo_Temperature}:"),
                TreeFormatter.CreateNode($"{Strings.SystemInfo_Average}: {temperatureInfo.AverageCelsius.ToLocalizedTemperatureText()}", false),
                TreeFormatter.CreateNode($"{Strings.SystemInfo_Maximum}: {temperatureInfo.MaximumCelsius.ToLocalizedTemperatureText()}", true)
            ];
        }

        private static string ToLocalizedTemperatureText(this float temperatureCelsius)
        {
            var value = usesFahrenheit ? temperatureCelsius * 9.0f / 5.0f + 32.0f : temperatureCelsius;
            var format = usesFahrenheit
                ? Strings.SystemInfo_TemperatureFahrenheitFormat
                : Strings.SystemInfo_TemperatureCelsiusFormat;
            return string.Format(CultureInfo.CurrentCulture, format, value);
        }

        private static bool UsesFahrenheit()
        {
            try
            {
                return !new RegionInfo(CultureInfo.CurrentCulture.Name).IsMetric;
            }
            catch
            {
                return false;
            }
        }
    }

    internal class TemperatureRepository
    {
        private const float MIN_VALID_TEMPERATURE_CELSIUS = 0.0f;
        private const float MAX_VALID_TEMPERATURE_CELSIUS = 150.0f;
        private readonly List<PerformanceCounter> temperatureCounters = [];
        private TemperatureInfo? temperatureInfo;

        internal bool IsAvailable { get; private set; } = true;

        internal TemperatureRepository()
        {
            try
            {
                var category = new PerformanceCounterCategory("Thermal Zone Information");
                var instanceNames = category.GetInstanceNames();
                if (instanceNames.Length > 0)
                {
                    foreach (var instance in instanceNames)
                    {
                        var counter = new PerformanceCounter("Thermal Zone Information", "Temperature", instance);
                        temperatureCounters.Add(counter);

                        // Discards first return value
                        _ = counter.NextValue();
                    }
                }
                else
                {
                    IsAvailable = false;
                }
            }
            catch
            {
                IsAvailable = false;
            }
        }

        internal void Update()
        {
            if (!IsAvailable || temperatureCounters.Count == 0) return;
            try
            {
                var temperaturesCelsius = new List<float>();
                foreach (var counter in temperatureCounters)
                {
                    var temperatureKelvin = counter.NextValue();
                    if (temperatureKelvin <= 0) continue;
                    var temperatureCelsius = temperatureKelvin - 273.15f;
                    if (temperatureCelsius is < MIN_VALID_TEMPERATURE_CELSIUS or > MAX_VALID_TEMPERATURE_CELSIUS) continue;
                    temperaturesCelsius.Add(temperatureCelsius);
                }

                temperatureInfo = temperaturesCelsius.Count == 0
                    ? null
                    : new TemperatureInfo
                    {
                        AverageCelsius = temperaturesCelsius.Average(),
                        MaximumCelsius = temperaturesCelsius.Max()
                    };
            }
            catch
            {
                IsAvailable = false;
            }
        }

        internal TemperatureInfo? Get()
        {
            if (!IsAvailable || !temperatureInfo.HasValue) return null;
            return temperatureInfo.Value;
        }

        internal void Close()
        {
            foreach (var counter in temperatureCounters)
            {
                counter.Close();
            }
        }
    }
}
