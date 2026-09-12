; da65 V2.18 - Ubuntu 2.19-1
; Created:    2026-09-09 21:53:17
; Input file: roms/vic20/basic.bin
; Page:       1


        .setcpu "6502"

L0022           := $0022
L0054           := $0054
L0073           := $0073
L0079           := $0079
L0080           := $0080
L0300           := $0300
L0302           := $0302
L0304           := $0304
L0306           := $0306
L0308           := $0308
L030A           := $030A
L0D00           := $0D00
L2045           := $2045
L3580           := $3580
L4144           := $4144
L414D           := $414D
L414F           := $414F
L43D2           := $43D2
L4544           := $4544
L454C           := $454C
L454D           := $454D
L464F           := $464F
L4745           := $4745
L4944           := $4944
L4946           := $4946
L494D           := $494D
L4957           := $4957
L4DC5           := $4DC5
L4E49           := $4E49
L4EC5           := $4EC5
L4F43           := $4F43
L4F46           := $4F46
L4F47           := $4F47
L4F4C           := $4F4C
L4F54           := $4F54
L504F           := $504F
L5241           := $5241
L5245           := $5245
L5246           := $5246
L5250           := $5250
L5453           := $5453
L5546           := $5546
L554F           := $554F
L5551           := $5551
L5553           := $5553
LBFD6           := $BFD6
LBFEA           := $BFEA
LE040           := $E040
LE109           := $E109
LE10F           := $E10F
LE115           := $E115
LE11B           := $E11B
LE121           := $E121
LFF90           := $FF90
LFFB7           := $FFB7
LFFCC           := $FFCC
LFFDB           := $FFDB
LFFDE           := $FFDE
LFFE1           := $FFE1
LFFE7           := $FFE7
LFFF0           := $FFF0
        sei
        .byte   $E3
        .byte   $67
        cpx     $43
        .byte   $42
        eor     $4142
        .byte   $53
        eor     #$43
LC00C:  .byte   $30
LC00D:  iny
        eor     ($C7,x)
        ora     $F7CD,x
        iny
        ldy     $CB
        ldx     $80CB,y
        bne     LC020
        cpy     LC9A4
        .byte   $9F
        iny
LC020:  bvs     LBFEA
        .byte   $27
        cmp     #$1C
        iny
        .byte   $82
        iny
        cmp     ($C8),y
        .byte   $3A
        cmp     #$2E
        iny
        lsr     a
        cmp     #$2C
        cld
        .byte   $64
        sbc     ($52,x)
        sbc     ($61,x)
        sbc     ($B2,x)
        .byte   $D3
        .byte   $23
        cld
        .byte   $7F
        dex
        .byte   $9F
        dex
        lsr     $C8,x
        .byte   $9B
        dec     $5D
        dec     $85
        dex
        rol     $E1
        tsx
        sbc     ($C3,x)
        sbc     ($7A,x)
        .byte   $CB
        eor     ($C6,x)
        and     LCCDC,y
        .byte   $DC
        cli
        .byte   $DC
        brk
        brk
        adc     $9ED3,x
        .byte   $D3
        adc     ($DF),y
        sty     $E0,x
        nop
        cmp     LDFED,y
        adc     ($E2,x)
        pla
        .byte   $E2
        lda     ($E2),y
        .byte   $0B
        .byte   $E3
        ora     $7CD8
        .byte   $D7
        adc     $D4
        lda     $8BD7
        .byte   $D7
        .byte   $EC
LC079:  dec     $00,x
        .byte   $D7
        bit     $37D7
        .byte   $D7
LC080:  .byte   $79
LC081:  .byte   $69
LC082:  cld
        adc     LD852,y
        .byte   $7B
        rol     a
        .byte   $DA
        .byte   $7B
        ora     ($DB),y
        .byte   $7F
        .byte   $7A
        .byte   $DF
        bvc     LC079
        .byte   $CF
        lsr     $E5
        .byte   $CF
        adc     LDFB3,x
        .byte   $5A
        .byte   $D3
        dec     $1564
LC09D:  .byte   $D0
LC09E:  eor     $4E
        cpy     $46
        .byte   $4F
        .byte   $D2
        lsr     $5845
        .byte   $D4
        .byte   $44
        eor     (L0054,x)
        cmp     ($49,x)
        lsr     $5550
        .byte   $54
        .byte   $A3
        eor     #$4E
        bvc     LC10B
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
        eor     $C2,x
        .byte   $52
        eor     L0054
        eor     $52,x
        dec     $4552
        cmp     L5453
        .byte   $4F
        bne     LC133
LC0E4:  dec     $4157
        eor     #$D4
        jmp     L414F

        cpy     $53
        eor     ($56,x)
        cmp     $56
        eor     $52
        eor     #$46
        cmp     L4544,y
        dec     $50
        .byte   $4F
        .byte   $4B
        cmp     $50
        .byte   $52
        eor     #$4E
        .byte   $54
        .byte   $A3
        bvc     LC158
        eor     #$4E
        .byte   $D4
        .byte   $43
        .byte   $4F
LC10B:  lsr     $4CD4
        eor     #$53
        .byte   $D4
        .byte   $43
        jmp     L43D2

        eor     $53C4
        eor     $4FD3,y
        bvc     LC162
        dec     $4C43
        .byte   $4F
        .byte   $53
        cmp     $47
        eor     $D4
        lsr     LD745
        .byte   $54
        eor     ($42,x)
        tay
        .byte   $54
        .byte   $CF
        lsr     $CE
        .byte   $53
        .byte   $50
LC133:  .byte   $43
        tay
        .byte   $54
        pha
        eor     $CE
        lsr     LD44F
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
LC158:  .byte   $D2
        lsr     $52
        cmp     $50
        .byte   $4F
        .byte   $D3
        .byte   $53
        eor     ($D2),y
LC162:  .byte   $52
        lsr     $4CC4
        .byte   $4F
        .byte   $C7
        eor     $58
        bne     LC1AF
        .byte   $4F
        .byte   $D3
        .byte   $53
        eor     #$CE
        .byte   $54
        eor     ($CE,x)
        eor     (L0054,x)
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
        ldy     $47
        .byte   $CF
        brk
        .byte   $54
        .byte   $4F
        .byte   $4F
        jsr     L414D
        lsr     $2059
        lsr     $49
        jmp     LD345

        lsr     $49
        .byte   $4C
LC1AF:  eor     $20
        .byte   $4F
        bvc     LC1F9
        dec     L4946
        jmp     L2045

        lsr     $544F
        jsr     L504F
        eor     $CE
        lsr     $49
        jmp     L2045

        lsr     $544F
        jsr     L4F46
        eor     $4E,x
        cpy     $44
        eor     $56
        eor     #$43
        eor     $20
        lsr     $544F
        jsr     L5250
        eor     $53
        eor     $4E
        .byte   $D4
        lsr     $544F
        jsr     L4E49
        bvc     LC23F
        .byte   $54
        jsr     L4946
        jmp     L4EC5

        .byte   $4F
        .byte   $54
        jsr     L554F
        .byte   $54
        bvc     LC24E
LC1F9:  .byte   $54
        jsr     L4946
        jmp     L4DC5

        eor     #$53
        .byte   $53
        eor     #$4E
        .byte   $47
        jsr     L4946
        jmp     L2045

        lsr     $4D41
        cmp     $49
        jmp     L454C

        .byte   $47
        eor     ($4C,x)
        jsr     L4544
        lsr     $49,x
        .byte   $43
        eor     $20
        lsr     $4D55
        .byte   $42
        eor     $D2
        lsr     $5845
        .byte   $54
        jsr     L4957
        .byte   $54
        pha
        .byte   $4F
        eor     L0054,x
        jsr     L4F46
        .byte   $D2
        .byte   $53
        eor     $544E,y
        eor     ($D8,x)
        .byte   $52
        eor     L0054
        .byte   $55
LC23F:  .byte   $52
        lsr     $5720
        eor     #$54
        pha
        .byte   $4F
        eor     L0054,x
        jsr     L4F47
        .byte   $53
        .byte   $55
LC24E:  .byte   $C2
        .byte   $4F
        eor     L0054,x
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
        cmp     $564F,y
        eor     $52
        lsr     $4C
        .byte   $4F
        .byte   $D7
        .byte   $4F
        eor     L0054,x
        jsr     L464F
        jsr     L454D
        eor     $524F
        cmp     $4E55,y
        .byte   $44
        eor     $46
        .byte   $27
        .byte   $44
        jsr     L5453
        eor     (L0054,x)
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
        lsr     $46C7
        eor     #$4C
        eor     $20
        .byte   $44
        eor     (L0054,x)
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
LC30E:  eor     $4E,x
        .byte   $44
        eor     $46
        .byte   $27
        .byte   $44
        jsr     L5546
        lsr     $5443
        eor     #$4F
        dec     $4556
        .byte   $52
        eor     #$46
        cmp     L4F4C,y
LC326:  .byte   $41
LC327:  cpy     $9E
        cmp     ($AC,x)
        cmp     ($B5,x)
        cmp     ($C2,x)
        cmp     ($D0,x)
        cmp     ($E2,x)
        cmp     ($F0,x)
        cmp     ($FF,x)
        cmp     ($10,x)
        .byte   $C2
        and     $C2
        and     $C2,x
        .byte   $3B
        .byte   $C2
        .byte   $4F
        .byte   $C2
        .byte   $5A
        .byte   $C2
        ror     a
        .byte   $C2
        .byte   $72
        .byte   $C2
        .byte   $7F
        .byte   $C2
        bcc     LC30E
        sta     $AAC2,x
        .byte   $C2
        tsx
        .byte   $C2
        iny
        .byte   $C2
        cmp     $C2,x
        cpx     $C2
        sbc     a:$C2
        .byte   $C3
        asl     $1EC3
        .byte   $C3
        bit     $C3
        .byte   $83
        .byte   $C3
        ora     $4B4F
        ora     L0D00
        jsr     L5245
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
        .byte   $A0
LC38A:  tsx
        inx
        inx
        inx
        inx
LC38F:  lda     $0101,x
        cmp     #$81
        bne     LC3B7
        lda     $4A
        bne     LC3A4
        lda     $0102,x
        sta     $49
        lda     $0103,x
        sta     $4A
LC3A4:  cmp     $0103,x
        bne     LC3B0
        lda     $49
        cmp     $0102,x
        beq     LC3B7
LC3B0:  txa
        clc
        adc     #$12
        tax
        bne     LC38F
LC3B7:  rts

LC3B8:  jsr     LC408
        sta     $31
        sty     $32
LC3BF:  sec
        lda     $5A
        sbc     $5F
        sta     L0022
        tay
        lda     $5B
        sbc     $60
        tax
        inx
        tya
        beq     LC3F3
        lda     $5A
        sec
        sbc     L0022
        sta     $5A
        bcs     LC3DC
        dec     $5B
        sec
LC3DC:  lda     $58
        sbc     L0022
        sta     $58
        bcs     LC3EC
        dec     $59
        bcc     LC3EC
LC3E8:  lda     ($5A),y
        sta     ($58),y
LC3EC:  dey
        bne     LC3E8
        lda     ($5A),y
        sta     ($58),y
LC3F3:  dec     $5B
        dec     $59
        dex
        bne     LC3EC
        rts

LC3FB:  asl     a
        adc     #$3E
        bcs     LC435
        sta     L0022
        tsx
        cpx     L0022
        bcc     LC435
        rts

LC408:  cpy     $34
        bcc     LC434
        bne     LC412
        cmp     $33
        bcc     LC434
LC412:  pha
        ldx     #$09
        tya
LC416:  pha
        lda     $57,x
        dex
        bpl     LC416
        jsr     LD526
        ldx     #$F7
LC421:  pla
        sta     $61,x
        inx
        bmi     LC421
        pla
        tay
        pla
        cpy     $34
        bcc     LC434
        bne     LC435
        cmp     $33
        bcs     LC435
LC434:  rts

LC435:  ldx     #$10
LC437:  jmp     (L0300)

        txa
        asl     a
        tax
        lda     LC326,x
        sta     L0022
        lda     LC327,x
        sta     $23
        jsr     LFFCC
        lda     #$00
        sta     $13
        jsr     LCAD7
        jsr     LCB45
        ldy     #$00
LC456:  lda     (L0022),y
        pha
        and     #$7F
        jsr     LCB47
        iny
        pla
        bpl     LC456
        jsr     LC67A
        lda     #$69
        ldy     #$C3
LC469:  jsr     LCB1E
        ldy     $3A
        iny
        beq     LC474
        jsr     LDDC2
LC474:  lda     #$76
        ldy     #$C3
        jsr     LCB1E
        lda     #$80
        jsr     LFF90
LC480:  jmp     (L0302)

        jsr     LC560
        stx     $7A
        sty     $7B
        jsr     L0073
        tax
        beq     LC480
        ldx     #$FF
        stx     $3A
        bcc     LC49C
        jsr     LC579
        jmp     LC7E1

LC49C:  jsr     LC96B
        jsr     LC579
        sty     $0B
        jsr     LC613
        bcc     LC4ED
        ldy     #$01
        lda     ($5F),y
        sta     $23
        lda     $2D
        sta     L0022
        lda     $60
        sta     $25
        lda     $5F
        dey
        sbc     ($5F),y
        clc
        adc     $2D
        sta     $2D
        sta     $24
        lda     $2E
        adc     #$FF
        sta     $2E
        sbc     $60
        tax
        sec
        lda     $5F
        sbc     $2D
        tay
        bcs     LC4D7
        inx
        dec     $25
LC4D7:  clc
        adc     L0022
        bcc     LC4DF
        dec     $23
        clc
LC4DF:  lda     (L0022),y
        sta     ($24),y
        iny
        bne     LC4DF
        inc     $23
        inc     $25
        dex
        bne     LC4DF
LC4ED:  jsr     LC659
        jsr     LC533
        lda     $0200
        beq     LC480
        clc
        lda     $2D
        sta     $5A
        adc     $0B
        sta     $58
        ldy     $2E
        sty     $5B
        bcc     LC508
        iny
LC508:  sty     $59
        jsr     LC3B8
        lda     $14
        ldy     $15
        sta     $01FE
        sty     $01FF
        lda     $31
        ldy     $32
        sta     $2D
        sty     $2E
        ldy     $0B
        dey
LC522:  lda     $01FC,y
        sta     ($5F),y
        dey
        bpl     LC522
        jsr     LC659
        jsr     LC533
        jmp     LC480

LC533:  lda     $2B
        ldy     $2C
        sta     L0022
        sty     $23
        clc
LC53C:  ldy     #$01
        lda     (L0022),y
        beq     LC55F
        ldy     #$04
LC544:  iny
        lda     (L0022),y
        bne     LC544
        iny
        tya
        adc     L0022
        tax
        ldy     #$00
        sta     (L0022),y
        lda     $23
        .byte   $69
LC555:  brk
        iny
        sta     (L0022),y
        stx     L0022
        sta     $23
        bcc     LC53C
LC55F:  rts

LC560:  ldx     #$00
LC562:  jsr     LE10F
        cmp     #$0D
        beq     LC576
        sta     $0200,x
        inx
        cpx     #$59
        bcc     LC562
        ldx     #$17
        jmp     LC437

LC576:  jmp     LCACA

LC579:  jmp     (L0304)

        ldx     $7A
        ldy     #$04
        sty     $0F
LC582:  lda     $0200,x
        bpl     LC58E
        cmp     #$FF
        beq     LC5C9
        inx
        bne     LC582
LC58E:  cmp     #$20
        beq     LC5C9
        sta     $08
        cmp     #$22
        beq     LC5EE
        bit     $0F
        bvs     LC5C9
        cmp     #$3F
        bne     LC5A4
        lda     #$99
        bne     LC5C9
LC5A4:  cmp     #$30
        bcc     LC5AC
        cmp     #$3C
        bcc     LC5C9
LC5AC:  sty     $71
        ldy     #$00
        sty     $0B
        dey
        stx     $7A
        dex
LC5B6:  iny
        inx
LC5B8:  lda     $0200,x
        sec
        sbc     LC09E,y
        beq     LC5B6
        cmp     #$80
        bne     LC5F5
        ora     $0B
LC5C7:  ldy     $71
LC5C9:  inx
        iny
        sta     $01FB,y
        lda     $01FB,y
        beq     LC609
        sec
        sbc     #$3A
        beq     LC5DC
        cmp     #$49
        bne     LC5DE
LC5DC:  sta     $0F
LC5DE:  sec
        sbc     #$55
        bne     LC582
        sta     $08
LC5E5:  lda     $0200,x
        beq     LC5C9
        cmp     $08
        beq     LC5C9
LC5EE:  iny
        sta     $01FB,y
        inx
        bne     LC5E5
LC5F5:  ldx     $7A
        inc     $0B
LC5F9:  iny
        lda     LC09D,y
        bpl     LC5F9
        lda     LC09E,y
        bne     LC5B8
        lda     $0200,x
        bpl     LC5C7
LC609:  sta     $01FD,y
        dec     $7B
        lda     #$FF
        sta     $7A
        rts

LC613:  lda     $2B
        ldx     $2C
LC617:  ldy     #$01
        sta     $5F
        stx     $60
        lda     ($5F),y
        beq     LC640
        iny
        iny
        lda     $15
        cmp     ($5F),y
        bcc     LC641
        beq     LC62E
        dey
        bne     LC637
LC62E:  lda     $14
        dey
        cmp     ($5F),y
        bcc     LC641
        beq     LC641
LC637:  dey
        lda     ($5F),y
        tax
        dey
        lda     ($5F),y
        bcs     LC617
LC640:  clc
LC641:  rts

        bne     LC641
        lda     #$00
        tay
        sta     ($2B),y
        iny
        sta     ($2B),y
        lda     $2B
        clc
        adc     #$02
        sta     $2D
        lda     $2C
        adc     #$00
        sta     $2E
LC659:  jsr     LC68E
        lda     #$00
        bne     LC68D
LC660:  jsr     LFFE7
        lda     $37
        ldy     $38
        sta     $33
        sty     $34
        lda     $2D
        ldy     $2E
        sta     $2F
        sty     $30
        sta     $31
        sty     $32
        jsr     LC81D
LC67A:  ldx     #$19
        stx     $16
        pla
        tay
        pla
        ldx     #$FA
        txs
        pha
        tya
        pha
        lda     #$00
        sta     $3E
        sta     $10
LC68D:  rts

LC68E:  clc
        lda     $2B
        adc     #$FF
        sta     $7A
        lda     $2C
        adc     #$FF
        sta     $7B
        rts

        bcc     LC6A4
        beq     LC6A4
        cmp     #$AB
        bne     LC68D
LC6A4:  jsr     LC96B
        jsr     LC613
        jsr     L0079
        beq     LC6BB
        cmp     #$AB
        bne     LC641
        jsr     L0073
        jsr     LC96B
        bne     LC641
LC6BB:  pla
        pla
        lda     $14
        ora     $15
        bne     LC6C9
        lda     #$FF
        sta     $14
        sta     $15
LC6C9:  ldy     #$01
        sty     $0F
        lda     ($5F),y
        beq     LC714
        jsr     LC82C
        jsr     LCAD7
        iny
        lda     ($5F),y
        tax
        iny
        lda     ($5F),y
        cmp     $15
        bne     LC6E6
        cpx     $14
        beq     LC6E8
LC6E6:  bcs     LC714
LC6E8:  sty     $49
        jsr     LDDCD
        lda     #$20
LC6EF:  ldy     $49
        and     #$7F
LC6F3:  jsr     LCB47
        cmp     #$22
        bne     LC700
        lda     $0F
        eor     #$FF
        sta     $0F
LC700:  iny
        beq     LC714
        lda     ($5F),y
        bne     LC717
        tay
        lda     ($5F),y
        tax
        iny
        lda     ($5F),y
        stx     $5F
        sta     $60
        bne     LC6C9
LC714:  jmp     LC474

LC717:  jmp     (L0306)

        bpl     LC6F3
        cmp     #$FF
        beq     LC6F3
        bit     $0F
        bmi     LC6F3
        sec
        sbc     #$7F
        tax
        sty     $49
        ldy     #$FF
LC72C:  dex
        beq     LC737
LC72F:  iny
        lda     LC09E,y
        bpl     LC72F
        bmi     LC72C
LC737:  iny
        lda     LC09E,y
        bmi     LC6EF
        jsr     LCB47
        bne     LC737
        lda     #$80
        sta     $10
        jsr     LC9A5
        jsr     LC38A
        bne     LC753
        txa
        adc     #$0F
        tax
        txs
LC753:  pla
        pla
        lda     #$09
        jsr     LC3FB
        jsr     LC906
        clc
        tya
        adc     $7A
        pha
        lda     $7B
        adc     #$00
        pha
        lda     $3A
        pha
        lda     $39
        pha
        lda     #$A4
        jsr     LCEFF
        jsr     LCD8D
        jsr     LCD8A
        lda     $66
        ora     #$7F
        and     $62
        sta     $62
        lda     #$8B
        ldy     #$C7
        sta     L0022
        sty     $23
        jmp     LCE43

        lda     #$BC
        ldy     #$D9
        jsr     LDBA2
        jsr     L0079
        cmp     #$A9
        bne     LC79F
        jsr     L0073
        jsr     LCD8A
LC79F:  jsr     LDC2B
        jsr     LCE38
        lda     $4A
        pha
        lda     $49
        pha
        lda     #$81
        pha
LC7AE:  jsr     LC82C
        lda     $7A
        ldy     $7B
        cpy     #$02
        nop
        beq     LC7BE
        sta     $3D
        sty     $3E
LC7BE:  ldy     #$00
        lda     ($7A),y
        bne     LC807
        ldy     #$02
        lda     ($7A),y
        clc
        bne     LC7CE
        jmp     LC84B

LC7CE:  iny
        lda     ($7A),y
        sta     $39
        iny
        lda     ($7A),y
        sta     $3A
        tya
        adc     $7A
        sta     $7A
        bcc     LC7E1
        inc     $7B
LC7E1:  jmp     (L0308)

        jsr     L0073
        jsr     LC7ED
        jmp     LC7AE

LC7ED:  beq     LC82B
LC7EF:  sbc     #$80
        bcc     LC804
        cmp     #$23
        bcs     LC80E
        asl     a
        tay
        lda     LC00D,y
        pha
        lda     LC00C,y
        pha
        jmp     L0073

LC804:  jmp     LC9A5

LC807:  cmp     #$3A
        beq     LC7E1
LC80B:  jmp     LCF08

LC80E:  cmp     #$4B
        bne     LC80B
        jsr     L0073
        lda     #$A4
        jsr     LCEFF
        jmp     LC8A0

LC81D:  sec
        lda     $2B
        sbc     #$01
        ldy     $2C
        bcs     LC827
        dey
LC827:  sta     $41
        sty     $42
LC82B:  rts

LC82C:  jsr     LFFE1
        bcs     LC832
        clc
LC832:  bne     LC870
        lda     $7A
        ldy     $7B
        ldx     $3A
        inx
        beq     LC849
        sta     $3D
        sty     $3E
        lda     $39
        ldy     $3A
        sta     $3B
        sty     $3C
LC849:  pla
        pla
LC84B:  lda     #$81
        ldy     #$C3
        bcc     LC854
        jmp     LC469

LC854:  jmp     LC474

        bne     LC870
        ldx     #$1A
        ldy     $3E
        bne     LC862
        jmp     LC437

LC862:  lda     $3D
        sta     $7A
        sty     $7B
        lda     $3B
        ldy     $3C
        sta     $39
        sty     $3A
LC870:  rts

        php
        lda     #$00
        jsr     LFF90
        plp
        bne     LC87D
        jmp     LC659

LC87D:  jsr     LC660
        jmp     LC897

        lda     #$03
        jsr     LC3FB
        lda     $7B
        pha
        lda     $7A
        pha
        lda     $3A
        pha
        lda     $39
        pha
        lda     #$8D
        pha
LC897:  jsr     L0079
        jsr     LC8A0
        jmp     LC7AE

LC8A0:  jsr     LC96B
        jsr     LC909
        sec
        lda     $39
        sbc     $14
        lda     $3A
        sbc     $15
        bcs     LC8BC
        tya
        sec
        adc     $7A
        ldx     $7B
        bcc     LC8C0
        inx
        bcs     LC8C0
LC8BC:  lda     $2B
        ldx     $2C
LC8C0:  jsr     LC617
        bcc     LC8E3
        lda     $5F
        sbc     #$01
        sta     $7A
        lda     $60
        sbc     #$00
        sta     $7B
LC8D1:  rts

        bne     LC8D1
        lda     #$FF
        sta     $4A
        jsr     LC38A
        txs
        cmp     #$8D
        beq     LC8EB
        ldx     #$0C
        .byte   $2C
LC8E3:  ldx     #$11
        jmp     LC437

LC8E8:  jmp     LCF08

LC8EB:  pla
        pla
        sta     $39
        pla
        sta     $3A
        pla
        sta     $7A
        pla
        sta     $7B
LC8F8:  jsr     LC906
LC8FB:  tya
        clc
        adc     $7A
        sta     $7A
        bcc     LC905
        inc     $7B
LC905:  rts

LC906:  ldx     #$3A
        .byte   $2C
LC909:  ldx     #$00
        stx     $07
        ldy     #$00
        sty     $08
LC911:  lda     $08
        ldx     $07
        sta     $07
        stx     $08
LC919:  lda     ($7A),y
        beq     LC905
        cmp     $08
        beq     LC905
        iny
        cmp     #$22
        bne     LC919
        beq     LC911
        jsr     LCD9E
        jsr     L0079
        cmp     #$89
        beq     LC937
        lda     #$A7
        jsr     LCEFF
LC937:  lda     $61
        bne     LC940
        jsr     LC909
        beq     LC8FB
LC940:  jsr     L0079
        bcs     LC948
        jmp     LC8A0

LC948:  jmp     LC7ED

        jsr     LD79E
        pha
        cmp     #$8D
        beq     LC957
LC953:  cmp     #$89
        bne     LC8E8
LC957:  dec     $65
        bne     LC95F
        pla
        jmp     LC7EF

LC95F:  jsr     L0073
        jsr     LC96B
        cmp     #$2C
        beq     LC957
        pla
LC96A:  rts

LC96B:  ldx     #$00
        stx     $14
        stx     $15
        bcs     LC96A
        sbc     #$2F
        sta     $07
        lda     $15
        sta     L0022
        cmp     #$19
        bcs     LC953
        lda     $14
        asl     a
        rol     L0022
        asl     a
        rol     L0022
        adc     $14
        sta     $14
        lda     L0022
        adc     $15
        sta     $15
        asl     $14
        rol     $15
        lda     $14
        adc     $07
        sta     $14
        bcc     LC99F
        inc     $15
LC99F:  jsr     L0073
        .byte   $4C
        .byte   $71
LC9A4:  .byte   $C9
LC9A5:  jsr     LD08B
        sta     $49
        sty     $4A
        lda     #$B2
        jsr     LCEFF
        lda     $0E
        pha
        lda     $0D
        pha
        jsr     LCD9E
        pla
        rol     a
        jsr     LCD90
        bne     LC9D9
        pla
LC9C2:  bpl     LC9D6
        jsr     LDC1B
        jsr     LD1BF
        ldy     #$00
        lda     $64
        sta     ($49),y
        iny
        lda     $65
        sta     ($49),y
        rts

LC9D6:  jmp     LDBD0

LC9D9:  pla
LC9DA:  ldy     $4A
        cpy     #$DF
        bne     LCA2C
        jsr     LD6A6
        cmp     #$06
        bne     LCA24
        ldy     #$00
        sty     $61
        sty     $66
LC9ED:  sty     $71
        jsr     LCA1D
        jsr     LDAE2
        inc     $71
        ldy     $71
        jsr     LCA1D
        jsr     LDC0C
        tax
        beq     LCA07
        inx
        txa
        jsr     LDAED
LCA07:  ldy     $71
        iny
        cpy     #$06
        bne     LC9ED
        jsr     LDAE2
        jsr     LDC9B
        ldx     $64
        ldy     $63
        lda     $65
        jmp     LFFDB

LCA1D:  lda     (L0022),y
        jsr     L0080
        bcc     LCA27
LCA24:  jmp     LD248

LCA27:  sbc     #$2F
        jmp     LDD7E

LCA2C:  ldy     #$02
        lda     ($64),y
        cmp     $34
        bcc     LCA4B
        bne     LCA3D
        dey
        lda     ($64),y
        cmp     $33
        bcc     LCA4B
LCA3D:  ldy     $65
        cpy     $2E
        bcc     LCA4B
        bne     LCA52
        lda     $64
        cmp     $2D
        bcs     LCA52
LCA4B:  lda     $64
        ldy     $65
        jmp     LCA68

LCA52:  ldy     #$00
        lda     ($64),y
        jsr     LD475
        lda     $50
        ldy     $51
        sta     $6F
        sty     $70
        jsr     LD67A
        lda     #$61
        ldy     #$00
LCA68:  sta     $50
        sty     $51
        jsr     LD6DB
        ldy     #$00
        lda     ($50),y
        sta     ($49),y
        iny
        lda     ($50),y
        sta     ($49),y
        iny
        lda     ($50),y
        sta     ($49),y
        rts

        jsr     LCA86
        jmp     LCBB5

LCA86:  jsr     LD79E
        beq     LCA90
        lda     #$2C
        jsr     LCEFF
LCA90:  php
        stx     $13
        jsr     LE115
        plp
        jmp     LCAA0

LCA9A:  jsr     LCB21
LCA9D:  jsr     L0079
LCAA0:  beq     LCAD7
LCAA2:  beq     LCAE7
        cmp     #$A3
        beq     LCAF8
        cmp     #$A6
        clc
        beq     LCAF8
        cmp     #$2C
        beq     LCAE8
        cmp     #$3B
        beq     LCB13
        jsr     LCD9E
        bit     $0D
        bmi     LCA9A
        jsr     LDDDD
        jsr     LD487
        jsr     LCB21
        jsr     LCB3B
        bne     LCA9D
LCACA:  lda     #$00
        sta     $0200,x
        ldx     #$FF
        ldy     #$01
        lda     $13
        bne     LCAE7
LCAD7:  lda     #$0D
        jsr     LCB47
        bit     $13
        bpl     LCAE5
        lda     #$0A
        jsr     LCB47
LCAE5:  eor     #$FF
LCAE7:  rts

LCAE8:  sec
        jsr     LFFF0
        tya
        sec
LCAEE:  sbc     #$0B
        bcs     LCAEE
        eor     #$FF
        adc     #$01
        bne     LCB0E
LCAF8:  php
        sec
        jsr     LFFF0
        sty     $09
        jsr     LD79B
        cmp     #$29
        bne     LCB5F
        plp
        bcc     LCB0F
        txa
        sbc     $09
        bcc     LCB13
LCB0E:  tax
LCB0F:  inx
LCB10:  dex
        bne     LCB19
LCB13:  jsr     L0073
        jmp     LCAA2

LCB19:  jsr     LCB3B
        bne     LCB10
LCB1E:  jsr     LD487
LCB21:  jsr     LD6A6
        tax
        ldy     #$00
        inx
LCB28:  dex
        beq     LCAE7
        lda     (L0022),y
        jsr     LCB47
        iny
        cmp     #$0D
        bne     LCB28
        jsr     LCAE5
        jmp     LCB28

LCB3B:  lda     $13
        beq     LCB42
        lda     #$20
        .byte   $2C
LCB42:  lda     #$1D
        .byte   $2C
LCB45:  lda     #$3F
LCB47:  jsr     LE109
        and     #$FF
        rts

LCB4D:  lda     $11
        beq     LCB62
        bmi     LCB57
        ldy     #$FF
        .byte   $D0
LCB56:  .byte   $04
LCB57:  lda     $3F
        ldy     $40
LCB5B:  sta     $39
        sty     $3A
LCB5F:  jmp     LCF08

LCB62:  lda     $13
        beq     LCB6B
        ldx     #$18
        jmp     LC437

LCB6B:  lda     #$0C
        ldy     #$CD
        jsr     LCB1E
        lda     $3D
        ldy     $3E
        sta     $7A
        sty     $7B
        rts

        jsr     LD3A6
        cmp     #$23
        bne     LCB92
        jsr     L0073
        jsr     LD79E
        lda     #$2C
        jsr     LCEFF
        stx     $13
        jsr     LE11B
LCB92:  ldx     #$01
        ldy     #$02
        lda     #$00
        sta     $0201
        lda     #$40
        jsr     LCC0F
        ldx     $13
        bne     LCBB7
        rts

        jsr     LD79E
        lda     #$2C
        jsr     LCEFF
        stx     $13
        jsr     LE11B
        jsr     LCBCE
LCBB5:  lda     $13
LCBB7:  jsr     LFFCC
        ldx     #$00
        stx     $13
        rts

        cmp     #$22
        bne     LCBCE
        jsr     LCEBD
        lda     #$3B
        jsr     LCEFF
        jsr     LCB21
LCBCE:  jsr     LD3A6
        lda     #$2C
        sta     $01FF
LCBD6:  jsr     LCBF9
        lda     $13
        beq     LCBEA
        jsr     LFFB7
        and     #$02
        beq     LCBEA
        jsr     LCBB5
        jmp     LC8F8

LCBEA:  lda     $0200
        bne     LCC0D
        lda     $13
        bne     LCBD6
        jsr     LC906
        jmp     LC8FB

LCBF9:  lda     $13
        bne     LCC03
        jsr     LCB45
        jsr     LCB3B
LCC03:  jmp     LC560

        ldx     $41
        ldy     $42
        lda     #$98
        .byte   $2C
LCC0D:  lda     #$00
LCC0F:  sta     $11
        stx     $43
        sty     $44
LCC15:  jsr     LD08B
        sta     $49
        sty     $4A
        lda     $7A
        ldy     $7B
        sta     $4B
        sty     $4C
        ldx     $43
        ldy     $44
        stx     $7A
        sty     $7B
        jsr     L0079
        bne     LCC51
        bit     $11
        bvc     LCC41
        jsr     LE121
        sta     $0200
        ldx     #$FF
        ldy     #$01
        bne     LCC4D
LCC41:  bmi     LCCB8
        lda     $13
        bne     LCC4A
        jsr     LCB45
LCC4A:  jsr     LCBF9
LCC4D:  stx     $7A
        sty     $7B
LCC51:  jsr     L0073
        bit     $0D
        bpl     LCC89
        bit     $11
        bvc     LCC65
        inx
        stx     $7A
        lda     #$00
        sta     $07
        beq     LCC71
LCC65:  sta     $07
        cmp     #$22
        beq     LCC72
        lda     #$3A
        sta     $07
        lda     #$2C
LCC71:  clc
LCC72:  sta     $08
        lda     $7A
        ldy     $7B
        adc     #$00
        bcc     LCC7D
        iny
LCC7D:  jsr     LD48D
        jsr     LD7E2
        jsr     LC9DA
        jmp     LCC91

LCC89:  jsr     LDCF3
        lda     $0E
        jsr     LC9C2
LCC91:  jsr     L0079
        beq     LCC9D
        cmp     #$2C
        beq     LCC9D
        jmp     LCB4D

LCC9D:  lda     $7A
        ldy     $7B
        sta     $43
        sty     $44
        lda     $4B
        ldy     $4C
        sta     $7A
        sty     $7B
        jsr     L0079
        beq     LCCDF
        jsr     LCEFD
        jmp     LCC15

LCCB8:  jsr     LC906
        iny
        tax
        bne     LCCD1
        ldx     #$0D
        iny
        lda     ($7A),y
        beq     LCD32
        iny
        lda     ($7A),y
        sta     $3F
        iny
        lda     ($7A),y
        iny
        sta     $40
LCCD1:  jsr     LC8FB
        jsr     L0079
        tax
        cpx     #$83
        bne     LCCB8
LCCDC:  jmp     LCC51

LCCDF:  lda     $43
        ldy     $44
        ldx     $11
        bpl     LCCEA
        jmp     LC827

LCCEA:  ldy     #$00
        lda     ($43),y
        beq     LCCFB
        lda     $13
        bne     LCCFB
        lda     #$FC
        ldy     #$CC
        jmp     LCB1E

LCCFB:  rts

        .byte   $3F
        eor     $58
        .byte   $54
        .byte   $52
        eor     ($20,x)
        eor     #$47
        lsr     $524F
        eor     $44
        ora     $3F00
        .byte   $52
        eor     $44
        .byte   $4F
        jsr     L5246
        .byte   $4F
        eor     $5320
        .byte   $54
        eor     ($52,x)
        .byte   $54
        ora     LD000
        .byte   $04
        ldy     #$00
        beq     LCD27
LCD24:  jsr     LD08B
LCD27:  sta     $49
        sty     $4A
        jsr     LC38A
        beq     LCD35
        ldx     #$0A
LCD32:  jmp     LC437

LCD35:  txs
        txa
        clc
        adc     #$04
        pha
        adc     #$06
        sta     $24
        pla
        ldy     #$01
        jsr     LDBA2
        tsx
        lda     $0109,x
        sta     $66
        lda     $49
        ldy     $4A
        jsr     LD867
        jsr     LDBD0
        ldy     #$01
        jsr     LDC5D
        tsx
        sec
        sbc     $0109,x
        beq     LCD78
        lda     $010F,x
        sta     $39
        lda     $0110,x
        sta     $3A
        lda     $0112,x
        sta     $7A
        lda     $0111,x
        sta     $7B
LCD75:  jmp     LC7AE

LCD78:  txa
        adc     #$11
        tax
        txs
        jsr     L0079
        cmp     #$2C
        bne     LCD75
        jsr     L0073
        jsr     LCD24
LCD8A:  jsr     LCD9E
LCD8D:  clc
        .byte   $24
LCD8F:  sec
LCD90:  bit     $0D
        bmi     LCD97
        bcs     LCD99
LCD96:  rts

LCD97:  bcs     LCD96
LCD99:  ldx     #$16
        jmp     LC437

LCD9E:  ldx     $7A
        bne     LCDA4
        dec     $7B
LCDA4:  dec     $7A
        ldx     #$00
        .byte   $24
LCDA9:  pha
        txa
        pha
        lda     #$01
        jsr     LC3FB
        jsr     LCE83
        lda     #$00
        sta     $4D
LCDB8:  jsr     L0079
LCDBB:  sec
        sbc     #$B1
        bcc     LCDD7
        cmp     #$03
        bcs     LCDD7
        cmp     #$01
        rol     a
        eor     #$01
        eor     $4D
        cmp     $4D
        bcc     LCE30
        sta     $4D
        jsr     L0073
        jmp     LCDBB

LCDD7:  ldx     $4D
        bne     LCE07
        bcs     LCE58
        adc     #$07
        bcc     LCE58
        adc     $0D
        bne     LCDE8
        jmp     LD63D

LCDE8:  adc     #$FF
        sta     L0022
        asl     a
        adc     L0022
        tay
LCDF0:  pla
        cmp     LC080,y
        bcs     LCE5D
        jsr     LCD8D
LCDF9:  pha
LCDFA:  jsr     LCE20
        pla
        ldy     $4B
        bpl     LCE19
        tax
        beq     LCE5B
        bne     LCE66
LCE07:  lsr     $0D
        txa
        rol     a
        ldx     $7A
        bne     LCE11
        dec     $7B
LCE11:  dec     $7A
        ldy     #$1B
        sta     $4D
        bne     LCDF0
LCE19:  cmp     LC080,y
        bcs     LCE66
        bcc     LCDF9
LCE20:  lda     LC082,y
        pha
        lda     LC081,y
        pha
        jsr     LCE33
        lda     $4D
        jmp     LCDA9

LCE30:  jmp     LCF08

LCE33:  lda     $66
        ldx     LC080,y
LCE38:  tay
        pla
        sta     L0022
        inc     L0022
        pla
        sta     $23
        tya
        pha
LCE43:  .byte   $20
        .byte   $1B
LCE45:  .byte   $DC
        lda     $65
        pha
        lda     $64
        pha
        lda     $63
        pha
        lda     $62
        pha
        lda     $61
        pha
        jmp     (L0022)

LCE58:  ldy     #$FF
        pla
LCE5B:  beq     LCE80
LCE5D:  cmp     #$64
        beq     LCE64
        jsr     LCD8D
LCE64:  sty     $4B
LCE66:  pla
        lsr     a
        sta     $12
        pla
        sta     $69
        pla
        sta     $6A
        pla
        sta     $6B
        pla
        sta     $6C
        pla
        sta     $6D
        pla
        sta     $6E
        eor     $66
        sta     $6F
LCE80:  lda     $61
        rts

LCE83:  jmp     (L030A)

        lda     #$00
        sta     $0D
LCE8A:  jsr     L0073
        bcs     LCE92
LCE8F:  jmp     LDCF3

LCE92:  jsr     LD113
        bcc     LCE9A
        jmp     LCF28

LCE9A:  cmp     #$FF
        bne     LCEAD
        lda     #$A8
        ldy     #$CE
        jsr     LDBA2
        jmp     L0073

        .byte   $82
        eor     #$0F
        .byte   $DA
        .byte   $A1
LCEAD:  cmp     #$2E
        beq     LCE8F
        cmp     #$AB
        beq     LCF0D
        cmp     #$AA
        beq     LCE8A
        cmp     #$22
        bne     LCECC
LCEBD:  lda     $7A
        ldy     $7B
        adc     #$00
        bcc     LCEC6
        iny
LCEC6:  jsr     LD487
        jmp     LD7E2

LCECC:  cmp     #$A8
        bne     LCEE3
        ldy     #$18
        bne     LCF0F
        jsr     LD1BF
        lda     $65
        eor     #$FF
        tay
        lda     $64
        eor     #$FF
        jmp     LD391

LCEE3:  cmp     #$A5
        bne     LCEEA
        jmp     LD3F4

LCEEA:  cmp     #$B4
        bcc     LCEF1
        jmp     LCFA7

LCEF1:  jsr     LCEFA
        jsr     LCD9E
LCEF7:  lda     #$29
        .byte   $2C
LCEFA:  lda     #$28
        .byte   $2C
LCEFD:  lda     #$2C
LCEFF:  ldy     #$00
        cmp     ($7A),y
        bne     LCF08
        jmp     L0073

LCF08:  ldx     #$0B
        jmp     LC437

LCF0D:  ldy     #$15
LCF0F:  pla
        pla
        jmp     LCDFA

LCF14:  sec
        lda     $64
        sbc     #$00
        lda     $65
        sbc     #$C0
        bcc     LCF27
        lda     #$87
        sbc     $64
        lda     #$E3
        sbc     $65
LCF27:  rts

LCF28:  jsr     LD08B
        sta     $64
        sty     $65
        ldx     $45
        ldy     $46
        lda     $0D
        beq     LCF5D
        lda     #$00
        sta     $70
        jsr     LCF14
        bcc     LCF5C
        cpx     #$54
        bne     LCF5C
        cpy     #$C9
        bne     LCF5C
        jsr     LCF84
        sty     $5E
        dey
        sty     $71
        ldy     #$06
        sty     $5D
        ldy     #$24
        jsr     LDE68
        jmp     LD46F

LCF5C:  rts

LCF5D:  bit     $0E
        bpl     LCF6E
        ldy     #$00
        lda     ($64),y
        tax
        iny
        lda     ($64),y
        tay
        txa
        jmp     LD391

LCF6E:  jsr     LCF14
        bcc     LCFA0
        cpx     #$54
        bne     LCF92
        cpy     #$49
        bne     LCFA0
        jsr     LCF84
        tya
        ldx     #$A0
        jmp     LDC4F

LCF84:  jsr     LFFDE
        stx     $64
        sty     $63
        sta     $65
        ldy     #$00
        sty     $62
        rts

LCF92:  cpx     #$53
        bne     LCFA0
        cpy     #$54
        bne     LCFA0
        jsr     LFFB7
        jmp     LDC3C

LCFA0:  lda     $64
        ldy     $65
        jmp     LDBA2

LCFA7:  asl     a
        pha
        tax
        jsr     L0073
        cpx     #$8F
        bcc     LCFD1
        jsr     LCEFA
        jsr     LCD9E
        jsr     LCEFD
        jsr     LCD8F
        pla
        tax
        lda     $65
        pha
        lda     $64
        pha
        txa
        pha
        jsr     LD79E
        pla
        tay
        txa
        pha
        jmp     LCFD6

LCFD1:  jsr     LCEF1
        pla
        tay
LCFD6:  lda     LBFEA,y
        sta     $55
        lda     $BFEB,y
        sta     $56
        jsr     L0054
        jmp     LCD8D

        ldy     #$FF
        bit     a:$A0
        sty     $0B
        jsr     LD1BF
        lda     $64
        eor     $0B
        sta     $07
        lda     $65
        eor     $0B
        sta     $08
        jsr     LDBFC
        .byte   $20
LD000:  .byte   $BF
        cmp     ($A5),y
        adc     $45
        .byte   $0B
        and     $08
        eor     $0B
        tay
        lda     $64
        eor     $0B
        and     $07
        eor     $0B
        jmp     LD391

        jsr     LCD90
        bcs     LD02E
        lda     $6E
        ora     #$7F
        and     $6A
        sta     $6A
        lda     #$69
        ldy     #$00
        jsr     LDC5B
        tax
        jmp     LD061

LD02E:  lda     #$00
        sta     $0D
        dec     $4D
        jsr     LD6A6
        sta     $61
        stx     $62
        sty     $63
        lda     $6C
        ldy     $6D
        jsr     LD6AA
        stx     $6C
        sty     $6D
        tax
        sec
        sbc     $61
        beq     LD056
        lda     #$01
        bcc     LD056
        ldx     $61
        lda     #$FF
LD056:  sta     $66
        ldy     #$FF
        inx
LD05B:  iny
        dex
        bne     LD066
        ldx     $66
LD061:  bmi     LD072
        clc
        bcc     LD072
LD066:  lda     ($6C),y
        cmp     ($62),y
        beq     LD05B
        ldx     #$FF
        bcs     LD072
        ldx     #$01
LD072:  inx
        txa
        rol     a
        and     $12
        beq     LD07B
        lda     #$FF
LD07B:  jmp     LDC3C

LD07E:  jsr     LCEFD
        tax
        jsr     LD090
        jsr     L0079
        bne     LD07E
        rts

LD08B:  ldx     #$00
        jsr     L0079
LD090:  stx     $0C
LD092:  sta     $45
        jsr     L0079
        jsr     LD113
        bcs     LD09F
LD09C:  jmp     LCF08

LD09F:  ldx     #$00
        stx     $0D
        stx     $0E
        jsr     L0073
        bcc     LD0AF
        jsr     LD113
        bcc     LD0BA
LD0AF:  tax
LD0B0:  jsr     L0073
        bcc     LD0B0
        jsr     LD113
        bcs     LD0B0
LD0BA:  cmp     #$24
        bne     LD0C4
        lda     #$FF
        sta     $0D
        bne     LD0D4
LD0C4:  cmp     #$25
        bne     LD0DB
        lda     $10
        bne     LD09C
        lda     #$80
        sta     $0E
        ora     $45
        sta     $45
LD0D4:  txa
        ora     #$80
        tax
        jsr     L0073
LD0DB:  stx     $46
        sec
        ora     $10
        sbc     #$28
        bne     LD0E7
        jmp     LD1D1

LD0E7:  ldy     #$00
        sty     $10
        lda     $2D
        ldx     $2E
LD0EF:  stx     $60
LD0F1:  sta     $5F
        cpx     $30
        bne     LD0FB
        cmp     $2F
        beq     LD11D
LD0FB:  lda     $45
        cmp     ($5F),y
        bne     LD109
        lda     $46
        iny
        cmp     ($5F),y
        beq     LD185
        dey
LD109:  clc
        lda     $5F
        adc     #$07
        bcc     LD0F1
        inx
        bne     LD0EF
LD113:  cmp     #$41
        bcc     LD11C
        sbc     #$5B
        sec
        sbc     #$A5
LD11C:  rts

LD11D:  pla
        pha
        cmp     #$2A
        bne     LD128
LD123:  lda     #$13
        ldy     #$DF
LD127:  rts

LD128:  lda     $45
        ldy     $46
        cmp     #$54
        bne     LD13B
        cpy     #$C9
        beq     LD123
        cpy     #$49
        bne     LD13B
LD138:  jmp     LCF08

LD13B:  cmp     #$53
        bne     LD143
        cpy     #$54
        beq     LD138
LD143:  lda     $2F
        ldy     $30
        sta     $5F
        sty     $60
        lda     $31
        ldy     $32
        sta     $5A
        sty     $5B
        clc
        adc     #$07
        bcc     LD159
        iny
LD159:  sta     $58
        sty     $59
        jsr     LC3B8
        lda     $58
        ldy     $59
        iny
        sta     $2F
        sty     $30
        ldy     #$00
        lda     $45
        sta     ($5F),y
        iny
        lda     $46
        sta     ($5F),y
        lda     #$00
        iny
        sta     ($5F),y
        iny
        sta     ($5F),y
        iny
        sta     ($5F),y
        iny
        sta     ($5F),y
        iny
        sta     ($5F),y
LD185:  lda     $5F
        clc
        adc     #$02
        ldy     $60
        bcc     LD18F
        iny
LD18F:  sta     $47
        sty     $48
        rts

LD194:  lda     $0B
        asl     a
        adc     #$05
        adc     $5F
        ldy     $60
        bcc     LD1A0
        iny
LD1A0:  sta     $58
        sty     $59
        rts

        bcc     LD127
        brk
        brk
        brk
        jsr     LD1BF
        lda     $64
        ldy     $65
        rts

LD1B2:  jsr     L0073
        jsr     LCD9E
LD1B8:  jsr     LCD8D
        lda     $66
        bmi     LD1CC
LD1BF:  lda     $61
        cmp     #$90
        bcc     LD1CE
        lda     #$A5
        ldy     #$D1
        jsr     LDC5B
LD1CC:  bne     LD248
LD1CE:  jmp     LDC9B

LD1D1:  lda     $0C
        ora     $0E
        pha
        lda     $0D
        pha
        ldy     #$00
LD1DB:  tya
        pha
        lda     $46
        pha
        lda     $45
        pha
        jsr     LD1B2
        pla
        sta     $45
        pla
        sta     $46
        pla
        tay
        tsx
        lda     $0102,x
        pha
        lda     $0101,x
        pha
        lda     $64
        sta     $0102,x
        lda     $65
        sta     $0101,x
        iny
        jsr     L0079
        cmp     #$2C
        beq     LD1DB
        sty     $0B
        jsr     LCEF7
        pla
        sta     $0D
        pla
        sta     $0E
        and     #$7F
        sta     $0C
        ldx     $2F
        lda     $30
LD21C:  stx     $5F
        sta     $60
        cmp     $32
        bne     LD228
        cpx     $31
        beq     LD261
LD228:  ldy     #$00
        lda     ($5F),y
        iny
        cmp     $45
        bne     LD237
        lda     $46
        cmp     ($5F),y
        beq     LD24D
LD237:  iny
        lda     ($5F),y
        clc
        adc     $5F
        tax
        iny
        lda     ($5F),y
        adc     $60
        bcc     LD21C
LD245:  ldx     #$12
        .byte   $2C
LD248:  ldx     #$0E
LD24A:  jmp     LC437

LD24D:  ldx     #$13
        lda     $0C
        bne     LD24A
        jsr     LD194
        lda     $0B
        ldy     #$04
        cmp     ($5F),y
        bne     LD245
        jmp     LD2EA

LD261:  jsr     LD194
        jsr     LC408
        ldy     #$00
        sty     $72
        ldx     #$05
        lda     $45
        sta     ($5F),y
        bpl     LD274
        dex
LD274:  iny
        lda     $46
        sta     ($5F),y
        bpl     LD27D
        dex
        dex
LD27D:  stx     $71
        lda     $0B
        iny
        iny
        iny
        sta     ($5F),y
LD286:  ldx     #$0B
        lda     #$00
        bit     $0C
        bvc     LD296
        pla
        clc
        adc     #$01
        tax
        pla
        adc     #$00
LD296:  iny
        sta     ($5F),y
        iny
        txa
        sta     ($5F),y
        jsr     LD34C
        stx     $71
        sta     $72
        ldy     L0022
        dec     $0B
        bne     LD286
        adc     $59
        bcs     LD30B
        sta     $59
        tay
        txa
        adc     $58
        bcc     LD2B9
        iny
        beq     LD30B
LD2B9:  jsr     LC408
        sta     $31
        sty     $32
        lda     #$00
        inc     $72
        ldy     $71
        beq     LD2CD
LD2C8:  dey
        sta     ($58),y
        bne     LD2C8
LD2CD:  dec     $59
        dec     $72
        bne     LD2C8
        inc     $59
        sec
        lda     $31
        sbc     $5F
        ldy     #$02
        sta     ($5F),y
        lda     $32
        iny
        sbc     $60
        sta     ($5F),y
        lda     $0C
        bne     LD34B
        iny
LD2EA:  lda     ($5F),y
        sta     $0B
        lda     #$00
        sta     $71
LD2F2:  sta     $72
        iny
        pla
        tax
        sta     $64
        pla
        sta     $65
        cmp     ($5F),y
        bcc     LD30E
        bne     LD308
        iny
        txa
        cmp     ($5F),y
        bcc     LD30F
LD308:  jmp     LD245

LD30B:  jmp     LC435

LD30E:  iny
LD30F:  lda     $72
        ora     $71
        clc
        beq     LD320
        jsr     LD34C
        txa
        adc     $64
        tax
        tya
        ldy     L0022
LD320:  adc     $65
        stx     $71
        dec     $0B
        bne     LD2F2
        sta     $72
        ldx     #$05
        lda     $45
        bpl     LD331
        dex
LD331:  lda     $46
        bpl     LD337
        dex
        dex
LD337:  stx     $28
        lda     #$00
        jsr     LD355
        txa
        adc     $58
        sta     $47
        tya
        .byte   $65
LD345:  eor     $4885,y
        tay
        lda     $47
LD34B:  rts

LD34C:  sty     L0022
        lda     ($5F),y
        sta     $28
        dey
        lda     ($5F),y
LD355:  sta     $29
        lda     #$10
        sta     $5D
        ldx     #$00
        ldy     #$00
LD35F:  txa
        asl     a
        tax
        tya
        rol     a
        tay
        bcs     LD30B
        asl     $71
        rol     $72
        bcc     LD378
        clc
        txa
        adc     $28
        tax
        tya
        adc     $29
        tay
        bcs     LD30B
LD378:  dec     $5D
        bne     LD35F
        rts

        lda     $0D
        beq     LD384
        jsr     LD6A6
LD384:  jsr     LD526
        sec
        lda     $33
        sbc     $31
        tay
        lda     $34
        sbc     $32
LD391:  ldx     #$00
        stx     $0D
        sta     $62
        sty     $63
        ldx     #$90
        jmp     LDC44

        sec
        jsr     LFFF0
LD3A2:  lda     #$00
        beq     LD391
LD3A6:  ldx     $3A
        inx
        bne     LD34B
        ldx     #$15
        .byte   $2C
LD3AE:  ldx     #$1B
        jmp     LC437

        jsr     LD3E1
        jsr     LD3A6
        jsr     LCEFA
        lda     #$80
        sta     $10
        jsr     LD08B
        jsr     LCD8D
        jsr     LCEF7
        lda     #$B2
        jsr     LCEFF
        pha
        lda     $48
        pha
        lda     $47
        pha
        lda     $7B
        pha
        lda     $7A
        pha
        jsr     LC8F8
        jmp     LD44F

LD3E1:  lda     #$A5
        jsr     LCEFF
        ora     #$80
        sta     $10
        jsr     LD092
        sta     $4E
        sty     $4F
        jmp     LCD8D

LD3F4:  jsr     LD3E1
        lda     $4F
        pha
        lda     $4E
        pha
        jsr     LCEF1
        jsr     LCD8D
        pla
        sta     $4E
        pla
        sta     $4F
        ldy     #$02
        lda     ($4E),y
        sta     $47
        tax
        iny
        lda     ($4E),y
        beq     LD3AE
        sta     $48
        iny
LD418:  lda     ($47),y
        pha
        dey
        bpl     LD418
        ldy     $48
        jsr     LDBD4
        lda     $7B
        pha
        lda     $7A
        pha
        lda     ($4E),y
        sta     $7A
        iny
        lda     ($4E),y
        sta     $7B
        lda     $48
        pha
        lda     $47
        pha
        jsr     LCD8A
        pla
        sta     $4E
        pla
        sta     $4F
        jsr     L0079
        beq     LD449
        jmp     LCF08

LD449:  pla
        sta     $7A
        pla
        sta     $7B
LD44F:  ldy     #$00
        pla
        sta     ($4E),y
        pla
        iny
        sta     ($4E),y
        pla
        iny
        sta     ($4E),y
        pla
        iny
        sta     ($4E),y
        pla
        iny
        sta     ($4E),y
        rts

        jsr     LCD8D
        ldy     #$00
        jsr     LDDDF
        pla
        pla
LD46F:  lda     #$FF
        ldy     #$00
        beq     LD487
LD475:  ldx     $64
        ldy     $65
        stx     $50
        sty     $51
LD47D:  jsr     LD4F4
        stx     $62
        sty     $63
        sta     $61
        rts

LD487:  ldx     #$22
        stx     $07
        stx     $08
LD48D:  sta     $6F
        sty     $70
        sta     $62
        sty     $63
        ldy     #$FF
LD497:  iny
        lda     ($6F),y
        beq     LD4A8
        cmp     $07
        beq     LD4A4
        cmp     $08
        bne     LD497
LD4A4:  cmp     #$22
        beq     LD4A9
LD4A8:  clc
LD4A9:  sty     $61
        tya
        adc     $6F
        sta     $71
        ldx     $70
        bcc     LD4B5
        inx
LD4B5:  stx     $72
        lda     $70
        beq     LD4BF
        cmp     #$02
        bne     LD4CA
LD4BF:  tya
        jsr     LD475
        ldx     $6F
        ldy     $70
        jsr     LD688
LD4CA:  ldx     $16
        cpx     #$22
        bne     LD4D5
        ldx     #$19
LD4D2:  jmp     LC437

LD4D5:  lda     $61
        sta     $00,x
        lda     $62
        sta     $01,x
        lda     $63
        sta     $02,x
        ldy     #$00
        stx     $64
        sty     $65
        sty     $70
        dey
        sty     $0D
        stx     $17
        inx
        inx
        inx
        stx     $16
        rts

LD4F4:  lsr     $0F
LD4F6:  pha
        eor     #$FF
        sec
        adc     $33
        ldy     $34
        bcs     LD501
        dey
LD501:  cpy     $32
        bcc     LD516
        bne     LD50B
        cmp     $31
        bcc     LD516
LD50B:  sta     $33
        sty     $34
        sta     $35
        sty     $36
        tax
        pla
        rts

LD516:  ldx     #$10
        lda     $0F
        bmi     LD4D2
        jsr     LD526
        lda     #$80
        sta     $0F
        pla
        bne     LD4F6
LD526:  ldx     $37
        lda     $38
LD52A:  stx     $33
        sta     $34
        ldy     #$00
        sty     $4F
        sty     $4E
        lda     $31
        ldx     $32
        sta     $5F
        stx     $60
        lda     #$19
        ldx     #$00
        sta     L0022
        stx     $23
LD544:  cmp     $16
        beq     LD54D
        jsr     LD5C7
        beq     LD544
LD54D:  lda     #$07
        sta     $53
        lda     $2D
        ldx     $2E
        sta     L0022
        stx     $23
LD559:  cpx     $30
        bne     LD561
        cmp     $2F
        beq     LD566
LD561:  jsr     LD5BD
        beq     LD559
LD566:  sta     $58
        stx     $59
        lda     #$03
        sta     $53
LD56E:  lda     $58
        ldx     $59
LD572:  cpx     $32
        bne     LD57D
        cmp     $31
        bne     LD57D
        jmp     LD606

LD57D:  sta     L0022
        stx     $23
        ldy     #$00
        lda     (L0022),y
        tax
        iny
        lda     (L0022),y
        php
        iny
        lda     (L0022),y
        adc     $58
        sta     $58
        iny
        lda     (L0022),y
        adc     $59
        sta     $59
        plp
        bpl     LD56E
        txa
        bmi     LD56E
        iny
        lda     (L0022),y
        ldy     #$00
        asl     a
        adc     #$05
        adc     L0022
        sta     L0022
        bcc     LD5AE
        inc     $23
LD5AE:  ldx     $23
LD5B0:  cpx     $59
        bne     LD5B8
        cmp     $58
        beq     LD572
LD5B8:  jsr     LD5C7
        beq     LD5B0
LD5BD:  lda     (L0022),y
        bmi     LD5F6
        iny
        lda     (L0022),y
        bpl     LD5F6
        iny
LD5C7:  lda     (L0022),y
        beq     LD5F6
        iny
        lda     (L0022),y
        tax
        iny
        lda     (L0022),y
        cmp     $34
        bcc     LD5DC
        bne     LD5F6
        cpx     $33
        bcs     LD5F6
LD5DC:  cmp     $60
        bcc     LD5F6
        bne     LD5E6
        cpx     $5F
        bcc     LD5F6
LD5E6:  stx     $5F
        sta     $60
        lda     L0022
        ldx     $23
        sta     $4E
        stx     $4F
        lda     $53
        sta     $55
LD5F6:  lda     $53
        clc
        adc     L0022
        sta     L0022
        bcc     LD601
        inc     $23
LD601:  ldx     $23
        ldy     #$00
        rts

LD606:  lda     $4F
        ora     $4E
        beq     LD601
        lda     $55
        and     #$04
        lsr     a
        tay
        sta     $55
        lda     ($4E),y
        adc     $5F
        sta     $5A
        lda     $60
        adc     #$00
        sta     $5B
        lda     $33
        ldx     $34
        sta     $58
        stx     $59
        jsr     LC3BF
        ldy     $55
        iny
        lda     $58
        sta     ($4E),y
        tax
        inc     $59
        lda     $59
        iny
        sta     ($4E),y
        jmp     LD52A

LD63D:  lda     $65
        pha
        lda     $64
        pha
        jsr     LCE83
        jsr     LCD8F
        pla
        sta     $6F
        pla
        sta     $70
        ldy     #$00
        lda     ($6F),y
        clc
        adc     ($64),y
        bcc     LD65D
        ldx     #$17
        jmp     LC437

LD65D:  jsr     LD475
        jsr     LD67A
        lda     $50
        ldy     $51
        jsr     LD6AA
        jsr     LD68C
        lda     $6F
        ldy     $70
        jsr     LD6AA
        jsr     LD4CA
        jmp     LCDB8

LD67A:  ldy     #$00
        lda     ($6F),y
        pha
        iny
        lda     ($6F),y
        tax
        iny
        lda     ($6F),y
        tay
        pla
LD688:  stx     L0022
        sty     $23
LD68C:  tay
        beq     LD699
        pha
LD690:  dey
        lda     (L0022),y
        sta     ($35),y
        tya
        bne     LD690
        pla
LD699:  clc
        adc     $35
        sta     $35
        bcc     LD6A2
        inc     $36
LD6A2:  rts

LD6A3:  jsr     LCD8F
LD6A6:  lda     $64
        ldy     $65
LD6AA:  sta     L0022
        sty     $23
        jsr     LD6DB
        php
        ldy     #$00
        lda     (L0022),y
        pha
        iny
        lda     (L0022),y
        tax
        iny
        lda     (L0022),y
        tay
        pla
        plp
        bne     LD6D6
        cpy     $34
        bne     LD6D6
        cpx     $33
        bne     LD6D6
        pha
        clc
        adc     $33
        sta     $33
        bcc     LD6D5
        inc     $34
LD6D5:  pla
LD6D6:  stx     L0022
        sty     $23
        rts

LD6DB:  cpy     $18
        bne     LD6EB
        cmp     $17
        bne     LD6EB
        sta     $16
        sbc     #$03
        sta     $17
        ldy     #$00
LD6EB:  rts

        jsr     LD7A1
        txa
        pha
        lda     #$01
        jsr     LD47D
        pla
        ldy     #$00
        sta     ($62),y
        pla
        pla
        jmp     LD4CA

        jsr     LD761
        cmp     ($50),y
        tya
LD706:  bcc     LD70C
        lda     ($50),y
        tax
        tya
LD70C:  pha
LD70D:  txa
LD70E:  pha
        jsr     LD47D
        lda     $50
        ldy     $51
        jsr     LD6AA
        pla
        tay
        pla
        clc
        adc     L0022
        sta     L0022
        bcc     LD725
        inc     $23
LD725:  tya
        jsr     LD68C
        jmp     LD4CA

        jsr     LD761
        clc
        sbc     ($50),y
        eor     #$FF
        jmp     LD706

        lda     #$FF
        sta     $65
        jsr     L0079
        cmp     #$29
        beq     LD748
        jsr     LCEFD
LD745:  jsr     LD79E
LD748:  jsr     LD761
        beq     LD798
        dex
        txa
        pha
        clc
        ldx     #$00
        sbc     ($50),y
        bcs     LD70D
        eor     #$FF
        cmp     $65
        bcc     LD70E
        lda     $65
        bcs     LD70E
LD761:  jsr     LCEF7
        pla
        tay
        pla
        sta     $55
        pla
        pla
        pla
        tax
        pla
        sta     $50
        pla
        sta     $51
        lda     $55
        pha
        tya
        pha
        ldy     #$00
        txa
        rts

        jsr     LD782
        jmp     LD3A2

LD782:  jsr     LD6A3
        ldx     #$00
        stx     $0D
        tay
        rts

        jsr     LD782
        beq     LD798
        ldy     #$00
        lda     (L0022),y
        tay
        jmp     LD3A2

LD798:  jmp     LD248

LD79B:  jsr     L0073
LD79E:  jsr     LCD8A
LD7A1:  jsr     LD1B8
        ldx     $64
        bne     LD798
        ldx     $65
        jmp     L0079

        jsr     LD782
        bne     LD7B5
        jmp     LD8F7

LD7B5:  ldx     $7A
        ldy     $7B
        stx     $71
        sty     $72
        ldx     L0022
        stx     $7A
        clc
        adc     L0022
        sta     $24
        ldx     $23
        stx     $7B
        bcc     LD7CD
        inx
LD7CD:  stx     $25
        ldy     #$00
        lda     ($24),y
        pha
        tya
        sta     ($24),y
        jsr     L0079
        jsr     LDCF3
        pla
        ldy     #$00
        sta     ($24),y
LD7E2:  ldx     $71
        ldy     $72
        stx     $7A
        sty     $7B
        rts

LD7EB:  jsr     LCD8A
        jsr     LD7F7
LD7F1:  jsr     LCEFD
        jmp     LD79E

LD7F7:  lda     $66
        bmi     LD798
        lda     $61
        cmp     #$91
        bcs     LD798
        jsr     LDC9B
        lda     $64
        ldy     $65
        sty     $14
        sta     $15
        rts

        lda     $15
        pha
        lda     $14
        pha
        jsr     LD7F7
        ldy     #$00
        lda     ($14),y
        tay
        pla
        sta     $14
        pla
        sta     $15
        jmp     LD3A2

        jsr     LD7EB
        txa
        ldy     #$00
        sta     ($14),y
        rts

        jsr     LD7EB
        stx     $49
        ldx     #$00
        jsr     L0079
        beq     LD83C
        jsr     LD7F1
LD83C:  stx     $4A
        ldy     #$00
LD840:  lda     ($14),y
        eor     $4A
        and     $49
        beq     LD840
LD848:  rts

LD849:  lda     #$11
        ldy     #$DF
        jmp     LD867

LD850:  .byte   $20
        .byte   $8C
LD852:  .byte   $DA
        lda     $66
        eor     #$FF
        sta     $66
        eor     $6E
        sta     $6F
        lda     $61
        jmp     LD86A

LD862:  jsr     LD999
        bcc     LD8A3
LD867:  .byte   $20
        .byte   $8C
LD869:  .byte   $DA
LD86A:  bne     LD86F
        jmp     LDBFC

LD86F:  ldx     $70
        stx     $56
        ldx     #$69
        lda     $69
LD877:  tay
        beq     LD848
        sec
        sbc     $61
        beq     LD8A3
        bcc     LD893
        sty     $61
        ldy     $6E
        sty     $66
        eor     #$FF
        adc     #$00
        ldy     #$00
        sty     $56
        ldx     #$61
        bne     LD897
LD893:  ldy     #$00
        sty     $70
LD897:  cmp     #$F9
        bmi     LD862
        tay
        lda     $70
        lsr     $01,x
        jsr     LD9B0
LD8A3:  bit     $6F
        bpl     LD8FE
        ldy     #$61
        cpx     #$69
        beq     LD8AF
        ldy     #$69
LD8AF:  sec
        eor     #$FF
        adc     $56
        sta     $70
        lda     $04,y
        sbc     $04,x
        sta     $65
        lda     $03,y
        sbc     $03,x
        sta     $64
        lda     $02,y
        sbc     $02,x
        sta     $63
        lda     $01,y
        sbc     $01,x
        sta     $62
LD8D2:  bcs     LD8D7
        jsr     LD947
LD8D7:  ldy     #$00
        tya
        clc
LD8DB:  ldx     $62
        bne     LD929
        ldx     $63
        stx     $62
        ldx     $64
        stx     $63
        ldx     $65
        stx     $64
        ldx     $70
        stx     $65
        sty     $70
        adc     #$08
        cmp     #$20
        bne     LD8DB
LD8F7:  lda     #$00
LD8F9:  sta     $61
LD8FB:  sta     $66
        rts

LD8FE:  adc     $56
        sta     $70
        lda     $65
        adc     $6D
        sta     $65
        lda     $64
        adc     $6C
        sta     $64
        lda     $63
        adc     $6B
        sta     $63
        lda     $62
        adc     $6A
        sta     $62
        jmp     LD936

LD91D:  adc     #$01
        asl     $70
        rol     $65
        rol     $64
        rol     $63
        rol     $62
LD929:  bpl     LD91D
        sec
        sbc     $61
        bcs     LD8F7
        eor     #$FF
        adc     #$01
        sta     $61
LD936:  bcc     LD946
LD938:  inc     $61
        beq     LD97E
        ror     $62
        ror     $63
        ror     $64
        ror     $65
        ror     $70
LD946:  rts

LD947:  lda     $66
        eor     #$FF
        sta     $66
LD94D:  lda     $62
        eor     #$FF
        sta     $62
        lda     $63
        eor     #$FF
        sta     $63
        lda     $64
        eor     #$FF
        sta     $64
        lda     $65
        eor     #$FF
        sta     $65
        lda     $70
        eor     #$FF
        sta     $70
        inc     $70
        bne     LD97D
LD96F:  inc     $65
        bne     LD97D
        inc     $64
        bne     LD97D
        inc     $63
        bne     LD97D
        inc     $62
LD97D:  rts

LD97E:  ldx     #$0F
        jmp     LC437

LD983:  ldx     #$25
LD985:  ldy     $04,x
        sty     $70
        ldy     $03,x
        sty     $04,x
        ldy     $02,x
        sty     $03,x
        ldy     $01,x
        sty     $02,x
        ldy     $68
        sty     $01,x
LD999:  adc     #$08
        bmi     LD985
        beq     LD985
        sbc     #$08
        tay
        lda     $70
        bcs     LD9BA
LD9A6:  asl     $01,x
        bcc     LD9AC
        inc     $01,x
LD9AC:  ror     $01,x
        ror     $01,x
LD9B0:  ror     $02,x
        ror     $03,x
        ror     $04,x
        ror     a
        iny
        bne     LD9A6
LD9BA:  clc
        rts

        sta     ($00,x)
        brk
        brk
        brk
        .byte   $03
        .byte   $7F
        lsr     LCB56,x
        adc     $1380,y
        .byte   $9B
        .byte   $0B
        .byte   $64
        .byte   $80
        ror     $38,x
        .byte   $93
        asl     $82,x
        sec
        tax
        .byte   $3B
        jsr     L3580
        .byte   $04
        .byte   $F3
        .byte   $34
        sta     ($35,x)
        .byte   $04
        .byte   $F3
        .byte   $34
        .byte   $80
        .byte   $80
        brk
        brk
        brk
        .byte   $80
        and     ($72),y
        .byte   $17
        sed
LD9EA:  jsr     LDC2B
        beq     LD9F1
        bpl     LD9F4
LD9F1:  jmp     LD248

LD9F4:  lda     $61
        sbc     #$7F
        pha
        lda     #$80
        sta     $61
        lda     #$D6
        ldy     #$D9
        jsr     LD867
        lda     #$DB
        ldy     #$D9
        jsr     LDB0F
        lda     #$BC
        ldy     #$D9
        jsr     LD850
        lda     #$C1
        ldy     #$D9
        jsr     LE040
        lda     #$E0
        ldy     #$D9
        jsr     LD867
        pla
        jsr     LDD7E
        lda     #$E5
        ldy     #$D9
LDA28:  jsr     LDA8C
        bne     LDA30
        jmp     LDA8B

LDA30:  jsr     LDAB7
        lda     #$00
        sta     $26
        sta     $27
        sta     $28
        sta     $29
        lda     $70
        jsr     LDA59
        lda     $65
        jsr     LDA59
        lda     $64
        jsr     LDA59
        lda     $63
        jsr     LDA59
        lda     $62
        jsr     LDA5E
        jmp     LDB8F

LDA59:  bne     LDA5E
        jmp     LD983

LDA5E:  lsr     a
        ora     #$80
LDA61:  tay
        bcc     LDA7D
        clc
        lda     $29
        adc     $6D
        sta     $29
        lda     $28
        adc     $6C
        sta     $28
        lda     $27
        adc     $6B
        sta     $27
        lda     $26
        adc     $6A
        sta     $26
LDA7D:  ror     $26
        ror     $27
        ror     $28
        ror     $29
        ror     $70
        tya
        lsr     a
        bne     LDA61
LDA8B:  rts

LDA8C:  sta     L0022
        sty     $23
        ldy     #$04
        lda     (L0022),y
        sta     $6D
        dey
        lda     (L0022),y
        sta     $6C
        dey
        lda     (L0022),y
        sta     $6B
        dey
        lda     (L0022),y
        sta     $6E
        eor     $66
        sta     $6F
        lda     $6E
        ora     #$80
        sta     $6A
        dey
        lda     (L0022),y
        sta     $69
        lda     $61
        rts

LDAB7:  lda     $69
        beq     LDADA
        clc
        adc     $61
        bcc     LDAC4
        bmi     LDADF
        clc
        .byte   $2C
LDAC4:  bpl     LDADA
        adc     #$80
        sta     $61
        bne     LDACF
        jmp     LD8FB

LDACF:  lda     $6F
        sta     $66
        rts

        lda     $66
        eor     #$FF
        bmi     LDADF
LDADA:  pla
        pla
        jmp     LD8F7

LDADF:  jmp     LD97E

LDAE2:  jsr     LDC0C
        tax
        beq     LDAF8
        clc
        adc     #$02
        bcs     LDADF
LDAED:  ldx     #$00
        stx     $6F
        jsr     LD877
        inc     $61
        beq     LDADF
LDAF8:  rts

        sty     $20
        brk
        brk
        brk
LDAFE:  jsr     LDC0C
        lda     #$F9
        ldy     #$DA
        ldx     #$00
        stx     $6F
        jsr     LDBA2
        jmp     LDB12

LDB0F:  jsr     LDA8C
LDB12:  beq     LDB8A
        jsr     LDC1B
        lda     #$00
        sec
        sbc     $61
        sta     $61
        jsr     LDAB7
        inc     $61
        beq     LDADF
        ldx     #$FC
        lda     #$01
LDB29:  ldy     $6A
        cpy     $62
        bne     LDB3F
        ldy     $6B
        cpy     $63
        bne     LDB3F
        ldy     $6C
        cpy     $64
        bne     LDB3F
        ldy     $6D
        cpy     $65
LDB3F:  php
        rol     a
        bcc     LDB4C
        inx
        sta     $29,x
        beq     LDB7A
        bpl     LDB7E
        lda     #$01
LDB4C:  plp
        bcs     LDB5D
LDB4F:  asl     $6D
        rol     $6C
        rol     $6B
        rol     $6A
        bcs     LDB3F
        bmi     LDB29
        bpl     LDB3F
LDB5D:  tay
        lda     $6D
        sbc     $65
        sta     $6D
        lda     $6C
        sbc     $64
        sta     $6C
        lda     $6B
        sbc     $63
        sta     $6B
        lda     $6A
        sbc     $62
        sta     $6A
        tya
        jmp     LDB4F

LDB7A:  lda     #$40
        bne     LDB4C
LDB7E:  asl     a
        asl     a
        asl     a
        asl     a
        asl     a
        asl     a
        sta     $70
        plp
        jmp     LDB8F

LDB8A:  ldx     #$14
        jmp     LC437

LDB8F:  lda     $26
        sta     $62
        lda     $27
        sta     $63
        lda     $28
        sta     $64
        lda     $29
        sta     $65
        jmp     LD8D7

LDBA2:  sta     L0022
        sty     $23
        ldy     #$04
        lda     (L0022),y
        sta     $65
        dey
        lda     (L0022),y
        sta     $64
        dey
        lda     (L0022),y
        sta     $63
        dey
        lda     (L0022),y
        sta     $66
        ora     #$80
        sta     $62
        dey
        lda     (L0022),y
        sta     $61
        sty     $70
        rts

        ldx     #$5C
        bit     $57A2
        ldy     #$00
        beq     LDBD4
LDBD0:  ldx     $49
        ldy     $4A
LDBD4:  jsr     LDC1B
        stx     L0022
        sty     $23
        ldy     #$04
        lda     $65
        sta     (L0022),y
        dey
        lda     $64
        sta     (L0022),y
        dey
        lda     $63
        sta     (L0022),y
        dey
        lda     $66
        ora     #$7F
        and     $62
        sta     (L0022),y
        dey
        lda     $61
        sta     (L0022),y
        sty     $70
        rts

LDBFC:  lda     $6E
LDBFE:  sta     $66
        ldx     #$05
LDC02:  lda     $68,x
        sta     $60,x
        dex
        bne     LDC02
        stx     $70
        rts

LDC0C:  jsr     LDC1B
        ldx     #$06
LDC11:  lda     $60,x
        sta     $68,x
        dex
        bne     LDC11
        stx     $70
LDC1A:  rts

LDC1B:  lda     $61
        beq     LDC1A
        asl     $70
        bcc     LDC1A
LDC23:  jsr     LD96F
        bne     LDC1A
        jmp     LD938

LDC2B:  lda     $61
        beq     LDC38
LDC2F:  lda     $66
LDC31:  rol     a
        lda     #$FF
        bcs     LDC38
        lda     #$01
LDC38:  rts

        jsr     LDC2B
LDC3C:  sta     $62
        lda     #$00
        sta     $63
        ldx     #$88
LDC44:  lda     $62
        eor     #$FF
        rol     a
LDC49:  lda     #$00
        sta     $65
        sta     $64
LDC4F:  stx     $61
        sta     $70
        sta     $66
        jmp     LD8D2

        lsr     $66
        rts

LDC5B:  sta     $24
LDC5D:  sty     $25
        ldy     #$00
        lda     ($24),y
        iny
        tax
        beq     LDC2B
        lda     ($24),y
        eor     $66
        bmi     LDC2F
        cpx     $61
        bne     LDC92
        lda     ($24),y
        ora     #$80
        cmp     $62
        bne     LDC92
        iny
        lda     ($24),y
        cmp     $63
        bne     LDC92
        iny
        lda     ($24),y
        cmp     $64
        bne     LDC92
        iny
        lda     #$7F
        cmp     $70
        lda     ($24),y
        sbc     $65
        beq     LDCBA
LDC92:  lda     $66
        bcc     LDC98
        eor     #$FF
LDC98:  jmp     LDC31

LDC9B:  lda     $61
        beq     LDCE9
        sec
        sbc     #$A0
        bit     $66
        bpl     LDCAF
        tax
        lda     #$FF
        sta     $68
        jsr     LD94D
        txa
LDCAF:  ldx     #$61
        cmp     #$F9
        bpl     LDCBB
        jsr     LD999
        sty     $68
LDCBA:  rts

LDCBB:  tay
        lda     $66
        and     #$80
        lsr     $62
        ora     $62
        sta     $62
        jsr     LD9B0
        sty     $68
        rts

LDCCC:  lda     $61
        cmp     #$A0
        bcs     LDCF2
        jsr     LDC9B
        sty     $70
        lda     $66
        sty     $66
        eor     #$80
        rol     a
        lda     #$A0
        sta     $61
        lda     $65
        sta     $07
        jmp     LD8D2

LDCE9:  sta     $62
        sta     $63
        sta     $64
        sta     $65
        tay
LDCF2:  rts

LDCF3:  ldy     #$00
        ldx     #$0A
LDCF7:  sty     $5D,x
        dex
        bpl     LDCF7
        bcc     LDD0D
        cmp     #$2D
        bne     LDD06
        stx     $67
        beq     LDD0A
LDD06:  cmp     #$2B
        bne     LDD0F
LDD0A:  jsr     L0073
LDD0D:  bcc     LDD6A
LDD0F:  cmp     #$2E
        beq     LDD41
        cmp     #$45
        bne     LDD47
        jsr     L0073
        bcc     LDD33
        cmp     #$AB
        beq     LDD2E
        cmp     #$2D
        beq     LDD2E
        cmp     #$AA
        beq     LDD30
        cmp     #$2B
        beq     LDD30
        bne     LDD35
LDD2E:  ror     $60
LDD30:  jsr     L0073
LDD33:  bcc     LDD91
LDD35:  bit     $60
        bpl     LDD47
        lda     #$00
        sec
        sbc     $5E
        jmp     LDD49

LDD41:  ror     $5F
        bit     $5F
        bvc     LDD0A
LDD47:  lda     $5E
LDD49:  sec
        sbc     $5D
        sta     $5E
        beq     LDD62
        bpl     LDD5B
LDD52:  jsr     LDAFE
        inc     $5E
        bne     LDD52
        beq     LDD62
LDD5B:  jsr     LDAE2
        dec     $5E
        bne     LDD5B
LDD62:  lda     $67
        bmi     LDD67
        rts

LDD67:  jmp     LDFB4

LDD6A:  pha
        bit     $5F
        bpl     LDD71
        inc     $5D
LDD71:  jsr     LDAE2
        pla
        sec
        sbc     #$30
        jsr     LDD7E
        jmp     LDD0A

LDD7E:  pha
        jsr     LDC0C
        pla
        jsr     LDC3C
        lda     $6E
        eor     $66
        sta     $6F
        ldx     $61
        jmp     LD86A

LDD91:  lda     $5E
        cmp     #$0A
        bcc     LDDA0
        lda     #$64
        bit     $60
        bmi     LDDAE
        jmp     LD97E

LDDA0:  asl     a
        asl     a
        clc
        adc     $5E
        asl     a
        clc
        ldy     #$00
        adc     ($7A),y
        sec
        sbc     #$30
LDDAE:  sta     $5E
        jmp     LDD30

        .byte   $9B
        rol     $1FBC,x
        sbc     $6E9E,x
        .byte   $6B
        .byte   $27
        sbc     $6E9E,x
        .byte   $6B
        plp
        brk
LDDC2:  lda     #$71
        ldy     #$C3
        jsr     LDDDA
        lda     $3A
        ldx     $39
LDDCD:  sta     $62
        stx     $63
        ldx     #$90
        sec
        jsr     LDC49
        jsr     LDDDF
LDDDA:  jmp     LCB1E

LDDDD:  ldy     #$01
LDDDF:  lda     #$20
        bit     $66
        bpl     LDDE7
        lda     #$2D
LDDE7:  sta     $FF,y
        sta     $66
        sty     $71
        iny
        lda     #$30
        ldx     $61
        bne     LDDF8
        jmp     LDF04

LDDF8:  lda     #$00
        cpx     #$80
        beq     LDE00
        bcs     LDE09
LDE00:  lda     #$BD
        ldy     #$DD
        jsr     LDA28
        lda     #$F7
LDE09:  sta     $5D
LDE0B:  lda     #$B8
        ldy     #$DD
        jsr     LDC5B
        beq     LDE32
        bpl     LDE28
LDE16:  lda     #$B3
        ldy     #$DD
        jsr     LDC5B
        beq     LDE21
        bpl     LDE2F
LDE21:  jsr     LDAE2
        dec     $5D
        bne     LDE16
LDE28:  jsr     LDAFE
        inc     $5D
        bne     LDE0B
LDE2F:  jsr     LD849
LDE32:  jsr     LDC9B
        ldx     #$01
        lda     $5D
        clc
        adc     #$0A
        bmi     LDE47
        cmp     #$0B
        bcs     LDE48
        adc     #$FF
        tax
        lda     #$02
LDE47:  sec
LDE48:  sbc     #$02
        sta     $5E
        stx     $5D
        txa
        beq     LDE53
        bpl     LDE66
LDE53:  ldy     $71
        lda     #$2E
        iny
        sta     $FF,y
        txa
        beq     LDE64
        lda     #$30
        iny
        sta     $FF,y
LDE64:  sty     $71
LDE66:  ldy     #$00
LDE68:  ldx     #$80
LDE6A:  lda     $65
        clc
        adc     LDF19,y
        sta     $65
        lda     $64
        adc     LDF18,y
        sta     $64
        lda     $63
        adc     LDF17,y
        sta     $63
        lda     $62
        adc     LDF16,y
        sta     $62
        inx
        bcs     LDE8E
        bpl     LDE6A
        bmi     LDE90
LDE8E:  bmi     LDE6A
LDE90:  txa
        bcc     LDE97
        eor     #$FF
        adc     #$0A
LDE97:  adc     #$2F
        iny
        iny
        iny
        iny
        sty     $47
        ldy     $71
        iny
        tax
        and     #$7F
        sta     $FF,y
        dec     $5D
        bne     LDEB2
        lda     #$2E
        iny
        sta     $FF,y
LDEB2:  sty     $71
        ldy     $47
        txa
        eor     #$FF
        and     #$80
        tax
        cpy     #$24
        beq     LDEC4
        cpy     #$3C
        bne     LDE6A
LDEC4:  ldy     $71
LDEC6:  lda     $FF,y
        dey
        cmp     #$30
        beq     LDEC6
        cmp     #$2E
        beq     LDED3
        iny
LDED3:  lda     #$2B
        ldx     $5E
        beq     LDF07
        bpl     LDEE3
        lda     #$00
        sec
LDEDE:  sbc     $5E
        tax
        lda     #$2D
LDEE3:  sta     $0101,y
        lda     #$45
        sta     $0100,y
        txa
        ldx     #$2F
        sec
LDEEF:  inx
        sbc     #$0A
        bcs     LDEEF
        adc     #$3A
        sta     $0103,y
        txa
        sta     $0102,y
        lda     #$00
        sta     $0104,y
        beq     LDF0C
LDF04:  sta     $FF,y
LDF07:  lda     #$00
        sta     $0100,y
LDF0C:  lda     #$00
        ldy     #$01
        rts

        .byte   $80
        brk
        brk
        brk
        brk
LDF16:  .byte   $FA
LDF17:  asl     a
LDF18:  .byte   $1F
LDF19:  brk
        brk
        tya
        stx     L0080,y
        .byte   $FF
        beq     LDEDE
        cpy     #$00
        ora     ($86,x)
        ldy     #$FF
        .byte   $FF
        cld
        beq     LDF2B
LDF2B:  brk
        .byte   $03
        inx
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $9C
        brk
        brk
        brk
        asl     a
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $FF
        .byte   $DF
        asl     a
        .byte   $80
        brk
        .byte   $03
        .byte   $4B
        cpy     #$FF
        .byte   $FF
        .byte   $73
        rts

        brk
        brk
        asl     $FF10
        .byte   $FF
        sbc     a:$A8,x
        brk
        brk
        .byte   $3C
        .byte   $BF
        tax
        tax
        tax
        tax
        tax
        tax
        tax
        tax
        tax
        tax
        tax
        tax
        tax
        tax
        tax
        tax
        tax
        tax
        tax
        tax
        tax
        tax
        tax
LDF6A:  tax
        tax
        tax
        tax
        tax
        tax
        tax
        jsr     LDC0C
        lda     #$11
        ldy     #$DF
        jsr     LDBA2
        beq     LDFED
        lda     $69
        bne     LDF84
        jmp     LD8F9

LDF84:  ldx     #$4E
        ldy     #$00
        jsr     LDBD4
        lda     $6E
        bpl     LDF9E
        jsr     LDCCC
        lda     #$4E
        ldy     #$00
        jsr     LDC5B
        bne     LDF9E
        tya
        ldy     $07
LDF9E:  jsr     LDBFE
        tya
        pha
        jsr     LD9EA
        lda     #$4E
        ldy     #$00
        jsr     LDA28
        jsr     LDFED
        pla
        lsr     a
        .byte   $90
LDFB3:  asl     a
LDFB4:  lda     $61
        beq     LDFBE
        lda     $66
        eor     #$FF
        sta     $66
LDFBE:  rts

        sta     ($38,x)
        tax
        .byte   $3B
        and     #$07
        adc     ($34),y
        cli
        rol     $7456,x
        asl     $7E,x
        .byte   $B3
        .byte   $1B
        .byte   $77
        .byte   $2F
        inc     $85E3
        .byte   $7A
        ora     $1C84,x
        rol     a
        .byte   $7C
        .byte   $63
        eor     $0A58,y
        ror     $FD75,x
        .byte   $E7
        dec     L0080
        and     ($72),y
        clc
        bpl     LDF6A
        brk
        brk
        brk
        brk
LDFED:  lda     #$BF
        ldy     #$DF
        jsr     LDA28
        lda     $70
        adc     #$50
        bcc     LDFFD
        jsr     LDC23
LDFFD:  sta     $56
        .byte   $20
