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

namespace RunCat365
{
    struct GPUInfo
    {
        internal float Average { get; set; }
        internal float Maximum { get; set; }
    }

    internal static class GPUInfoExtension
    {
        internal static string GetDescription(this GPUInfo gpuInfo)
        {
            return $"{Strings.SystemInfo_GPU}: {gpuInfo.Maximum:f1}%";
        }

        internal static List<string> GenerateIndicator(this GPUInfo gpuInfo)
        {
            var resultLines = new List<string>
            {
                TreeFormatter.CreateRoot($"{Strings.SystemInfo_GPU}:"),
                TreeFormatter.CreateNode($"{Strings.SystemInfo_Average}: {gpuInfo.Average:f1}%", false),
                TreeFormatter.CreateNode($"{Strings.SystemInfo_Maximum}: {gpuInfo.Maximum:f1}%", true)
            };
            return resultLines;
        }
    }

    internal class GPUPerformanceCounters
    {
        private const string CATEGORY_NAME = "GPU Engine";
        private const string COUNTER_NAME = "Utilization Percentage";
        private const string ENGINE_TYPE_FILTER = "engtype_3D";

        private readonly Dictionary<string, PerformanceCounter> countersByInstance = [];

        private GPUPerformanceCounters() { }

        internal int Count => countersByInstance.Count;

        internal static GPUPerformanceCounters? TryCreate()
        {
            try
            {
                _ = new PerformanceCounterCategory(CATEGORY_NAME);
            }
            catch
            {
                return null;
            }

            var instance = new GPUPerformanceCounters();
            instance.RefreshInstances();
            return instance.Count == 0 ? null : instance;
        }

        internal void RefreshInstances()
        {
            string[] currentInstanceNames;
            try
            {
                var category = new PerformanceCounterCategory(CATEGORY_NAME);
                currentInstanceNames = category.GetInstanceNames()
                    .Where(name => name.Contains(ENGINE_TYPE_FILTER))
                    .ToArray();
            }
            catch (Exception exception) when (
                exception is InvalidOperationException
                or System.ComponentModel.Win32Exception
                or UnauthorizedAccessException)
            {
                Debug.WriteLine($"GPUPerformanceCounters.RefreshInstances failed: {exception.Message}");
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
                    Debug.WriteLine($"GPUPerformanceCounters: failed to create counter for {instanceName}: {exception.Message}");
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
                    Debug.WriteLine($"GPUPerformanceCounters: counter {pair.Key} failed: {exception.Message}");
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

    internal class GPURepository
    {
        private const int GPU_INFO_LIST_LIMIT_SIZE = 5;
        private const int REFRESH_INTERVAL_TICKS = 30;

        private readonly GPUPerformanceCounters? counters;
        private readonly List<GPUInfo> gpuInfoList = [];
        private int ticksSinceLastRefresh;

        internal bool IsAvailable => counters is not null;

        internal GPURepository()
        {
            counters = GPUPerformanceCounters.TryCreate();
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

            var values = counters.ReadValues();
            if (values.Count == 0) return;

            var gpuInfo = new GPUInfo
            {
                Average = Math.Min(100, values.Average()),
                Maximum = Math.Min(100, values.Max())
            };

            gpuInfoList.Add(gpuInfo);
            if (GPU_INFO_LIST_LIMIT_SIZE < gpuInfoList.Count)
            {
                gpuInfoList.RemoveAt(0);
            }
        }

        internal GPUInfo? Get()
        {
            if (counters is null || gpuInfoList.Count == 0) return null;

            return new GPUInfo
            {
                Average = gpuInfoList.Average(x => x.Average),
                Maximum = gpuInfoList.Max(x => x.Maximum)
            };
        }

        internal void Close()
        {
            counters?.Close();
        }
    }
}
