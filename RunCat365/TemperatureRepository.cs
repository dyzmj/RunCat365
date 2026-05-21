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
        private const float CELSIUS_TO_FAHRENHEIT_SCALE = 9.0f / 5.0f;
        private const float CELSIUS_TO_FAHRENHEIT_OFFSET = 32.0f;

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
            var value = usesFahrenheit
                ? temperatureCelsius * CELSIUS_TO_FAHRENHEIT_SCALE + CELSIUS_TO_FAHRENHEIT_OFFSET
                : temperatureCelsius;
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
            catch (ArgumentException)
            {
                return false;
            }
        }
    }

    internal class TemperaturePerformanceCounters
    {
        private const string CATEGORY_NAME = "Thermal Zone Information";
        private const string COUNTER_NAME = "Temperature";

        internal IReadOnlyList<PerformanceCounter> Counters { get; }

        private TemperaturePerformanceCounters(List<PerformanceCounter> counters)
        {
            Counters = counters;
        }

        internal static TemperaturePerformanceCounters? TryCreate()
        {
            var counters = new List<PerformanceCounter>();
            try
            {
                var category = new PerformanceCounterCategory(CATEGORY_NAME);
                var instanceNames = category.GetInstanceNames();
                if (instanceNames.Length == 0) return null;

                foreach (var instance in instanceNames)
                {
                    var counter = new PerformanceCounter(CATEGORY_NAME, COUNTER_NAME, instance);
                    counters.Add(counter);
                    _ = counter.NextValue();
                }
                return new TemperaturePerformanceCounters(counters);
            }
            catch
            {
                foreach (var counter in counters) counter.Close();
                return null;
            }
        }

        internal void Close()
        {
            foreach (var counter in Counters) counter.Close();
        }
    }

    internal class TemperatureRepository
    {
        private const float KELVIN_TO_CELSIUS_OFFSET = 273.15f;
        private const float MIN_VALID_TEMPERATURE_CELSIUS = -50.0f;
        private const float MAX_VALID_TEMPERATURE_CELSIUS = 150.0f;

        private readonly TemperaturePerformanceCounters? counters;
        private TemperatureInfo? temperatureInfo;

        internal bool IsAvailable => counters is not null;

        internal TemperatureRepository()
        {
            counters = TemperaturePerformanceCounters.TryCreate();
        }

        internal void Update()
        {
            if (counters is null) return;
            try
            {
                var temperaturesCelsius = new List<float>();
                foreach (var counter in counters.Counters)
                {
                    var temperatureKelvin = counter.NextValue();
                    if (temperatureKelvin <= 0) continue;
                    var temperatureCelsius = temperatureKelvin - KELVIN_TO_CELSIUS_OFFSET;
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
            catch (InvalidOperationException)
            {
                temperatureInfo = null;
            }
        }

        internal TemperatureInfo? Get()
        {
            return temperatureInfo;
        }

        internal void Close()
        {
            counters?.Close();
        }
    }
}
