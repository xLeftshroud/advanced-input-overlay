#pragma once

#include "Common.h"
#include <queue>
#include <mutex>
#include <thread>

class IPCManager
{
public:
    IPCManager();
    ~IPCManager();

    bool Initialize();
    void Shutdown();
    void Cleanup(); // Add missing cleanup method

    bool SendMessage(const IPCMessage& message);
    bool ReceiveMessage(IPCMessage& message);

    bool IsConnected() const { return m_isConnected; }

private:
    HANDLE m_hPipe;
    bool m_isConnected;
    bool m_shouldStop;

    std::queue<IPCMessage> m_incomingMessages;
    std::queue<IPCMessage> m_outgoingMessages;
    std::mutex m_incomingMutex;
    std::mutex m_outgoingMutex;

    std::thread m_readerThread;
    std::thread m_writerThread;

    // Thread functions
    void ReaderThreadFunc();
    void WriterThreadFunc();

    // Message serialization
    std::string SerializeMessage(const IPCMessage& message);
    bool DeserializeMessage(const std::string& data, IPCMessage& message);

    // Pipe operations
    bool CreateNamedPipe();
    bool ConnectToPipe();
    void DisconnectPipe();
};

// C# IPC Interface - will be implemented in C# side
namespace IPCProtocol
{
    // Message format: [TYPE:4][ID:4][SIZE:4][DATA:SIZE]
    const size_t HEADER_SIZE = 12;

    struct MessageHeader
    {
        uint32_t type;
        uint32_t overlayId;
        uint32_t dataSize;
    };

    std::string EncodeMessage(const IPCMessage& message);
    bool DecodeMessage(const std::string& buffer, IPCMessage& message);
}