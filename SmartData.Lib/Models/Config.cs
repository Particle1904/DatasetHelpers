﻿namespace SmartData.Lib.Models
{
    public class Config
    {
        public float TaggerThreshold { get; set; } = 0.4f;
        public string SelectedFolder { get; set; } = string.Empty;
        public string DiscardedFolder { get; set; } = string.Empty;
        public string BackupFolder { get; set; } = string.Empty;
        public string ResizedFolder { get; set; } = string.Empty;
        public string CombinedOutputFolder { get; set; } = string.Empty;
    }
}
