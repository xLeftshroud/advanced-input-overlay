#pragma once

#include "Common.h"

class OverlayRenderer
{
public:
    OverlayRenderer();
    ~OverlayRenderer();

    bool Initialize();
    void Shutdown();

    std::unique_ptr<sf::RenderWindow> CreateOverlayWindow(const OverlayConfig& config, bool noBorders, bool topMost);
    void RenderOverlay(sf::RenderWindow& window, const OverlayConfig& config);

private:
    std::map<std::string, sf::Texture> m_textures;
    sf::Font m_debugFont;

    bool LoadTexture(const std::string& filePath);
    sf::Texture* GetTexture(const std::string& filePath);
    void DrawElement(sf::RenderWindow& window, const OverlayElement& element, const sf::Texture& texture);
    void SetWindowProperties(sf::RenderWindow& window, bool noBorders, bool topMost);
};

// Window utility functions
namespace WindowUtils
{
    void MakeWindowClickThrough(HWND hwnd);
    void SetWindowTopMost(HWND hwnd, bool topMost);
    void RemoveWindowBorders(HWND hwnd);
}