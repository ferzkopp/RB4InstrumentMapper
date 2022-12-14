# RB4InstrumentMapper

A program that maps packets from Xbox One Rock Band 4 instruments to virtual controllers, for use in [Clone Hero](https://clonehero.net/).

![RB4InstrumentMapper Application Screenshot](/Docs/Images/ProgramScreenshot.png "RB4InstrumentMapper Application Screenshot")

Both guitars and drums are supported through an Xbox One wireless receiver.

## Software Requirements

- Windows 10 64-bit
- [WinPCap](https://www.winpcap.org/install/default.htm)
- [USBPCap](https://desowin.org/usbpcap/)
- [ViGEmBus](https://github.com/ViGEm/ViGEmBus/releases/latest) or [vJoy](https://github.com/jshafer817/vJoy/releases/latest)

## Hardware Notes

Jaguar guitars require a firmware update in order to connect to the receiver.

- [Instructions](https://bit.ly/2UHzonU)
- [Firmware installer backup](https://drive.google.com/file/d/1DQxkkbBfi-UOqdX6vp5TaX6F2N2OBDra/view?usp=drivesdk)

Some guitars/drumkits might not sync properly when using just the sync button. This includes the PDP drumkit. Follow these steps to sync your device correctly:

1. Go to Windows settings > Devices > Bluetooth & other devices
2. Click `Add Bluetooth or other device` and pick the `Everything else` option.
3. Press and hold the sync button until the Xbox button light flashes quickly.
4. Select `Xbox compatible game controller` from the list once it appears.
5. If that doesn't work, restart your PC and try again.

## Installation

1. Install [WinPCap](https://www.winpcap.org/install/default.htm).
   - While Npcap is newer, it does not seem to work with Xbox One receivers currently, and trying it isn't recommended unless WinPcap doesn't work and none of the other troubleshooting helps.
2. Install [USBPCap](https://desowin.org/usbpcap/).
3. Install [ViGEmBus](https://github.com/ViGEm/ViGEmBus/releases/latest) (recommended) or [vJoy](https://github.com/jshafer817/vJoy/releases/latest).
   - If you installed vJoy, configure it:
     1. Open your Start menu, find the `vJoy` folder, and open the `Configure vJoy` program inside it.
     2. Configure one device for each one of your guitars/drumkits, using these settings:
        - Number of Buttons: 16
        - POV Hat Switch: Continuous, POVs: 1
        - Axes: `X`, `Y`, `Z`

        ![vJoy Configuration Screenshot](/Docs/Images/vJoyConfiguration.png "vJoy Configuration Screenshot")

     3. Click Apply.
   - If you installed ViGEmBus, there's no configuration required. Outputs for guitars and drums will match that of their Xbox 360 counterparts.
4. Restart your PC.

## Usage

1. Select your Xbox One receiver from the dropdown menu.
   - Xbox receivers should be detected automatically by device name.
   - If they are not, click the `Auto-Detect Pcap` button and follow its instructions.
   - You can also check the device list yourself for a device called `MT7612US_RL`.
   - If that doesn't work:
     - Make sure you are not using a USB 3.0 port, as those may cause the receiver to not show up for some reason.
     - If you installed WinPcap, try installing Npcap instead, or vice versa.
2. Select either vJoy or ViGEmBus in the Controller Type dropdown.
3. Connect your instruments if you haven't yet.
4. Click the Start button. Devices will be detected automatically.
5. Map the controls for each instrument in Clone Hero:
   1. Press Space on the main menu.
   2. Click the Assign Controller button and press a button on the instrument for it to be assigned.
   3. Click the slots in the Controller column to map each of the controls.
   4. Repeat for each connected device.
   5. Click `Done`.

Selections are saved and should persist across program sessions.

## Packet Logs

RB4InstrumentMapper is capable of logging packets to a file for debugging purposes. To do so, enable both the `Show Packets (for debugging)` and `Log packets to file` checkboxes, then hit Start. Packet logs get saved to a `RB4InstrumentMapper` > `PacketLogs` folder inside your Documents folder. Make sure to include it when getting help or creating an issue report for packet parsing issues.

Note that these settings are meant for debugging purposes only, leaving them enabled can reduce the performance of the program somewhat.

## Error Logs

In the case that the program crashes, an error log is saved to a `RB4InstrumentMapper` > `Logs` folder inside your Documents folder. Make sure to include it when getting help or creating an issue report for the crash.

## References

- [GuitarSniffer repository](https://github.com/artman41/guitarsniffer)
- [DrumSniffer repository](https://github.com/Dunkalunk/guitarsniffer)

Packet Data:

- [GuitarSniffer guitar packet logs](https://1drv.ms/f/s!AgQGk0OeTMLwhA-uDO9IQHEHqGhv)
- GuitarSniffer guitar packet spreadsheets: [New](https://docs.google.com/spreadsheets/d/1ITZUvRniGpfS_HV_rBpSwlDdGukc3GC1CeOe7SavQBo/edit?usp=sharing), [Old](https://1drv.ms/x/s!AgQGk0OeTMLwg3GBDXFUC3Erj4Wb)
- See [PacketFormats.md](/Docs/PacketFormats.md) for a breakdown of the known packet data.

## Building

To build this program, you will need:

- Visual Studio (or MSBuild + your code editor of choice) for the program
- [WiX Toolset](https://wixtoolset.org/) for the installer

## License

Copyright (c) 2021 Andreas Schiffler

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), 
to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, 
and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS 
IN THE SOFTWARE.