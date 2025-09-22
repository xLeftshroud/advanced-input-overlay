#include "../include/ConfigParser.h"
#include <iostream>
#include <algorithm>
#include <regex>

ConfigParser::ConfigParser()
{
}

ConfigParser::~ConfigParser()
{
}

bool ConfigParser::ParseConfigFromFile(const std::string& filePath, OverlayConfig& config)
{
    std::ifstream file(filePath);
    if (!file.is_open())
    {
        std::cerr << "Failed to open config file: " << filePath << std::endl;
        return false;
    }

    std::stringstream buffer;
    buffer << file.rdbuf();
    std::string jsonContent = buffer.str();
    file.close();

    return ParseConfigFromJSON(jsonContent, config);
}

bool ConfigParser::ParseFromString(const std::string& jsonString, OverlayConfig& config)
{
    return ParseConfigFromJSON(jsonString, config);
}

bool ConfigParser::ParseConfigFromJSON(const std::string& jsonString, OverlayConfig& config)
{
    try
    {
        // Parse version
        config.version = JSONUtils::ExtractIntValue(jsonString, "version");
        if (config.version == 0) config.version = 1; // Default

        // Parse texture section
        std::string textureJson = JSONUtils::ExtractValue(jsonString, "texture");
        if (!textureJson.empty())
        {
            ParseTexture(textureJson, config);
        }

        // Parse canvas section
        std::string canvasJson = JSONUtils::ExtractValue(jsonString, "canvas");
        if (!canvasJson.empty())
        {
            ParseCanvas(canvasJson, config);
        }

        // Parse defaults section
        std::string defaultsJson = JSONUtils::ExtractValue(jsonString, "defaults");
        if (!defaultsJson.empty())
        {
            ParseDefaults(defaultsJson, config);
        }

        // Parse elements array
        std::string elementsJson = JSONUtils::ExtractValue(jsonString, "elements");
        if (!elementsJson.empty())
        {
            ParseElements(elementsJson, config);
        }

        return true;
    }
    catch (const std::exception& e)
    {
        std::cerr << "JSON parsing error: " << e.what() << std::endl;
        return false;
    }
}

bool ConfigParser::ParseTexture(const std::string& textureJson, OverlayConfig& config)
{
    config.textureFile = JSONUtils::ExtractStringValue(textureJson, "file");

    auto sizeArray = JSONUtils::ExtractIntArray(textureJson, "size");
    if (sizeArray.size() >= 2)
    {
        config.textureSize.x = sizeArray[0];
        config.textureSize.y = sizeArray[1];
    }

    return true;
}

bool ConfigParser::ParseCanvas(const std::string& canvasJson, OverlayConfig& config)
{
    auto sizeArray = JSONUtils::ExtractIntArray(canvasJson, "size");
    if (sizeArray.size() >= 2)
    {
        config.canvasSize.x = sizeArray[0];
        config.canvasSize.y = sizeArray[1];
    }

    auto bgArray = JSONUtils::ExtractIntArray(canvasJson, "background");
    if (bgArray.size() >= 4)
    {
        config.backgroundColor = Color(
            static_cast<unsigned char>(bgArray[0]),
            static_cast<unsigned char>(bgArray[1]),
            static_cast<unsigned char>(bgArray[2]),
            static_cast<unsigned char>(bgArray[3])
        );
    }

    return true;
}

bool ConfigParser::ParseDefaults(const std::string& defaultsJson, OverlayConfig& config)
{
    auto offsetArray = JSONUtils::ExtractIntArray(defaultsJson, "pressed_offset");
    if (offsetArray.size() >= 2)
    {
        config.defaultPressedOffset.x = offsetArray[0];
        config.defaultPressedOffset.y = offsetArray[1];
    }

    return true;
}

bool ConfigParser::ParseElements(const std::string& elementsJson, OverlayConfig& config)
{
    // Remove array brackets and split by elements
    std::string cleanJson = elementsJson;
    if (cleanJson.front() == '[') cleanJson.erase(0, 1);
    if (cleanJson.back() == ']') cleanJson.pop_back();

    auto elementStrings = JSONUtils::SplitArray(cleanJson);

    for (const auto& elementStr : elementStrings)
    {
        OverlayElement element;
        if (ParseElement(elementStr, element))
        {
            config.elements.push_back(element);
        }
    }

    return true;
}

bool ConfigParser::ParseElement(const std::string& elementJson, OverlayElement& element)
{
    element.id = JSONUtils::ExtractStringValue(elementJson, "id");

    // Parse codes
    std::string codesJson = JSONUtils::ExtractValue(elementJson, "codes");
    ParseCodes(codesJson, element.key);

    // Parse position
    auto posArray = JSONUtils::ExtractIntArray(elementJson, "pos");
    if (posArray.size() >= 2)
    {
        element.position.x = posArray[0];
        element.position.y = posArray[1];
    }

    // Parse sprite
    std::string spriteJson = JSONUtils::ExtractValue(elementJson, "sprite");
    ParseSprite(spriteJson, element.sprite, Vector2i(0, 0)); // Will use global default if needed

    // Parse z-order
    element.zOrder = JSONUtils::ExtractIntValue(elementJson, "z");

    return true;
}

bool ConfigParser::ParseCodes(const std::string& codesJson, InputKey& key)
{
    key.hid = JSONUtils::ExtractIntValue(codesJson, "hid");
    key.winvk = JSONUtils::ExtractIntValue(codesJson, "winvk");
    key.evdev = JSONUtils::ExtractIntValue(codesJson, "evdev");

    return true;
}

bool ConfigParser::ParseSprite(const std::string& spriteJson, SpriteInfo& sprite, const Vector2i& defaultOffset)
{
    // Parse normal sprite rect
    auto normalArray = JSONUtils::ExtractIntArray(spriteJson, "normal");
    if (normalArray.size() >= 4)
    {
        sprite.normal = IntRect(normalArray[0], normalArray[1], normalArray[2], normalArray[3]);
    }

    // Parse pressed sprite rect (optional)
    auto pressedArray = JSONUtils::ExtractIntArray(spriteJson, "pressed");
    if (pressedArray.size() >= 4)
    {
        sprite.pressed = IntRect(pressedArray[0], pressedArray[1], pressedArray[2], pressedArray[3]);
        sprite.hasPressedState = true;
    }
    else if (defaultOffset.x != 0 || defaultOffset.y != 0)
    {
        // Use default offset
        sprite.pressed = IntRect(
            sprite.normal.left + defaultOffset.x,
            sprite.normal.top + defaultOffset.y,
            sprite.normal.width,
            sprite.normal.height
        );
        sprite.hasPressedState = true;
    }

    return true;
}

// JSON Utility Functions
namespace JSONUtils
{
    std::string ExtractValue(const std::string& json, const std::string& key)
    {
        std::string value;
        if (FindKeyValue(json, key, value))
        {
            return value;
        }
        return "";
    }

    std::string ExtractStringValue(const std::string& json, const std::string& key)
    {
        std::string value = ExtractValue(json, key);
        if (!value.empty() && value.front() == '"' && value.back() == '"')
        {
            value = value.substr(1, value.length() - 2); // Remove quotes
        }
        return value;
    }

    int ExtractIntValue(const std::string& json, const std::string& key)
    {
        std::string value = ExtractValue(json, key);
        try
        {
            return std::stoi(value);
        }
        catch (...)
        {
            return 0;
        }
    }

    std::vector<int> ExtractIntArray(const std::string& json, const std::string& key)
    {
        std::vector<int> result;
        std::string value = ExtractValue(json, key);

        if (value.empty()) return result;

        // Remove array brackets
        if (value.front() == '[') value.erase(0, 1);
        if (value.back() == ']') value.pop_back();

        // Split by commas and parse integers
        std::stringstream ss(value);
        std::string item;
        while (std::getline(ss, item, ','))
        {
            item.erase(std::remove_if(item.begin(), item.end(), ::isspace), item.end());
            try
            {
                result.push_back(std::stoi(item));
            }
            catch (...)
            {
                // Skip invalid numbers
            }
        }

        return result;
    }

    bool FindKeyValue(const std::string& json, const std::string& key, std::string& value)
    {
        // Find the key in quotes
        std::string searchKey = "\"" + key + "\"";
        size_t keyPos = json.find(searchKey);

        if (keyPos == std::string::npos)
            return false;

        // Find the colon after the key
        size_t colonPos = json.find(":", keyPos);
        if (colonPos == std::string::npos)
            return false;

        // Skip whitespace after colon
        size_t valueStart = colonPos + 1;
        while (valueStart < json.length() && std::isspace(json[valueStart]))
            valueStart++;

        if (valueStart >= json.length())
            return false;

        // Determine value type and extract accordingly
        char firstChar = json[valueStart];
        size_t valueEnd;

        if (firstChar == '"')
        {
            // String value
            valueEnd = json.find('"', valueStart + 1);
            if (valueEnd == std::string::npos)
                return false;
            value = json.substr(valueStart, valueEnd - valueStart + 1);
        }
        else if (firstChar == '[')
        {
            // Array value
            int bracketCount = 0;
            valueEnd = valueStart;
            do
            {
                if (json[valueEnd] == '[') bracketCount++;
                else if (json[valueEnd] == ']') bracketCount--;
                valueEnd++;
            } while (valueEnd < json.length() && bracketCount > 0);

            value = json.substr(valueStart, valueEnd - valueStart);
        }
        else if (firstChar == '{')
        {
            // Object value
            int braceCount = 0;
            valueEnd = valueStart;
            do
            {
                if (json[valueEnd] == '{') braceCount++;
                else if (json[valueEnd] == '}') braceCount--;
                valueEnd++;
            } while (valueEnd < json.length() && braceCount > 0);

            value = json.substr(valueStart, valueEnd - valueStart);
        }
        else
        {
            // Number or boolean value
            valueEnd = valueStart;
            while (valueEnd < json.length() &&
                   json[valueEnd] != ',' &&
                   json[valueEnd] != '}' &&
                   json[valueEnd] != ']' &&
                   !std::isspace(json[valueEnd]))
            {
                valueEnd++;
            }
            value = json.substr(valueStart, valueEnd - valueStart);
        }

        return true;
    }

    std::vector<std::string> SplitArray(const std::string& arrayJson)
    {
        std::vector<std::string> result;

        if (arrayJson.empty())
            return result;

        size_t pos = 0;
        int braceCount = 0;
        size_t start = 0;

        while (pos < arrayJson.length())
        {
            char c = arrayJson[pos];

            if (c == '{')
            {
                braceCount++;
            }
            else if (c == '}')
            {
                braceCount--;
                if (braceCount == 0)
                {
                    // Found complete object
                    std::string obj = arrayJson.substr(start, pos - start + 1);
                    result.push_back(obj);

                    // Find next object start
                    pos++;
                    while (pos < arrayJson.length() && (arrayJson[pos] == ',' || std::isspace(arrayJson[pos])))
                        pos++;
                    start = pos;
                    continue;
                }
            }

            pos++;
        }

        return result;
    }
}