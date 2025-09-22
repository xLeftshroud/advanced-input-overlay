#pragma once

#include <iostream>
#include <vector>
#include <map>
#include <string>
#include <memory>
#include <windows.h>
#include <dinput.h>

// Version and application info
#define INPUT_OVERLAY_VERSION "Input Overlay v1.0.0"

// IPC Message Types
enum class IPCMessageType
{
    DISPLAY_ALL = 1,
    CLOSE_ALL = 2,
    SHOW_OVERLAY = 3,
    CLOSE_OVERLAY = 4,
    ADD_OVERLAY = 5,
    REMOVE_OVERLAY = 6,
    UPDATE_OVERLAY = 7,
    STATUS_UPDATE = 8
};

// Basic vector and rect structures (replacing SFML temporarily)
struct Vector2i
{
    int x = 0;
    int y = 0;
    Vector2i() = default;
    Vector2i(int x, int y) : x(x), y(y) {}
};

struct IntRect
{
    int left = 0;
    int top = 0;
    int width = 0;
    int height = 0;
    IntRect() = default;
    IntRect(int left, int top, int width, int height) : left(left), top(top), width(width), height(height) {}
};

struct Color
{
    unsigned char r = 0;
    unsigned char g = 0;
    unsigned char b = 0;
    unsigned char a = 255;
    Color() = default;
    Color(unsigned char r, unsigned char g, unsigned char b, unsigned char a = 255) : r(r), g(g), b(b), a(a) {}
    static const Color Transparent;
};

// Input key structure
struct InputKey
{
    int hid = 0;
    int winvk = 0;
    int evdev = 0;
    std::string id;
};

// Sprite information
struct SpriteInfo
{
    IntRect normal;
    IntRect pressed;
    bool hasPressedState = false;
};

// Overlay element
struct OverlayElement
{
    std::string id;
    InputKey key;
    Vector2i position;
    SpriteInfo sprite;
    int zOrder = 0;
    bool isPressed = false;
};

// Overlay configuration
struct OverlayConfig
{
    int version = 1;
    std::string textureFile;
    Vector2i textureSize;
    Vector2i canvasSize;
    Color backgroundColor = Color::Transparent;
    Vector2i defaultPressedOffset;
    std::vector<OverlayElement> elements;
};

// IPC Message structure
struct IPCMessage
{
    IPCMessageType type;
    int overlayId = 0;
    std::string data;
    bool noBorders = false;
    bool topMost = false;
};

// Global constants
const std::string PIPE_NAME = "\\\\.\\pipe\\InputOverlayPipe";
const int MAX_MESSAGE_SIZE = 4096;