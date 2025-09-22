#pragma once

#include "Common.h"

class InputDetection
{
public:
    InputDetection();
    ~InputDetection();

    bool Initialize();
    void Shutdown();
    void Update();

    bool IsKeyPressed(const InputKey& key);
    bool IsMouseButtonPressed(int button);
    Vector2i GetMousePosition();
    void Cleanup(); // Add missing cleanup method

private:
    // DirectInput for mouse
    LPDIRECTINPUT8 m_pDirectInput;
    LPDIRECTINPUTDEVICE8 m_pMouseDevice;
    DIMOUSESTATE m_mouseState;
    HINSTANCE m_hInstance;

    // State tracking
    std::map<int, bool> m_keyStates;
    std::map<int, bool> m_previousKeyStates;
    Vector2i m_mousePosition;

    // Private methods
    bool InitializeDirectInput();
    bool InitializeMouse();
    void UpdateKeyboardInput();
    void UpdateMouseInput();
    int ConvertHIDToVirtualKey(int hidCode);
    int ConvertEvdevToVirtualKey(int evdevCode);
};

// HID to Virtual Key conversion table (partial)
struct HIDToVKMapping
{
    int hid;
    int vk;
};

// Common HID mappings
const HIDToVKMapping HID_VK_TABLE[] = {
    {4, 'A'},   // HID_KEYBOARD_A
    {5, 'B'},   // HID_KEYBOARD_B
    {6, 'C'},   // HID_KEYBOARD_C
    {7, 'D'},   // HID_KEYBOARD_D
    {8, 'E'},   // HID_KEYBOARD_E
    {9, 'F'},   // HID_KEYBOARD_F
    {10, 'G'},  // HID_KEYBOARD_G
    {11, 'H'},  // HID_KEYBOARD_H
    {12, 'I'},  // HID_KEYBOARD_I
    {13, 'J'},  // HID_KEYBOARD_J
    {14, 'K'},  // HID_KEYBOARD_K
    {15, 'L'},  // HID_KEYBOARD_L
    {16, 'M'},  // HID_KEYBOARD_M
    {17, 'N'},  // HID_KEYBOARD_N
    {18, 'O'},  // HID_KEYBOARD_O
    {19, 'P'},  // HID_KEYBOARD_P
    {20, 'Q'},  // HID_KEYBOARD_Q
    {21, 'R'},  // HID_KEYBOARD_R
    {22, 'S'},  // HID_KEYBOARD_S
    {23, 'T'},  // HID_KEYBOARD_T
    {24, 'U'},  // HID_KEYBOARD_U
    {25, 'V'},  // HID_KEYBOARD_V
    {26, 'W'},  // HID_KEYBOARD_W
    {27, 'X'},  // HID_KEYBOARD_X
    {28, 'Y'},  // HID_KEYBOARD_Y
    {29, 'Z'},  // HID_KEYBOARD_Z
    {30, '1'},  // HID_KEYBOARD_1
    {31, '2'},  // HID_KEYBOARD_2
    {32, '3'},  // HID_KEYBOARD_3
    {33, '4'},  // HID_KEYBOARD_4
    {34, '5'},  // HID_KEYBOARD_5
    {35, '6'},  // HID_KEYBOARD_6
    {36, '7'},  // HID_KEYBOARD_7
    {37, '8'},  // HID_KEYBOARD_8
    {38, '9'},  // HID_KEYBOARD_9
    {39, '0'},  // HID_KEYBOARD_0
    {40, VK_RETURN},    // HID_KEYBOARD_ENTER
    {41, VK_ESCAPE},    // HID_KEYBOARD_ESCAPE
    {42, VK_BACK},      // HID_KEYBOARD_BACKSPACE
    {43, VK_TAB},       // HID_KEYBOARD_TAB
    {44, VK_SPACE},     // HID_KEYBOARD_SPACE
    {79, VK_RIGHT},     // HID_KEYBOARD_RIGHT_ARROW
    {80, VK_LEFT},      // HID_KEYBOARD_LEFT_ARROW
    {81, VK_DOWN},      // HID_KEYBOARD_DOWN_ARROW
    {82, VK_UP},        // HID_KEYBOARD_UP_ARROW
    {224, VK_LCONTROL}, // HID_KEYBOARD_LEFT_CONTROL
    {225, VK_LSHIFT},   // HID_KEYBOARD_LEFT_SHIFT
    {226, VK_LMENU},    // HID_KEYBOARD_LEFT_ALT
    {0, 0} // End marker
};