# VIC-20 test disks

| File | Source | Purpose |
| ------ | --------- | -------- |
| `vic20-load.d64` | (pre-existing) | - |
| `vic20-via-pb7.d64` | [VICE-testprogs](https://github.com/libsidplayfp/VICE-testprogs) `VIC20/via_pb7/viapb7.d64` | VIA Timer1 PB7 output (one-shot vs free-run) - real-hardware-verified reference table in the upstream `readme.txt`. Programs on disk: `UNEXPANDED`, `EXPANDED`. |
| `vic20-via-t1irqack.d64` | [VICE-testprogs](https://github.com/libsidplayfp/VICE-testprogs) `VIC20/via_t1irqack/viat1irqack.d64` | VIA Timer1 IRQ-ack-on-high-latch-write behavior (raster-IRQ trick used by the real game "Bandits"). Programs on disk: `BANDITS-VIA1`, `BANDITS-VIA2`, `BANDITS-VIA1-8K`, `BANDITS-VIA2-8K`. |

Both are official VICE-project regression tests (software only, no hardware dongles needed) -
picked specifically because they exercise the exact `MOS6522` behavior touched by
`fix/vic20-via1-nmi-irq` (PB7 one-shot toggle, VIA1/VIA2 interrupt-flag-clear-on-write).

Load with e.g. `LOAD"UNEXPANDED",8,1` then `RUN` - give the machine ~200,000 instructions to
finish KERNAL boot (reach `READY.`) before typing, or the keyboard scan isn't live yet and typed
characters get silently dropped.
