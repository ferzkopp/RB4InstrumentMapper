# RB4InstrumentMapper

A utility that connects up to three Xbox One Rock Band 4 instruments (2 guitars and 1 drumkit) for use in [Clone Hero](https://clonehero.net/).

![RB4InstrumentMapper Application Screenshot](/Docs/Images/ProgramScreenshot.png "RB4InstrumentMapper Application Screenshot")

Almost all features for guitars and drums are supported.

## Requirements

- Xbox One Wireless Receiver
- Xbox One RB4 Guitars (up to 2)
  - For Jaguar guitars, you will need to install a [firmware update](https://bit.ly/2UHzonU).
- Xbox One RB4 Drums
  - Note that PDP drums don't seem to work, they turn off after syncing to the receiver.
- Windows 10 64-bit
- Npcap in WinPcap compatibility mode, or WinPcap
- USBPcap
- vJoy or ViGEmBus

## Installation

1. Install [Npcap](https://nmap.org/npcap/#download) in WinPCap compatibility mode (recommended) or [WinPCap](https://www.winpcap.org/install/default.htm).
2. Install [USBPCap](https://desowin.org/usbpcap/).
3. Install [ViGEmBus](https://github.com/ViGEm/ViGEmBus/releases/latest) (recommended) or [vJoy](https://github.com/jshafer817/vJoy/releases/latest).
   - If you installed vJoy, configure it:
     1. Open your Start menu, find the `vJoy` folder, and open the `Configure vJoy` program inside it.
     2. Configure devices 1, 2, and 3 with these settings:
        - Number of Buttons: 16
        - POV Hat Switch: Continuous, POVs: 1
        - Axes: `X`, `Y`, `Z`
     3. Click Apply.<!-- Backslash for a forced hard line break -->\
     ![vJoy Configuration Screenshot](/Docs/Images/vJoyConfiguration.png "vJoy Configuration Screenshot")
   - If you installed ViGEmBus, there's no configuration required. Outputs for guitars and drums will match that of their Xbox 360 counterparts.
4. Restart your PC.
5. Download the latest release from the [Releases tab](https://github.com/ferzkopp/RB4InstrumentMapper/releases/latest) and extract it to a folder.

## Usage

1. Configure the selected Pcap device:
   - Click the `Auto-Detect Pcap` button and follow its instructions.
2. Configure the selected controller device for each guitar and drumkit:
   - If you installed vJoy:
     - Pick one of the vJoy devices that you configured for each instrument you will be using.
   - If you installed ViGEmBus:
     - Pick the `ViGEmBus Device` item in the dropdown for each instrument you will be using. One emulated Xbox 360 controller will be created for each instrument that has this selected.
3. Connect your instruments if you haven't yet.
4. Assign the instrument ID for each instrument:
   - Click the `Auto-Detect ID` button next to each ID field.
     - Guitars should auto-detect automatically as they are constantly sending packets. Retry if duplicate IDs were detected (and rejected).
     - Drums require an action such as a button press on the instrument you are assigning within 2 seconds after 'Auto-Detect' was clicked.
5. Click the Start button.
   - Note: launch *joy.cpl* to check Controller inputs.
6. Map the controls for each instrument in Clone Hero:
   1. Press Space on the main menu.
   2. Click the Assign Controller button and do an action on the instrument to be assigned.
   3. Click the slots in the Controller column to map the controls for one of the instruments.
   4. Repeat for Player 2 and 3.
   5. Click `Done`.

Selections and IDs are saved and should persist across program sessions.

## References

- [GuitarSniffer repository](https://github.com/artman41/guitarsniffer)
- [DrumSniffer repository](https://github.com/Dunkalunk/guitarsniffer)

Packet Data:

- [GuitarSniffer guitar packet logs](https://1drv.ms/f/s!AgQGk0OeTMLwhA-uDO9IQHEHqGhv)
- GuitarSniffer guitar packet spreadsheets: [New](https://docs.google.com/spreadsheets/d/1ITZUvRniGpfS_HV_rBpSwlDdGukc3GC1CeOe7SavQBo/edit?usp=sharing), [Old](https://1drv.ms/x/s!AgQGk0OeTMLwg3GBDXFUC3Erj4Wb)
- See [PacketFormats.md](PacketFormats.md) for a breakdown of the known packet data.

## Build Tools

Compile was done with Visual Studio 2019 Community edition.

Installer was created using the following tools:

- https://wixtoolset.org/
  - https://wixtoolset.org/releases/v3.11.2/stable
  - https://marketplace.visualstudio.com/items?itemName=WixToolset.WixToolsetVisualStudio2019Extension
  - https://marketplace.visualstudio.com/items?itemName=TomEnglert.Wax

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