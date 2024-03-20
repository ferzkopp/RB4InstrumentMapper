using System;
using System.IO;
using RB4InstrumentMapper.Vigem;
using RB4InstrumentMapper.Vjoy;

namespace RB4InstrumentMapper.Parsing
{
    /// <summary>
    /// Replays packet log files to help with debugging packet handling issues.
    /// </summary>
    /// <remarks>
    /// Since it's only really meant for debugging, this replayer has a number
    /// of limitations and is not meant for general use:<br/>
    /// - It can only handle one device at a time.
    /// - Arrival and descriptor messages must be present in the log.
    /// </remarks>
    public static class ReplayBackend
    {
        public static bool ReplayLog(string logPath)
        {
            if (!File.Exists(logPath))
            {
                Console.WriteLine($"File not found: {logPath}");
                return false;
            }

            MappingMode mappingMode;
            if (VigemClient.TryInitialize())
            {
                mappingMode = MappingMode.ViGEmBus;
            }
            else if (VjoyClient.Enabled)
            {
                mappingMode = MappingMode.vJoy;
            }
            else
            {
                Console.WriteLine("No controller emulators available! Please make sure ViGEmBus and/or vJoy is installed.");
                return false;
            }

            Console.WriteLine($"Using mapping mode {mappingMode}");

            string[] lines = File.ReadAllLines(logPath);
            var device = new XboxDevice(mappingMode, BackendType.Replay);
            foreach (string line in lines)
            {
                // Stop if the device has been removed
                if (device == null)
                    break;

                // Remove any comments
                int spanEnd = line.IndexOf("//");
                if (spanEnd < 0)
                    spanEnd = line.Length;

                var lineSpan = line.AsSpan().Slice(0, spanEnd).Trim();
                if (lineSpan.IsEmpty)
                    continue;

                if (!XboxPacket.TryParse(lineSpan, out var packet))
                {
                    Console.WriteLine($"Couldn't parse line: {line}");
                    continue;
                }

                Console.WriteLine($"Processing line: {line}");
                try
                {
                retry:
                    var result = device.HandlePacket(packet);
                    switch (result)
                    {
                        case XboxResult.Success:
                            break;
                        case XboxResult.Disconnected:
                            device.Dispose();
                            device = null;
                            Console.WriteLine("Device was disconnected");
                            break;

                        case XboxResult.Reconnected:
                            device.Dispose();
                            device = new XboxDevice(mappingMode, BackendType.Replay);
                            Console.WriteLine("Device was reconnected");
                            goto retry;

                        case XboxResult.UnsupportedDevice:
                            device.Dispose();
                            device = null;
                            Console.WriteLine("Unsupported device");
                            break;

                        default:
                            Console.WriteLine($"Unhandled device result: {result}");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error while handling line: {ex}");
                    continue;
                }
            }

            device?.Dispose();
            return true;
        }
    }
}