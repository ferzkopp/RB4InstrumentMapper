namespace RB4InstrumentMapper.Parsing
{
    internal class XboxMessage
    {
        private CommandHeader _header;
        private byte[] _data;

        public CommandHeader Header
        {
            get => _header;
            set
            {
                _header = value;
                _header.DataLength = _data?.Length ?? 0;
            }
        }

        public byte[] Data
        {
            get => _data;
            set
            {
                _data = value;
                _header.DataLength = _data?.Length ?? 0;
            }
        }
    }

    internal unsafe class XboxMessage<TData>
        where TData : unmanaged
    {
        private CommandHeader _header;
        public TData Data;

        public CommandHeader Header
        {
            get => _header;
            set
            {
                _header = value;
                _header.DataLength = sizeof(TData);
            }
        }
    }
}