using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json.Nodes;

namespace OneClick.Server.Services.Automation.Modules;

public class KakaoAutomation : IAutomationModule
{
    public string ModuleName => "KakaoBot";

    // --- Win32 API Constants ---
    private const int WM_SETTEXT = 0x000C;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_KEYUP = 0x0101;
    private const int WM_LBUTTONDOWN = 0x0201;
    private const int WM_LBUTTONUP = 0x0202;
    
    private const int VK_RETURN = 0x0D;
    private const int VK_CONTROL = 0x11;
    private const int VK_V = 0x56;
    private const int VK_ESCAPE = 0x1B;
    private const int VK_BACK = 0x08;
    
    private const uint KEYEVENTF_KEYUP = 0x0002;
    private const int SW_RESTORE = 9;
    private const int VK_F = 0x46;

    // --- P/Invoke ---
    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr FindWindow(string? lpClassName, string? lpWindowName);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string? lpszClass, string? lpszWindow);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, string lParam);

    [DllImport("user32.dll")]
    private static extern bool PostMessage(IntPtr hWnd, uint Msg, int wParam, int lParam);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);
    
    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
    
    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr ProcessId);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    [DllImport("user32.dll")]
    private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern IntPtr SetFocus(IntPtr hWnd);

    // --- Clipboard API ---
    [DllImport("user32.dll")]
    private static extern bool OpenClipboard(IntPtr hWndNewOwner);
    [DllImport("user32.dll")]
    private static extern bool CloseClipboard();
    [DllImport("user32.dll")]
    private static extern bool EmptyClipboard();
    [DllImport("user32.dll")]
    private static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);
    [DllImport("kernel32.dll")]
    private static extern IntPtr GlobalAlloc(uint uFlags, UIntPtr dwBytes);
    [DllImport("kernel32.dll")]
    private static extern IntPtr GlobalLock(IntPtr hMem);
    [DllImport("kernel32.dll")]
    private static extern bool GlobalUnlock(IntPtr hMem);
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalFree(IntPtr hMem);

    private const uint GMEM_MOVEABLE = 0x0002;
    private const uint GMEM_ZEROINIT = 0x0040;
    private const uint CF_UNICODETEXT = 13;

    private CancellationTokenSource? _cts;

    public void Cancel()
    {
        _cts?.Cancel();
    }

    public async Task<string> ExecuteAsync(string? parameters = null)
    {
        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        var log = new StringBuilder();
        log.AppendLine("Started KakaoTalk Automation (Win32 API v4 - AttachThreadInput)...");

        try
        {
            if (string.IsNullOrEmpty(parameters)) return "Error: No parameters.";

            var json = JsonNode.Parse(parameters);
            string roomNamesInput = json?["roomName"]?.ToString() ?? "";
            string message = json?["message"]?.ToString() ?? "";

            if (string.IsNullOrEmpty(roomNamesInput) || string.IsNullOrEmpty(message))
                return "Error: Need 'roomName' and 'message'.";

            // 1. 카카오톡 메인 핸들 찾기
            IntPtr hwndMain = FindKakaoMainHandle();
            if (hwndMain == IntPtr.Zero)
                return "Error: KakaoTalk Main Window Not Found.";
            
            log.AppendLine($"Found Main Window: 0x{hwndMain.ToInt64():X}");

            var roomNames = roomNamesInput.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            int successCount = 0;

            foreach (var room in roomNames)
            {
                // 취소 요청 확인
                if (token.IsCancellationRequested)
                {
                    log.AppendLine("Process Cancelled by User.");
                    return $"Cancelled.\n{log}\nSuccess: {successCount}/{roomNames.Length}";
                }

                log.Append($"Processing '{room}'... ");

                // 2. 검색창 찾기 (Reference Logic)
                IntPtr hwndSearch = FindSearchEditControl(hwndMain);

                if (hwndSearch != IntPtr.Zero)
                {
                    // 3-A. Edit 컨트롤을 찾은 경우: SendMessage로 입력
                    // 채팅방 검색창에 채팅방 이름 입력 (WM_SETTEXT = 0x000C)
                    SendMessage(hwndSearch, WM_SETTEXT, 0, room);
                    await Task.Delay(1000, token); // 목록이 나오는 것을 기다림

                    // 엔터쳐서 채팅방 열기
                    // 포커스를 주지 않고 메시지만 보내는 방식 시도
                    PostMessage(hwndSearch, WM_KEYDOWN, VK_RETURN, 0);
                    await Task.Delay(100, token);
                    PostMessage(hwndSearch, WM_KEYUP, VK_RETURN, 0);
                }
                else
                {
                    // 3-B. 못 찾은 경우: 기존 Ctrl+F 방식 (Fallback)
                    ForceActivateAndFocus(hwndMain);
                    await Task.Delay(500, token);

                    // Ctrl + F
                    keybd_event(VK_CONTROL, 0, 0, 0);
                    keybd_event((byte)VK_F, 0, 0, 0);
                    await Task.Delay(50, token);
                    keybd_event((byte)VK_F, 0, KEYEVENTF_KEYUP, 0);
                    keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, 0);
                    await Task.Delay(1000, token); 

                    // 붙여넣기
                    await SendTextUsingClipboard(room, pressEnter: false);
                    await Task.Delay(1000, token);
                    
                    // Enter
                    keybd_event((byte)VK_RETURN, 0, 0, 0);
                    keybd_event((byte)VK_RETURN, 0, KEYEVENTF_KEYUP, 0);
                }

                // 6. 채팅방 열렸는지 확인
                IntPtr hwndRoom = IntPtr.Zero;
                for (int i = 0; i < 30; i++) // 최대 15초 대기
                {
                    if (token.IsCancellationRequested) break;
                    
                    hwndRoom = FindWindowByTitleContaining(room);
                    if (hwndRoom != IntPtr.Zero) break;
                    await Task.Delay(500, token);
                }
                
                // 검색창 닫기 (Esc)
                // ... (로직 동일)

                if (hwndRoom == IntPtr.Zero)
                {
                    log.AppendLine("FAILED (Chatroom window not found).");
                    // 실패했으면 메인창 검색 모드라도 닫아둠
                    ForceActivateAndFocus(hwndMain);
                    keybd_event(VK_ESCAPE, 0, 0, 0);
                    keybd_event(VK_ESCAPE, 0, KEYEVENTF_KEYUP, 0);
                    continue;
                }

                if (token.IsCancellationRequested)
                {
                    log.AppendLine("Cancelled during wait.");
                    return $"Cancelled.\n{log}";
                }

                log.Append($"Opened(0x{hwndRoom.ToInt64():X}). Sending... ");

                // 7. 메시지 전송
                // [핵심] AttachThreadInput을 사용하여 강제로 포커스 및 활성화
                ForceActivateAndFocus(hwndRoom);
                await Task.Delay(500, token);

                // 붙여넣기 + Enter (메시지 전송)
                await SendTextUsingClipboard(message, pressEnter: true);
                
                await Task.Delay(800, token);

                // 8. 닫기 (Esc)
                keybd_event((byte)VK_ESCAPE, 0, 0, 0);
                keybd_event((byte)VK_ESCAPE, 0, KEYEVENTF_KEYUP, 0);
                await Task.Delay(500, token);

                log.AppendLine("DONE.");
                successCount++;
            }

            return $"Finished.\n{log}\nSuccess: {successCount}/{roomNames.Length}";
        }
        catch (OperationCanceledException)
        {
            return $"Cancelled.\n{log}";
        }
        catch (Exception ex)
        {
            return $"Critical Error: {ex.Message}\nLog: {log}";
        }
    }

    // --- Helper Methods ---

    private IntPtr FindKakaoMainHandle()
    {
        // Reference: Provided JS/AutoHotkey logic
        // 무한 루프를 돌면서 'EVA_Window_Dblclk' 클래스를 가진 모든 윈도우를 순서대로 확인
        
        IntPtr hwnd = IntPtr.Zero;
        while (true)
        {
            hwnd = FindWindowEx(IntPtr.Zero, hwnd, "EVA_Window_Dblclk", null);
            if (hwnd == IntPtr.Zero) break;

            // --- 검증 단계 ---
            // 1단계: 자식 컨테이너(EVA_ChildWindow) 확인
            IntPtr hwndChild = FindWindowEx(hwnd, IntPtr.Zero, "EVA_ChildWindow", null);
            if (hwndChild != IntPtr.Zero)
            {
                // 2단계: 그 안에 친구 목록/채팅 목록(EVA_Window)이 있는지 확인
                // 가짜 창은 이 단계에서 0이 나옵니다.
                IntPtr hwndList = FindWindowEx(hwndChild, IntPtr.Zero, "EVA_Window", null);
                if (hwndList != IntPtr.Zero)
                {
                   // 구조가 일치하면 반환
                   return hwnd;
                }
            }
        }
        return IntPtr.Zero;
    }

    // Snippet logic to find the Search Edit Control
    private IntPtr FindSearchEditControl(IntPtr hwndMain)
    {
        IntPtr hwndChild = FindWindowEx(hwndMain, IntPtr.Zero, "EVA_ChildWindow", null);
        if (hwndChild == IntPtr.Zero) return IntPtr.Zero;

        // 1st EVA_Window (Friend List)
        IntPtr hwndFriend = FindWindowEx(hwndChild, IntPtr.Zero, "EVA_Window", null);
        if (hwndFriend == IntPtr.Zero) return IntPtr.Zero;

        // 2nd EVA_Window (Chat List)
        IntPtr hwndChat = FindWindowEx(hwndChild, hwndFriend, "EVA_Window", null);
        if (hwndChat == IntPtr.Zero) return IntPtr.Zero;

        // Edit Control inside Chat List
        // Note: The snippet says 'Edit'.
        return FindWindowEx(hwndChat, IntPtr.Zero, "Edit", null);
    }

    private IntPtr FindWindowByTitleContaining(string partialTitle)
    {
        IntPtr foundHwnd = IntPtr.Zero;
        EnumWindows((hwnd, lParam) =>
        {
            StringBuilder sb = new StringBuilder(256);
            if (GetWindowText(hwnd, sb, 256) > 0)
            {
                string title = sb.ToString();
                if (title.Contains(partialTitle) && title != "카카오톡")
                {
                    foundHwnd = hwnd;
                    return false; // Stop enumeration
                }
            }
            return true; // Continue
        }, IntPtr.Zero);

        return foundHwnd;
    }

    // [핵심] 강력한 강제 활성화 및 포커스 함수
    private void ForceActivateAndFocus(IntPtr hwndWindow)
    {
        uint foreThread = GetWindowThreadProcessId(GetForegroundWindow(), IntPtr.Zero);
        uint appThread = GetCurrentThreadId();
        uint targetThread = GetWindowThreadProcessId(hwndWindow, IntPtr.Zero);

        // 1. 스레드 입력 연결
        if (foreThread != targetThread)
        {
            AttachThreadInput(foreThread, appThread, true);
            AttachThreadInput(targetThread, appThread, true);
        }

        // 2. 윈도우 복원 및 활성화
        ShowWindow(hwndWindow, SW_RESTORE);
        SetForegroundWindow(hwndWindow);
        
        // [중요] 내부 컨트롤(Edit 등)을 직접 찾아 포커스하는 로직 제거
        // Ctrl+F와 같은 글로벌 단축키를 믿고 메인 윈도우만 활성화합니다.
        // 잘못된 컨트롤을 찾아 포커스하면 엉뚱한 동작(친구추가 등)이 발생할 수 있음.

        // 4. 스레드 연결 해제 (Clean up)
        if (foreThread != targetThread)
        {
            AttachThreadInput(targetThread, appThread, false);
            AttachThreadInput(foreThread, appThread, false);
        }
    }

    private async Task SendTextUsingClipboard(string text, bool pressEnter = true)
    {
        if (OpenClipboard(IntPtr.Zero))
        {
            EmptyClipboard();
            int size = (text.Length + 1) * 2;
            IntPtr hGlobal = GlobalAlloc(GMEM_MOVEABLE | GMEM_ZEROINIT, (UIntPtr)size);
            if (hGlobal != IntPtr.Zero)
            {
                IntPtr target = GlobalLock(hGlobal);
                if (target != IntPtr.Zero)
                {
                    byte[] bytes = Encoding.Unicode.GetBytes(text);
                    Marshal.Copy(bytes, 0, target, bytes.Length);
                    GlobalUnlock(hGlobal);
                    SetClipboardData(CF_UNICODETEXT, hGlobal);
                }
                else GlobalFree(hGlobal);
            }
            CloseClipboard();
        }
        await Task.Delay(300);

        // Ctrl + V
        keybd_event(VK_CONTROL, 0, 0, 0);
        keybd_event(VK_V, 0, 0, 0);
        await Task.Delay(50);
        keybd_event(VK_V, 0, KEYEVENTF_KEYUP, 0);
        keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, 0);
        
        await Task.Delay(300);

        if (pressEnter)
        {
            // Enter
            keybd_event((byte)VK_RETURN, 0, 0, 0);
            keybd_event((byte)VK_RETURN, 0, KEYEVENTF_KEYUP, 0);
        }
    }
}
