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

namespace RunCat365
{
    enum Theme
    {
        System,
        Light,
        Dark,
    }

    internal static class ThemeExtension
    {
        internal static string GetLocalizedString(this Theme theme)
        {
            return theme switch
            {
                Theme.System => Strings.Theme_System,
                Theme.Light => Strings.Theme_Light,
                Theme.Dark => Strings.Theme_Dark,
                _ => "",
            };
        }

        internal static Color GetContrastColor(this Theme theme)
        {
            return theme switch
            {
                Theme.Dark => Color.White,
                Theme.Light => Color.Black,
                _ => Color.Gray,
            };
        }
    }
}
