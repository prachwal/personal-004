; da65 V2.18 - Ubuntu 2.19-1
; Created:    2026-09-09 23:08:38
; Input file: roms/pet/pet-2001-8/rom-1-c000.901439-01.bin
; Page:       1


        .setcpu "6502"

L00C2           := $00C2
L00C8           := $00C8
L0D00           := $0D00
L4144           := $4144
L414F           := $414F
L43D2           := $43D2
L454C           := $454C
L454D           := $454D
L464F           := $464F
L4745           := $4745
L4944           := $4944
L494D           := $494D
L4957           := $4957
L4E49           := $4E49
L4F43           := $4F43
L4F46           := $4F46
L4F47           := $4F47
L4F4C           := $4F4C
L4F54           := $4F54
L5241           := $5241
L5453           := $5453
L5546           := $5546
L5551           := $5551
L5553           := $5553
LBFDD           := $BFDD
LC801           := $C801
LC863           := $C863
LC89D           := $C89D
LC9C8           := $C9C8
LC9D2           := $C9D2
LCA27           := $CA27
LCA47           := $CA47
LCA49           := $CA49
LCCA4           := $CCA4
LCCA7           := $CCA7
LCD52           := $CD52
LCD5D           := $CD5D
LCE13           := $CE13
LCE1C           := $CE1C
LCE45           := $CE45
LD130           := $D130
LD404           := $D404
LD676           := $D676
LDA74           := $DA74
LDAFD           := $DAFD
LDC94           := $DC94
LDC9F           := $DC9F
LFFCC           := $FFCC
LFFCF           := $FFCF
LFFE1           := $FFE1
LFFE7           := $FFE7
LC000:  .byte   $1D
LC001:  .byte   $C7
        pha
        dec     $35
        cpy     LC7EF
        cmp     $CA
        .byte   $DF
LC00B:  dex
        bvs     LBFDD
        .byte   $23
        .byte   $CB
        .byte   $9C
        iny
        .byte   $9C
        .byte   $C7
        .byte   $74
        .byte   $C7
        .byte   $1F
        iny
        .byte   $0C
        .byte   $C7
        .byte   $7F
        .byte   $C7
        cmp     #$C7
        .byte   $32
        iny
        .byte   $1B
        .byte   $C7
        .byte   $42
        iny
        ora     ($D7,x)
        .byte   $D4
        .byte   $FF
        .byte   $D7
        .byte   $FF
        .byte   $DA
        .byte   $FF
        sty     $D2,x
        sed
        dec     $7E,x
        cmp     #$9E
        cmp     #$44
        .byte   $C7
        .byte   $A7
        cmp     $6F
        .byte   $C7
        sty     $C9
        cmp     $BFFF,x
        .byte   $FF
        .byte   $C2
        .byte   $FF
        .byte   $9E
        dex
        bvc     LC00B
        .byte   $0B
        .byte   $DB
        .byte   $9E
        .byte   $DB
        rol     a
        .byte   $DB
        brk
        brk
        .byte   $64
        .byte   $D2
        sta     $D2
        bit     $DE
        eor     $DF
        .byte   $BF
        cld
        ldy     #$DE
        .byte   $9E
        .byte   $DF
        lda     $DF
        inc     $48DF
        cpx     #$E6
        dec     $54,x
        dec     $49,x
        .byte   $D3
        sta     $D6
        .byte   $63
        dec     $C4,x
        cmp     $D8,x
        cmp     $04,x
        dec     $0F,x
        dec     $79,x
        rol     $79D7,x
        .byte   $27
        .byte   $D7
        .byte   $7B
        .byte   $FF
        cld
        .byte   $7B
        .byte   $E3
        cmp     $2D7F,y
        dec     $D850,x
        dec     $D546
        dec     $667D
        dec     $E75A,x
        cmp     $0564
LC091:  .byte   $CF
LC092:  eor     $4E
        cpy     $46
        .byte   $4F
        .byte   $D2
        lsr     $5845
        .byte   $D4
        .byte   $44
        eor     ($54,x)
        cmp     ($49,x)
        lsr     $5550
        .byte   $54
        .byte   $A3
        eor     #$4E
        bvc     LC0FF
        .byte   $D4
        .byte   $44
        eor     #$CD
        .byte   $52
        eor     $41
        cpy     $4C
        eor     $D4
        .byte   $47
        .byte   $4F
        .byte   $54
        .byte   $CF
        .byte   $52
        eor     $CE,x
        eor     #$C6
        .byte   $52
        eor     $53
        .byte   $54
        .byte   $4F
        .byte   $52
        cmp     $47
        .byte   $4F
        .byte   $53
        eor     L00C2,x
        .byte   $52
        eor     $54
        eor     $52,x
        dec     $4552
        cmp     L5453
        .byte   $4F
        bne     LC127
        dec     $4157
        eor     #$D4
        jmp     L414F

        cpy     $53
        eor     ($56,x)
        cmp     $56
        eor     $52
        eor     #$46
        cmp     $4544,y
        dec     $50
        .byte   $4F
        .byte   $4B
        cmp     $50
        .byte   $52
        eor     #$4E
        .byte   $54
        .byte   $A3
        bvc     LC14C
        eor     #$4E
        .byte   $D4
        .byte   $43
        .byte   $4F
LC0FF:  lsr     $4CD4
        eor     #$53
        .byte   $D4
        .byte   $43
        jmp     L43D2

        eor     $53C4
        eor     $4FD3,y
        bvc     LC156
        dec     $4C43
        .byte   $4F
        .byte   $53
        cmp     $47
        eor     $D4
        lsr     $D745
        .byte   $54
        eor     ($42,x)
        tay
        .byte   $54
        .byte   $CF
        lsr     $CE
        .byte   $53
        .byte   $50
LC127:  .byte   $43
        tay
        .byte   $54
        pha
        eor     $CE
        lsr     $D44F
        .byte   $53
        .byte   $54
        eor     $D0
        .byte   $AB
        lda     $AFAA
        dec     $4E41,x
        cpy     $4F
        .byte   $D2
        ldx     $BCBD,y
        .byte   $53
        .byte   $47
        dec     L4E49
        .byte   $D4
        eor     ($42,x)
        .byte   $D3
        eor     $53,x
LC14C:  .byte   $D2
        lsr     $52
        cmp     $50
        .byte   $4F
        .byte   $D3
        .byte   $53
        eor     ($D2),y
LC156:  .byte   $52
        lsr     $4CC4
        .byte   $4F
        .byte   $C7
        eor     $58
        bne     LC1A3
        .byte   $4F
        .byte   $D3
        .byte   $53
        eor     #$CE
        .byte   $54
        eor     ($CE,x)
        eor     ($54,x)
        dec     $4550
        eor     $CB
        jmp     LCE45

        .byte   $53
        .byte   $54
        .byte   $52
        ldy     $56
        eor     ($CC,x)
        eor     ($53,x)
        .byte   $C3
        .byte   $43
        pha
        .byte   $52
        ldy     $4C
        eor     $46
        .byte   $54
        ldy     $52
        eor     #$47
        pha
        .byte   $54
        ldy     $4D
        eor     #$44
        ldy     $00
LC190:  lsr     $5845
        .byte   $54
        jsr     L4957
        .byte   $54
        pha
        .byte   $4F
        eor     $54,x
        jsr     L4F46
        .byte   $D2
        .byte   $53
        .byte   $59
        .byte   $4E
LC1A3:  .byte   $54
        eor     ($D8,x)
        .byte   $52
        eor     $54
        eor     $52,x
        lsr     $5720
        eor     #$54
        pha
        .byte   $4F
        eor     $54,x
        jsr     L4F47
        .byte   $53
        eor     L00C2,x
        .byte   $4F
        eor     $54,x
        jsr     L464F
        jsr     L4144
        .byte   $54
        cmp     ($49,x)
        jmp     L454C

        .byte   $47
        eor     ($4C,x)
        jsr     L5551
        eor     ($4E,x)
        .byte   $54
        eor     #$54
        cmp     $00,y
        brk
        brk
        brk
        .byte   $4F
        lsr     $45,x
        .byte   $52
        lsr     $4C
        .byte   $4F
        .byte   $D7
        .byte   $4F
        eor     $54,x
        jsr     L464F
        jsr     L454D
        eor     $524F
        cmp     $4E55,y
        .byte   $44
        eor     $46
        .byte   $27
        .byte   $44
        jsr     L5453
        eor     ($54,x)
        eor     $4D
        eor     $4E
        .byte   $D4
        .byte   $42
        eor     ($44,x)
        jsr     L5553
        .byte   $42
        .byte   $53
        .byte   $43
        .byte   $52
        eor     #$50
        .byte   $D4
        .byte   $52
        eor     $44
        eor     #$4D
        .byte   $27
        .byte   $44
        jsr     L5241
        .byte   $52
        eor     ($D9,x)
        .byte   $44
        eor     #$56
        eor     #$53
        eor     #$4F
        lsr     $4220
        eor     $5A20,y
        eor     $52
        .byte   $CF
        eor     #$4C
        jmp     L4745

        eor     ($4C,x)
        jsr     L4944
        .byte   $52
        eor     $43
        .byte   $D4
        .byte   $54
        eor     $4550,y
        jsr     L494D
        .byte   $53
        eor     $5441
        .byte   $43
        iny
        .byte   $53
        .byte   $54
        .byte   $52
        eor     #$4E
        .byte   $47
        jsr     L4F54
        .byte   $4F
        jsr     L4F4C
        lsr     $42C7
        eor     ($44,x)
        jsr     L4144
        .byte   $54
        cmp     ($46,x)
        .byte   $4F
        .byte   $52
        eor     $4C55
        eor     ($20,x)
        .byte   $54
        .byte   $4F
        .byte   $4F
        jsr     L4F43
        eor     $4C50
        eor     $D8
        .byte   $43
        eor     ($4E,x)
        .byte   $27
        .byte   $54
        jsr     L4F43
        lsr     $4954
        lsr     LC555
        eor     $4E,x
        .byte   $44
        eor     $46
        .byte   $27
        .byte   $44
        jsr     L5546
        lsr     $5443
        eor     #$4F
        dec     $4520
        .byte   $52
        .byte   $52
        .byte   $4F
        .byte   $52
        brk
        jsr     L4E49
        jsr     L0D00
        asl     a
        .byte   $52
        eor     $41
        .byte   $44
        eor     $0D2E,y
        asl     a
        brk
        ora     $420A
        .byte   $52
        eor     $41
        .byte   $4B
        brk
LC2AC:  tsx
        inx
        inx
        inx
        inx
LC2B1:  lda     $0101,x
        cmp     #$81
        bne     LC2D9
        lda     $99
        bne     LC2C6
        lda     $0102,x
        sta     $98
        lda     $0103,x
        sta     $99
LC2C6:  cmp     $0103,x
        bne     LC2D2
        lda     $98
        cmp     $0102,x
        beq     LC2D9
LC2D2:  txa
        clc
        adc     #$12
        tax
        bne     LC2B1
LC2D9:  rts

LC2DA:  jsr     LC32A
        sta     $80
        sty     $81
        sec
        lda     $A9
        sbc     $AE
        sta     $71
        tay
        lda     $AA
        sbc     $AF
        tax
        inx
        tya
        beq     LC315
        lda     $A9
        sec
        sbc     $71
        sta     $A9
        bcs     LC2FE
        dec     $AA
        sec
LC2FE:  lda     $A7
        sbc     $71
        sta     $A7
        bcs     LC30E
        dec     $A8
        bcc     LC30E
LC30A:  lda     ($A9),y
        sta     ($A7),y
LC30E:  dey
        bne     LC30A
        lda     ($A9),y
        sta     ($A7),y
LC315:  dec     $AA
        dec     $A8
        dex
        bne     LC30E
        rts

LC31D:  asl     a
        adc     #$36
        bcs     LC357
        sta     $71
        tsx
        cpx     $71
        bcc     LC357
        rts

LC32A:  cpy     $83
        bcc     LC356
        bne     LC334
        cmp     $82
        bcc     LC356
LC334:  pha
        ldx     #$09
        tya
LC338:  pha
        lda     $A6,x
        dex
        bpl     LC338
        jsr     LD404
        ldx     #$F7
LC343:  pla
        sta     $B0,x
        inx
        bmi     LC343
        pla
        tay
        pla
        cpy     $83
        bcc     LC356
        bne     LC357
        cmp     $82
        bcs     LC357
LC356:  rts

LC357:  ldx     #$52
LC359:  lsr     $64
        lda     $03
        beq     LC366
        jsr     LFFCC
        lda     #$00
        sta     $03
LC366:  jsr     LC9D2
        jsr     LCA47
LC36C:  lda     LC190,x
        pha
        and     #$7F
        jsr     LCA49
        inx
        pla
        bpl     LC36C
        jsr     LC584
        lda     #$8D
        ldy     #$C2
LC380:  jsr     LCA27
        ldy     $89
        iny
        beq     LC38B
        jsr     LDC94
LC38B:  lsr     $64
        lda     #$99
        ldy     #$C2
        jsr     LCA27
LC394:  jsr     LC468
        stx     $C9
        sty     $CA
        jsr     L00C2
        beq     LC394
        ldx     #$FF
        stx     $89
        bcc     LC3AC
        jsr     LC48D
        jmp     LC6E9

LC3AC:  jsr     LC863
        jsr     LC48D
        sty     $5C
        jsr     LC522
        bcc     LC3FD
        ldy     #$01
        lda     ($AE),y
        sta     $72
        lda     $7C
        sta     $71
        lda     $AF
        sta     $74
        lda     $AE
        dey
        sbc     ($AE),y
        clc
        adc     $7C
        sta     $7C
        sta     $73
        lda     $7D
        adc     #$FF
        sta     $7D
        sbc     $AF
        tax
        sec
        lda     $AE
        sbc     $7C
        tay
        bcs     LC3E7
        inx
        dec     $74
LC3E7:  clc
        adc     $71
        bcc     LC3EF
        dec     $72
        clc
LC3EF:  lda     ($71),y
        sta     ($73),y
        iny
        bne     LC3EF
        inc     $72
        inc     $74
        dex
        bne     LC3EF
LC3FD:  lda     $0A
        beq     LC430
        lda     $86
        ldy     $87
        sta     $82
        sty     $83
        lda     $7C
        sta     $A9
        adc     $5C
        sta     $A7
        ldy     $7D
        sty     $AA
        bcc     LC418
        iny
LC418:  sty     $A8
        jsr     LC2DA
        lda     $80
        ldy     $81
        sta     $7C
        sty     $7D
        ldy     $5C
        dey
LC428:  lda     $06,y
        sta     ($AE),y
        dey
        bpl     LC428
LC430:  jsr     LC567
        lda     $7A
        ldy     $7B
        sta     $71
        sty     $72
        clc
LC43C:  ldy     #$01
        lda     ($71),y
        bne     LC445
        jmp     LC394

LC445:  ldy     #$04
LC447:  iny
        lda     ($71),y
        bne     LC447
        iny
        tya
        adc     $71
        tax
        ldy     #$00
        sta     ($71),y
        lda     $72
        adc     #$00
        iny
        sta     ($71),y
        stx     $71
        sta     $72
        bcc     LC43C
        dex
        bpl     LC46A
        jsr     LC9D2
LC468:  ldx     #$00
LC46A:  jsr     LC479
        cmp     #$0D
        beq     LC476
        sta     $0A,x
        inx
        bne     LC46A
LC476:  jmp     LC9C8

LC479:  jsr     LFFCF
        ldy     $03
        bne     LC48C
        cmp     #$0F
        bne     LC48C
        pha
        lda     $64
        eor     #$FF
        sta     $64
        pla
LC48C:  rts

LC48D:  ldx     $C9
        ldy     #$04
        sty     $60
LC493:  lda     $00,x
        bpl     LC49E
        cmp     #$FF
        beq     LC4DC
        inx
        bne     LC493
LC49E:  cmp     #$20
        beq     LC4DC
        sta     $5B
        cmp     #$22
        beq     LC500
        bit     $60
        bvs     LC4DC
        cmp     #$3F
        bne     LC4B4
        lda     #$99
        bne     LC4DC
LC4B4:  cmp     #$30
        bcc     LC4BC
        cmp     #$3C
        bcc     LC4DC
LC4BC:  sty     $C0
        ldy     #$00
        sty     $5C
        dey
        stx     $C9
        dex
LC4C6:  iny
LC4C7:  inx
LC4C8:  lda     $00,x
        cmp     #$20
        beq     LC4C7
        sec
        sbc     LC092,y
        beq     LC4C6
        cmp     #$80
        bne     LC507
        ora     $5C
LC4DA:  ldy     $C0
LC4DC:  inx
        iny
        sta     $05,y
        lda     $05,y
        beq     LC51A
        sec
        sbc     #$3A
        beq     LC4EF
        cmp     #$49
        bne     LC4F1
LC4EF:  sta     $60
LC4F1:  sec
        sbc     #$55
        bne     LC493
        sta     $5B
LC4F8:  lda     $00,x
        beq     LC4DC
        cmp     $5B
        beq     LC4DC
LC500:  iny
        sta     $05,y
        inx
        bne     LC4F8
LC507:  ldx     $C9
        inc     $5C
LC50B:  iny
        lda     LC091,y
        bpl     LC50B
        lda     LC092,y
        bne     LC4C8
        lda     $00,x
        bpl     LC4DA
LC51A:  sta     $07,y
        lda     #$09
        sta     $C9
        rts

LC522:  lda     $7A
        ldx     $7B
LC526:  ldy     #$01
        sta     $AE
        stx     $AF
        lda     ($AE),y
        beq     LC54F
        iny
        iny
        lda     $09
        cmp     ($AE),y
        bcc     LC550
        beq     LC53D
        dey
        bne     LC546
LC53D:  lda     $08
        dey
        cmp     ($AE),y
        bcc     LC550
        beq     LC550
LC546:  dey
        lda     ($AE),y
        tax
        dey
        lda     ($AE),y
        bcs     LC526
LC54F:  clc
LC550:  rts

        bne     LC550
        lda     #$00
LC555:  tay
        sta     ($7A),y
        iny
        sta     ($7A),y
        lda     $7A
        adc     #$02
        sta     $7C
        lda     $7B
        adc     #$00
        sta     $7D
LC567:  jsr     LC59A
LC56A:  lda     $86
        ldy     $87
        sta     $82
        sty     $83
        jsr     LFFE7
        lda     $7C
        ldy     $7D
        sta     $7E
        sty     $7F
        sta     $80
        sty     $81
        jsr     LC70D
LC584:  ldx     #$68
        stx     $65
        pla
        sta     $01FD
        pla
        sta     $01FE
        ldx     #$FC
        txs
        lda     #$00
        sta     $8D
        sta     $61
LC599:  rts

LC59A:  clc
        lda     $7A
        adc     #$FF
        sta     $C9
        lda     $7B
        adc     #$FF
        sta     $CA
        rts

        bcc     LC5B0
        beq     LC5B0
        cmp     #$AB
        bne     LC599
LC5B0:  jsr     LC863
        jsr     LC522
        jsr     L00C8
        beq     LC5C7
        cmp     #$AB
        bne     LC550
        jsr     L00C2
        jsr     LC863
        bne     LC550
LC5C7:  pla
        pla
        lda     $08
        ora     $09
        bne     LC5D5
        lda     #$FF
        sta     $08
        sta     $09
LC5D5:  ldy     #$01
        sty     $60
        lda     ($AE),y
        beq     LC61E
        jsr     LFFE1
        jsr     LC9D2
        iny
        lda     ($AE),y
        tax
        iny
        lda     ($AE),y
        cmp     $09
        bne     LC5F2
        cpx     $08
        beq     LC5F4
LC5F2:  bcs     LC61E
LC5F4:  sty     $98
        jsr     LDC9F
        lda     #$20
LC5FB:  ldy     $98
        and     #$7F
LC5FF:  jsr     LCA49
        cmp     #$22
        bne     LC60C
        lda     $60
        eor     #$FF
        sta     $60
LC60C:  iny
        lda     ($AE),y
        bne     LC621
        tay
        lda     ($AE),y
        tax
        iny
        lda     ($AE),y
        stx     $AE
        sta     $AF
        bne     LC5D5
LC61E:  jmp     LC38B

LC621:  bpl     LC5FF
        cmp     #$FF
        beq     LC5FF
        bit     $60
        bmi     LC5FF
        sec
        sbc     #$7F
        tax
        sty     $98
        ldy     #$FF
LC633:  dex
        beq     LC63E
LC636:  iny
        lda     LC092,y
        bpl     LC636
        bmi     LC633
LC63E:  iny
        lda     LC092,y
        bmi     LC5FB
        jsr     LCA49
        bne     LC63E
        lda     #$80
        sta     $61
        jsr     LC89D
        jsr     LC2AC
        bne     LC65A
        txa
        adc     #$0F
        tax
        txs
LC65A:  pla
        pla
        lda     #$09
        jsr     LC31D
        jsr     LC7FE
        clc
        tya
        adc     $C9
        pha
        lda     $CA
        adc     #$00
        pha
        lda     $89
        pha
        lda     $88
        pha
        lda     #$A4
        jsr     LCE13
        jsr     LCCA7
        jsr     LCCA4
        lda     $B5
        ora     #$7F
        and     $B1
        sta     $B1
        lda     #$92
        ldy     #$C6
        sta     $71
        sty     $72
        jmp     LCD5D

        lda     #$91
        ldy     #$D8
        jsr     LDA74
        jsr     L00C8
        cmp     #$A9
        bne     LC6A6
        jsr     L00C2
        jsr     LCCA4
LC6A6:  jsr     LDAFD
        jsr     LCD52
        lda     $99
        pha
        lda     $98
        pha
        lda     #$81
        pha
LC6B5:  jsr     LFFE1
        lda     $C9
        ldy     $CA
        beq     LC6C4
        sta     $8C
        sty     $8D
        ldy     #$00
LC6C4:  lda     ($C9),y
        beq     LC6CF
        cmp     #$3A
        beq     LC6E9
LC6CC:  jmp     LCE1C

LC6CF:  ldy     #$02
        lda     ($C9),y
        clc
        beq     LC735
        iny
        lda     ($C9),y
        sta     $88
        iny
        lda     ($C9),y
        sta     $89
        tya
        adc     $C9
        sta     $C9
        bcc     LC6E9
        inc     $CA
LC6E9:  jsr     L00C2
        jsr     LC6F2
        jmp     LC6B5

LC6F2:  beq     LC75E
        sec
        sbc     #$80
        bcs     LC6FC
        jmp     LC89D

LC6FC:  cmp     #$23
        bcs     LC6CC
        asl     a
        tay
        lda     LC001,y
        pha
        lda     LC000,y
        pha
        jmp     L00C2

LC70D:  sec
        lda     $7A
        sbc     #$01
        ldy     $7B
        bcs     LC717
        dey
LC717:  sta     $90
        sty     $91
        rts

        bcs     LC71F
        clc
LC71F:  bne     LC75E
        lda     $C9
        ldy     $CA
        beq     LC733
        sta     $8C
        sty     $8D
        lda     $88
        ldy     $89
        sta     $8A
        sty     $8B
LC733:  pla
        pla
LC735:  lda     #$A4
        ldy     #$C2
        ldx     #$00
        stx     $64
        bcc     LC742
        jmp     LC380

LC742:  jmp     LC38B

        bne     LC75E
        ldx     #$DF
        ldy     $8D
        bne     LC750
        jmp     LC359

LC750:  lda     $8C
        sta     $C9
        sty     $CA
        lda     $8A
        ldy     $8B
        sta     $88
        sty     $89
LC75E:  rts

        jsr     LD676
        bne     LC75E
        inx
        cpx     #$0A
        bcs     LC76D
        dex
        stx     $04
        rts

LC76D:  jmp     LD130

        bne     LC75E
        jmp     LC56A

        bne     LC77A
        jmp     LC567

LC77A:  jsr     LC56A
        jmp     LC794

        lda     #$03
        jsr     LC31D
        lda     $CA
        pha
        lda     $C9
        pha
        lda     $89
        pha
        lda     $88
        pha
        lda     #$8D
        pha
LC794:  jsr     L00C8
        jsr     LC79D
        jmp     LC6B5

LC79D:  jsr     LC863
        jsr     LC801
        lda     $89
        cmp     $09
        bcs     LC7B4
        tya
        sec
        adc     $C9
        ldx     $CA
        bcc     LC7B8
        inx
        bcs     LC7B8
LC7B4:  lda     $7A
        ldx     $7B
LC7B8:  jsr     LC526
        bcc     LC7DB
        lda     $AE
        sbc     #$01
        sta     $C9
        lda     $AF
        sbc     #$00
        sta     $CA
LC7C9:  rts

        bne     LC7C9
        lda     #$FF
        sta     $98
        jsr     LC2AC
        txs
        cmp     #$8D
        beq     LC7E3
        ldx     #$16
        .byte   $2C
LC7DB:  ldx     #$5F
        jmp     LC359

        jmp     LCE1C

LC7E3:  pla
        pla
        sta     $88
        pla
        sta     $89
        pla
        sta     $C9
        pla
        .byte   $85
LC7EF:  dex
        jsr     LC7FE
        tya
        clc
        adc     $C9
        sta     $C9
        bcc     LC7FD
        inc     $CA
LC7FD:  rts

LC7FE:  ldx     #$3A
