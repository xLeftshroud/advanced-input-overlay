#pragma once

#include "Common.h"
#include <fstream>
#include <sstream>

// Simple JSON parser for overlay configuration
// This is a lightweight implementation focused on our specific schema
class ConfigParser
{
public:
    ConfigParser();
    ~ConfigParser();

    bool ParseConfigFromFile(const std::string& filePath, OverlayConfig& config);
    bool ParseConfigFromJSON(const std::string& jsonString, OverlayConfig& config);
    bool ParseFromString(const std::string& jsonString, OverlayConfig& config); // Alias for compatibility
    bool SaveConfigToFile(const std::string& filePath, const OverlayConfig& config);
    std::string ConfigToJSON(const OverlayConfig& config);

private:
    // JSON parsing helpers
    std::string Trim(const std::string& str);
    std::string GetStringValue(const std::string& json, const std::string& key);
    int GetIntValue(const std::string& json, const std::string& key);
    std::vector<int> GetIntArray(const std::string& json, const std::string& key);
    std::string GetObjectValue(const std::string& json, const std::string& key);
    std::vector<std::string> GetArrayObjects(const std::string& json, const std::string& key);

    // Parsing specific sections
    bool ParseTexture(const std::string& textureJson, OverlayConfig& config);
    bool ParseCanvas(const std::string& canvasJson, OverlayConfig& config);
    bool ParseDefaults(const std::string& defaultsJson, OverlayConfig& config);
    bool ParseElements(const std::string& elementsJson, OverlayConfig& config);
    bool ParseElement(const std::string& elementJson, OverlayElement& element);
    bool ParseCodes(const std::string& codesJson, InputKey& key);
    bool ParseSprite(const std::string& spriteJson, SpriteInfo& sprite, const Vector2i& defaultOffset);

    // JSON generation helpers
    std::string IntArrayToJSON(const std::vector<int>& arr);
    std::string Vector2iToJSON(const Vector2i& vec);
    std::string IntRectToJSON(const IntRect& rect);
};

// Utility functions for JSON string manipulation
namespace JSONUtils
{
    std::string ExtractValue(const std::string& json, const std::string& key);
    std::string ExtractStringValue(const std::string& json, const std::string& key);
    int ExtractIntValue(const std::string& json, const std::string& key);
    std::vector<int> ExtractIntArray(const std::string& json, const std::string& key);
    bool FindKeyValue(const std::string& json, const std::string& key, std::string& value);
    std::vector<std::string> SplitArray(const std::string& arrayJson);
}