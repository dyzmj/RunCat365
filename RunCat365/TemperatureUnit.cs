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
using System.Globalization;

namespace RunCat365
{
    enum TemperatureUnit
    {
        System,
        Celsius,
        Fahrenheit,
    }

    internal static class TemperatureUnitExtension
    {
        private static readonly TemperatureUnit systemDefault = DetectSystemDefault();

        internal static string GetLocalizedString(this TemperatureUnit unit)
        {
            return unit switch
            {
                TemperatureUnit.System => Strings.TemperatureUnit_System,
                TemperatureUnit.Celsius => Strings.TemperatureUnit_Celsius,
                TemperatureUnit.Fahrenheit => Strings.TemperatureUnit_Fahrenheit,
                _ => "",
            };
        }

        internal static TemperatureUnit Resolve(this TemperatureUnit unit)
        {
            return unit == TemperatureUnit.System ? systemDefault : unit;
        }

        private static TemperatureUnit DetectSystemDefault()
        {
            try
            {
                return new RegionInfo(CultureInfo.CurrentCulture.Name).IsMetric
                    ? TemperatureUnit.Celsius
                    : TemperatureUnit.Fahrenheit;
            }
            catch (ArgumentException)
            {
                return TemperatureUnit.Celsius;
            }
        }
    }
}
