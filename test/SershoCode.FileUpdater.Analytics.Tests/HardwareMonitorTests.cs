using SershoCode.FileUpdater.Analytics.HardwareMonitoring;
using Xunit.Abstractions;

namespace SershoCode.FileUpdater.Analytics.Tests
{
    public class HardwareMonitorTests(ITestOutputHelper testOutputHelper)
    {
        private readonly ITestOutputHelper _testOutputHelper = testOutputHelper;

        [Fact]
        public async Task CpuHardwareMonitor_Start_ValuesContainsElementsAsync()
        {
            var cpuMonitor = new CpuHardwareMonitor();

            cpuMonitor.Start();

            await Task.Delay(TimeSpan.FromSeconds(1));

            cpuMonitor.Stop();

            var maxValue = cpuMonitor.GetMaxValue();
            var averageValue = cpuMonitor.GetAverageValue();

            _testOutputHelper.WriteLine($"Max value of CPU load: {maxValue}");
            _testOutputHelper.WriteLine($"Average value of CPU load: {averageValue}");

            Assert.NotEqual(0, maxValue);
            Assert.NotEqual(0, averageValue);
        }

        [Fact]
        public async Task RamHardwareMonitor_Start_ValuesContainsElementsAsync()
        {
            var ramMonitor = new RamHardwareMonitor();

            ramMonitor.Start();

            await Task.Delay(TimeSpan.FromSeconds(1));

            ramMonitor.Stop();

            var maxValue = ramMonitor.GetMaxValue();
            var averageValue = ramMonitor.GetAverageValue();

            _testOutputHelper.WriteLine($"Max value of RAM load: {maxValue}");
            _testOutputHelper.WriteLine($"Average value of RAM load: {averageValue}");

            Assert.NotEqual(0, maxValue);
            Assert.NotEqual(0, averageValue);
        }
    }
}