; da65 V2.18 - Ubuntu 2.19-1
; Created:    2026-09-09 23:08:28
; Input file: roms/pet/pet-2001-8/rom-1-f000.901439-04.bin
; Page:       1


        .setcpu "6502"

L0008           := $0008
L00C8           := $00C8
L0DA3           := $0DA3
L2045           := $2045
L414D           := $414D
L414F           := $414F
L4552           := $4552
L4946           := $4946
L4C50           := $4C50
L4E49           := $4E49
L4EC5           := $4EC5
L4F46           := $4F46
L504F           := $504F
L5250           := $5250
L554F           := $554F
LC379           := $C379
LC430           := $C430
LC581           := $C581
LC59A           := $C59A
LC71C           := $C71C
LCCA4           := $CCA4
LCCB8           := $CCB8
LCE13           := $CE13
LCE1C           := $CE1C
LD345           := $D345
LD57B           := $D57B
LD676           := $D676
LD6D0           := $D6D0
LE27D           := $E27D
LE2FA           := $E2FA
LE3EA           := $E3EA
LE7DE           := $E7DE
LF806           := $F806
LF80C           := $F80C
LF82D           := $F82D
LF83B           := $F83B
LF871           := $F871
LF87F           := $F87F
LF88A           := $F88A
LF8B9           := $F8B9
LF8BC           := $F8BC
LF8C4           := $F8C4
LF913           := $F913
LFBDC           := $FBDC
LFBE5           := $FBE5
LFD90           := $FD90
        .byte   $54
        .byte   $4F
        .byte   $4F
        jsr     L414D
        lsr     $2059
        lsr     $49
        jmp     LD345

        lsr     $49
        jmp     L2045

        .byte   $4F
        bvc     LF05B
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
        cpy     $0D
        .byte   $53
        eor     $41
        .byte   $52
        .byte   $43
        pha
        eor     #$4E
        .byte   $47
        ldy     #$46
        .byte   $4F
        .byte   $52
        ldy     #$0D
        bvc     LF096
        eor     $53
        .byte   $53
        jsr     L4C50
        eor     ($59,x)
        ldy     #$26
        jsr     L4552
        .byte   $43
        .byte   $4F
        .byte   $52
        .byte   $44
        ldy     #$4F
        lsr     $5420
        .byte   $41
LF05B:  bvc     LF0A2
        jsr     L0DA3
        jmp     L414F

        cpy     $0D
        .byte   $57
        .byte   $52
        eor     #$54
        eor     #$4E
        .byte   $47
        ldy     #$0D
        lsr     $45,x
        .byte   $52
        eor     #$46
        cmp     $4544,y
        lsr     $49,x
        .byte   $43
        eor     $20
        lsr     $544F
        jsr     L5250
        eor     $53
        eor     $4E
        .byte   $D4
        lsr     $544F
        jsr     L4E49
        bvc     LF0E3
        .byte   $54
        jsr     L4946
        jmp     L4EC5

        .byte   $4F
LF096:  .byte   $54
        jsr     L554F
        .byte   $54
        bvc     LF0F2
        .byte   $54
        jsr     L4946
        .byte   $4C
LF0A2:  cmp     $0D
        lsr     $4F
        eor     $4E,x
        .byte   $44
        ldy     #$0D
        .byte   $4F
        .byte   $4B
        sta     $520D
        eor     $41
        .byte   $44
        eor     $8D2E,y
LF0B6:  lda     #$40
        bne     LF0BC
LF0BA:  lda     #$20
LF0BC:  pha
        lda     $E840
        ora     #$02
        sta     $E840
        lda     #$3C
        sta     $E821
        bit     $021D
        beq     LF0E1
        lda     #$34
        sta     $E811
        jsr     LF0F1
        lda     #$00
        sta     $021D
        lda     #$3C
        sta     $E811
LF0E1:  pla
        .byte   $05
LF0E3:  sbc     ($8D),y
        .byte   $22
        .byte   $02
LF0E7:  lda     $E840
        bpl     LF0E7
        and     #$FB
        sta     $E840
LF0F1:  .byte   $A9
LF0F2:  .byte   $3C
        sta     $E823
        lda     $E840
        and     #$41
        cmp     #$41
        beq     LF142
        lda     $0222
        eor     #$FF
        sta     $E822
LF107:  bit     $E840
        bvc     LF107
        lda     #$34
        sta     $E823
        lda     #$FF
        sta     $E845
LF116:  lda     $E840
        bit     $E84D
        bvs     LF13B
        lsr     a
        bcc     LF116
LF121:  lda     #$3C
        sta     $E823
        lda     #$FF
        sta     $E822
        rts

LF12C:  sta     $0222
        jsr     LF0F1
LF132:  lda     $E840
        ora     #$04
        sta     $E840
        rts

LF13B:  lda     #$01
LF13D:  jsr     LFBE5
        bne     LF121
LF142:  lda     #$80
        bmi     LF13D
LF146:  lda     #$02
        jsr     LFBE5
LF14B:  lda     $E840
        and     #$FD
        sta     $E840
        lda     #$34
        sta     $E821
        lda     #$0D
        rts

LF15B:  sta     $0222
        jsr     LF0F1
LF161:  jsr     LF14B
        jmp     LF132

LF167:  bit     $021D
        bmi     LF171
        dec     $021D
        bne     LF176
LF171:  pha
        jsr     LF0F1
        pla
LF176:  sta     $0222
        rts

LF17A:  lda     #$5F
        bne     LF180
LF17E:  lda     #$3F
LF180:  sta     $F1
        jsr     LF0BC
        bne     LF132
LF187:  lda     #$34
        sta     $E821
        lda     $E840
        ora     #$02
        sta     $E840
        lda     #$FF
        sta     $E845
LF199:  bit     $E84D
        bvs     LF146
        bit     $E840
        bmi     LF199
        lda     $E840
        and     #$FD
        sta     $E840
        bit     $E810
        bvs     LF1B5
        lda     #$40
        jsr     LFBE5
LF1B5:  lda     $E820
        eor     #$FF
        pha
        lda     #$3C
        sta     $E821
LF1C0:  bit     $E840
        bpl     LF1C0
        lda     #$34
        sta     $E821
        pla
        rts

        lda     #$00
        sta     $020C
        lda     $0263
        bne     LF1F1
        lda     $020D
        beq     LF22C
        sei
        jmp     LE27D

        lda     $0263
        bne     LF1F1
        lda     $E2
        sta     $0221
        lda     $F5
        sta     $0220
        jmp     LE2FA

LF1F1:  cmp     #$03
        bne     LF200
        sta     $0260
        lda     $F2
        sta     $021E
        jmp     LE2FA

LF200:  bcs     LF227
        stx     $0261
LF205:  jsr     LF82D
        bne     LF218
        jsr     LF87F
        ldy     #$00
        tya
        ldx     $F1
        sta     $0270,x
        jmp     LF205

LF218:  lda     ($F3),y
        bne     LF223
        lda     #$40
        jsr     LFBE5
        bne     LF205
LF223:  ldx     $0261
        rts

LF227:  lda     $020C
        beq     LF22D
LF22C:  rts

LF22D:  jmp     LF187

        pha
        lda     $0264
        bne     LF239
        jmp     LC379

LF239:  cmp     #$03
        bne     LF241
        pla
        jmp     LE3EA

LF241:  bmi     LF247
        pla
        jmp     LF167

LF247:  pla
LF248:  sta     $E9
        cmp     #$1D
        bne     LF253
        inc     $026A
        beq     LF22C
LF253:  cmp     #$0A
        beq     LF22C
        pha
        txa
        pha
        tya
        pha
        jsr     LF82D
        bne     LF273
        jsr     LF8B9
        ldx     $F1
        lda     #$01
        sta     $0270,x
        jsr     LF5E3
        lda     #$02
        sta     ($F3),y
        iny
LF273:  lda     $E9
        sta     ($F3),y
LF277:  pla
        tay
        pla
        tax
        pla
        rts

LF27D:  lda     $0264
        beq     LF28B
        cmp     #$03
        beq     LF28B
        bmi     LF28B
        jsr     LF17E
LF28B:  lda     $0263
        beq     LF299
        cmp     #$03
        beq     LF299
        bmi     LF299
        jsr     LF17A
LF299:  lda     #$00
        sta     $0263
        lda     #$03
        sta     $0264
        rts

        lda     #$00
        sta     $0262
        beq     LF27D
LF2AB:  ldx     $0262
LF2AE:  dex
        bmi     LF2C7
        cmp     $0242,x
        beq     LF2C7
        bne     LF2AE
LF2B8:  lda     $0242,x
        sta     $EF
        lda     $024C,x
        sta     $F1
        lda     $0256,x
        sta     $F0
LF2C7:  rts

        jsr     LF4D4
        lda     $EF
        jsr     LF2AB
        bne     LF329
        jsr     LF2B8
        txa
        pha
        lda     $F1
        beq     LF30A
        cmp     #$03
        beq     LF30A
        bcs     LF307
        lda     $F0
        beq     LF30A
        jsr     LF667
        ldx     #$02
LF2EA:  lda     LF304,x
        jsr     LF248
        dex
        bpl     LF2EA
        jsr     LF8B9
        lda     $F0
        cmp     #$02
        bne     LF30A
        lda     #$05
        jsr     LF5ED
        jmp     LF30A

LF304:  ora     a:$31
LF307:  jsr     LF6E6
LF30A:  pla
        tax
        dec     $0262
        cpx     $0262
        beq     LF329
        ldy     $0262
        lda     $0242,y
        sta     $0242,x
        lda     $024C,y
        sta     $024C,x
        lda     $0256,y
        sta     $0256,x
LF329:  rts

LF32A:  lda     $0209
        cmp     #$EF
        bne     LF338
        php
        lda     #$00
        sta     $020D
        plp
LF338:  rts

LF339:  jsr     LF32A
        jmp     LC71C

LF33F:  lda     $CA
        bne     LF338
        jmp     LE7DE

        lda     #$00
        sta     $020B
LF34B:  jsr     LF433
        lda     #$FF
LF350:  cmp     $0209
        bne     LF350
        cmp     $0209
        bne     LF350
        lda     #$04
        sta     $F8
        lda     #$00
        sta     $F7
        lda     $F1
        bne     LF369
LF366:  jmp     LCE1C

LF369:  cmp     #$03
        beq     LF366
        bcc     LF3A5
        jsr     LF71C
        jsr     LF3FF
        jsr     LF462
        jsr     LF0B6
        jsr     LF422
LF37E:  jsr     LF339
        jsr     LF187
        ldx     $020C
        bmi     LF3CC
        ldy     $020B
        beq     LF39A
        dey
        cmp     ($F7),y
        beq     LF39C
        ldx     #$02
        stx     $020C
        bne     LF39C
LF39A:  sta     ($F7),y
LF39C:  inc     $F7
        bne     LF37E
        inc     $F8
        jmp     LF37E

LF3A5:  jsr     LF667
        jsr     LF83B
        jsr     LF3FF
LF3AE:  lda     $EE
        beq     LF3BA
        jsr     LF495
        bne     LF3BF
LF3B7:  jmp     LF579

LF3BA:  jsr     LF5AE
        beq     LF3B7
LF3BF:  cpx     #$01
        bne     LF3AE
        jsr     LF64D
        jsr     LF422
        jsr     LF88A
LF3CC:  lda     $020B
        bne     LF421
        jsr     LF913
        lda     $020C
        and     #$10
        beq     LF3E5
        ldy     #$00
        sty     $020D
        ldy     #$60
        jmp     LF57B

LF3E5:  ldy     #$AE
        jsr     LF33F
        lda     $CA
        bne     LF3F9
        lda     $E6
        sta     $7D
        lda     $E5
        sta     $7C
        jmp     LC430

LF3F9:  jsr     LC59A
        jmp     LC581

LF3FF:  lda     $CA
        bne     LF421
        ldy     #$32
        jsr     LE7DE
        lda     $EE
        beq     LF421
        ldy     #$3D
        jsr     LE7DE
LF411:  ldy     $EE
        beq     LF421
        ldy     #$00
LF417:  lda     ($F9),y
        jsr     LE3EA
        iny
        cpy     $EE
        bne     LF417
LF421:  rts

LF422:  ldy     #$5F
        lda     $020B
        beq     LF42B
        ldy     #$6D
LF42B:  jsr     LF33F
        ldy     #$39
        jmp     LF33F

LF433:  ldx     #$00
        stx     $020C
        stx     $E5
        stx     $EE
        stx     $F0
        inx
        stx     $F1
        lda     #$04
        stx     $E6
        jsr     LF515
        jsr     LF504
        jsr     LF515
        jsr     LF45C
        stx     $F1
        jsr     LF515
        jsr     LF45C
        stx     $F0
LF45B:  rts

LF45C:  jsr     LF51D
        jmp     LD676

LF462:  lda     $F0
        bmi     LF45B
        ldy     $EE
        beq     LF45B
        jsr     LF0BA
        lda     $F0
        ora     #$40
        sta     $F0
        ora     #$F0
        jsr     LF12C
        lda     $020C
        bpl     LF482
LF47D:  ldy     #$74
        jmp     LF57B

LF482:  lda     $EE
        beq     LF492
        ldy     #$00
LF488:  lda     ($F9),y
        jsr     LF167
        iny
        cpy     $EE
        bne     LF488
LF492:  jmp     LF17E

LF495:  jsr     LF5AE
        beq     LF4BA
        ldy     #$05
        sty     $0268
        ldy     #$00
        sty     $E9
LF4A3:  cpy     $EE
        beq     LF4B9
        lda     ($F9),y
        ldy     $0268
        cmp     ($F3),y
        bne     LF495
        inc     $E9
        inc     $0268
        ldy     $E9
        bne     LF4A3
LF4B9:  tya
LF4BA:  rts

        lda     #$01
        sta     $020B
        jsr     LF34B
        lda     $020C
        and     #$10
        beq     LF4CF
        ldy     #$6E
        jmp     LF57B

LF4CF:  ldy     #$AA
        jmp     LE7DE

LF4D4:  ldx     #$00
        stx     $F0
        stx     $020C
        stx     $EE
        inx
        stx     $F1
        jsr     LF522
        jsr     LD676
        stx     $EF
        jsr     LF515
        jsr     LF45C
        stx     $F1
        cpx     #$03
        bcc     LF4F6
        dec     $F0
LF4F6:  jsr     LF515
        jsr     LF45C
        stx     $F0
        jsr     LF515
        jsr     LF51D
LF504:  jsr     LCCB8
        jsr     LD57B
        sta     $EE
        lda     $71
        sta     $F9
        lda     $72
        sta     $FA
        rts

LF515:  jsr     L00C8
        bne     LF51C
        pla
        pla
LF51C:  rts

LF51D:  lda     #$2C
        jsr     LCE13
LF522:  jsr     L00C8
        bne     LF51C
        jmp     LCE1C

        jsr     LF4D4
        lda     $EF
        bne     LF534
        jmp     LCE1C

LF534:  jsr     LF2AB
        bne     LF53D
        ldy     #$0E
LF53B:  bne     LF57B
LF53D:  ldx     $0262
        ldy     #$00
        sty     $020C
        cpx     #$0A
        beq     LF53B
        inc     $0262
        lda     $EF
        sta     $0242,x
        lda     $F0
        sta     $0256,x
        lda     $F1
        sta     $024C,x
        beq     LF5AD
        cmp     #$03
        beq     LF5AD
        bcc     LF566
        jmp     LF462

LF566:  lda     $F0
        bne     LF592
        jsr     LF83B
        jsr     LF3FF
        lda     $EE
        beq     LF58B
        jsr     LF495
        bne     LF59A
LF579:  ldy     #$24
LF57B:  lda     #$0D
        jsr     LE3EA
        lda     #$3F
        jsr     LE3EA
        jsr     LE7DE
        jmp     LC379

LF58B:  jsr     LF5AE
        beq     LF579
        bne     LF59A
LF592:  jsr     LF871
        lda     #$04
        jsr     LF5ED
LF59A:  ldx     $F1
        lda     #$BF
        ldy     $F0
        beq     LF5AA
        jsr     LF5E3
        lda     #$02
        sta     ($F3),y
        tya
LF5AA:  sta     $0270,x
LF5AD:  rts

LF5AE:  lda     $020B
        pha
LF5B2:  jsr     LF87F
        ldy     #$00
        lda     ($F3),y
        cmp     #$05
        beq     LF5DD
        cmp     #$01
        beq     LF5C5
        cmp     #$04
        bne     LF5B2
LF5C5:  tax
        lda     $CA
        bne     LF5DB
        ldy     #$A3
        jsr     LE7DE
        ldy     #$05
LF5D1:  lda     ($F3),y
        jsr     LE3EA
        iny
        cpy     #$15
        bne     LF5D1
LF5DB:  ldy     #$01
LF5DD:  pla
        sta     $020B
        tya
        rts

LF5E3:  ldy     #$BF
        lda     #$20
LF5E7:  sta     ($F3),y
        dey
        bne     LF5E7
        rts

LF5ED:  sta     $E9
        lda     $F8
        pha
        lda     $F7
        pha
        lda     $E6
        pha
        lda     $E5
        pha
        jsr     LF5E3
        lda     $E9
        sta     ($F3),y
        iny
        lda     $F7
        sta     ($F3),y
        iny
        lda     $F8
        sta     ($F3),y
        iny
        lda     $E5
        sta     ($F3),y
        iny
        lda     $E6
        sta     ($F3),y
        iny
        sty     $0268
        ldy     #$00
        sty     $E9
LF61E:  ldy     $E9
        cpy     $EE
        beq     LF632
        lda     ($F9),y
        ldy     $0268
        sta     ($F3),y
        inc     $E9
        inc     $0268
        bne     LF61E
LF632:  jsr     LF67D
        jsr     LF913
        lda     #$69
        sta     $0279
        jsr     LF8C4
        pla
        sta     $E5
        pla
        sta     $E6
        pla
        sta     $F7
        pla
        sta     $F8
        rts

LF64D:  jsr     LF913
        ldx     #$00
        ldy     #$01
LF654:  lda     ($F3),y
        sta     $E3,x
        inx
        iny
        cpx     #$04
        bne     LF654
        lda     $E3
        sta     $F7
        lda     $E4
        sta     $F8
        rts

LF667:  lda     #$7A
        sta     $F3
        lda     #$02
        sta     $F4
        lda     $F1
        lsr     a
        bcs     LF67C
        lda     #$3A
        sta     $F3
        lda     #$03
        sta     $F4
LF67C:  rts

LF67D:  jsr     LF913
        jsr     LF667
        lda     $F3
        sta     $F7
        clc
        adc     #$C0
        sta     $E5
        lda     $F4
        sta     $F8
        adc     #$00
        sta     $E6
        rts

        jsr     LCCA4
        jsr     LD6D0
        jmp     (L0008)

        jsr     LF433
        lda     $7C
        sta     $E5
        lda     $7D
        sta     $E6
        lda     #$04
        sta     $F8
        lda     #$00
        sta     $F7
        lda     $F1
        bne     LF6BA
LF6B5:  ldy     #$74
        jmp     LF57B

LF6BA:  cmp     #$03
        beq     LF6B5
        bcc     LF6F6
        jsr     LF71C
        jsr     LF462
        jsr     LF0BA
        ldy     #$00
        jsr     LFBDC
LF6CE:  jsr     LFD90
        beq     LF6E3
        lda     ($E3),y
        jsr     LF167
        jsr     LF339
        inc     $E3
        bne     LF6CE
        inc     $E4
        bne     LF6CE
LF6E3:  jsr     LF17E
LF6E6:  bit     $F0
        bmi     LF735
        jsr     LF0BA
        lda     $F0
        and     #$EF
        ora     #$E0
        jmp     LF12C

LF6F6:  jsr     LF667
        jsr     LF871
        lda     $CA
        bne     LF708
        ldy     #$64
        jsr     LE7DE
        jsr     LF411
LF708:  lda     #$01
        jsr     LF5ED
        jsr     LF8BC
        ldx     $F0
        beq     LF735
        dex
        beq     LF735
        lda     #$05
        jmp     LF5ED

LF71C:  ldx     #$00
        stx     $F0
LF720:  ldx     #$00
        inc     $F0
LF724:  cpx     $0262
        beq     LF787
        lda     $0256,x
        and     #$1F
        cmp     $F0
        beq     LF720
        inx
        bne     LF724
LF735:  rts

        lda     $0205
        adc     #$01
        sta     $0205
        bcc     LF743
        inc     $0206
LF743:  cmp     #$6F
        bne     LF74E
        lda     $0206
        cmp     #$02
        beq     LF774
LF74E:  inc     $0202
        bne     LF75B
        inc     $0201
        bne     LF75B
        inc     $0200
LF75B:  ldx     #$00
LF75D:  lda     $0200,x
        cmp     LF788,x
        bcc     LF77C
        inx
        cpx     #$03
        bne     LF75D
        lda     #$00
LF76C:  sta     $01FF,x
        dex
        bne     LF76C
        beq     LF77C
LF774:  lda     #$00
        sta     $0205
        sta     $0206
LF77C:  lda     $E812
        cmp     $E812
        bne     LF77C
        sta     $0209
LF787:  rts

LF788:  .byte   $4F
        .byte   $1A
        ora     ($48,x)
        txa
        pha
        tya
        pha
        lda     #$00
        sta     $020C
        txa
        jsr     LF2AB
        beq     LF7A0
LF79B:  ldy     #$17
LF79D:  jmp     LF57B

LF7A0:  jsr     LF2B8
        lda     $F1
        beq     LF7B5
        cmp     #$03
        beq     LF7B5
        bcs     LF7BB
        ldx     $F0
        beq     LF7B5
        ldy     #$86
        bne     LF79D
LF7B5:  sta     $0263
        jmp     LF277

LF7BB:  pha
        jsr     LF0B6
        lda     $F0
        bpl     LF7C9
        jsr     LF161
        jmp     LF7D0

LF7C9:  and     #$1F
        ora     #$60
        jsr     LF15B
LF7D0:  lda     $020C
        bpl     LF7D8
        jmp     LF47D

LF7D8:  pla
        jmp     LF7B5

        pha
        txa
        pha
        tya
        pha
        lda     #$00
        sta     $020C
        lda     #$FF
        sta     $026A
        txa
        jsr     LF2AB
        bne     LF79B
        jsr     LF2B8
        lda     $F1
        beq     LF79B
        cmp     #$03
        beq     LF806
        bpl     LF80C
        ldx     $F0
