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

        internal IReadOnlyList<PerformanceCounter> Counters { get; }

        private GPUPerformanceCounters(List<PerformanceCounter> counters)
        {
            Counters = counters;
        }

        internal static GPUPerformanceCounters? TryCreate()
        {
            var counters = new List<PerformanceCounter>();
            try
            {
                var category = new PerformanceCounterCategory(CATEGORY_NAME);
                var instances = category.GetInstanceNames()
                    .Where(n => n.Contains(ENGINE_TYPE_FILTER))
                    .ToList();
                if (instances.Count == 0) return null;

                foreach (var instance in instances)
                {
                    var counter = new PerformanceCounter(CATEGORY_NAME, COUNTER_NAME, instance);
                    counters.Add(counter);
                    _ = counter.NextValue();
                }
                return new GPUPerformanceCounters(counters);
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

    internal class GPURepository
    {
        private const int GPU_INFO_LIST_LIMIT_SIZE = 5;

        private readonly GPUPerformanceCounters? counters;
        private readonly List<GPUInfo> gpuInfoList = [];

        internal bool IsAvailable => counters is not null;

        internal GPURepository()
        {
            counters = GPUPerformanceCounters.TryCreate();
        }

        internal void Update()
        {
            if (counters is null) return;
            try
            {
                var values = counters.Counters.Select(counter => counter.NextValue()).ToList();
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
            catch (Exception exception) when (
                exception is InvalidOperationException
                or System.ComponentModel.Win32Exception
                or UnauthorizedAccessException)
            {
                Debug.WriteLine($"GPURepository.Update failed: {exception.Message}");
                gpuInfoList.Clear();
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
