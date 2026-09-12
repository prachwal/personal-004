; VIC-20 sound path test.  The cartridge wrapper supplies the BASIC reset
; sequence; BASIC executes SYS 4110 ($100E), where this routine starts.
; The melody is intentionally original: three alternating test tones.

.setcpu "6502"

.segment "CODE"
.org $1001

; 10 SYS 4110
.byte $0D, $10, $01, $00, $9E, $20, "4", "1", "1", "0", $00, $00, $00

.org $100E

start:
    lda #$0F
    sta $900E              ; maximum VIC volume

next_tone:
    lda tones,x
    sta $900A              ; oscillator 1 frequency + enable bit
    jsr tone_delay
    lda #$00
    sta $900A              ; stop the oscillator between tones
    jsr short_delay
    inx
    cpx #tone_count
    bne next_tone
    ldx #$00
    jmp next_tone

; A visible, audible delay at the VIC-20 CPU speed.
tone_delay:
    ldy #$50
tone_outer:
    jsr short_delay
    dey
    bne tone_outer
    rts

short_delay:
    ldy #$00
short_loop:
    dey
    bne short_loop
    rts

tones:
    .byte $F0, $F8, $FC
tone_count = * - tones
