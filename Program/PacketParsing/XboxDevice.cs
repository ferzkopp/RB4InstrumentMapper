using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace RB4InstrumentMapper.Parsing
{
    internal enum BackendType
    {
        Pcap,
        Usb,
    }

    internal enum XboxResult
    {
        Success,
        Pending,
        Disconnected,
        InvalidMessage,
        UnsupportedDevice,
    }

    /// <summary>
    /// An Xbox device.
    /// </summary>
    internal class XboxDevice : IDisposable
    {
        /// <summary>
        /// The clients currently on the device.
        /// </summary>
        private readonly Dictionary<byte, XboxClient> clients = new Dictionary<byte, XboxClient>();

        private readonly int maxPacketSize;

        public MappingMode MappingMode { get; }
        public BackendType Backend { get; }
        public bool MapGuideButton { get; }

        public XboxDevice(MappingMode mode, BackendType backend) : this(mode, backend, mapGuide: false, 0)
        {
        }

        protected XboxDevice(MappingMode mode, BackendType backend, bool mapGuide, int maxPacket)
        {
            MappingMode = mode;
            Backend = backend;
            MapGuideButton = mapGuide;
            maxPacketSize = maxPacket;
        }

        ~XboxDevice()
        {
            Dispose(false);
        }

        /// <summary>
        /// Handles an incoming packet for this device.
        /// </summary>
        public unsafe XboxResult HandlePacket(XboxPacket packet)
        {
            // Debugging (if enabled)
            PacketLogging.LogPacket(packet);

            // Some devices may send multiple messages in a single packet, placing them back-to-back
            // The header length is very important in these scenarios, as it determines which bytes are part of the message
            // and where the next message's header begins.
            var data = packet.Data;
            while (!data.IsEmpty)
            {
                // Command header
                if (!XboxCommandHeader.TryParse(data, out var header, out int headerLength))
                {
                    return XboxResult.InvalidMessage;
                }
                int messageLength = headerLength + header.DataLength;

                // Verify bounds
                if (data.Length < messageLength)
                {
                    return XboxResult.InvalidMessage;
                }

                var messageData = data.Slice(0, messageLength);
                var commandData = messageData.Slice(headerLength);

                if (!clients.TryGetValue(header.Client, out var client))
                {
                    client = new XboxClient(this, header.Client);
                    clients.Add(header.Client, client);
                }

                var clientResult = client.HandleMessage(header, commandData);
                switch (clientResult)
                {
                    case XboxResult.Success:
                    case XboxResult.Pending:
                        break;
                    case XboxResult.UnsupportedDevice:
                        client.Dispose();
                        clients.Remove(header.Client);
                        if (header.Client == 0)
                            return clientResult;
                        break;
                    case XboxResult.Disconnected:
                        client.Dispose();
                        clients.Remove(header.Client);
                        PacketLogging.PrintMessage($"Client {client.Arrival.SerialNumber:X12} disconnected");
                        break;
                    default:
                        PacketLogging.PrintVerboseError($"Error handling message: {clientResult}");
                        break;
                }

                // Progress to next message
                data = data.Slice(messageData.Length);
            }

            return XboxResult.Success;
        }

        internal unsafe XboxResult SendMessage(XboxMessage message)
        {
            return SendMessage(message.Header, message.Data);
        }

        internal unsafe XboxResult SendMessage<T>(XboxMessage<T> message)
            where T : unmanaged
        {
            return SendMessage(message.Header, ref message.Data);
        }

        internal unsafe XboxResult SendMessage(XboxCommandHeader header)
        {
            return SendMessage(header, Span<byte>.Empty);
        }

        internal unsafe XboxResult SendMessage<T>(XboxCommandHeader header, ref T data)
            where T : unmanaged
        {
            // Create a byte buffer for the given data
            var writeBuffer = new Span<byte>(Unsafe.AsPointer(ref data), sizeof(T));
            return SendMessage(header, writeBuffer);
        }

        // TODO: Span instead of ReadOnlySpan since the WinUSB lib doesn't use ReadOnlySpan for writing atm
        internal XboxResult SendMessage(XboxCommandHeader header, Span<byte> data)
        {
            // For devices handled by Pcap and not over USB
            if (maxPacketSize < XboxCommandHeader.MinimumByteLength)
                return XboxResult.Success;

            // Initialize lengths
            header.DataLength = data.Length;
            header.ChunkIndex = 0;
            int packetLength = header.GetByteLength() + data.Length;

            // Chunked messages
            if (packetLength > maxPacketSize)
            {
                // Sending chunked messages isn't supported currently, as we never need to send one
                Debug.Fail($"Message is too long! Max packet length: {maxPacketSize}, message size: {packetLength}");
                return XboxResult.InvalidMessage;
            }

            // Create buffer and send it
            Span<byte> packetBuffer = stackalloc byte[packetLength];
            if (!header.TryWriteToBuffer(packetBuffer, out int bytesWritten) ||
                !data.TryCopyTo(packetBuffer.Slice(bytesWritten)))
            {
                Debug.Fail("Failed to create packet buffer!");
                return XboxResult.InvalidMessage;
            }

            var xboxPacket = new XboxPacket(packetBuffer, directionIn: false);
            PacketLogging.LogPacket(xboxPacket);
            return SendPacket(packetBuffer);
        }

        protected virtual XboxResult SendPacket(Span<byte> data)
        {
            // No-op by default, for Pcap
            return XboxResult.Success;
        }

        /// <summary>
        /// Performs cleanup for the device.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                ReleaseManagedResources();
            }
        }

        protected virtual void ReleaseManagedResources()
        {
            foreach (var client in clients.Values)
            {
                client.Dispose();
            }
        }
    }
}