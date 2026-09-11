# VIC-20 IEC Disk

## IEC Wiring

The VIC-20 uses the bit-serial CBM IEC bus, not the PET's parallel IEEE-488
bus. The KERNAL shifts each byte LSB-first, eight bits per transfer.

| IEC signal | VIC-20 connection | Register or control bit |
| --- | --- | --- |
| ATN out | VIA1 PA7 | `$9111` / `$911F`, bit 7 |
| CLK in | VIA1 PA0 | `$9111` / `$911F`, bit 0 |
| DATA in | VIA1 PA1 | `$9111` / `$911F`, bit 1 |
| CLK out | VIA2 CA2 | `$912C` PCR, bit 1 |
| DATA out | VIA2 CB2 | `$912C` PCR, bit 5 |

There is no separate ATN input pin in this VIC-20 wiring. IEC lines are
active-low open-collector signals: zero asserts a line and one releases it.
The binding must therefore poll the VIA inputs and output control levels and
edge-detect them; `MOS6522` has no output-change event for CA2 or CB2.

The mapping is confirmed by the commented VIC-20 KERNAL disassembly:

- `vic-20-rom.asm`, lines 996-1006 and 1073-1083: VIA1 PA7 is ATN out,
  PA0 is CLK in, and PA1 is DATA in.
- `vic-20-rom.asm`, lines 1111-1117: VIA2 CA2 is CLK out and CB2 is DATA
  out through PCR bits 1 and 5.
- `vic-20-rom.asm`, lines 11163-11239 and 11433-11516: serial output and
  input shift eight bits LSB-first.
- `vic-20-rom.asm`, lines 11348-11376: `FCIOUT` feeds the serial sender.
- `vic-20-rom.asm`, lines 15124-15128: `SETLFS` stores the logical file,
  device number, and secondary address; disk device 8 is documented at lines
  15108-15118.
- `vic-20-rom.asm`, lines 11140-11142 and 11301-11305: ATN is asserted and
  released through VIA1 PA7.

Sources:

- [Lee Davison's commented VIC-20 ROM disassembly](https://gist.githubusercontent.com/cbmeeks/65c0f2acc1f0ad041c236637732aac8c/raw/8a13786a47f6cbbb8b86a465fce82500921a1ec0/vic-20-rom.asm)
- [VICE VIC-20 IEC implementation](https://github.com/VICEEmulator/VICE/blob/master/src/vic20/vic20iec.c)

The implementation must remain bit-serial and must not reuse the PET
`PetIeeeBus` byte-level DAV/NRFD/NDAC handshake.
