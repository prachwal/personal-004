; da65 V2.18 - Ubuntu 2.19-1
; Created:    2026-09-09 21:53:17
; Input file: roms/vic20/kernal.bin
; Page:       1


        .setcpu "6502"

L0014           := $0014
L0079           := $0079
L028F           := $028F
L0314           := $0314
L0316           := $0316
L0318           := $0318
L031A           := $031A
L031C           := $031C
L031E           := $031E
L0320           := $0320
L0322           := $0322
L0324           := $0324
L0326           := $0326
L0328           := $0328
L032A           := $032A
L032C           := $032C
L0330           := $0330
L0332           := $0332
L0DA3           := $0DA3
L1D3B           := $1D3B
L2026           := $2026
L2A2A           := $2A2A
L3256           := $3256
L414F           := $414F
L4154           := $4154
L4243           := $4243
L435A           := $435A
L4552           := $4552
L4C50           := $4C50
L4E4F           := $4E4F
L5245           := $5245
L5246           := $5246
L5942           := $5942
L91CC           := $91CC
LA000           := $A000
LA002           := $A002
LC000           := $C000
LC002           := $C002
LC408           := $C408
LC437           := $C437
LC474           := $C474
LC52A           := $C52A
LC533           := $C533
LC644           := $C644
LC663           := $C663
LC677           := $C677
LC67A           := $C67A
LC68E           := $C68E
LCB1E           := $CB1E
LCD8A           := $CD8A
LCD9E           := $CD9E
LCEFD           := $CEFD
LCF08           := $CF08
LD6A3           := $D6A3
LD79E           := $D79E
LD7F7           := $D7F7
LD849           := $D849
LD850           := $D850
LD853           := $D853
LD867           := $D867
LD8D7           := $D8D7
LDA28           := $DA28
LDAB9           := $DAB9
LDAD4           := $DAD4
LDB07           := $DB07
LDB0F           := $DB0F
LDBA2           := $DBA2
LDBC7           := $DBC7
LDBCA           := $DBCA
LDBD4           := $DBD4
LDC0C           := $DC0C
LDC2B           := $DC2B
LDCCC           := $DCCC
LDDCD           := $DDCD
LDFB4           := $DFB4
        .byte   $0F
        .byte   $DC
        lda     $61
        cmp     #$88
        bcc     LE00B
LE008:  jsr     LDAD4
LE00B:  jsr     LDCCC
        lda     $07
        clc
        adc     #$81
        beq     LE008
        sec
        sbc     #$01
        pha
        ldx     #$05
LE01B:  lda     $69,x
        ldy     $61,x
        sta     $61,x
        sty     $69,x
        dex
        bpl     LE01B
        lda     $56
        sta     $70
        jsr     LD853
        jsr     LDFB4
        lda     #$C4
        ldy     #$DF
        jsr     LE056
        lda     #$00
        sta     $6F
        pla
        jsr     LDAB9
        rts

LE040:  sta     $71
        sty     $72
        jsr     LDBCA
        lda     #$57
        jsr     LDA28
        jsr     LE05A
        lda     #$57
        ldy     #$00
        jmp     LDA28

LE056:  sta     $71
        sty     $72
LE05A:  jsr     LDBC7
        .byte   $B1
LE05E:  adc     ($85),y
        .byte   $67
        ldy     $71
        iny
        tya
        bne     LE069
        inc     $72
LE069:  sta     $71
        ldy     $72
LE06D:  jsr     LDA28
        lda     $71
        ldy     $72
        clc
        adc     #$05
        bcc     LE07A
        iny
LE07A:  sta     $71
        sty     $72
        jsr     LD867
        lda     #$5C
        ldy     #$00
        dec     $67
        bne     LE06D
        rts

        tya
        and     $44,x
        .byte   $7A
        brk
        pla
        plp
        lda     ($46),y
        brk
        jsr     LDC2B
        bmi     LE0D0
        bne     LE0BB
        jsr     LFFF3
        stx     $22
        sty     $23
        ldy     #$04
        lda     ($22),y
        sta     $62
        iny
        lda     ($22),y
        sta     $64
        ldy     #$08
        lda     ($22),y
        sta     $63
        iny
        lda     ($22),y
        sta     $65
        jmp     LE0E0

LE0BB:  lda     #$8B
        ldy     #$00
        jsr     LDBA2
        lda     #$8A
        ldy     #$E0
        jsr     LDA28
        lda     #$8F
        ldy     #$E0
        jsr     LD867
LE0D0:  ldx     $65
        lda     $62
        sta     $65
        stx     $62
        ldx     $63
        lda     $64
        sta     $63
        stx     $64
LE0E0:  lda     #$00
        sta     $66
        lda     $61
        sta     $70
        lda     #$80
        sta     $61
        jsr     LD8D7
        ldx     #$8B
        ldy     #$00
LE0F3:  jmp     LDBD4

LE0F6:  cmp     #$F0
        bne     LE101
        sty     $38
        stx     $37
        jmp     LC663

LE101:  tax
        bne     LE106
        ldx     #$1E
LE106:  jmp     LC437

        jsr     LFFD2
        bcs     LE0F6
        rts

        jsr     LFFCF
        bcs     LE0F6
        rts

        jsr     LFFC9
        bcs     LE0F6
        rts

        jsr     LFFC6
        bcs     LE0F6
        rts

        jsr     LFFE4
        bcs     LE0F6
        rts

        jsr     LCD8A
        jsr     LD7F7
        lda     #$E1
        pha
        lda     #$43
        pha
        lda     $030F
        pha
        lda     $030C
        ldx     $030D
        ldy     $030E
        plp
        jmp     (L0014)

        php
        sta     $030C
        stx     $030D
        sty     $030E
        pla
        sta     $030F
        rts

        jsr     LE1D1
        ldx     $2D
        ldy     $2E
        lda     #$2B
        jsr     LFFD8
        bcs     LE0F6
        rts

        lda     #$01
        bit     a:$A9
        sta     $0A
        jsr     LE1D1
        lda     $0A
        ldx     $2B
        ldy     $2C
        jsr     LFFD5
        bcs     LE1CE
        lda     $0A
        beq     LE195
        ldx     #$1C
        jsr     LFFB7
        and     #$10
        beq     LE187
        jmp     LC437

LE187:  lda     $7A
        cmp     #$02
        beq     LE194
        lda     #$64
        ldy     #$C3
        jmp     LCB1E

LE194:  rts

LE195:  jsr     LFFB7
        and     #$BF
        beq     LE1A1
        ldx     #$1D
        jmp     LC437

LE1A1:  lda     $7B
        cmp     #$02
        bne     LE1B5
        stx     $2D
        sty     $2E
        lda     #$76
        ldy     #$C3
        jsr     LCB1E
        jmp     LC52A

LE1B5:  jsr     LC68E
        jmp     LE476

        jsr     LE216
        jsr     LFFC0
        bcs     LE1CE
        rts

        jsr     LE216
        lda     $49
        jsr     LFFC3
        bcc     LE194
LE1CE:  jmp     LE0F6

LE1D1:  lda     #$00
        jsr     LFFBD
        ldx     #$01
        ldy     #$00
        jsr     LFFBA
        jsr     LE203
        jsr     LE254
        jsr     LE203
        jsr     LE1FD
        ldy     #$00
        stx     $49
        jsr     LFFBA
        jsr     LE203
        jsr     LE1FD
        txa
        tay
        ldx     $49
        jmp     LFFBA

LE1FD:  jsr     LE20B
        jmp     LD79E

LE203:  jsr     L0079
        bne     LE20A
        pla
        pla
LE20A:  rts

LE20B:  jsr     LCEFD
LE20E:  jsr     L0079
        bne     LE20A
        jmp     LCF08

LE216:  lda     #$00
        jsr     LFFBD
        jsr     LE20E
        jsr     LD79E
        stx     $49
        txa
        ldx     #$01
        ldy     #$00
        jsr     LFFBA
        jsr     LE203
        jsr     LE1FD
        stx     $4A
        ldy     #$00
        lda     $49
        cpx     #$03
        bcc     LE23C
        dey
LE23C:  jsr     LFFBA
        jsr     LE203
        jsr     LE1FD
        txa
        tay
        ldx     $4A
        lda     $49
        jsr     LFFBA
        jsr     LE203
        jsr     LE20B
LE254:  jsr     LCD9E
        jsr     LD6A3
        ldx     $22
        ldy     $23
        jmp     LFFBD

        lda     #$DD
        ldy     #$E2
        jsr     LD867
LE268:  jsr     LDC0C
        lda     #$E2
        ldy     #$E2
        ldx     $6E
        jsr     LDB07
        jsr     LDC0C
        jsr     LDCCC
        lda     #$00
        sta     $6F
        jsr     LD853
        lda     #$E7
        ldy     #$E2
        jsr     LD850
        lda     $66
        pha
        bpl     LE29A
        jsr     LD849
        lda     $66
        bmi     LE29D
        lda     $12
        eor     #$FF
        sta     $12
LE29A:  jsr     LDFB4
LE29D:  lda     #$E7
        ldy     #$E2
        jsr     LD867
        pla
        bpl     LE2AA
        jsr     LDFB4
LE2AA:  lda     #$EC
        ldy     #$E2
        jmp     LE040

        jsr     LDBCA
        lda     #$00
        sta     $12
        jsr     LE268
        ldx     #$4E
        ldy     #$00
        jsr     LE0F3
        lda     #$57
        ldy     #$00
        jsr     LDBA2
        lda     #$00
        sta     $66
        lda     $12
        jsr     LE2D9
        lda     #$4E
        ldy     #$00
        jmp     LDB0F

LE2D9:  pha
        jmp     LE29A

        sta     ($49,x)
        .byte   $0F
        .byte   $DA
        ldx     #$83
        eor     #$0F
        .byte   $DA
        ldx     #$7F
        brk
        brk
        brk
        brk
        ora     $84
        inc     $1A
        and     $861B
        plp
        .byte   $07
        .byte   $FB
        sed
        .byte   $87
        sta     $8968,y
        ora     ($87,x)
        .byte   $23
        and     $DF,x
        sbc     ($86,x)
        lda     $5D
        .byte   $E7
        plp
        .byte   $83
        eor     #$0F
        .byte   $DA
        ldx     #$A5
        ror     $48
        bpl     LE313
        jsr     LDFB4
LE313:  lda     $61
        pha
        cmp     #$81
        bcc     LE321
        lda     #$BC
        ldy     #$D9
        jsr     LDB0F
LE321:  lda     #$3B
        ldy     #$E3
        jsr     LE040
        pla
        cmp     #$81
        bcc     LE334
        lda     #$DD
        ldy     #$E2
        jsr     LD850
LE334:  pla
        bpl     LE33A
        jmp     LDFB4

LE33A:  rts

        .byte   $0B
        ror     $B3,x
        .byte   $83
        lda     $79D3,x
        asl     $A6F4,x
        sbc     $7B,x
        .byte   $83
        .byte   $FC
        bcs     LE35B
        .byte   $7C
        .byte   $0C
        .byte   $1F
        .byte   $67
        dex
        .byte   $7C
        dec     $CB53,x
        cmp     ($7D,x)
        .byte   $14
        .byte   $64
        bvs     LE3A6
        .byte   $7D
LE35B:  .byte   $B7
        nop
        eor     ($7A),y
        adc     $3063,x
        dey
        ror     $927E,x
        .byte   $44
        sta     $7E3A,y
        jmp     L91CC

        .byte   $C7
        .byte   $7F
        tax
        tax
        tax
        .byte   $13
        sta     ($00,x)
        brk
        brk
        brk
        jsr     LE45B
        jsr     LE3A4
        jsr     LE404
        ldx     #$FB
        txs
        jmp     LC474

LE387:  inc     $7A
        bne     LE38D
        inc     $7B
LE38D:  lda     LEA60
        cmp     #$3A
        bcs     LE39E
        cmp     #$20
        beq     LE387
        sec
        sbc     #$30
        sec
        sbc     #$D0
LE39E:  rts

        .byte   $80
        .byte   $4F
        .byte   $C7
        .byte   $52
        cli
LE3A4:  lda     #$4C
LE3A6:  sta     $54
        sta     $00
        lda     #$48
        ldy     #$D2
        sta     $01
        sty     $02
        lda     #$91
        ldy     #$D3
        sta     $05
        sty     $06
        lda     #$AA
        ldy     #$D1
        sta     $03
        sty     $04
        ldx     #$1C
LE3C4:  lda     LE387,x
        sta     $73,x
        dex
        bpl     LE3C4
        lda     #$03
        sta     $53
        lda     #$00
        sta     $68
        sta     $13
        sta     $18
        ldx     #$01
        stx     $01FD
        stx     $01FC
        ldx     #$19
        stx     $16
        sec
        jsr     LFF9C
        stx     $2B
        sty     $2C
        sec
        jsr     LFF99
        stx     $37
        sty     $38
        stx     $33
        sty     $34
        ldy     #$00
        tya
        sta     ($2B),y
        inc     $2B
        bne     LE403
        inc     $2C
LE403:  rts

LE404:  lda     $2B
        ldy     $2C
        jsr     LC408
        lda     #$36
        ldy     #$E4
        jsr     LCB1E
        lda     $37
        sec
        sbc     $2B
        tax
        lda     $38
        sbc     $2C
        jsr     LDDCD
        lda     #$29
        ldy     #$E4
        jsr     LCB1E
        jmp     LC644

        jsr     L5942
        .byte   $54
        eor     $53
        jsr     L5246
        eor     $45
        ora     $9300
        rol     a
        rol     a
        rol     a
        rol     a
        jsr     L4243
        eor     $4220
        eor     ($53,x)
        eor     #$43
        jsr     L3256
        jsr     L2A2A
        rol     a
        rol     a
        .byte   $0D
        brk
LE44F:  .byte   $3A
        cpy     $83
        cpy     $7C
        cmp     $1A
        .byte   $C7
        cpx     $C7
        stx     $CE
LE45B:  ldx     #$0B
LE45D:  lda     LE44F,x
        sta     $0300,x
        dex
        bpl     LE45D
        rts

        jsr     LFFCC
        lda     #$00
        sta     $13
        jsr     LC67A
        cli
        jmp     LC474

        inx
LE476:  jsr     LC533
        jmp     LC677

        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
LE4A0:  lda     $912C
        and     #$DF
        sta     $912C
        rts

LE4A9:  lda     $912C
        ora     #$20
        sta     $912C
        rts

LE4B2:  lda     $911F
        cmp     $911F
        bne     LE4B2
        lsr     a
        rts

LE4BC:  ldx     $B9
        jmp     LF647

LE4C1:  txa
        bne     LE4CC
        lda     $C3
        sta     $AE
        lda     $C4
        sta     $AF
LE4CC:  jmp     LF66A

LE4CF:  jsr     LF8E3
        bcc     LE4D7
        pla
        lda     #$00
LE4D7:  jmp     LF39E

        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
LE500:  ldx     #$10
        ldy     #$91
        rts

LE505:  ldx     #$16
        ldy     #$17
        rts

LE50A:  bcs     LE513
        stx     $D6
        sty     $D3
        jsr     LE587
LE513:  ldx     $D6
        ldy     $D3
        rts

LE518:  jsr     LE5BB
        lda     $0288
        and     #$FD
        asl     a
        asl     a
        ora     #$80
        sta     $9005
        lda     $0288
        and     #$02
        beq     LE536
        lda     #$80
        ora     $9002
        sta     $9002
LE536:  lda     #$00
        sta     $0291
        sta     $CF
        lda     #$DC
        sta     L028F
        lda     #$EB
        sta     $0290
        lda     #$0A
        sta     $0289
        sta     $028C
        lda     #$06
        sta     $0286
        lda     #$04
        sta     $028B
        lda     #$0C
        sta     $CD
        sta     $CC
LE55F:  lda     $0288
        ora     #$80
        tay
        lda     #$00
        tax
LE568:  sty     $D9,x
        clc
        adc     #$16
        bcc     LE570
        iny
LE570:  inx
        cpx     #$18
        bne     LE568
        lda     #$FF
        sta     $D9,x
        ldx     #$16
LE57B:  jsr     LEA8D
        dex
        bpl     LE57B
LE581:  ldy     #$00
        sty     $D3
        sty     $D6
LE587:  ldx     $D6
        lda     $D3
LE58B:  ldy     $D9,x
        bmi     LE597
        clc
        adc     #$16
        sta     $D3
        dex
        bpl     LE58B
LE597:  lda     $D9,x
        and     #$03
        ora     $0288
        sta     $D2
        lda     LEDFD,x
        sta     $D1
        lda     #$15
        inx
LE5A8:  ldy     $D9,x
        bmi     LE5B2
        clc
        adc     #$16
        inx
        bpl     LE5A8
LE5B2:  sta     $D5
        rts

        jsr     LE5BB
        jmp     LE581

LE5BB:  lda     #$03
        sta     $9A
        lda     #$00
        sta     $99
LE5C3:  ldx     #$10
LE5C5:  lda     LEDE3,x
        sta     $8FFF,x
        dex
        bne     LE5C5
        rts

LE5CF:  ldy     $0277
        ldx     #$00
LE5D4:  lda     $0278,x
        sta     $0277,x
        inx
        cpx     $C6
        bne     LE5D4
        dec     $C6
        tya
        cli
        clc
        rts

LE5E5:  jsr     LE742
LE5E8:  lda     $C6
        sta     $CC
        sta     $0292
        beq     LE5E8
        sei
        lda     $CF
        beq     LE602
        lda     $CE
        ldx     $0287
        ldy     #$00
        sty     $CF
        jsr     LEAA1
LE602:  jsr     LE5CF
        cmp     #$83
        bne     LE619
        ldx     #$09
        sei
        stx     $C6
LE60E:  lda     LEDF3,x
        sta     $0276,x
        dex
        bne     LE60E
        beq     LE5E8
LE619:  cmp     #$0D
        bne     LE5E5
        ldy     $D5
        sty     $D0
LE621:  lda     ($D1),y
        cmp     #$20
        bne     LE62A
        dey
        bne     LE621
LE62A:  iny
        sty     $C8
        ldy     #$00
        sty     $0292
        sty     $D3
        sty     $D4
        lda     $C9
        bmi     LE657
        ldx     $D6
        jsr     LE719
        cpx     $C9
        bne     LE657
        bne     LE657
        lda     $CA
        sta     $D3
        cmp     $C8
        bcc     LE657
        bcs     LE691
LE64F:  tya
        pha
        txa
        pha
        lda     $D0
        beq     LE5E8
LE657:  ldy     $D3
        lda     ($D1),y
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        sta     $D7
        and     #$3F
        asl     $D7
        bit     $D7
        bpl     LE67E
        ora     #$80
LE67E:  bcc     LE684
        ldx     $D4
        bne     LE688
LE684:  bvs     LE688
        ora     #$40
LE688:  inc     $D3
        jsr     LE6B8
        cpy     $C8
        bne     LE6A8
LE691:  lda     #$00
        sta     $D0
        lda     #$0D
        ldx     $99
        cpx     #$03
        beq     LE6A3
        ldx     $9A
        cpx     #$03
        beq     LE6A6
LE6A3:  jsr     LE742
LE6A6:  lda     #$0D
LE6A8:  sta     $D7
        pla
        tax
        pla
        tay
        lda     $D7
        cmp     #$DE
        bne     LE6B6
        lda     #$FF
LE6B6:  clc
        rts

LE6B8:  cmp     #$22
        bne     LE6C4
        lda     $D4
        eor     #$01
        sta     $D4
        lda     #$22
LE6C4:  rts

LE6C5:  ora     #$40
LE6C7:  ldx     $C7
        beq     LE6CD
LE6CB:  ora     #$80
LE6CD:  ldx     $D8
        beq     LE6D3
        dec     $D8
LE6D3:  ldx     $0286
        jsr     LEAA1
        jsr     LE6EA
LE6DC:  pla
        tay
        lda     $D8
        beq     LE6E4
        lsr     $D4
LE6E4:  pla
        tax
        pla
        clc
        cli
        rts

LE6EA:  jsr     LE8FA
        inc     $D3
        lda     $D5
        cmp     $D3
        bcs     LE72C
        cmp     #$57
        beq     LE723
        lda     $0292
        beq     LE701
        jmp     LE9F0

LE701:  ldx     $D6
        cpx     #$17
        bcc     LE70E
        jsr     LE975
        dec     $D6
        ldx     $D6
LE70E:  asl     $D9,x
        lsr     $D9,x
        jmp     LED5B

LE715:  adc     #$16
        sta     $D5
LE719:  lda     $D9,x
        bmi     LE720
        dex
        bne     LE719
LE720:  jmp     LEA7E

LE723:  dec     $D6
        jsr     LE8C3
        lda     #$00
        sta     $D3
LE72C:  rts

LE72D:  ldx     $D6
        bne     LE737
        stx     $D3
        pla
        pla
        bne     LE6DC
LE737:  dex
        stx     $D6
        jsr     LE587
        ldy     $D5
        sty     $D3
        rts

LE742:  pha
        sta     $D7
        txa
        pha
        tya
        pha
        lda     #$00
        sta     $D0
        ldy     $D3
        lda     $D7
        bpl     LE756
        jmp     LE800

LE756:  cmp     #$0D
        bne     LE75D
        jmp     LE8D8

LE75D:  cmp     #$20
        bcc     LE771
        cmp     #$60
        bcc     LE769
        and     #$DF
        bne     LE76B
LE769:  and     #$3F
LE76B:  jsr     LE6B8
        jmp     LE6C7

LE771:  ldx     $D8
        beq     LE778
        jmp     LE6CB

LE778:  cmp     #$14
        bne     LE7AA
        tya
        bne     LE785
        jsr     LE72D
        jmp     LE79F

LE785:  jsr     LE8E8
        dey
        sty     $D3
        jsr     LEAB2
LE78E:  iny
        lda     ($D1),y
        dey
        sta     ($D1),y
        iny
        lda     ($F3),y
        dey
        sta     ($F3),y
        iny
        cpy     $D5
        bne     LE78E
LE79F:  lda     #$20
        sta     ($D1),y
        lda     $0286
        sta     ($F3),y
        bpl     LE7F7
LE7AA:  ldx     $D4
        beq     LE7B1
        jmp     LE6CB

LE7B1:  cmp     #$12
        bne     LE7B7
        sta     $C7
LE7B7:  cmp     #$13
        bne     LE7BE
        jsr     LE581
LE7BE:  cmp     #$1D
        bne     LE7D9
        iny
        jsr     LE8FA
        sty     $D3
        dey
        cpy     $D5
        bcc     LE7D6
        dec     $D6
        jsr     LE8C3
        ldy     #$00
LE7D4:  sty     $D3
LE7D6:  jmp     LE6DC

LE7D9:  cmp     #$11
        bne     LE7FA
        clc
        tya
        adc     #$16
        tay
        inc     $D6
        cmp     $D5
        bcc     LE7D4
        beq     LE7D4
        dec     $D6
LE7EC:  sbc     #$16
        bcc     LE7F4
        sta     $D3
        bne     LE7EC
LE7F4:  jsr     LE8C3
LE7F7:  jmp     LE6DC

LE7FA:  jsr     LE912
        jmp     LED21

LE800:  nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        and     #$7F
        cmp     #$7F
        bne     LE81D
        lda     #$5E
LE81D:  nop
        nop
        nop
        nop
        nop
        nop
        cmp     #$20
        bcc     LE82A
        jmp     LE6C5

LE82A:  cmp     #$0D
        bne     LE831
        jmp     LE8D8

LE831:  ldx     $D4
        bne     LE874
        cmp     #$14
        bne     LE870
        ldy     $D5
        lda     ($D1),y
        cmp     #$20
        bne     LE845
        cpy     $D3
        bne     LE84C
LE845:  cpy     #$57
        beq     LE86D
        jsr     LE9EE
LE84C:  ldy     $D5
        jsr     LEAB2
LE851:  dey
        lda     ($D1),y
        iny
        sta     ($D1),y
        dey
        lda     ($F3),y
        iny
        sta     ($F3),y
        dey
        cpy     $D3
        bne     LE851
        lda     #$20
        sta     ($D1),y
        lda     $0286
        sta     ($F3),y
        inc     $D8
LE86D:  jmp     LE6DC

LE870:  ldx     $D8
        beq     LE879
LE874:  ora     #$40
        jmp     LE6CB

LE879:  cmp     #$11
        bne     LE893
        ldx     $D6
        beq     LE8B8
        dec     $D6
        lda     $D3
        sec
        sbc     #$16
        bcc     LE88E
        sta     $D3
        bpl     LE8B8
LE88E:  jsr     LE587
        bne     LE8B8
LE893:  cmp     #$12
        bne     LE89B
        lda     #$00
        sta     $C7
LE89B:  cmp     #$1D
        bne     LE8B1
        tya
        beq     LE8AB
        jsr     LE8E8
        dey
        sty     $D3
        jmp     LE6DC

LE8AB:  jsr     LE72D
        jmp     LE6DC

LE8B1:  cmp     #$13
        bne     LE8BB
        jsr     LE55F
LE8B8:  jmp     LE6DC

LE8BB:  ora     #$80
        jsr     LE912
        jmp     LED30

LE8C3:  lsr     $C9
        ldx     $D6
LE8C7:  inx
        cpx     #$17
        bne     LE8CF
        jsr     LE975
LE8CF:  lda     $D9,x
        bpl     LE8C7
        stx     $D6
        jmp     LE587

LE8D8:  ldx     #$00
        stx     $D8
        stx     $C7
        stx     $D4
        stx     $D3
        jsr     LE8C3
        jmp     LE6DC

LE8E8:  ldx     #$04
        lda     #$00
LE8EC:  cmp     $D3
        beq     LE8F7
        clc
        adc     #$16
        dex
        bne     LE8EC
        rts

LE8F7:  dec     $D6
        rts

LE8FA:  ldx     #$04
        lda     #$15
LE8FE:  cmp     $D3
        beq     LE909
        clc
        adc     #$16
        dex
        bne     LE8FE
        rts

LE909:  ldx     $D6
        cpx     #$17
        beq     LE911
        inc     $D6
LE911:  rts

LE912:  ldx     #$07
LE914:  cmp     LE921,x
        beq     LE91D
        dex
        bpl     LE914
        rts

LE91D:  stx     $0286
        rts

LE921:  bcc     LE928
        .byte   $1C
        .byte   $9F
        .byte   $9C
        .byte   $1E
        .byte   $1F
LE928:  .byte   $9E
        .byte   $EF
        lda     ($DF,x)
        ldx     $E1
        lda     ($E2),y
        .byte   $B2
        .byte   $E3
        .byte   $B3
        cpx     $B4
        sbc     $B5
        inc     $B6
        .byte   $E7
        .byte   $B7
        inx
        clv
        sbc     #$B9
        .byte   $FA
        tsx
        .byte   $FB
        .byte   $BB
        .byte   $FC
        ldy     $BDEC,x
        inc     $84BE,x
        .byte   $BF
        .byte   $F7
        cpy     #$F8
        .byte   $DB
        sbc     LEADD,y
        dec     LE05E,x
        .byte   $5B
        sbc     ($5D,x)
        .byte   $E2
        rti

        bcs     LE9BD
        lda     ($78),y
        .byte   $DB
        adc     $66DD,y
        ldx     $77,y
        cpy     #$70
        beq     LE9D9
        sbc     ($72),y
        .byte   $F2
        .byte   $73
        .byte   $F3
        .byte   $74
        .byte   $F4
        adc     $F5,x
        ror     $F6,x
        .byte   $7D
        .byte   $FD
LE975:  lda     $AC
        pha
        lda     $AD
        pha
        lda     $AE
        pha
        lda     $AF
        pha
LE981:  ldx     #$FF
        dec     $D6
        dec     $C9
        dec     $F2
LE989:  inx
        jsr     LEA7E
        cpx     #$16
        bcs     LE99D
        lda     LEDFE,x
        sta     $AC
        lda     $DA,x
        jsr     LEA56
        bmi     LE989
LE99D:  jsr     LEA8D
        ldx     #$00
LE9A2:  lda     $D9,x
        and     #$7F
        ldy     $DA,x
        bpl     LE9AC
        ora     #$80
LE9AC:  sta     $D9,x
        inx
        cpx     #$16
        bne     LE9A2
        lda     $EF
        ora     #$80
        sta     $EF
        lda     $D9
        bpl     LE981
LE9BD:  inc     $D6
        inc     $F2
        lda     #$FB
        sta     $9120
        lda     $9121
        cmp     #$FE
        php
        lda     #$F7
        sta     $9120
        plp
        bne     LE9DF
        ldy     #$00
LE9D6:  nop
        dex
        .byte   $D0
LE9D9:  .byte   $FC
        dey
        bne     LE9D6
        sty     $C6
LE9DF:  ldx     $D6
        pla
        sta     $AF
        pla
        sta     $AE
        pla
        sta     $AD
        pla
        sta     $AC
        rts

LE9EE:  ldx     $D6
LE9F0:  inx
        lda     $D9,x
        bpl     LE9F0
        stx     $F2
        cpx     #$16
        beq     LEA08
        bcc     LEA08
        jsr     LE975
        ldx     $F2
        dex
        dec     $D6
        jmp     LE70E

LEA08:  lda     $AC
        pha
        lda     $AD
        pha
        lda     $AE
        pha
        lda     $AF
        pha
        ldx     #$17
LEA16:  dex
        jsr     LEA7E
        cpx     $F2
        bcc     LEA2C
        beq     LEA2C
        lda     LEDFC,x
        sta     $AC
        lda     $D8,x
        jsr     LEA56
        bmi     LEA16
LEA2C:  jsr     LEA8D
        ldx     #$15
LEA31:  cpx     $F2
        bcc     LEA44
        lda     $DA,x
        and     #$7F
        ldy     $D9,x
        bpl     LEA3F
        ora     #$80
LEA3F:  sta     $DA,x
        dex
        bne     LEA31
LEA44:  ldx     $F2
        jsr     LE70E
        pla
        sta     $AF
        pla
        sta     $AE
        pla
        sta     $AD
        pla
        sta     $AC
        rts

LEA56:  and     #$03
        ora     $0288
        sta     $AD
        jsr     LEA6E
LEA60:  ldy     #$15
LEA62:  lda     ($AC),y
        sta     ($D1),y
        lda     ($AE),y
        sta     ($F3),y
        dey
        bpl     LEA62
        rts

LEA6E:  jsr     LEAB2
        lda     $AC
        sta     $AE
        lda     $AD
        and     #$03
        ora     #$94
        sta     $AF
        rts

LEA7E:  lda     LEDFD,x
        sta     $D1
        lda     $D9,x
        and     #$03
        ora     $0288
        sta     $D2
        rts

LEA8D:  ldy     #$15
        jsr     LEA7E
        jsr     LEAB2
LEA95:  lda     #$20
        sta     ($D1),y
        lda     #$01
        sta     ($F3),y
        dey
        bpl     LEA95
        rts

LEAA1:  tay
        lda     #$02
        sta     $CD
        jsr     LEAB2
        tya
LEAAA:  ldy     $D3
        sta     ($D1),y
        txa
        sta     ($F3),y
        rts

LEAB2:  lda     $D1
        sta     $F3
        lda     $D2
        and     #$03
        ora     #$94
        sta     $F4
        rts

        jsr     LFFEA
        lda     $CC
        bne     LEAEF
        dec     $CD
        bne     LEAEF
        lda     #$14
        sta     $CD
        ldy     $D3
        lsr     $CF
        ldx     $0287
        lda     ($D1),y
        bcs     LEAEA
        inc     $CF
        sta     $CE
LEADD:  jsr     LEAB2
        lda     ($F3),y
        sta     $0287
        ldx     $0286
        lda     $CE
LEAEA:  eor     #$80
        jsr     LEAAA
LEAEF:  lda     $911F
        and     #$40
        beq     LEB01
        ldy     #$00
        sty     $C0
        lda     $911C
        ora     #$02
        bne     LEB0A
LEB01:  lda     $C0
        bne     LEB12
        lda     $911C
        and     #$FD
LEB0A:  bit     $911E
        bvs     LEB12
        sta     $911C
LEB12:  jsr     LEB1E
        bit     $9124
        pla
        tay
        pla
        tax
        pla
        rti

LEB1E:  lda     #$00
        sta     $028D
        ldy     #$40
        sty     $CB
        sta     $9120
        ldx     $9121
        cpx     #$FF
        beq     LEB8F
        lda     #$FE
        sta     $9120
        ldy     #$00
        lda     #$5E
        sta     $F5
        lda     #$EC
        sta     $F6
LEB40:  ldx     #$08
        lda     $9121
        cmp     $9121
        bne     LEB40
LEB4A:  lsr     a
        bcs     LEB63
        pha
        lda     ($F5),y
        cmp     #$05
        bcs     LEB60
        cmp     #$03
        beq     LEB60
        ora     $028D
        sta     $028D
        bpl     LEB62
LEB60:  sty     $CB
LEB62:  pla
LEB63:  iny
        cpy     #$41
        bcs     LEB71
        dex
        bne     LEB4A
        sec
        rol     $9120
        bne     LEB40
LEB71:  jmp     (L028F)

LEB74:  ldy     $CB
        lda     ($F5),y
        tax
        cpy     $C5
        beq     LEB84
        ldy     #$10
        sty     $028C
        bne     LEBBA
LEB84:  and     #$7F
        bit     $028A
        bmi     LEBA1
        bvs     LEBD6
        cmp     #$7F
LEB8F:  beq     LEBBA
        cmp     #$14
        beq     LEBA1
        cmp     #$20
        beq     LEBA1
        cmp     #$1D
        beq     LEBA1
        cmp     #$11
        bne     LEBD6
LEBA1:  ldy     $028C
        beq     LEBAB
        dec     $028C
        bne     LEBD6
LEBAB:  dec     $028B
        bne     LEBD6
        ldy     #$04
        sty     $028B
        ldy     $C6
        dey
        bpl     LEBD6
LEBBA:  ldy     $CB
        sty     $C5
        ldy     $028D
        sty     $028E
        cpx     #$FF
        beq     LEBD6
        txa
        ldx     $C6
        cpx     $0289
        bcs     LEBD6
        sta     $0277,x
        inx
        stx     $C6
LEBD6:  lda     #$F7
        sta     $9120
        rts

        lda     $028D
        cmp     #$03
        bne     LEC0F
        cmp     $028E
        beq     LEBD6
        lda     $0291
        bmi     LEC43
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        lda     $9005
        eor     #$02
        sta     $9005
        nop
        nop
        nop
        nop
        jmp     LEC43

LEC0F:  asl     a
        cmp     #$08
        bcc     LEC18
        lda     #$06
        nop
        nop
LEC18:  nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        tax
        lda     LEC46,x
        sta     $F5
        lda     LEC47,x
        sta     $F6
LEC43:  jmp     LEB74

LEC46:  .byte   $5E
LEC47:  cpx     LEC9F
        cpx     #$EC
        .byte   $A3
        sbc     LEC5E
        .byte   $9F
        cpx     LED69
        .byte   $A3
        sbc     LED21
        adc     #$ED
        adc     #$ED
        .byte   $A3
        .byte   $ED
LEC5E:  and     ($33),y
        and     $37,x
        and     $5C2B,y
        .byte   $14
        .byte   $5F
        .byte   $57
        .byte   $52
        eor     $5049,y
        rol     a
        ora     $4104
        .byte   $44
        .byte   $47
        lsr     a
        jmp     L1D3B

        .byte   $03
        ora     ($58,x)
        lsr     $4E,x
        bit     $112F
        jsr     L435A
        .byte   $42
        eor     $012E
        sta     $02
        .byte   $53
        lsr     $48
        .byte   $4B
        .byte   $3A
        and     $5186,x
        eor     $54
        eor     $4F,x
        rti

        lsr     $3287,x
        .byte   $34
        rol     $38,x
        bmi     LECC9
        .byte   $13
        dey
        .byte   $FF
LEC9F:  and     ($23,x)
        and     $27
        and     #$DB
        lda     #$94
        .byte   $5F
        .byte   $D7
        .byte   $D2
        cmp     $D0C9,y
        cpy     #$8D
        .byte   $04
        cmp     ($C4,x)
        .byte   $C7
        dex
        cpy     $9D5D
        .byte   $83
        ora     ($D8,x)
LECBA:  dec     $CE,x
        .byte   $3C
        .byte   $3F
        sta     ($A0),y
        .byte   $DA
        .byte   $C3
        .byte   $C2
        cmp     $013E
        .byte   $89
        .byte   $02
        .byte   $D3
LECC9:  dec     $C8
        .byte   $CB
        .byte   $5B
        and     $D18A,x
        cmp     $D4
        cmp     $CF,x
        tsx
        dec     $228B,x
        bit     $26
        plp
        bmi     LECBA
        .byte   $93
        sty     $21FF
        .byte   $23
        and     $27
        and     #$A6
        tay
        sty     $5F,x
        .byte   $B3
        .byte   $B2
        .byte   $B7
        ldx     #$AF
        .byte   $DF
        sta     $B004
        ldy     $B5A5
        ldx     $5D,y
        sta     $0183,x
LECFA:  lda     $AABE,x
        .byte   $3C
        .byte   $3F
        sta     ($A0),y
        lda     $BFBC
        .byte   $A7
        rol     $8901,x
        .byte   $02
        ldx     $B4BB
        lda     ($5B,x)
        and     $AB8A,x
        lda     ($A3),y
        clv
        lda     $DEA4,y
        .byte   $8B
        .byte   $22
        bit     $26
        plp
        bmi     LECFA
        .byte   $93
        .byte   $8C
        .byte   $FF
LED21:  cmp     #$0E
        bne     LED30
        lda     #$02
        ora     $9005
        sta     $9005
        jmp     LE6DC

LED30:  cmp     #$8E
        bne     LED3F
        lda     #$FD
        and     $9005
        sta     $9005
LED3C:  jmp     LE6DC

LED3F:  cmp     #$08
        bne     LED4D
        lda     #$80
        ora     $0291
        sta     $0291
        bmi     LED3C
LED4D:  cmp     #$09
        bne     LED3C
        lda     #$7F
        and     $0291
        sta     $0291
        bpl     LED3C
LED5B:  inx
        lda     $D9,x
        ora     #$80
        sta     $D9,x
        dex
        lda     $D5
        clc
        jmp     LE715

LED69:  .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $04
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $E2
LED79:  sta     $0183,x
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        sta     ($A0),y
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        inc     $8901
        .byte   $02
        .byte   $FF
        .byte   $FF
LED8D:  .byte   $FF
        .byte   $FF
        sbc     ($FD,x)
        txa
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        bcs     LED79
        .byte   $8B
        .byte   $F2
        .byte   $F4
        inc     $FF,x
        beq     LED8D
        .byte   $93
        sty     $90FF
        .byte   $1C
        .byte   $9C
        .byte   $1F
        .byte   $12
        .byte   $FF
        .byte   $FF
        .byte   $FF
        asl     $FF
        .byte   $12
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
LEDCD:  .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        ora     $9F
        asl     $929E,x
        .byte   $FF
        .byte   $FF
        .byte   $FF
LEDE3:  .byte   $FF
        .byte   $0C
        rol     $16
        rol     LC000
        brk
        brk
        brk
        brk
        brk
        brk
        brk
        brk
        brk
LEDF3:  .byte   $1B
        jmp     L414F

        .byte   $44
        ora     $5552
        .byte   $4E
LEDFC:  .byte   $0D
LEDFD:  brk
LEDFE:  asl     $2C,x
        .byte   $42
        cli
        ror     $9A84
        bcs     LEDCD
        .byte   $DC
        .byte   $F2
        php
        asl     $4A34,x
        rts

        ror     $8C,x
        ldx     #$B8
        .byte   $CE
        .byte   $E4
LEE14:  ora     #$40
        .byte   $2C
LEE17:  ora     #$20
        jsr     LF160
LEE1C:  pha
        bit     $94
        bpl     LEE2B
        sec
        ror     $A3
        jsr     LEE49
        lsr     $94
        lsr     $A3
LEE2B:  pla
        sta     $95
        jsr     LE4A0
        cmp     #$3F
        bne     LEE38
        jsr     LEF84
LEE38:  lda     $911F
        ora     #$80
        sta     $911F
LEE40:  jsr     LEF8D
        jsr     LE4A0
        jsr     LEF96
LEE49:  sei
        jsr     LE4A0
        jsr     LE4B2
        lsr     a
        bcs     LEEB4
        jsr     LEF84
        bit     $A3
        bpl     LEE66
LEE5A:  jsr     LE4B2
        lsr     a
        bcc     LEE5A
LEE60:  jsr     LE4B2
        lsr     a
        bcs     LEE60
LEE66:  jsr     LE4B2
        lsr     a
        bcc     LEE66
        jsr     LEF8D
        lda     #$08
        sta     $A5
LEE73:  lda     $911F
        cmp     $911F
        bne     LEE73
        lsr     a
        lsr     a
        bcc     LEEB7
        ror     $95
        bcs     LEE88
        jsr     LE4A9
        bne     LEE8B
LEE88:  jsr     LE4A0
LEE8B:  jsr     LEF84
        nop
        nop
        nop
        nop
        lda     $912C
        and     #$DF
        ora     #$02
        sta     $912C
        dec     $A5
        bne     LEE73
        lda     #$04
        sta     $9129
LEEA5:  lda     $912D
        and     #$20
        bne     LEEB7
        jsr     LE4B2
        lsr     a
        bcs     LEEA5
        cli
        rts

LEEB4:  lda     #$80
        .byte   $2C
LEEB7:  lda     #$03
LEEB9:  jsr     LFE6A
        cli
        clc
        bcc     LEF09
LEEC0:  sta     $95
        jsr     LEE40
LEEC5:  lda     $911F
        and     #$7F
        sta     $911F
        rts

LEECE:  sta     $95
        jsr     LEE40
LEED3:  sei
        jsr     LE4A9
        jsr     LEEC5
        jsr     LEF84
LEEDD:  jsr     LE4B2
        bcs     LEEDD
        cli
        rts

LEEE4:  bit     $94
        bmi     LEEED
        sec
        ror     $94
        bne     LEEF2
LEEED:  pha
        jsr     LEE49
        pla
LEEF2:  sta     $95
        clc
        rts

LEEF6:  jsr     LEF8D
        lda     $911F
        ora     #$80
        sta     $911F
        lda     #$5F
        .byte   $2C
LEF04:  lda     #$3F
        jsr     LEE1C
LEF09:  jsr     LEEC5
LEF0C:  txa
        ldx     #$0B
LEF0F:  dex
        bne     LEF0F
        tax
        jsr     LEF84
        jmp     LE4A0

LEF19:  sei
        lda     #$00
        sta     $A5
        jsr     LEF84
LEF21:  jsr     LE4B2
        bcc     LEF21
        jsr     LE4A0
LEF29:  lda     #$01
        sta     $9129
LEF2E:  lda     $912D
        and     #$20
        bne     LEF3C
        jsr     LE4B2
        bcs     LEF2E
        bcc     LEF54
LEF3C:  lda     $A5
        beq     LEF45
        lda     #$02
        jmp     LEEB9

LEF45:  jsr     LE4A9
        jsr     LEF0C
        lda     #$40
        jsr     LFE6A
        inc     $A5
        bne     LEF29
LEF54:  lda     #$08
        sta     $A5
LEF58:  lda     $911F
        cmp     $911F
        bne     LEF58
        lsr     a
        bcc     LEF58
        lsr     a
        ror     $A4
LEF66:  lda     $911F
        cmp     $911F
        bne     LEF66
        lsr     a
        bcs     LEF66
        dec     $A5
        bne     LEF58
        jsr     LE4A9
        lda     $90
        beq     LEF7F
        jsr     LEF0C
LEF7F:  lda     $A4
        cli
        clc
        rts

LEF84:  lda     $912C
        and     #$FD
        sta     $912C
        rts

LEF8D:  lda     $912C
        ora     #$02
        sta     $912C
        rts

LEF96:  lda     #$04
        sta     $9129
LEF9B:  lda     $912D
        and     #$20
        beq     LEF9B
        rts

LEFA3:  lda     $B4
        beq     LEFEE
        bmi     LEFE8
        lsr     $B6
        ldx     #$00
        bcc     LEFB0
        dex
LEFB0:  txa
        eor     $BD
        sta     $BD
        dec     $B4
        beq     LEFBF
LEFB9:  txa
        and     #$20
        sta     $B5
        rts

LEFBF:  lda     #$20
        bit     $0294
        beq     LEFDA
        bmi     LEFE4
        bvs     LEFDE
        lda     $BD
        bne     LEFCF
LEFCE:  dex
LEFCF:  dec     $B4
        lda     $0293
        bpl     LEFB9
        dec     $B4
        bne     LEFB9
LEFDA:  inc     $B4
        bne     LEFCE
LEFDE:  lda     $BD
        beq     LEFCF
        bne     LEFCE
LEFE4:  bvs     LEFCF
        bvc     LEFCE
LEFE8:  inc     $B4
        ldx     #$FF
        bne     LEFB9
LEFEE:  lda     $0294
        lsr     a
        bcc     LEFFB
        bit     $9120
        bpl     LF016
        bvc     LF019
LEFFB:  lda     #$00
        sta     $BD
        sta     $B5
        ldx     $0298
        stx     $B4
        ldy     $029D
        cpy     $029E
        beq     LF021
        lda     ($F9),y
        sta     $B6
        inc     $029D
        rts

LF016:  lda     #$40
        .byte   $2C
LF019:  lda     #$10
        ora     $0297
        sta     $0297
LF021:  lda     #$40
        sta     $911E
        rts

LF027:  ldx     #$09
        lda     #$20
        bit     $0293
        beq     LF031
        dex
LF031:  bvc     LF035
        dex
        dex
LF035:  rts

LF036:  ldx     $A9
        bne     LF068
        dec     $A8
        beq     LF06F
        bmi     LF04D
        lda     $A7
        eor     $AB
        sta     $AB
        lsr     $A7
        ror     $AA
LF04A:  rts

LF04B:  dec     $A8
LF04D:  lda     $A7
        beq     LF0B3
        lda     $0293
        asl     a
        lda     #$01
        adc     $A8
        bne     LF04A
LF05B:  lda     #$90
        sta     $911E
        sta     $A9
        lda     #$20
        sta     $911E
        rts

LF068:  lda     $A7
        bne     LF05B
        sta     $A9
        rts

LF06F:  ldy     $029B
        iny
        cpy     $029C
        beq     LF0A2
        sty     $029B
        dey
        lda     $AA
        ldx     $0298
LF081:  cpx     #$09
        beq     LF089
        lsr     a
        inx
        bne     LF081
LF089:  sta     ($F7),y
        lda     #$20
        bit     $0294
        beq     LF04B
        bmi     LF04A
        lda     $A7
        eor     $AB
        beq     LF09D
        bvs     LF04A
        .byte   $2C
LF09D:  bvc     LF04A
        lda     #$01
        .byte   $2C
LF0A2:  lda     #$04
        .byte   $2C
LF0A5:  lda     #$80
        .byte   $2C
LF0A8:  lda     #$02
        ora     $0297
        sta     $0297
        jmp     LF05B

LF0B3:  lda     $AA
        bne     LF0A8
        beq     LF0A5
LF0B9:  jmp     LF796

LF0BC:  sta     $9A
        lda     $0294
        lsr     a
        bcc     LF0EB
        lda     #$02
        bit     $9110
        bpl     LF0E8
        bne     LF0EB
LF0CD:  lda     $911E
        and     #$30
        bne     LF0CD
LF0D4:  bit     $9110
        bvs     LF0D4
        lda     $9110
        ora     #$02
        sta     $9110
LF0E1:  bit     $9110
        bvs     LF0EB
        bmi     LF0E1
LF0E8:  jsr     LF016
LF0EB:  clc
        rts

LF0ED:  ldy     $029E
        iny
        cpy     $029D
        beq     LF0ED
        sty     $029E
        dey
        sta     ($F9),y
        bit     $911E
        bvc     LF102
        rts

LF102:  lda     $0299
        sta     $9114
        lda     $029A
        sta     $9115
        lda     #$C0
        sta     $911E
        jmp     LEFEE

LF116:  sta     $99
        lda     $0294
        lsr     a
        bcc     LF146
        and     #$08
        beq     LF146
        lda     #$02
        bit     $9110
        bpl     LF0E8
        beq     LF144
LF12B:  bit     $911E
        bvs     LF12B
        lda     $9110
        and     #$FD
        sta     $9110
LF138:  lda     $9110
        and     #$04
        beq     LF138
LF13F:  lda     #$90
        sta     $911E
LF144:  clc
        rts

LF146:  lda     $911E
        and     #$30
        beq     LF13F
        clc
        rts

LF14F:  ldy     $029C
        cpy     $029B
        beq     LF15D
        lda     ($F7),y
        inc     $029C
        rts

LF15D:  lda     #$00
        rts

LF160:  pha
        lda     $911E
        beq     LF172
LF166:  .byte   $AD
LF167:  asl     $2991,x
        rts

        bne     LF166
        lda     #$10
        sta     $911E
LF172:  pla
        rts

LF174:  ora     $2F49
        .byte   $4F
        jsr     L5245
        .byte   $52
        .byte   $4F
        .byte   $52
        jsr     L0DA3
        .byte   $53
LF182:  eor     $41
        .byte   $52
        .byte   $43
        pha
        eor     #$4E
        .byte   $47
        ldy     #$46
        .byte   $4F
        .byte   $52
        ldy     #$0D
        bvc     LF1E4
        eor     $53
        .byte   $53
        jsr     L4C50
        eor     ($59,x)
        jsr     L4E4F
        jsr     L4154
        bvc     LF167
        bvc     LF1F6
        eor     $53
        .byte   $53
        jsr     L4552
        .byte   $43
        .byte   $4F
        .byte   $52
        .byte   $44
        jsr     L2026
        bvc     LF1FF
        eor     ($59,x)
        jsr     L4E4F
        jsr     L4154
        bvc     LF182
        ora     $4F4C
        eor     ($44,x)
        eor     #$4E
        .byte   $C7
        ora     $4153
        lsr     $49,x
        lsr     $A047
        ora     $4556
        .byte   $52
        eor     #$46
        eor     $4E49,y
        .byte   $C7
        ora     $4F46
        eor     $4E,x
        .byte   $44
        ldy     #$0D
        .byte   $4F
        .byte   $4B
        .byte   $8D
LF1E2:  bit     $9D
LF1E4:  bpl     LF1F3
LF1E6:  lda     LF174,y
        php
        and     #$7F
        jsr     LFFD2
        iny
        plp
        bpl     LF1E6
LF1F3:  clc
        rts

        .byte   $A5
LF1F6:  sta     $08D0,y
        lda     $C6
        beq     LF26A
        sei
        .byte   $4C
LF1FF:  .byte   $CF
        sbc     $C9
        .byte   $02
        bne     LF21D
LF205:  sty     $97
        jsr     LF14F
        ldy     $97
        clc
        rts

        lda     $99
        bne     LF21D
        lda     $D3
        sta     $CA
        lda     $D6
        sta     $C9
        jmp     LE64F

LF21D:  cmp     #$03
        bne     LF22A
        sta     $D0
        lda     $D5
        sta     $C8
        jmp     LE64F

LF22A:  bcs     LF264
        cmp     #$02
        beq     LF26F
        stx     $97
        jsr     LF250
        bcs     LF24D
        pha
        jsr     LF250
        bcs     LF24A
        bne     LF244
        lda     #$40
        jsr     LFE6A
LF244:  dec     $A6
        ldx     $97
        pla
        rts

LF24A:  tax
        pla
        txa
LF24D:  ldx     $97
        rts

LF250:  jsr     LF88A
        bne     LF260
        jsr     LF8C0
        bcs     LF26B
        lda     #$00
        sta     $A6
        beq     LF250
LF260:  lda     ($B2),y
        clc
        rts

LF264:  lda     $90
        beq     LF26C
        lda     #$0D
LF26A:  clc
LF26B:  rts

LF26C:  jmp     LEF19

LF26F:  jsr     LF205
        bcs     LF279
        cmp     #$00
        beq     LF26F
        clc
LF279:  rts

        pha
        lda     $9A
        cmp     #$03
        bne     LF285
        pla
        jmp     LE742

LF285:  bcc     LF28B
        pla
        jmp     LEEE4

LF28B:  cmp     #$02
        beq     LF2B9
        pla
LF290:  sta     $9E
        pha
        txa
        pha
        tya
        pha
        jsr     LF88A
        bne     LF2AA
        jsr     LF8E3
        bcs     LF2AF
        lda     #$02
        ldy     #$00
        sta     ($B2),y
        iny
        sty     $A6
LF2AA:  lda     $9E
        sta     ($B2),y
        clc
LF2AF:  pla
        tay
        pla
        tax
        pla
        bcc     LF2B8
        lda     #$00
LF2B8:  rts

LF2B9:  pla
        stx     $97
        sty     $9E
        jsr     LF0ED
        ldx     $97
        ldy     $9E
        clc
        rts

        jsr     LF3CF
        beq     LF2CF
        jmp     LF784

LF2CF:  jsr     LF3DF
        lda     $BA
        beq     LF2EC
        cmp     #$03
        beq     LF2EC
        bcs     LF2F0
        cmp     #$02
        bne     LF2E3
        jmp     LF116

LF2E3:  ldx     $B9
        cpx     #$60
        beq     LF2EC
        jmp     LF78D

LF2EC:  sta     $99
        clc
        rts

LF2F0:  tax
        jsr     LEE14
        lda     $B9
        bpl     LF2FE
        jsr     LEED3
        jmp     LF301

LF2FE:  jsr     LEECE
LF301:  txa
        bit     $90
        bpl     LF2EC
        jmp     LF78A

        jsr     LF3CF
        beq     LF311
        jmp     LF784

LF311:  jsr     LF3DF
        lda     $BA
        bne     LF31B
LF318:  jmp     LF790

LF31B:  cmp     #$03
        beq     LF32E
        bcs     LF332
        cmp     #$02
        bne     LF328
        jmp     LF0BC

LF328:  ldx     $B9
        cpx     #$60
        beq     LF318
LF32E:  sta     $9A
        clc
        rts

LF332:  tax
        jsr     LEE17
        lda     $B9
        bpl     LF33F
        jsr     LEEC5
        bne     LF342
LF33F:  jsr     LEEC0
LF342:  txa
        bit     $90
        bpl     LF32E
        jmp     LF78A

        jsr     LF3D4
        beq     LF351
        clc
        rts

LF351:  jsr     LF3DF
        txa
        pha
        lda     $BA
        beq     LF3B1
        cmp     #$03
        beq     LF3B1
        bcs     LF3AE
        cmp     #$02
        bne     LF38D
        pla
        jsr     LF3B2
        lda     #$7D
        sta     $911E
        lda     #$06
        sta     $9110
        lda     #$EE
        sta     $911C
        jsr     LFE75
        lda     $F8
        beq     LF37F
        iny
LF37F:  lda     $FA
        beq     LF384
        iny
LF384:  lda     #$00
        sta     $F8
        sta     $FA
        jmp     LF53C

LF38D:  lda     $B9
        and     #$0F
        beq     LF3B1
        jsr     LF84D
        lda     #$00
        jsr     LF290
        jmp     LE4CF

LF39E:  bcs     LF3CE
        lda     $B9
        cmp     #$62
        bne     LF3B1
        lda     #$05
        jsr     LF7E7
        jmp     LF3B1

LF3AE:  jsr     LF6DA
LF3B1:  pla
LF3B2:  tax
        dec     $98
        cpx     $98
        beq     LF3CD
        ldy     $98
        lda     $0259,y
        sta     $0259,x
        lda     $0263,y
        sta     $0263,x
        lda     $026D,y
        sta     $026D,x
LF3CD:  clc
LF3CE:  rts

LF3CF:  lda     #$00
        sta     $90
        txa
LF3D4:  ldx     $98
LF3D6:  dex
        bmi     LF3EE
        cmp     $0259,x
        bne     LF3D6
        rts

LF3DF:  lda     $0259,x
        sta     $B8
        lda     $0263,x
        sta     $BA
        lda     $026D,x
        sta     $B9
LF3EE:  rts

        lda     #$00
        sta     $98
        ldx     #$03
        cpx     $9A
        bcs     LF3FC
        jsr     LEF04
LF3FC:  cpx     $99
        bcs     LF403
        jsr     LEEF6
LF403:  stx     $9A
        lda     #$00
        sta     $99
        rts

        ldx     $B8
        bne     LF411
        jmp     LF78D

LF411:  jsr     LF3CF
        bne     LF419
        jmp     LF781

LF419:  ldx     $98
        cpx     #$0A
        bcc     LF422
        jmp     LF77E

LF422:  inc     $98
        lda     $B8
        sta     $0259,x
        lda     $B9
        ora     #$60
        sta     $B9
        sta     $026D,x
        lda     $BA
        sta     $0263,x
        beq     LF493
        cmp     #$03
        beq     LF493
        bcc     LF444
        jsr     LF495
        bcc     LF493
LF444:  cmp     #$02
        bne     LF44B
        jmp     LF4C7

LF44B:  jsr     LF84D
        bcs     LF453
        jmp     LF796

LF453:  lda     $B9
        and     #$0F
        bne     LF478
        jsr     LF894
        bcs     LF494
        jsr     LF647
        lda     $B7
        beq     LF46F
        jsr     LF867
        bcc     LF482
        beq     LF494
LF46C:  jmp     LF787

LF46F:  jsr     LF7AF
        beq     LF494
        bcc     LF482
        bcs     LF46C
LF478:  jsr     LF8B7
        bcs     LF494
        lda     #$04
        jsr     LF7E7
LF482:  lda     #$BF
        ldy     $B9
        cpy     #$60
        beq     LF491
        ldy     #$00
        lda     #$02
        sta     ($B2),y
        tya
LF491:  sta     $A6
LF493:  clc
LF494:  rts

LF495:  lda     $B9
        bmi     LF4C5
        ldy     $B7
        beq     LF4C5
        lda     $BA
        jsr     LEE17
        lda     $B9
        ora     #$F0
        jsr     LEEC0
        lda     $90
        bpl     LF4B2
        pla
        pla
        jmp     LF78A

LF4B2:  lda     $B7
        beq     LF4C2
        ldy     #$00
LF4B8:  lda     ($BB),y
        jsr     LEEE4
        iny
        cpy     $B7
        bne     LF4B8
LF4C2:  jsr     LEF04
LF4C5:  clc
        rts

LF4C7:  lda     #$06
        sta     $9112
        sta     $9110
        lda     #$EE
        sta     $911C
        ldy     #$00
        sty     $0297
LF4D9:  cpy     $B7
        beq     LF4E7
        lda     ($BB),y
        sta     $0293,y
        iny
        cpy     #$04
        bne     LF4D9
LF4E7:  jsr     LF027
        stx     $0298
        lda     $0293
        and     #$0F
        bne     LF4F4
LF4F4:  asl     a
        tax
        lda     LFF5A,x
        asl     a
        tay
        lda     LFF5B,x
        rol     a
        pha
        tya
        adc     #$C8
        sta     $0299
        pla
        adc     #$00
        sta     $029A
        lda     $0294
        lsr     a
        bcc     LF51B
        lda     $9120
        asl     a
        bcs     LF51B
        jmp     LF016

LF51B:  lda     $029B
        sta     $029C
        lda     $029E
        sta     $029D
        jsr     LFE75
        lda     $F8
        bne     LF533
        dey
        sty     $F8
        stx     $F7
LF533:  lda     $FA
        bne     LF53C
        dey
        sty     $FA
        stx     $F9
LF53C:  sec
        lda     #$F0
        jmp     LFE7B

LF542:  stx     $C3
        sty     $C4
        jmp     (L0330)

LF549:  sta     $93
        lda     #$00
        sta     $90
        lda     $BA
        bne     LF556
LF553:  jmp     LF796

LF556:  cmp     #$03
        beq     LF553
        bcc     LF5CA
        ldy     $B7
        bne     LF563
        jmp     LF793

LF563:  jsr     LE4BC
        lda     #$60
        sta     $B9
        jsr     LF495
        lda     $BA
        jsr     LEE14
        lda     $B9
        jsr     LEECE
        jsr     LEF19
        sta     $AE
        lda     $90
        lsr     a
        lsr     a
        bcs     LF5C7
        jsr     LEF19
        sta     $AF
        jsr     LE4C1
LF58A:  lda     #$FD
        and     $90
        sta     $90
        jsr     LFFE1
        bne     LF598
        jmp     LF6CB

LF598:  jsr     LEF19
        tax
        lda     $90
        lsr     a
        lsr     a
        bcs     LF58A
        txa
        ldy     $93
        beq     LF5B3
        ldy     #$00
        cmp     ($AE),y
        beq     LF5B5
        lda     #$10
        jsr     LFE6A
        .byte   $2C
LF5B3:  sta     ($AE),y
LF5B5:  inc     $AE
        bne     LF5BB
        inc     $AF
LF5BB:  bit     $90
        bvc     LF58A
        jsr     LEEF6
        jsr     LF6DA
        bcc     LF641
LF5C7:  jmp     LF787

LF5CA:  cmp     #$02
        bne     LF5D1
        jmp     LF0B9

LF5D1:  jsr     LF84D
        bcs     LF5D9
        jmp     LF796

LF5D9:  jsr     LF894
        bcs     LF646
        jsr     LF647
LF5E1:  lda     $B7
        beq     LF5EE
        jsr     LF867
        bcc     LF5F5
        beq     LF646
        bcs     LF5C7
LF5EE:  jsr     LF7AF
        beq     LF646
        bcs     LF5C7
LF5F5:  lda     $90
        and     #$10
        sec
        bne     LF646
        cpx     #$01
        beq     LF611
        cpx     #$03
        bne     LF5E1
LF604:  ldy     #$01
        lda     ($B2),y
        sta     $C3
        iny
        lda     ($B2),y
        sta     $C4
        bcs     LF615
LF611:  lda     $B9
        bne     LF604
LF615:  ldy     #$03
        lda     ($B2),y
        ldy     #$01
        sbc     ($B2),y
        tax
        ldy     #$04
        lda     ($B2),y
        ldy     #$02
        sbc     ($B2),y
        tay
        clc
        txa
        adc     $C3
        sta     $AE
        tya
        adc     $C4
        sta     $AF
        lda     $C3
        sta     $C1
        lda     $C4
        sta     $C2
        jsr     LF66A
        jsr     LF8C9
        .byte   $24
LF641:  clc
        ldx     $AE
        ldy     $AF
LF646:  rts

LF647:  lda     $9D
        bpl     LF669
        ldy     #$0C
        jsr     LF1E6
        lda     $B7
        beq     LF669
        ldy     #$17
        jsr     LF1E6
LF659:  ldy     $B7
        beq     LF669
        ldy     #$00
LF65F:  lda     ($BB),y
        jsr     LFFD2
        iny
        cpy     $B7
        bne     LF65F
LF669:  rts

LF66A:  ldy     #$49
        lda     $93
        beq     LF672
        ldy     #$59
LF672:  jmp     LF1E2

LF675:  stx     $AE
        sty     $AF
        tax
        lda     $00,x
        sta     $C1
        lda     $01,x
        sta     $C2
        jmp     (L0332)

        lda     $BA
        bne     LF68C
LF689:  jmp     LF796

LF68C:  cmp     #$03
        beq     LF689
        bcc     LF6F1
        lda     #$61
        sta     $B9
        ldy     $B7
        bne     LF69D
        jmp     LF793

LF69D:  jsr     LF495
        jsr     LF728
        lda     $BA
        jsr     LEE17
        lda     $B9
        jsr     LEEC0
        ldy     #$00
        jsr     LFBD2
        lda     $AC
        jsr     LEEE4
        lda     $AD
        jsr     LEEE4
LF6BC:  jsr     LFD11
        bcs     LF6D7
        lda     ($AC),y
        jsr     LEEE4
        jsr     LFFE1
        bne     LF6D2
LF6CB:  jsr     LF6DA
        lda     #$00
        sec
        rts

LF6D2:  jsr     LFD1B
        bne     LF6BC
LF6D7:  jsr     LEF04
LF6DA:  bit     $B9
        bmi     LF6EF
        lda     $BA
        jsr     LEE17
        lda     $B9
        and     #$EF
        ora     #$E0
        jsr     LEEC0
        jsr     LEF04
LF6EF:  clc
        rts

LF6F1:  cmp     #$02
        bne     LF6F8
        jmp     LF0B9

LF6F8:  jsr     LF84D
        bcc     LF689
        jsr     LF8B7
        bcs     LF727
        jsr     LF728
        ldx     #$03
        lda     $B9
        and     #$01
        bne     LF70F
        ldx     #$01
LF70F:  txa
        jsr     LF7E7
        bcs     LF727
        jsr     LF8E6
        bcs     LF727
        lda     $B9
        and     #$02
        beq     LF726
        lda     #$05
        jsr     LF7E7
        .byte   $24
LF726:  clc
LF727:  rts

LF728:  lda     $9D
        bpl     LF727
        ldy     #$51
        jsr     LF1E6
        jmp     LF659

LF734:  ldx     #$00
        inc     $A2
        bne     LF740
        inc     $A1
        bne     LF740
        inc     $A0
LF740:  sec
        lda     $A2
        sbc     #$01
        lda     $A1
        sbc     #$1A
        lda     $A0
        sbc     #$4F
        bcc     LF755
        stx     $A0
        stx     $A1
        stx     $A2
LF755:  lda     $912F
        cmp     $912F
        bne     LF755
        sta     $91
        rts

LF760:  sei
        lda     $A2
        ldx     $A1
        ldy     $A0
LF767:  sei
        sta     $A2
        stx     $A1
        sty     $A0
        cli
        rts

        lda     $91
        cmp     #$FE
        bne     LF77D
        php
        jsr     LFFCC
        sta     $C6
        plp
LF77D:  rts

LF77E:  lda     #$01
        .byte   $2C
LF781:  lda     #$02
        .byte   $2C
LF784:  lda     #$03
        .byte   $2C
LF787:  lda     #$04
        .byte   $2C
LF78A:  lda     #$05
        .byte   $2C
LF78D:  lda     #$06
        .byte   $2C
LF790:  lda     #$07
        .byte   $2C
LF793:  lda     #$08
        .byte   $2C
LF796:  lda     #$09
        pha
        jsr     LFFCC
        ldy     #$00
        bit     $9D
        bvc     LF7AC
        jsr     LF1E6
        pla
        pha
        ora     #$30
        jsr     LFFD2
LF7AC:  pla
        sec
        rts

LF7AF:  lda     $93
        pha
        jsr     LF8C0
        pla
        sta     $93
        bcs     LF7E6
        ldy     #$00
        lda     ($B2),y
        cmp     #$05
        beq     LF7E6
        cmp     #$01
        beq     LF7CE
        cmp     #$03
        beq     LF7CE
        cmp     #$04
        bne     LF7AF
LF7CE:  tax
        bit     $9D
        bpl     LF7E4
        ldy     #$63
        jsr     LF1E6
        ldy     #$05
LF7DA:  lda     ($B2),y
        jsr     LFFD2
        iny
        cpy     #$15
        bne     LF7DA
LF7E4:  clc
        dey
LF7E6:  rts

LF7E7:  sta     $9E
        jsr     LF84D
        bcc     LF84C
        lda     $C2
        pha
        lda     $C1
        pha
        lda     $AF
        pha
        lda     $AE
        pha
        ldy     #$BF
        lda     #$20
LF7FE:  sta     ($B2),y
        dey
        bne     LF7FE
        lda     $9E
        sta     ($B2),y
        iny
        lda     $C1
        sta     ($B2),y
        iny
        lda     $C2
        sta     ($B2),y
        iny
        lda     $AE
        sta     ($B2),y
        iny
        lda     $AF
        sta     ($B2),y
        iny
        sty     $9F
        ldy     #$00
        sty     $9E
LF822:  ldy     $9E
        cpy     $B7
        beq     LF834
        lda     ($BB),y
        ldy     $9F
        sta     ($B2),y
        inc     $9E
        inc     $9F
        bne     LF822
LF834:  jsr     LF854
        lda     #$69
        sta     $AB
        jsr     LF8EA
        tay
        pla
        sta     $AE
        pla
        sta     $AF
        pla
        sta     $C1
        pla
        sta     $C2
        tya
LF84C:  rts

LF84D:  ldx     $B2
        ldy     $B3
        cpy     #$02
        rts

LF854:  jsr     LF84D
        txa
        sta     $C1
        clc
        adc     #$C0
        sta     $AE
        tya
        sta     $C2
        adc     #$00
        sta     $AF
        rts

LF867:  jsr     LF7AF
        bcs     LF889
        ldy     #$05
        sty     $9F
        ldy     #$00
        sty     $9E
LF874:  cpy     $B7
        beq     LF888
        lda     ($BB),y
        ldy     $9F
        cmp     ($B2),y
        bne     LF867
        inc     $9E
        inc     $9F
        ldy     $9E
        bne     LF874
LF888:  clc
LF889:  rts

LF88A:  jsr     LF84D
        inc     $A6
        ldy     $A6
        cpy     #$C0
        rts

LF894:  jsr     LF8AB
        beq     LF8B5
        ldy     #$1B
LF89B:  jsr     LF1E6
LF89E:  jsr     LF94B
        jsr     LF8AB
        bne     LF89E
        ldy     #$6A
        jmp     LF1E6

LF8AB:  lda     #$40
        bit     $911F
        bne     LF8B5
        bit     $911F
LF8B5:  clc
        rts

LF8B7:  jsr     LF8AB
        beq     LF8B5
        ldy     #$2E
        bne     LF89B
LF8C0:  lda     #$00
        sta     $90
        sta     $93
        jsr     LF854
LF8C9:  jsr     LF894
        bcs     LF8ED
        sei
        lda     #$00
        sta     $AA
        sta     $B4
        sta     $B0
        sta     $9E
        sta     $9F
        sta     $9C
        lda     #$82
        ldx     #$0E
        bne     LF8F4
LF8E3:  jsr     LF854
LF8E6:  lda     #$14
        sta     $AB
LF8EA:  jsr     LF8B7
LF8ED:  bcs     LF957
        sei
        lda     #$A0
        ldx     #$08
LF8F4:  ldy     #$7F
        sty     $912E
        sta     $912E
        jsr     LF160
        lda     L0314
        sta     $029F
        lda     $0315
        sta     $02A0
        jsr     LFCFB
        lda     #$02
        sta     $BE
        jsr     LFBDB
        lda     $911C
        and     #$FD
        ora     #$0C
        sta     $911C
        sta     $C0
        ldx     #$FF
LF923:  ldy     #$FF
LF925:  dey
        bne     LF925
        dex
        bne     LF923
        sta     $9129
        cli
LF92F:  lda     $02A0
        cmp     $0315
        clc
        beq     LF957
        jsr     LF94B
        lda     $912D
        and     #$40
        beq     LF92F
        lda     $9114
        jsr     LF734
        jmp     LF92F

LF94B:  jsr     LFFE1
        clc
        bne     LF95C
        jsr     LFCCF
        sec
        pla
        pla
LF957:  lda     #$00
        sta     $02A0
LF95C:  rts

LF95D:  stx     $B1
        lda     $B0
        asl     a
        asl     a
        clc
        adc     $B0
        clc
        adc     $B1
        sta     $B1
        lda     #$00
        bit     $B0
        bmi     LF972
        rol     a
LF972:  asl     $B1
        rol     a
        asl     $B1
        rol     a
        tax
LF979:  lda     $9128
        cmp     #$15
        bcc     LF979
        adc     $B1
        sta     $9124
        txa
        adc     $9129
        sta     $9125
        cli
        rts

LF98E:  ldx     $9129
        ldy     #$FF
        tya
        sbc     $9128
        cpx     $9129
        bne     LF98E
        stx     $B1
        tax
        sty     $9128
        sty     $9129
        tya
        sbc     $B1
        stx     $B1
        lsr     a
        ror     $B1
        lsr     a
        ror     $B1
        lda     $B0
        clc
        adc     #$3C
        bit     $9121
        cmp     $B1
        bcs     LFA06
        ldx     $9C
        beq     LF9C3
        jmp     LFAAD

LF9C3:  ldx     $A3
        bmi     LF9E2
        ldx     #$00
        adc     #$30
        adc     $B0
        cmp     $B1
        bcs     LF9ED
        inx
        adc     #$26
        adc     $B0
        cmp     $B1
        bcs     LF9F1
        adc     #$2C
        adc     $B0
        cmp     $B1
        bcc     LF9E5
LF9E2:  jmp     LFA60

LF9E5:  lda     $B4
        beq     LFA06
        sta     $A8
        bne     LFA06
LF9ED:  inc     $A9
        bcs     LF9F3
LF9F1:  dec     $A9
LF9F3:  sec
        sbc     #$13
        sbc     $B1
        adc     $92
        sta     $92
        lda     $A4
        eor     #$01
        sta     $A4
        beq     LFA25
        stx     $D7
LFA06:  lda     $B4
        beq     LFA22
        bit     $912D
        bvc     LFA22
        lda     #$00
        sta     $A4
        lda     $A3
        bpl     LFA47
        bmi     LF9E2
LFA19:  ldx     #$A6
        jsr     LF95D
        lda     $9B
        bne     LF9E5
LFA22:  jmp     LFF56

LFA25:  lda     $92
        beq     LFA30
        bmi     LFA2E
        dec     $B0
        .byte   $2C
LFA2E:  inc     $B0
LFA30:  lda     #$00
        sta     $92
        cpx     $D7
        bne     LFA47
        txa
        bne     LF9E5
        lda     $A9
        bmi     LFA06
        cmp     #$10
        bcc     LFA06
        sta     $96
        bcs     LFA06
LFA47:  txa
        eor     $9B
        sta     $9B
        lda     $B4
        beq     LFA22
        dec     $A3
        bmi     LFA19
        lsr     $D7
        ror     $BF
        ldx     #$DA
        jsr     LF95D
        jmp     LFF56

LFA60:  lda     $96
        beq     LFA68
        lda     $B4
        beq     LFA6C
LFA68:  lda     $A3
        bpl     LF9F1
LFA6C:  lsr     $B1
        lda     #$93
        sec
        sbc     $B1
        adc     $B0
        asl     a
        tax
        jsr     LF95D
        inc     $9C
        lda     $B4
        bne     LFA91
        lda     $96
        beq     LFAAA
        sta     $A8
        lda     #$00
        sta     $96
        lda     #$C0
        sta     $912E
        sta     $B4
LFA91:  lda     $96
        sta     $B5
        beq     LFAA0
        lda     #$00
        sta     $B4
        lda     #$40
        sta     $912E
LFAA0:  lda     $BF
        sta     $BD
        lda     $A8
        ora     $A9
        sta     $B6
LFAAA:  jmp     LFF56

LFAAD:  jsr     LFBDB
        sta     $9C
        ldx     #$DA
        jsr     LF95D
        lda     $BE
        beq     LFABD
        sta     $A7
LFABD:  lda     #$0F
        bit     $AA
        bpl     LFADA
        lda     $B5
        bne     LFAD3
        ldx     $BE
        dex
        bne     LFAD7
        lda     #$08
        jsr     LFE6A
        bne     LFAD7
LFAD3:  lda     #$00
        sta     $AA
LFAD7:  jmp     LFF56

LFADA:  bvs     LFB0D
        bne     LFAF6
        lda     $B5
        bne     LFAD7
        lda     $B6
        bne     LFAD7
        lda     $A7
        lsr     a
        lda     $BD
        bmi     LFAF0
        bcc     LFB07
        clc
LFAF0:  bcs     LFB07
        and     #$0F
        sta     $AA
LFAF6:  dec     $AA
        bne     LFAD7
        lda     #$40
        sta     $AA
        jsr     LFBD2
        lda     #$00
        sta     $AB
        beq     LFAD7
LFB07:  lda     #$80
        sta     $AA
        bne     LFAD7
LFB0D:  lda     $B5
        beq     LFB1B
        lda     #$04
        jsr     LFE6A
        lda     #$00
        jmp     LFB97

LFB1B:  jsr     LFD11
        bcc     LFB23
        jmp     LFB95

LFB23:  ldx     $A7
        dex
        beq     LFB55
        lda     $93
        beq     LFB38
        ldy     #$00
        lda     $BD
        cmp     ($AC),y
        beq     LFB38
        lda     #$01
        sta     $B6
LFB38:  lda     $B6
        beq     LFB87
        ldx     #$3D
        cpx     $9E
        bcc     LFB80
        ldx     $9E
        lda     $AD
        sta     $0101,x
        lda     $AC
        sta     $0100,x
        inx
        inx
        stx     $9E
        jmp     LFB87

LFB55:  ldx     $9F
        cpx     $9E
        beq     LFB90
        lda     $AC
        cmp     $0100,x
        bne     LFB90
        lda     $AD
        cmp     $0101,x
        bne     LFB90
        inc     $9F
        inc     $9F
        lda     $93
        beq     LFB7C
        lda     $BD
        ldy     #$00
        cmp     ($AC),y
        beq     LFB90
        iny
        sty     $B6
LFB7C:  lda     $B6
        beq     LFB87
LFB80:  lda     #$10
        jsr     LFE6A
        bne     LFB90
LFB87:  lda     $93
        bne     LFB90
        tay
        lda     $BD
        sta     ($AC),y
LFB90:  jsr     LFD1B
        bne     LFBCF
LFB95:  lda     #$80
LFB97:  sta     $AA
        ldx     $BE
        dex
        bmi     LFBA0
        stx     $BE
LFBA0:  dec     $A7
        beq     LFBAC
        lda     $9E
        bne     LFBCF
        sta     $BE
        beq     LFBCF
LFBAC:  jsr     LFCCF
        jsr     LFBD2
        ldy     #$00
        sty     $AB
LFBB6:  lda     ($AC),y
        eor     $AB
        sta     $AB
        jsr     LFD1B
        jsr     LFD11
        bcc     LFBB6
        lda     $AB
        eor     $BD
        beq     LFBCF
        lda     #$20
        jsr     LFE6A
LFBCF:  jmp     LFF56

LFBD2:  lda     $C2
        sta     $AD
        lda     $C1
        sta     $AC
        rts

LFBDB:  lda     #$08
        sta     $A3
        lda     #$00
        sta     $A4
        sta     $A8
        sta     $9B
        sta     $A9
        rts

LFBEA:  lda     $BD
        lsr     a
        lda     #$60
        bcc     LFBF3
LFBF1:  lda     #$B0
LFBF3:  ldx     #$00
LFBF5:  sta     $9128
        stx     $9129
        lda     $9120
        eor     #$08
        sta     $9120
        and     #$08
        rts

LFC06:  sec
        ror     $AD
        bmi     LFC47
        lda     $A8
        bne     LFC21
        lda     #$10
        ldx     #$01
        jsr     LFBF5
        bne     LFC47
        inc     $A8
        lda     $AD
        bpl     LFC47
        jmp     LFC95

LFC21:  lda     $A9
        bne     LFC2E
        jsr     LFBF1
        bne     LFC47
        inc     $A9
        bne     LFC47
LFC2E:  jsr     LFBEA
        bne     LFC47
        lda     $A4
        eor     #$01
        sta     $A4
        beq     LFC4A
        lda     $BD
        eor     #$01
        sta     $BD
        and     #$01
        eor     $9B
        sta     $9B
LFC47:  jmp     LFF56

LFC4A:  lsr     $BD
        dec     $A3
        lda     $A3
        beq     LFC8C
        bpl     LFC47
LFC54:  jsr     LFBDB
        cli
        lda     $A5
        beq     LFC6E
        ldx     #$00
        stx     $D7
        dec     $A5
        ldx     $BE
        cpx     #$02
        bne     LFC6A
        ora     #$80
LFC6A:  sta     $BD
        bne     LFC47
LFC6E:  jsr     LFD11
        bcc     LFC7D
        bne     LFC06
        inc     $AD
        lda     $D7
        sta     $BD
        bcs     LFC47
LFC7D:  ldy     #$00
        lda     ($AC),y
        sta     $BD
        eor     $D7
        sta     $D7
        jsr     LFD1B
        bne     LFC47
LFC8C:  lda     $9B
        eor     #$01
        sta     $BD
LFC92:  jmp     LFF56

LFC95:  dec     $BE
        bne     LFC9C
        jsr     LFD08
LFC9C:  lda     #$50
        sta     $A7
        ldx     #$08
        sei
        jsr     LFCFB
        bne     LFC92
        lda     #$78
        jsr     LFBF3
        bne     LFC92
        dec     $A7
        bne     LFC92
        jsr     LFBDB
        dec     $AB
        bpl     LFC92
        ldx     #$0A
        jsr     LFCFB
        cli
        inc     $AB
        lda     $BE
        beq     LFCF6
        jsr     LFBD2
        ldx     #$09
        stx     $A5
        bne     LFC54
LFCCF:  php
        sei
        jsr     LFD08
        lda     #$7F
        sta     $912E
        lda     #$F7
        sta     $9120
        lda     #$40
        sta     $912B
        jsr     LFE39
        lda     $02A0
        beq     LFCF4
        sta     $0315
        lda     $029F
        sta     L0314
LFCF4:  plp
        rts

LFCF6:  jsr     LFCCF
        beq     LFC92
LFCFB:  lda     LFDE9,x
        sta     L0314
        lda     LFDEA,x
        sta     $0315
        rts

LFD08:  lda     $911C
        ora     #$0E
        sta     $911C
        rts

LFD11:  sec
        lda     $AC
        sbc     $AE
        lda     $AD
        sbc     $AF
        rts

LFD1B:  inc     $AC
        bne     LFD21
        inc     $AD
LFD21:  rts

        ldx     #$FF
        sei
        txs
        cld
        jsr     LFD3F
        bne     LFD2F
        jmp     (LA000)

LFD2F:  jsr     LFD8D
        jsr     LFD52
        jsr     LFDF9
        jsr     LE518
        cli
        jmp     (LC000)

LFD3F:  ldx     #$05
LFD41:  lda     LFD4C,x
        cmp     $A003,x
        bne     LFD4C
        dex
        bne     LFD41
LFD4C:  rts

        eor     ($30,x)
        .byte   $C3
        .byte   $C2
        .byte   $CD
LFD52:  ldx     #$6D
        ldy     #$FD
        clc
LFD57:  stx     $C3
        sty     $C4
        ldy     #$1F
LFD5D:  lda     L0314,y
        bcs     LFD64
        lda     ($C3),y
LFD64:  sta     ($C3),y
        sta     L0314,y
        dey
        bpl     LFD5D
        rts

        .byte   $BF
        nop
        .byte   $D2
        inc     LFEAD,x
        asl     a
        .byte   $F4
        lsr     a
        .byte   $F3
        .byte   $C7
        .byte   $F2
        .byte   $09
LFD7A:  .byte   $F3
        .byte   $F3
        .byte   $F3
        asl     $7AF2
        .byte   $F2
        bvs     LFD7A
        sbc     $F1,x
        .byte   $EF
        .byte   $F3
        .byte   $D2
        inc     LF549,x
        sta     $F6
LFD8D:  lda     #$00
        tax
LFD90:  sta     $00,x
        sta     $0200,x
        sta     $0300,x
        inx
        bne     LFD90
        ldx     #$3C
        ldy     #$03
        stx     $B2
        sty     $B3
        sta     $C1
        sta     $97
        sta     $0281
        tay
        lda     #$04
        sta     $C2
LFDAF:  inc     $C1
        bne     LFDB5
        inc     $C2
LFDB5:  jsr     LFE91
        lda     $97
        beq     LFDDE
        bcs     LFDAF
        ldy     $C2
        ldx     $C1
        cpy     #$20
        bcc     LFDEB
        cpy     #$21
        bcs     LFDD2
        ldy     #$1E
        sty     $0288
LFDCF:  jmp     LFE7B

LFDD2:  lda     #$12
        sta     $0282
        lda     #$10
        sta     $0288
        bne     LFDCF
LFDDE:  bcc     LFDAF
        lda     $C2
        sta     $0282
        sta     $97
        cmp     #$11
LFDE9:  .byte   $90
LFDEA:  .byte   $C4
LFDEB:  jsr     LE5C3
        jmp     LFDEB

        tay
        .byte   $FC
        .byte   $0B
        .byte   $FC
        .byte   $BF
        nop
        .byte   $8E
        .byte   $F9
LFDF9:  lda     #$7F
        sta     $911E
        sta     $912E
        lda     #$40
        sta     $912B
        lda     #$40
        sta     $911B
        lda     #$FE
        sta     $911C
        lda     #$DE
        sta     $912C
        ldx     #$00
        stx     $9112
        ldx     #$FF
        stx     $9122
        ldx     #$00
        stx     $9123
        ldx     #$80
        stx     $9113
        ldx     #$00
        stx     $911F
        jsr     LEF84
        lda     #$82
        sta     $911E
        jsr     LEF8D
LFE39:  lda     #$C0
        sta     $912E
        lda     #$26
        sta     $9124
        lda     #$48
        sta     $9125
        rts

LFE49:  sta     $B7
        stx     $BB
        sty     $BC
        rts

LFE50:  sta     $B8
        stx     $BA
        sty     $B9
        rts

LFE57:  lda     $BA
        cmp     #$02
        bne     LFE68
        lda     $0297
        lda     #$00
        sta     $0297
        rts

LFE66:  sta     $9D
LFE68:  lda     $90
LFE6A:  ora     $90
        sta     $90
        rts

LFE6F:  sta     $0285
        rts

LFE73:  bcc     LFE7B
LFE75:  ldx     $0283
        ldy     $0284
LFE7B:  stx     $0283
        sty     $0284
        rts

LFE82:  bcc     LFE8A
        ldx     $0281
        ldy     $0282
LFE8A:  stx     $0281
        sty     $0282
        rts

LFE91:  lda     ($C1),y
        tax
        lda     #$55
        sta     ($C1),y
        cmp     ($C1),y
        bne     LFEA4
        ror     a
        sta     ($C1),y
        cmp     ($C1),y
        bne     LFEA4
        .byte   $A9
LFEA4:  clc
        txa
        sta     ($C1),y
        rts

        sei
        jmp     (L0318)

LFEAD:  pha
        txa
        pha
        tya
        pha
        lda     $911D
        bpl     LFEFF
        and     $911E
        tax
        and     #$02
        beq     LFEDE
        jsr     LFD3F
        bne     LFEC7
        jmp     (LA002)

LFEC7:  bit     $9111
        jsr     LF734
        jsr     LFFE1
        bne     LFEFF
        jsr     LFD52
        jsr     LFDF9
        jsr     LE518
        jmp     (LC002)

LFEDE:  lda     $911E
        ora     #$80
        pha
        lda     #$7F
        sta     $911E
        txa
        and     #$40
        beq     LFF02
        lda     #$CE
        ora     $B5
        sta     $911C
        lda     $9114
        pla
        sta     $911E
        jsr     LEFA3
LFEFF:  jmp     LFF56

LFF02:  txa
        and     #$20
        beq     LFF2C
        lda     $9110
        and     #$01
        sta     $A7
        lda     $9118
        sbc     #$16
        adc     $0299
        sta     $9118
        lda     $9119
        adc     $029A
        sta     $9119
        pla
        sta     $911E
        jsr     LF036
        jmp     LFF56

LFF2C:  txa
        and     #$10
        beq     LFF56
        lda     $0293
        and     #$0F
        bne     LFF38
LFF38:  asl     a
        tax
        lda     LFF5A,x
        sta     $9118
        lda     LFF5B,x
        sta     $9119
        lda     $9110
        pla
        ora     #$20
        and     #$EF
        sta     $911E
        ldx     $0298
        stx     $A8
LFF56:  pla
        tay
        pla
        tax
LFF5A:  pla
LFF5B:  rti

        inc     $2A
        sei
        .byte   $1C
        eor     #$13
        lda     ($0F),y
        asl     a
        asl     $06D3
        sec
        .byte   $03
        ror     a
        ora     ($D0,x)
        brk
        .byte   $83
        brk
        rol     $00,x
LFF72:  pha
        txa
        pha
        tya
        pha
        tsx
        lda     $0104,x
        and     #$10
        beq     LFF82
        jmp     (L0316)

LFF82:  jmp     (L0314)

        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        jmp     LFD52

        jmp     LFD57

        jmp     LFE66

        jmp     LEEC0

        jmp     LEECE

LFF99:  jmp     LFE73

LFF9C:  jmp     LFE82

        jmp     LEB1E

        jmp     LFE6F

        jmp     LEF19

        jmp     LEEE4

        jmp     LEEF6

        jmp     LEF04

        jmp     LEE17

        jmp     LEE14

LFFB7:  jmp     LFE57

LFFBA:  jmp     LFE50

LFFBD:  jmp     LFE49

LFFC0:  jmp     (L031A)

LFFC3:  jmp     (L031C)

LFFC6:  jmp     (L031E)

LFFC9:  jmp     (L0320)

LFFCC:  jmp     (L0322)

LFFCF:  jmp     (L0324)

LFFD2:  jmp     (L0326)

LFFD5:  jmp     LF542

LFFD8:  jmp     LF675

        jmp     LF767

        jmp     LF760

LFFE1:  jmp     (L0328)

LFFE4:  jmp     (L032A)

        jmp     (L032C)

LFFEA:  jmp     LF734

        jmp     LE505

        jmp     LE50A

LFFF3:  jmp     LE500

        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        lda     #$FE
        .byte   $22
        sbc     LFF72,x
