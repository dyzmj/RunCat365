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
    struct CPUInfo
    {
        internal float Total { get; set; }
        internal float User { get; set; }
        internal float Kernel { get; set; }
        internal float Idle { get; set; }
    }

    internal static class CPUInfoExtension
    {
        internal static string GetDescription(this CPUInfo cpuInfo)
        {
            return $"{Strings.SystemInfo_CPU}: {cpuInfo.Total:f1}%";
        }

        internal static List<string> GenerateIndicator(this CPUInfo cpuInfo)
        {
            var resultLines = new List<string>
            {
                TreeFormatter.CreateRoot($"{Strings.SystemInfo_CPU}: {cpuInfo.Total:f1}%"),
                TreeFormatter.CreateNode($"{Strings.SystemInfo_User}: {cpuInfo.User:f1}%", false),
                TreeFormatter.CreateNode($"{Strings.SystemInfo_Kernel}: {cpuInfo.Kernel:f1}%", false),
                TreeFormatter.CreateNode($"{Strings.SystemInfo_Available}: {cpuInfo.Idle:f1}%", true)
            };
            return resultLines;
        }
    }

    internal class CPUPerformanceCounters
    {
        private const string PROCESSOR_INFORMATION_CATEGORY = "Processor Information";
        private const string PROCESSOR_CATEGORY = "Processor";
        private const string TOTAL_INSTANCE = "_Total";
        private const string PROCESSOR_UTILITY_COUNTER = "% Processor Utility";
        private const string PROCESSOR_TIME_COUNTER = "% Processor Time";
        private const string USER_TIME_COUNTER = "% User Time";
        private const string PRIVILEGED_TIME_COUNTER = "% Privileged Time";
        private const int PROCESSOR_TIME_BASED_TASK_MANAGER_MINIMUM_BUILD = 26100;

        internal PerformanceCounter Total { get; }
        internal PerformanceCounter User { get; }
        internal PerformanceCounter Kernel { get; }

        private CPUPerformanceCounters(
            PerformanceCounter total,
            PerformanceCounter user,
            PerformanceCounter kernel)
        {
            Total = total;
            User = user;
            Kernel = kernel;
        }

        internal static CPUPerformanceCounters? TryCreate()
        {
            return TryCreateFromCategory(PROCESSOR_INFORMATION_CATEGORY, TOTAL_INSTANCE)
                ?? TryCreateFromCategory(PROCESSOR_CATEGORY, TOTAL_INSTANCE);
        }

        private static bool TaskManagerUsesProcessorTime()
        {
            return PROCESSOR_TIME_BASED_TASK_MANAGER_MINIMUM_BUILD <= Environment.OSVersion.Version.Build;
        }

        private static CPUPerformanceCounters? TryCreateFromCategory(string categoryName, string instanceName)
        {
            PerformanceCounter? total = null;
            PerformanceCounter? user = null;
            PerformanceCounter? kernel = null;
            if (!TaskManagerUsesProcessorTime())
            {
                try
                {
                    total = new PerformanceCounter(categoryName, PROCESSOR_UTILITY_COUNTER, instanceName);
                }
                catch
                {
                    total?.Close();
                    total = null;
                }
            }
            try
            {
                total ??= new PerformanceCounter(categoryName, PROCESSOR_TIME_COUNTER, instanceName);
                user = new PerformanceCounter(categoryName, USER_TIME_COUNTER, instanceName);
                kernel = new PerformanceCounter(categoryName, PRIVILEGED_TIME_COUNTER, instanceName);
                _ = total.NextValue();
                _ = user.NextValue();
                _ = kernel.NextValue();
                return new CPUPerformanceCounters(total, user, kernel);
            }
            catch
            {
                total?.Close();
                user?.Close();
                kernel?.Close();
                return null;
            }
        }

        internal void Close()
        {
            Total.Close();
            User.Close();
            Kernel.Close();
        }
    }

    internal class CPURepository
    {
        private readonly CPUPerformanceCounters? counters;
        private readonly List<CPUInfo> cpuInfoList = [];
        private const int CPU_INFO_LIST_LIMIT_SIZE = 5;

        internal bool IsAvailable => counters is not null;

        internal CPURepository()
        {
            counters = CPUPerformanceCounters.TryCreate();
        }

        internal void Update()
        {
            if (counters is null) return;

            var total = Math.Min(100, counters.Total.NextValue());
            var user = Math.Min(100, counters.User.NextValue());
            var kernel = Math.Min(100, counters.Kernel.NextValue());
            var idle = Math.Max(0, 100 - user - kernel);

            var cpuInfo = new CPUInfo
            {
                Total = total,
                User = user,
                Kernel = kernel,
                Idle = idle,
            };

            cpuInfoList.Add(cpuInfo);
            if (CPU_INFO_LIST_LIMIT_SIZE < cpuInfoList.Count)
            {
                cpuInfoList.RemoveAt(0);
            }
        }

        internal CPUInfo Get()
        {
            if (cpuInfoList.Count == 0) return new CPUInfo();

            return new CPUInfo
            {
                Total = cpuInfoList.Average(x => x.Total),
                User = cpuInfoList.Average(x => x.User),
                Kernel = cpuInfoList.Average(x => x.Kernel),
                Idle = cpuInfoList.Average(x => x.Idle)
            };
        }

        internal void Close()
        {
            counters?.Close();
        }
    }
}
