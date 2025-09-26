#include "../include/Common.h"
#include "../include/InputDetection.h"
#include "../include/ConfigParser.h"
#include "../include/IPCManager.h"

using namespace std;

// Global variables
bool g_running = true;
InputDetection g_inputDetection;
ConfigParser g_configParser;
IPCManager g_ipcManager;

// Map to store active overlays (simplified)
std::map<int, OverlayConfig> g_overlayConfigs;

// Mouse state tracking
MouseEventData g_previousMouseState;
bool g_hasMouseOverlays = false;

void ProcessIPCMessage(const IPCMessage& message)
{
    switch (message.type)
    {
    case IPCMessageType::DISPLAY_ALL:
        cout << "Processing DISPLAY_ALL command" << endl;
        break;

    case IPCMessageType::CLOSE_ALL:
        cout << "Processing CLOSE_ALL command" << endl;
        break;

    case IPCMessageType::SHOW_OVERLAY:
        cout << "Processing SHOW_OVERLAY for ID: " << message.overlayId << endl;
        break;

    case IPCMessageType::CLOSE_OVERLAY:
        cout << "Processing CLOSE_OVERLAY for ID: " << message.overlayId << endl;
        break;

    case IPCMessageType::ADD_OVERLAY:
    {
        cout << "Processing ADD_OVERLAY for ID: " << message.overlayId << endl;

        // Try to parse the configuration
        OverlayConfig config;
        if (g_configParser.ParseFromString(message.data, config))
        {
            g_overlayConfigs[message.overlayId] = config;

            // Check if this overlay has cursor elements
            for (const auto& element : config.elements)
            {
                if (element.cursor.enabled)
                {
                    g_hasMouseOverlays = true;
                    break;
                }
            }

            cout << "Successfully added overlay configuration" << endl;
        }
        else
        {
            cout << "Failed to parse overlay configuration" << endl;
        }
        break;
    }

    case IPCMessageType::REMOVE_OVERLAY:
        cout << "Processing REMOVE_OVERLAY for ID: " << message.overlayId << endl;
        g_overlayConfigs.erase(message.overlayId);
        break;

    case IPCMessageType::UPDATE_OVERLAY:
        cout << "Processing UPDATE_OVERLAY for ID: " << message.overlayId << endl;
        break;

    case IPCMessageType::STATUS_UPDATE:
        cout << "Processing STATUS_UPDATE" << endl;
        break;

    default:
        cout << "Unknown IPC message type" << endl;
        break;
    }
}

void SendMouseEventUpdate()
{
    if (!g_hasMouseOverlays)
        return;

    // Get current mouse state
    MouseEventData currentMouseState;
    currentMouseState.position = g_inputDetection.GetMousePosition();
    currentMouseState.movement = g_inputDetection.GetMouseMovement();
    currentMouseState.wheelDelta = g_inputDetection.GetMouseWheelDelta();
    currentMouseState.leftButton = g_inputDetection.IsMouseButtonPressed(1);
    currentMouseState.rightButton = g_inputDetection.IsMouseButtonPressed(2);
    currentMouseState.middleButton = g_inputDetection.IsMouseButtonPressed(3);
    currentMouseState.xButton1 = g_inputDetection.IsMouseButtonPressed(5);
    currentMouseState.xButton2 = g_inputDetection.IsMouseButtonPressed(4);

    // Check if anything changed
    bool hasChanged =
        (currentMouseState.position.x != g_previousMouseState.position.x) ||
        (currentMouseState.position.y != g_previousMouseState.position.y) ||
        (currentMouseState.movement.x != 0) ||
        (currentMouseState.movement.y != 0) ||
        (currentMouseState.wheelDelta != 0) ||
        (currentMouseState.leftButton != g_previousMouseState.leftButton) ||
        (currentMouseState.rightButton != g_previousMouseState.rightButton) ||
        (currentMouseState.middleButton != g_previousMouseState.middleButton) ||
        (currentMouseState.xButton1 != g_previousMouseState.xButton1) ||
        (currentMouseState.xButton2 != g_previousMouseState.xButton2);

    if (hasChanged)
    {
        // Create a simple JSON representation of mouse data
        std::string mouseData = "{";
        mouseData += "\"position\":[" + std::to_string(currentMouseState.position.x) + "," + std::to_string(currentMouseState.position.y) + "],";
        mouseData += "\"movement\":[" + std::to_string(currentMouseState.movement.x) + "," + std::to_string(currentMouseState.movement.y) + "],";
        mouseData += "\"wheelDelta\":" + std::to_string(currentMouseState.wheelDelta) + ",";
        mouseData += "\"leftButton\":" + std::string(currentMouseState.leftButton ? "true" : "false") + ",";
        mouseData += "\"rightButton\":" + std::string(currentMouseState.rightButton ? "true" : "false") + ",";
        mouseData += "\"middleButton\":" + std::string(currentMouseState.middleButton ? "true" : "false") + ",";
        mouseData += "\"xButton1\":" + std::string(currentMouseState.xButton1 ? "true" : "false") + ",";
        mouseData += "\"xButton2\":" + std::string(currentMouseState.xButton2 ? "true" : "false");
        mouseData += "}";

        // Send mouse event message
        IPCMessage mouseMessage;
        mouseMessage.type = IPCMessageType::MOUSE_EVENT;
        mouseMessage.overlayId = 0; // Mouse events are global
        mouseMessage.data = mouseData;

        g_ipcManager.SendMessage(mouseMessage);

        // Update previous state
        g_previousMouseState = currentMouseState;
    }
}

int main()
{
    cout << INPUT_OVERLAY_VERSION << " - Starting Core Engine..." << endl;

    // Initialize input detection
    if (!g_inputDetection.Initialize())
    {
        cout << "Failed to initialize input detection!" << endl;
        return -1;
    }

    // Initialize IPC
    if (!g_ipcManager.Initialize())
    {
        cout << "Failed to initialize IPC manager!" << endl;
        return -1;
    }

    cout << "Core engine initialized successfully!" << endl;
    cout << "Waiting for IPC messages..." << endl;

    // Main loop
    while (g_running)
    {
        // Check for IPC messages
        IPCMessage message;
        if (g_ipcManager.ReceiveMessage(message))
        {
            ProcessIPCMessage(message);
        }

        // Update input detection
        g_inputDetection.Update();

        // Send mouse events if needed
        SendMouseEventUpdate();

        // Small delay to prevent high CPU usage
        Sleep(16); // ~60 FPS equivalent
    }

    cout << "Shutting down core engine..." << endl;

    g_inputDetection.Cleanup();
    g_ipcManager.Cleanup();

    return 0;
}