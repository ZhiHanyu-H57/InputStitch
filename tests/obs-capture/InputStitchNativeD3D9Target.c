#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <stdlib.h>
#include <string.h>

/* Native Win32/D3D9 acceptance target: no CLR, WinForms, or secondary overlay window. */
typedef struct Parameters {
    UINT Width, Height, Format, Count, MultiSample, Quality, SwapEffect;
    HWND Window;
    BOOL Windowed, AutoDepth;
    UINT DepthFormat, Flags, Refresh, Interval;
} Parameters;
typedef struct Vertex { float x, y, z, rhw; DWORD color; } Vertex;
typedef char Require64Bit[(sizeof(void *) == 8 && sizeof(Parameters) == 64) ? 1 : -1];
typedef void *(WINAPI *Create9Fn)(UINT);
typedef HRESULT (WINAPI *CreateDeviceFn)(void *, UINT, UINT, HWND, DWORD, Parameters *, void **);
typedef HRESULT (WINAPI *ClearFn)(void *, DWORD, void *, DWORD, DWORD, float, DWORD);
typedef HRESULT (WINAPI *SceneFn)(void *);
typedef HRESULT (WINAPI *PresentFn)(void *, void *, void *, HWND, void *);
typedef HRESULT (WINAPI *ResetFn)(void *, Parameters *);
typedef HRESULT (WINAPI *SetFVFFn)(void *, DWORD);
typedef HRESULT (WINAPI *DrawUPFn)(void *, UINT, UINT, const void *, UINT);
typedef HRESULT (WINAPI *RenderStateFn)(void *, UINT, DWORD);
typedef ULONG (WINAPI *ReleaseFn)(void *);
#define METHOD(object, index, type) ((type)((*(void ***)(object))[index]))
static HANDLE logFile = INVALID_HANDLE_VALUE;
static void *d3d, *device;
static HMODULE d3dModule;
static Parameters pp;
static int needsResize;
static const DWORD BLUE = 0xff143252, GREEN = 0xff36ee78, WHITE = 0xfff0f6ff;

static void Log(const char *message)
{
    DWORD written;
    if (logFile != INVALID_HANDLE_VALUE) { WriteFile(logFile, message, (DWORD)strlen(message), &written, NULL); FlushFileBuffers(logFile); }
}
static void LogNumber(const char *stage, DWORD value)
{
    char text[192]; wsprintfA(text, "%s=%lu (0x%08lX)\r\n", stage, value, value); Log(text);
}
static void OpenLog(void)
{
    WCHAR file[MAX_PATH]; DWORD length = GetModuleFileNameW(NULL, file, MAX_PATH); int i;
    if (!length || length >= MAX_PATH) return;
    for (i = (int)length - 1; i >= 0 && file[i] != L'\\'; --i) { }
    lstrcpyW(file + i + 1, L"native-target-run.log");
    logFile = CreateFileW(file, GENERIC_WRITE, FILE_SHARE_READ, NULL, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL);
    Log("InputStitch native D3D9 target; x64 C; ordinary main window; no managed runtime\r\n");
    LogNumber("processId", GetCurrentProcessId());
}
static int InitD3D(HWND window)
{
    Create9Fn create; HRESULT result;
    d3dModule = LoadLibraryW(L"d3d9.dll");
    if (!d3dModule) { LogNumber("LoadLibraryError", GetLastError()); return 0; }
    create = (Create9Fn)GetProcAddress(d3dModule, "Direct3DCreate9");
    if (!create || !(d3d = create(32))) { Log("Direct3DCreate9 failed\r\n"); return 0; }
    ZeroMemory(&pp, sizeof(pp));
    pp.Width = 800; pp.Height = 400; pp.Count = 1; pp.SwapEffect = 1;
    pp.Window = window; pp.Windowed = TRUE; pp.Interval = 0x80000000;
    result = METHOD(d3d, 16, CreateDeviceFn)(d3d, 0, 1, window, 0x20, &pp, &device);
    LogNumber("CreateDeviceHRESULT", (DWORD)result);
    if (FAILED(result)) return 0;
    needsResize = 0;
    Log("Created real IDirect3DDevice9; software vertex processing, hardware rasterizer\r\n");
    return 1;
}
static HRESULT RectanglePrimitive(float x, float y, float width, float height, DWORD color)
{
    Vertex vertices[4]; int i;
    for (i = 0; i < 4; ++i) { vertices[i].z = 0; vertices[i].rhw = 1; vertices[i].color = color; }
    vertices[0].x = x - .5f; vertices[0].y = y - .5f;
    vertices[1].x = x + width - .5f; vertices[1].y = y - .5f;
    vertices[2].x = x - .5f; vertices[2].y = y + height - .5f;
    vertices[3].x = x + width - .5f; vertices[3].y = y + height - .5f;
    return METHOD(device, 83, DrawUPFn)(device, 5, 2, vertices, sizeof(Vertex));
}
static void Digit(int digit, float x, float y)
{
    static const unsigned char masks[10] = { 0x3f, 0x06, 0x5b, 0x4f, 0x66, 0x6d, 0x7d, 0x07, 0x7f, 0x6f };
    unsigned char mask = masks[digit];
    if (mask & 1) RectanglePrimitive(x + 4, y, 16, 4, WHITE);
    if (mask & 2) RectanglePrimitive(x + 20, y + 4, 4, 16, WHITE);
    if (mask & 4) RectanglePrimitive(x + 20, y + 24, 4, 16, WHITE);
    if (mask & 8) RectanglePrimitive(x + 4, y + 40, 16, 4, WHITE);
    if (mask & 16) RectanglePrimitive(x, y + 24, 4, 16, WHITE);
    if (mask & 32) RectanglePrimitive(x, y + 4, 4, 16, WHITE);
    if (mask & 64) RectanglePrimitive(x + 4, y + 20, 16, 4, WHITE);
}
static void FrameNumber(DWORD frame)
{
    char text[32]; int i; wsprintfA(text, "%lu", frame);
    for (i = 0; text[i]; ++i) Digit(text[i] - '0', 24.0f + i * 32.0f, 54.0f);
}
static void FrameLabel(void)
{
    static const unsigned char glyphs[5][7] = {
        {31,16,16,30,16,16,16}, {30,17,17,30,20,18,17},
        {14,17,17,31,17,17,17}, {17,27,21,21,17,17,17}, {31,16,16,30,16,16,31}
    };
    int c, row, col;
    for (c = 0; c < 5; ++c) for (row = 0; row < 7; ++row) for (col = 0; col < 5; ++col)
        if (glyphs[c][row] & (1 << (4 - col))) RectanglePrimitive(24 + c * 18 + col * 3, 18 + row * 3, 3, 3, WHITE);
}
static HRESULT Render(DWORD frame, DWORD elapsed)
{
    HRESULT result; Vertex point; DWORD phase = elapsed % 6000;
    float x = 30.0f + (phase <= 3000 ? phase : 6000 - phase) * 0.22f;
    result = METHOD(device, 43, ClearFn)(device, 0, NULL, 1, BLUE, 1, 0);
    if (FAILED(result)) return result;
    result = METHOD(device, 41, SceneFn)(device);
    if (FAILED(result)) return result;
    METHOD(device, 57, RenderStateFn)(device, 7, 0);
    METHOD(device, 57, RenderStateFn)(device, 22, 1);
    METHOD(device, 57, RenderStateFn)(device, 137, 0);
    result = METHOD(device, 89, SetFVFFn)(device, 0x44);
    if (SUCCEEDED(result)) {
        point.x = 0; point.y = 0; point.z = 0; point.rhw = 1; point.color = BLUE;
        result = METHOD(device, 83, DrawUPFn)(device, 1, 1, &point, sizeof(point));
        /* Official RTSS DesktopOverlayHost renders a point for OSD observation. */
        if (SUCCEEDED(result)) result = RectanglePrimitive(x, 180, 64, 64, GREEN);
        FrameLabel(); FrameNumber(frame);
    }
    METHOD(device, 42, SceneFn)(device);
    if (FAILED(result)) return result;
    return METHOD(device, 17, PresentFn)(device, NULL, NULL, NULL, NULL);
}
static void ReleaseD3D(void)
{
    if (device) { METHOD(device, 2, ReleaseFn)(device); device = NULL; }
    if (d3d) { METHOD(d3d, 2, ReleaseFn)(d3d); d3d = NULL; }
    if (d3dModule) { FreeLibrary(d3dModule); d3dModule = NULL; }
}
static LRESULT CALLBACK WindowProc(HWND window, UINT message, WPARAM wparam, LPARAM lparam)
{
    if (message == WM_CLOSE) { DestroyWindow(window); return 0; }
    if (message == WM_DESTROY) { PostQuitMessage(0); return 0; }
    if (message == WM_KEYDOWN && wparam == VK_ESCAPE) { DestroyWindow(window); return 0; }
    if (message == WM_SIZE) { needsResize = 1; return 0; }
    if (message == WM_ERASEBKGND) return 1;
    return DefWindowProcW(window, message, wparam, lparam);
}
int WINAPI WinMain(HINSTANCE instance, HINSTANCE previous, LPSTR commandLine, int show)
{
    WNDCLASSW cls; HWND window; RECT bounds; MSG message; DWORD begin, frame = 0, nextLog = 0, seconds = 0;
    HRESULT result = S_OK, lastError = S_OK; int quitting = 0, exitCode = 0; const char *option;
    typedef BOOL (WINAPI *DpiFn)(HANDLE);
    DpiFn dpi = (DpiFn)GetProcAddress(GetModuleHandleW(L"user32.dll"), "SetProcessDpiAwarenessContext");
    if (dpi) dpi((HANDLE)-4);
    option = strstr(commandLine, "--seconds"); if (option) seconds = (DWORD)strtoul(option + 9, NULL, 10);
    if (seconds > 86400) seconds = 86400;
    OpenLog(); LogNumber("automaticExitSeconds", seconds);
    ZeroMemory(&cls, sizeof(cls)); cls.lpfnWndProc = WindowProc; cls.hInstance = instance;
    cls.hCursor = LoadCursor(NULL, IDC_ARROW); cls.lpszClassName = L"InputStitchNativeD3D9TargetClass";
    if (!RegisterClassW(&cls)) { LogNumber("RegisterClassError", GetLastError()); exitCode = 1; goto cleanup; }
    bounds.left = 0; bounds.top = 0; bounds.right = 800; bounds.bottom = 400; AdjustWindowRect(&bounds, WS_OVERLAPPEDWINDOW, FALSE);
    window = CreateWindowExW(0, cls.lpszClassName, L"InputStitch Native D3D9 Target", WS_OVERLAPPEDWINDOW,
        90, 90, bounds.right - bounds.left, bounds.bottom - bounds.top, NULL, NULL, instance, NULL);
    if (!window) { LogNumber("CreateWindowError", GetLastError()); exitCode = 1; goto cleanup; }
    {
        RECT client, outer;
        GetClientRect(window, &client); GetWindowRect(window, &outer);
        SetWindowPos(window, NULL, 0, 0, 800 + outer.right - outer.left - client.right,
            400 + outer.bottom - outer.top - client.bottom, SWP_NOMOVE | SWP_NOZORDER | SWP_NOACTIVATE);
        GetClientRect(window, &client);
        LogNumber("initialClientWidth", client.right); LogNumber("initialClientHeight", client.bottom);
    }
    ShowWindow(window, SW_SHOWNORMAL); UpdateWindow(window);
    if (!InitD3D(window)) { DestroyWindow(window); exitCode = 1; goto cleanup; }
    begin = GetTickCount();
    while (!quitting) {
        DWORD elapsed = GetTickCount() - begin;
        while (PeekMessageW(&message, NULL, 0, 0, PM_REMOVE)) {
            if (message.message == WM_QUIT) { quitting = 1; break; }
            TranslateMessage(&message); DispatchMessageW(&message);
        }
        if (quitting) break;
        if (seconds && elapsed >= seconds * 1000) { DestroyWindow(window); continue; }
        if (needsResize && !IsIconic(window)) {
            RECT client; GetClientRect(window, &client);
            if (client.right > 0 && client.bottom > 0) {
                pp.Width = client.right; pp.Height = client.bottom;
                result = METHOD(device, 16, ResetFn)(device, &pp); needsResize = FAILED(result);
            }
        }
        result = Render(frame, elapsed);
        if (SUCCEEDED(result)) { ++frame; lastError = S_OK; }
        else {
            HRESULT cooperative;
            if (result != lastError) { LogNumber("RenderHRESULT", (DWORD)result); lastError = result; }
            cooperative = METHOD(device, 3, SceneFn)(device);
            if ((DWORD)cooperative == 0x88760869) METHOD(device, 16, ResetFn)(device, &pp);
            Sleep(32);
        }
        if (elapsed >= nextLog) {
            char status[192]; wsprintfA(status, "elapsedMs=%lu frames=%lu presentHRESULT=0x%08lX\r\n", elapsed, frame, (DWORD)result); Log(status); nextLog = elapsed + 1000;
        }
        Sleep(15);
    }
    LogNumber("completedFrames", frame); LogNumber("durationMs", GetTickCount() - begin);
cleanup:
    ReleaseD3D(); LogNumber("exitCode", exitCode); if (logFile != INVALID_HANDLE_VALUE) CloseHandle(logFile);
    return exitCode;
}
