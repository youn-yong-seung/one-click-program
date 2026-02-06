using System;
using System.Collections.Generic;

namespace OneClick.Client.Modules
{
    public class KakaoSenderStateService
    {
        public List<string> SourceRooms { get; set; } = new();
        public List<string> FreeRooms { get; set; } = new();
        public List<string> MemberRooms { get; set; } = new();
        public List<string> SelectedRooms { get; set; } = new();

        public string FreeMessage { get; set; } = "";
        public string MemberMessage { get; set; } = "";
        public string AttachmentPath { get; set; } = "";
        public string StatusMessage { get; set; } = "";

        public bool FileFirst { get; set; }
        public bool IsScheduleConfirmed { get; set; }
        public bool IsScheduled { get; set; }
        public bool IsSending { get; set; }

        public DateTime? ScheduledTime { get; set; }

        public int SentCount { get; set; }
        public int TotalCount { get; set; }
        public int ProgressPercentage { get; set; }

        public void Reset()
        {
            SourceRooms.Clear();
            FreeRooms.Clear();
            MemberRooms.Clear();
            SelectedRooms.Clear();
            FreeMessage = "";
            MemberMessage = "";
            AttachmentPath = "";
            StatusMessage = "";
            FileFirst = false;
            IsScheduleConfirmed = false;
            IsScheduled = false;
            IsSending = false;
            ScheduledTime = null;
            SentCount = 0;
            TotalCount = 0;
            ProgressPercentage = 0;
        }
    }
}
