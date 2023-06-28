using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace RB4InstrumentMapper.Parsing
{
    /// <summary>
    /// The descriptor data of an Xbox One device.
    /// A large amount of the descriptor data is ignored, only data necessary for identifying device types is read.
    /// </summary>
    public class XboxDescriptor
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct Header
        {
            public ushort HeaderLength;
            public int unk1;
            public ulong unk2;
            public ushort DataLength;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct Offsets
        {
            public ushort CustomCommands;
            public ushort FirmwareVersions;
            public ushort AudioFormats;
            public ushort OutputCommands;
            public ushort InputCommands;
            public ushort ClassNames;
            public ushort InterfaceGuids;
            public ushort HidDescriptor;
            public ushort unk1;
            public ushort unk2;
            public ushort unk3;
        }

        public IReadOnlyList<string> ClassNames { get; private set; }
        public IReadOnlyList<Guid> InterfaceGuids { get; private set; }

        public static bool Parse(ReadOnlySpan<byte> data, out XboxDescriptor descriptor)
        {
            descriptor = new XboxDescriptor();
            return descriptor.Parse(data);
        }

        private unsafe bool Parse(ReadOnlySpan<byte> data)
        {
            if (data.IsEmpty)
                throw new ArgumentNullException(nameof(data));

            // Descriptor header size
            if (data.Length < sizeof(ushort)
                || !MemoryMarshal.TryRead(data, out ushort headerSize))
            {
                Debug.Fail($"Couldn't parse descriptor header size!  Buffer size: {data.Length}, element size: {sizeof(ushort)}");
                return false;
            }

            // Expecting a certain size
            if (headerSize != sizeof(Header))
            {
                Debug.Fail($"Header size does not match expected size!  Expected: {sizeof(Header)}, actual: {headerSize}");
                return false;
            }

            // Descriptor header
            if (data.Length < headerSize
                || !MemoryMarshal.TryRead(data, out Header header))
            {
                Debug.Fail($"Couldn't parse descriptor header!  Buffer size: {data.Length}, header size: {headerSize}");
                return false;
            }

            // Verify buffer size
            if (data.Length < header.DataLength)
            {
                Debug.Fail($"Buffer size is smaller than size listed in header!  Buffer size: {data.Length}, listed size: {header.DataLength}");
                return false;
            }
            Debug.WriteLineIf(data.Length != header.DataLength, $"Buffer size is not the same as size listed in header!  Buffer size: {data.Length}, listed size: {header.DataLength}");
            data = data.Slice(header.HeaderLength);

            // Data offsets
            if (data.Length < sizeof(Offsets)
                || !MemoryMarshal.TryRead(data, out Offsets offsets))
            {
                Debug.Fail($"Couldn't parse descriptor offsets!  Buffer size: {data.Length}, offsets size: {sizeof(Offsets)}");
                return false;
            }
            // No slice, offsets are relative to the start of the offsets block

            // Data elements
            ClassNames = ParseStrings(data, offsets.ClassNames, nameof(ClassNames));
            InterfaceGuids = ParseElements<Guid>(data, offsets.InterfaceGuids, nameof(InterfaceGuids));

            return true;
        }

        private static bool VerifyOffset(ReadOnlySpan<byte> buffer, ushort offset, int elementSize, out byte count, string elementName)
        {
            // Null offset means no elements are available
            if (offset == 0)
            {
                count = 0;
                return false;
            }

            // Ensure offset is within bounds
            if (buffer.Length <= offset)
            {
                Debug.Fail($"Offset of {elementName} is greater than size of buffer!  Offset: {offset}; Buffer size: {buffer.Length}");
                count = 0;
                return false;
            }

            // Get number of elements
            count = buffer[offset];
            // Zero count means no elements are available
            if (count == 0)
            {
                return false;
            }

            // Element size of 0 is used for variable-length types like strings
            if (elementSize == 0)
            {
                // Can't verify bounds here, everything else checks out so treat it as valid
                return true;
            }

            // Ensure total size of elements is within bounds
            var fromElements = buffer.Slice(offset);
            int elementsSize = elementSize * count;
            if (fromElements.Length < elementsSize)
            {
                Debug.Fail($"Size of {elementName} is greater than size of buffer from offset!  Offset: {offset:X4}; Count: {count}; Element size: {elementSize}; Total length of elements: {elementsSize}; Buffer length from offset: {fromElements.Length}");
                count = 0;
                return false;
            }

            // Offset is valid
            return true;
        }

        private static unsafe T[] ParseElements<T>(ReadOnlySpan<byte> buffer, ushort offset, string elementName, Func<T, bool> customCheck = null)
            where T : unmanaged
        {
            if (!VerifyOffset(buffer, offset, sizeof(T), out byte count, elementName) || count == 0)
            {
                return null;
            }

            // Get data bounds
            buffer = buffer.Slice(offset + sizeof(byte), count * sizeof(T));
            // Get element data
            if (customCheck == null)
            {
                // No checks, get everything at once
                return MemoryMarshal.Cast<byte, T>(buffer).ToArray();
            }

            // Checks required, go through elements individually
            var elements = new T[count];
            for (byte index = 0; index < count; index++)
            {
                if (!MemoryMarshal.TryRead(buffer, out T element))
                {
                    Debug.Fail($"Failed to read element from buffer!  Buffer size: {buffer.Length}, element size: {sizeof(T)}");
                    TruncateArray(ref elements, index);
                    break;
                }

                if (!customCheck(element))
                {
                    Debug.Fail($"Check for {elementName} failed!");
                    TruncateArray(ref elements, index);
                    break;
                }

                elements[index] = element;
                buffer = buffer.Slice(sizeof(T));
            }

            return elements;
        }

        private static unsafe string[] ParseStrings(ReadOnlySpan<byte> buffer, ushort offset, string elementName)
        {
            if (!VerifyOffset(buffer, offset, 0, out byte count, elementName) || count == 0)
            {
                return null;
            }

            var elements = new string[count];
            buffer = buffer.Slice(offset + 1);
            for (byte index = 0; index < elements.Length; index++)
            {
                // Get length
                if (!MemoryMarshal.TryRead(buffer, out ushort length))
                {
                    // Resize array to exclude null elements
                    TruncateArray(ref elements, index);
                    break;
                }
                buffer = buffer.Slice(sizeof(ushort));

                // Ensure length is within bounds
                if (buffer.Length < length)
                {
                    Debug.Fail($"Descriptor string length is greater than buffer size!  Index: {index}; String length: {length}; Buffer size: {buffer.Length}");
                    // Resize array to exclude null elements
                    TruncateArray(ref elements, index);
                    break;
                }

                // Parse `length` bytes into a string
                // Pointers are more efficient here, `char` is 2 bytes while these strings are 1-byte characters
                fixed (byte* ptr = buffer)
                {
                    sbyte* sPtr = (sbyte*)ptr;
                    elements[index] = new string(sPtr, 0, length);
                }
                buffer = buffer.Slice(length);
            }

            return elements;
        }

        private static void TruncateArray<T>(ref T[] array, int length)
        {
            if (length == 0)
                array = null;
            else
                array = array.AsSpan().Slice(0, length).ToArray();
        }
    }
}