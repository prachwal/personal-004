; da65 V2.18 - Ubuntu 2.19-1
; Created:    2026-09-09 23:08:38
; Input file: roms/pet/pet-2001-8/rom-1-d000.901439-02.bin
; Page:       1


        .setcpu "6502"

L00A4           := $00A4
L00C2           := $00C2
L00C8           := $00C8
LC2DA           := $C2DA
LC2E1           := $C2E1
LC32A           := $C32A
LC357           := $C357
LC359           := $C359
LC7F0           := $C7F0
LCCA4           := $CCA4
LCCA7           := $CCA7
LCCA9           := $CCA9
LCCD2           := $CCD2
LCD9D           := $CD9D
LCE05           := $CE05
LCE0B           := $CE0B
LCE0E           := $CE0E
LCE11           := $CE11
LCE13           := $CE13
LCE1C           := $CE1C
LCF7B           := $CF7B
LCF82           := $CF82
LCFE1           := $CFE1
LCFE3           := $CFE3
LD80B           := $D80B
LD81C           := $D81C
LD86E           := $D86E
LD885           := $D885
LD95E           := $D95E
LDAA6           := $DAA6
LDACE           := $DACE
LDB16           := $DB16
LDB2D           := $DB2D
LDB6D           := $DB6D
LDBC5           := $DBC5
LDCB1           := $DCB1
LE7F3           := $E7F3
        bcc     LCFE3
        inx
        bne     LCFE1
        cmp     #$41
        bcc     LD00E
        sbc     #$5B
        sec
        sbc     #$A5
LD00E:  rts

        pla
        pha
        cmp     #$2A
        bne     LD01C
LD015:  lda     #$1A
        ldy     #$D0
        rts

        brk
LD01B:  brk
LD01C:  lda     $94
        ldy     $95
        cmp     #$54
        bne     LD02F
        cpy     #$C9
        beq     LD015
        cpy     #$49
        bne     LD02F
LD02C:  jmp     LCE1C

LD02F:  cmp     #$53
        bne     LD037
        cpy     #$54
        beq     LD02C
LD037:  lda     $7E
        ldy     $7F
        sta     $AE
        sty     $AF
        lda     $80
        ldy     $81
        sta     $A9
        sty     $AA
        clc
        adc     #$07
        bcc     LD04D
        iny
LD04D:  sta     $A7
        sty     $A8
        jsr     LC2DA
        lda     $A7
        ldy     $A8
        iny
        sta     $7E
        sty     $7F
        ldy     #$00
        lda     $94
        sta     ($AE),y
        iny
        lda     $95
        sta     ($AE),y
        lda     #$00
        iny
        sta     ($AE),y
        iny
        sta     ($AE),y
        iny
        sta     ($AE),y
        iny
        sta     ($AE),y
        iny
        sta     ($AE),y
        lda     $AE
        clc
        adc     #$02
        ldy     $AF
        bcc     LD083
        iny
LD083:  sta     $96
        sty     $97
        rts

LD088:  lda     $5C
        asl     a
        adc     #$05
        adc     $AE
        ldy     $AF
        bcc     LD094
        iny
LD094:  sta     $A7
        sty     $A8
        rts

        bcc     LD01B
        brk
        brk
LD09D:  jsr     L00C2
        jsr     LCCA4
LD0A3:  lda     $B5
        bmi     LD0B4
        lda     $B0
        cmp     #$90
        bcc     LD0B6
        lda     #$99
        ldy     #$D0
        jsr     LDB2D
LD0B4:  bne     LD130
LD0B6:  jmp     LDB6D

        lda     $5D
        ora     $5F
        pha
        lda     $5E
        pha
        ldy     #$00
LD0C3:  tya
        pha
        lda     $95
        pha
        lda     $94
        pha
        jsr     LD09D
        pla
        sta     $94
        pla
        sta     $95
        pla
        tay
        tsx
        lda     $0102,x
        pha
        lda     $0101,x
        pha
        lda     $B3
        sta     $0102,x
        lda     $B4
        sta     $0101,x
        iny
        jsr     L00C8
        cmp     #$2C
        beq     LD0C3
        sty     $5C
        jsr     LCE0B
        pla
        sta     $5E
        pla
        sta     $5F
        and     #$7F
        sta     $5D
        ldx     $7E
        lda     $7F
LD104:  stx     $AE
        sta     $AF
        cmp     $81
        bne     LD110
        cpx     $80
        beq     LD149
LD110:  ldy     #$00
        lda     ($AE),y
        iny
        cmp     $94
        bne     LD11F
        lda     $95
        cmp     ($AE),y
        beq     LD135
LD11F:  iny
        lda     ($AE),y
        clc
        adc     $AE
        tax
        iny
        lda     ($AE),y
        adc     $AF
        bcc     LD104
LD12D:  ldx     #$70
        .byte   $2C
LD130:  ldx     #$35
LD132:  jmp     LC359

LD135:  ldx     #$7D
        lda     $5D
        bne     LD132
        jsr     LD088
        lda     $5C
        ldy     #$04
        cmp     ($AE),y
        bne     LD12D
        jmp     LD1D3

LD149:  jsr     LD088
        jsr     LC32A
        lda     #$00
        tay
        sta     $C1
        ldx     #$05
        lda     $94
        sta     ($AE),y
        bpl     LD15D
        dex
LD15D:  iny
        lda     $95
        sta     ($AE),y
        bpl     LD166
        dex
        dex
LD166:  stx     $C0
        lda     $5C
        iny
        iny
        iny
        sta     ($AE),y
LD16F:  ldx     #$0B
        lda     #$00
        bit     $5D
        bvc     LD17F
        pla
        clc
        adc     #$01
        tax
        pla
        adc     #$00
LD17F:  iny
        sta     ($AE),y
        iny
        txa
        sta     ($AE),y
        jsr     LD233
        stx     $C0
        sta     $C1
        ldy     $71
        dec     $5C
        bne     LD16F
        adc     $A8
        bcs     LD1F4
        sta     $A8
        tay
        txa
        adc     $A7
        bcc     LD1A2
        iny
        beq     LD1F4
LD1A2:  jsr     LC32A
        sta     $80
        sty     $81
        lda     #$00
        inc     $C1
        ldy     $C0
        beq     LD1B6
LD1B1:  dey
        sta     ($A7),y
        bne     LD1B1
LD1B6:  dec     $A8
        dec     $C1
        bne     LD1B1
        inc     $A8
        sec
        lda     $80
        sbc     $AE
        ldy     #$02
        sta     ($AE),y
        lda     $81
        iny
        sbc     $AF
        sta     ($AE),y
        lda     $5D
        bne     LD232
        iny
LD1D3:  lda     ($AE),y
        sta     $5C
        lda     #$00
        sta     $C0
LD1DB:  sta     $C1
        iny
        pla
        tax
        sta     $B3
        pla
        sta     $B4
        cmp     ($AE),y
        bcc     LD1F7
        bne     LD1F1
        iny
        txa
        cmp     ($AE),y
        bcc     LD1F8
LD1F1:  jmp     LD12D

LD1F4:  jmp     LC357

LD1F7:  iny
LD1F8:  lda     $C1
        ora     $C0
        clc
        beq     LD209
        jsr     LD233
        txa
        adc     $B3
        tax
        tya
        ldy     $71
LD209:  adc     $B4
        stx     $C0
        dec     $5C
        bne     LD1DB
        ldx     #$05
        lda     $94
        bpl     LD218
        dex
LD218:  lda     $95
        bpl     LD21E
        dex
        dex
LD21E:  stx     $77
        lda     #$00
        jsr     LD23C
        txa
        adc     $A7
        sta     $96
        tya
        adc     $A8
        sta     $97
        tay
        lda     $96
LD232:  rts

LD233:  sty     $71
        lda     ($AE),y
        sta     $77
        dey
        lda     ($AE),y
LD23C:  sta     $78
        lda     #$10
        sta     $AC
        ldx     #$00
        ldy     #$00
LD246:  txa
        asl     a
        tax
        tya
        rol     a
        tay
        bcs     LD1F4
        asl     $C0
        rol     $C1
        bcc     LD25F
        clc
        txa
        adc     $77
        tax
        tya
        adc     $78
        tay
        bcs     LD1F4
LD25F:  dec     $AC
        bne     LD246
        rts

        lda     $5E
        beq     LD26B
        jsr     LD57E
LD26B:  jsr     LD404
        sec
        lda     $82
        sbc     $80
        tay
        lda     $83
        sbc     $81
LD278:  ldx     #$00
        stx     $5E
        sta     $B1
        sty     $B2
        ldx     #$90
        jmp     LDB16

        ldy     $05
LD287:  lda     #$00
        beq     LD278
LD28B:  ldx     $89
        inx
        bne     LD232
        ldx     #$9A
LD292:  jmp     LC359

        jsr     LD2C3
        jsr     LD28B
        jsr     LCE0E
        lda     #$80
        sta     $61
        jsr     LCF7B
        jsr     LCCA7
        jsr     LCE0B
        lda     #$B2
        jsr     LCE13
        pha
        lda     $97
        pha
        lda     $96
        pha
        lda     $CA
        pha
        lda     $C9
        pha
        jsr     LC7F0
        jmp     LD333

LD2C3:  lda     #$A5
        jsr     LCE13
        ora     #$80
        sta     $61
        jsr     LCF82
        sta     $9D
        sty     $9E
        jmp     LCCA7

        jsr     LD2C3
        lda     $9E
        pha
        lda     $9D
        pha
        jsr     LCE05
        jsr     LCCA7
        pla
        sta     $9D
        pla
        sta     $9E
        ldy     #$02
        ldx     #$ED
        lda     ($9D),y
        beq     LD292
        sta     $96
        tax
        iny
        lda     ($9D),y
        sta     $97
        iny
LD2FC:  lda     ($96),y
        pha
        dey
        bpl     LD2FC
        ldy     $97
        jsr     LDAA6
        lda     $CA
        pha
        lda     $C9
        pha
        lda     ($9D),y
        sta     $C9
        iny
        lda     ($9D),y
        sta     $CA
        lda     $97
        pha
        lda     $96
        pha
        jsr     LCCA4
        pla
        sta     $9D
        pla
        sta     $9E
        jsr     L00C8
        beq     LD32D
        jmp     LCE1C

LD32D:  pla
        sta     $C9
        pla
        sta     $CA
LD333:  ldy     #$00
        pla
        sta     ($9D),y
        pla
        iny
        sta     ($9D),y
        pla
        iny
        sta     ($9D),y
        pla
        iny
        sta     ($9D),y
        pla
        iny
        sta     ($9D),y
        rts

        jsr     LCCA7
        ldy     #$00
        jsr     LDCB1
        pla
        pla
        lda     #$FF
        ldy     #$00
        beq     LD36B
LD359:  ldx     $B3
        ldy     $B4
        stx     $9F
        sty     $A0
LD361:  jsr     LD3D2
        stx     $B1
        sty     $B2
        sta     $B0
        rts

LD36B:  ldx     #$22
        stx     $5A
        stx     $5B
        sta     $BE
        sty     $BF
        sta     $B1
        sty     $B2
        ldy     #$FF
LD37B:  iny
        lda     ($BE),y
        beq     LD38C
        cmp     $5A
        beq     LD388
        cmp     $5B
        bne     LD37B
LD388:  cmp     #$22
        beq     LD38D
LD38C:  clc
LD38D:  sty     $B0
        tya
        adc     $BE
        sta     $C0
        ldx     $BF
        bcc     LD399
        inx
LD399:  stx     $C1
        lda     $BF
        bne     LD3AA
        tya
        jsr     LD359
        ldx     $BE
        ldy     $BF
        jsr     LD560
LD3AA:  ldx     $65
        cpx     #$71
        bne     LD3B5
        ldx     #$CC
LD3B2:  jmp     LC359

LD3B5:  lda     $B0
        sta     $00,x
        lda     $B1
        sta     $01,x
        lda     $B2
        sta     $02,x
        ldy     #$00
        stx     $B3
        sty     $B4
        dey
        sty     $5E
        stx     $66
        inx
        inx
        inx
        stx     $65
        rts

LD3D2:  lsr     $60
LD3D4:  pha
        eor     #$FF
        sec
        adc     $82
        ldy     $83
        bcs     LD3DF
        dey
LD3DF:  cpy     $81
        bcc     LD3F4
        bne     LD3E9
        cmp     $80
        bcc     LD3F4
LD3E9:  sta     $82
        sty     $83
        sta     $84
        sty     $85
        tax
        pla
        rts

LD3F4:  ldx     #$52
        lda     $60
        bmi     LD3B2
        jsr     LD404
        lda     #$80
        sta     $60
        pla
        bne     LD3D4
LD404:  ldx     $86
        lda     $87
LD408:  stx     $82
        sta     $83
        ldy     #$00
        sty     $9E
        lda     $80
        ldx     $81
        sta     $AE
        stx     $AF
        lda     #$68
        ldx     #$00
        sta     $71
        stx     $72
LD420:  cmp     $65
        beq     LD429
        jsr     LD4A1
        beq     LD420
LD429:  lda     #$07
        sta     $A2
        lda     $7C
        ldx     $7D
        sta     $71
        stx     $72
LD435:  cpx     $7F
        bne     LD43D
        cmp     $7E
        beq     LD442
LD43D:  jsr     LD497
        beq     LD435
LD442:  sta     $A7
        stx     $A8
        lda     #$03
        sta     $A2
LD44A:  lda     $A7
        ldx     $A8
LD44E:  cpx     $81
        bne     LD459
        cmp     $80
        bne     LD459
        jmp     LD4E0

LD459:  sta     $71
        stx     $72
        ldy     #$00
        lda     ($71),y
        tax
        iny
        lda     ($71),y
        php
        iny
        lda     ($71),y
        adc     $A7
        sta     $A7
        iny
        lda     ($71),y
        adc     $A8
        sta     $A8
        plp
        bpl     LD44A
        txa
        bmi     LD44A
        iny
        lda     ($71),y
        jsr     LE7F3
        adc     $71
        sta     $71
        bcc     LD488
        inc     $72
LD488:  ldx     $72
LD48A:  cpx     $A8
        bne     LD492
        cmp     $A7
        beq     LD44E
LD492:  jsr     LD4A1
        beq     LD48A
LD497:  lda     ($71),y
        bmi     LD4D0
        iny
        lda     ($71),y
        bpl     LD4D0
        iny
LD4A1:  lda     ($71),y
        beq     LD4D0
        iny
        lda     ($71),y
        tax
        iny
        lda     ($71),y
        cmp     $83
        bcc     LD4B6
        bne     LD4D0
        cpx     $82
        bcs     LD4D0
LD4B6:  cmp     $AF
        bcc     LD4D0
        bne     LD4C0
        cpx     $AE
        bcc     LD4D0
LD4C0:  stx     $AE
        sta     $AF
        lda     $71
        ldx     $72
        sta     $9D
        stx     $9E
        lda     $A2
        sta     L00A4
LD4D0:  lda     $A2
        clc
        adc     $71
        sta     $71
        bcc     LD4DB
        inc     $72
LD4DB:  ldx     $72
        ldy     #$00
        rts

LD4E0:  ldx     $9E
        beq     LD4DB
        lda     L00A4
        sbc     #$03
        lsr     a
        tay
        sta     L00A4
        lda     ($9D),y
        adc     $AE
        sta     $A9
        lda     $AF
        adc     #$00
        sta     $AA
        lda     $82
        ldx     $83
        sta     $A7
        stx     $A8
        jsr     LC2E1
        ldy     L00A4
        iny
        lda     $A7
        sta     ($9D),y
        tax
        inc     $A8
        lda     $A8
        iny
        sta     ($9D),y
        jmp     LD408

        lda     $B4
        pha
        lda     $B3
        pha
        jsr     LCD9D
        jsr     LCCA9
        pla
        sta     $BE
        pla
        sta     $BF
        ldy     #$00
        lda     ($BE),y
        clc
        adc     ($B3),y
        bcc     LD535
        ldx     #$B5
        jmp     LC359

LD535:  jsr     LD359
        jsr     LD552
        lda     $9F
        ldy     $A0
        jsr     LD582
        jsr     LD564
        lda     $BE
        ldy     $BF
        jsr     LD582
        jsr     LD3AA
        jmp     LCCD2

LD552:  ldy     #$00
        lda     ($BE),y
        pha
        iny
        lda     ($BE),y
        tax
        iny
        lda     ($BE),y
        tay
        pla
LD560:  stx     $71
        sty     $72
LD564:  tay
        beq     LD571
        pha
LD568:  dey
        lda     ($71),y
        sta     ($84),y
        tya
        bne     LD568
        pla
LD571:  clc
        adc     $84
        sta     $84
        bcc     LD57A
        inc     $85
LD57A:  rts

LD57B:  jsr     LCCA9
LD57E:  lda     $B3
        ldy     $B4
LD582:  sta     $71
        sty     $72
        jsr     LD5B3
        php
        ldy     #$00
        lda     ($71),y
        pha
        iny
        lda     ($71),y
        tax
        iny
        lda     ($71),y
        tay
        pla
        plp
        bne     LD5AE
        cpy     $83
        bne     LD5AE
        cpx     $82
        bne     LD5AE
        pha
        clc
        adc     $82
        sta     $82
        bcc     LD5AD
        inc     $83
LD5AD:  pla
LD5AE:  stx     $71
        sty     $72
        rts

LD5B3:  cpy     $67
        bne     LD5C3
        cmp     $66
        bne     LD5C3
        sta     $65
        sbc     #$03
        sta     $66
        ldy     #$00
LD5C3:  rts

        jsr     LD679
        txa
        pha
        lda     #$01
        jsr     LD361
        pla
        ldy     #$00
        sta     ($B1),y
        pla
        pla
        jmp     LD3AA

        jsr     LD637
        cmp     ($9F),y
        tya
LD5DE:  bcc     LD5E4
        lda     ($9F),y
        tax
        tya
LD5E4:  pha
LD5E5:  txa
LD5E6:  pha
        jsr     LD361
        lda     $9F
        ldy     $A0
        jsr     LD582
        pla
        tay
        pla
        clc
        adc     $71
        sta     $71
        bcc     LD5FD
        inc     $72
LD5FD:  tya
        jsr     LD564
        jmp     LD3AA

        jsr     LD637
        clc
        sbc     ($9F),y
        eor     #$FF
        jmp     LD5DE

        lda     #$FF
        sta     $B4
        jsr     L00C8
        cmp     #$29
        beq     LD620
        jsr     LCE11
        jsr     LD676
LD620:  jsr     LD637
        dex
        txa
        pha
        clc
        ldx     #$00
        sbc     ($9F),y
        bcs     LD5E5
        eor     #$FF
        cmp     $B4
        bcc     LD5E6
        lda     $B4
        bcs     LD5E6
LD637:  jsr     LCE0B
        pla
        sta     L00A4
        pla
        sta     $A5
        pla
        pla
        pla
        tax
        pla
        sta     $9F
        pla
        sta     $A0
        ldy     #$00
        txa
        beq     LD670
        inc     L00A4
        jmp     (L00A4)

        jsr     LD65A
LD657:  jmp     LD287

LD65A:  jsr     LD57B
        ldx     #$00
        stx     $5E
        tay
        rts

        jsr     LD65A
        beq     LD670
        ldy     #$00
        lda     ($71),y
        tay
        jmp     LD657

LD670:  jmp     LD130

        jsr     L00C2
LD676:  jsr     LCCA4
LD679:  jsr     LD0A3
        ldx     $B3
        bne     LD670
        ldx     $B4
        jmp     L00C8

        jsr     LD65A
        bne     LD68D
        jmp     LD7CC

LD68D:  ldx     $C9
        ldy     $CA
        stx     $C0
        sty     $C1
        ldx     $71
        stx     $C9
        clc
        adc     $71
        sta     $73
        ldx     $72
        stx     $CA
        bcc     LD6A5
        inx
LD6A5:  stx     $74
        ldy     #$00
        lda     ($73),y
        pha
        lda     #$00
        sta     ($73),y
        jsr     L00C8
        jsr     LDBC5
        pla
        ldy     #$00
        sta     ($73),y
        ldx     $C0
        ldy     $C1
        stx     $C9
        sty     $CA
        rts

LD6C4:  jsr     LCCA4
        jsr     LD6D0
LD6CA:  jsr     LCE11
        jmp     LD676

LD6D0:  lda     $B5
        bmi     LD670
        lda     $B0
        cmp     #$91
        bcs     LD670
        jsr     LDB6D
        lda     $B3
        ldy     $B4
        sty     $08
        sta     $09
        rts

        jsr     LD6D0
        ldy     #$00
        cmp     #$C0
        bcc     LD6F3
        cmp     #$E1
        bcc     LD6F6
LD6F3:  lda     ($08),y
        tay
LD6F6:  jmp     LD287

        jsr     LD6C4
        txa
        ldy     #$00
        sta     ($08),y
        rts

        jsr     LD6C4
        stx     $98
        ldx     #$00
        jsr     L00C8
        beq     LD711
        jsr     LD6CA
LD711:  stx     $99
        ldy     #$00
LD715:  lda     ($08),y
        eor     $99
        and     $98
        beq     LD715
LD71D:  rts

        lda     #$E3
        ldy     #$DD
        jmp     LD73C

        jsr     LD95E
        lda     $B5
        eor     #$FF
        sta     $B5
        eor     $BD
        sta     $BE
        lda     $B0
        jmp     LD73F

LD737:  jsr     LD86E
        bcc     LD778
LD73C:  jsr     LD95E
LD73F:  bne     LD744
        jmp     LDACE

LD744:  ldx     $BF
        stx     $A5
        ldx     #$B8
        lda     $B8
        tay
        beq     LD71D
        sec
        sbc     $B0
        beq     LD778
        bcc     LD768
        sty     $B0
        ldy     $BD
        sty     $B5
        eor     #$FF
        adc     #$00
        ldy     #$00
        sty     $A5
        ldx     #$B0
        bne     LD76C
LD768:  ldy     #$00
        sty     $BF
LD76C:  cmp     #$F9
        bmi     LD737
        tay
        lda     $BF
        lsr     $01,x
        jsr     LD885
LD778:  bit     $BE
        bpl     LD7D3
        ldy     #$B0
        cpx     #$B8
        beq     LD784
        ldy     #$B8
LD784:  sec
        eor     #$FF
        adc     $A5
        sta     $BF
        lda     $04,y
        sbc     $04,x
        sta     $B4
        lda     $03,y
        sbc     $03,x
        sta     $B3
        lda     $02,y
        sbc     $02,x
        sta     $B2
        lda     $01,y
        sbc     $01,x
        sta     $B1
        bcs     LD7AC
        jsr     LD81C
LD7AC:  ldy     #$00
        tya
        clc
LD7B0:  ldx     $B1
        bne     LD7FE
        ldx     $B2
        stx     $B1
        ldx     $B3
        stx     $B2
        ldx     $B4
        stx     $B3
        ldx     $BF
        stx     $B4
        sty     $BF
        adc     #$08
        cmp     #$20
        bne     LD7B0
LD7CC:  lda     #$00
        sta     $B0
        sta     $B5
        rts

LD7D3:  adc     $A5
        sta     $BF
        lda     $B4
        adc     $BC
        sta     $B4
        lda     $B3
        adc     $BB
        sta     $B3
        lda     $B2
        adc     $BA
        sta     $B2
        lda     $B1
        adc     $B9
        sta     $B1
        jmp     LD80B

LD7F2:  adc     #$01
        asl     $BF
        rol     $B4
        rol     $B3
        rol     $B2
        rol     $B1
LD7FE:  bpl     LD7F2
