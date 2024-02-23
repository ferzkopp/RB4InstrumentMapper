namespace RB4InstrumentMapper.Parsing
{
    public enum MappingMode
    {
        NotSet = 0,
        ViGEmBus = 1,
        vJoy = 2
    }

    /// <summary>
    /// Backend for handling controllers via Pcap.
    /// </summary>
    public static class BackendSettings
    {
        /// <summary>
        /// The controller emulator to use.
        /// </summary>
        public static MappingMode MapperMode { get; set; } = MappingMode.NotSet;

        /// <summary>
        /// Whether or not packets should be logged to the console.
        /// </summary>
        public static bool LogPackets { get; set; } = false;

        /// <summary>
        /// Whether or not verbose errors should be logged to the console.
        /// </summary>
        public static bool PrintVerboseErrors { get; set; } = false;
    }
}