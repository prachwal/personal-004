; da65 V2.18 - Ubuntu 2.19-1
; Created:    2026-09-09 23:08:38
; Input file: roms/pet/pet-2001-8/rom-1-d800.901439-06.bin
; Page:       1


        .setcpu "6502"

L00C2           := $00C2
L3580           := $3580
LC359           := $C359
LCA27           := $CA27
LD130           := $D130
LD71E           := $D71E
LD725           := $D725
LD728           := $D728
LD73C           := $D73C
LD73F           := $D73F
LD74C           := $D74C
LD7A7           := $D7A7
LD7AC           := $D7AC
LD7CC           := $D7CC
LD7CE           := $D7CE
LD7D0           := $D7D0
        sec
        sbc     $B0
        bcs     LD7CC
        eor     #$FF
        adc     #$01
        sta     $B0
        bcc     LD81B
LD80D:  inc     $B0
        beq     LD853
        ror     $B1
        ror     $B2
        ror     $B3
        ror     $B4
        ror     $BF
LD81B:  rts

        lda     $B5
        eor     #$FF
        sta     $B5
LD822:  lda     $B1
        eor     #$FF
        sta     $B1
        lda     $B2
        eor     #$FF
        sta     $B2
        lda     $B3
        eor     #$FF
        sta     $B3
        lda     $B4
        eor     #$FF
        sta     $B4
        lda     $BF
        eor     #$FF
        sta     $BF
        inc     $BF
        bne     LD852
LD844:  inc     $B4
        bne     LD852
        inc     $B3
        bne     LD852
        inc     $B2
        bne     LD852
        inc     $B1
LD852:  rts

LD853:  ldx     #$4A
        jmp     LC359

LD858:  ldx     #$74
LD85A:  ldy     $04,x
        sty     $BF
        ldy     $03,x
        sty     $04,x
        ldy     $02,x
        sty     $03,x
        ldy     $01,x
        sty     $02,x
        ldy     $B7
        sty     $01,x
LD86E:  adc     #$08
        bmi     LD85A
        beq     LD85A
        sbc     #$08
        tay
        lda     $BF
        bcs     LD88F
LD87B:  asl     $01,x
        bcc     LD881
        inc     $01,x
LD881:  ror     $01,x
        ror     $01,x
LD885:  ror     $02,x
        ror     $03,x
        ror     $04,x
        ror     a
        iny
        bne     LD87B
LD88F:  clc
        rts

        sta     ($00,x)
        brk
        brk
        brk
        .byte   $03
        .byte   $7F
        lsr     $CB56,x
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
LD8BF:  jsr     LDAFD
        beq     LD8C6
        bpl     LD8C9
LD8C6:  jmp     LD130

LD8C9:  lda     $B0
        sbc     #$7F
        pha
        lda     #$80
        sta     $B0
        lda     #$AB
        ldy     #$D8
        jsr     LD73C
        lda     #$B0
        ldy     #$D8
        jsr     LD9E1
        lda     #$91
        ldy     #$D8
        jsr     LD725
        lda     #$96
        ldy     #$D8
        jsr     LDEF3
        lda     #$B5
        ldy     #$D8
        jsr     LD73C
        pla
        jsr     LDC50
        lda     #$BA
        ldy     #$D8
LD8FD:  jsr     LD95E
        beq     LD95D
        jsr     LD989
        lda     #$00
        sta     $75
        sta     $76
        sta     $77
        sta     $78
        lda     $BF
        jsr     LD92B
        lda     $B4
        jsr     LD92B
        lda     $B3
        jsr     LD92B
        lda     $B2
        jsr     LD92B
        lda     $B1
        jsr     LD930
        jmp     LDA61

LD92B:  bne     LD930
        jmp     LD858

LD930:  lsr     a
        ora     #$80
LD933:  tay
        bcc     LD94F
        clc
        lda     $78
        adc     $BC
        sta     $78
        lda     $77
        adc     $BB
        sta     $77
        lda     $76
        adc     $BA
        sta     $76
        lda     $75
        adc     $B9
        sta     $75
LD94F:  ror     $75
        ror     $76
        ror     $77
        ror     $78
        ror     $BF
        tya
        lsr     a
        bne     LD933
LD95D:  rts

LD95E:  sta     $71
        sty     $72
        ldy     #$04
        lda     ($71),y
        sta     $BC
        dey
        lda     ($71),y
        sta     $BB
        dey
        lda     ($71),y
        sta     $BA
        dey
        lda     ($71),y
        sta     $BD
        eor     $B5
        sta     $BE
        lda     $BD
        ora     #$80
        sta     $B9
        dey
        lda     ($71),y
        sta     $B8
        lda     $B0
        rts

LD989:  lda     $B8
LD98B:  beq     LD9AC
        clc
        adc     $B0
        bcc     LD996
        bmi     LD9B1
        clc
        .byte   $2C
LD996:  bpl     LD9AC
        adc     #$80
        sta     $B0
        bne     LD9A1
        jmp     LD7D0

LD9A1:  lda     $BE
        sta     $B5
        rts

LD9A6:  lda     $B5
        eor     #$FF
        bmi     LD9B1
LD9AC:  pla
        pla
        jmp     LD7CC

LD9B1:  jmp     LD853

LD9B4:  jsr     LDADE
        tax
        beq     LD9CA
        clc
        adc     #$02
        bcs     LD9B1
        ldx     #$00
        stx     $BE
        jsr     LD74C
        inc     $B0
        beq     LD9B1
LD9CA:  rts

        sty     $20
        brk
        brk
        brk
LD9D0:  jsr     LDADE
        lda     #$CB
        ldy     #$D9
        ldx     #$00
LD9D9:  stx     $BE
        jsr     LDA74
        jmp     LD9E4

LD9E1:  jsr     LD95E
LD9E4:  beq     LDA5C
        jsr     LDAED
        lda     #$00
        sec
        sbc     $B0
        sta     $B0
        jsr     LD989
        inc     $B0
        beq     LD9B1
        ldx     #$FC
        lda     #$01
LD9FB:  ldy     $B9
        cpy     $B1
        bne     LDA11
        ldy     $BA
        cpy     $B2
        bne     LDA11
        ldy     $BB
        cpy     $B3
        bne     LDA11
        ldy     $BC
        cpy     $B4
LDA11:  php
        rol     a
        bcc     LDA1E
        inx
        sta     $78,x
        beq     LDA4C
        bpl     LDA50
        lda     #$01
LDA1E:  plp
        bcs     LDA2F
LDA21:  asl     $BC
        rol     $BB
        rol     $BA
        rol     $B9
        bcs     LDA11
        bmi     LD9FB
        bpl     LDA11
LDA2F:  tay
        lda     $BC
        sbc     $B4
        sta     $BC
        lda     $BB
        sbc     $B3
        sta     $BB
        lda     $BA
        sbc     $B2
        sta     $BA
        lda     $B9
        sbc     $B1
        sta     $B9
        tya
        jmp     LDA21

LDA4C:  lda     #$40
        bne     LDA1E
LDA50:  asl     a
        asl     a
        asl     a
        asl     a
        asl     a
        asl     a
        sta     $BF
        plp
        jmp     LDA61

LDA5C:  ldx     #$8A
        jmp     LC359

LDA61:  lda     $75
        sta     $B1
        lda     $76
        sta     $B2
        lda     $77
        sta     $B3
        lda     $78
        sta     $B4
        jmp     LD7AC

LDA74:  sta     $71
        sty     $72
        ldy     #$04
        lda     ($71),y
        sta     $B4
        dey
        lda     ($71),y
        sta     $B3
        dey
        lda     ($71),y
        sta     $B2
        dey
        lda     ($71),y
        sta     $B5
        ora     #$80
        sta     $B1
        dey
        lda     ($71),y
        sta     $B0
        sty     $BF
        rts

LDA99:  ldx     #$AB
        .byte   $2C
LDA9C:  ldx     #$A6
        ldy     #$00
        beq     LDAA6
        ldx     $98
        ldy     $99
LDAA6:  jsr     LDAED
        stx     $71
        sty     $72
        ldy     #$04
        lda     $B4
        sta     ($71),y
        dey
        lda     $B3
        sta     ($71),y
        dey
        lda     $B2
        sta     ($71),y
        dey
        lda     $B5
        ora     #$7F
        and     $B1
        sta     ($71),y
        dey
        lda     $B0
        sta     ($71),y
        sty     $BF
        rts

        lda     $BD
LDAD0:  sta     $B5
        ldx     #$05
LDAD4:  lda     $B7,x
        sta     $AF,x
        dex
        bne     LDAD4
        stx     $BF
        rts

LDADE:  jsr     LDAED
LDAE1:  ldx     #$06
LDAE3:  lda     $AF,x
        sta     $B7,x
        dex
        bne     LDAE3
        stx     $BF
LDAEC:  rts

LDAED:  lda     $B0
        beq     LDAEC
        asl     $BF
        bcc     LDAEC
LDAF5:  jsr     LD844
        bne     LDAEC
        jmp     LD80D

LDAFD:  lda     $B0
        beq     LDB0A
LDB01:  lda     $B5
LDB03:  rol     a
        lda     #$FF
        bcs     LDB0A
        lda     #$01
LDB0A:  rts

        jsr     LDAFD
LDB0E:  sta     $B1
        lda     #$00
        sta     $B2
        ldx     #$88
        lda     $B1
        eor     #$FF
        rol     a
LDB1B:  lda     #$00
        sta     $B4
        sta     $B3
        stx     $B0
        sta     $BF
        sta     $B5
        jmp     LD7A7

        lsr     $B5
        rts

LDB2D:  sta     $73
        sty     $74
        ldy     #$00
        lda     ($73),y
        iny
        tax
        beq     LDAFD
        lda     ($73),y
        eor     $B5
        bmi     LDB01
        cpx     $B0
        bne     LDB64
        lda     ($73),y
        ora     #$80
        cmp     $B1
        bne     LDB64
        iny
        lda     ($73),y
        cmp     $B2
        bne     LDB64
        iny
        lda     ($73),y
        cmp     $B3
        bne     LDB64
        iny
        lda     #$7F
        cmp     $BF
        lda     ($73),y
        sbc     $B4
        beq     LDB8C
LDB64:  lda     $B5
        bcc     LDB6A
        eor     #$FF
LDB6A:  jmp     LDB03

LDB6D:  lda     $B0
        beq     LDBBB
        sec
        sbc     #$A0
        bit     $B5
        bpl     LDB81
        tax
        lda     #$FF
        sta     $B7
        jsr     LD822
        txa
LDB81:  ldx     #$B0
        cmp     #$F9
        bpl     LDB8D
        jsr     LD86E
        sty     $B7
LDB8C:  rts

LDB8D:  tay
        lda     $B5
        and     #$80
        lsr     $B1
        ora     $B1
        sta     $B1
        jsr     LD885
        sty     $B7
        rts

LDB9E:  lda     $B0
        cmp     #$A0
        bcs     LDBC4
        jsr     LDB6D
        sty     $BF
        lda     $B5
        sty     $B5
        eor     #$80
        rol     a
        lda     #$A0
        sta     $B0
        lda     $B4
        sta     $5A
        jmp     LD7A7

LDBBB:  sta     $B1
        sta     $B2
        sta     $B3
        sta     $B4
        tay
LDBC4:  rts

        ldy     #$00
        ldx     #$0A
LDBC9:  sty     $AC,x
        dex
        bpl     LDBC9
        bcc     LDBDF
        cmp     #$2D
        bne     LDBD8
        stx     $B6
        beq     LDBDC
LDBD8:  cmp     #$2B
        bne     LDBE1
LDBDC:  jsr     L00C2
LDBDF:  bcc     LDC3C
LDBE1:  cmp     #$2E
        beq     LDC13
        cmp     #$45
        bne     LDC19
        jsr     L00C2
        bcc     LDC05
        cmp     #$AB
        beq     LDC00
        cmp     #$2D
        beq     LDC00
        cmp     #$AA
        beq     LDC02
        cmp     #$2B
        beq     LDC02
        bne     LDC07
LDC00:  ror     $AF
LDC02:  jsr     L00C2
LDC05:  bcc     LDC63
LDC07:  bit     $AF
        bpl     LDC19
        lda     #$00
        sec
        sbc     $AD
        jmp     LDC1B

LDC13:  ror     $AE
        bit     $AE
        bvc     LDBDC
LDC19:  lda     $AD
LDC1B:  sec
        sbc     $AC
        sta     $AD
        beq     LDC34
        bpl     LDC2D
LDC24:  jsr     LD9D0
        inc     $AD
        bne     LDC24
        beq     LDC34
LDC2D:  jsr     LD9B4
        dec     $AD
        bne     LDC2D
LDC34:  lda     $B6
        bmi     LDC39
        rts

LDC39:  jmp     LDE67

LDC3C:  pha
        bit     $AE
        bpl     LDC43
        inc     $AC
LDC43:  jsr     LD9B4
        pla
        sec
        sbc     #$30
        jsr     LDC50
        jmp     LDBDC

LDC50:  pha
        jsr     LDADE
        pla
        jsr     LDB0E
        lda     $BD
        eor     $B5
        sta     $BE
        ldx     $B0
        jmp     LD73F

LDC63:  lda     $AD
        cmp     #$0C
        bcc     LDC72
        bit     $AF
        bmi     LDC70
        jmp     LD853

LDC70:  lda     #$0B
LDC72:  asl     a
        asl     a
        clc
        adc     $AD
        asl     a
        clc
        ldy     #$00
        adc     ($C9),y
        sec
        sbc     #$30
        sta     $AD
        jmp     LDC02

        .byte   $9B
        rol     $1FBC,x
        sbc     $6E9E,x
        .byte   $6B
        .byte   $27
        inc     $6E9E,x
        .byte   $6B
        plp
        brk
        lda     #$94
        ldy     #$C2
        jsr     LDCAC
        lda     $89
        ldx     $88
        sta     $B1
        stx     $B2
        ldx     #$90
        sec
        jsr     LDB1B
        jsr     LDCAF
LDCAC:  jmp     LCA27

LDCAF:  ldy     #$01
        lda     #$20
        bit     $B5
        bpl     LDCB9
        lda     #$2D
LDCB9:  sta     $FF,y
        sta     $B5
        sty     $C0
        iny
        lda     #$30
        ldx     $B0
        bne     LDCCA
        jmp     LDDD6

LDCCA:  lda     #$00
        cpx     #$80
        beq     LDCD2
        bcs     LDCDB
LDCD2:  lda     #$8F
        ldy     #$DC
        jsr     LD8FD
        lda     #$F7
LDCDB:  sta     $AC
LDCDD:  lda     #$8A
        ldy     #$DC
        jsr     LDB2D
        beq     LDD04
        bpl     LDCFA
LDCE8:  lda     #$85
        ldy     #$DC
        jsr     LDB2D
        beq     LDCF3
        bpl     LDD01
LDCF3:  jsr     LD9B4
        dec     $AC
        bne     LDCE8
LDCFA:  jsr     LD9D0
        inc     $AC
        bne     LDCDD
LDD01:  jsr     LD71E
LDD04:  jsr     LDB6D
        ldx     #$01
        lda     $AC
        clc
        adc     #$0A
        bmi     LDD19
        cmp     #$0B
        bcs     LDD1A
        adc     #$FF
        tax
        lda     #$02
LDD19:  sec
LDD1A:  sbc     #$02
        sta     $AD
        stx     $AC
        txa
        beq     LDD25
        bpl     LDD38
LDD25:  ldy     $C0
        lda     #$2E
        iny
        sta     $FF,y
        txa
        beq     LDD36
        lda     #$30
        iny
        sta     $FF,y
LDD36:  sty     $C0
LDD38:  ldy     #$00
        ldx     #$80
LDD3C:  lda     $B4
        clc
        adc     LDDEB,y
        sta     $B4
        lda     $B3
        adc     LDDEA,y
        sta     $B3
        lda     $B2
        adc     LDDE9,y
        sta     $B2
        lda     $B1
        adc     LDDE8,y
        sta     $B1
        inx
        bcs     LDD60
        bpl     LDD3C
        bmi     LDD62
LDD60:  bmi     LDD3C
LDD62:  txa
        bcc     LDD69
        eor     #$FF
        adc     #$0A
LDD69:  adc     #$2F
        iny
        iny
        iny
        iny
        sty     $96
        ldy     $C0
        iny
        tax
        and     #$7F
        sta     $FF,y
        dec     $AC
        bne     LDD84
        lda     #$2E
        iny
        sta     $FF,y
LDD84:  sty     $C0
        ldy     $96
        txa
        eor     #$FF
        and     #$80
        tax
        cpy     #$24
        beq     LDD96
        cpy     #$3C
        bne     LDD3C
LDD96:  ldy     $C0
LDD98:  lda     $FF,y
        dey
        cmp     #$30
        beq     LDD98
        cmp     #$2E
        beq     LDDA5
        iny
LDDA5:  lda     #$2B
        ldx     $AD
        beq     LDDD9
        bpl     LDDB5
        lda     #$00
        sec
LDDB0:  sbc     $AD
        tax
        lda     #$2D
LDDB5:  sta     $0101,y
        lda     #$45
        sta     $0100,y
        txa
        ldx     #$2F
        sec
LDDC1:  inx
        sbc     #$0A
        bcs     LDDC1
        adc     #$3A
        sta     $0103,y
        txa
        sta     $0102,y
        lda     #$00
        sta     $0104,y
        beq     LDDDE
LDDD6:  sta     $FF,y
LDDD9:  lda     #$00
        sta     $0100,y
LDDDE:  lda     #$00
        ldy     #$01
        rts

        .byte   $80
        brk
        brk
        brk
        brk
LDDE8:  .byte   $FA
LDDE9:  asl     a
LDDEA:  .byte   $1F
LDDEB:  brk
        brk
        tya
        stx     $80,y
        .byte   $FF
        beq     LDDB0
        cpy     #$00
        ora     ($86,x)
        ldy     #$FF
        .byte   $FF
        cld
        beq     LDDFD
LDDFD:  brk
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
LDE1D:  .byte   $FF
        sbc     a:$A8,x
        brk
        brk
        .byte   $3C
        jsr     LDADE
        lda     #$E3
        ldy     #$DD
        jsr     LDA74
        beq     LDEA0
        lda     $B8
        bne     LDE37
        jmp     LD7CE

LDE37:  ldx     #$9D
        ldy     #$00
        jsr     LDAA6
        lda     $BD
        bpl     LDE51
        jsr     LDB9E
        lda     #$9D
        ldy     #$00
        jsr     LDB2D
        bne     LDE51
        tya
        ldy     $5A
LDE51:  jsr     LDAD0
        tya
        pha
        jsr     LD8BF
        lda     #$9D
        ldy     #$00
        jsr     LD8FD
        jsr     LDEA0
        pla
        lsr     a
        bcc     LDE71
LDE67:  lda     $B0
        beq     LDE71
        lda     $B5
        eor     #$FF
        sta     $B5
LDE71:  rts

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
        dec     $80
        and     ($72),y
        clc
        bpl     LDE1D
        brk
        brk
        brk
        brk
LDEA0:  lda     #$72
        ldy     #$DE
        jsr     LD8FD
        lda     $BF
        adc     #$50
        bcc     LDEB0
        jsr     LDAF5
LDEB0:  sta     $A5
        jsr     LDAE1
        lda     $B0
        cmp     #$88
        bcc     LDEBE
LDEBB:  jsr     LD9A6
LDEBE:  jsr     LDB9E
        lda     $5A
        clc
        adc     #$81
        beq     LDEBB
        sec
        sbc     #$01
        pha
        ldx     #$05
LDECE:  lda     $B8,x
        ldy     $B0,x
        sta     $B0,x
        sty     $B8,x
        dex
        bpl     LDECE
        lda     $A5
        sta     $BF
        jsr     LD728
        jsr     LDE67
        lda     #$77
        ldy     #$DE
        jsr     LDF09
        lda     #$00
        sta     $BE
        pla
        jsr     LD98B
        rts

LDEF3:  sta     $C0
        sty     $C1
        jsr     LDA9C
        lda     #$A6
        jsr     LD8FD
        jsr     LDF0D
        lda     #$A6
        ldy     #$00
        jmp     LD8FD

LDF09:  sta     $C0
        sty     $C1
LDF0D:  jsr     LDA99
        lda     ($C0),y
        sta     $B6
        ldy     $C0
        iny
        tya
        bne     LDF1C
        inc     $C1
LDF1C:  sta     $C0
        ldy     $C1
LDF20:  jsr     LD8FD
        lda     $C0
        ldy     $C1
        clc
        adc     #$05
        bcc     LDF2D
        iny
LDF2D:  sta     $C0
        sty     $C1
        jsr     LD73C
        lda     #$AB
        ldy     #$00
        dec     $B6
        bne     LDF20
        rts

        tya
        and     $44,x
        .byte   $7A
        pla
        plp
        lda     ($46),y
        jsr     LDAFD
        bmi     LDF78
        bne     LDF63
        lda     $9044
        sta     $B1
        lda     $9048
        sta     $B2
        lda     $9045
        sta     $B3
        lda     $9049
        sta     $B4
        jmp     LDF88

LDF63:  lda     #$DA
        ldy     #$00
        jsr     LDA74
        lda     #$3D
        ldy     #$DF
        jsr     LD8FD
        lda     #$41
        ldy     #$DF
        jsr     LD73C
LDF78:  ldx     $B4
        lda     $B1
        sta     $B4
        stx     $B1
        ldx     $B2
        lda     $B3
        sta     $B2
        stx     $B3
LDF88:  lda     #$00
        sta     $B5
        lda     $B0
        sta     $BF
        lda     #$80
        sta     $B0
        jsr     LD7AC
        ldx     #$DA
        ldy     #$00
LDF9B:  jmp     LDAA6

        lda     #$1A
        ldy     #$E0
        jsr     LD73C
LDFA5:  jsr     LDADE
        lda     #$1F
        ldy     #$E0
        ldx     $BD
        jsr     LD9D9
        jsr     LDADE
        jsr     LDB9E
        lda     #$00
        sta     $BE
        jsr     LD728
        lda     #$24
        ldy     #$E0
        jsr     LD725
        lda     $B5
        pha
        bpl     LDFD7
        jsr     LD71E
        lda     $B5
        bmi     LDFDA
        lda     $63
        eor     #$FF
        sta     $63
LDFD7:  jsr     LDE67
LDFDA:  lda     #$24
        ldy     #$E0
        jsr     LD73C
        pla
        bpl     LDFE7
        jsr     LDE67
LDFE7:  lda     #$29
        ldy     #$E0
        jmp     LDEF3

        jsr     LDA9C
        lda     #$00
        sta     $63
        jsr     LDFA5
        ldx     #$9D
        ldy     #$00
        jsr     LDF9B
        .byte   $A9
