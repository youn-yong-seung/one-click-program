using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows; // Added for Clipboard

namespace OneClick.Client.Modules
{
    /// <summary>
    /// 카카오톡 타겟 발송 자동화 모듈
    /// - 열린 채팅방 감지
    /// - 무료방/멤버십 자동 분류
    /// - 메시지/파일 일괄 발송
    /// </summary>
    public class KakaoTargetSenderAutomation
    {
        public string ModuleName => "KakaoTargetSender";

        #region Win32 API Declarations

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr FindWindow(string? lpClassName, string? lpWindowName);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string? lpszClass, string? lpszWindow);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        // Virtual Key Codes
        private const byte VK_RETURN = 0x0D;
        private const byte VK_CONTROL = 0x11;
        private const byte VK_V = 0x56;
        private const byte VK_T = 0x54;
        private const byte VK_ESCAPE = 0x1B;

        // Key Event Flags
        private const uint KEYEVENTF_KEYUP = 0x0002;

        #endregion

        #region Public Methods

        /// <summary>
        /// 이미지 파일 선택 다이얼로그 열기 (STA Thread)
        /// </summary>
        public Task<string?> PickImageFileAsync()
        {
            var tcs = new TaskCompletionSource<string?>();

            var thread = new Thread(() =>
            {
                try
                {
                    var dialog = new Microsoft.Win32.OpenFileDialog
                    {
                        Title = "이미지 파일 선택",
                        Filter = "Image Files|*.jpg;*.jpeg;*.png;*.gif;*.bmp|All Files|*.*",
                        Multiselect = false
                    };

                    if (dialog.ShowDialog() == true)
                    {
                        tcs.SetResult(dialog.FileName);
                    }
                    else
                    {
                        tcs.SetResult(null);
                    }
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });

            thread.SetApartmentState(ApartmentState.STA); // WPF Dialog requires STA
            thread.Start();

            return tcs.Task;
        }

        /// <summary>
        /// 현재 열려있는 모든 카카오톡 채팅방 목록 가져오기
        /// </summary>
        public List<string> GetOpenChatWindows()
        {
            var chatRooms = new List<string>();

            EnumWindows((hWnd, lParam) =>
            {
                var className = new StringBuilder(256);
                GetClassName(hWnd, className, 256);

                // 카카오톡 창 클래스명: EVA_Window_Dblclk
                if (className.ToString() == "EVA_Window_Dblclk")
                {
                    int length = GetWindowTextLength(hWnd);
                    if (length > 0)
                    {
                        var title = new StringBuilder(length + 1);
                        GetWindowText(hWnd, title, length + 1);

                        string windowTitle = title.ToString();

                        // "카카오톡" 메인 창 제외
                        if (windowTitle != "카카오톡" && !string.IsNullOrWhiteSpace(windowTitle))
                        {
                            chatRooms.Add(windowTitle);
                        }
                    }
                }

                return true;
            }, IntPtr.Zero);

            return chatRooms;
        }

        /// <summary>
        /// 채팅방 열기 (이미 열린 창 찾기)
        /// </summary>
        public IntPtr OpenRoom(string roomName)
        {
            IntPtr hwnd = FindWindow(null, roomName);

            if (hwnd != IntPtr.Zero)
            {
                SetForegroundWindow(hwnd);
                Thread.Sleep(500);
                return hwnd;
            }

            return IntPtr.Zero;
        }

        /// <summary>
        /// 텍스트 메시지 전송 (클립보드 + Ctrl+V + Enter)
        /// </summary>
        public void SendText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            // 클립보드에 복사
            SetClipboardText(text);
            Thread.Sleep(500); // 100 -> 500

            // Ctrl+V 붙여넣기
            keybd_event(VK_CONTROL, 0, 0, UIntPtr.Zero);
            keybd_event(VK_V, 0, 0, UIntPtr.Zero);
            keybd_event(VK_V, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            Thread.Sleep(500);

            // Enter 전송
            keybd_event(VK_RETURN, 0, 0, UIntPtr.Zero);
            keybd_event(VK_RETURN, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            Thread.Sleep(800);
        }

        /// <summary>
        /// 파일 전송 (Ctrl+T로 파일 전송 창 열기)
        /// </summary>
        public void SendFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                return;

            string absolutePath = Path.GetFullPath(filePath);

            // 클립보드에 파일 경로 복사
            SetClipboardText(absolutePath);
            Thread.Sleep(500); // 100 -> 500

            // Ctrl+T : 파일 전송 대화상자
            keybd_event(VK_CONTROL, 0, 0, UIntPtr.Zero);
            keybd_event(VK_T, 0, 0, UIntPtr.Zero);
            keybd_event(VK_T, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            Thread.Sleep(1000);

            // Ctrl+V 붙여넣기 (경로)
            keybd_event(VK_CONTROL, 0, 0, UIntPtr.Zero);
            keybd_event(VK_V, 0, 0, UIntPtr.Zero);
            keybd_event(VK_V, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            Thread.Sleep(500);

            // Enter : 파일 열기
            keybd_event(VK_RETURN, 0, 0, UIntPtr.Zero);
            keybd_event(VK_RETURN, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            Thread.Sleep(1000);

            // Enter : 전송
            keybd_event(VK_RETURN, 0, 0, UIntPtr.Zero);
            keybd_event(VK_RETURN, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            Thread.Sleep(1000);
        }

        /// <summary>
        /// 채팅방 닫기 (ESC)
        /// </summary>
        public void CloseRoom()
        {
            keybd_event(VK_ESCAPE, 0, 0, UIntPtr.Zero);
            keybd_event(VK_ESCAPE, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            Thread.Sleep(500);
        }

        #endregion

        #region Automation Implementation

        public async Task<string> ExecuteWithProgressAsync(SendRequest request, IProgress<SendProgress>? progress = null)
        {
            try
            {
                if (request == null)
                    return System.Text.Json.JsonSerializer.Serialize(new { success = false, message = "Invalid parameters" });

                var batches = request.Batches.Select(b => new SendBatch
                {
                    Type = b.Type,
                    Rooms = b.Rooms,
                    Message = b.Message
                }).ToList();

                int totalSuccess = 0;
                int totalFail = 0;
                int totalRooms = batches.Sum(b => b.Rooms.Count);
                int currentStep = 0;

                // 초기 진행 보고
                progress?.Report(new SendProgress { Total = totalRooms, Current = 0, Message = "시작..." });

                foreach (var batch in batches)
                {
                    foreach (var roomName in batch.Rooms)
                    {
                        currentStep++;
                        progress?.Report(new SendProgress 
                        { 
                            Total = totalRooms, 
                            Current = currentStep, 
                            Message = $"[{currentStep}/{totalRooms}] {roomName} 전송 중..." 
                        });

                        // 방 열기
                        IntPtr hwnd = OpenRoom(roomName);

                        if (hwnd != IntPtr.Zero)
                        {
                            try
                            {
                                bool hasFile = !string.IsNullOrWhiteSpace(request.FilePath) && File.Exists(request.FilePath);

                                // 파일 전송 로직
                                if (hasFile && request.FileFirst)
                                {
                                    if (request.FilePath != null)
                                        SendFile(request.FilePath);
                                    if (!string.IsNullOrWhiteSpace(batch.Message))
                                        SendText(batch.Message);
                                }
                                else if (hasFile && !request.FileFirst)
                                {
                                    if (!string.IsNullOrWhiteSpace(batch.Message))
                                        SendText(batch.Message);
                                    if (request.FilePath != null)
                                        SendFile(request.FilePath);
                                }
                                else
                                {
                                    if (!string.IsNullOrWhiteSpace(batch.Message))
                                        SendText(batch.Message);
                                }

                                totalSuccess++;
                            }
                            catch
                            {
                                totalFail++;
                            }
                        }
                        else
                        {
                            totalFail++;
                        }

                        // 방 간 대기 (랜덤)
                        if (currentStep < totalRooms)
                        {
                            double delay = Random.Shared.NextDouble() * (request.DelayMax - request.DelayMin) + request.DelayMin;
                            int delayMs = (int)(delay * 1000);
                            await Task.Delay(delayMs);
                        }
                    }
                }

                // 마지막 작업 후 안정화 대기
                await Task.Delay(2000);

                string resultMessage = $"모든 작업 완료.\n성공: {totalSuccess}개, 실패: {totalFail}개";
                
                progress?.Report(new SendProgress 
                { 
                    Total = totalRooms, 
                    Current = totalRooms, 
                    Message = "완료" 
                });

                return System.Text.Json.JsonSerializer.Serialize(new
                {
                    success = true,
                    message = resultMessage,
                    totalSuccess,
                    totalFail
                });
            }
            catch (Exception ex)
            {
                return System.Text.Json.JsonSerializer.Serialize(new
                {
                    success = false,
                    message = $"오류 발생: {ex.Message}"
                });
            }
        }

        #endregion

        #region Helper Methods

        private void SetClipboardText(string text)
        {
             // Use STA Thread to access Clipboard safely without dependencies
             Application.Current.Dispatcher.Invoke(() =>
             {
                try {
                    Clipboard.Clear();
                    Clipboard.SetDataObject(text, true);
                } catch { /* Ignore or retry */ }
             });
        }

        #endregion
    }
}

/// <summary>
/// 발송 배치 정보
/// </summary>
public class SendBatch
{
    public string Type { get; set; } = string.Empty; // "무료방" 또는 "멤버십"
    public List<string> Rooms { get; set; } = new();
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// 발송 요청 DTO
/// </summary>
public class SendRequest
{
    public List<SendBatch> Batches { get; set; } = new();
    public string? FilePath { get; set; }
    public bool FileFirst { get; set; } = true;
    public double DelayMin { get; set; } = 1.0;
    public double DelayMax { get; set; } = 3.0;
}

public class SendProgress
{
    public int Total { get; set; }
    public int Current { get; set; }
    public string Message { get; set; } = string.Empty;
}
