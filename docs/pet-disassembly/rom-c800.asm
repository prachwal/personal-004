; da65 V2.18 - Ubuntu 2.19-1
; Created:    2026-09-09 23:08:38
; Input file: roms/pet/pet-2001-8/rom-1-c800.901439-05.bin
; Page:       1


        .setcpu "6502"

L0071           := $0071
L00A3           := $00A3
L00C2           := $00C2
L00C8           := $00C8
L00CF           := $00CF
L5246           := $5246
LC2AC           := $C2AC
LC31D           := $C31D
LC359           := $C359
LC468           := $C468
LC6B5           := $C6B5
LC6F2           := $C6F2
LC6F5           := $C6F5
LC717           := $C717
LC79D           := $C79D
LC7E0           := $C7E0
LC7F0           := $C7F0
LC7F3           := $C7F3
LC7FD           := $C7FD
LC7FE           := $C7FE
LD005           := $D005
LD00F           := $D00F
LD079           := $D079
LD0A7           := $D0A7
LD0B9           := $D0B9
LD130           := $D130
LD278           := $D278
LD28B           := $D28B
LD2D6           := $D2D6
LD353           := $D353
LD359           := $D359
LD36B           := $D36B
LD371           := $D371
LD515           := $D515
LD552           := $D552
LD57E           := $D57E
LD582           := $D582
LD5B3           := $D5B3
LD673           := $D673
LD676           := $D676
LD6BB           := $D6BB
LD73C           := $D73C
LD9B4           := $D9B4
LD9BF           := $D9BF
LDA74           := $DA74
LDAA2           := $DAA2
LDACE           := $DACE
LDADE           := $DADE
LDAED           := $DAED
LDB0E           := $DB0E
LDB21           := $DB21
LDB2D           := $DB2D
LDB2F           := $DB2F
LDB6D           := $DB6D
LDBC5           := $DBC5
LDC50           := $DC50
LDCAF           := $DCAF
LDD3A           := $DD3A
LE19B           := $E19B
LE19F           := $E19F
LE1AB           := $E1AB
LE1BC           := $E1BC
LE1C2           := $E1C2
LE1CC           := $E1CC
LFFC6           := $FFC6
LFFC9           := $FFC9
LFFCC           := $FFCC
LFFD2           := $FFD2
LFFE4           := $FFE4
        .byte   $2C
LC801:  ldx     #$00
        stx     $5A
        ldy     #$00
        sty     $5B
LC809:  lda     $5B
        ldx     $5A
        sta     $5A
        stx     $5B
LC811:  lda     ($C9),y
        beq     LC7FD
        cmp     $5B
        beq     LC7FD
        iny
        cmp     #$22
        beq     LC809
        bne     LC811
        jsr     LCCB8
        jsr     L00C8
        cmp     #$89
        beq     LC82F
        lda     #$A7
        jsr     LCE13
LC82F:  lda     $B0
        bne     LC838
        jsr     LC801
        beq     LC7F3
LC838:  jsr     L00C8
        bcs     LC840
        jmp     LC79D

LC840:  jmp     LC6F2

        jsr     LD676
        pha
        cmp     #$8D
        beq     LC84F
LC84B:  cmp     #$89
        bne     LC7E0
LC84F:  dec     $B4
        bne     LC857
        pla
        jmp     LC6F5

LC857:  jsr     L00C2
        jsr     LC863
        cmp     #$2C
        beq     LC84F
        pla
LC862:  rts

LC863:  ldx     #$00
        stx     $08
        stx     $09
LC869:  bcs     LC862
        sbc     #$2F
        sta     $5A
        lda     $09
        sta     L0071
        cmp     #$19
        bcs     LC84B
        lda     $08
        asl     a
        rol     L0071
        asl     a
        rol     L0071
        adc     $08
        sta     $08
        lda     L0071
        adc     $09
        sta     $09
        asl     $08
        rol     $09
        lda     $08
        adc     $5A
        sta     $08
        bcc     LC897
        inc     $09
LC897:  jsr     L00C2
        jmp     LC869

        jsr     LCF7B
        sta     $98
        sty     $99
        lda     #$B2
        jsr     LCE13
        lda     $5F
        pha
        lda     $5E
        pha
        jsr     LCCB8
        pla
        rol     a
        jsr     LCCAA
        bne     LC8D1
        pla
LC8BA:  bpl     LC8CE
        jsr     LDAED
        jsr     LD0A7
        ldy     #$00
        lda     $B3
        sta     ($98),y
        iny
        lda     $B4
        sta     ($98),y
        rts

LC8CE:  jmp     LDAA2

LC8D1:  pla
LC8D2:  ldy     $99
        cpy     #$D0
        bne     LC92B
        jsr     LD57E
        cmp     #$06
        beq     LC8E2
        jmp     LD130

LC8E2:  ldy     #$00
        sty     $B0
        sty     $B5
LC8E8:  sty     $C0
        jsr     LC91C
        jsr     LD9B4
        inc     $C0
        ldy     $C0
        jsr     LC91C
        jsr     LDADE
        tax
        beq     LC902
        inx
        txa
        jsr     LD9BF
LC902:  ldy     $C0
        iny
        cpy     #$06
        bne     LC8E8
        jsr     LD9B4
        jsr     LDB6D
        ldx     #$02
        sei
LC912:  lda     $B2,x
        sta     $0200,x
        dex
        bpl     LC912
        cli
        rts

LC91C:  lda     (L0071),y
        jsr     L00CF
        bcc     LC926
        jmp     LD130

LC926:  sbc     #$2F
        jmp     LDC50

LC92B:  ldy     #$02
        lda     ($B3),y
        cmp     $83
        bcc     LC94A
        bne     LC93C
        dey
        lda     ($B3),y
        cmp     $82
        bcc     LC94A
LC93C:  ldy     $B4
        cpy     $7D
        bcc     LC94A
        bne     LC951
        lda     $B3
        cmp     $7C
        bcs     LC951
LC94A:  lda     $B3
        ldy     $B4
        jmp     LC967

LC951:  ldy     #$00
        lda     ($B3),y
        jsr     LD359
        lda     $9F
        ldy     $A0
        sta     $BE
        sty     $BF
        jsr     LD552
        lda     #$B0
        ldy     #$00
LC967:  sta     $9F
        sty     $A0
        jsr     LD5B3
        ldy     #$00
        lda     ($9F),y
        sta     ($98),y
        iny
        lda     ($9F),y
        sta     ($98),y
        iny
        lda     ($9F),y
        sta     ($98),y
        rts

        jsr     LC985
        jmp     LCAD6

LC985:  jsr     LD676
        beq     LC98F
        lda     #$2C
        jsr     LCE13
LC98F:  php
        jsr     LFFC9
        stx     $03
        plp
        jmp     LC99F

LC999:  jsr     LCA2A
LC99C:  jsr     L00C8
LC99F:  beq     LC9D2
LC9A1:  beq     LC9F8
        cmp     #$A3
        beq     LCA06
        cmp     #$A6
        beq     LCA06
        cmp     #$2C
        beq     LC9F9
        cmp     #$3B
        beq     LCA21
        jsr     LCCB8
        bit     $5E
        bmi     LC999
        jsr     LDCAF
        jsr     LD36B
        jsr     LCA2A
        jsr     LCA44
        bne     LC99C
        ldy     #$00
        sty     $0A,x
        ldx     #$09
        lda     $03
        bne     LC9F8
LC9D2:  lda     $03
        bne     LC9D8
        sta     $05
LC9D8:  lda     #$0D
        jsr     LCA49
        lda     #$0A
        jsr     LCA49
LC9E2:  lda     $03
        bne     LC9F8
        txa
        pha
        ldx     $04
        beq     LC9F4
        lda     #$00
LC9EE:  jsr     LCA49
        dex
        bne     LC9EE
LC9F4:  stx     $05
        pla
        tax
LC9F8:  rts

LC9F9:  lda     $05
        sec
LC9FC:  sbc     #$0A
        bcs     LC9FC
        eor     #$FF
        adc     #$01
        bne     LCA1A
LCA06:  pha
        jsr     LD673
        cmp     #$29
        bne     LCA83
        pla
        cmp     #$A3
        bne     LCA1B
        txa
        sbc     $05
        bcc     LCA21
        beq     LCA21
LCA1A:  tax
LCA1B:  jsr     LCA44
        dex
        bne     LCA1B
LCA21:  jsr     L00C2
        jmp     LC9A1

LCA27:  jsr     LD36B
LCA2A:  jsr     LD57E
        tax
        ldy     #$00
        inx
LCA31:  dex
        beq     LC9F8
        lda     (L0071),y
        jsr     LCA49
        iny
        cmp     #$0D
        bne     LCA31
        jsr     LC9E2
        jmp     LCA31

LCA44:  lda     #$1D
        .byte   $2C
LCA47:  lda     #$3F
LCA49:  bit     $64
        bmi     LCA74
        pha
        cmp     #$1D
        beq     LCA6A
        cmp     #$9D
        beq     LCA5A
        cmp     #$14
        bne     LCA64
LCA5A:  lda     $05
        beq     LCA70
        lda     $03
        bne     LCA70
        dec     $05
LCA64:  and     #$7F
        cmp     #$20
        bcc     LCA70
LCA6A:  lda     $03
        jsr     LE1CC
        nop
LCA70:  pla
        jsr     LFFD2
LCA74:  and     #$FF
        rts

LCA77:  lda     $62
        beq     LCA86
        jsr     LE1C2
        nop
        sta     $88
        sty     $89
LCA83:  jmp     LCE1C

LCA86:  lda     $03
        beq     LCA8F
        ldx     #$C4
        jmp     LC359

LCA8F:  lda     #$23
        ldy     #$CC
        jsr     LCA27
        lda     $8C
        ldy     $8D
        sta     $C9
        sty     $CA
        rts

        jsr     LD28B
        cmp     #$23
        bne     LCAB6
        jsr     L00C2
        jsr     LD676
        lda     #$2C
        jsr     LCE13
        jsr     LFFC6
        stx     $03
LCAB6:  ldx     #$0B
        ldy     #$00
        sty     $0B
        lda     #$40
        jsr     LCB2A
        ldx     $03
        bne     LCAD8
        rts

        jsr     LD676
        lda     #$2C
        jsr     LCE13
        jsr     LFFC6
        stx     $03
        jsr     LCAF1
LCAD6:  lda     $03
LCAD8:  jsr     LFFCC
        ldx     #$00
        stx     $03
        rts

        lsr     $64
        cmp     #$22
        bne     LCAF1
        jsr     LCDD1
        lda     #$3B
        jsr     LCE13
        jsr     LCA2A
LCAF1:  jsr     LD28B
        lda     #$2C
        sta     $09
LCAF8:  jsr     LCB17
        lda     $03
        beq     LCB0C
        lda     $020C
        and     #$02
        beq     LCB0C
        jsr     LCAD6
        jmp     LC7F0

LCB0C:  lda     $0A
        bne     LCB29
        lda     $03
        bne     LCAF8
        jmp     LE19B

LCB17:  lda     $03
        bne     LCB21
        jsr     LCA47
        jsr     LCA44
LCB21:  jmp     LC468

        ldx     $90
        ldy     $91
        .byte   $A9
LCB29:  tya
LCB2A:  sta     $62
        stx     $92
        sty     $93
LCB30:  jsr     LCF7B
        sta     $98
        sty     $99
        lda     $C9
        ldy     $CA
        sta     $08
        sty     $09
        ldx     $92
        ldy     $93
        stx     $C9
        sty     $CA
        jsr     L00C8
        bne     LCB6B
        bit     $62
        bvc     LCB5B
        jsr     LFFE4
        sta     $0A
        ldy     #$00
        ldx     #$09
        bne     LCB67
LCB5B:  bmi     LCBCF
        lda     $03
        bne     LCB64
        jsr     LCA47
LCB64:  jsr     LCB17
LCB67:  stx     $C9
        sty     $CA
LCB6B:  jsr     L00C2
        bit     $5E
        bpl     LCBA0
        bit     $62
        bvc     LCB7C
        lda     #$00
        jsr     LE1BC
        nop
LCB7C:  sta     $5A
        cmp     #$22
        beq     LCB89
        lda     #$3A
        sta     $5A
        lda     #$2C
        clc
LCB89:  sta     $5B
        lda     $C9
        ldy     $CA
        adc     #$00
        bcc     LCB94
        iny
LCB94:  jsr     LD371
        jsr     LD6BB
        jsr     LC8D2
        jmp     LCBA8

LCBA0:  jsr     LDBC5
        lda     $5F
        jsr     LC8BA
LCBA8:  jsr     L00C8
        beq     LCBB4
        cmp     #$2C
        beq     LCBB4
        jmp     LCA77

LCBB4:  lda     $C9
        ldy     $CA
        sta     $92
        sty     $93
        lda     $08
        ldy     $09
        sta     $C9
        sty     $CA
        jsr     L00C8
        beq     LCBF5
        jsr     LCE11
        jmp     LCB30

LCBCF:  jsr     LC7FE
        iny
        tax
        bne     LCBE8
        ldx     #$2A
        iny
        lda     ($C9),y
        beq     LCC4A
        iny
        lda     ($C9),y
        sta     $8E
        iny
        lda     ($C9),y
        iny
        sta     $8F
LCBE8:  lda     ($C9),y
        tax
        jsr     LC7F3
        cpx     #$83
        bne     LCBCF
        jmp     LCB6B

LCBF5:  lda     $92
        ldy     $93
        ldx     $62
        bpl     LCC00
        jmp     LC717

LCC00:  ldy     #$00
        lda     ($92),y
        beq     LCC11
        lda     $03
        bne     LCC11
        lda     #$12
        ldy     #$CC
        jmp     LCA27

LCC11:  rts

        .byte   $3F
        eor     $58
        .byte   $54
        .byte   $52
        eor     ($20,x)
        eor     #$47
        lsr     $524F
        eor     $44
        ora     a:$0A
        .byte   $3F
        .byte   $52
        eor     $44
        .byte   $4F
        jsr     L5246
        .byte   $4F
        eor     $5320
        .byte   $54
        eor     ($52,x)
        .byte   $54
        ora     a:$0A
        bne     LCC3C
        ldy     #$00
        beq     LCC3F
LCC3C:  jsr     LCF7B
LCC3F:  sta     $98
        sty     $99
        jsr     LC2AC
        beq     LCC4C
        ldx     #$00
LCC4A:  beq     LCCB5
LCC4C:  txs
        inx
        inx
        inx
        inx
        txa
        inx
        inx
        inx
        inx
        inx
        inx
        stx     $73
        ldy     #$01
        jsr     LDA74
        tsx
        lda     $0109,x
        sta     $B5
        lda     $98
        ldy     $99
        jsr     LD73C
        jsr     LDAA2
        ldy     #$01
        jsr     LDB2F
        tsx
        sec
        sbc     $0109,x
        beq     LCC92
        lda     $010F,x
        sta     $88
        lda     $0110,x
        sta     $89
        lda     $0112,x
        sta     $C9
        lda     $0111,x
        sta     $CA
LCC8F:  jmp     LC6B5

LCC92:  txa
        adc     #$11
        tax
        txs
        jsr     L00C8
        cmp     #$2C
        bne     LCC8F
        jsr     L00C2
        jsr     LCC3C
        jsr     LCCB8
LCCA7:  clc
        .byte   $24
LCCA9:  sec
LCCAA:  bit     $5E
        bmi     LCCB1
        bcs     LCCB3
LCCB0:  rts

LCCB1:  bcs     LCCB0
LCCB3:  ldx     #$A8
LCCB5:  jmp     LC359

LCCB8:  ldx     $C9
        bne     LCCBE
        dec     $CA
LCCBE:  dec     $C9
        ldx     #$00
        .byte   $24
LCCC3:  pha
        txa
        pha
        lda     #$01
        jsr     LC31D
        jsr     LCD9D
        lda     #$00
        sta     $9C
        jsr     L00C8
LCCD5:  sec
        sbc     #$B1
        bcc     LCCF1
        cmp     #$03
        bcs     LCCF1
        cmp     #$01
        rol     a
        eor     #$01
        eor     $9C
        cmp     $9C
        bcc     LCD4A
        sta     $9C
        jsr     L00C2
        jmp     LCCD5

LCCF1:  ldx     $9C
        bne     LCD21
        bcs     LCD72
        adc     #$07
        bcc     LCD72
        adc     $5E
        bne     LCD02
        jmp     LD515

LCD02:  adc     #$FF
        sta     L0071
        asl     a
        adc     L0071
        tay
LCD0A:  pla
        cmp     $C074,y
        bcs     LCD77
        jsr     LCCA7
LCD13:  pha
LCD14:  jsr     LCD3A
        pla
        ldy     $9A
        bpl     LCD33
        tax
        beq     LCD75
        bne     LCD80
LCD21:  lsr     $5E
        txa
        rol     a
        ldx     $C9
        bne     LCD2B
        dec     $CA
LCD2B:  dec     $C9
        ldy     #$1B
        sta     $9C
        bne     LCD0A
LCD33:  cmp     $C074,y
        bcs     LCD80
        bcc     LCD13
LCD3A:  lda     $C076,y
        pha
        lda     $C075,y
        pha
        jsr     LCD4D
        lda     $9C
        jmp     LCCC3

LCD4A:  jmp     LCE1C

LCD4D:  lda     $B5
        ldx     $C074,y
        tay
        pla
        sta     L0071
        inc     L0071
        pla
        sta     $72
        tya
        pha
        jsr     LDAED
        lda     $B4
        pha
        lda     $B3
        pha
        lda     $B2
        pha
        lda     $B1
        pha
        lda     $B0
        pha
        jmp     (L0071)

LCD72:  ldy     #$FF
        pla
LCD75:  beq     LCD9A
LCD77:  cmp     #$64
        beq     LCD7E
        jsr     LCCA7
LCD7E:  sty     $9A
LCD80:  pla
        lsr     a
        sta     $63
        pla
        sta     $B8
        pla
        sta     $B9
        pla
        sta     $BA
        pla
        sta     $BB
        pla
        sta     $BC
        pla
        sta     $BD
        eor     $B5
        sta     $BE
LCD9A:  lda     $B0
        rts

LCD9D:  lda     #$00
        sta     $5E
LCDA1:  jsr     L00C2
        bcs     LCDA9
LCDA6:  jmp     LDBC5

LCDA9:  jsr     LD005
        bcs     LCE28
        cmp     #$FF
        bne     LCDC1
        lda     #$BC
        ldy     #$CD
        jsr     LDA74
        jmp     L00C2

        .byte   $82
        eor     #$0F
        .byte   $DA
        .byte   $A1
LCDC1:  cmp     #$2E
        beq     LCDA6
        cmp     #$AB
        beq     LCE21
        cmp     #$AA
        beq     LCDA1
        cmp     #$22
        bne     LCDE0
LCDD1:  lda     $C9
        ldy     $CA
        adc     #$00
        bcc     LCDDA
        iny
LCDDA:  jsr     LD36B
        jmp     LD6BB

LCDE0:  cmp     #$A8
        bne     LCDF7
        ldy     #$18
        bne     LCE23
        jsr     LD0A7
        lda     $B4
        eor     #$FF
        tay
        lda     $B3
        eor     #$FF
        jmp     LD278

LCDF7:  cmp     #$A5
        bne     LCDFE
        jmp     LD2D6

LCDFE:  cmp     #$B4
        bcc     LCE05
        jmp     LCE97

LCE05:  jsr     LCE0E
        jsr     LCCB8
        lda     #$29
        .byte   $2C
LCE0E:  lda     #$28
        .byte   $2C
LCE11:  lda     #$2C
LCE13:  ldy     #$00
        cmp     ($C9),y
        bne     LCE1C
        jmp     L00C2

LCE1C:  ldx     #$10
        jmp     LC359

LCE21:  ldy     #$15
LCE23:  pla
        pla
        jmp     LCD14

LCE28:  jsr     LCF7B
        sta     $B3
        sty     $B4
        lda     $94
        ldy     $95
        ldx     $5E
        beq     LCE54
        jmp     LE19F

        clc
        cpy     #$C9
        bne     LCE53
        jsr     LCE76
        sty     $AD
        dey
        sty     $C0
        ldy     #$06
        sty     $AC
        ldy     #$24
        jsr     LDD3A
        jmp     LD353

LCE53:  rts

LCE54:  ldx     $5F
        bpl     LCE65
        ldy     #$00
        lda     ($B3),y
        tax
        iny
        lda     ($B3),y
        tay
        txa
        jmp     LD278

LCE65:  jmp     LE1AB

        ora     $49C0,y
        bne     LCE82
        jsr     LCE76
        tya
        ldx     #$A0
        jmp     LDB21

LCE76:  lda     #$FE
        ldy     #$01
        sei
        jsr     LDA74
        cli
        sty     $B1
        rts

LCE82:  cmp     #$53
        bne     LCE90
        cpy     #$54
        bne     LCE90
        lda     $020C
        jmp     LDB0E

LCE90:  lda     $B3
        ldy     $B4
        jmp     LDA74

LCE97:  asl     a
        pha
        tax
        jsr     L00C2
        cpx     #$8F
        bcc     LCEC1
        jsr     LCE0E
        jsr     LCCB8
        jsr     LCE11
        jsr     LCCA9
        pla
        tax
        lda     $B4
        pha
        lda     $B3
        pha
        txa
        pha
        jsr     LD676
        pla
        tay
        txa
        pha
        jmp     LCEC6

LCEC1:  jsr     LCE05
        pla
        tay
LCEC6:  lda     $BFDE,y
        sta     $A4
        lda     $BFDF,y
        sta     $A5
        jsr     L00A3
        jmp     LCCA7

        ldy     #$FF
        bit     a:$A0
        sty     $5C
        jsr     LD0A7
        lda     $B3
        eor     $5C
        sta     $5A
        lda     $B4
        eor     $5C
        sta     $5B
        jsr     LDACE
        jsr     LD0A7
        lda     $B4
        eor     $5C
        and     $5B
        eor     $5C
        tay
        lda     $B3
        eor     $5C
        and     $5A
        eor     $5C
        jmp     LD278

        jsr     LCCAA
        bcs     LCF1E
        lda     $BD
        ora     #$7F
        and     $B9
        sta     $B9
        lda     #$B8
        ldy     #$00
        jsr     LDB2D
        tax
        jmp     LCF51

LCF1E:  lda     #$00
        sta     $5E
        dec     $9C
        jsr     LD57E
        sta     $B0
        stx     $B1
        sty     $B2
        lda     $BB
        ldy     $BC
        jsr     LD582
        stx     $BB
        sty     $BC
        tax
        sec
        sbc     $B0
        beq     LCF46
        lda     #$01
        bcc     LCF46
        ldx     $B0
        lda     #$FF
LCF46:  sta     $B5
        ldy     #$FF
        inx
LCF4B:  iny
        dex
        bne     LCF56
        ldx     $B5
LCF51:  bmi     LCF62
        clc
        bcc     LCF62
LCF56:  lda     ($BB),y
        cmp     ($B1),y
        beq     LCF4B
        ldx     #$FF
        bcs     LCF62
        ldx     #$01
LCF62:  inx
        txa
        rol     a
        and     $63
        beq     LCF6B
        lda     #$FF
LCF6B:  jmp     LDB0E

LCF6E:  jsr     LCE11
        tax
        jsr     LCF80
        jsr     L00C8
        bne     LCF6E
        rts

LCF7B:  ldx     #$00
        jsr     L00C8
LCF80:  stx     $5D
        sta     $94
        jsr     L00C8
        jsr     LD005
        bcs     LCF8F
LCF8C:  jmp     LCE1C

LCF8F:  ldx     #$00
        stx     $5E
        stx     $5F
        jsr     L00C2
        bcc     LCF9F
        jsr     LD005
        bcc     LCFAA
LCF9F:  tax
LCFA0:  jsr     L00C2
        bcc     LCFA0
        jsr     LD005
        bcs     LCFA0
LCFAA:  cmp     #$24
        bne     LCFB4
        lda     #$FF
        sta     $5E
        bne     LCFC4
LCFB4:  cmp     #$25
        bne     LCFCB
        lda     $61
        bne     LCF8C
        lda     #$80
        sta     $5F
        ora     $94
        sta     $94
LCFC4:  txa
        ora     #$80
        tax
        jsr     L00C2
LCFCB:  stx     $95
        sec
        ora     $61
        sbc     #$28
        bne     LCFD7
        jmp     LD0B9

LCFD7:  lda     #$00
        sta     $61
        lda     $7C
        ldx     $7D
        ldy     #$00
        stx     $AF
        sta     $AE
        cpx     $7F
        bne     LCFED
        cmp     $7E
        beq     LD00F
LCFED:  lda     $94
        cmp     ($AE),y
        bne     LCFFB
        lda     $95
        iny
        cmp     ($AE),y
        beq     LD079
        dey
LCFFB:  clc
        lda     $AE
        adc     #$07
