; SuperPET Waterloo 6809 ROM $E000-$FFFF (970034-12) - disassembled from waterloo-e000-ffff-970034-12.bin
; Base address $E000, length 8192 ($2000) bytes
; Generated with capstone (CS_ARCH_M680X / CS_MODE_M680X_6809) - a real 6809
; disassembler, not hand-decoded. Labels/comments are NOT included here (raw
; mnemonic + operand listing only); see docs/pet/waterloo-investigation.md for
; the annotated, hand-verified routines this session actually traced through.

E000: 62         fcb      $62
E001: 6300       com      0, x
E003: 345b       pshs     cc, a, dp, x, u
E005: 6f0a       clr      10, x
E007: 75         fcb      $75
E008: 746571     lsr      $6571
E00B: 0470       lsr      $70
E00D: 695c       rol      -4, u
E00F: 797277     rol      $7277
E012: 0936       rol      $36
E014: 276c       beq      $e082
E016: 0d6a       tst      $6a
E018: 6764       asr      4, s
E01A: 61         fcb      $61
E01B: 353b       puls     cc, a, dp, x, y
E01D: 6b         fcb      $6b
E01E: 7b         fcb      $7b
E01F: 6866       lsl      6, s
E021: 730639     com      $0639
E024: ff6037     stu      $6037
E027: 3037       leax     -9, y
E029: 3431       pshs     cc, x, y
E02B: ffff07     stu      $ff07
E02E: 38         fcb      $38
E02F: 3d         mul      
E030: 38         fcb      $38
E031: 3532       puls     a, x, y
E033: 00ff       neg      $ff
E035: ff5f02     stu      $5f02
E038: 285e       bvc      $e098
E03A: 23ff       bls      $e03b
E03C: 813f       cmpa     #63
E03E: ff0c4d     stu      $0c4d
E041: 2058       bra      $e09b
E043: ff827f     stu      $827f
E046: ff8a3c     stu      $8a3c
E049: 4e         fcb      $4e
E04A: 56         rorb     
E04B: 5a         decb     
E04C: 8300ff     subd     #255
E04F: 8b3e       adda     #62
E051: 42         fcb      $42
E052: 43         coma     
E053: 0084       neg      $84
E055: 5d         tstb     
E056: 4f         clra     
E057: 0b         fcb      $0b
E058: 55         fcb      $55
E059: 54         lsrb     
E05A: 45         fcb      $45
E05B: 51         fcb      $51
E05C: 05         fcb      $05
E05D: 50         negb     
E05E: 49         rola     
E05F: 7c5952     inc      $5952
E062: 57         asrb     
E063: 0986       rol      $86
E065: 224c       bhi      $e0b3
E067: 0d4a       tst      $4a
E069: 47         asra     
E06A: 44         lsra     
E06B: 41         fcb      $41
E06C: 853a       bita     #58
E06E: 4b         fcb      $4b
E06F: 7d4846     tst      $4846
E072: 53         comb     
E073: 0689       ror      $89
E075: ff7e87     stu      $7e87
E078: 2926       bvs      $e0a0
E07A: 2421       bcc      $e09d
E07C: ffff08     stu      $ff08
E07F: 882b       eora     #43
E081: 2a25       bpl      $e0a8
E083: 40         nega     
E084: 2854       bvc      $e0da
E086: 0e29       jmp      $29
E088: 54         lsrb     
E089: 0f47       clr      $47
E08B: 4d         tsta     
E08C: 10         fcb      $10
E08D: 48         lsla     
E08E: 4d         tsta     
E08F: 11         fcb      $11
E090: 4f         clra     
E091: 4d         tsta     
E092: 12         nop      
E093: 4f         clra     
E094: 3f         swi      
E095: 13         sync     
E096: 4f         clra     
E097: 5f         clrb     
E098: 14         fcb      $14
E099: 4f         clra     
E09A: 50         negb     
E09B: 15         fcb      $15
E09C: 47         asra     
E09D: 54         lsrb     
E09E: 16424a     lbra     $22eb
E0A1: 174e4a     lbsr     $2eee
E0A4: 18         fcb      $18
E0A5: 3f         swi      
E0A6: 5f         clrb     
E0A7: 19         daa      
E0A8: 2f5f       ble      $e109
E0AA: 1a43       orcc     #67
E0AC: 4a         deca     
E0AD: 1b         fcb      $1b
E0AE: 4c         inca     
E0AF: 4b         fcb      $4b
E0B0: 1c4b       andcc    #75
E0B2: 2e1d       bgt      $e0d1
E0B4: 4c         inca     
E0B5: 2b1e       bmi      $e0d5
E0B7: 4e         fcb      $4e
E0B8: 42         fcb      $42
E0B9: 1f00       tfr      d, d
E0BB: 0000       neg      $00
E0BD: 3128       leay     8, y
E0BF: 290f       bvs      $e0d0
E0C1: 2700       beq      $e0c3
E0C3: 19         daa      
E0C4: 2000       bra      $e0c6
E0C6: 0900       rol      $00
E0C8: 0030       neg      $30
E0CA: 0000       neg      $00
E0CC: 0000       neg      $00
E0CE: 00be       neg      $be
E0D0: 01         fcb      $01
E0D1: 5e         fcb      $5e
E0D2: 3001       leax     1, x
E0D4: 8c02ef     cmpx     #751
E0D7: 2d05       blt      $e0de
E0D9: 8e0000     ldx      #0
E0DC: 2012       bra      $e0f0
E0DE: 108e0163   ldy      #355
E0E2: 863c       lda      #60
E0E4: 6ca4       inc      , y
E0E6: a1a4       cmpa     , y
E0E8: 2e06       bgt      $e0f0
E0EA: 6fa4       clr      , y
E0EC: 313f       leay     -1, y
E0EE: 20f4       bra      $e0e4
E0F0: bf015e     stx      $015e
E0F3: 39         rts      
E0F4: 3406       pshs     a, b
E0F6: bde768     jsr      $e768
E0F9: cc0004     ldd      #4
E0FC: 3406       pshs     a, b
E0FE: cc0160     ldd      #352
E101: 3406       pshs     a, b
E103: ec64       ldd      4, s
E105: 2046       bra      $e14d
E107: 3406       pshs     a, b
E109: 327e       leas     -2, s
E10B: ec62       ldd      2, s
E10D: bdb7b1     jsr      $b7b1
E110: ede4       std      , s
E112: 83000b     subd     #11
E115: 2f05       ble      $e11c
E117: cc000b     ldd      #11
E11A: ede4       std      , s
E11C: ece4       ldd      , s
E11E: 3406       pshs     a, b
E120: cc0164     ldd      #356
E123: 3406       pshs     a, b
E125: ec66       ldd      6, s
E127: bdbb68     jsr      $bb68
E12A: 3264       leas     4, s
E12C: ece4       ldd      , s
E12E: c30164     addd     #356
E131: 3406       pshs     a, b
E133: 4f         clra     
E134: 5f         clrb     
E135: 3510       puls     x
E137: e784       stb      , x
E139: 3264       leas     4, s
E13B: 39         rts      
E13C: 3406       pshs     a, b
E13E: bde768     jsr      $e768
E141: cc0004     ldd      #4
E144: 3406       pshs     a, b
E146: ec62       ldd      2, s
E148: 3406       pshs     a, b
E14A: cc0160     ldd      #352
E14D: bdbb68     jsr      $bb68
E150: 3264       leas     4, s
E152: bde765     jsr      $e765
E155: 3262       leas     2, s
E157: 39         rts      
E158: cc015e     ldd      #350
E15B: c30006     addd     #6
E15E: 39         rts      
E15F: 3277       leas     -9, s
E161: cc0400     ldd      #1024
E164: ede4       std      , s
E166: ed62       std      2, s
E168: bddd82     jsr      $dd82
E16B: e766       stb      6, s
E16D: c103       cmpb     #3
E16F: 2753       beq      $e1c4
E171: e666       ldb      6, s
E173: 271c       beq      $e191
E175: c17f       cmpb     #127
E177: 2704       beq      $e17d
E179: 4f         clra     
E17A: bdd714     jsr      $d714
E17D: 4f         clra     
E17E: e666       ldb      6, s
E180: e7f802     stb      [2, s]
E183: ec62       ldd      2, s
E185: 8d40       bsr      $e1c7
E187: ed64       std      4, s
E189: a3e4       subd     , s
E18B: 2704       beq      $e191
E18D: ec64       ldd      4, s
E18F: ed62       std      2, s
E191: ece4       ldd      , s
E193: a362       subd     2, s
E195: 2713       beq      $e1aa
E197: 4f         clra     
E198: f6eff1     ldb      $eff1
E19B: c410       andb     #16
E19D: 270b       beq      $e1aa
E19F: e6f4       ldb      [, s]
E1A1: f7eff0     stb      $eff0
E1A4: ece4       ldd      , s
E1A6: 8d1f       bsr      $e1c7
E1A8: ede4       std      , s
E1AA: 4f         clra     
E1AB: f6eff1     ldb      $eff1
E1AE: c408       andb     #8
E1B0: 27b6       beq      $e168
E1B2: f6eff0     ldb      $eff0
E1B5: c47f       andb     #127
E1B7: e766       stb      6, s
E1B9: 27ad       beq      $e168
E1BB: c10a       cmpb     #10
E1BD: 27a9       beq      $e168
E1BF: bdd714     jsr      $d714
E1C2: 20a4       bra      $e168
E1C4: 3269       leas     9, s
E1C6: 39         rts      
E1C7: 3406       pshs     a, b
E1C9: ece4       ldd      , s
E1CB: c30001     addd     #1
E1CE: ede4       std      , s
E1D0: 3406       pshs     a, b
E1D2: cc0400     ldd      #1024
E1D5: c30050     addd     #80
E1D8: 3406       pshs     a, b
E1DA: ec62       ldd      2, s
E1DC: a3e4       subd     , s
E1DE: 3510       puls     x
E1E0: 3510       puls     x
E1E2: 2605       bne      $e1e9
E1E4: cc0400     ldd      #1024
E1E7: ede4       std      , s
E1E9: ece4       ldd      , s
E1EB: 3262       leas     2, s
E1ED: 39         rts      
E1EE: 3406       pshs     a, b
E1F0: cc000f     ldd      #15
E1F3: bde76b     jsr      $e76b
E1F6: ece4       ldd      , s
E1F8: dd11       std      $11
E1FA: 9e11       ldx      $11
E1FC: ec04       ldd      4, x
E1FE: dd13       std      $13
E200: bde4df     jsr      $e4df
E203: d66a       ldb      $6a
E205: 102600e2   lbne     $e2eb
E209: bde373     jsr      $e373
E20C: 4f         clra     
E20D: 9e13       ldx      $13
E20F: e602       ldb      2, x
E211: c480       andb     #-128
E213: 2711       beq      $e226
E215: bde3d1     jsr      $e3d1
E218: bde408     jsr      $e408
E21B: 9e13       ldx      $13
E21D: e602       ldb      2, x
E21F: c181       cmpb     #-127
E221: 2603       bne      $e226
E223: bde444     jsr      $e444
E226: 9e13       ldx      $13
E228: e602       ldb      2, x
E22A: c181       cmpb     #-127
E22C: 2615       bne      $e243
E22E: 6f0c       clr      12, x
E230: 9e68       ldx      $68
E232: e684       ldb      , x
E234: 2705       beq      $e23b
E236: bde5b9     jsr      $e5b9
E239: 205e       bra      $e299
E23B: c60f       ldb      #15
E23D: 9e13       ldx      $13
E23F: e70a       stb      10, x
E241: 2056       bra      $e299
E243: c183       cmpb     #-125
E245: 260b       bne      $e252
E247: 9e68       ldx      $68
E249: e684       ldb      , x
E24B: 276b       beq      $e2b8
E24D: bde47d     jsr      $e47d
E250: 2066       bra      $e2b8
E252: c104       cmpb     #4
E254: 2629       bne      $e27f
E256: bde47d     jsr      $e47d
E259: cceff0     ldd      #-4112
E25C: 9e13       ldx      $13
E25E: ed09       std      9, x
E260: 9e11       ldx      $11
E262: e684       ldb      , x
E264: c174       cmpb     #116
E266: 260e       bne      $e276
E268: 4f         clra     
E269: 9e13       ldx      $13
E26B: e601       ldb      1, x
E26D: c4c0       andb     #-64
E26F: 2605       bne      $e276
E271: 6f8810     clr      16, x
E274: 2042       bra      $e2b8
E276: c6ff       ldb      #-1
E278: 9e13       ldx      $13
E27A: e78810     stb      16, x
E27D: 2039       bra      $e2b8
E27F: c106       cmpb     #6
E281: 2607       bne      $e28a
E283: cceff0     ldd      #-4112
E286: ed09       std      9, x
E288: 2018       bra      $e2a2
E28A: e602       ldb      2, x
E28C: 2610       bne      $e29e
E28E: 6f0c       clr      12, x
E290: bde5b9     jsr      $e5b9
E293: c681       ldb      #-127
E295: 9e13       ldx      $13
E297: e702       stb      2, x
E299: cc00fe     ldd      #254
E29C: 2017       bra      $e2b5
E29E: c182       cmpb     #-126
E2A0: 2605       bne      $e2a7
E2A2: bde476     jsr      $e476
E2A5: 2011       bra      $e2b8
E2A7: c105       cmpb     #5
E2A9: 2704       beq      $e2af
E2AB: c107       cmpb     #7
E2AD: 2609       bne      $e2b8
E2AF: bde476     jsr      $e476
E2B2: cc0050     ldd      #80
E2B5: bde4c6     jsr      $e4c6
E2B8: 9e13       ldx      $13
E2BA: e602       ldb      2, x
E2BC: c181       cmpb     #-127
E2BE: 261e       bne      $e2de
E2C0: 4f         clra     
E2C1: e60c       ldb      12, x
E2C3: ca06       orb      #6
E2C5: e70c       stb      12, x
E2C7: 9e11       ldx      $11
E2C9: e684       ldb      , x
E2CB: c176       cmpb     #118
E2CD: 260f       bne      $e2de
E2CF: aee4       ldx      , s
E2D1: ec01       ldd      1, x
E2D3: 2709       beq      $e2de
E2D5: 9e11       ldx      $11
E2D7: ec01       ldd      1, x
E2D9: c30002     addd     #2
E2DC: ed01       std      1, x
E2DE: 9e11       ldx      $11
E2E0: ec01       ldd      1, x
E2E2: 9e13       ldx      $13
E2E4: ed05       std      5, x
E2E6: cc0001     ldd      #1
E2E9: ed03       std      3, x
E2EB: 3262       leas     2, s
E2ED: 39         rts      
E2EE: 3406       pshs     a, b
E2F0: cc0010     ldd      #16
E2F3: bde76b     jsr      $e76b
E2F6: ece4       ldd      , s
E2F8: dd11       std      $11
E2FA: 9e11       ldx      $11
E2FC: ec04       ldd      4, x
E2FE: dd13       std      $13
E300: 8d71       bsr      $e373
E302: 9e13       ldx      $13
E304: e602       ldb      2, x
E306: c181       cmpb     #-127
E308: 2608       bne      $e312
E30A: bde3d1     jsr      $e3d1
E30D: bde444     jsr      $e444
E310: 2008       bra      $e31a
E312: e602       ldb      2, x
E314: 2604       bne      $e31a
E316: c681       ldb      #-127
E318: e702       stb      2, x
E31A: 9e13       ldx      $13
E31C: e602       ldb      2, x
E31E: c104       cmpb     #4
E320: 2615       bne      $e337
E322: cceff0     ldd      #-4112
E325: ed09       std      9, x
E327: 9e68       ldx      $68
E329: e684       ldb      , x
E32B: c12e       cmpb     #46
E32D: 2635       bne      $e364
E32F: cc0001     ldd      #1
E332: bde6f1     jsr      $e6f1
E335: 202d       bra      $e364
E337: c181       cmpb     #-127
E339: 262c       bne      $e367
E33B: 9e68       ldx      $68
E33D: e684       ldb      , x
E33F: 2626       bne      $e367
E341: 9e13       ldx      $13
E343: 6f0a       clr      10, x
E345: e60b       ldb      11, x
E347: 2609       bne      $e352
E349: dc68       ldd      $68
E34B: 3406       pshs     a, b
E34D: cce6ff     ldd      #-6401
E350: 2007       bra      $e359
E352: dc68       ldd      $68
E354: 3406       pshs     a, b
E356: cce702     ldd      #-6398
E359: bdb7ff     jsr      $b7ff
E35C: 3262       leas     2, s
E35E: c646       ldb      #70
E360: 9e13       ldx      $13
E362: e70c       stb      12, x
E364: 7ee56d     jmp      $e56d
E367: c603       ldb      #3
E369: 9e13       ldx      $13
E36B: e784       stb      , x
E36D: cce705     ldd      #-6395
E370: 7ee56a     jmp      $e56a
E373: 327e       leas     -2, s
E375: 9e13       ldx      $13
E377: 6f02       clr      2, x
E379: bde6c0     jsr      $e6c0
E37C: ede4       std      , s
E37E: 272c       beq      $e3ac
E380: 83000a     subd     #10
E383: 2e27       bgt      $e3ac
E385: ece4       ldd      , s
E387: 3406       pshs     a, b
E389: dc68       ldd      $68
E38B: 3406       pshs     a, b
E38D: ccb124     ldd      #-20188
E390: bdbba7     jsr      $bba7
E393: 3264       leas     4, s
E395: c3b155     addd     #-20139
E398: 3406       pshs     a, b
E39A: 4f         clra     
E39B: 3510       puls     x
E39D: e684       ldb      , x
E39F: 9e13       ldx      $13
E3A1: e702       stb      2, x
E3A3: 2707       beq      $e3ac
E3A5: ece4       ldd      , s
E3A7: bde6f1     jsr      $e6f1
E3AA: 200d       bra      $e3b9
E3AC: c608       ldb      #8
E3AE: 9e13       ldx      $13
E3B0: e709       stb      9, x
E3B2: bde570     jsr      $e570
E3B5: 9e13       ldx      $13
E3B7: 6f0b       clr      11, x
E3B9: 4f         clra     
E3BA: 9e13       ldx      $13
E3BC: e602       ldb      2, x
E3BE: c47f       andb     #127
E3C0: 58         lslb     
E3C1: 49         rola     
E3C2: c30368     addd     #872
E3C5: 3406       pshs     a, b
E3C7: 3510       puls     x
E3C9: ec84       ldd      , x
E3CB: 9e13       ldx      $13
E3CD: ed07       std      7, x
E3CF: 2071       bra      $e442
E3D1: 327e       leas     -2, s
E3D3: bde6d5     jsr      $e6d5
E3D6: ede4       std      , s
E3D8: 2714       beq      $e3ee
E3DA: 3406       pshs     a, b
E3DC: dc68       ldd      $68
E3DE: bdb84a     jsr      $b84a
E3E1: 3262       leas     2, s
E3E3: 9e13       ldx      $13
E3E5: e709       stb      9, x
E3E7: ece4       ldd      , s
E3E9: bde6f1     jsr      $e6f1
E3EC: 207f       bra      $e46d
E3EE: 9e13       ldx      $13
E3F0: e602       ldb      2, x
E3F2: c181       cmpb     #-127
E3F4: 2604       bne      $e3fa
E3F6: c608       ldb      #8
E3F8: 2006       bra      $e400
E3FA: c182       cmpb     #-126
E3FC: 2606       bne      $e404
E3FE: c604       ldb      #4
E400: e709       stb      9, x
E402: 206f       bra      $e473
E404: 6f09       clr      9, x
E406: 206b       bra      $e473
E408: 327e       leas     -2, s
E40A: 9e68       ldx      $68
E40C: e684       ldb      , x
E40E: c12d       cmpb     #45
E410: 2621       bne      $e433
E412: cc0001     ldd      #1
E415: bde6f1     jsr      $e6f1
E418: bde6d5     jsr      $e6d5
E41B: ede4       std      , s
E41D: 2714       beq      $e433
E41F: 3406       pshs     a, b
E421: dc68       ldd      $68
E423: bdb84a     jsr      $b84a
E426: 3262       leas     2, s
E428: 9e13       ldx      $13
E42A: e70a       stb      10, x
E42C: ece4       ldd      , s
E42E: bde6f1     jsr      $e6f1
E431: 2040       bra      $e473
E433: 9e13       ldx      $13
E435: e602       ldb      2, x
E437: c181       cmpb     #-127
E439: 2605       bne      $e440
E43B: bde570     jsr      $e570
E43E: 2033       bra      $e473
E440: 6f0a       clr      10, x
E442: 202f       bra      $e473
E444: 327e       leas     -2, s
E446: 9e68       ldx      $68
E448: e684       ldb      , x
E44A: c12f       cmpb     #47
E44C: 2621       bne      $e46f
E44E: cc0001     ldd      #1
E451: bde6f1     jsr      $e6f1
E454: bde6d5     jsr      $e6d5
E457: ede4       std      , s
E459: 2714       beq      $e46f
E45B: 3406       pshs     a, b
E45D: dc68       ldd      $68
E45F: bdb84a     jsr      $b84a
E462: 3262       leas     2, s
E464: 9e13       ldx      $13
E466: e70b       stb      11, x
E468: ece4       ldd      , s
E46A: bde6f1     jsr      $e6f1
E46D: 2004       bra      $e473
E46F: 9e13       ldx      $13
E471: 6f0b       clr      11, x
E473: 3262       leas     2, s
E475: 39         rts      
E476: 9e68       ldx      $68
E478: e684       ldb      , x
E47A: 263d       bne      $e4b9
E47C: 39         rts      
E47D: 9e13       ldx      $13
E47F: e602       ldb      2, x
E481: 260a       bne      $e48d
E483: 9e68       ldx      $68
E485: e684       ldb      , x
E487: c12e       cmpb     #46
E489: 272e       beq      $e4b9
E48B: 200e       bra      $e49b
E48D: 9e68       ldx      $68
E48F: e684       ldb      , x
E491: c12e       cmpb     #46
E493: 2624       bne      $e4b9
E495: cc0001     ldd      #1
E498: bde6f1     jsr      $e6f1
E49B: 9e68       ldx      $68
E49D: e684       ldb      , x
E49F: 2718       beq      $e4b9
E4A1: c12c       cmpb     #44
E4A3: 2714       beq      $e4b9
E4A5: 9e13       ldx      $13
E4A7: e602       ldb      2, x
E4A9: c181       cmpb     #-127
E4AB: 2704       beq      $e4b1
E4AD: e602       ldb      2, x
E4AF: 2607       bne      $e4b8
E4B1: 4f         clra     
E4B2: e60c       ldb      12, x
E4B4: ca40       orb      #64
E4B6: e70c       stb      12, x
E4B8: 39         rts      
E4B9: c603       ldb      #3
E4BB: 9e13       ldx      $13
E4BD: e784       stb      , x
E4BF: cce713     ldd      #-6381
E4C2: 7eb653     jmp      $b653
E4C5: 39         rts      
E4C6: 3406       pshs     a, b
E4C8: 9e11       ldx      $11
E4CA: ec01       ldd      1, x
E4CC: a3e4       subd     , s
E4CE: 2f0c       ble      $e4dc
E4D0: cce724     ldd      #-6364
E4D3: bdb653     jsr      $b653
E4D6: c603       ldb      #3
E4D8: 9e13       ldx      $13
E4DA: e784       stb      , x
E4DC: 3262       leas     2, s
E4DE: 39         rts      
E4DF: 327e       leas     -2, s
E4E1: 4f         clra     
E4E2: 5f         clrb     
E4E3: 9e11       ldx      $11
E4E5: ed01       std      1, x
E4E7: c674       ldb      #116
E4E9: e784       stb      , x
E4EB: 9e68       ldx      $68
E4ED: e684       ldb      , x
E4EF: c128       cmpb     #40
E4F1: 267a       bne      $e56d
E4F3: cc0001     ldd      #1
E4F6: bde6f1     jsr      $e6f1
E4F9: bde6c0     jsr      $e6c0
E4FC: ede4       std      , s
E4FE: 2761       beq      $e561
E500: 83000a     subd     #10
E503: 2e5c       bgt      $e561
E505: ece4       ldd      , s
E507: 3406       pshs     a, b
E509: dc68       ldd      $68
E50B: 3406       pshs     a, b
E50D: ccb15d     ldd      #-20131
E510: bdbba7     jsr      $bba7
E513: 3264       leas     4, s
E515: c3b172     addd     #-20110
E518: 3406       pshs     a, b
E51A: 4f         clra     
E51B: 3510       puls     x
E51D: e684       ldb      , x
E51F: 9e11       ldx      $11
E521: e784       stb      , x
E523: 273c       beq      $e561
E525: ece4       ldd      , s
E527: bde6f1     jsr      $e6f1
E52A: 9e68       ldx      $68
E52C: e684       ldb      , x
E52E: c13a       cmpb     #58
E530: 261f       bne      $e551
E532: cc0001     ldd      #1
E535: bde6f1     jsr      $e6f1
E538: bde6d5     jsr      $e6d5
E53B: ede4       std      , s
E53D: 3406       pshs     a, b
E53F: dc68       ldd      $68
E541: bdb84a     jsr      $b84a
E544: 3262       leas     2, s
E546: 9e11       ldx      $11
E548: ed01       std      1, x
E54A: 2715       beq      $e561
E54C: ece4       ldd      , s
E54E: bde6f1     jsr      $e6f1
E551: 9e68       ldx      $68
E553: e684       ldb      , x
E555: c129       cmpb     #41
E557: 2608       bne      $e561
E559: cc0001     ldd      #1
E55C: bde6f1     jsr      $e6f1
E55F: 200c       bra      $e56d
E561: c603       ldb      #3
E563: 9e13       ldx      $13
E565: e784       stb      , x
E567: cce713     ldd      #-6381
E56A: bdb653     jsr      $b653
E56D: 3262       leas     2, s
E56F: 39         rts      
E570: 327c       leas     -4, s
E572: c602       ldb      #2
E574: 9e13       ldx      $13
E576: e70a       stb      10, x
E578: 9e6b       ldx      $6b
E57A: ec06       ldd      6, x
E57C: ede4       std      , s
E57E: 936b       subd     $6b
E580: 2734       beq      $e5b6
E582: aee4       ldx      , s
E584: ec04       ldd      4, x
E586: ed62       std      2, s
E588: 9313       subd     $13
E58A: 2726       beq      $e5b2
E58C: ae62       ldx      2, s
E58E: e602       ldb      2, x
E590: c181       cmpb     #-127
E592: 261e       bne      $e5b2
E594: 9e13       ldx      $13
E596: e609       ldb      9, x
E598: ae62       ldx      2, s
E59A: e109       cmpb     9, x
E59C: 2614       bne      $e5b2
E59E: 9e13       ldx      $13
E5A0: e60a       ldb      10, x
E5A2: ae62       ldx      2, s
E5A4: e10a       cmpb     10, x
E5A6: 260a       bne      $e5b2
E5A8: 4f         clra     
E5A9: 9e13       ldx      $13
E5AB: e60a       ldb      10, x
E5AD: c30001     addd     #1
E5B0: 20c4       bra      $e576
E5B2: aee4       ldx      , s
E5B4: 20c4       bra      $e57a
E5B6: 3264       leas     4, s
E5B8: 39         rts      
E5B9: bde47d     jsr      $e47d
E5BC: 9e11       ldx      $11
E5BE: ae04       ldx      4, x
E5C0: e60b       ldb      11, x
E5C2: 2609       bne      $e5cd
E5C4: dc68       ldd      $68
E5C6: 3406       pshs     a, b
E5C8: cce738     ldd      #-6344
E5CB: 2007       bra      $e5d4
E5CD: dc68       ldd      $68
E5CF: 3406       pshs     a, b
E5D1: cce73b     ldd      #-6341
E5D4: bdb80a     jsr      $b80a
E5D7: 3262       leas     2, s
E5D9: 327b       leas     -5, s
E5DB: dc68       ldd      $68
E5DD: ede4       std      , s
E5DF: aee4       ldx      , s
E5E1: e684       ldb      , x
E5E3: 270b       beq      $e5f0
E5E5: c12c       cmpb     #44
E5E7: 273a       beq      $e623
E5E9: ece4       ldd      , s
E5EB: c30001     addd     #1
E5EE: 20ed       bra      $e5dd
E5F0: 4f         clra     
E5F1: 9e13       ldx      $13
E5F3: e601       ldb      1, x
E5F5: c4c0       andb     #-64
E5F7: 2615       bne      $e60e
E5F9: dc68       ldd      $68
E5FB: 3406       pshs     a, b
E5FD: cce73e     ldd      #-6338
E600: bdb832     jsr      $b832
E603: 3262       leas     2, s
E605: 4f         clra     
E606: 9e13       ldx      $13
E608: e60c       ldb      12, x
E60A: ca10       orb      #16
E60C: 2067       bra      $e675
E60E: dc68       ldd      $68
E610: 3406       pshs     a, b
E612: cce743     ldd      #-6333
E615: bdb832     jsr      $b832
E618: 3262       leas     2, s
E61A: 4f         clra     
E61B: 9e13       ldx      $13
E61D: e60c       ldb      12, x
E61F: ca20       orb      #32
E621: 2052       bra      $e675
E623: ece4       ldd      , s
E625: c30001     addd     #1
E628: ede4       std      , s
E62A: ed62       std      2, s
E62C: 4f         clra     
E62D: e6f4       ldb      [, s]
E62F: bdb9d8     jsr      $b9d8
E632: 2709       beq      $e63d
E634: ece4       ldd      , s
E636: c30001     addd     #1
E639: ede4       std      , s
E63B: 20ef       bra      $e62c
E63D: e6f4       ldb      [, s]
E63F: 2638       bne      $e679
E641: ece4       ldd      , s
E643: a362       subd     2, s
E645: 830003     subd     #3
E648: 262f       bne      $e679
E64A: cc0003     ldd      #3
E64D: 3406       pshs     a, b
E64F: ec64       ldd      4, s
E651: 3406       pshs     a, b
E653: ccb176     ldd      #-20106
E656: bdbba7     jsr      $bba7
E659: 3264       leas     4, s
E65B: c3b187     addd     #-20089
E65E: 3406       pshs     a, b
E660: 4f         clra     
E661: 3510       puls     x
E663: e684       ldb      , x
E665: e764       stb      4, s
E667: 2710       beq      $e679
E669: ec62       ldd      2, s
E66B: bdba77     jsr      $ba77
E66E: 4f         clra     
E66F: 9e13       ldx      $13
E671: e60c       ldb      12, x
E673: ea64       orb      4, s
E675: e70c       stb      12, x
E677: 200c       bra      $e685
E679: c603       ldb      #3
E67B: 9e13       ldx      $13
E67D: e784       stb      , x
E67F: cce748     ldd      #-6328
E682: bdb653     jsr      $b653
E685: 4f         clra     
E686: 9e13       ldx      $13
E688: e60c       ldb      12, x
E68A: c408       andb     #8
E68C: 2613       bne      $e6a1
E68E: e601       ldb      1, x
E690: c403       andb     #3
E692: 830003     subd     #3
E695: 260a       bne      $e6a1
E697: c603       ldb      #3
E699: e784       stb      , x
E69B: cce756     ldd      #-6314
E69E: bdb653     jsr      $b653
E6A1: 4f         clra     
E6A2: 9e13       ldx      $13
E6A4: e60c       ldb      12, x
E6A6: c408       andb     #8
E6A8: 2608       bne      $e6b2
E6AA: 9e11       ldx      $11
E6AC: e684       ldb      , x
E6AE: c166       cmpb     #102
E6B0: 260b       bne      $e6bd
E6B2: 9e11       ldx      $11
E6B4: ec01       ldd      1, x
E6B6: 2605       bne      $e6bd
E6B8: cc0050     ldd      #80
E6BB: ed01       std      1, x
E6BD: 3265       leas     5, s
E6BF: 39         rts      
E6C0: 327e       leas     -2, s
E6C2: dc68       ldd      $68
E6C4: ede4       std      , s
E6C6: 4f         clra     
E6C7: e6f4       ldb      [, s]
E6C9: bdb9d8     jsr      $b9d8
E6CC: 271c       beq      $e6ea
E6CE: ece4       ldd      , s
E6D0: c30001     addd     #1
E6D3: 20ef       bra      $e6c4
E6D5: 327e       leas     -2, s
E6D7: dc68       ldd      $68
E6D9: ede4       std      , s
E6DB: 4f         clra     
E6DC: e6f4       ldb      [, s]
E6DE: bdba06     jsr      $ba06
E6E1: 2707       beq      $e6ea
E6E3: ece4       ldd      , s
E6E5: c30001     addd     #1
E6E8: 20ef       bra      $e6d9
E6EA: ece4       ldd      , s
E6EC: 9368       subd     $68
E6EE: 3262       leas     2, s
E6F0: 39         rts      
E6F1: 3406       pshs     a, b
E6F3: dc68       ldd      $68
E6F5: 3406       pshs     a, b
E6F7: e362       addd     2, s
E6F9: bdb7ff     jsr      $b7ff
E6FC: 3264       leas     4, s
E6FE: 39         rts      
E6FF: 2430       bcc      $e731
E701: 0024       neg      $24
E703: 3100       leay     0, x
E705: 6e6f       jmp      15, s
E707: 742073     lsr      $2073
E70A: 75         fcb      $75
E70B: 70706f     neg      $706f
E70E: 72         fcb      $72
E70F: 746564     lsr      $6564
E712: 0069       neg      $69
E714: 6e76       jmp      -10, s
E716: 61         fcb      $61
E717: 6c69       inc      9, s
E719: 6420       lsr      0, y
E71B: 6669       ror      9, s
E71D: 6c65       inc      5, s
E71F: 6e61       jmp      1, s
E721: 6d65       tst      5, s
E723: 0069       neg      $69
E725: 6e76       jmp      -10, s
E727: 61         fcb      $61
E728: 6c69       inc      9, s
E72A: 6420       lsr      0, y
E72C: 72         fcb      $72
E72D: 65         fcb      $65
E72E: 636f       com      15, s
E730: 72         fcb      $72
E731: 6420       lsr      0, y
E733: 73697a     com      $697a
E736: 65         fcb      $65
E737: 0030       neg      $30
E739: 3a         abx      
E73A: 0031       neg      $31
E73C: 3a         abx      
E73D: 002c       neg      $2c
E73F: 53         comb     
E740: 45         fcb      $45
E741: 51         fcb      $51
E742: 002c       neg      $2c
E744: 50         negb     
E745: 52         fcb      $52
E746: 47         asra     
E747: 0062       neg      $62
E749: 61         fcb      $61
E74A: 6420       lsr      0, y
E74C: 6669       ror      9, s
E74E: 6c65       inc      5, s
E750: 2074       bra      $e7c6
E752: 797065     rol      $7065
E755: 0069       neg      $69
E757: 6e76       jmp      -10, s
E759: 61         fcb      $61
E75A: 6c69       inc      9, s
E75C: 6420       lsr      0, y
E75E: 61         fcb      $61
E75F: 6363       com      3, s
E761: 65         fcb      $65
E762: 737300     com      $7300
E765: 1cef       andcc    #-17
E767: 39         rts      
E768: 1a10       orcc     #16
E76A: 39         rts      
E76B: 1f01       tfr      d, x
E76D: e6890580   ldb      1408, x
E771: 2706       beq      $e779
E773: 1f01       tfr      d, x
E775: 6e9905c0   jmp      [1472, x]
E779: 39         rts      
E77A: 327e       leas     -2, s
E77C: cc0580     ldd      #1408
E77F: ede4       std      , s
E781: 830600     subd     #1536
E784: 2409       bcc      $e78f
E786: 6ff4       clr      [, s]
E788: ece4       ldd      , s
E78A: c30001     addd     #1
E78D: 20f0       bra      $e77f
E78F: 3262       leas     2, s
E791: 39         rts      
E792: 10839000   cmpd     #-28672
E796: 253e       bcs      $e7d6
E798: 10839fff   cmpd     #-24577
E79C: 2238       bhi      $e7d6
E79E: 1f01       tfr      d, x
E7A0: e6c4       ldb      , u
E7A2: 3404       pshs     b
E7A4: f7effc     stb      $effc
E7A7: e664       ldb      4, s
E7A9: 260a       bne      $e7b5
E7AB: 6d85       tst      b, x
E7AD: 2703       beq      $e7b2
E7AF: 5c         incb     
E7B0: 20f9       bra      $e7ab
E7B2: 5c         incb     
E7B3: e764       stb      4, s
E7B5: 3a         abx      
E7B6: a682       lda      , -x
E7B8: a7c2       sta      , -u
E7BA: 5a         decb     
E7BB: 2702       beq      $e7bf
E7BD: 20f7       bra      $e7b6
E7BF: 1f31       tfr      u, x
E7C1: e664       ldb      4, s
E7C3: e7c2       stb      , -u
E7C5: cce7d7     ldd      #-6185
E7C8: edc3       std      , --u
E7CA: 3504       puls     b
E7CC: e7c2       stb      , -u
E7CE: f60220     ldb      $0220
E7D1: f7effc     stb      $effc
E7D4: 1f10       tfr      x, d
E7D6: 39         rts      
E7D7: 3406       pshs     a, b
E7D9: e6c0       ldb      , u+
E7DB: 33c5       leau     b, u
E7DD: 3724       pulu     b, y
E7DF: f70220     stb      $0220
E7E2: f7effc     stb      $effc
E7E5: ece1       ldd      , s++
E7E7: 6ea4       jmp      , y
E7E9: ede4       std      , s
E7EB: ece4       ldd      , s
E7ED: 58         lslb     
E7EE: 49         rola     
E7EF: c3010e     addd     #270
E7F2: 3406       pshs     a, b
E7F4: ec64       ldd      4, s
E7F6: 3406       pshs     a, b
E7F8: c30002     addd     #2
E7FB: ed66       std      6, s
E7FD: 3510       puls     x
E7FF: ec         fcb      $ec
E800: ffffff     stu      $ffff
E803: ffffff     stu      $ffff
E806: ffffff     stu      $ffff
E809: ffffff     stu      $ffff
E80C: ffffff     stu      $ffff
E80F: ffffff     stu      $ffff
E812: ffffff     stu      $ffff
E815: ffffff     stu      $ffff
E818: ffffff     stu      $ffff
E81B: ffffff     stu      $ffff
E81E: ffffff     stu      $ffff
E821: ffffff     stu      $ffff
E824: ffffff     stu      $ffff
E827: ffffff     stu      $ffff
E82A: ffffff     stu      $ffff
E82D: ffffff     stu      $ffff
E830: ffffff     stu      $ffff
E833: ffffff     stu      $ffff
E836: ffffff     stu      $ffff
E839: ffffff     stu      $ffff
E83C: ffffff     stu      $ffff
E83F: ffffff     stu      $ffff
E842: ffffff     stu      $ffff
E845: ffffff     stu      $ffff
E848: ffffff     stu      $ffff
E84B: ffffff     stu      $ffff
E84E: ffffff     stu      $ffff
E851: ffffff     stu      $ffff
E854: ffffff     stu      $ffff
E857: ffffff     stu      $ffff
E85A: ffffff     stu      $ffff
E85D: ffffff     stu      $ffff
E860: ffffff     stu      $ffff
E863: ffffff     stu      $ffff
E866: ffffff     stu      $ffff
E869: ffffff     stu      $ffff
E86C: ffffff     stu      $ffff
E86F: ffffff     stu      $ffff
E872: ffffff     stu      $ffff
E875: ffffff     stu      $ffff
E878: ffffff     stu      $ffff
E87B: ffffff     stu      $ffff
E87E: ffffff     stu      $ffff
E881: ffffff     stu      $ffff
E884: ffffff     stu      $ffff
E887: ffffff     stu      $ffff
E88A: ffffff     stu      $ffff
E88D: ffffff     stu      $ffff
E890: ffffff     stu      $ffff
E893: ffffff     stu      $ffff
E896: ffffff     stu      $ffff
E899: ffffff     stu      $ffff
E89C: ffffff     stu      $ffff
E89F: ffffff     stu      $ffff
E8A2: ffffff     stu      $ffff
E8A5: ffffff     stu      $ffff
E8A8: ffffff     stu      $ffff
E8AB: ffffff     stu      $ffff
E8AE: ffffff     stu      $ffff
E8B1: ffffff     stu      $ffff
E8B4: ffffff     stu      $ffff
E8B7: ffffff     stu      $ffff
E8BA: ffffff     stu      $ffff
E8BD: ffffff     stu      $ffff
E8C0: ffffff     stu      $ffff
E8C3: ffffff     stu      $ffff
E8C6: ffffff     stu      $ffff
E8C9: ffffff     stu      $ffff
E8CC: ffffff     stu      $ffff
E8CF: ffffff     stu      $ffff
E8D2: ffffff     stu      $ffff
E8D5: ffffff     stu      $ffff
E8D8: ffffff     stu      $ffff
E8DB: ffffff     stu      $ffff
E8DE: ffffff     stu      $ffff
E8E1: ffffff     stu      $ffff
E8E4: ffffff     stu      $ffff
E8E7: ffffff     stu      $ffff
E8EA: ffffff     stu      $ffff
E8ED: ffffff     stu      $ffff
E8F0: ffffff     stu      $ffff
E8F3: ffffff     stu      $ffff
E8F6: ffffff     stu      $ffff
E8F9: ffffff     stu      $ffff
E8FC: ffffff     stu      $ffff
E8FF: ffffff     stu      $ffff
E902: ffffff     stu      $ffff
E905: ffffff     stu      $ffff
E908: ffffff     stu      $ffff
E90B: ffffff     stu      $ffff
E90E: ffffff     stu      $ffff
E911: ffffff     stu      $ffff
E914: ffffff     stu      $ffff
E917: ffffff     stu      $ffff
E91A: ffffff     stu      $ffff
E91D: ffffff     stu      $ffff
E920: ffffff     stu      $ffff
E923: ffffff     stu      $ffff
E926: ffffff     stu      $ffff
E929: ffffff     stu      $ffff
E92C: ffffff     stu      $ffff
E92F: ffffff     stu      $ffff
E932: ffffff     stu      $ffff
E935: ffffff     stu      $ffff
E938: ffffff     stu      $ffff
E93B: ffffff     stu      $ffff
E93E: ffffff     stu      $ffff
E941: ffffff     stu      $ffff
E944: ffffff     stu      $ffff
E947: ffffff     stu      $ffff
E94A: ffffff     stu      $ffff
E94D: ffffff     stu      $ffff
E950: ffffff     stu      $ffff
E953: ffffff     stu      $ffff
E956: ffffff     stu      $ffff
E959: ffffff     stu      $ffff
E95C: ffffff     stu      $ffff
E95F: ffffff     stu      $ffff
E962: ffffff     stu      $ffff
E965: ffffff     stu      $ffff
E968: ffffff     stu      $ffff
E96B: ffffff     stu      $ffff
E96E: ffffff     stu      $ffff
E971: ffffff     stu      $ffff
E974: ffffff     stu      $ffff
E977: ffffff     stu      $ffff
E97A: ffffff     stu      $ffff
E97D: ffffff     stu      $ffff
E980: ffffff     stu      $ffff
E983: ffffff     stu      $ffff
E986: ffffff     stu      $ffff
E989: ffffff     stu      $ffff
E98C: ffffff     stu      $ffff
E98F: ffffff     stu      $ffff
E992: ffffff     stu      $ffff
E995: ffffff     stu      $ffff
E998: ffffff     stu      $ffff
E99B: ffffff     stu      $ffff
E99E: ffffff     stu      $ffff
E9A1: ffffff     stu      $ffff
E9A4: ffffff     stu      $ffff
E9A7: ffffff     stu      $ffff
E9AA: ffffff     stu      $ffff
E9AD: ffffff     stu      $ffff
E9B0: ffffff     stu      $ffff
E9B3: ffffff     stu      $ffff
E9B6: ffffff     stu      $ffff
E9B9: ffffff     stu      $ffff
E9BC: ffffff     stu      $ffff
E9BF: ffffff     stu      $ffff
E9C2: ffffff     stu      $ffff
E9C5: ffffff     stu      $ffff
E9C8: ffffff     stu      $ffff
E9CB: ffffff     stu      $ffff
E9CE: ffffff     stu      $ffff
E9D1: ffffff     stu      $ffff
E9D4: ffffff     stu      $ffff
E9D7: ffffff     stu      $ffff
E9DA: ffffff     stu      $ffff
E9DD: ffffff     stu      $ffff
E9E0: ffffff     stu      $ffff
E9E3: ffffff     stu      $ffff
E9E6: ffffff     stu      $ffff
E9E9: ffffff     stu      $ffff
E9EC: ffffff     stu      $ffff
E9EF: ffffff     stu      $ffff
E9F2: ffffff     stu      $ffff
E9F5: ffffff     stu      $ffff
E9F8: ffffff     stu      $ffff
E9FB: ffffff     stu      $ffff
E9FE: ffffff     stu      $ffff
EA01: ffffff     stu      $ffff
EA04: ffffff     stu      $ffff
EA07: ffffff     stu      $ffff
EA0A: ffffff     stu      $ffff
EA0D: ffffff     stu      $ffff
EA10: ffffff     stu      $ffff
EA13: ffffff     stu      $ffff
EA16: ffffff     stu      $ffff
EA19: ffffff     stu      $ffff
EA1C: ffffff     stu      $ffff
EA1F: ffffff     stu      $ffff
EA22: ffffff     stu      $ffff
EA25: ffffff     stu      $ffff
EA28: ffffff     stu      $ffff
EA2B: ffffff     stu      $ffff
EA2E: ffffff     stu      $ffff
EA31: ffffff     stu      $ffff
EA34: ffffff     stu      $ffff
EA37: ffffff     stu      $ffff
EA3A: ffffff     stu      $ffff
EA3D: ffffff     stu      $ffff
EA40: ffffff     stu      $ffff
EA43: ffffff     stu      $ffff
EA46: ffffff     stu      $ffff
EA49: ffffff     stu      $ffff
EA4C: ffffff     stu      $ffff
EA4F: ffffff     stu      $ffff
EA52: ffffff     stu      $ffff
EA55: ffffff     stu      $ffff
EA58: ffffff     stu      $ffff
EA5B: ffffff     stu      $ffff
EA5E: ffffff     stu      $ffff
EA61: ffffff     stu      $ffff
EA64: ffffff     stu      $ffff
EA67: ffffff     stu      $ffff
EA6A: ffffff     stu      $ffff
EA6D: ffffff     stu      $ffff
EA70: ffffff     stu      $ffff
EA73: ffffff     stu      $ffff
EA76: ffffff     stu      $ffff
EA79: ffffff     stu      $ffff
EA7C: ffffff     stu      $ffff
EA7F: ffffff     stu      $ffff
EA82: ffffff     stu      $ffff
EA85: ffffff     stu      $ffff
EA88: ffffff     stu      $ffff
EA8B: ffffff     stu      $ffff
EA8E: ffffff     stu      $ffff
EA91: ffffff     stu      $ffff
EA94: ffffff     stu      $ffff
EA97: ffffff     stu      $ffff
EA9A: ffffff     stu      $ffff
EA9D: ffffff     stu      $ffff
EAA0: ffffff     stu      $ffff
EAA3: ffffff     stu      $ffff
EAA6: ffffff     stu      $ffff
EAA9: ffffff     stu      $ffff
EAAC: ffffff     stu      $ffff
EAAF: ffffff     stu      $ffff
EAB2: ffffff     stu      $ffff
EAB5: ffffff     stu      $ffff
EAB8: ffffff     stu      $ffff
EABB: ffffff     stu      $ffff
EABE: ffffff     stu      $ffff
EAC1: ffffff     stu      $ffff
EAC4: ffffff     stu      $ffff
EAC7: ffffff     stu      $ffff
EACA: ffffff     stu      $ffff
EACD: ffffff     stu      $ffff
EAD0: ffffff     stu      $ffff
EAD3: ffffff     stu      $ffff
EAD6: ffffff     stu      $ffff
EAD9: ffffff     stu      $ffff
EADC: ffffff     stu      $ffff
EADF: ffffff     stu      $ffff
EAE2: ffffff     stu      $ffff
EAE5: ffffff     stu      $ffff
EAE8: ffffff     stu      $ffff
EAEB: ffffff     stu      $ffff
EAEE: ffffff     stu      $ffff
EAF1: ffffff     stu      $ffff
EAF4: ffffff     stu      $ffff
EAF7: ffffff     stu      $ffff
EAFA: ffffff     stu      $ffff
EAFD: ffffff     stu      $ffff
EB00: ffffff     stu      $ffff
EB03: ffffff     stu      $ffff
EB06: ffffff     stu      $ffff
EB09: ffffff     stu      $ffff
EB0C: ffffff     stu      $ffff
EB0F: ffffff     stu      $ffff
EB12: ffffff     stu      $ffff
EB15: ffffff     stu      $ffff
EB18: ffffff     stu      $ffff
EB1B: ffffff     stu      $ffff
EB1E: ffffff     stu      $ffff
EB21: ffffff     stu      $ffff
EB24: ffffff     stu      $ffff
EB27: ffffff     stu      $ffff
EB2A: ffffff     stu      $ffff
EB2D: ffffff     stu      $ffff
EB30: ffffff     stu      $ffff
EB33: ffffff     stu      $ffff
EB36: ffffff     stu      $ffff
EB39: ffffff     stu      $ffff
EB3C: ffffff     stu      $ffff
EB3F: ffffff     stu      $ffff
EB42: ffffff     stu      $ffff
EB45: ffffff     stu      $ffff
EB48: ffffff     stu      $ffff
EB4B: ffffff     stu      $ffff
EB4E: ffffff     stu      $ffff
EB51: ffffff     stu      $ffff
EB54: ffffff     stu      $ffff
EB57: ffffff     stu      $ffff
EB5A: ffffff     stu      $ffff
EB5D: ffffff     stu      $ffff
EB60: ffffff     stu      $ffff
EB63: ffffff     stu      $ffff
EB66: ffffff     stu      $ffff
EB69: ffffff     stu      $ffff
EB6C: ffffff     stu      $ffff
EB6F: ffffff     stu      $ffff
EB72: ffffff     stu      $ffff
EB75: ffffff     stu      $ffff
EB78: ffffff     stu      $ffff
EB7B: ffffff     stu      $ffff
EB7E: ffffff     stu      $ffff
EB81: ffffff     stu      $ffff
EB84: ffffff     stu      $ffff
EB87: ffffff     stu      $ffff
EB8A: ffffff     stu      $ffff
EB8D: ffffff     stu      $ffff
EB90: ffffff     stu      $ffff
EB93: ffffff     stu      $ffff
EB96: ffffff     stu      $ffff
EB99: ffffff     stu      $ffff
EB9C: ffffff     stu      $ffff
EB9F: ffffff     stu      $ffff
EBA2: ffffff     stu      $ffff
EBA5: ffffff     stu      $ffff
EBA8: ffffff     stu      $ffff
EBAB: ffffff     stu      $ffff
EBAE: ffffff     stu      $ffff
EBB1: ffffff     stu      $ffff
EBB4: ffffff     stu      $ffff
EBB7: ffffff     stu      $ffff
EBBA: ffffff     stu      $ffff
EBBD: ffffff     stu      $ffff
EBC0: ffffff     stu      $ffff
EBC3: ffffff     stu      $ffff
EBC6: ffffff     stu      $ffff
EBC9: ffffff     stu      $ffff
EBCC: ffffff     stu      $ffff
EBCF: ffffff     stu      $ffff
EBD2: ffffff     stu      $ffff
EBD5: ffffff     stu      $ffff
EBD8: ffffff     stu      $ffff
EBDB: ffffff     stu      $ffff
EBDE: ffffff     stu      $ffff
EBE1: ffffff     stu      $ffff
EBE4: ffffff     stu      $ffff
EBE7: ffffff     stu      $ffff
EBEA: ffffff     stu      $ffff
EBED: ffffff     stu      $ffff
EBF0: ffffff     stu      $ffff
EBF3: ffffff     stu      $ffff
EBF6: ffffff     stu      $ffff
EBF9: ffffff     stu      $ffff
EBFC: ffffff     stu      $ffff
EBFF: ffffff     stu      $ffff
EC02: ffffff     stu      $ffff
EC05: ffffff     stu      $ffff
EC08: ffffff     stu      $ffff
EC0B: ffffff     stu      $ffff
EC0E: ffffff     stu      $ffff
EC11: ffffff     stu      $ffff
EC14: ffffff     stu      $ffff
EC17: ffffff     stu      $ffff
EC1A: ffffff     stu      $ffff
EC1D: ffffff     stu      $ffff
EC20: ffffff     stu      $ffff
EC23: ffffff     stu      $ffff
EC26: ffffff     stu      $ffff
EC29: ffffff     stu      $ffff
EC2C: ffffff     stu      $ffff
EC2F: ffffff     stu      $ffff
EC32: ffffff     stu      $ffff
EC35: ffffff     stu      $ffff
EC38: ffffff     stu      $ffff
EC3B: ffffff     stu      $ffff
EC3E: ffffff     stu      $ffff
EC41: ffffff     stu      $ffff
EC44: ffffff     stu      $ffff
EC47: ffffff     stu      $ffff
EC4A: ffffff     stu      $ffff
EC4D: ffffff     stu      $ffff
EC50: ffffff     stu      $ffff
EC53: ffffff     stu      $ffff
EC56: ffffff     stu      $ffff
EC59: ffffff     stu      $ffff
EC5C: ffffff     stu      $ffff
EC5F: ffffff     stu      $ffff
EC62: ffffff     stu      $ffff
EC65: ffffff     stu      $ffff
EC68: ffffff     stu      $ffff
EC6B: ffffff     stu      $ffff
EC6E: ffffff     stu      $ffff
EC71: ffffff     stu      $ffff
EC74: ffffff     stu      $ffff
EC77: ffffff     stu      $ffff
EC7A: ffffff     stu      $ffff
EC7D: ffffff     stu      $ffff
EC80: ffffff     stu      $ffff
EC83: ffffff     stu      $ffff
EC86: ffffff     stu      $ffff
EC89: ffffff     stu      $ffff
EC8C: ffffff     stu      $ffff
EC8F: ffffff     stu      $ffff
EC92: ffffff     stu      $ffff
EC95: ffffff     stu      $ffff
EC98: ffffff     stu      $ffff
EC9B: ffffff     stu      $ffff
EC9E: ffffff     stu      $ffff
ECA1: ffffff     stu      $ffff
ECA4: ffffff     stu      $ffff
ECA7: ffffff     stu      $ffff
ECAA: ffffff     stu      $ffff
ECAD: ffffff     stu      $ffff
ECB0: ffffff     stu      $ffff
ECB3: ffffff     stu      $ffff
ECB6: ffffff     stu      $ffff
ECB9: ffffff     stu      $ffff
ECBC: ffffff     stu      $ffff
ECBF: ffffff     stu      $ffff
ECC2: ffffff     stu      $ffff
ECC5: ffffff     stu      $ffff
ECC8: ffffff     stu      $ffff
ECCB: ffffff     stu      $ffff
ECCE: ffffff     stu      $ffff
ECD1: ffffff     stu      $ffff
ECD4: ffffff     stu      $ffff
ECD7: ffffff     stu      $ffff
ECDA: ffffff     stu      $ffff
ECDD: ffffff     stu      $ffff
ECE0: ffffff     stu      $ffff
ECE3: ffffff     stu      $ffff
ECE6: ffffff     stu      $ffff
ECE9: ffffff     stu      $ffff
ECEC: ffffff     stu      $ffff
ECEF: ffffff     stu      $ffff
ECF2: ffffff     stu      $ffff
ECF5: ffffff     stu      $ffff
ECF8: ffffff     stu      $ffff
ECFB: ffffff     stu      $ffff
ECFE: ffffff     stu      $ffff
ED01: ffffff     stu      $ffff
ED04: ffffff     stu      $ffff
ED07: ffffff     stu      $ffff
ED0A: ffffff     stu      $ffff
ED0D: ffffff     stu      $ffff
ED10: ffffff     stu      $ffff
ED13: ffffff     stu      $ffff
ED16: ffffff     stu      $ffff
ED19: ffffff     stu      $ffff
ED1C: ffffff     stu      $ffff
ED1F: ffffff     stu      $ffff
ED22: ffffff     stu      $ffff
ED25: ffffff     stu      $ffff
ED28: ffffff     stu      $ffff
ED2B: ffffff     stu      $ffff
ED2E: ffffff     stu      $ffff
ED31: ffffff     stu      $ffff
ED34: ffffff     stu      $ffff
ED37: ffffff     stu      $ffff
ED3A: ffffff     stu      $ffff
ED3D: ffffff     stu      $ffff
ED40: ffffff     stu      $ffff
ED43: ffffff     stu      $ffff
ED46: ffffff     stu      $ffff
ED49: ffffff     stu      $ffff
ED4C: ffffff     stu      $ffff
ED4F: ffffff     stu      $ffff
ED52: ffffff     stu      $ffff
ED55: ffffff     stu      $ffff
ED58: ffffff     stu      $ffff
ED5B: ffffff     stu      $ffff
ED5E: ffffff     stu      $ffff
ED61: ffffff     stu      $ffff
ED64: ffffff     stu      $ffff
ED67: ffffff     stu      $ffff
ED6A: ffffff     stu      $ffff
ED6D: ffffff     stu      $ffff
ED70: ffffff     stu      $ffff
ED73: ffffff     stu      $ffff
ED76: ffffff     stu      $ffff
ED79: ffffff     stu      $ffff
ED7C: ffffff     stu      $ffff
ED7F: ffffff     stu      $ffff
ED82: ffffff     stu      $ffff
ED85: ffffff     stu      $ffff
ED88: ffffff     stu      $ffff
ED8B: ffffff     stu      $ffff
ED8E: ffffff     stu      $ffff
ED91: ffffff     stu      $ffff
ED94: ffffff     stu      $ffff
ED97: ffffff     stu      $ffff
ED9A: ffffff     stu      $ffff
ED9D: ffffff     stu      $ffff
EDA0: ffffff     stu      $ffff
EDA3: ffffff     stu      $ffff
EDA6: ffffff     stu      $ffff
EDA9: ffffff     stu      $ffff
EDAC: ffffff     stu      $ffff
EDAF: ffffff     stu      $ffff
EDB2: ffffff     stu      $ffff
EDB5: ffffff     stu      $ffff
EDB8: ffffff     stu      $ffff
EDBB: ffffff     stu      $ffff
EDBE: ffffff     stu      $ffff
EDC1: ffffff     stu      $ffff
EDC4: ffffff     stu      $ffff
EDC7: ffffff     stu      $ffff
EDCA: ffffff     stu      $ffff
EDCD: ffffff     stu      $ffff
EDD0: ffffff     stu      $ffff
EDD3: ffffff     stu      $ffff
EDD6: ffffff     stu      $ffff
EDD9: ffffff     stu      $ffff
EDDC: ffffff     stu      $ffff
EDDF: ffffff     stu      $ffff
EDE2: ffffff     stu      $ffff
EDE5: ffffff     stu      $ffff
EDE8: ffffff     stu      $ffff
EDEB: ffffff     stu      $ffff
EDEE: ffffff     stu      $ffff
EDF1: ffffff     stu      $ffff
EDF4: ffffff     stu      $ffff
EDF7: ffffff     stu      $ffff
EDFA: ffffff     stu      $ffff
EDFD: ffffff     stu      $ffff
EE00: ffffff     stu      $ffff
EE03: ffffff     stu      $ffff
EE06: ffffff     stu      $ffff
EE09: ffffff     stu      $ffff
EE0C: ffffff     stu      $ffff
EE0F: ffffff     stu      $ffff
EE12: ffffff     stu      $ffff
EE15: ffffff     stu      $ffff
EE18: ffffff     stu      $ffff
EE1B: ffffff     stu      $ffff
EE1E: ffffff     stu      $ffff
EE21: ffffff     stu      $ffff
EE24: ffffff     stu      $ffff
EE27: ffffff     stu      $ffff
EE2A: ffffff     stu      $ffff
EE2D: ffffff     stu      $ffff
EE30: ffffff     stu      $ffff
EE33: ffffff     stu      $ffff
EE36: ffffff     stu      $ffff
EE39: ffffff     stu      $ffff
EE3C: ffffff     stu      $ffff
EE3F: ffffff     stu      $ffff
EE42: ffffff     stu      $ffff
EE45: ffffff     stu      $ffff
EE48: ffffff     stu      $ffff
EE4B: ffffff     stu      $ffff
EE4E: ffffff     stu      $ffff
EE51: ffffff     stu      $ffff
EE54: ffffff     stu      $ffff
EE57: ffffff     stu      $ffff
EE5A: ffffff     stu      $ffff
EE5D: ffffff     stu      $ffff
EE60: ffffff     stu      $ffff
EE63: ffffff     stu      $ffff
EE66: ffffff     stu      $ffff
EE69: ffffff     stu      $ffff
EE6C: ffffff     stu      $ffff
EE6F: ffffff     stu      $ffff
EE72: ffffff     stu      $ffff
EE75: ffffff     stu      $ffff
EE78: ffffff     stu      $ffff
EE7B: ffffff     stu      $ffff
EE7E: ffffff     stu      $ffff
EE81: ffffff     stu      $ffff
EE84: ffffff     stu      $ffff
EE87: ffffff     stu      $ffff
EE8A: ffffff     stu      $ffff
EE8D: ffffff     stu      $ffff
EE90: ffffff     stu      $ffff
EE93: ffffff     stu      $ffff
EE96: ffffff     stu      $ffff
EE99: ffffff     stu      $ffff
EE9C: ffffff     stu      $ffff
EE9F: ffffff     stu      $ffff
EEA2: ffffff     stu      $ffff
EEA5: ffffff     stu      $ffff
EEA8: ffffff     stu      $ffff
EEAB: ffffff     stu      $ffff
EEAE: ffffff     stu      $ffff
EEB1: ffffff     stu      $ffff
EEB4: ffffff     stu      $ffff
EEB7: ffffff     stu      $ffff
EEBA: ffffff     stu      $ffff
EEBD: ffffff     stu      $ffff
EEC0: ffffff     stu      $ffff
EEC3: ffffff     stu      $ffff
EEC6: ffffff     stu      $ffff
EEC9: ffffff     stu      $ffff
EECC: ffffff     stu      $ffff
EECF: ffffff     stu      $ffff
EED2: ffffff     stu      $ffff
EED5: ffffff     stu      $ffff
EED8: ffffff     stu      $ffff
EEDB: ffffff     stu      $ffff
EEDE: ffffff     stu      $ffff
EEE1: ffffff     stu      $ffff
EEE4: ffffff     stu      $ffff
EEE7: ffffff     stu      $ffff
EEEA: ffffff     stu      $ffff
EEED: ffffff     stu      $ffff
EEF0: ffffff     stu      $ffff
EEF3: ffffff     stu      $ffff
EEF6: ffffff     stu      $ffff
EEF9: ffffff     stu      $ffff
EEFC: ffffff     stu      $ffff
EEFF: ffffff     stu      $ffff
EF02: ffffff     stu      $ffff
EF05: ffffff     stu      $ffff
EF08: ffffff     stu      $ffff
EF0B: ffffff     stu      $ffff
EF0E: ffffff     stu      $ffff
EF11: ffffff     stu      $ffff
EF14: ffffff     stu      $ffff
EF17: ffffff     stu      $ffff
EF1A: ffffff     stu      $ffff
EF1D: ffffff     stu      $ffff
EF20: ffffff     stu      $ffff
EF23: ffffff     stu      $ffff
EF26: ffffff     stu      $ffff
EF29: ffffff     stu      $ffff
EF2C: ffffff     stu      $ffff
EF2F: ffffff     stu      $ffff
EF32: ffffff     stu      $ffff
EF35: ffffff     stu      $ffff
EF38: ffffff     stu      $ffff
EF3B: ffffff     stu      $ffff
EF3E: ffffff     stu      $ffff
EF41: ffffff     stu      $ffff
EF44: ffffff     stu      $ffff
EF47: ffffff     stu      $ffff
EF4A: ffffff     stu      $ffff
EF4D: ffffff     stu      $ffff
EF50: ffffff     stu      $ffff
EF53: ffffff     stu      $ffff
EF56: ffffff     stu      $ffff
EF59: ffffff     stu      $ffff
EF5C: ffffff     stu      $ffff
EF5F: ffffff     stu      $ffff
EF62: ffffff     stu      $ffff
EF65: ffffff     stu      $ffff
EF68: ffffff     stu      $ffff
EF6B: ffffff     stu      $ffff
EF6E: ffffff     stu      $ffff
EF71: ffffff     stu      $ffff
EF74: ffffff     stu      $ffff
EF77: ffffff     stu      $ffff
EF7A: ffffff     stu      $ffff
EF7D: ffffff     stu      $ffff
EF80: ffffff     stu      $ffff
EF83: ffffff     stu      $ffff
EF86: ffffff     stu      $ffff
EF89: ffffff     stu      $ffff
EF8C: ffffff     stu      $ffff
EF8F: ffffff     stu      $ffff
EF92: ffffff     stu      $ffff
EF95: ffffff     stu      $ffff
EF98: ffffff     stu      $ffff
EF9B: ffffff     stu      $ffff
EF9E: ffffff     stu      $ffff
EFA1: ffffff     stu      $ffff
EFA4: ffffff     stu      $ffff
EFA7: ffffff     stu      $ffff
EFAA: ffffff     stu      $ffff
EFAD: ffffff     stu      $ffff
EFB0: ffffff     stu      $ffff
EFB3: ffffff     stu      $ffff
EFB6: ffffff     stu      $ffff
EFB9: ffffff     stu      $ffff
EFBC: ffffff     stu      $ffff
EFBF: ffffff     stu      $ffff
EFC2: ffffff     stu      $ffff
EFC5: ffffff     stu      $ffff
EFC8: ffffff     stu      $ffff
EFCB: ffffff     stu      $ffff
EFCE: ffffff     stu      $ffff
EFD1: ffffff     stu      $ffff
EFD4: ffffff     stu      $ffff
EFD7: ffffff     stu      $ffff
EFDA: ffffff     stu      $ffff
EFDD: ffffff     stu      $ffff
EFE0: ffffff     stu      $ffff
EFE3: ffffff     stu      $ffff
EFE6: ffffff     stu      $ffff
EFE9: ffffff     stu      $ffff
EFEC: ffffff     stu      $ffff
EFEF: ffffff     stu      $ffff
EFF2: ffffff     stu      $ffff
EFF5: ffffff     stu      $ffff
EFF8: ffffff     stu      $ffff
EFFB: ffffff     stu      $ffff
EFFE: ffff10     stu      $ff10
F001: ce0220     ldu      #544
F004: bdfe74     jsr      $fe74
F007: bdb00c     jsr      $b00c
F00A: bdb0a8     jsr      $b0a8
F00D: 7ea990     jmp      $a990
F010: aa         fcb      $aa
F011: aa         fcb      $aa
F012: aa         fcb      $aa
F013: aa         fcb      $aa
F014: aa         fcb      $aa
F015: aa         fcb      $aa
F016: aa         fcb      $aa
F017: aa         fcb      $aa
F018: aa         fcb      $aa
F019: aa         fcb      $aa
F01A: aa         fcb      $aa
F01B: aa         fcb      $aa
F01C: aa         fcb      $aa
F01D: aa         fcb      $aa
F01E: aa         fcb      $aa
F01F: aa34       ora      -12, y
F021: 06e6       ror      $e6
F023: 61         fcb      $61
F024: c101       cmpb     #1
F026: 2605       bne      $f02d
F028: bdf05c     jsr      $f05c
F02B: 200b       bra      $f038
F02D: 4f         clra     
F02E: 3406       pshs     a, b
F030: ccf03d     ldd      #-4035
F033: bdb0b7     jsr      $b0b7
F036: 3262       leas     2, s
F038: 0f32       clr      $32
F03A: 3262       leas     2, s
F03C: 39         rts      
F03D: 55         fcb      $55
F03E: 6e61       jmp      1, s
F040: 62         fcb      $62
F041: 6c65       inc      5, s
F043: 2074       bra      $f0b9
F045: 6f20       clr      0, y
F047: 706572     neg      $6572
F04A: 666f       ror      15, s
F04C: 72         fcb      $72
F04D: 6d20       tst      0, y
F04F: 72         fcb      $72
F050: 65         fcb      $65
F051: 71         fcb      $71
F052: 75         fcb      $75
F053: 65         fcb      $65
F054: 737420     com      $7420
F057: 2564       bcs      $f0bd
F059: 256e       bcs      $f0c9
F05B: 0032       neg      $32
F05D: 7ebdb0     jmp      $bdb0
F060: abbdf160   adda     [$e1c4, pcr]
F064: 4f         clra     
F065: 5f         clrb     
F066: ede4       std      , s
F068: ece4       ldd      , s
F06A: 58         lslb     
F06B: 49         rola     
F06C: c30182     addd     #386
F06F: 3406       pshs     a, b
F071: 4f         clra     
F072: 5f         clrb     
F073: 3510       puls     x
F075: ed84       std      , x
F077: ece4       ldd      , s
F079: c30001     addd     #1
F07C: ede4       std      , s
F07E: 830005     subd     #5
F081: 26e5       bne      $f068
F083: cc0005     ldd      #5
F086: 58         lslb     
F087: 49         rola     
F088: c30182     addd     #386
F08B: 3406       pshs     a, b
F08D: cc0220     ldd      #544
F090: 3510       puls     x
F092: ed84       std      , x
F094: cc0006     ldd      #6
F097: 58         lslb     
F098: 49         rola     
F099: c30182     addd     #386
F09C: 3406       pshs     a, b
F09E: ccc000     ldd      #-16384
F0A1: 3510       puls     x
F0A3: ed84       std      , x
F0A5: cc000a     ldd      #10
F0A8: 3406       pshs     a, b
F0AA: ccfdde     ldd      #-546
F0AD: bdff97     jsr      $ff97
F0B0: 3262       leas     2, s
F0B2: cc000c     ldd      #12
F0B5: 3406       pshs     a, b
F0B7: ccfdde     ldd      #-546
F0BA: bdff97     jsr      $ff97
F0BD: 3262       leas     2, s
F0BF: ccf123     ldd      #-3805
F0C2: bdb0b7     jsr      $b0b7
F0C5: ccf0cd     ldd      #-3891
F0C8: bdb000     jsr      $b000
F0CB: 2053       bra      $f120
F0CD: 327e       leas     -2, s
F0CF: ccf13d     ldd      #-3779
F0D2: bdb0b7     jsr      $b0b7
F0D5: bdb0c6     jsr      $b0c6
F0D8: f70175     stb      $0175
F0DB: c13e       cmpb     #62
F0DD: 2606       bne      $f0e5
F0DF: bdb0c6     jsr      $b0c6
F0E2: f70175     stb      $0175
F0E5: c10d       cmpb     #13
F0E7: 27e6       beq      $f0cf
F0E9: 4f         clra     
F0EA: ca20       orb      #32
F0EC: f70175     stb      $0175
F0EF: c171       cmpb     #113
F0F1: 2606       bne      $f0f9
F0F3: bdfdbd     jsr      $fdbd
F0F6: bdb003     jsr      $b003
F0F9: ccfe3c     ldd      #-452
F0FC: ede4       std      , s
F0FE: aee4       ldx      , s
F100: ec84       ldd      , x
F102: 270f       beq      $f113
F104: 4f         clra     
F105: f60175     ldb      $0175
F108: a384       subd     , x
F10A: 270f       beq      $f11b
F10C: ece4       ldd      , s
F10E: c30004     addd     #4
F111: 20e9       bra      $f0fc
F113: bdfdbd     jsr      $fdbd
F116: bdfdc6     jsr      $fdc6
F119: 20b4       bra      $f0cf
F11B: ad9802     jsr      [2, x]
F11E: 20af       bra      $f0cf
F120: 3262       leas     2, s
F122: 39         rts      
F123: 57         asrb     
F124: 61         fcb      $61
F125: 746572     lsr      $6572
F128: 6c6f       inc      15, s
F12A: 6f20       clr      0, y
F12C: 6d69       tst      9, s
F12E: 6372       com      -14, s
F130: 6f4d       clr      13, u
F132: 6f6e       clr      14, s
F134: 6974       rol      -12, s
F136: 6f72       clr      -14, s
F138: 256e       bcs      $f1a8
F13A: 256e       bcs      $f1aa
F13C: 003e       neg      $3e
F13E: 00bd       neg      $bd
F140: fd26f6     std      $26f6
F143: 01         fcb      $01
F144: 742605     lsr      $2605
F147: fc0170     ldd      $0170
F14A: 2007       bra      $f153
F14C: bdfdbd     jsr      $fdbd
F14F: 7efdc6     jmp      $fdc6
F152: 39         rts      
F153: 3406       pshs     a, b
F155: ece4       ldd      , s
F157: f7effc     stb      $effc
F15A: 3262       leas     2, s
F15C: 39         rts      
F15D: bdfdbd     jsr      $fdbd
F160: 327e       leas     -2, s
F162: 4f         clra     
F163: 5f         clrb     
F164: ede4       std      , s
F166: ece4       ldd      , s
F168: 58         lslb     
F169: 49         rola     
F16A: c30176     addd     #374
F16D: 3406       pshs     a, b
F16F: 4f         clra     
F170: 5f         clrb     
F171: 3510       puls     x
F173: ed84       std      , x
F175: ece4       ldd      , s
F177: c30001     addd     #1
F17A: ede4       std      , s
F17C: 830004     subd     #4
F17F: 26e5       bne      $f166
F181: 3262       leas     2, s
F183: 39         rts      
F184: 327e       leas     -2, s
F186: bdfd26     jsr      $fd26
F189: f60174     ldb      $0174
F18C: 2629       bne      $f1b7
F18E: fc0172     ldd      $0172
F191: b30170     subd     $0170
F194: 44         lsra     
F195: 56         rorb     
F196: 44         lsra     
F197: 56         rorb     
F198: 44         lsra     
F199: 56         rorb     
F19A: ede4       std      , s
F19C: 8d22       bsr      $f1c0
F19E: bdb081     jsr      $b081
F1A1: 261a       bne      $f1bd
F1A3: fc0170     ldd      $0170
F1A6: c30008     addd     #8
F1A9: fd0170     std      $0170
F1AC: ece4       ldd      , s
F1AE: c3ffff     addd     #-1
F1B1: ede4       std      , s
F1B3: 2ce7       bge      $f19c
F1B5: 2006       bra      $f1bd
F1B7: bdfdbd     jsr      $fdbd
F1BA: bdfdc6     jsr      $fdc6
F1BD: 3262       leas     2, s
F1BF: 39         rts      
F1C0: 327c       leas     -4, s
F1C2: fc0170     ldd      $0170
F1C5: ede4       std      , s
F1C7: ccf24e     ldd      #-3506
F1CA: bdb0b7     jsr      $b0b7
F1CD: ece4       ldd      , s
F1CF: 3406       pshs     a, b
F1D1: ec62       ldd      2, s
F1D3: 1f89       tfr      a, b
F1D5: 4f         clra     
F1D6: 3406       pshs     a, b
F1D8: ccf250     ldd      #-3504
F1DB: bdb0b7     jsr      $b0b7
F1DE: 3264       leas     4, s
F1E0: cc0008     ldd      #8
F1E3: ed62       std      2, s
F1E5: ece4       ldd      , s
F1E7: 3406       pshs     a, b
F1E9: c30001     addd     #1
F1EC: ed62       std      2, s
F1EE: 3510       puls     x
F1F0: 4f         clra     
F1F1: e684       ldb      , x
F1F3: 3406       pshs     a, b
F1F5: ccf256     ldd      #-3498
F1F8: bdb0b7     jsr      $b0b7
F1FB: 3262       leas     2, s
F1FD: ec62       ldd      2, s
F1FF: c3ffff     addd     #-1
F202: ed62       std      2, s
F204: 26df       bne      $f1e5
F206: ccf25a     ldd      #-3494
F209: bdb0b7     jsr      $b0b7
F20C: fc0170     ldd      $0170
F20F: ede4       std      , s
F211: cc0008     ldd      #8
F214: ed62       std      2, s
F216: aee4       ldx      , s
F218: e684       ldb      , x
F21A: c120       cmpb     #32
F21C: 2511       bcs      $f22f
F21E: c17e       cmpb     #126
F220: 220d       bhi      $f22f
F222: 4f         clra     
F223: 3406       pshs     a, b
F225: ccf25c     ldd      #-3492
F228: bdb0b7     jsr      $b0b7
F22B: 3262       leas     2, s
F22D: 2006       bra      $f235
F22F: ccf25f     ldd      #-3489
F232: bdb0b7     jsr      $b0b7
F235: ece4       ldd      , s
F237: c30001     addd     #1
F23A: ede4       std      , s
F23C: ec62       ldd      2, s
F23E: c3ffff     addd     #-1
F241: ed62       std      2, s
F243: 26d1       bne      $f216
F245: ccf261     ldd      #-3487
F248: bdb0b7     jsr      $b0b7
F24B: 3264       leas     4, s
F24D: 39         rts      
F24E: 3b         rti      
F24F: 0025       neg      $25
F251: 6825       lsl      5, y
F253: 6820       lsl      0, y
F255: 0025       neg      $25
F257: 6820       lsl      0, y
F259: 002a       neg      $2a
F25B: 0025       neg      $25
F25D: 6300       com      0, x
F25F: 2e00       bgt      $f261
F261: 256e       bcs      $f2d1
F263: 0032       neg      $32
F265: 7cbdfd     inc      $bdfd
F268: 26f6       bne      $f260
F26A: 01         fcb      $01
F26B: 74262e     lsr      $262e
F26E: bdfd66     jsr      $fd66
F271: ede4       std      , s
F273: 8300ff     subd     #255
F276: 2e24       bgt      $f29c
F278: fc0172     ldd      $0172
F27B: b30170     subd     $0170
F27E: ed62       std      2, s
F280: fc0170     ldd      $0170
F283: 3406       pshs     a, b
F285: c30001     addd     #1
F288: fd0170     std      $0170
F28B: ec62       ldd      2, s
F28D: 3510       puls     x
F28F: e784       stb      , x
F291: ec62       ldd      2, s
F293: c3ffff     addd     #-1
F296: ed62       std      2, s
F298: 2ee6       bgt      $f280
F29A: 2003       bra      $f29f
F29C: bdfdc6     jsr      $fdc6
F29F: 3264       leas     4, s
F2A1: 39         rts      
F2A2: 327c       leas     -4, s
F2A4: bdfd26     jsr      $fd26
F2A7: f60174     ldb      $0174
F2AA: 264e       bne      $f2fa
F2AC: fc0170     ldd      $0170
F2AF: 2710       beq      $f2c1
F2B1: 4f         clra     
F2B2: 5f         clrb     
F2B3: 58         lslb     
F2B4: 49         rola     
F2B5: c30182     addd     #386
F2B8: 3406       pshs     a, b
F2BA: fc0170     ldd      $0170
F2BD: 3510       puls     x
F2BF: ed84       std      , x
F2C1: 4f         clra     
F2C2: 5f         clrb     
F2C3: ed62       std      2, s
F2C5: ec62       ldd      2, s
F2C7: 58         lslb     
F2C8: 49         rola     
F2C9: c30176     addd     #374
F2CC: 3406       pshs     a, b
F2CE: 3510       puls     x
F2D0: ec84       ldd      , x
F2D2: ede4       std      , s
F2D4: 271f       beq      $f2f5
F2D6: ec62       ldd      2, s
F2D8: c3017e     addd     #382
F2DB: 3406       pshs     a, b
F2DD: 4f         clra     
F2DE: e6f802     ldb      [2, s]
F2E1: 3510       puls     x
F2E3: e784       stb      , x
F2E5: c63f       ldb      #63
F2E7: e7f4       stb      [, s]
F2E9: ec62       ldd      2, s
F2EB: c30001     addd     #1
F2EE: ed62       std      2, s
F2F0: 830004     subd     #4
F2F3: 26d0       bne      $f2c5
F2F5: bdfe17     jsr      $fe17
F2F8: 2006       bra      $f300
F2FA: bdfdbd     jsr      $fdbd
F2FD: bdfdc6     jsr      $fdc6
F300: 3264       leas     4, s
F302: 39         rts      
F303: 32e8e6     leas     -26, s
F306: bdb0c6     jsr      $b0c6
F309: e7e4       stb      , s
F30B: c120       cmpb     #32
F30D: 27f7       beq      $f306
F30F: 4f         clra     
F310: 5f         clrb     
F311: ed61       std      1, s
F313: e6e4       ldb      , s
F315: c10d       cmpb     #13
F317: 272b       beq      $f344
F319: ec61       ldd      1, s
F31B: 3406       pshs     a, b
F31D: 1f41       tfr      s, x
F31F: c607       ldb      #7
F321: 3a         abx      
F322: 3410       pshs     x
F324: 3506       puls     a, b
F326: e3e4       addd     , s
F328: 3510       puls     x
F32A: 3406       pshs     a, b
F32C: 4f         clra     
F32D: e662       ldb      2, s
F32F: 3510       puls     x
F331: e784       stb      , x
F333: bdb0c6     jsr      $b0c6
F336: e7e4       stb      , s
F338: ec61       ldd      1, s
F33A: c30001     addd     #1
F33D: ed61       std      1, s
F33F: 830014     subd     #20
F342: 26cf       bne      $f313
F344: e6e4       ldb      , s
F346: c10d       cmpb     #13
F348: 264a       bne      $f394
F34A: ec61       ldd      1, s
F34C: 3406       pshs     a, b
F34E: 1f41       tfr      s, x
F350: c607       ldb      #7
F352: 3a         abx      
F353: 3410       pshs     x
F355: 3506       puls     a, b
F357: e3e4       addd     , s
F359: 3510       puls     x
F35B: 3406       pshs     a, b
F35D: 4f         clra     
F35E: 5f         clrb     
F35F: 3510       puls     x
F361: e784       stb      , x
F363: ccf41b     ldd      #-3045
F366: 3406       pshs     a, b
F368: 1f41       tfr      s, x
F36A: c607       ldb      #7
F36C: 3a         abx      
F36D: 3410       pshs     x
F36F: 3506       puls     a, b
F371: bdb0ae     jsr      $b0ae
F374: 3262       leas     2, s
F376: ed63       std      3, s
F378: 2611       bne      $f38b
F37A: 1f41       tfr      s, x
F37C: c605       ldb      #5
F37E: 3a         abx      
F37F: 3410       pshs     x
F381: ccf41d     ldd      #-3043
F384: bdb0b7     jsr      $b0b7
F387: 3262       leas     2, s
F389: 200f       bra      $f39a
F38B: 8d11       bsr      $f39e
F38D: ec63       ldd      3, s
F38F: bdb0b1     jsr      $b0b1
F392: 2006       bra      $f39a
F394: ccf431     ldd      #-3023
F397: bdb0b7     jsr      $b0b7
F39A: 32e81a     leas     26, s
F39D: 39         rts      
F39E: 3406       pshs     a, b
F3A0: 3274       leas     -12, s
F3A2: 1f41       tfr      s, x
F3A4: c606       ldb      #6
F3A6: 3a         abx      
F3A7: 3410       pshs     x
F3A9: 3506       puls     a, b
F3AB: ed64       std      4, s
F3AD: cc0006     ldd      #6
F3B0: 3406       pshs     a, b
F3B2: ec66       ldd      6, s
F3B4: 3406       pshs     a, b
F3B6: ece810     ldd      16, s
F3B9: 8d40       bsr      $f3fb
F3BB: 3264       leas     4, s
F3BD: ec6c       ldd      12, s
F3BF: bdb0e1     jsr      $b0e1
F3C2: 2628       bne      $f3ec
F3C4: ae64       ldx      4, s
F3C6: e604       ldb      4, x
F3C8: c102       cmpb     #2
F3CA: 2726       beq      $f3f2
F3CC: c101       cmpb     #1
F3CE: 2606       bne      $f3d6
F3D0: 4f         clra     
F3D1: e605       ldb      5, x
F3D3: f7effc     stb      $effc
F3D6: ec02       ldd      2, x
F3D8: 3406       pshs     a, b
F3DA: ec84       ldd      , x
F3DC: 3406       pshs     a, b
F3DE: ece810     ldd      16, s
F3E1: 8d18       bsr      $f3fb
F3E3: 3264       leas     4, s
F3E5: ec6c       ldd      12, s
F3E7: bdb0e1     jsr      $b0e1
F3EA: 27c1       beq      $f3ad
F3EC: ccf443     ldd      #-3005
F3EF: bdb0b7     jsr      $b0b7
F3F2: 7feffc     clr      $effc
F3F5: 7f0220     clr      $0220
F3F8: 326e       leas     14, s
F3FA: 39         rts      
F3FB: 3406       pshs     a, b
F3FD: ec64       ldd      4, s
F3FF: 3406       pshs     a, b
F401: c30001     addd     #1
F404: ed66       std      6, s
F406: ec62       ldd      2, s
F408: bdb0d8     jsr      $b0d8
F40B: 3510       puls     x
F40D: e784       stb      , x
F40F: ec66       ldd      6, s
F411: c3ffff     addd     #-1
F414: ed66       std      6, s
F416: 26e5       bne      $f3fd
F418: 3262       leas     2, s
F41A: 39         rts      
F41B: 4c         inca     
F41C: 0075       neg      $75
F41E: 6e61       jmp      1, s
F420: 62         fcb      $62
F421: 6c65       inc      5, s
F423: 2074       bra      $f499
F425: 6f20       clr      0, y
F427: 6f70       clr      -16, s
F429: 65         fcb      $65
F42A: 6e20       jmp      0, y
F42C: 2573       bcs      $f4a1
F42E: 256e       bcs      $f49e
F430: 0069       neg      $69
F432: 6e76       jmp      -10, s
F434: 61         fcb      $61
F435: 6c69       inc      9, s
F437: 6420       lsr      0, y
F439: 636f       com      15, s
F43B: 6d6d       tst      13, s
F43D: 61         fcb      $61
F43E: 6e64       jmp      4, s
F440: 256e       bcs      $f4b0
F442: 0065       neg      $65
F444: 72         fcb      $72
F445: 72         fcb      $72
F446: 6f72       clr      -14, s
F448: 206c       bra      $f4b6
F44A: 6f61       clr      1, s
F44C: 6469       lsr      9, s
F44E: 6e67       jmp      7, s
F450: 2066       bra      $f4b8
F452: 696c       rol      12, s
F454: 65         fcb      $65
F455: 256e       bcs      $f4c5
F457: 0032       neg      $32
F459: 7ebdfd     jmp      $bdfd
F45C: 26f6       bne      $f454
F45E: 01         fcb      $01
F45F: 742637     lsr      $2637
F462: bdfd66     jsr      $fd66
F465: ede4       std      , s
F467: f60175     ldb      $0175
F46A: c120       cmpb     #32
F46C: 261a       bne      $f488
F46E: ece4       ldd      , s
F470: 8300ff     subd     #255
F473: 2e24       bgt      $f499
F475: fc0170     ldd      $0170
F478: 3406       pshs     a, b
F47A: c30001     addd     #1
F47D: fd0170     std      $0170
F480: ec62       ldd      2, s
F482: 3510       puls     x
F484: e784       stb      , x
F486: 20da       bra      $f462
F488: c10d       cmpb     #13
F48A: 260d       bne      $f499
F48C: be0170     ldx      $0170
F48F: 3410       pshs     x
F491: ec62       ldd      2, s
F493: 3510       puls     x
F495: e784       stb      , x
F497: 2012       bra      $f4ab
F499: f60175     ldb      $0175
F49C: c12a       cmpb     #42
F49E: 2605       bne      $f4a5
F4A0: bdfdbd     jsr      $fdbd
F4A3: 2006       bra      $f4ab
F4A5: bdfdbd     jsr      $fdbd
F4A8: bdfdc6     jsr      $fdc6
F4AB: 3262       leas     2, s
F4AD: 39         rts      
F4AE: bdb0c6     jsr      $b0c6
F4B1: 83000d     subd     #13
F4B4: 26f8       bne      $f4ae
F4B6: 7eb075     jmp      $b075
F4B9: bdfdbd     jsr      $fdbd
F4BC: 8d07       bsr      $f4c5
F4BE: 8d0b       bsr      $f4cb
F4C0: ccf5a7     ldd      #-2649
F4C3: 2003       bra      $f4c8
F4C5: ccf5aa     ldd      #-2646
F4C8: 7eb0b7     jmp      $b0b7
F4CB: 327c       leas     -4, s
F4CD: ccf5d1     ldd      #-2607
F4D0: bdb0b7     jsr      $b0b7
F4D3: cc0006     ldd      #6
F4D6: ed62       std      2, s
F4D8: cc0170     ldd      #368
F4DB: c30012     addd     #18
F4DE: ede4       std      , s
F4E0: ece4       ldd      , s
F4E2: 3406       pshs     a, b
F4E4: c30001     addd     #1
F4E7: ed62       std      2, s
F4E9: 3510       puls     x
F4EB: 4f         clra     
F4EC: e684       ldb      , x
F4EE: 3406       pshs     a, b
F4F0: ccf5d3     ldd      #-2605
F4F3: bdb0b7     jsr      $b0b7
F4F6: 3262       leas     2, s
F4F8: ece4       ldd      , s
F4FA: 3406       pshs     a, b
F4FC: c30001     addd     #1
F4FF: ed62       std      2, s
F501: 3510       puls     x
F503: 4f         clra     
F504: e684       ldb      , x
F506: 3406       pshs     a, b
F508: ccf5d6     ldd      #-2602
F50B: bdb0b7     jsr      $b0b7
F50E: 3262       leas     2, s
F510: ec62       ldd      2, s
F512: c3ffff     addd     #-1
F515: ed62       std      2, s
F517: 26c7       bne      $f4e0
F519: ece4       ldd      , s
F51B: 3406       pshs     a, b
F51D: c30001     addd     #1
F520: ed62       std      2, s
F522: 3510       puls     x
F524: 4f         clra     
F525: e684       ldb      , x
F527: 3406       pshs     a, b
F529: ccf5d6     ldd      #-2602
F52C: bdb0b7     jsr      $b0b7
F52F: 3262       leas     2, s
F531: 4f         clra     
F532: e6f4       ldb      [, s]
F534: 3406       pshs     a, b
F536: ccf5d3     ldd      #-2605
F539: bdb0b7     jsr      $b0b7
F53C: 2066       bra      $f5a4
F53E: 327a       leas     -6, s
F540: cc0006     ldd      #6
F543: ed64       std      4, s
F545: cc0170     ldd      #368
F548: c30012     addd     #18
F54B: ede4       std      , s
F54D: ece4       ldd      , s
F54F: 3406       pshs     a, b
F551: c30002     addd     #2
F554: ed62       std      2, s
F556: bdfd66     jsr      $fd66
F559: 3510       puls     x
F55B: ed84       std      , x
F55D: f60175     ldb      $0175
F560: c120       cmpb     #32
F562: 2634       bne      $f598
F564: ec64       ldd      4, s
F566: c3ffff     addd     #-1
F569: ed64       std      4, s
F56B: 26e0       bne      $f54d
F56D: ece4       ldd      , s
F56F: ed62       std      2, s
F571: cc0002     ldd      #2
F574: ed64       std      4, s
F576: ec62       ldd      2, s
F578: 3406       pshs     a, b
F57A: c30001     addd     #1
F57D: ed64       std      4, s
F57F: bdfd66     jsr      $fd66
F582: 3510       puls     x
F584: e784       stb      , x
F586: f60175     ldb      $0175
F589: c120       cmpb     #32
F58B: 260b       bne      $f598
F58D: ec64       ldd      4, s
F58F: c3ffff     addd     #-1
F592: ed64       std      4, s
F594: 26e0       bne      $f576
F596: 200c       bra      $f5a4
F598: c10d       cmpb     #13
F59A: 2602       bne      $f59e
F59C: 2006       bra      $f5a4
F59E: bdfdbd     jsr      $fdbd
F5A1: bdfdc6     jsr      $fdc6
F5A4: 3266       leas     6, s
F5A6: 39         rts      
F5A7: 256e       bcs      $f617
F5A9: 0020       neg      $20
F5AB: 2050       bra      $f5fd
F5AD: 43         coma     
F5AE: 2020       bra      $f5d0
F5B0: 2044       bra      $f5f6
F5B2: 2020       bra      $f5d4
F5B4: 2020       bra      $f5d6
F5B6: 58         lslb     
F5B7: 2020       bra      $f5d9
F5B9: 2020       bra      $f5db
F5BB: 59         rolb     
F5BC: 2020       bra      $f5de
F5BE: 2020       bra      $f5e0
F5C0: 55         fcb      $55
F5C1: 2020       bra      $f5e3
F5C3: 2020       bra      $f5e5
F5C5: 53         comb     
F5C6: 2020       bra      $f5e8
F5C8: 2043       bra      $f60d
F5CA: 43         coma     
F5CB: 2044       bra      $f611
F5CD: 50         negb     
F5CE: 256e       bcs      $f63e
F5D0: 003a       neg      $3a
F5D2: 0025       neg      $25
F5D4: 6800       lsl      0, x
F5D6: 2568       bcs      $f640
F5D8: 2000       bra      $f5da
F5DA: 327e       leas     -2, s
F5DC: bdfd26     jsr      $fd26
F5DF: f60174     ldb      $0174
F5E2: 10260093   lbne     $f679
F5E6: fc0170     ldd      $0170
F5E9: 273e       beq      $f629
F5EB: 4f         clra     
F5EC: 5f         clrb     
F5ED: ede4       std      , s
F5EF: ece4       ldd      , s
F5F1: 58         lslb     
F5F2: 49         rola     
F5F3: c30176     addd     #374
F5F6: 3406       pshs     a, b
F5F8: 3510       puls     x
F5FA: ec84       ldd      , x
F5FC: 270c       beq      $f60a
F5FE: ece4       ldd      , s
F600: c30001     addd     #1
F603: ede4       std      , s
F605: 830004     subd     #4
F608: 26e5       bne      $f5ef
F60A: ece4       ldd      , s
F60C: 830004     subd     #4
F60F: 2c12       bge      $f623
F611: ece4       ldd      , s
F613: 58         lslb     
F614: 49         rola     
F615: c30176     addd     #374
F618: 3406       pshs     a, b
F61A: fc0170     ldd      $0170
F61D: 3510       puls     x
F61F: ed84       std      , x
F621: 2006       bra      $f629
F623: ccf6c2     ldd      #-2366
F626: bdb0b7     jsr      $b0b7
F629: 4f         clra     
F62A: 5f         clrb     
F62B: ede4       std      , s
F62D: ece4       ldd      , s
F62F: 58         lslb     
F630: 49         rola     
F631: c30176     addd     #374
F634: 3406       pshs     a, b
F636: 3510       puls     x
F638: ec84       ldd      , x
F63A: 2735       beq      $f671
F63C: ece4       ldd      , s
F63E: 58         lslb     
F63F: 49         rola     
F640: c30176     addd     #374
F643: 3406       pshs     a, b
F645: 3510       puls     x
F647: ec84       ldd      , x
F649: 3406       pshs     a, b
F64B: ec62       ldd      2, s
F64D: 58         lslb     
F64E: 49         rola     
F64F: c30176     addd     #374
F652: 3406       pshs     a, b
F654: 3510       puls     x
F656: ec84       ldd      , x
F658: 1f89       tfr      a, b
F65A: 4f         clra     
F65B: 3406       pshs     a, b
F65D: ccf6d7     ldd      #-2345
F660: bdb0b7     jsr      $b0b7
F663: 3264       leas     4, s
F665: ece4       ldd      , s
F667: c30001     addd     #1
F66A: ede4       std      , s
F66C: 830004     subd     #4
F66F: 26bc       bne      $f62d
F671: ccf6dd     ldd      #-2339
F674: bdb0b7     jsr      $b0b7
F677: 2006       bra      $f67f
F679: bdfdbd     jsr      $fdbd
F67C: bdfdc6     jsr      $fdc6
F67F: 3262       leas     2, s
F681: 39         rts      
F682: 327c       leas     -4, s
F684: ccf6e0     ldd      #-2336
F687: bdb0b7     jsr      $b0b7
F68A: bdf4bc     jsr      $f4bc
F68D: 4f         clra     
F68E: 5f         clrb     
F68F: ed62       std      2, s
F691: ec62       ldd      2, s
F693: 58         lslb     
F694: 49         rola     
F695: c30176     addd     #374
F698: 3406       pshs     a, b
F69A: 3510       puls     x
F69C: ec84       ldd      , x
F69E: ede4       std      , s
F6A0: 270e       beq      $f6b0
F6A2: ec62       ldd      2, s
F6A4: c3017e     addd     #382
F6A7: 3406       pshs     a, b
F6A9: 4f         clra     
F6AA: 3510       puls     x
F6AC: e684       ldb      , x
F6AE: e7f4       stb      [, s]
F6B0: ec62       ldd      2, s
F6B2: c30001     addd     #1
F6B5: ed62       std      2, s
F6B7: 830004     subd     #4
F6BA: 26d5       bne      $f691
F6BC: bdf0cd     jsr      $f0cd
F6BF: 3264       leas     4, s
F6C1: 39         rts      
F6C2: 746f6f     lsr      $6f6f
F6C5: 206d       bra      $f734
F6C7: 61         fcb      $61
F6C8: 6e79       jmp      -7, s
F6CA: 2073       bra      $f73f
F6CC: 746f70     lsr      $6f70
F6CF: 732073     com      $2073
F6D2: 65         fcb      $65
F6D3: 74256e     lsr      $256e
F6D6: 0025       neg      $25
F6D8: 6825       lsl      5, y
F6DA: 6820       lsl      0, y
F6DC: 0025       neg      $25
F6DE: 6e00       jmp      0, x
F6E0: 696e       rol      14, s
F6E2: 746572     lsr      $6572
F6E5: 72         fcb      $72
F6E6: 75         fcb      $75
F6E7: 707425     neg      $7425
F6EA: 6e00       jmp      0, x
F6EC: bfbf4e     stx      $bf4e
F6EF: 4f         clra     
F6F0: d053       subb     $53
F6F2: 59         rolb     
F6F3: 4e         fcb      $4e
F6F4: c3bfbf     addd     #-16449
F6F7: 4c         inca     
F6F8: 42         fcb      $42
F6F9: 52         fcb      $52
F6FA: c14c       cmpb     #76
F6FC: 42         fcb      $42
F6FD: 53         comb     
F6FE: d2bf       sbcb     $bf
F700: 44         lsra     
F701: 41         fcb      $41
F702: c14f       cmpb     #79
F704: 52         fcb      $52
F705: 43         coma     
F706: c3bf41     addd     #-16575
F709: 4e         fcb      $4e
F70A: 44         lsra     
F70B: 43         coma     
F70C: c35345     addd     #21317
F70F: d845       eorb     $45
F711: 58         lslb     
F712: 47         asra     
F713: a054       suba     -12, u
F715: 46         rora     
F716: 52         fcb      $52
F717: a042       suba     2, u
F719: 52         fcb      $52
F71A: c142       cmpb     #66
F71C: 52         fcb      $52
F71D: ce4248     ldu      #16968
F720: c942       adcb     #66
F722: 4c         inca     
F723: d342       addd     $42
F725: 48         lsla     
F726: d342       addd     $42
F728: 4c         inca     
F729: cf         fcb      $cf
F72A: 42         fcb      $42
F72B: 4e         fcb      $4e
F72C: c542       bitb     #66
F72E: 45         fcb      $45
F72F: d142       cmpb     $42
F731: 56         rorb     
F732: c34256     addd     #16982
F735: d342       addd     $42
F737: 50         negb     
F738: cc424d     ldd      #16973
F73B: c942       adcb     #66
F73D: 47         asra     
F73E: c542       bitb     #66
F740: 4c         inca     
F741: d442       andb     $42
F743: 47         asra     
F744: d442       andb     $42
F746: 4c         inca     
F747: c54c       bitb     #76
F749: 45         fcb      $45
F74A: 41         fcb      $41
F74B: d84c       eorb     $4c
F74D: 45         fcb      $45
F74E: 41         fcb      $41
F74F: d94c       adcb     $4c
F751: 45         fcb      $45
F752: 41         fcb      $41
F753: d34c       addd     $4c
F755: 45         fcb      $45
F756: 41         fcb      $41
F757: d550       bitb     $50
F759: 53         comb     
F75A: 48         lsla     
F75B: d350       addd     $50
F75D: 55         fcb      $55
F75E: 4c         inca     
F75F: d350       addd     $50
F761: 53         comb     
F762: 48         lsla     
F763: d550       bitb     $50
F765: 55         fcb      $55
F766: 4c         inca     
F767: d5bf       bitb     $bf
F769: 52         fcb      $52
F76A: 54         lsrb     
F76B: d341       addd     $41
F76D: 42         fcb      $42
F76E: d852       eorb     $52
F770: 54         lsrb     
F771: c943       adcb     #67
F773: 57         asrb     
F774: 41         fcb      $41
F775: c94d       adcb     #77
F777: 55         fcb      $55
F778: ccbf53     ldd      #-16557
F77B: 57         asrb     
F77C: c94e       adcb     #78
F77E: 45         fcb      $45
F77F: c7         fcb      $c7
F780: bfbf43     stx      $bf43
F783: 4f         clra     
F784: cd         fcb      $cd
F785: 4c         inca     
F786: 53         comb     
F787: d2bf       sbcb     $bf
F789: 52         fcb      $52
F78A: 4f         clra     
F78B: d241       sbcb     $41
F78D: 53         comb     
F78E: d241       sbcb     $41
F790: 53         comb     
F791: cc524f     ldd      #21071
F794: cc4445     ldd      #17477
F797: c3bf49     addd     #-16567
F79A: 4e         fcb      $4e
F79B: c35453     addd     #21587
F79E: d44a       andb     $4a
F7A0: 4d         tsta     
F7A1: d043       subb     $43
F7A3: 4c         inca     
F7A4: d253       sbcb     $53
F7A6: 55         fcb      $55
F7A7: 42         fcb      $42
F7A8: c143       cmpb     #67
F7AA: 4d         tsta     
F7AB: 50         negb     
F7AC: c153       cmpb     #83
F7AE: 42         fcb      $42
F7AF: 43         coma     
F7B0: c153       cmpb     #83
F7B2: 55         fcb      $55
F7B3: 42         fcb      $42
F7B4: c441       andb     #65
F7B6: 4e         fcb      $4e
F7B7: 44         lsra     
F7B8: c142       cmpb     #66
F7BA: 49         rola     
F7BB: 54         lsrb     
F7BC: c14c       cmpb     #76
F7BE: 44         lsra     
F7BF: 41         fcb      $41
F7C0: a053       suba     -13, u
F7C2: 54         lsrb     
F7C3: 41         fcb      $41
F7C4: a045       suba     5, u
F7C6: 4f         clra     
F7C7: 52         fcb      $52
F7C8: c141       cmpb     #65
F7CA: 44         lsra     
F7CB: 43         coma     
F7CC: c14f       cmpb     #79
F7CE: 52         fcb      $52
F7CF: 41         fcb      $41
F7D0: a041       suba     1, u
F7D2: 44         lsra     
F7D3: 44         lsra     
F7D4: c143       cmpb     #67
F7D6: 4d         tsta     
F7D7: 50         negb     
F7D8: d84a       eorb     $4a
F7DA: 53         comb     
F7DB: 52         fcb      $52
F7DC: a04c       suba     12, u
F7DE: 44         lsra     
F7DF: 58         lslb     
F7E0: a053       suba     -13, u
F7E2: 54         lsrb     
F7E3: 58         lslb     
F7E4: a053       suba     -13, u
F7E6: 55         fcb      $55
F7E7: 42         fcb      $42
F7E8: c243       sbcb     #67
F7EA: 4d         tsta     
F7EB: 50         negb     
F7EC: c253       sbcb     #83
F7EE: 42         fcb      $42
F7EF: 43         coma     
F7F0: c241       sbcb     #65
F7F2: 44         lsra     
F7F3: 44         lsra     
F7F4: c441       andb     #65
F7F6: 4e         fcb      $4e
F7F7: 44         lsra     
F7F8: c242       sbcb     #66
F7FA: 49         rola     
F7FB: 54         lsrb     
F7FC: c24c       sbcb     #76
F7FE: 44         lsra     
F7FF: 42         fcb      $42
F800: a053       suba     -13, u
F802: 54         lsrb     
F803: 42         fcb      $42
F804: a045       suba     5, u
F806: 4f         clra     
F807: 52         fcb      $52
F808: c241       sbcb     #65
F80A: 44         lsra     
F80B: 43         coma     
F80C: c24f       sbcb     #79
F80E: 52         fcb      $52
F80F: 42         fcb      $42
F810: a041       suba     1, u
F812: 44         lsra     
F813: 44         lsra     
F814: c24c       sbcb     #76
F816: 44         lsra     
F817: 44         lsra     
F818: a053       suba     -13, u
F81A: 54         lsrb     
F81B: 44         lsra     
F81C: a04c       suba     12, u
F81E: 44         lsra     
F81F: 55         fcb      $55
F820: a053       suba     -13, u
F822: 54         lsrb     
F823: 55         fcb      $55
F824: a0         fcb      $a0
F825: f77df9     stb      $7df9
F828: 6df6       tst      [a, s]
F82A: ec         fcb      $ec
F82B: faf8f7     orb      $f8f7
F82E: 18         fcb      $18
F82F: fa9ef7     orb      $9ef7
F832: 48         lsla     
F833: fb25f7     addb     $25f7
F836: 7dfaed     tst      $faed
F839: f77dfa     stb      $7dfa
F83C: f2f77d     sbcb     $f77d
F83F: f98bf7     adcb     $8bf7
F842: 7df977     tst      $f977
F845: f7a5f9     stb      $a5f9
F848: 45         fcb      $45
F849: f7a5f9     stb      $a5f9
F84C: 6f         fcb      $6f
F84D: f7a5f9     stb      $a5f9
F850: 8df7       bsr      $f849
F852: a5f979f7   bita     [31223, s]
F856: e5f945f7   bitb     [17911, s]
F85A: e5f96ff7   bitb     [28663, s]
F85E: e5f98df7   bitb     [-29193, s]
F862: e5f9794e   bitb     [31054, s]
F866: 5e         fcb      $5e
F867: 87         fcb      $87
F868: 8f         fcb      $8f
F869: c7         fcb      $c7
F86A: cd         fcb      $cd
F86B: cf         fcb      $cf
F86C: 0058       neg      $58
F86E: 59         rolb     
F86F: 55         fcb      $55
F870: 53         comb     
F871: bdfd26     jsr      $fd26
F874: f60174     ldb      $0174
F877: 2628       bne      $f8a1
F879: fc0172     ldd      $0172
F87C: b30170     subd     $0170
F87F: fd0190     std      $0190
F882: fc0170     ldd      $0170
F885: 3406       pshs     a, b
F887: 1f89       tfr      a, b
F889: 4f         clra     
F88A: 3406       pshs     a, b
F88C: ccfcab     ldd      #-853
F88F: bdb0b7     jsr      $b0b7
F892: 3264       leas     4, s
F894: 8d12       bsr      $f8a8
F896: bdb081     jsr      $b081
F899: 2605       bne      $f8a0
F89B: fc0190     ldd      $0190
F89E: 2ee2       bgt      $f882
F8A0: 39         rts      
F8A1: bdfdbd     jsr      $fdbd
F8A4: 7efdc6     jmp      $fdc6
F8A7: 39         rts      
F8A8: 327b       leas     -5, s
F8AA: bdfb59     jsr      $fb59
F8AD: e7e4       stb      , s
F8AF: ccf865     ldd      #-1947
F8B2: ed61       std      1, s
F8B4: e6f801     ldb      [1, s]
F8B7: 2713       beq      $f8cc
F8B9: ec61       ldd      1, s
F8BB: 3406       pshs     a, b
F8BD: c30001     addd     #1
F8C0: ed63       std      3, s
F8C2: 3510       puls     x
F8C4: e6e4       ldb      , s
F8C6: e184       cmpb     , x
F8C8: 276c       beq      $f936
F8CA: 20e8       bra      $f8b4
F8CC: e6e4       ldb      , s
F8CE: c18d       cmpb     #-115
F8D0: 260b       bne      $f8dd
F8D2: ccfcb2     ldd      #-846
F8D5: bdb0b7     jsr      $b0b7
F8D8: bdfa9e     jsr      $fa9e
F8DB: 205f       bra      $f93c
F8DD: c110       cmpb     #16
F8DF: 2605       bne      $f8e6
F8E1: bdfb73     jsr      $fb73
F8E4: 2056       bra      $f93c
F8E6: c111       cmpb     #17
F8E8: 2605       bne      $f8ef
F8EA: bdfc0d     jsr      $fc0d
F8ED: 204d       bra      $f93c
F8EF: 4f         clra     
F8F0: c4f0       andb     #-16
F8F2: 44         lsra     
F8F3: 56         rorb     
F8F4: 44         lsra     
F8F5: 56         rorb     
F8F6: 44         lsra     
F8F7: 56         rorb     
F8F8: ed63       std      3, s
F8FA: 58         lslb     
F8FB: 49         rola     
F8FC: c3f825     addd     #-2011
F8FF: 3406       pshs     a, b
F901: 3510       puls     x
F903: ec84       ldd      , x
F905: 3406       pshs     a, b
F907: 4f         clra     
F908: e662       ldb      2, s
F90A: bdfc5a     jsr      $fc5a
F90D: 3262       leas     2, s
F90F: ed61       std      1, s
F911: e6f801     ldb      [1, s]
F914: c1bf       cmpb     #-65
F916: 271e       beq      $f936
F918: ec61       ldd      1, s
F91A: bdfc87     jsr      $fc87
F91D: 4f         clra     
F91E: e6e4       ldb      , s
F920: 3406       pshs     a, b
F922: ec65       ldd      5, s
F924: c30001     addd     #1
F927: 58         lslb     
F928: 49         rola     
F929: c3f825     addd     #-2011
F92C: 3406       pshs     a, b
F92E: 3510       puls     x
F930: 3506       puls     a, b
F932: ad94       jsr      [, x]
F934: 2006       bra      $f93c
F936: ccfcb6     ldd      #-842
F939: bdb0b7     jsr      $b0b7
F93C: ccfcbb     ldd      #-837
F93F: bdb0b7     jsr      $b0b7
F942: 3265       leas     5, s
F944: 39         rts      
F945: 3406       pshs     a, b
F947: ccfcbe     ldd      #-834
F94A: bdb0b7     jsr      $b0b7
F94D: bdfb4c     jsr      $fb4c
F950: 4f         clra     
F951: e661       ldb      1, s
F953: c40f       andb     #15
F955: e761       stb      1, s
F957: c103       cmpb     #3
F959: 270c       beq      $f967
F95B: c10c       cmpb     #12
F95D: 2708       beq      $f967
F95F: c10d       cmpb     #13
F961: 2704       beq      $f967
F963: c10e       cmpb     #14
F965: 2603       bne      $f96a
F967: bdfb4c     jsr      $fb4c
F96A: 3262       leas     2, s
F96C: 39         rts      
F96D: 8d16       bsr      $f985
F96F: ccfcc2     ldd      #-830
F972: bdb0b7     jsr      $b0b7
F975: 200b       bra      $f982
F977: 8d0c       bsr      $f985
F979: ccfcc2     ldd      #-830
F97C: bdb0b7     jsr      $b0b7
F97F: bdfb4c     jsr      $fb4c
F982: 7efb4c     jmp      $fb4c
F985: cc0020     ldd      #32
F988: 7eb0bd     jmp      $b0bd
F98B: 8df8       bsr      $f985
F98D: 327c       leas     -4, s
F98F: bdfb59     jsr      $fb59
F992: e7e4       stb      , s
F994: ccfcc5     ldd      #-827
F997: bdb0b7     jsr      $b0b7
F99A: 4f         clra     
F99B: e6e4       ldb      , s
F99D: c460       andb     #96
F99F: 44         lsra     
F9A0: 56         rorb     
F9A1: 44         lsra     
F9A2: 56         rorb     
F9A3: 44         lsra     
F9A4: 56         rorb     
F9A5: 44         lsra     
F9A6: 56         rorb     
F9A7: 44         lsra     
F9A8: 56         rorb     
F9A9: e763       stb      3, s
F9AB: 4f         clra     
F9AC: e6e4       ldb      , s
F9AE: c40f       andb     #15
F9B0: e761       stb      1, s
F9B2: e6e4       ldb      , s
F9B4: c480       andb     #-128
F9B6: 261e       bne      $f9d6
F9B8: e6e4       ldb      , s
F9BA: c41f       andb     #31
F9BC: e762       stb      2, s
F9BE: c10f       cmpb     #15
F9C0: 2304       bls      $f9c6
F9C2: cae0       orb      #-32
F9C4: e762       stb      2, s
F9C6: 3406       pshs     a, b
F9C8: ccfcc7     ldd      #-825
F9CB: bdb0b7     jsr      $b0b7
F9CE: 3262       leas     2, s
F9D0: c604       ldb      #4
F9D2: e761       stb      1, s
F9D4: 200c       bra      $f9e2
F9D6: e6e4       ldb      , s
F9D8: c410       andb     #16
F9DA: 2706       beq      $f9e2
F9DC: ccfccb     ldd      #-821
F9DF: bdb0b7     jsr      $b0b7
F9E2: e661       ldb      1, s
F9E4: c108       cmpb     #8
F9E6: 2704       beq      $f9ec
F9E8: c10c       cmpb     #12
F9EA: 2608       bne      $f9f4
F9EC: ccfccd     ldd      #-819
F9EF: bdb0b7     jsr      $b0b7
F9F2: 2015       bra      $fa09
F9F4: c109       cmpb     #9
F9F6: 2708       beq      $fa00
F9F8: c10d       cmpb     #13
F9FA: 2704       beq      $fa00
F9FC: c10f       cmpb     #15
F9FE: 260e       bne      $fa0e
FA00: ccfccd     ldd      #-819
FA03: bdb0b7     jsr      $b0b7
FA06: bdfb4c     jsr      $fb4c
FA09: bdfb4c     jsr      $fb4c
FA0C: 201c       bra      $fa2a
FA0E: c105       cmpb     #5
FA10: 2605       bne      $fa17
FA12: ccfccf     ldd      #-817
FA15: 2010       bra      $fa27
FA17: c106       cmpb     #6
FA19: 2605       bne      $fa20
FA1B: ccfcd1     ldd      #-815
FA1E: 2007       bra      $fa27
FA20: c10b       cmpb     #11
FA22: 2606       bne      $fa2a
FA24: ccfcd3     ldd      #-813
FA27: bdb0b7     jsr      $b0b7
FA2A: e661       ldb      1, s
FA2C: c10f       cmpb     #15
FA2E: 2744       beq      $fa74
FA30: ccfcd5     ldd      #-811
FA33: bdb0b7     jsr      $b0b7
FA36: e661       ldb      1, s
FA38: c102       cmpb     #2
FA3A: 2605       bne      $fa41
FA3C: ccfcd7     ldd      #-809
FA3F: 2007       bra      $fa48
FA41: c103       cmpb     #3
FA43: 2606       bne      $fa4b
FA45: ccfcd9     ldd      #-807
FA48: bdb0b7     jsr      $b0b7
FA4B: e661       ldb      1, s
FA4D: c10c       cmpb     #12
FA4F: 2704       beq      $fa55
FA51: c10d       cmpb     #13
FA53: 2608       bne      $fa5d
FA55: ccfcdc     ldd      #-804
FA58: bdb0b7     jsr      $b0b7
FA5B: 2017       bra      $fa74
FA5D: 4f         clra     
FA5E: e663       ldb      3, s
FA60: c3f86d     addd     #-1939
FA63: 3406       pshs     a, b
FA65: 4f         clra     
FA66: 3510       puls     x
FA68: e684       ldb      , x
FA6A: 3406       pshs     a, b
FA6C: ccfce0     ldd      #-800
FA6F: bdb0b7     jsr      $b0b7
FA72: 3262       leas     2, s
FA74: e661       ldb      1, s
FA76: 2605       bne      $fa7d
FA78: ccfce3     ldd      #-797
FA7B: 2007       bra      $fa84
FA7D: c101       cmpb     #1
FA7F: 2606       bne      $fa87
FA81: ccfce5     ldd      #-795
FA84: bdb0b7     jsr      $b0b7
FA87: 4f         clra     
FA88: e6e4       ldb      , s
FA8A: c410       andb     #16
FA8C: 102701f4   lbeq     $fc84
FA90: e6e4       ldb      , s
FA92: c480       andb     #-128
FA94: 102701ec   lbeq     $fc84
FA98: ccfce8     ldd      #-792
FA9B: 7efc55     jmp      $fc55
FA9E: 327e       leas     -2, s
FAA0: bdfb59     jsr      $fb59
FAA3: ede4       std      , s
FAA5: 83007f     subd     #127
FAA8: 2f06       ble      $fab0
FAAA: ece4       ldd      , s
FAAC: 8aff       ora      #-1
FAAE: ede4       std      , s
FAB0: ece4       ldd      , s
FAB2: f30170     addd     $0170
FAB5: ede4       std      , s
FAB7: 3406       pshs     a, b
FAB9: ec62       ldd      2, s
FABB: 1f89       tfr      a, b
FABD: 4f         clra     
FABE: 3406       pshs     a, b
FAC0: ccfcea     ldd      #-790
FAC3: 2025       bra      $faea
FAC5: 327e       leas     -2, s
FAC7: be0170     ldx      $0170
FACA: ec84       ldd      , x
FACC: ede4       std      , s
FACE: fc0170     ldd      $0170
FAD1: c30002     addd     #2
FAD4: fd0170     std      $0170
FAD7: ece4       ldd      , s
FAD9: f30170     addd     $0170
FADC: ede4       std      , s
FADE: 3406       pshs     a, b
FAE0: ec62       ldd      2, s
FAE2: 1f89       tfr      a, b
FAE4: 4f         clra     
FAE5: 3406       pshs     a, b
FAE7: ccfcf2     ldd      #-782
FAEA: 7efc07     jmp      $fc07
FAED: ccfcd1     ldd      #-815
FAF0: 2003       bra      $faf5
FAF2: ccfccf     ldd      #-817
FAF5: 7eb0b7     jmp      $b0b7
FAF8: 3406       pshs     a, b
FAFA: 327f       leas     -1, s
FAFC: 4f         clra     
FAFD: e662       ldb      2, s
FAFF: c40f       andb     #15
FB01: e7e4       stb      , s
FB03: c106       cmpb     #6
FB05: 2704       beq      $fb0b
FB07: c107       cmpb     #7
FB09: 2604       bne      $fb0f
FB0B: 8db8       bsr      $fac5
FB0D: 203a       bra      $fb49
FB0F: c10a       cmpb     #10
FB11: 2704       beq      $fb17
FB13: c10c       cmpb     #12
FB15: 2606       bne      $fb1d
FB17: 5f         clrb     
FB18: bdf945     jsr      $f945
FB1B: 202c       bra      $fb49
FB1D: c10e       cmpb     #14
FB1F: 2720       beq      $fb41
FB21: c10f       cmpb     #15
FB23: 201a       bra      $fb3f
FB25: 3406       pshs     a, b
FB27: 327f       leas     -1, s
FB29: 4f         clra     
FB2A: e662       ldb      2, s
FB2C: c40f       andb     #15
FB2E: e7e4       stb      , s
FB30: c103       cmpb     #3
FB32: 2205       bhi      $fb39
FB34: bdf98d     jsr      $f98d
FB37: 2010       bra      $fb49
FB39: c107       cmpb     #7
FB3B: 2304       bls      $fb41
FB3D: c10c       cmpb     #12
FB3F: 2608       bne      $fb49
FB41: ccfcc2     ldd      #-830
FB44: bdb0b7     jsr      $b0b7
FB47: 8d03       bsr      $fb4c
FB49: 3263       leas     3, s
FB4B: 39         rts      
FB4C: 8d0b       bsr      $fb59
FB4E: 3406       pshs     a, b
FB50: ccfcf9     ldd      #-775
FB53: bdb0b7     jsr      $b0b7
FB56: 3262       leas     2, s
FB58: 39         rts      
FB59: fc0190     ldd      $0190
FB5C: c3ffff     addd     #-1
FB5F: fd0190     std      $0190
FB62: fc0170     ldd      $0170
FB65: 3406       pshs     a, b
FB67: c30001     addd     #1
FB6A: fd0170     std      $0170
FB6D: 3510       puls     x
FB6F: 4f         clra     
FB70: e684       ldb      , x
FB72: 39         rts      
FB73: 327a       leas     -6, s
FB75: 8de2       bsr      $fb59
FB77: e7e4       stb      , s
FB79: c121       cmpb     #33
FB7B: 2521       bcs      $fb9e
FB7D: c12f       cmpb     #47
FB7F: 221d       bhi      $fb9e
FB81: ccfcfc     ldd      #-772
FB84: bdb0b7     jsr      $b0b7
FB87: ccf718     ldd      #-2280
FB8A: 3406       pshs     a, b
FB8C: 4f         clra     
FB8D: e662       ldb      2, s
FB8F: bdfc5a     jsr      $fc5a
FB92: 3262       leas     2, s
FB94: ed62       std      2, s
FB96: bdfc87     jsr      $fc87
FB99: bdfac5     jsr      $fac5
FB9C: 206c       bra      $fc0a
FB9E: 4f         clra     
FB9F: c40f       andb     #15
FBA1: e761       stb      1, s
FBA3: c103       cmpb     #3
FBA5: 2605       bne      $fbac
FBA7: ccfcfe     ldd      #-770
FBAA: 202f       bra      $fbdb
FBAC: c10c       cmpb     #12
FBAE: 2605       bne      $fbb5
FBB0: ccfd03     ldd      #-765
FBB3: 2026       bra      $fbdb
FBB5: c10e       cmpb     #14
FBB7: 2610       bne      $fbc9
FBB9: e6e4       ldb      , s
FBBB: c440       andb     #64
FBBD: 2605       bne      $fbc4
FBBF: ccfd08     ldd      #-760
FBC2: 2017       bra      $fbdb
FBC4: ccfd0d     ldd      #-755
FBC7: 2012       bra      $fbdb
FBC9: c10f       cmpb     #15
FBCB: 2637       bne      $fc04
FBCD: e6e4       ldb      , s
FBCF: c440       andb     #64
FBD1: 2605       bne      $fbd8
FBD3: ccfd12     ldd      #-750
FBD6: 2003       bra      $fbdb
FBD8: ccfd17     ldd      #-745
FBDB: bdb0b7     jsr      $b0b7
FBDE: 4f         clra     
FBDF: e6e4       ldb      , s
FBE1: c4f0       andb     #-16
FBE3: 44         lsra     
FBE4: 56         rorb     
FBE5: 44         lsra     
FBE6: 56         rorb     
FBE7: 44         lsra     
FBE8: 56         rorb     
FBE9: ed64       std      4, s
FBEB: 4f         clra     
FBEC: e6e4       ldb      , s
FBEE: 3406       pshs     a, b
FBF0: ec66       ldd      6, s
FBF2: c30001     addd     #1
FBF5: 58         lslb     
FBF6: 49         rola     
FBF7: c3f825     addd     #-2011
FBFA: 3406       pshs     a, b
FBFC: 3510       puls     x
FBFE: 3506       puls     a, b
FC00: ad94       jsr      [, x]
FC02: 2006       bra      $fc0a
FC04: ccfcb6     ldd      #-842
FC07: bdb0b7     jsr      $b0b7
FC0A: 3266       leas     6, s
FC0C: 39         rts      
FC0D: 327c       leas     -4, s
FC0F: bdfb59     jsr      $fb59
FC12: e7e4       stb      , s
FC14: 4f         clra     
FC15: c40f       andb     #15
FC17: e761       stb      1, s
FC19: c103       cmpb     #3
FC1B: 2605       bne      $fc22
FC1D: ccfd1c     ldd      #-740
FC20: 2007       bra      $fc29
FC22: c10c       cmpb     #12
FC24: 262c       bne      $fc52
FC26: ccfd21     ldd      #-735
FC29: bdb0b7     jsr      $b0b7
FC2C: 4f         clra     
FC2D: e6e4       ldb      , s
FC2F: c4f0       andb     #-16
FC31: 44         lsra     
FC32: 56         rorb     
FC33: 44         lsra     
FC34: 56         rorb     
FC35: 44         lsra     
FC36: 56         rorb     
FC37: ed62       std      2, s
FC39: 4f         clra     
FC3A: e6e4       ldb      , s
FC3C: 3406       pshs     a, b
FC3E: ec64       ldd      4, s
FC40: c30001     addd     #1
FC43: 58         lslb     
FC44: 49         rola     
FC45: c3f825     addd     #-2011
FC48: 3406       pshs     a, b
FC4A: 3510       puls     x
FC4C: 3506       puls     a, b
FC4E: ad94       jsr      [, x]
FC50: 2032       bra      $fc84
FC52: ccfcb6     ldd      #-842
FC55: bdb0b7     jsr      $b0b7
FC58: 202a       bra      $fc84
FC5A: 3406       pshs     a, b
FC5C: 327e       leas     -2, s
FC5E: 4f         clra     
FC5F: e663       ldb      3, s
FC61: c40f       andb     #15
FC63: ede4       std      , s
FC65: ece4       ldd      , s
FC67: 2719       beq      $fc82
FC69: ec66       ldd      6, s
FC6B: 3406       pshs     a, b
FC6D: c30001     addd     #1
FC70: ed68       std      8, s
FC72: 3510       puls     x
FC74: 4f         clra     
FC75: e684       ldb      , x
FC77: c480       andb     #-128
FC79: 27ee       beq      $fc69
FC7B: ece4       ldd      , s
FC7D: c3ffff     addd     #-1
FC80: 20e1       bra      $fc63
FC82: ec66       ldd      6, s
FC84: 3264       leas     4, s
FC86: 39         rts      
FC87: 3406       pshs     a, b
FC89: 4f         clra     
FC8A: e6f4       ldb      [, s]
FC8C: c47f       andb     #127
FC8E: 3406       pshs     a, b
FC90: ccfce0     ldd      #-800
FC93: bdb0b7     jsr      $b0b7
FC96: 3262       leas     2, s
FC98: 4f         clra     
FC99: e6f4       ldb      [, s]
FC9B: c480       andb     #-128
FC9D: 2609       bne      $fca8
FC9F: ece4       ldd      , s
FCA1: c30001     addd     #1
FCA4: ede4       std      , s
FCA6: 20e1       bra      $fc89
FCA8: 3262       leas     2, s
FCAA: 39         rts      
FCAB: 2568       bcs      $fd15
FCAD: 2568       bcs      $fd17
FCAF: 2020       bra      $fcd1
FCB1: 0042       neg      $42
FCB3: 53         comb     
FCB4: 52         fcb      $52
FCB5: 003f       neg      $3f
FCB7: 3f         swi      
FCB8: 3f         swi      
FCB9: 3f         swi      
FCBA: 0025       neg      $25
FCBC: 6e00       jmp      0, x
FCBE: 2023       bra      $fce3
FCC0: 2400       bcc      $fcc2
FCC2: 2024       bra      $fce8
FCC4: 0020       neg      $20
FCC6: 0024       neg      $24
FCC8: 2568       bcs      $fd32
FCCA: 005b       neg      $5b
FCCC: 0024       neg      $24
FCCE: 0042       neg      $42
FCD0: 0041       neg      $41
FCD2: 0044       neg      $44
FCD4: 002c       neg      $2c
FCD6: 002d       neg      $2d
FCD8: 002d       neg      $2d
FCDA: 2d00       blt      $fcdc
FCDC: 50         negb     
FCDD: 43         coma     
FCDE: 52         fcb      $52
FCDF: 0025       neg      $25
FCE1: 6300       com      0, x
FCE3: 2b00       bmi      $fce5
FCE5: 2b2b       bmi      $fd12
FCE7: 005d       neg      $5d
FCE9: 0020       neg      $20
FCEB: 2024       bra      $fd11
FCED: 2568       bcs      $fd57
FCEF: 2568       bcs      $fd59
FCF1: 0020       neg      $20
FCF3: 2425       bcc      $fd1a
FCF5: 6825       lsl      5, y
FCF7: 6800       lsl      0, x
FCF9: 2568       bcs      $fd63
FCFB: 004c       neg      $4c
FCFD: 0043       neg      $43
FCFF: 4d         tsta     
FD00: 50         negb     
FD01: 44         lsra     
FD02: 0043       neg      $43
FD04: 4d         tsta     
FD05: 50         negb     
FD06: 59         rolb     
FD07: 004c       neg      $4c
FD09: 44         lsra     
FD0A: 59         rolb     
FD0B: 2000       bra      $fd0d
FD0D: 4c         inca     
FD0E: 44         lsra     
FD0F: 53         comb     
FD10: 2000       bra      $fd12
FD12: 53         comb     
FD13: 54         lsrb     
FD14: 59         rolb     
FD15: 2000       bra      $fd17
FD17: 53         comb     
FD18: 54         lsrb     
FD19: 53         comb     
FD1A: 2000       bra      $fd1c
FD1C: 43         coma     
FD1D: 4d         tsta     
FD1E: 50         negb     
FD1F: 55         fcb      $55
FD20: 0043       neg      $43
FD22: 4d         tsta     
FD23: 50         negb     
FD24: 53         comb     
FD25: 008d       neg      $8d
FD27: 3e         fcb      $3e
FD28: fd0170     std      $0170
FD2B: f60175     ldb      $0175
FD2E: c12e       cmpb     #46
FD30: 260d       bne      $fd3f
FD32: fc0170     ldd      $0170
FD35: 3406       pshs     a, b
FD37: 8d2d       bsr      $fd66
FD39: e3e4       addd     , s
FD3B: 3510       puls     x
FD3D: 200b       bra      $fd4a
FD3F: c12d       cmpb     #45
FD41: 2604       bne      $fd47
FD43: 8d21       bsr      $fd66
FD45: 2003       bra      $fd4a
FD47: fc0170     ldd      $0170
FD4A: fd0172     std      $0172
FD4D: f60175     ldb      $0175
FD50: c120       cmpb     #32
FD52: 2708       beq      $fd5c
FD54: c10d       cmpb     #13
FD56: 2704       beq      $fd5c
FD58: c12c       cmpb     #44
FD5A: 2604       bne      $fd60
FD5C: 7f0174     clr      $0174
FD5F: 39         rts      
FD60: c6ff       ldb      #-1
FD62: f70174     stb      $0174
FD65: 39         rts      
FD66: 327c       leas     -4, s
FD68: 4f         clra     
FD69: 5f         clrb     
FD6A: ed62       std      2, s
FD6C: bdb0c6     jsr      $b0c6
FD6F: f70175     stb      $0175
FD72: c120       cmpb     #32
FD74: 27f6       beq      $fd6c
FD76: 8d25       bsr      $fd9d
FD78: ede4       std      , s
FD7A: f60174     ldb      $0174
FD7D: 2616       bne      $fd95
FD7F: ec62       ldd      2, s
FD81: 58         lslb     
FD82: 49         rola     
FD83: 58         lslb     
FD84: 49         rola     
FD85: 58         lslb     
FD86: 49         rola     
FD87: 58         lslb     
FD88: 49         rola     
FD89: e3e4       addd     , s
FD8B: ed62       std      2, s
FD8D: bdb0c6     jsr      $b0c6
FD90: f70175     stb      $0175
FD93: 20e1       bra      $fd76
FD95: 7f0174     clr      $0174
FD98: ec62       ldd      2, s
FD9A: 3264       leas     4, s
FD9C: 39         rts      
FD9D: 327f       leas     -1, s
FD9F: 4f         clra     
FDA0: f60175     ldb      $0175
FDA3: e7e4       stb      , s
FDA5: bdb02a     jsr      $b02a
FDA8: 2705       beq      $fdaf
FDAA: 7f0174     clr      $0174
FDAD: 2005       bra      $fdb4
FDAF: c6ff       ldb      #-1
FDB1: f70174     stb      $0174
FDB4: 4f         clra     
FDB5: e6e4       ldb      , s
FDB7: bdb018     jsr      $b018
FDBA: 3261       leas     1, s
FDBC: 39         rts      
FDBD: bdb0c6     jsr      $b0c6
FDC0: 83000d     subd     #13
FDC3: 26f8       bne      $fdbd
FDC5: 39         rts      
FDC6: ccfdcc     ldd      #-564
FDC9: 7eb0b7     jmp      $b0b7
FDCC: 49         rola     
FDCD: 6e76       jmp      -10, s
FDCF: 61         fcb      $61
FDD0: 6c69       inc      9, s
FDD2: 6420       lsr      0, y
FDD4: 636f       com      15, s
FDD6: 6d6d       tst      13, s
FDD8: 61         fcb      $61
FDD9: 6e64       jmp      4, s
FDDB: 256e       bcs      $fe4b
FDDD: 0035       neg      $35
FDDF: 0635       ror      $35
FDE1: 02         fcb      $02
FDE2: b7018e     sta      $018e
FDE5: 3506       puls     a, b
FDE7: fd0184     std      $0184
FDEA: 3502       puls     a
FDEC: b7018f     sta      $018f
FDEF: 3570       puls     x, y, u
FDF1: bf0186     stx      $0186
FDF4: 10bf0188   sty      $0188
FDF8: ff018a     stu      $018a
FDFB: 3510       puls     x
FDFD: 301f       leax     -1, x
FDFF: 863f       lda      #63
FE01: a184       cmpa     , x
FE03: 2702       beq      $fe07
FE05: 3001       leax     1, x
FE07: bf0182     stx      $0182
FE0A: 10ff018c   sts      $018c
FE0E: 10ce021f   lds      #543
FE12: 1cef       andcc    #-17
FE14: 7ef682     jmp      $f682
FE17: 10fe018c   lds      $018c
FE1B: fc0182     ldd      $0182
FE1E: 3406       pshs     a, b
FE20: be0186     ldx      $0186
FE23: 10be0188   ldy      $0188
FE27: fe018a     ldu      $018a
FE2A: 3470       pshs     x, y, u
FE2C: b6018f     lda      $018f
FE2F: 3402       pshs     a
FE31: fc0184     ldd      $0184
FE34: 3406       pshs     a, b
FE36: b6018e     lda      $018e
FE39: 3402       pshs     a
FE3B: 3b         rti      
FE3C: 0062       neg      $62
FE3E: f13f00     cmpb     $3f00
FE41: 63f1       com      [, s++]
FE43: 5d         tstb     
FE44: 0064       neg      $64
FE46: f18400     cmpb     $8400
FE49: 66         fcb      $66
FE4A: f26400     sbcb     $6400
FE4D: 67         fcb      $67
FE4E: f2a200     sbcb     $a200
FE51: 6cf3       inc      [, --s]
FE53: 0300       com      $00
FE55: 6df4       tst      [, s]
FE57: 58         lslb     
FE58: 003b       neg      $3b
FE5A: f45800     andb     $5800
FE5D: 70b075     neg      $b075
FE60: 0072       neg      $72
FE62: f4b900     andb     $b900
FE65: 3a         abx      
FE66: f53e00     bitb     $3e00
FE69: 73f5da     com      $f5da
FE6C: 0074       neg      $74
FE6E: f87100     eorb     $7100
FE71: 0000       neg      $00
FE73: 0032       neg      $32
FE75: 79c6ff     rol      $c6ff
FE78: f7eff1     stb      $eff1
FE7B: 7fe811     clr      $e811
FE7E: 7fe813     clr      $e813
FE81: 7fe810     clr      $e810
FE84: 7fe812     clr      $e812
FE87: 4f         clra     
FE88: 5f         clrb     
FE89: ede4       std      , s
FE8B: ece4       ldd      , s
FE8D: 3406       pshs     a, b
FE8F: 1f41       tfr      s, x
FE91: c604       ldb      #4
FE93: 3a         abx      
FE94: 3410       pshs     x
FE96: 3506       puls     a, b
FE98: e3e4       addd     , s
FE9A: 3510       puls     x
FE9C: 3406       pshs     a, b
FE9E: 4f         clra     
FE9F: 5f         clrb     
FEA0: 3510       puls     x
FEA2: e784       stb      , x
FEA4: ece4       ldd      , s
FEA6: c30001     addd     #1
FEA9: ede4       std      , s
FEAB: 830004     subd     #4
FEAE: 2fdb       ble      $fe8b
FEB0: 1f41       tfr      s, x
FEB2: c602       ldb      #2
FEB4: 3a         abx      
FEB5: 3410       pshs     x
FEB7: 3506       puls     a, b
FEB9: bdb0f9     jsr      $b0f9
FEBC: ccfec5     ldd      #-315
FEBF: bdb0f3     jsr      $b0f3
FEC2: 3267       leas     7, s
FEC4: 39         rts      
FEC5: 0000       neg      $00
FEC7: ffffff     stu      $ffff
FECA: ffffff     stu      $ffff
FECD: ffffff     stu      $ffff
FED0: ffffff     stu      $ffff
FED3: ffffff     stu      $ffff
FED6: ffffff     stu      $ffff
FED9: ffffff     stu      $ffff
FEDC: ffffff     stu      $ffff
FEDF: ffffff     stu      $ffff
FEE2: ffffff     stu      $ffff
FEE5: ffffff     stu      $ffff
FEE8: ffffff     stu      $ffff
FEEB: ffffff     stu      $ffff
FEEE: ffffff     stu      $ffff
FEF1: ffffff     stu      $ffff
FEF4: ffffff     stu      $ffff
FEF7: ffffff     stu      $ffff
FEFA: ffffff     stu      $ffff
FEFD: ffffff     stu      $ffff
FF00: ffffff     stu      $ffff
FF03: ffffff     stu      $ffff
FF06: ffffff     stu      $ffff
FF09: ffffff     stu      $ffff
FF0C: ffffff     stu      $ffff
FF0F: ffffff     stu      $ffff
FF12: ffffff     stu      $ffff
FF15: ffffff     stu      $ffff
FF18: ffffff     stu      $ffff
FF1B: ffffff     stu      $ffff
FF1E: ffffff     stu      $ffff
FF21: ffffff     stu      $ffff
FF24: ffffff     stu      $ffff
FF27: ffffff     stu      $ffff
FF2A: ffffff     stu      $ffff
FF2D: ffffff     stu      $ffff
FF30: ffffff     stu      $ffff
FF33: ffffff     stu      $ffff
FF36: ffffff     stu      $ffff
FF39: ffffff     stu      $ffff
FF3C: ffffff     stu      $ffff
FF3F: ffffff     stu      $ffff
FF42: ffffff     stu      $ffff
FF45: ffffff     stu      $ffff
FF48: ffffff     stu      $ffff
FF4B: ffffff     stu      $ffff
FF4E: ffffff     stu      $ffff
FF51: ffffff     stu      $ffff
FF54: ffffff     stu      $ffff
FF57: ffffff     stu      $ffff
FF5A: ffffff     stu      $ffff
FF5D: ffffff     stu      $ffff
FF60: ffffff     stu      $ffff
FF63: ffffff     stu      $ffff
FF66: ffffff     stu      $ffff
FF69: ffffff     stu      $ffff
FF6C: ffffff     stu      $ffff
FF6F: ffffff     stu      $ffff
FF72: ffffff     stu      $ffff
FF75: ffffff     stu      $ffff
FF78: ffffff     stu      $ffff
FF7B: ffffff     stu      $ffff
FF7E: ffff8e     stu      $ff8e
FF81: ffb1c6     stu      $b1c6
FF84: 0710       asr      $10
FF86: 8e0100     ldx      #256
FF89: afa1       stx      , y++
FF8B: 5a         decb     
FF8C: 26fb       bne      $ff89
FF8E: 8effae     ldx      #-82
FF91: bf0108     stx      $0108
FF94: 7ef000     jmp      $f000
FF97: 1a10       orcc     #16
FF99: ae62       ldx      2, s
FF9B: ed890100   std      256, x
FF9F: 1cef       andcc    #-17
FFA1: 39         rts      
FFA2: 3506       puls     a, b
FFA4: 83ffb4     subd     #-76
FFA7: 8e0100     ldx      #256
FFAA: 3a         abx      
FFAB: ad94       jsr      [, x]
FFAD: 3b         rti      
FFAE: 7deff1     tst      $eff1
FFB1: 39         rts      
FFB2: 8dee       bsr      $ffa2
FFB4: 8dec       bsr      $ffa2
FFB6: 8dea       bsr      $ffa2
FFB8: 8de8       bsr      $ffa2
FFBA: 8de6       bsr      $ffa2
FFBC: 8de4       bsr      $ffa2
FFBE: 8de2       bsr      $ffa2
FFC0: 0000       neg      $00
FFC2: 0000       neg      $00
FFC4: 0000       neg      $00
FFC6: 0000       neg      $00
FFC8: 0000       neg      $00
FFCA: 0000       neg      $00
FFCC: 0000       neg      $00
FFCE: 0000       neg      $00
FFD0: 0000       neg      $00
FFD2: 0000       neg      $00
FFD4: 0000       neg      $00
FFD6: 0000       neg      $00
FFD8: 0000       neg      $00
FFDA: 0000       neg      $00
FFDC: 0000       neg      $00
FFDE: 0000       neg      $00
FFE0: 0000       neg      $00
FFE2: 0000       neg      $00
FFE4: 0000       neg      $00
FFE6: 0000       neg      $00
FFE8: 0000       neg      $00
FFEA: 0000       neg      $00
FFEC: 0000       neg      $00
FFEE: 0000       neg      $00
FFF0: ffb2ff     stu      $b2ff
FFF3: b4ffb6     anda     $ffb6
FFF6: ffb8ff     stu      $b8ff
FFF9: baffbc     ora      $ffbc
FFFC: ffbeff     stu      $beff
FFFF: 80         fcb      $80
