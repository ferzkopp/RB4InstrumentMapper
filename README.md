# RB4InstrumentMapper

Utility that connects up to 3 *RockBand 4* wireless Xbox instruments (2 guitars and 1 drum) for use in [Clone Hero](https://clonehero.net/).

![RB4InstrumentMapper Application Screenshot](/Docs/Images/Screenshot.png "RB4InstrumentMapper Application Screenshot")

Devices should be connected via a compatible USB Xbox Wireless Device Adapter and will be mapped to virtual joystick devices.

The *Guitar* mapper supports all buttons and analog whammy and tilt, the *Drum* mapper all drums, cymbals and 2x kick.

## Hardware Requirements

- Xbox Wireless Device Adapter
- RB4 Guitars (up to 2)
- RB4 Drums
- Windows 10 (64bit)

## Compilation

- Obtain [Visual Studio 2019 community edition](https://visualstudio.microsoft.com/vs/community/)
- Open *RB4InstrumentMapper.sln*
- Select "Release/x64" then Build - *Build Solution*

The application can now be found in the ```RB4InstrumentMapper\bin\x64\Release``` folder.

## Installation

### Install WinPCap

- [WinPCap](https://www.winpcap.org/install/bin/WinPcap_4_1_3.exe)

### Install USBPcap

- [USBPCap](https://desowin.org/usbpcap/)

### Install vJoy

* [vJoy](https://github.com/jshafer817/vJoy/releases/latest)

### Configure vJoy

- Open `vJoyConf`
- Format your `vJoy Device 1`
  - 16 Buttons
  - Axes `X`, `Y`, `Z`
  - Use for Guitar 1
- Repeat for `vJoy Device 2`
  - 16 Buttons
  - Axes `X`, `Y`, `Z`
  - Use for Guitar 2
- Repeat for `vJoy Device 3`
  - 16 Buttons
  - Use for Drum
- Restart PC.

## Usage

### Configure RB4InstrumentMapper

#### pCap Device Configuration

Unplug wireless USB adapter.

- Launch *RB4InstrumentMapper* application.
- Select *pCap Device* dropdown and observe devices.
- Close application.

Plug in wireless USB adapter.

- Launch *RB4InstrumentMapper* application.
- Pick *pCap Device* that was newly added to the list (the wireless USB adapter).

Connect all instruments to the wireless USB adapter.

- Press *Start*.

Use the *Show Packets* button to verify packets are being received. Guitars will send continously, Drums only when hit.

- Close application.

The pCap device is now configured and the setting saved.

#### vJoy Device configuration

- Launch *RB4InstrumentMapper* application.
- Pick Guitar 1 as *vJoy Device 1*.
- Pick Guitar 2 as *vJoy Device 2* (or skip if only one guitar is available).
- Pick Drum as *vJoy Device 3*.
- Press *Start*.

Hit the drum. Hex IDs of each instrument should be found and are displayed. Hex IDs can be manually cleared (just edit them out) and should repopulate.

- Close application.

The vJoy devices are now configured and the settings were saved.

### Use RB4InstrumentMapper

- Connect all instruments.
- Launch *RB4InstrumentMapper* application.
- Press *Start*.

Previous settings should be shown and instrument data is mapped and send to joystick devices.

### Configure Clone Hero

- Connect all instruments.
- Launch *RB4InstrumentMapper* application.
- Press *Start*.
- Launch *Clone Hero* application.
- Press *Space* to enter configuration mode.
- Select *Player 1*, *Remove*, *Assign Controller* and then a button on the instrument to assign.
- For every action, click Controller button and then press the instrument key (or move the instrument) corresponding to the action. 
  - Repeat for each action as necessary to get correct mappings.
  - Repeat for *Player 2* and *Player 3* for all devices.
- Press *Done*.

Clone Hero is now configured. Instruments can join by pressing the assigned *Start* button and select their type.

It is highly recommended to calibrate the latency of audio and video with the mapped instruments.

## References

### Packet Data

See [PacketFormats.md](PacketFormats.md) for a breakdown of the packet data.
