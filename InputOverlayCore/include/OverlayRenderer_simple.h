#pragma once

#include "Common.h"

class OverlayRenderer
{
public:
    OverlayRenderer();
    ~OverlayRenderer();

    bool Initialize();
    void Shutdown();

    // Simplified interface for testing (no actual rendering)
    bool CreateOverlayWindow(const OverlayConfig& config, bool noBorders, bool topMost);
    void RenderOverlay(int overlayId, const OverlayConfig& config);

private:
    std::map<std::string, bool> m_loadedTextures; // Track loaded textures (simplified)

    bool LoadTexture(const std::string& filePath);
    void DrawElement(const OverlayElement& element);
    void SetWindowProperties(bool noBorders, bool topMost);
};

// Window utility functions
namespace WindowUtils
{
    void MakeWindowClickThrough(HWND hwnd);
    void SetWindowTopMost(HWND hwnd, bool topMost);
    void RemoveWindowBorders(HWND hwnd);
}