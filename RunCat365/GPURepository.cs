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

    internal sealed class GPUPerformanceCounters : InstancedPerformanceCounters
    {
        private const string ENGINE_TYPE_FILTER = "engtype_3D";

        protected override string CategoryName => "GPU Engine";
        protected override string CounterName => "Utilization Percentage";

        protected override bool ShouldIncludeInstance(string instanceName)
        {
            return instanceName.Contains(ENGINE_TYPE_FILTER, StringComparison.Ordinal);
        }

        internal static GPUPerformanceCounters? TryCreate()
        {
            var instance = new GPUPerformanceCounters();
            return instance.TryInitialize() ? instance : null;
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
