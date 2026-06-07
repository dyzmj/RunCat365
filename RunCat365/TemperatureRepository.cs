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

using RunCat365.Properties;
using System.Diagnostics;
using System.Globalization;

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

        internal static string GetDescription(this TemperatureInfo temperatureInfo, TemperatureUnit unit)
        {
            var resolvedUnit = unit.Resolve();
            return $"{Strings.SystemInfo_Temperature}: {temperatureInfo.MaximumCelsius.ToLocalizedTemperatureText(resolvedUnit)}";
        }

        internal static List<string> GenerateIndicator(this TemperatureInfo temperatureInfo, TemperatureUnit unit)
        {
            var resolvedUnit = unit.Resolve();
            return [
                TreeFormatter.CreateRoot($"{Strings.SystemInfo_Temperature}:"),
                TreeFormatter.CreateNode($"{Strings.SystemInfo_Average}: {temperatureInfo.AverageCelsius.ToLocalizedTemperatureText(resolvedUnit)}", false),
                TreeFormatter.CreateNode($"{Strings.SystemInfo_Maximum}: {temperatureInfo.MaximumCelsius.ToLocalizedTemperatureText(resolvedUnit)}", true)
            ];
        }

        private static string ToLocalizedTemperatureText(this float temperatureCelsius, TemperatureUnit resolvedUnit)
        {
            var useFahrenheit = resolvedUnit == TemperatureUnit.Fahrenheit;
            var value = useFahrenheit
                ? temperatureCelsius * CELSIUS_TO_FAHRENHEIT_SCALE + CELSIUS_TO_FAHRENHEIT_OFFSET
                : temperatureCelsius;
            var format = useFahrenheit
                ? Strings.SystemInfo_TemperatureFahrenheitFormat
                : Strings.SystemInfo_TemperatureCelsiusFormat;
            return string.Format(CultureInfo.CurrentCulture, format, value);
        }
    }

    internal class TemperaturePerformanceCounters
    {
        private const string CATEGORY_NAME = "Thermal Zone Information";
        private const string COUNTER_NAME = "Temperature";

        private readonly Dictionary<string, PerformanceCounter> countersByInstance = [];

        private TemperaturePerformanceCounters() { }

        internal int Count => countersByInstance.Count;

        internal static TemperaturePerformanceCounters? TryCreate()
        {
            try
            {
                _ = new PerformanceCounterCategory(CATEGORY_NAME);
            }
            catch
            {
                return null;
            }

            var instance = new TemperaturePerformanceCounters();
            instance.RefreshInstances();
            return instance.Count == 0 ? null : instance;
        }

        internal void RefreshInstances()
        {
            string[] currentInstanceNames;
            try
            {
                var category = new PerformanceCounterCategory(CATEGORY_NAME);
                currentInstanceNames = category.GetInstanceNames();
            }
            catch (Exception exception) when (
                exception is InvalidOperationException
                or System.ComponentModel.Win32Exception
                or UnauthorizedAccessException)
            {
                Debug.WriteLine($"TemperaturePerformanceCounters.RefreshInstances failed: {exception.Message}");
                return;
            }

            var currentSet = new HashSet<string>(currentInstanceNames, StringComparer.Ordinal);
            var staleInstances = countersByInstance.Keys
                .Where(name => !currentSet.Contains(name))
                .ToList();
            foreach (var instanceName in staleInstances)
            {
                countersByInstance[instanceName].Close();
                countersByInstance.Remove(instanceName);
            }

            foreach (var instanceName in currentInstanceNames)
            {
                if (countersByInstance.ContainsKey(instanceName)) continue;
                try
                {
                    var counter = new PerformanceCounter(CATEGORY_NAME, COUNTER_NAME, instanceName);
                    _ = counter.NextValue();
                    countersByInstance[instanceName] = counter;
                }
                catch (Exception exception) when (
                    exception is InvalidOperationException
                    or System.ComponentModel.Win32Exception
                    or UnauthorizedAccessException)
                {
                    Debug.WriteLine($"TemperaturePerformanceCounters: failed to create counter for {instanceName}: {exception.Message}");
                }
            }
        }

        internal List<float> ReadValues()
        {
            var values = new List<float>(countersByInstance.Count);
            var deadInstances = new List<string>();
            foreach (var pair in countersByInstance)
            {
                try
                {
                    values.Add(pair.Value.NextValue());
                }
                catch (Exception exception) when (
                    exception is InvalidOperationException
                    or System.ComponentModel.Win32Exception
                    or UnauthorizedAccessException)
                {
                    Debug.WriteLine($"TemperaturePerformanceCounters: counter {pair.Key} failed: {exception.Message}");
                    deadInstances.Add(pair.Key);
                }
            }
            foreach (var instanceName in deadInstances)
            {
                countersByInstance[instanceName].Close();
                countersByInstance.Remove(instanceName);
            }
            return values;
        }

        internal void Close()
        {
            foreach (var counter in countersByInstance.Values) counter.Close();
            countersByInstance.Clear();
        }
    }

    internal class TemperatureRepository
    {
        private const float KELVIN_TO_CELSIUS_OFFSET = 273.15f;
        private const float MIN_VALID_TEMPERATURE_CELSIUS = -50.0f;
        private const float MAX_VALID_TEMPERATURE_CELSIUS = 150.0f;
        private const int REFRESH_INTERVAL_TICKS = 30;

        private readonly TemperaturePerformanceCounters? counters;
        private TemperatureInfo? temperatureInfo;
        private int ticksSinceLastRefresh;

        internal bool IsAvailable => counters is not null;

        internal TemperatureRepository()
        {
            counters = TemperaturePerformanceCounters.TryCreate();
        }

        internal void Update()
        {
            if (counters is null) return;

            ticksSinceLastRefresh += 1;
            if (REFRESH_INTERVAL_TICKS <= ticksSinceLastRefresh)
            {
                ticksSinceLastRefresh = 0;
                counters.RefreshInstances();
            }

            var rawValues = counters.ReadValues();
            var temperaturesCelsius = new List<float>(rawValues.Count);
            foreach (var temperatureKelvin in rawValues)
            {
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
