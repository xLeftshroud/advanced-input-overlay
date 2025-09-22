#include "../include/IPCManager.h"
#include <iostream>
#include <sstream>

IPCManager::IPCManager()
    : m_hPipe(INVALID_HANDLE_VALUE)
    , m_isConnected(false)
    , m_shouldStop(false)
{
}

IPCManager::~IPCManager()
{
    Shutdown();
}

bool IPCManager::Initialize()
{
    if (!CreateNamedPipe())
    {
        std::cerr << "Failed to create named pipe!" << std::endl;
        return false;
    }

    // Start worker threads
    m_readerThread = std::thread(&IPCManager::ReaderThreadFunc, this);
    m_writerThread = std::thread(&IPCManager::WriterThreadFunc, this);

    std::cout << "IPC Manager initialized. Waiting for UI connection..." << std::endl;
    return true;
}

void IPCManager::Shutdown()
{
    m_shouldStop = true;

    DisconnectPipe();

    if (m_readerThread.joinable())
        m_readerThread.join();

    if (m_writerThread.joinable())
        m_writerThread.join();
}

void IPCManager::Cleanup()
{
    Shutdown();
}

bool IPCManager::CreateNamedPipe()
{
    // First, try to clean up any existing pipe with the same name
    HANDLE testPipe = CreateFileA(
        PIPE_NAME.c_str(),
        GENERIC_READ | GENERIC_WRITE,
        0,
        NULL,
        OPEN_EXISTING,
        0,
        NULL
    );

    if (testPipe != INVALID_HANDLE_VALUE)
    {
        CloseHandle(testPipe);
        std::cout << "Warning: Pipe already exists, attempting to create anyway..." << std::endl;
    }

    m_hPipe = CreateNamedPipeA(
        PIPE_NAME.c_str(),
        PIPE_ACCESS_DUPLEX,
        PIPE_TYPE_MESSAGE | PIPE_READMODE_MESSAGE | PIPE_WAIT,
        PIPE_UNLIMITED_INSTANCES, // Allow multiple instances to prevent conflicts
        MAX_MESSAGE_SIZE,
        MAX_MESSAGE_SIZE,
        0, // Default timeout
        NULL // Default security
    );

    if (m_hPipe == INVALID_HANDLE_VALUE)
    {
        DWORD error = GetLastError();
        std::cerr << "CreateNamedPipe failed: " << error << std::endl;

        if (error == ERROR_ALREADY_EXISTS || error == ERROR_PIPE_BUSY)
        {
            std::cerr << "Pipe is already in use. Please ensure no other instances are running." << std::endl;
            std::cerr << "You may need to run: taskkill //f //im InputOverlayCore.exe" << std::endl;
        }

        return false;
    }

    return true;
}

void IPCManager::DisconnectPipe()
{
    if (m_hPipe != INVALID_HANDLE_VALUE)
    {
        DisconnectNamedPipe(m_hPipe);
        CloseHandle(m_hPipe);
        m_hPipe = INVALID_HANDLE_VALUE;
    }
    m_isConnected = false;
}

bool IPCManager::SendMessage(const IPCMessage& message)
{
    std::lock_guard<std::mutex> lock(m_outgoingMutex);
    m_outgoingMessages.push(message);
    return true;
}

bool IPCManager::ReceiveMessage(IPCMessage& message)
{
    std::lock_guard<std::mutex> lock(m_incomingMutex);
    if (m_incomingMessages.empty())
        return false;

    message = m_incomingMessages.front();
    m_incomingMessages.pop();
    return true;
}

void IPCManager::ReaderThreadFunc()
{
    char buffer[MAX_MESSAGE_SIZE];

    while (!m_shouldStop)
    {
        if (!m_isConnected)
        {
            // Wait for client connection
            if (ConnectNamedPipe(m_hPipe, NULL) || GetLastError() == ERROR_PIPE_CONNECTED)
            {
                m_isConnected = true;
                std::cout << "UI connected to IPC pipe." << std::endl;
            }
            else
            {
                Sleep(100);
                continue;
            }
        }

        DWORD bytesRead = 0;
        BOOL success = ReadFile(m_hPipe, buffer, MAX_MESSAGE_SIZE, &bytesRead, NULL);

        if (!success || bytesRead == 0)
        {
            DWORD error = GetLastError();
            if (error == ERROR_BROKEN_PIPE)
            {
                std::cout << "UI disconnected from IPC pipe." << std::endl;
                DisconnectNamedPipe(m_hPipe);
                m_isConnected = false;
                continue;
            }
            else
            {
                std::cerr << "ReadFile failed: " << error << std::endl;
                Sleep(100);
                continue;
            }
        }

        // Parse received message
        std::string messageData(buffer, bytesRead);
        IPCMessage message;
        if (DeserializeMessage(messageData, message))
        {
            std::lock_guard<std::mutex> lock(m_incomingMutex);
            m_incomingMessages.push(message);
        }
    }
}

void IPCManager::WriterThreadFunc()
{
    while (!m_shouldStop)
    {
        if (!m_isConnected)
        {
            Sleep(100);
            continue;
        }

        IPCMessage message;
        bool hasMessage = false;

        {
            std::lock_guard<std::mutex> lock(m_outgoingMutex);
            if (!m_outgoingMessages.empty())
            {
                message = m_outgoingMessages.front();
                m_outgoingMessages.pop();
                hasMessage = true;
            }
        }

        if (!hasMessage)
        {
            Sleep(10);
            continue;
        }

        std::string messageData = SerializeMessage(message);
        DWORD bytesWritten = 0;

        BOOL success = WriteFile(
            m_hPipe,
            messageData.c_str(),
            static_cast<DWORD>(messageData.length()),
            &bytesWritten,
            NULL
        );

        if (!success)
        {
            DWORD error = GetLastError();
            if (error == ERROR_BROKEN_PIPE)
            {
                std::cout << "UI disconnected during write." << std::endl;
                DisconnectNamedPipe(m_hPipe);
                m_isConnected = false;
            }
            else
            {
                std::cerr << "WriteFile failed: " << error << std::endl;
            }
        }
    }
}

std::string IPCManager::SerializeMessage(const IPCMessage& message)
{
    std::ostringstream oss;
    oss << static_cast<int>(message.type) << "|"
        << message.overlayId << "|"
        << (message.noBorders ? 1 : 0) << "|"
        << (message.topMost ? 1 : 0) << "|"
        << message.data.length() << "|"
        << message.data;
    return oss.str();
}

bool IPCManager::DeserializeMessage(const std::string& data, IPCMessage& message)
{
    std::istringstream iss(data);
    std::string token;

    try
    {
        // Parse type
        if (!std::getline(iss, token, '|')) return false;
        message.type = static_cast<IPCMessageType>(std::stoi(token));

        // Parse overlay ID
        if (!std::getline(iss, token, '|')) return false;
        message.overlayId = std::stoi(token);

        // Parse noBorders
        if (!std::getline(iss, token, '|')) return false;
        message.noBorders = (std::stoi(token) != 0);

        // Parse topMost
        if (!std::getline(iss, token, '|')) return false;
        message.topMost = (std::stoi(token) != 0);

        // Parse data length
        if (!std::getline(iss, token, '|')) return false;
        size_t dataLength = std::stoul(token);

        // Parse data
        std::string remainingData;
        std::getline(iss, remainingData);
        if (remainingData.length() != dataLength)
        {
            std::cerr << "Data length mismatch in IPC message" << std::endl;
            return false;
        }

        message.data = remainingData;
        return true;
    }
    catch (const std::exception& e)
    {
        std::cerr << "IPC message deserialization error: " << e.what() << std::endl;
        return false;
    }
}

// Protocol implementation
namespace IPCProtocol
{
    std::string EncodeMessage(const IPCMessage& message)
    {
        MessageHeader header;
        header.type = static_cast<uint32_t>(message.type);
        header.overlayId = static_cast<uint32_t>(message.overlayId);
        header.dataSize = static_cast<uint32_t>(message.data.length());

        std::string encoded;
        encoded.resize(HEADER_SIZE + header.dataSize);

        // Copy header
        memcpy(&encoded[0], &header, HEADER_SIZE);

        // Copy data
        if (header.dataSize > 0)
        {
            memcpy(&encoded[HEADER_SIZE], message.data.c_str(), header.dataSize);
        }

        return encoded;
    }

    bool DecodeMessage(const std::string& buffer, IPCMessage& message)
    {
        if (buffer.length() < HEADER_SIZE)
            return false;

        MessageHeader header;
        memcpy(&header, buffer.c_str(), HEADER_SIZE);

        message.type = static_cast<IPCMessageType>(header.type);
        message.overlayId = static_cast<int>(header.overlayId);

        if (header.dataSize > 0)
        {
            if (buffer.length() < HEADER_SIZE + header.dataSize)
                return false;

            message.data = buffer.substr(HEADER_SIZE, header.dataSize);
        }

        return true;
    }
}