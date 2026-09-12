; da65 V2.18 - Ubuntu 2.19-1
; Created:    2026-09-09 23:08:28
; Input file: roms/pet/pet-2001-8/rom-1-f800.901439-07.bin
; Page:       1


        .setcpu "6502"

LC71C           := $C71C
LE0D2           := $E0D2
LE1E1           := $E1E1
LE1F0           := $E1F0
LE3EA           := $E3EA
LE67E           := $E67E
LE7DE           := $E7DE
LF0BA           := $F0BA
LF12C           := $F12C
LF132           := $F132
LF1CC           := $F1CC
LF1DF           := $F1DF
LF230           := $F230
LF277           := $F277
LF27D           := $F27D
LF299           := $F299
LF2A4           := $F2A4
LF2C8           := $F2C8
LF32A           := $F32A
LF339           := $F339
LF346           := $F346
LF47D           := $F47D
LF4BB           := $F4BB
LF52A           := $F52A
LF667           := $F667
LF67D           := $F67D
LF695           := $F695
LF69E           := $F69E
LF736           := $F736
LF78B           := $F78B
LF79D           := $F79D
LF7DC           := $F7DC
        bne     LF806
        ldy     #$94
        bne     LF79D
LF806:  sta     $0264
        jmp     LF277

        pha
        jsr     LF0BA
        lda     $F0
        bpl     LF81A
        jsr     LF132
        jmp     LF821

LF81A:  and     #$1F
        ora     #$60
        jsr     LF12C
LF821:  lda     $020C
        bpl     LF829
        jmp     LF47D

LF829:  pla
        jmp     LF806

        jsr     LF667
        ldx     $F1
        inc     $0270,x
        ldy     $0270,x
        cpy     #$C0
        rts

LF83B:  jsr     LF85E
        beq     LF870
        ldy     #$41
LF842:  jsr     LE7DE
        ldy     #$56
        jsr     LE7DE
        lda     $F1
        ora     #$30
        jsr     LE3EA
LF851:  jsr     LF339
        jsr     LF85E
        bne     LF851
        ldy     #$AA
        jmp     LE7DE

LF85E:  lda     #$10
        ldx     $F1
        cpx     #$01
        beq     LF868
        lda     #$20
LF868:  bit     $E810
        bne     LF870
        bit     $E810
LF870:  rts

LF871:  jsr     LF85E
        beq     LF870
        ldy     #$41
        jsr     LE7DE
        ldy     #$4D
        bne     LF842
        lda     #$00
        sta     $020C
        sta     $020B
        jsr     LF67D
        jsr     LF913
        jsr     LF83B
        sei
        lda     #$00
        sta     $0278
        sta     $EB
        sta     $E7
        sta     $0276
        sta     $0277
        sta     $0266
        ldx     #$0E
        jsr     LFD1B
        ldx     $F1
        dex
        beq     LF8B4
        lda     #$90
        sta     $E84E
        bne     LF8D2
LF8B4:  inc     $E811
        bne     LF8D2
        jsr     LF67D
        jsr     LF913
        lda     #$14
        sta     $0279
        jsr     LF871
        sei
        lda     #$A0
        sta     $E84E
        ldx     #$08
        jsr     LFD1B
LF8D2:  lda     #$02
        sta     $FD
        jsr     LFBEC
        dec     $E813
        ldx     $F1
        dex
        bne     LF8EB
        lda     #$34
        sta     $E813
        sta     $0207
        bne     LF8F6
LF8EB:  lda     $E840
        stx     $0208
        and     #$EF
        sta     $E840
LF8F6:  sta     $E849
        cli
LF8FA:  lda     #$E6
        cmp     $021A
        beq     LF912
        jsr     LF91E
        bit     $E813
        bpl     LF8FA
        bit     $E812
        jsr     LF736
        jmp     LF8FA

LF912:  rts

LF913:  jsr     LF91E
        lda     #$E6
        cmp     $021A
        bne     LF913
        rts

LF91E:  jsr     LF32A
        bne     LF92B
        php
        jsr     LFCFB
        jsr     LF299
        plp
LF92B:  jmp     LC71C

LF92E:  stx     $E8
        lda     $E7
        asl     a
        asl     a
        clc
        adc     $E7
        clc
        adc     $E8
        sta     $E8
        lda     #$00
        bit     $E7
        bmi     LF943
        rol     a
LF943:  asl     $E8
        rol     a
        asl     $E8
        rol     a
        tax
LF94A:  lda     $E848
        cmp     #$15
        bcc     LF94A
        adc     $E8
        sta     $E844
        txa
        adc     $E849
        sta     $E845
        cli
        rts

LF95F:  ldx     $E849
        ldy     #$FF
        tya
        sbc     $E848
        cpx     $E849
        bne     LF95F
        stx     $E8
        tax
        sty     $E848
        sty     $E849
        tya
        sbc     $E8
        stx     $E8
        lsr     a
        ror     $E8
        lsr     a
        ror     $E8
        lda     $E7
        clc
        adc     #$3C
        bit     $E840
        bit     $E810
        cmp     $E8
        bcs     LF9E5
        ldx     $0266
        beq     LF998
        jmp     LFAA3

LF998:  ldx     $026C
        bmi     LF9B8
        ldx     #$00
        adc     #$30
        adc     $E7
        cmp     $E8
        bcs     LF9C5
        inx
        adc     #$26
        adc     $E7
        cmp     $E8
        bcs     LF9CB
        adc     #$2C
        adc     $E7
        cmp     $E8
        bcc     LF9BB
LF9B8:  jmp     LFA4A

LF9BB:  nop
LF9BC:  lda     $EB
        beq     LF9E5
        sta     $0274
        bne     LF9E5
LF9C5:  inc     $0275
        bcs     LF9CE
LF9CA:  nop
LF9CB:  dec     $0275
LF9CE:  sec
        sbc     #$13
        sbc     $E8
        adc     $020A
        sta     $020A
        lda     $026F
        eor     #$01
        sta     $026F
        beq     LFA07
        stx     $F6
LF9E5:  lda     $EB
        beq     LFA04
        bit     $E84D
        bvc     LFA04
        lda     #$00
        sta     $026F
        lda     $026C
        bpl     LFA2E
        bmi     LF9B8
LF9FA:  ldx     #$A6
        jsr     LF92E
        lda     $0265
        bne     LF9BC
LFA04:  jmp     LE67E

LFA07:  lda     $020A
        beq     LFA14
        bmi     LFA12
        dec     $E7
        dec     $E7
LFA12:  inc     $E7
LFA14:  lda     #$00
        sta     $020A
        cpx     $F6
        bne     LFA2E
        txa
        bne     LF9BC
        lda     $0275
        bmi     LF9E5
        cmp     #$10
        bcc     LF9E5
        sta     $0228
        bcs     LF9E5
LFA2E:  txa
        eor     $0265
        sta     $0265
        lda     $EB
        beq     LFA04
        dec     $026C
        bmi     LF9FA
        lsr     $F6
        ror     $FE
        ldx     #$DA
        jsr     LF92E
        jmp     LE67E

LFA4A:  lda     $0228
        beq     LFA53
        lda     $EB
        beq     LFA5B
LFA53:  lda     $026C
        bmi     LFA5B
        jmp     LF9CA

LFA5B:  lsr     $E8
        lda     #$93
        sec
        sbc     $E8
        adc     $E7
        asl     a
        tax
        jsr     LF92E
        inc     $0266
        lda     $EB
        bne     LFA84
        lda     $0228
        beq     LFAA0
        sta     $0274
        lda     #$00
        sta     $0228
        lda     #$C0
        sta     $E84E
        sta     $EB
LFA84:  lda     $0228
        sta     $EC
        beq     LFA94
        lda     #$00
        sta     $EB
        lda     #$40
        sta     $E84E
LFA94:  lda     $FE
        sta     $FC
        lda     $0274
        ora     $0275
        sta     $ED
LFAA0:  jmp     LE67E

LFAA3:  jsr     LFBEC
        sta     $0266
        ldx     #$DA
        jsr     LF92E
        lda     $FD
        beq     LFAB5
        sta     $0273
LFAB5:  lda     #$0F
        bit     $0278
        bpl     LFAD4
        lda     $EC
        bne     LFACC
        ldx     $FD
        dex
        bne     LFAD1
        lda     #$08
        jsr     LFBE5
        bne     LFAD1
LFACC:  lda     #$00
        sta     $0278
LFAD1:  jmp     LE67E

LFAD4:  bvs     LFB0D
        bne     LFAF2
        lda     $EC
        bne     LFAD1
        lda     $ED
        bne     LFAD1
        lda     $0273
        lsr     a
        lda     $FC
        bmi     LFAEB
        bcc     LFB06
        clc
LFAEB:  bcs     LFB06
        and     #$0F
        sta     $0278
LFAF2:  dec     $0278
        bne     LFAD1
        lda     #$40
        sta     $0278
        jsr     LFBDC
        lda     #$00
        sta     $0279
        beq     LFAD1
LFB06:  lda     #$80
        sta     $0278
        bne     LFAD1
LFB0D:  lda     $EC
        beq     LFB1B
        lda     #$04
        jsr     LFBE5
        lda     #$00
        jmp     LFBA7

LFB1B:  jsr     LFD90
        bne     LFB23
        jmp     LFBA5

LFB23:  ldx     $0273
        dex
        beq     LFB5A
        lda     $020B
        beq     LFB3A
        ldy     #$00
        lda     $FC
        cmp     ($E3),y
        beq     LFB3A
        lda     #$01
        sta     $ED
LFB3A:  lda     $ED
        beq     LFB92
        ldx     #$3D
        cpx     $0276
        bcc     LFB8B
        ldx     $0276
        lda     $E4
        sta     $0101,x
        lda     $E3
        sta     $0100,x
        inx
        inx
        stx     $0276
        jmp     LFB92

LFB5A:  ldx     $0277
        cpx     $0276
        beq     LFB9D
        lda     $E3
        cmp     $0100,x
        bne     LFB9D
        lda     $E4
        cmp     $0101,x
        bne     LFB9D
        inc     $0277
        inc     $0277
        lda     $020B
        beq     LFB87
        lda     $FC
        ldy     #$00
        cmp     ($E3),y
        beq     LFB9D
        lda     #$01
        sta     $ED
LFB87:  lda     $ED
        beq     LFB92
LFB8B:  lda     #$10
        jsr     LFBE5
        bne     LFB9D
LFB92:  lda     $020B
        bne     LFB9D
        lda     $FC
        ldy     #$00
        sta     ($E3),y
LFB9D:  inc     $E3
        bne     LFBD9
        inc     $E4
        bne     LFBD9
LFBA5:  lda     #$80
LFBA7:  sta     $0278
        ldx     $FD
        dex
        bmi     LFBB1
        stx     $FD
LFBB1:  dec     $0273
        beq     LFBBF
        lda     $0276
        bne     LFBD9
        sta     $FD
        beq     LFBD9
LFBBF:  jsr     LFCFB
        jsr     LFBDC
        ldy     #$00
        sty     $0279
        jsr     LFD7C
        lda     $0279
        eor     $FC
        beq     LFBD9
        lda     #$20
        jsr     LFBE5
LFBD9:  jmp     LE67E

LFBDC:  lda     $F8
        sta     $E4
        lda     $F7
        sta     $E3
        rts

LFBE5:  ora     $020C
        sta     $020C
        rts

LFBEC:  lda     #$08
        sta     $026C
        lda     #$00
        sta     $026F
        sta     $0274
        sta     $0265
        sta     $0275
        rts

LFC00:  lda     $FC
        lsr     a
        lda     #$60
        bcc     LFC09
LFC07:  lda     #$B0
LFC09:  ldx     #$00
LFC0B:  sta     $E848
        stx     $E849
        lda     $E840
        eor     #$08
        sta     $E840
        and     #$08
        rts

LFC1C:  sec
        ror     $E4
        bmi     LFC65
        lda     $0274
        bne     LFC39
        lda     #$10
        ldx     #$01
        jsr     LFC0B
        bne     LFC65
        inc     $0274
        lda     $E4
        bpl     LFC65
        jmp     LFCBB

LFC39:  lda     $0275
        bne     LFC48
        jsr     LFC07
        bne     LFC65
        inc     $0275
        bne     LFC65
LFC48:  jsr     LFC00
        bne     LFC65
        lda     $026F
        eor     #$01
        sta     $026F
        beq     LFC68
        lda     $FC
        eor     #$01
        sta     $FC
        and     #$01
        eor     $0265
        sta     $0265
LFC65:  jmp     LE67E

LFC68:  lsr     $FC
        dec     $026C
        lda     $026C
        beq     LFCB1
        bpl     LFC65
LFC74:  jsr     LFBEC
        cli
        lda     $0270
        beq     LFC90
        ldx     #$00
        stx     $F6
        dec     $0270
        ldx     $FD
        cpx     #$02
        bne     LFC8C
        ora     #$80
LFC8C:  sta     $FC
        bne     LFC65
LFC90:  jsr     LFD90
        bcc     LFC9F
        bne     LFC1C
        inc     $E4
        lda     $F6
        sta     $FC
        bcs     LFC65
LFC9F:  ldy     #$00
        lda     ($E3),y
        sta     $FC
        eor     $F6
        sta     $F6
        inc     $E3
        bne     LFC65
        inc     $E4
        bne     LFC65
LFCB1:  lda     $0265
        eor     #$01
        sta     $FC
LFCB8:  jmp     LE67E

LFCBB:  dec     $FD
        bne     LFCC2
        jsr     LFFED
LFCC2:  lda     #$50
        sta     $0273
        ldx     #$08
        sei
        jsr     LFD1B
        bne     LFCB8
LFCCF:  lda     #$78
        jsr     LFC09
        bne     LFCB8
        dec     $0273
        bne     LFCB8
        jsr     LFBEC
        dec     $0279
        bpl     LFCB8
        ldx     #$0A
        jsr     LFD1B
        cli
        inc     $0279
        lda     $FD
        beq     LFD16
        jsr     LFBDC
        ldx     #$09
        stx     $0270
        jmp     LFC74

LFCFB:  php
        sei
        jsr     LFFED
        lda     #$7F
        sta     $E84E
        lda     #$3C
        sta     $E811
        lda     #$3D
        sta     $E813
        ldx     #$0C
        jsr     LFD1B
        plp
        rts

LFD16:  jsr     LFCFB
        beq     LFCB8
LFD1B:  lda     LFD28,x
        sta     $0219
        lda     LFD29,x
        sta     $021A
        rts

LFD28:  .byte   $C2
LFD29:  sbc     LFE80,x
        brk
        brk
        .byte   $9E
        inc     LFCCF,x
        and     ($FC,x)
        sta     $E6
        .byte   $5F
        sbc     LFFA2,y
        txs
        cld
        jsr     LE1E1
        lda     $E810
        bpl     LFD9B
        cli
        jmp     LE0D2

LFD48:  sty     $00
        sty     $01
        ldy     #$03
LFD4E:  tya
        clc
        adc     $01
        eor     $02
        sta     ($00),y
        iny
        bne     LFD4E
        inc     $01
        cpx     $01
        bne     LFD4E
        sty     $00
        sty     $01
        ldy     #$03
LFD65:  tya
        clc
        adc     $01
        eor     $02
        cmp     ($00),y
LFD6D:  bne     LFD6D
        iny
        bne     LFD65
        inc     $01
        cpx     $01
        bne     LFD65
        jmp     LFE2D

        brk
LFD7C:  lda     ($E3),y
        eor     $0279
        sta     $0279
        inc     $E3
        bne     LFD8A
        inc     $E4
LFD8A:  jsr     LFD90
        bne     LFD7C
        rts

LFD90:  lda     $E4
        cmp     $E6
        bne     LFD9A
        lda     $E3
        cmp     $E5
LFD9A:  rts

LFD9B:  stx     $E811
        stx     $E810
        lda     #$04
        sta     $E811
        sta     $E810
        jsr     LFD1B
        dex
        stx     $E844
        bit     $E812
LFDB3:  bit     $E813
        bpl     LFDB3
        stx     $E845
        bit     $E812
        cli
LFDBF:  jmp     LFDBF

        lda     $E844
        ldy     $E845
        bit     $E813
LFDCB:  bpl     LFDCB
        and     #$C0
        cmp     #$C0
LFDD1:  bne     LFDD1
        cpy     #$BE
LFDD5:  bne     LFDD5
        dec     $E813
        bit     $E812
        lda     #$00
        sta     $0279
        sta     $E3
        sta     $E5
        tay
        ldx     #$C0
        stx     $E4
        ldx     #$E8
        stx     $E6
        jsr     LFD7C
        ldx     #$F0
        stx     $E4
        sty     $E6
        jsr     LFD7C
        cpy     $0279
LFDFE:  bne     LFDFE
        ldy     #$FF
        sty     $02
        iny
        sty     $00
        lda     #$10
        sta     $01
        ldx     #$20
LFE0D:  lda     ($00),y
        eor     $02
        sta     ($00),y
        cmp     ($00),y
        beq     LFE18
        iny
LFE18:  inc     $01
        cpx     $01
        bne     LFE0D
        cpy     #$00
        beq     LFE2A
        cpy     #$10
LFE24:  bne     LFE24
        ldx     #$10
        ldy     #$00
LFE2A:  jmp     LFD48

LFE2D:  inc     $02
        cpy     $02
        beq     LFE2A
        sty     $E811
        lda     #$0F
        sta     $E810
        lda     #$3C
        sta     $E811
        lda     #$FD
        ldx     #$09
LFE44:  stx     $E810
        cmp     $E812
LFE4A:  bne     LFE4A
        lsr     a
        bcc     LFE51
        ora     #$80
LFE51:  dex
        bpl     LFE44
        stx     $E810
        cpx     $E812
LFE5A:  bne     LFE5A
        sty     $E811
        sty     $E810
        lda     #$3C
        sta     $E811
        ldx     #$02
        jsr     LFD1B
        ldx     #$F7
        stx     $E840
        inc     $E811
        bit     $E810
        cli
        asl     $E840
        lsr     $E840
LFE7E:  bne     LFE7E
LFE80:  dec     $E811
LFE83:  bpl     LFE83
        bit     $E810
        ldx     #$06
        jsr     LFD1B
        bit     $E840
        lda     #$90
        sta     $E84E
        cli
        asl     $E840
        lsr     $E840
LFE9C:  bne     LFE9C
        bit     $E84D
LFEA1:  bpl     LFEA1
        lda     #$7F
        sta     $E84E
        bit     $E840
        jmp     LFEC3

LFEAE:  bit     $E841
        bvs     LFEAE
LFEB3:  bit     $E84F
        bvc     LFEB3
LFEB8:  bit     $E84F
        bvs     LFEB8
        lda     $E84D
        and     #$02
        rts

LFEC3:  ldx     #$0A
LFEC5:  bit     $E841
        bpl     LFEC5
LFECA:  bit     $E841
        bmi     LFECA
LFECF:  dex
        bne     LFECF
        bit     $E841
        bmi     LFED9
LFED7:  bpl     LFED7
LFED9:  inc     LFFFF
        bit     $E841
LFEDF:  bmi     LFEDF
LFEE1:  bit     $E841
        bvc     LFEE1
LFEE6:  bit     $E841
        bvs     LFEE6
LFEEB:  bit     $E841
        bvc     LFEEB
LFEF0:  bit     $E841
        bvs     LFEF0
        jsr     LE1F0
        jsr     LFEAE
LFEFB:  bne     LFEFB
        ldy     #$00
LFEFF:  lda     #$A0
        sta     ($E0),y
        jsr     LFEAE
LFF06:  beq     LFF06
        lda     #$20
        sta     ($E0),y
        jsr     LFEAE
LFF0F:  bne     LFF0F
        cpy     #$E7
        bne     LFF1B
        lda     #$83
        cmp     $E1
        beq     LFF24
LFF1B:  iny
        bne     LFEFF
        inc     $E1
        bne     LFEFF
LFF22:  beq     LFF22
LFF24:  tya
        sta     ($E0),y
        dey
        cpy     #$FF
        bne     LFF24
        dec     $E1
        lda     #$7F
        cmp     $E1
        bne     LFF24
        lda     #$34
        sta     $E811
        jsr     LFEAE
LFF3C:  bne     LFF3C
        lda     #$3C
        sta     $E811
        ldx     $00
LFF45:  stx     $E822
        cpx     $E820
LFF4B:  bne     LFF4B
        inx
        bne     LFF45
        bit     $E820
        lda     #$FB
        sta     $E840
        bit     $E821
LFF5B:  bpl     LFF5B
        bit     $E840
LFF60:  bvc     LFF60
        lsr     a
        sta     $E840
        bit     $E840
LFF69:  bvs     LFF69
LFF6B:  bpl     LFF6B
        lda     #$34
        sta     $E823
        lda     $E840
LFF75:  bmi     LFF75
        lsr     a
LFF78:  bcc     LFF78
        lda     #$34
        sta     $E821
        lsr     $E840
LFF82:  bcs     LFF82
        lda     #$02
        sta     $E843
        sta     $E841
        bit     $E810
LFF8F:  bvc     LFF8F
        stx     $E841
        bit     $E810
        bvc     LFF9E
        nop
        nop
LFF9B:  jmp     LFF9B

LFF9E:  stx     $E811
        .byte   $A0
LFFA2:  .byte   $0F
        sty     $E810
        asl     a
        sta     $E811
        eor     #$FF
        sta     $E810
LFFAF:  jmp     LFFAF

        adc     ($00,x)
        brk
        brk
        brk
        brk
        brk
        brk
        brk
        brk
        brk
        brk
        brk
        brk
        jmp     LF52A

        jmp     LF2C8

        jmp     LF78B

        jmp     LF7DC

        jmp     LF27D

        jmp     LF1DF

        jmp     LF230

        jmp     LF346

        jmp     LF69E

        jmp     LF4BB

        jmp     LF695

        jmp     LF339

        jmp     LF1CC

        jmp     LF2A4

        jmp     LF736

LFFED:  lda     #$3C
        sta     $E813
        lda     $E840
        ora     #$10
        sta     $E840
        rts

        dex
        sec
        .byte   $FD
        .byte   $6B
LFFFF:  .byte   $E6
