#include "../include/InputDetection.h"
#include <iostream>

InputDetection::InputDetection()
    : m_pDirectInput(nullptr)
    , m_pMouseDevice(nullptr)
    , m_hInstance(nullptr)
{
    ZeroMemory(&m_mouseState, sizeof(m_mouseState));
    m_mousePosition = Vector2i(0, 0);
    m_previousMousePosition = Vector2i(0, 0);
    m_mouseMovement = Vector2i(0, 0);
    m_mouseWheelDelta = 0;
    m_previousWheelDelta = 0;
}

InputDetection::~InputDetection()
{
    Shutdown();
}

bool InputDetection::Initialize()
{
    m_hInstance = GetModuleHandle(nullptr);

    if (!InitializeDirectInput())
    {
        std::cerr << "Failed to initialize DirectInput!" << std::endl;
        return false;
    }

    if (!InitializeMouse())
    {
        std::cerr << "Failed to initialize mouse input!" << std::endl;
        return false;
    }

    std::cout << "Input detection initialized successfully." << std::endl;
    return true;
}

void InputDetection::Shutdown()
{
    if (m_pMouseDevice)
    {
        m_pMouseDevice->Unacquire();
        m_pMouseDevice->Release();
        m_pMouseDevice = nullptr;
    }

    if (m_pDirectInput)
    {
        m_pDirectInput->Release();
        m_pDirectInput = nullptr;
    }
}

bool InputDetection::InitializeDirectInput()
{
    HRESULT hr = DirectInput8Create(
        m_hInstance,
        DIRECTINPUT_VERSION,
        IID_IDirectInput8,
        (void**)&m_pDirectInput,
        nullptr
    );

    if (FAILED(hr))
    {
        std::cerr << "DirectInput8Create failed: " << std::hex << hr << std::endl;
        return false;
    }

    return true;
}

bool InputDetection::InitializeMouse()
{
    if (!m_pDirectInput)
        return false;

    // Create mouse device
    HRESULT hr = m_pDirectInput->CreateDevice(GUID_SysMouse, &m_pMouseDevice, nullptr);
    if (FAILED(hr))
    {
        std::cerr << "Failed to create mouse device: " << std::hex << hr << std::endl;
        return false;
    }

    // Set data format
    hr = m_pMouseDevice->SetDataFormat(&c_dfDIMouse);
    if (FAILED(hr))
    {
        std::cerr << "Failed to set mouse data format: " << std::hex << hr << std::endl;
        return false;
    }

    // Set cooperative level
    HWND hwnd = GetConsoleWindow(); // Use console window, or could be nullptr for background
    hr = m_pMouseDevice->SetCooperativeLevel(hwnd, DISCL_BACKGROUND | DISCL_NONEXCLUSIVE);
    if (FAILED(hr))
    {
        std::cerr << "Failed to set mouse cooperative level: " << std::hex << hr << std::endl;
        return false;
    }

    // Acquire the device
    hr = m_pMouseDevice->Acquire();
    if (FAILED(hr))
    {
        std::cerr << "Failed to acquire mouse device: " << std::hex << hr << std::endl;
        return false;
    }

    return true;
}

void InputDetection::Update()
{
    // Store previous states
    m_previousKeyStates = m_keyStates;

    // Update keyboard input
    UpdateKeyboardInput();

    // Update mouse input
    UpdateMouseInput();
}

void InputDetection::UpdateKeyboardInput()
{
    // Update all keys we might care about
    // Using GetKeyState for synchronous state checking (like Bongo Cat)

    // Letters A-Z
    for (int i = 'A'; i <= 'Z'; ++i)
    {
        m_keyStates[i] = (GetKeyState(i) & 0x8000) != 0;
    }

    // Numbers 0-9
    for (int i = '0'; i <= '9'; ++i)
    {
        m_keyStates[i] = (GetKeyState(i) & 0x8000) != 0;
    }

    // Special keys
    m_keyStates[VK_SPACE] = (GetKeyState(VK_SPACE) & 0x8000) != 0;
    m_keyStates[VK_RETURN] = (GetKeyState(VK_RETURN) & 0x8000) != 0;
    m_keyStates[VK_ESCAPE] = (GetKeyState(VK_ESCAPE) & 0x8000) != 0;
    m_keyStates[VK_TAB] = (GetKeyState(VK_TAB) & 0x8000) != 0;
    m_keyStates[VK_BACK] = (GetKeyState(VK_BACK) & 0x8000) != 0;
    m_keyStates[VK_SHIFT] = (GetKeyState(VK_SHIFT) & 0x8000) != 0;
    m_keyStates[VK_CONTROL] = (GetKeyState(VK_CONTROL) & 0x8000) != 0;
    m_keyStates[VK_MENU] = (GetKeyState(VK_MENU) & 0x8000) != 0;

    // Arrow keys
    m_keyStates[VK_LEFT] = (GetKeyState(VK_LEFT) & 0x8000) != 0;
    m_keyStates[VK_RIGHT] = (GetKeyState(VK_RIGHT) & 0x8000) != 0;
    m_keyStates[VK_UP] = (GetKeyState(VK_UP) & 0x8000) != 0;
    m_keyStates[VK_DOWN] = (GetKeyState(VK_DOWN) & 0x8000) != 0;

    // Function keys
    for (int i = VK_F1; i <= VK_F12; ++i)
    {
        m_keyStates[i] = (GetKeyState(i) & 0x8000) != 0;
    }
}

void InputDetection::UpdateMouseInput()
{
    if (!m_pMouseDevice)
        return;

    HRESULT hr = m_pMouseDevice->GetDeviceState(sizeof(DIMOUSESTATE), &m_mouseState);

    if (FAILED(hr))
    {
        // Try to reacquire if lost
        if (hr == DIERR_INPUTLOST || hr == DIERR_NOTACQUIRED)
        {
            m_pMouseDevice->Acquire();
        }
        return;
    }

    // Store previous mouse position and wheel state
    m_previousMousePosition = m_mousePosition;
    m_previousWheelDelta = m_mouseWheelDelta;

    // Update mouse position (get absolute position)
    POINT cursorPos;
    if (GetCursorPos(&cursorPos))
    {
        m_mousePosition.x = cursorPos.x;
        m_mousePosition.y = cursorPos.y;
    }

    // Calculate mouse movement
    m_mouseMovement.x = m_mousePosition.x - m_previousMousePosition.x;
    m_mouseMovement.y = m_mousePosition.y - m_previousMousePosition.y;

    // Update mouse wheel delta from DirectInput
    m_mouseWheelDelta = m_mouseState.lZ;

    // Update mouse button states (using DirectInput for precise timing)
    // Note: Could also use GetKeyState(VK_LBUTTON) etc. for consistency
    m_keyStates[VK_LBUTTON] = (m_mouseState.rgbButtons[0] & 0x80) != 0;
    m_keyStates[VK_RBUTTON] = (m_mouseState.rgbButtons[1] & 0x80) != 0;
    m_keyStates[VK_MBUTTON] = (m_mouseState.rgbButtons[2] & 0x80) != 0;

    // Support for additional mouse buttons (X buttons)
    if (m_mouseState.rgbButtons[3] & 0x80) // X Button 1
        m_keyStates[VK_XBUTTON1] = true;
    else
        m_keyStates[VK_XBUTTON1] = false;

    if (m_mouseState.rgbButtons[4] & 0x80) // X Button 2
        m_keyStates[VK_XBUTTON2] = true;
    else
        m_keyStates[VK_XBUTTON2] = false;
}

bool InputDetection::IsKeyPressed(const InputKey& key)
{
    int virtualKey = 0;

    // Priority: WinVK > HID > Evdev
    if (key.winvk != 0)
    {
        virtualKey = key.winvk;
    }
    else if (key.hid != 0)
    {
        virtualKey = ConvertHIDToVirtualKey(key.hid);
    }
    else if (key.evdev != 0)
    {
        virtualKey = ConvertEvdevToVirtualKey(key.evdev);
    }

    if (virtualKey == 0)
        return false;

    auto it = m_keyStates.find(virtualKey);
    return (it != m_keyStates.end()) ? it->second : false;
}

bool InputDetection::IsMouseButtonPressed(int button)
{
    switch (button)
    {
    case 1: return IsKeyPressed({1, VK_LBUTTON, 0, "left_mouse"});   // HID 1 = Left Button
    case 2: return IsKeyPressed({2, VK_RBUTTON, 0, "right_mouse"});  // HID 2 = Right Button
    case 3: return IsKeyPressed({3, VK_MBUTTON, 0, "middle_mouse"}); // HID 3 = Middle Button
    case 4: return IsKeyPressed({4, VK_XBUTTON2, 0, "xbutton2"});    // HID 4 = X Button 2
    case 5: return IsKeyPressed({5, VK_XBUTTON1, 0, "xbutton1"});    // HID 5 = X Button 1
    default: return false;
    }
}

Vector2i InputDetection::GetMousePosition()
{
    return m_mousePosition;
}

Vector2i InputDetection::GetMouseMovement()
{
    return m_mouseMovement;
}

int InputDetection::GetMouseWheelDelta()
{
    return m_mouseWheelDelta;
}

void InputDetection::Cleanup()
{
    Shutdown();
}

int InputDetection::ConvertHIDToVirtualKey(int hidCode)
{
    for (int i = 0; HID_VK_TABLE[i].hid != 0; ++i)
    {
        if (HID_VK_TABLE[i].hid == hidCode)
        {
            return HID_VK_TABLE[i].vk;
        }
    }
    return 0; // Not found
}

int InputDetection::ConvertEvdevToVirtualKey(int evdevCode)
{
    // Basic evdev to VK conversion
    // This is a simplified mapping - a full implementation would have more codes
    switch (evdevCode)
    {
    case 1: return VK_ESCAPE;
    case 2: return '1';
    case 3: return '2';
    case 4: return '3';
    case 5: return '4';
    case 6: return '5';
    case 7: return '6';
    case 8: return '7';
    case 9: return '8';
    case 10: return '9';
    case 11: return '0';
    case 16: return 'Q';
    case 17: return 'W';
    case 18: return 'E';
    case 19: return 'R';
    case 20: return 'T';
    case 21: return 'Y';
    case 22: return 'U';
    case 23: return 'I';
    case 24: return 'O';
    case 25: return 'P';
    case 30: return 'A';
    case 31: return 'S';
    case 32: return 'D';
    case 33: return 'F';
    case 34: return 'G';
    case 35: return 'H';
    case 36: return 'J';
    case 37: return 'K';
    case 38: return 'L';
    case 44: return 'Z';
    case 45: return 'X';
    case 46: return 'C';
    case 47: return 'V';
    case 48: return 'B';
    case 49: return 'N';
    case 50: return 'M';
    case 57: return VK_SPACE;
    case 103: return VK_UP;
    case 105: return VK_LEFT;
    case 106: return VK_RIGHT;
    case 108: return VK_DOWN;
    default: return 0;
    }
}