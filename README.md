# RB4InstrumentMapper

Utility that connects up to 3 *RockBand 4* wireless Xbox instruments (2 guitars and 1 drum) for use in [Clone Hero](https://clonehero.net/).

![RB4InstrumentMapper Application Screenshot](/Images/Screenshot.png "RB4InstrumentMapper Application Screenshot")

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

### Raw Packets

#### Packet Sizes

- Guitar: 40 bytes
- Drums: 36 bytes

#### Packet Frames

- Bytes 0-21 (Xbox header)
- Bytes 12-15 (Unique instrument ID)
- Bytes 22+ (Instrument payload)

#### Guitar 1 Sample

```
[2021-10-31T13:35:10] 2021-10-31 01:35:10.807 [40] 8811A0006245B4E9D18A7EED 8FFE198A 6245B4E9D18A 9055000020005A0A00003C00000000000000
[2021-10-31T13:35:10] 2021-10-31 01:35:10.799 [40] 8811A0006245B4E9D18A7EED 8FFE198A 6245B4E9D18A 805500002000590A00003B00000000000000
[2021-10-31T13:35:10] 2021-10-31 01:35:10.783 [40] 8811A0006245B4E9D18A7EED 8FFE198A 6245B4E9D18A 705500002000580A00003C00000000000000
[2021-10-31T13:35:10] 2021-10-31 01:35:10.766 [40] 8811A0006245B4E9D18A7EED 8FFE198A 6245B4E9D18A 605500002000570A00003B00000000000000
[2021-10-31T13:35:10] 2021-10-31 01:35:10.758 [40] 8811A0006245B4E9D18A7EED 8FFE198A 6245B4E9D18A 505500002000560A00003C00000000000000
[2021-10-31T13:35:10] 2021-10-31 01:35:10.750 [40] 8811A0006245B4E9D18A7EED 8FFE198A 6245B4E9D18A 405500002000550A00003D00000000000000
[2021-10-31T13:35:10] 2021-10-31 01:35:10.742 [40] 8811A0006245B4E9D18A7EED 8FFE198A 6245B4E9D18A 305500002000540A00003E00000000000000
[2021-10-31T13:35:10] 2021-10-31 01:35:10.730 [40] 8811A0006245B4E9D18A7EED 8FFE198A 6245B4E9D18A 205500002000530A00003C00000000000000
```

#### Guitar 2 Sample

```
[2021-10-31T13:58:23] 2021-10-31 01:58:23.459 [40] 8811A0006245B4E9D18A7EED 8FFB14BF 6245B4E9D18A 809B00002000CC0A10009000400100000000
[2021-10-31T13:58:23] 2021-10-31 01:58:23.443 [40] 8811A0006245B4E9D18A7EED 8FFB14BF 6245B4E9D18A 709B00002000CB0A10008F00400100000000
[2021-10-31T13:58:23] 2021-10-31 01:58:23.411 [40] 8811A0006245B4E9D18A7EED 8FFB14BF 6245B4E9D18A 609B00002000CA0A10008D00400100000000
[2021-10-31T13:58:23] 2021-10-31 01:58:23.403 [40] 8811A0006245B4E9D18A7EED 8FFB14BF 6245B4E9D18A 509B00002000C90A10008B00400100000000
[2021-10-31T13:58:23] 2021-10-31 01:58:23.371 [40] 8811A0006245B4E9D18A7EED 8FFB14BF 6245B4E9D18A 409B00002000C80A10008A00400100000000
[2021-10-31T13:58:23] 2021-10-31 01:58:23.363 [40] 8811A0006245B4E9D18A7EED 8FFB14BF 6245B4E9D18A 309B00002000C70A10008900400100000000
[2021-10-31T13:58:23] 2021-10-31 01:58:23.354 [40] 8811A0006245B4E9D18A7EED 8FFB14BF 6245B4E9D18A 209B00002000C60A10008A00400100000000
```

8FFB14BF

#### Drum Sample

```
[2021-10-31T14:25:32] 2021-10-31 02:25:32.656 [36] 8811A0006245B4E9D18A7EED 8FFFCF6B 6245B4E9D18A 1003000020003206000000000000
[2021-10-31T14:25:32] 2021-10-31 02:25:32.608 [36] 8811A0006245B4E9D18A7EED 8FFFCF6B 6245B4E9D18A 0003000020003106000000400000
[2021-10-31T14:25:32] 2021-10-31 02:25:32.367 [36] 8811A0006245B4E9D18A7EED 8FFFCF6B 6245B4E9D18A F002000020003006000000000000
[2021-10-31T14:25:32] 2021-10-31 02:25:32.327 [36] 8811A0006245B4E9D18A7EED 8FFFCF6B 6245B4E9D18A E002000020002F06000004000000
[2021-10-31T14:25:32] 2021-10-31 02:25:32.086 [36] 8811A0006245B4E9D18A7EED 8FFFCF6B 6245B4E9D18A D002000020002E06000000000000
[2021-10-31T14:25:32] 2021-10-31 02:25:32.038 [36] 8811A0006245B4E9D18A7EED 8FFFCF6B 6245B4E9D18A C002000020002D06200040000000
[2021-10-31T14:25:31] 2021-10-31 02:25:31.773 [36] 8811A0006245B4E9D18A7EED 8FFFCF6B 6245B4E9D18A B002000020002C06000000000000
[2021-10-31T14:25:31] 2021-10-31 02:25:31.725 [36] 8811A0006245B4E9D18A7EED 8FFFCF6B 6245B4E9D18A A002000020002B06000004000000
```