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
        internal string Name { get; set; }
        internal float Celsius { get; set; }
    }

    internal static class TemperatureInfoExtension
    {
        internal static string GetDescription(this List<TemperatureInfo> temperatureInfoList)
        {
            if (temperatureInfoList.Count == 0) return "";

            var highestTemperatureCelsius = temperatureInfoList.Max(x => x.Celsius);
            return $"{Strings.SystemInfo_Temperature}: {highestTemperatureCelsius.ToLocalizedTemperatureText()}";
        }

        internal static List<string> GenerateIndicator(this List<TemperatureInfo> temperatureInfoList)
        {
            var resultLines = new List<string>
            {
                TreeFormatter.CreateRoot($"{Strings.SystemInfo_ThermalZone}:")
            };

            for (int i = 0; i < temperatureInfoList.Count; i++)
            {
                var temperatureInfo = temperatureInfoList[i];
                var isLastItem = (i == temperatureInfoList.Count - 1);
                resultLines.Add(TreeFormatter.CreateNode($"{temperatureInfo.Name}: {temperatureInfo.Celsius.ToLocalizedTemperatureText()}", isLastItem));
            }

            return resultLines;
        }

        private static string ToLocalizedTemperatureText(this float temperatureCelsius)
        {
            var usesFahrenheit = UsesFahrenheit();
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
        private readonly List<PerformanceCounter> temperatureCounters = [];
        private readonly List<TemperatureInfo> temperatureInfoList = [];

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
                temperatureInfoList.Clear();
                foreach (var counter in temperatureCounters)
                {
                    var temperatureKelvin = counter.NextValue();
                    if (temperatureKelvin <= 0) continue;
                    temperatureInfoList.Add(new TemperatureInfo
                    {
                        Name = counter.InstanceName,
                        Celsius = temperatureKelvin - 273.15f
                    });
                }
            }
            catch
            {
                IsAvailable = false;
            }
        }

        internal List<TemperatureInfo> Get()
        {
            if (!IsAvailable || temperatureInfoList.Count == 0) return [];
            return [.. temperatureInfoList];
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
