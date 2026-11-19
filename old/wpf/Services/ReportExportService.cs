using System;
using System.IO;

namespace HyperBoostX.Services
{
    public sealed class ReportExportService
    {
        public string ReportsDirectory { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HyperBoost X", "reports");
        public void EnsureReady() => Directory.CreateDirectory(ReportsDirectory);
    }
}
