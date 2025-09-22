#include "../include/OverlayRenderer.h"
#include <iostream>
#include <algorithm>

OverlayRenderer::OverlayRenderer()
{
}

OverlayRenderer::~OverlayRenderer()
{
    Shutdown();
}

bool OverlayRenderer::Initialize()
{
    // Load default debug font (optional)
    // For production, you might want to embed a font or load from system fonts

    std::cout << "Overlay renderer initialized." << std::endl;
    return true;
}

void OverlayRenderer::Shutdown()
{
    m_textures.clear();
}

std::unique_ptr<sf::RenderWindow> OverlayRenderer::CreateOverlayWindow(const OverlayConfig& config, bool noBorders, bool topMost)
{
    // Create window with appropriate style
    sf::Uint32 style = sf::Style::None; // Start with no decorations
    if (!noBorders)
    {
        style = sf::Style::Titlebar | sf::Style::Close; // Add title bar if borders requested
    }

    auto window = std::make_unique<sf::RenderWindow>(
        sf::VideoMode(config.canvasSize.x, config.canvasSize.y),
        "Input Overlay",
        style
    );

    if (!window->isOpen())
    {
        std::cerr << "Failed to create overlay window!" << std::endl;
        return nullptr;
    }

    // Set window properties
    SetWindowProperties(*window, noBorders, topMost);

    // Load texture if specified
    if (!config.textureFile.empty())
    {
        LoadTexture(config.textureFile);
    }

    // Set background color
    window->clear(config.backgroundColor);

    return window;
}

void OverlayRenderer::RenderOverlay(sf::RenderWindow& window, const OverlayConfig& config)
{
    if (!window.isOpen())
        return;

    // Clear with background color
    window.clear(config.backgroundColor);

    // Get texture for rendering
    sf::Texture* texture = GetTexture(config.textureFile);
    if (!texture)
    {
        // No texture available, just display the window
        window.display();
        return;
    }

    // Sort elements by z-order
    auto sortedElements = config.elements;
    std::sort(sortedElements.begin(), sortedElements.end(),
        [](const OverlayElement& a, const OverlayElement& b) {
            return a.zOrder < b.zOrder;
        });

    // Render all elements
    for (const auto& element : sortedElements)
    {
        DrawElement(window, element, *texture);
    }

    window.display();
}

bool OverlayRenderer::LoadTexture(const std::string& filePath)
{
    if (m_textures.find(filePath) != m_textures.end())
    {
        return true; // Already loaded
    }

    sf::Texture texture;
    if (!texture.loadFromFile(filePath))
    {
        std::cerr << "Failed to load texture: " << filePath << std::endl;
        return false;
    }

    m_textures[filePath] = std::move(texture);
    std::cout << "Loaded texture: " << filePath << std::endl;
    return true;
}

sf::Texture* OverlayRenderer::GetTexture(const std::string& filePath)
{
    auto it = m_textures.find(filePath);
    return (it != m_textures.end()) ? &it->second : nullptr;
}

void OverlayRenderer::DrawElement(sf::RenderWindow& window, const OverlayElement& element, const sf::Texture& texture)
{
    sf::Sprite sprite(texture);

    // Choose sprite rect based on pressed state
    sf::IntRect rect = element.sprite.normal;
    if (element.isPressed && element.sprite.hasPressedState)
    {
        rect = element.sprite.pressed;
    }

    sprite.setTextureRect(rect);
    sprite.setPosition(static_cast<float>(element.position.x), static_cast<float>(element.position.y));

    window.draw(sprite);
}

void OverlayRenderer::SetWindowProperties(sf::RenderWindow& window, bool noBorders, bool topMost)
{
    HWND hwnd = window.getSystemHandle();

    if (noBorders)
    {
        WindowUtils::RemoveWindowBorders(hwnd);
        WindowUtils::MakeWindowClickThrough(hwnd);
    }

    if (topMost)
    {
        WindowUtils::SetWindowTopMost(hwnd, true);
    }
}

// Window utility functions implementation
namespace WindowUtils
{
    void MakeWindowClickThrough(HWND hwnd)
    {
        // Make window transparent to mouse clicks
        LONG_PTR exStyle = GetWindowLongPtr(hwnd, GWL_EXSTYLE);
        SetWindowLongPtr(hwnd, GWL_EXSTYLE, exStyle | WS_EX_TRANSPARENT | WS_EX_LAYERED);
    }

    void SetWindowTopMost(HWND hwnd, bool topMost)
    {
        HWND insertAfter = topMost ? HWND_TOPMOST : HWND_NOTOPMOST;
        SetWindowPos(hwnd, insertAfter, 0, 0, 0, 0,
                     SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
    }

    void RemoveWindowBorders(HWND hwnd)
    {
        // Remove title bar and borders
        LONG_PTR style = GetWindowLongPtr(hwnd, GWL_STYLE);
        style &= ~(WS_CAPTION | WS_THICKFRAME | WS_MINIMIZEBOX | WS_MAXIMIZEBOX | WS_SYSMENU);
        SetWindowLongPtr(hwnd, GWL_STYLE, style);

        // Update window to apply changes
        SetWindowPos(hwnd, NULL, 0, 0, 0, 0,
                     SWP_FRAMECHANGED | SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_NOOWNERZORDER);
    }
}