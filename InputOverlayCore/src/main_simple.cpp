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

        // Small delay to prevent high CPU usage
        Sleep(16); // ~60 FPS equivalent
    }

    cout << "Shutting down core engine..." << endl;

    g_inputDetection.Cleanup();
    g_ipcManager.Cleanup();

    return 0;
}