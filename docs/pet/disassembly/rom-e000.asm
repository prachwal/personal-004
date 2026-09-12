; da65 V2.18 - Ubuntu 2.19-1
; Created:    2026-09-09 23:08:28
; Input file: roms/pet/pet-2001-8/rom-1-e000.901439-03.bin
; Page:       1


        .setcpu "6502"

L0219           := $0219
L021B           := $021B
L125B           := $125B
L2A2A           := $2A2A
L474A           := $474A
L4F43           := $4F43
L5246           := $5246
L5942           := $5942
L7048           := $7048
L91CC           := $91CC
LC32A           := $C32A
LC38B           := $C38B
LC553           := $C553
LC72B           := $C72B
LC9D2           := $C9D2
LCA27           := $CA27
LCB88           := $CB88
LCE3B           := $CE3B
LCE69           := $CE69
LCE82           := $CE82
LCE90           := $CE90
LD725           := $D725
LD9E1           := $D9E1
LDA74           := $DA74
LDC9F           := $DC9F
LDE67           := $DE67
LDEF3           := $DEF3
LDFD7           := $DFD7
LE824           := $E824
LE838           := $E838
LFFEA           := $FFEA
        ldx     $A0
        brk
        jsr     LDA74
        lda     #$00
        sta     $B5
        lda     $63
        jsr     LE016
        lda     #$9D
        ldy     #$00
        jmp     LD9E1

LE016:  pha
        jmp     LDFD7

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
        lda     $48,x
        bpl     LE050
        jsr     LDE67
LE050:  lda     $B0
        pha
        cmp     #$81
        bcc     LE05E
        lda     #$91
        ldy     #$D8
        jsr     LD9E1
LE05E:  lda     #$78
        ldy     #$E0
        jsr     LDEF3
        pla
        cmp     #$81
        bcc     LE071
        lda     #$1A
        ldy     #$E0
        jsr     LD725
LE071:  pla
        bpl     LE077
        jmp     LDE67

LE077:  rts

        .byte   $0B
        ror     $B3,x
        .byte   $83
        lda     $79D3,x
        asl     $A6F4,x
        sbc     $7B,x
        .byte   $83
        .byte   $FC
        bcs     LE098
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
        bvs     LE0E3
        .byte   $7D
LE098:  .byte   $B7
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
LE0B4:  brk
LE0B5:  inc     $C9
        bne     LE0BB
        inc     $CA
LE0BB:  lda     $EA60
        cmp     #$3A
        bcs     LE0CC
        cmp     #$20
        beq     LE0B5
        sec
        sbc     #$30
        sec
        sbc     #$D0
LE0CC:  rts

        .byte   $80
        .byte   $4F
        .byte   $C7
        .byte   $52
        eor     $FFA2,y
        stx     $89
        txs
        lda     #$4C
        sta     $A3
        sta     $00
        lda     #$30
        ldy     #$D1
        sta     $01
LE0E3:  sty     $02
        ldx     #$1C
LE0E7:  lda     LE0B4,x
        sta     $C1,x
        dex
        bne     LE0E7
        txa
        sta     $B7
        sta     $03
        sta     $67
        sta     $04
        sta     $05
        pha
        sta     $64
        lda     #$03
        sta     $A2
        lda     #$2C
        sta     $09
        jsr     LC9D2
        ldx     #$68
        stx     $65
        lda     #$00
        ldy     #$04
        sta     $08
        sty     $09
        ldy     #$00
LE116:  inc     $08
        bne     LE122
        inc     $09
        lda     $09
        cmp     #$80
        beq     LE131
LE122:  lda     #$92
        sta     ($08),y
        cmp     ($08),y
        bne     LE131
        asl     a
        sta     ($08),y
        cmp     ($08),y
        beq     LE116
LE131:  lda     $08
        ldy     $09
        sta     $86
        sty     $87
        sta     $82
        sty     $83
        ldx     #$00
        ldy     #$04
        stx     $7A
        sty     $7B
        ldy     #$00
        tya
        sta     ($7A),y
        inc     $7A
        bne     LE150
        inc     $7B
LE150:  lda     $7A
        ldy     $7B
        jsr     LC32A
        jsr     LC9D2
        lda     $86
        sec
        sbc     $7A
        tax
        lda     $87
        sbc     $7B
        jsr     LDC9F
        lda     #$74
        ldy     #$E1
        jsr     LCA27
        jsr     LC553
        jmp     LC38B

        jsr     L5942
        .byte   $54
        eor     $53
        jsr     L5246
        eor     $45
        .byte   $13
        rol     a
        rol     a
        rol     a
        jsr     L4F43
        eor     $4F4D
        .byte   $44
        .byte   $4F
        .byte   $52
        eor     $20
        .byte   $42
        eor     ($53,x)
        eor     #$43
        jsr     L2A2A
        rol     a
        ora     ($11),y
        ora     ($00),y
        clc
        jmp     LC72B

        bit     $B4
        bpl     LE1AA
        cmp     #$54
        bne     LE1AA
        jmp     LCE3B

LE1AA:  rts

        bit     $B4
        bmi     LE1B2
        jmp     LCE90

LE1B2:  cmp     #$54
        beq     LE1B9
        jmp     LCE82

LE1B9:  jmp     LCE69

        sta     $5A
        inx
        jmp     LE1D9

        bpl     LE1C9
        lda     $8E
        ldy     $8F
        rts

LE1C9:  ldy     #$FF
        rts

        bne     LE1D8
LE1CE:  inc     $05
        bne     LE1D8
        lda     $E2
        sta     $05
        bne     LE1CE
LE1D8:  rts

LE1D9:  stx     $C9
        pla
        pla
        tya
        jmp     LCB88

        lda     #$7F
        sta     $E84E
        sta     $06
        sta     $07
        ldx     #$00
        txa
LE1ED:  sta     $0200,x
        dex
        bne     LE1ED
        dex
        lda     #$85
        sta     L0219
        lda     #$E6
        sta     $021A
        lda     #$03
        sta     $0264
        lda     #$0F
        sta     $E810
        asl     a
        sta     $E840
        sta     $E842
        stx     $E822
        stx     $E845
        lda     #$3D
        sta     $E813
        bit     $E812
        lda     #$3C
        sta     $E821
        sta     $E823
        sta     $E811
        stx     $E822
        lda     #$0C
        sta     $E84C
        sta     $0225
        sta     $0224
LE236:  ldy     #$00
        sty     $020E
        sty     $E0
        lda     #$84
        sta     $E1
LE241:  lda     #$20
        dec     $E1
LE245:  sta     ($E0),y
        dey
        bne     LE245
        lda     $E1
        cmp     #$80
        bne     LE241
        lda     #$80
        ldx     #$19
LE254:  sta     $0228,x
        dex
        bne     LE254
        ldy     #$12
        jsr     LE273
        ldy     #$0C
        jsr     LE273
        ldy     #$05
        jsr     LE273
LE269:  ldy     #$00
        sty     $E2
        sty     $F5
        jsr     LE5DB
        rts

LE273:  ldx     #$18
LE275:  inc     $0229,x
        dex
        dey
        bne     LE275
        rts

LE27D:  ldy     $020F
        ldx     #$00
LE282:  lda     $0210,x
        sta     $020F,x
        inx
        cpx     $020D
        bne     LE282
        dec     $020D
        tya
        cli
        rts

LE294:  jsr     LE3EA
LE297:  lda     $020D
        sta     $0224
        beq     LE297
        sei
        lda     $0227
        beq     LE2B0
        lda     $0226
        ldy     #$00
        sty     $0227
        jsr     LE7AC
LE2B0:  jsr     LE27D
        cmp     #$83
        bne     LE2C8
        sei
        ldx     #$09
        stx     $020D
LE2BD:  lda     LE7D4,x
        sta     $020E,x
        dex
        bne     LE2BD
        beq     LE297
LE2C8:  cmp     #$0D
        bne     LE294
        ldy     $F2
        sty     $0260
LE2D1:  lda     ($E0),y
        cmp     #$20
        bne     LE2DA
        dey
        bne     LE2D1
LE2DA:  iny
        sty     $021E
        ldy     #$00
        sty     $E2
        sty     $EA
        lda     $0220
        bmi     LE303
        cmp     $F5
        bne     LE303
        lda     $0221
        sta     $E2
        cmp     $021E
        bcc     LE303
        jmp     LE327

        tya
        pha
        txa
        pha
        lda     $0260
        beq     LE297
LE303:  ldy     $E2
        lda     ($E0),y
        sta     $F6
        and     #$3F
        asl     $F6
        bit     $F6
        bpl     LE313
        ora     #$80
LE313:  bcc     LE319
        ldx     $EA
        bne     LE31D
LE319:  bvs     LE31D
        ora     #$40
LE31D:  inc     $E2
        jsr     LE349
        cpy     $021E
        bne     LE33A
LE327:  lda     #$00
        sta     $0260
        lda     #$0D
        ldx     $0264
        cpx     #$03
        beq     LE338
        jsr     LE3EA
LE338:  lda     #$0D
LE33A:  sta     $F6
        pla
        tax
        pla
        tay
        lda     $F6
        cmp     #$DE
        bne     LE348
        lda     #$FF
LE348:  rts

LE349:  cmp     #$22
        bne     LE355
        lda     $EA
        eor     #$01
        sta     $EA
        lda     #$22
LE355:  rts

LE356:  ora     #$40
LE358:  ldx     $020E
        beq     LE35F
LE35D:  ora     #$80
LE35F:  ldx     $FB
        beq     LE365
        dec     $FB
LE365:  jsr     LE7AC
        inc     $E2
        ldy     $F2
        cpy     $E2
        bcs     LE38A
        ldx     $F5
        cpy     #$4F
        bne     LE383
        jsr     LE397
        jsr     LE530
        lda     #$00
        sta     $E2
        jmp     LE38A

LE383:  cpx     #$18
        bne     LE3A4
        jsr     LE3AA
LE38A:  pla
        tay
        lda     $FB
        beq     LE392
        lsr     $EA
LE392:  pla
        tax
        pla
        cli
        rts

LE397:  cpx     #$17
        bcs     LE3A3
        lda     $022B,x
        ora     #$80
        sta     $022B,x
LE3A3:  rts

LE3A4:  jsr     LE3B1
        jmp     LE38A

LE3AA:  jsr     LE559
        dec     $F5
        ldx     $F5
LE3B1:  asl     $022A,x
        lsr     $022A,x
        jsr     LE397
        lda     $E2
        pha
        jsr     LE5DB
        pla
        sta     $E2
        rts

LE3C4:  ldy     #$27
        ldx     $F5
        bne     LE3D0
        stx     $E2
        pla
        pla
        bne     LE38A
LE3D0:  lda     $0228,x
        bmi     LE3DB
        dex
        lda     $0228,x
        ldy     #$4F
LE3DB:  dex
        stx     $F5
        sta     $E1
        lda     LE7BC,x
        sta     $E0
        sty     $E2
        sty     $F2
        rts

LE3EA:  pha
        sta     $F6
        txa
        pha
        tya
        pha
        lda     #$00
        sta     $0260
        ldy     $E2
        lda     $F6
        bpl     LE3FF
        jmp     LE48F

LE3FF:  cmp     #$0D
        bne     LE406
        jmp     LE548

LE406:  cmp     #$20
        bcc     LE412
        and     #$3F
        jsr     LE349
        jmp     LE358

LE412:  ldx     $FB
        beq     LE419
        jmp     LE35D

LE419:  cmp     #$14
        bne     LE439
        dey
        sty     $E2
        bpl     LE428
        jsr     LE3C4
        jmp     LE433

LE428:  iny
        lda     ($E0),y
        dey
        sta     ($E0),y
        iny
        cpy     $F2
        bne     LE428
LE433:  lda     #$20
        sta     ($E0),y
        bne     LE476
LE439:  ldx     $EA
        beq     LE440
        jmp     LE35D

LE440:  cmp     #$12
        bne     LE447
        sta     $020E
LE447:  cmp     #$13
        bne     LE44E
        jsr     LE269
LE44E:  cmp     #$1D
        bne     LE464
        iny
        sty     $E2
        dey
        cpy     $F2
        bcc     LE461
        jsr     LE530
        ldy     #$00
LE45F:  sty     $E2
LE461:  jmp     LE38A

LE464:  cmp     #$11
        bne     LE476
        clc
        tya
        adc     #$28
        tay
        cmp     $F2
        bcc     LE45F
        beq     LE45F
        jsr     LE530
LE476:  jmp     LE38A

        inx
        sta     $F5
        tya
        sbc     #$28
        sta     $E2
        inc     $F5
        lda     LE7BC
        sta     $E0
        lda     $0229
        sta     $E1
        bne     LE476
LE48F:  and     #$7F
        cmp     #$7F
        bne     LE497
        lda     #$5E
LE497:  cmp     #$20
        bcc     LE49E
        jmp     LE356

LE49E:  cmp     #$0D
        bne     LE4A5
        jmp     LE548

LE4A5:  ldx     $EA
        bne     LE4D9
        cmp     #$14
        bne     LE4D5
        ldy     $F2
        lda     ($E0),y
        cmp     #$20
        bne     LE4B9
        cpy     $E2
        bne     LE4C0
LE4B9:  cpy     #$4F
        beq     LE476
        jsr     LE605
LE4C0:  ldy     $F2
LE4C2:  dey
        lda     ($E0),y
        iny
        sta     ($E0),y
        dey
        cpy     $E2
        bne     LE4C2
        lda     #$20
        sta     ($E0),y
        inc     $FB
        bne     LE52D
LE4D5:  ldx     $FB
        beq     LE4DE
LE4D9:  ora     #$40
        jmp     LE35D

LE4DE:  cmp     #$11
        bne     LE50E
        lda     $E2
        cmp     #$28
        bcc     LE4EE
        sbc     #$28
        sta     $E2
        bcs     LE52D
LE4EE:  ldx     $F5
        beq     LE52D
        lda     $0228,x
        bpl     LE4FE
        dec     $F5
        jsr     LE5DB
        bcc     LE52D
LE4FE:  dex
        dex
        stx     $F5
        jsr     LE5DB
        lda     $E2
        clc
        adc     #$28
        sta     $E2
        bne     LE52D
LE50E:  cmp     #$12
        bne     LE517
        lda     #$00
        sta     $020E
LE517:  cmp     #$1D
        bne     LE526
        dey
        sty     $E2
        bpl     LE52D
        jsr     LE3C4
        jmp     LE38A

LE526:  cmp     #$13
        bne     LE52D
        jsr     LE236
LE52D:  jmp     LE38A

LE530:  sec
        lsr     $0220
        ldx     $F5
LE536:  inx
        cpx     #$19
        bne     LE53E
        jsr     LE559
LE53E:  lda     $0229,x
        bpl     LE536
        stx     $F5
        jmp     LE5DB

LE548:  lda     #$00
        sta     $FB
        sta     $020E
        sta     $EA
        sta     $E2
        jsr     LE530
        jmp     LE38A

LE559:  sei
        ldy     #$00
        sty     $E0
        lda     #$80
        sta     $E4
        sta     $E1
        lda     #$28
        bit     $022A
        bmi     LE56D
        lda     #$50
LE56D:  sta     $E3
        lda     #$34
        sta     $E811
LE574:  lda     ($E3),y
        sta     ($E0),y
        iny
        bne     LE574
        inc     $E4
        inc     $E1
        lda     #$84
        cmp     $E4
        bne     LE574
        lda     #$E8
        sta     $E0
        dec     $E1
        lda     #$20
LE58D:  dec     $E0
        dec     $E3
        sta     ($E0),y
        bne     LE58D
        ldx     #$19
        stx     $F5
LE599:  ldx     #$00
        dec     $F5
LE59D:  lda     $0229,x
        and     #$7F
        ldy     $022A,x
        bpl     LE5A9
        ora     #$80
LE5A9:  sta     $0229,x
        inx
        cpx     #$19
        bne     LE59D
        lda     #$83
        sta     $0241
        lda     $0229
        bpl     LE599
        lda     #$3C
        sta     $E811
        cli
        lda     #$FE
        cmp     $E812
        bne     LE5D8
        ldy     #$08
LE5CA:  sta     $E845
LE5CD:  bit     $E84D
        bvc     LE5CD
        dey
        bne     LE5CA
        sty     $020D
LE5D8:  ldx     $F5
        rts

LE5DB:  ldx     $F5
        lda     $0229,x
        ora     #$80
        sta     $E1
        lda     LE7BC,x
        sta     $E0
        lda     #$27
        sta     $F2
        cpx     #$18
        beq     LE5FA
        lda     $022A,x
        bmi     LE5FA
        lda     #$4F
        sta     $F2
LE5FA:  lda     $E2
        cmp     #$28
        bcc     LE604
        sbc     #$28
        sta     $E2
LE604:  rts

LE605:  ldx     $F5
        inx
        sei
        lda     #$34
        sta     $E811
        cpx     #$17
        beq     LE648
        bcc     LE617
        jmp     LE3AA

LE617:  ldx     #$17
LE619:  lda     $022A,x
        ora     #$80
        sta     $E4
        ldy     $0229,x
        bmi     LE627
        and     #$7F
LE627:  sta     $022A,x
        tya
        ora     #$80
        sta     $E1
        ldy     #$27
        lda     LE7BD,x
        sta     $E3
        lda     LE7BC,x
        sta     $E0
LE63B:  lda     ($E0),y
        sta     ($E3),y
        dey
        bpl     LE63B
        dex
        cpx     $F5
        bne     LE619
        inx
LE648:  lda     $0229,x
        ora     #$80
        sta     $E1
        and     #$7F
        sta     $0229,x
        lda     LE7BC,x
        sta     $E0
        ldy     #$27
        lda     #$20
LE65D:  sta     ($E0),y
        dey
        bpl     LE65D
        lda     #$3C
        sta     $E811
        cli
        jmp     LE5DB

        pha
        txa
        pha
        tya
        pha
        tsx
        lda     $0104,x
        and     #$10
        beq     LE67B
        jmp     (L021B)

LE67B:  jmp     (L0219)

LE67E:  pla
        tay
        pla
        tax
        pla
        rti

        rts

        jsr     LFFEA
        lda     $0224
        bne     LE6B0
        dec     $0225
        bne     LE6B0
        lda     #$14
        sta     $0225
        ldy     $E2
        lsr     $0227
        lda     ($E0),y
        bcs     LE6A6
        inc     $0227
        sta     $0226
LE6A6:  asl     a
        bcs     LE6AC
        sec
        bcs     LE6AD
LE6AC:  clc
LE6AD:  ror     a
        sta     ($E0),y
LE6B0:  ldx     #$FF
        stx     $0223
        inx
        stx     $0204
        ldx     #$50
        lda     $E810
        and     #$F0
        sta     $E810
        ldy     #$00
        lda     $E810
        asl     a
        asl     a
        asl     a
        bpl     LE6D4
        sty     $0207
        lda     #$3D
        bne     LE6DB
LE6D4:  lda     $0207
        bne     LE6DE
        lda     #$35
LE6DB:  sta     $E813
LE6DE:  bcc     LE6EA
        sty     $0208
        lda     $E840
        ora     #$10
        bne     LE6F4
LE6EA:  lda     $0208
        bne     LE6F7
        lda     $E840
        and     #$EF
LE6F4:  sta     $E840
LE6F7:  ldy     #$08
        lda     $E812
        cmp     $E812
        bne     LE6F7
LE701:  lsr     a
        bcs     LE709
        pha
        jsr     LE73F
        pla
LE709:  dex
        beq     LE714
        dey
        bne     LE701
        inc     $E810
        bne     LE6F7
LE714:  lda     $0223
        cmp     $0203
        beq     LE73C
        sta     $0203
        tax
        bmi     LE73C
        lda     LE75B,x
        lsr     $0204
        bcc     LE72C
        ora     #$80
LE72C:  ldx     $020D
        sta     $020F,x
        inx
        cpx     #$0A
        bne     LE739
        ldx     #$00
LE739:  stx     $020D
LE73C:  jmp     LE67E

LE73F:  lda     LE75B,x
        bne     LE74B
        lda     #$01
        sta     $0204
        bne     LE75B
LE74B:  cmp     #$FF
        beq     LE75B
        cmp     #$3C
        bne     LE758
        bit     $E811
        bmi     LE75B
LE758:  stx     $0223
LE75B:  rts

        and     $FF2E,x
        .byte   $03
        .byte   $3C
        jsr     L125B
        and     a:$30
        rol     $5DFF,x
        rti

        brk
        .byte   $2B
        .byte   $32
        .byte   $FF
        .byte   $3F
        bit     $564E
        cli
        .byte   $33
        and     ($0D),y
        .byte   $3B
        eor     $4342
        .byte   $5A
        rol     a
        and     $FF,x
        .byte   $3A
LE780:  .byte   $4B
        pha
        lsr     $53
        rol     $34,x
        .byte   $FF
        jmp     L474A

        .byte   $44
        eor     ($2F,x)
        sec
        .byte   $FF
        bvc     LE7DA
        eor     $5752,y
        and     $5E37,y
        .byte   $4F
        eor     $54,x
        eor     $51
        .byte   $14
        ora     ($FF),y
        and     #$5C
        .byte   $27
        bit     $22
        ora     $5F13,x
        plp
        rol     $25
        .byte   $23
        .byte   $21
LE7AC:  tay
LE7AD:  lda     $E840
        and     #$20
        bne     LE7AD
        tya
        ldy     $E2
        sta     ($E0),y
        rts

        pla
        rts

LE7BC:  brk
LE7BD:  plp
        bvc     LE838
        ldy     #$C8
        beq     LE7DC
        rti

        pla
        bcc     LE780
LE7C8:  cpx     #$08
        bmi     LE824
        .byte   $80
        tay
        bne     LE7C8
        jsr     L7048
        tya
LE7D4:  cpy     #$4C
        .byte   $4F
        eor     ($44,x)
        .byte   $0D
LE7DA:  .byte   $52
        .byte   $55
LE7DC:  lsr     $B90D
        brk
        beq     LE7EA
        and     #$7F
        jsr     LE3EA
        iny
        plp
        .byte   $10
LE7EA:  .byte   $F3
        rts

        brk
        brk
        brk
        brk
        brk
        brk
        brk
        asl     a
        adc     #$05
        ldy     #$00
        rts

        brk
        brk
        brk
        brk
        brk
        brk
        brk
