#include "../include/Common.h"
#include "../include/InputDetection.h"
#include "../include/OverlayRenderer.h"
#include "../include/ConfigParser.h"
#include "../include/IPCManager.h"

using namespace std;

// Global variables
bool g_running = true;
InputDetection g_inputDetection;
OverlayRenderer g_overlayRenderer;
ConfigParser g_configParser;
IPCManager g_ipcManager;

// Map to store active overlays
std::map<int, std::unique_ptr<sf::RenderWindow>> g_overlayWindows;
std::map<int, OverlayConfig> g_overlayConfigs;

void ProcessIPCMessage(const IPCMessage& message)
{
    switch (message.type)
    {
    case IPCMessageType::DISPLAY_ALL:
        for (auto& [id, window] : g_overlayWindows)
        {
            if (window && !window->isOpen())
            {
                // Recreate window if closed
                const auto& config = g_overlayConfigs[id];
                window = g_overlayRenderer.CreateOverlayWindow(config, message.noBorders, message.topMost);
            }
        }
        break;

    case IPCMessageType::CLOSE_ALL:
        for (auto& [id, window] : g_overlayWindows)
        {
            if (window && window->isOpen())
            {
                window->close();
            }
        }
        break;

    case IPCMessageType::SHOW_OVERLAY:
        if (g_overlayWindows.find(message.overlayId) != g_overlayWindows.end())
        {
            auto& window = g_overlayWindows[message.overlayId];
            if (!window || !window->isOpen())
            {
                const auto& config = g_overlayConfigs[message.overlayId];
                window = g_overlayRenderer.CreateOverlayWindow(config, message.noBorders, message.topMost);
            }
        }
        break;

    case IPCMessageType::CLOSE_OVERLAY:
        if (g_overlayWindows.find(message.overlayId) != g_overlayWindows.end())
        {
            auto& window = g_overlayWindows[message.overlayId];
            if (window && window->isOpen())
            {
                window->close();
            }
        }
        break;

    case IPCMessageType::ADD_OVERLAY:
        {
            OverlayConfig config;
            if (g_configParser.ParseConfigFromJSON(message.data, config))
            {
                g_overlayConfigs[message.overlayId] = config;
                g_overlayWindows[message.overlayId] = nullptr; // Will be created when shown
            }
        }
        break;

    case IPCMessageType::REMOVE_OVERLAY:
        {
            auto windowIt = g_overlayWindows.find(message.overlayId);
            if (windowIt != g_overlayWindows.end())
            {
                if (windowIt->second && windowIt->second->isOpen())
                {
                    windowIt->second->close();
                }
                g_overlayWindows.erase(windowIt);
            }
            g_overlayConfigs.erase(message.overlayId);
        }
        break;
    }
}

int main()
{
    cout << INPUT_OVERLAY_VERSION << " Starting..." << endl;

    // Initialize components
    if (!g_inputDetection.Initialize())
    {
        cerr << "Failed to initialize input detection!" << endl;
        return 1;
    }

    if (!g_overlayRenderer.Initialize())
    {
        cerr << "Failed to initialize overlay renderer!" << endl;
        return 1;
    }

    if (!g_ipcManager.Initialize())
    {
        cerr << "Failed to initialize IPC manager!" << endl;
        return 1;
    }

    cout << "Input Overlay Core initialized successfully." << endl;

    // Main loop
    sf::Clock frameClock;
    const sf::Time frameTime = sf::seconds(1.0f / 60.0f); // 60 FPS

    while (g_running)
    {
        // Process IPC messages from UI
        IPCMessage message;
        while (g_ipcManager.ReceiveMessage(message))
        {
            ProcessIPCMessage(message);
        }

        // Update input detection
        g_inputDetection.Update();

        // Update overlays
        for (auto& [id, window] : g_overlayWindows)
        {
            if (!window || !window->isOpen())
                continue;

            // Handle window events
            sf::Event event;
            while (window->pollEvent(event))
            {
                if (event.type == sf::Event::Closed)
                {
                    window->close();
                    // Send status to UI
                    IPCMessage statusMsg;
                    statusMsg.type = IPCMessageType::STATUS_UPDATE;
                    statusMsg.overlayId = id;
                    statusMsg.data = "closed";
                    g_ipcManager.SendMessage(statusMsg);
                }
            }

            // Update element states based on input
            auto& config = g_overlayConfigs[id];
            for (auto& element : config.elements)
            {
                bool wasPressed = element.isPressed;
                element.isPressed = g_inputDetection.IsKeyPressed(element.key);

                // Send state change notification if needed
                if (element.isPressed != wasPressed)
                {
                    // Could send detailed state updates to UI here
                }
            }

            // Render overlay
            g_overlayRenderer.RenderOverlay(*window, config);
        }

        // Frame rate limiting
        sf::Time elapsed = frameClock.getElapsedTime();
        if (elapsed < frameTime)
        {
            sf::sleep(frameTime - elapsed);
        }
        frameClock.restart();

        // Check for shutdown signal
        if (GetAsyncKeyState(VK_ESCAPE) & 0x8000)
        {
            // Emergency exit with Escape key
            g_running = false;
        }
    }

    // Cleanup
    cout << "Shutting down Input Overlay Core..." << endl;

    // Close all overlay windows
    for (auto& [id, window] : g_overlayWindows)
    {
        if (window && window->isOpen())
        {
            window->close();
        }
    }

    g_ipcManager.Shutdown();
    g_inputDetection.Shutdown();

    return 0;
}