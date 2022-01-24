# Xbox One RB4 Instrument Data Packets

This documentation is far from fully comprehensive yet, and it also needs some better formatting than just long lists.

Byte numbers in lists are 0-indexed.

References:

- [GuitarSniffer guitar packet logs](https://1drv.ms/f/s!AgQGk0OeTMLwhA-uDO9IQHEHqGhv)
- GuitarSniffer guitar packet spreadsheets: [New](https://docs.google.com/spreadsheets/d/1ITZUvRniGpfS_HV_rBpSwlDdGukc3GC1CeOe7SavQBo/edit?usp=sharing), [Old](https://1drv.ms/x/s!AgQGk0OeTMLwg3GBDXFUC3Erj4Wb)

To be referenced later:

- <https://github.com/quantus/xbox-one-controller-protocol>

## Packet Frames

- Bytes 0-21: Xbox header
- Bytes 22-29: Packet metadata
- Bytes 30 onward: Input data

## Header Data

- 22 bytes long

Bytes:

- Not fully understood, more research needed
  - 0-6 seem constant, `8811A0006245B4`
    - 2 seems to be `19` instead of `11` for a single packet in the guitar power-on log and in the whammy log
  - 7-9 have been observed to be different between the examples here and the GuitarSniffer packet logs folder
  - 10-11 seem constant, `7EED`
- 12-15 - Instrument ID
  - 12 appears to be constant, `8F`
- 16-21 seem to mirror 4-9

## Packet Metadata

- 8 bytes long, comes after the 22 header bytes

Bytes:

- Not well understood, there are some edge cases that need to be researched further
- 22:
  - High 4 bits seem to increment by 1 with every packet
- 23:
  - Seems to increment every time 22's high 4 bits roll over from F to 0
- 24:
  - Seems to be `01` during power-on or Xbox button packets, and `00` everywhere else
- 25:
  - Seems to be a constant `00`
- 26:
  - Type of data? seems to be a constant `20` during regular packets and `07` in Xbox button packets
- 27:
  - Unsure, seems to be a constant `00` during regular packets and `20` in Xbox button packets
- 28:
  - Seems to increment with every packet, but its value doesn't seem to start at the same time as 22 and 23
- 29:
  - Data bytes length?
    - This does not seem to be the case for all of the power-on packets logged, but it seems to be the case 100% of the time after power-on
  - On guitars, this is `0A`, except in Xbox button packets where it is `02`
  - On drums, this is `06`, except presumably in Xbox button packets where it is probably `02`

## Guitar Input Data

- 40 bytes long, including the Xbox header and packet count data
- 10 bytes long without the header and count data
- 32(?) bytes long when the Xbox button is pressed, including the Xbox header and packet count data
- Some random packets here and there are 34 bytes long
- Packet length varies wildly during power-on (anywhere from 32 to 90), but none there seem to be 40 bytes long.

Bytes:

- 30 - Buttons
  - Bit 0 (`0x01`) - Xbox
  - Bit 1 (`0x02`) - Unknown (maybe equivalent to the Share button?)
  - Bit 2 (`0x04`) - Menu button
  - Bit 3 (`0x08`) - Options button
  - Bit 4 (`0x10`) - Active when pressing either of the Green frets (equivalent to the A button on a regular controller?)
  - Bit 5 (`0x20`) - Active when pressing either of the Red frets (equivalent to the B button on a regular controller?)
  - Bit 6 (`0x40`) - Active when pressing either of the Blue frets (equivalent to the X button on a regular controller?)
  - Bit 7 (`0x80`) - Active when pressing either of the Yellow frets (equivalent to the Y button on a regular controller?)
- 31 - D-pad/Strum Bar / Bumpers/Stick Presses
  - Bit 0 (`0x01`) - Down (Strum Up)
  - Bit 1 (`0x02`) - Up (Strum Down)
  - Bit 2 (`0x04`) - Left
  - Bit 3 (`0x08`) - Right
  - Bit 4 (`0x10`) - Active when pressing either of the Orange frets (equivalent to the LB button on a regular controller?)
  - Bit 5 (`0x20`) - Unused (equivalent to the RB button on a regular controller?)
  - Bit 6 (`0x40`) - Active when pressing the lower frets (equivalent to the left stick button on a regular controller?)
  - Bit 7 (`0x80`) - Unused (equivalent to the right stick button on a regular controller?)
- 32 - Tilt (Axis)
  - Has a threshold of `70`? (values below get cut off to `00`)
- 33 - Whammy Bar (Axis)
  - Uses full byte range
- 34 - Pickup Switch/Slider (Axis)
  - Uses top 4 bytes, possible values are `00`, `10`, `20`, `30`, and `40`
- 35 - Upper Frets
  - Bit 0 (`0x01`) - Green
  - Bit 1 (`0x02`) - Red
  - Bit 2 (`0x04`) - Yellow
  - Bit 3 (`0x08`) - Blue
  - Bit 4 (`0x10`) - Orange
  - Bits 5-7 - Unused
- 36 - Lower Frets
  - Bit 0 (`0x01`) - Green
  - Bit 1 (`0x02`) - Red
  - Bit 2 (`0x04`) - Yellow
  - Bit 3 (`0x08`) - Blue
  - Bit 4 (`0x10`) - Orange
  - Bits 5-7 - Unused
- 37-39 are unknown

## Drums Input Data

- 36 bytes long, including the Xbox header and packet count data
- 6 bytes long without the header and count data
- Presumably 32(?) bytes long when the Xbox button is pressed, including the Xbox header and packet count data

Bytes:

- 30 - Buttons + Red/Green Pads
  - Bit 0 (`0x01`) - Xbox
  - Bit 1 (`0x02`) - Unknown (maybe equivalent to the Share button?)
  - Bit 2 (`0x04`) - Menu button
  - Bit 3 (`0x08`) - Options button
  - Bit 4 (`0x10`) - Green Pad (equivalent to the A button on a regular controller?)
  - Bit 5 (`0x20`) - Red Pad (equivalent to the B button on a regular controller?)
  - Bit 6 (`0x40`) - (Interpolated) Blue Pad? (equivalent to the X button on a regular controller?)
  - Bit 7 (`0x80`) - (Interpolated) Yellow Pad? (equivalent to the Y button on a regular controller?)
- 31 - D-pad
  - Bit 0 (`0x01`) - Down
  - Bit 1 (`0x02`) - Up
  - Bit 2 (`0x04`) - Left
  - Bit 3 (`0x08`) - Right
  - Bit 4 (`0x10`) - 1st Kick Pedal (equivalent to the LB button on a regular controller?)
  - Bit 5 (`0x20`) - 2nd Kick Pedal (equivalent to the RB button on a regular controller?)
  - Bit 6 (`0x40`) - Unused? (equivalent to the left stick button on a regular controller?)
  - Bit 7 (`0x80`) - Unused? (equivalent to the right stick button on a regular controller?)
- 32 - Yellow Pad
  - Uses bits 0-3
- 33 - Blue Pad
  - Uses bits 4-7
- 34 - Yellow & Blue Cymbal
  - Bits 0-3 - Blue Cymbal
  - Bits 4-7 - Yellow Cymbal
- 35 - Green Cymbal
  - Uses bits 4-7

## Samples

Guitar 1 Sample

```
2021-10-31 01:35:10.730 [40] 8811A0006245B4E9D18A7EED 8FFE198A 6245B4E9D18A 205500002000530A 00003C00000000000000
2021-10-31 01:35:10.742 [40] 8811A0006245B4E9D18A7EED 8FFE198A 6245B4E9D18A 305500002000540A 00003E00000000000000
2021-10-31 01:35:10.750 [40] 8811A0006245B4E9D18A7EED 8FFE198A 6245B4E9D18A 405500002000550A 00003D00000000000000
2021-10-31 01:35:10.758 [40] 8811A0006245B4E9D18A7EED 8FFE198A 6245B4E9D18A 505500002000560A 00003C00000000000000
2021-10-31 01:35:10.766 [40] 8811A0006245B4E9D18A7EED 8FFE198A 6245B4E9D18A 605500002000570A 00003B00000000000000
2021-10-31 01:35:10.783 [40] 8811A0006245B4E9D18A7EED 8FFE198A 6245B4E9D18A 705500002000580A 00003C00000000000000
2021-10-31 01:35:10.799 [40] 8811A0006245B4E9D18A7EED 8FFE198A 6245B4E9D18A 805500002000590A 00003B00000000000000
2021-10-31 01:35:10.807 [40] 8811A0006245B4E9D18A7EED 8FFE198A 6245B4E9D18A 9055000020005A0A 00003C00000000000000
```

Guitar 2 Sample

```
2021-10-31 01:58:23.354 [40] 8811A0006245B4E9D18A7EED 8FFB14BF 6245B4E9D18A 209B00002000C60A 10008A00400100000000
2021-10-31 01:58:23.363 [40] 8811A0006245B4E9D18A7EED 8FFB14BF 6245B4E9D18A 309B00002000C70A 10008900400100000000
2021-10-31 01:58:23.371 [40] 8811A0006245B4E9D18A7EED 8FFB14BF 6245B4E9D18A 409B00002000C80A 10008A00400100000000
2021-10-31 01:58:23.403 [40] 8811A0006245B4E9D18A7EED 8FFB14BF 6245B4E9D18A 509B00002000C90A 10008B00400100000000
2021-10-31 01:58:23.411 [40] 8811A0006245B4E9D18A7EED 8FFB14BF 6245B4E9D18A 609B00002000CA0A 10008D00400100000000
2021-10-31 01:58:23.443 [40] 8811A0006245B4E9D18A7EED 8FFB14BF 6245B4E9D18A 709B00002000CB0A 10008F00400100000000
2021-10-31 01:58:23.459 [40] 8811A0006245B4E9D18A7EED 8FFB14BF 6245B4E9D18A 809B00002000CC0A 10009000400100000000
```

Drum Sample

```
2021-10-31 02:25:31.725 [36] 8811A0006245B4E9D18A7EED 8FFFCF6B 6245B4E9D18A A002000020002B06 000004000000
2021-10-31 02:25:31.773 [36] 8811A0006245B4E9D18A7EED 8FFFCF6B 6245B4E9D18A B002000020002C06 000000000000
2021-10-31 02:25:32.038 [36] 8811A0006245B4E9D18A7EED 8FFFCF6B 6245B4E9D18A C002000020002D06 200040000000
2021-10-31 02:25:32.086 [36] 8811A0006245B4E9D18A7EED 8FFFCF6B 6245B4E9D18A D002000020002E06 000000000000
2021-10-31 02:25:32.327 [36] 8811A0006245B4E9D18A7EED 8FFFCF6B 6245B4E9D18A E002000020002F06 000004000000
2021-10-31 02:25:32.367 [36] 8811A0006245B4E9D18A7EED 8FFFCF6B 6245B4E9D18A F002000020003006 000000000000
2021-10-31 02:25:32.608 [36] 8811A0006245B4E9D18A7EED 8FFFCF6B 6245B4E9D18A 0003000020003106 000000400000
2021-10-31 02:25:32.656 [36] 8811A0006245B4E9D18A7EED 8FFFCF6B 6245B4E9D18A 1003000020003206 000000000000
```
