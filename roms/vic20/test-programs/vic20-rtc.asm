; MC146818/DS12887 RTC stage for the VIC-20 test cartridge.
; The Python builder places this routine at $BF40. A proven VIC-20
; autostart bootstrap invokes SYS 48960 after BASIC initialization; this
; routine installs the IRQ handler and returns to the normal BASIC prompt.
; It displays only HH:MM in the top-right area of the screen.

.setcpu "6502"
.segment "CODE"
.org $BF40

; Keep the saved IRQ vector in unused RAM. The screen pointer still uses the
; KERNAL's $F2/$F3 pair, but update_clock saves and restores it atomically.
old_irq = $02F0
screen_pointer = $00F2

rtc_start:
    sei
    lda $0314
    sta old_irq
    lda $0315
    sta old_irq + 1
    lda #<irq_handler
    sta $0314
    lda #>irq_handler
    sta $0315

    ; MC146818 register B: UIE + PIE + 24-hour BCD mode.
    lda #$0B
    sta $9C00
    lda #$52
    sta $9C01
    jsr update_clock
    cli
    rts

irq_handler:
    pha
    txa
    pha
    tya
    pha

    ; Reading register C acknowledges the RTC update-ended interrupt.
    lda #$0C
    sta $9C00
    lda $9C01
    and #$50
    beq irq_restore
    jsr update_clock

irq_restore:
    pla
    tay
    pla
    tax
    pla
    jmp (old_irq)

update_clock:
    lda $00F2
    pha
    lda $00F3
    pha
    lda #<$1E11
    sta screen_pointer
    lda #>$1E11
    sta screen_pointer + 1

    lda #$04
    jsr read_rtc
    jsr write_bcd
    jsr write_colon
    lda #$02
    jsr read_rtc
    jsr write_bcd
    pla
    sta $00F3
    pla
    sta $00F2
    rts

read_rtc:
    sta $9C00
    lda $9C01
    rts

write_bcd:
    pha
    and #$F0
    lsr
    lsr
    lsr
    lsr
    clc
    adc #$30
    ldy #$00
    sta (screen_pointer),y
    inc screen_pointer
    pla
    and #$0F
    clc
    adc #$30
    ldy #$00
    sta (screen_pointer),y
    inc screen_pointer
    rts

write_colon:
    lda #$3A
    ldy #$00
    sta (screen_pointer),y
    inc screen_pointer
    rts
