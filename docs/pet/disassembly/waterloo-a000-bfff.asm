; SuperPET Waterloo 6809 ROM $A000-$BFFF (970018-12) - disassembled from waterloo-a000-bfff.970018-12.bin
; Base address $A000, length 8192 ($2000) bytes
; Generated with capstone (CS_ARCH_M680X / CS_MODE_M680X_6809) - a real 6809
; disassembler, not hand-decoded. Labels/comments are NOT included here (raw
; mnemonic + operand listing only); see docs/pet/waterloo-investigation.md for
; the annotated, hand-verified routines this session actually traced through.

A000: 7ea65d     jmp      $a65d
A003: 7ea666     jmp      $a666
A006: 7ea6e2     jmp      $a6e2
A009: 7ea739     jmp      $a739
A00C: 7ea799     jmp      $a799
A00F: 7ea7d7     jmp      $a7d7
A012: 7ea7dd     jmp      $a7dd
A015: 7ea2bc     jmp      $a2bc
A018: 7ea2db     jmp      $a2db
A01B: 7ea2fa     jmp      $a2fa
A01E: 7ea314     jmp      $a314
A021: 7ea33f     jmp      $a33f
A024: 7ea3b0     jmp      $a3b0
A027: 7ea35d     jmp      $a35d
A02A: 7ea386     jmp      $a386
A02D: 7ea39a     jmp      $a39a
A030: 7ea43f     jmp      $a43f
A033: 7ea434     jmp      $a434
A036: 7ea430     jmp      $a430
A039: 7ea42b     jmp      $a42b
A03C: 7ea3e4     jmp      $a3e4
A03F: 7ea41a     jmp      $a41a
A042: 7ea4c8     jmp      $a4c8
A045: 7ea4c3     jmp      $a4c3
A048: 7ea55b     jmp      $a55b
A04B: 7ea54b     jmp      $a54b
A04E: 7ea5e6     jmp      $a5e6
A051: 7ea1b7     jmp      $a1b7
A054: 7ea06f     jmp      $a06f
A057: 7ea4b6     jmp      $a4b6
A05A: 7ea613     jmp      $a613
A05D: 7ea36b     jmp      $a36b
A060: 7ea555     jmp      $a555
A063: 7ea939     jmp      $a939
A066: 7ea439     jmp      $a439
A069: 7ea3e7     jmp      $a3e7
A06C: 7ea93e     jmp      $a93e
A06F: 1f01       tfr      d, x
A071: 8620       lda      #32
A073: 0d80       tst      $80
A075: 2a04       bpl      $a07b
A077: 862d       lda      #45
A079: 0f80       clr      $80
A07B: a784       sta      , x
A07D: 3001       leax     1, x
A07F: 9f8a       stx      $8a
A081: 9681       lda      $81
A083: 2608       bne      $a08d
A085: 8601       lda      #1
A087: 9788       sta      $88
A089: 0f89       clr      $89
A08B: 2002       bra      $a08f
A08D: 8d09       bsr      $a098
A08F: 8d57       bsr      $a0e8
A091: 9e8a       ldx      $8a
A093: 6f84       clr      , x
A095: dc8a       ldd      $8a
A097: 39         rts      
A098: 5f         clrb     
A099: 8180       cmpa     #-128
A09B: 2208       bhi      $a0a5
A09D: 8ea1b2     ldx      #-24142
A0A0: bda4c5     jsr      $a4c5
A0A3: c6f7       ldb      #-9
A0A5: d788       stb      $88
A0A7: 8ea1a6     ldx      #-24154
A0AA: bda3e7     jsr      $a3e7
A0AD: 271d       beq      $a0cc
A0AF: 2a11       bpl      $a0c2
A0B1: 8ea1ac     ldx      #-24148
A0B4: bda3e7     jsr      $a3e7
A0B7: 2702       beq      $a0bb
A0B9: 2a0e       bpl      $a0c9
A0BB: bda36b     jsr      $a36b
A0BE: 0a88       dec      $88
A0C0: 20ef       bra      $a0b1
A0C2: bda555     jsr      $a555
A0C5: 0c88       inc      $88
A0C7: 20de       bra      $a0a7
A0C9: bda439     jsr      $a439
A0CC: bda93e     jsr      $a93e
A0CF: c601       ldb      #1
A0D1: 9688       lda      $88
A0D3: 8b0d       adda     #13
A0D5: 2b0a       bmi      $a0e1
A0D7: 810e       cmpa     #14
A0D9: 2406       bcc      $a0e1
A0DB: 8004       suba     #4
A0DD: 1f89       tfr      a, b
A0DF: 8605       lda      #5
A0E1: 8005       suba     #5
A0E3: 9789       sta      $89
A0E5: d788       stb      $88
A0E7: 39         rts      
A0E8: 9688       lda      $88
A0EA: 4a         deca     
A0EB: 2a17       bpl      $a104
A0ED: 9e8a       ldx      $8a
A0EF: 862e       lda      #46
A0F1: a784       sta      , x
A0F3: 3001       leax     1, x
A0F5: 8630       lda      #48
A0F7: d688       ldb      $88
A0F9: 2707       beq      $a102
A0FB: a784       sta      , x
A0FD: 3001       leax     1, x
A0FF: 5c         incb     
A100: 20f7       bra      $a0f9
A102: 9f8a       stx      $8a
A104: 8ea182     ldx      #-24190
A107: a684       lda      , x
A109: 2b09       bmi      $a114
A10B: 860a       lda      #10
A10D: 4a         deca     
A10E: 8d37       bsr      $a147
A110: 24fb       bcc      $a10d
A112: 2007       bra      $a11b
A114: 86ff       lda      #-1
A116: 4c         inca     
A117: 8d2e       bsr      $a147
A119: 25fb       bcs      $a116
A11B: 8b30       adda     #48
A11D: 3001       leax     1, x
A11F: 3001       leax     1, x
A121: 3001       leax     1, x
A123: 3001       leax     1, x
A125: 3410       pshs     x
A127: 9e8a       ldx      $8a
A129: a784       sta      , x
A12B: 3001       leax     1, x
A12D: 0a88       dec      $88
A12F: 2606       bne      $a137
A131: 862e       lda      #46
A133: a784       sta      , x
A135: 3001       leax     1, x
A137: 9f8a       stx      $8a
A139: 3510       puls     x
A13B: 8ca1a6     cmpx     #-24154
A13E: 26c7       bne      $a107
A140: d689       ldb      $89
A142: 2702       beq      $a146
A144: 8d14       bsr      $a15a
A146: 39         rts      
A147: 3402       pshs     a
A149: dc84       ldd      $84
A14B: e302       addd     2, x
A14D: dd84       std      $84
A14F: dc82       ldd      $82
A151: e901       adcb     1, x
A153: a984       adca     , x
A155: dd82       std      $82
A157: 3502       puls     a
A159: 39         rts      
A15A: 8645       lda      #69
A15C: 9e8a       ldx      $8a
A15E: a784       sta      , x
A160: 3001       leax     1, x
A162: 862b       lda      #43
A164: 5d         tstb     
A165: 2a03       bpl      $a16a
A167: 862d       lda      #45
A169: 50         negb     
A16A: a784       sta      , x
A16C: 3001       leax     1, x
A16E: 862f       lda      #47
A170: 4c         inca     
A171: c00a       subb     #10
A173: 2afb       bpl      $a170
A175: a784       sta      , x
A177: 3001       leax     1, x
A179: cb3a       addb     #58
A17B: e784       stb      , x
A17D: 3001       leax     1, x
A17F: 9f8a       stx      $8a
A181: 39         rts      
A182: fa0a1f     orb      $0a1f
A185: 0000       neg      $00
A187: 9896       eora     $96
A189: 80ff       suba     #-1
A18B: f0bdc0     subb     $bdc0
A18E: 0001       neg      $01
A190: 86a0       lda      #-96
A192: ffffd8     stu      $ffd8
A195: f00000     subb     >$0000
A198: 03e8       com      $e8
A19A: ffffff     stu      $ffff
A19D: 9c00       cmpx     $00
A19F: 0000       neg      $00
A1A1: 0aff       dec      $ff
A1A3: ffffff     stu      $ffff
A1A6: 009e       neg      $9e
A1A8: ee6b       ldu      11, s
A1AA: 27fd       beq      $a1a9
A1AC: 009b       neg      $9b
A1AE: bebc1f     ldx      $bc1f
A1B1: fd4f6e     std      $4f6e
A1B4: 6b         fcb      $6b
A1B5: 2800       bvc      $a1b7
A1B7: dd8a       std      $8a
A1B9: 1f41       tfr      s, x
A1BB: ec02       ldd      2, x
A1BD: dd8e       std      $8e
A1BF: bda3b0     jsr      $a3b0
A1C2: 0f88       clr      $88
A1C4: 0f89       clr      $89
A1C6: bda28e     jsr      $a28e
A1C9: bda2a7     jsr      $a2a7
A1CC: 8d67       bsr      $a235
A1CE: 1f89       tfr      a, b
A1D0: bda27d     jsr      $a27d
A1D3: 2421       bcc      $a1f6
A1D5: 5d         tstb     
A1D6: 2a02       bpl      $a1da
A1D8: 0c88       inc      $88
A1DA: 3404       pshs     b
A1DC: 8030       suba     #48
A1DE: 3402       pshs     a
A1E0: bda36b     jsr      $a36b
A1E3: bda386     jsr      $a386
A1E6: 4f         clra     
A1E7: 3504       puls     b
A1E9: 5d         tstb     
A1EA: 2706       beq      $a1f2
A1EC: bda39a     jsr      $a39a
A1EF: bda43f     jsr      $a43f
A1F2: 3504       puls     b
A1F4: 2017       bra      $a20d
A1F6: 812e       cmpa     #46
A1F8: 2606       bne      $a200
A1FA: c880       eorb     #-128
A1FC: 2a11       bpl      $a20f
A1FE: 200d       bra      $a20d
A200: bda2a7     jsr      $a2a7
A203: 8a20       ora      #32
A205: 8165       cmpa     #101
A207: 2606       bne      $a20f
A209: 8d3a       bsr      $a245
A20B: 2002       bra      $a20f
A20D: 20c1       bra      $a1d0
A20F: 3404       pshs     b
A211: 9689       lda      $89
A213: 9088       suba     $88
A215: 9789       sta      $89
A217: 2a07       bpl      $a220
A219: bda555     jsr      $a555
A21C: 0c89       inc      $89
A21E: 26f9       bne      $a219
A220: 2707       beq      $a229
A222: bda36b     jsr      $a36b
A225: 0a89       dec      $89
A227: 26f9       bne      $a222
A229: 3504       puls     b
A22B: c501       bitb     #1
A22D: 2703       beq      $a232
A22F: bda5e6     jsr      $a5e6
A232: dc8c       ldd      $8c
A234: 39         rts      
A235: 812d       cmpa     #45
A237: 2604       bne      $a23d
A239: 8601       lda      #1
A23B: 2007       bra      $a244
A23D: 802b       suba     #43
A23F: 2703       beq      $a244
A241: 8d5d       bsr      $a2a0
A243: 4f         clra     
A244: 39         rts      
A245: 9e8c       ldx      $8c
A247: 3410       pshs     x
A249: 8d32       bsr      $a27d
A24B: 8de8       bsr      $a235
A24D: 2702       beq      $a251
A24F: ca02       orb      #2
A251: 3404       pshs     b
A253: 8d28       bsr      $a27d
A255: 2415       bcc      $a26c
A257: 8030       suba     #48
A259: d689       ldb      $89
A25B: 58         lslb     
A25C: 58         lslb     
A25D: db89       addb     $89
A25F: 58         lslb     
A260: d789       stb      $89
A262: 9b89       adda     $89
A264: 9789       sta      $89
A266: 8d15       bsr      $a27d
A268: 25ed       bcs      $a257
A26A: 1a01       orcc     #1
A26C: 3504       puls     b
A26E: 3510       puls     x
A270: 2504       bcs      $a276
A272: 9f8c       stx      $8c
A274: 2006       bra      $a27c
A276: c502       bitb     #2
A278: 2702       beq      $a27c
A27A: 0089       neg      $89
A27C: 39         rts      
A27D: 9e8a       ldx      $8a
A27F: 3001       leax     1, x
A281: 9f8c       stx      $8c
A283: 9e8a       ldx      $8a
A285: 4f         clra     
A286: 9c8e       cmpx     $8e
A288: 2715       beq      $a29f
A28A: 3001       leax     1, x
A28C: 9f8a       stx      $8a
A28E: 9e8a       ldx      $8a
A290: 4f         clra     
A291: 9c8e       cmpx     $8e
A293: 270a       beq      $a29f
A295: a684       lda      , x
A297: 813a       cmpa     #58
A299: 2404       bcc      $a29f
A29B: 8030       suba     #48
A29D: 80d0       suba     #-48
A29F: 39         rts      
A2A0: 9e8a       ldx      $8a
A2A2: 301f       leax     -1, x
A2A4: 9f8a       stx      $8a
A2A6: 39         rts      
A2A7: 8120       cmpa     #32
A2A9: 2604       bne      $a2af
A2AB: 8dd6       bsr      $a283
A2AD: 20f8       bra      $a2a7
A2AF: 39         rts      
A2B0: 7fffff     clr      $ffff
A2B3: ffff86     stu      $ff86
A2B6: 02         fcb      $02
A2B7: 9787       sta      $87
A2B9: cca2b0     ldd      #-23888
A2BC: 1f01       tfr      d, x
A2BE: ec03       ldd      3, x
A2C0: dd84       std      $84
A2C2: ec01       ldd      1, x
A2C4: dd82       std      $82
A2C6: e684       ldb      , x
A2C8: 4f         clra     
A2C9: 9786       sta      $86
A2CB: 0882       lsl      $82
A2CD: 59         rolb     
A2CE: 2401       bcc      $a2d1
A2D0: 43         coma     
A2D1: dd80       std      $80
A2D3: 5d         tstb     
A2D4: 2702       beq      $a2d8
A2D6: 1a01       orcc     #1
A2D8: 0682       ror      $82
A2DA: 39         rts      
A2DB: 1f01       tfr      d, x
A2DD: ec03       ldd      3, x
A2DF: dd94       std      $94
A2E1: ec01       ldd      1, x
A2E3: dd92       std      $92
A2E5: e684       ldb      , x
A2E7: 4f         clra     
A2E8: 9796       sta      $96
A2EA: 0892       lsl      $92
A2EC: 59         rolb     
A2ED: 2401       bcc      $a2f0
A2EF: 43         coma     
A2F0: dd90       std      $90
A2F2: 5d         tstb     
A2F3: 2702       beq      $a2f7
A2F5: 1a01       orcc     #1
A2F7: 0692       ror      $92
A2F9: 39         rts      
A2FA: 1f01       tfr      d, x
A2FC: bda4a8     jsr      $a4a8
A2FF: dc84       ldd      $84
A301: ed03       std      3, x
A303: dc82       ldd      $82
A305: ed01       std      1, x
A307: 9681       lda      $81
A309: a784       sta      , x
A30B: 6801       lsl      1, x
A30D: 0780       asr      $80
A30F: 6684       ror      , x
A311: 6601       ror      1, x
A313: 39         rts      
A314: 9681       lda      $81
A316: 8181       cmpa     #-127
A318: 2404       bcc      $a31e
A31A: 4f         clra     
A31B: 5f         clrb     
A31C: 2020       bra      $a33e
A31E: 818f       cmpa     #-113
A320: 2308       bls      $a32a
A322: 8602       lda      #2
A324: 9787       sta      $87
A326: dc82       ldd      $82
A328: 2014       bra      $a33e
A32A: 0482       lsr      $82
A32C: 0683       ror      $83
A32E: 4c         inca     
A32F: 8190       cmpa     #-112
A331: 26f7       bne      $a32a
A333: dc82       ldd      $82
A335: 0d80       tst      $80
A337: 2a05       bpl      $a33e
A339: 43         coma     
A33A: 53         comb     
A33B: c30001     addd     #1
A33E: 39         rts      
A33F: 9681       lda      $81
A341: 81a0       cmpa     #-96
A343: 2504       bcs      $a349
A345: 8d69       bsr      $a3b0
A347: 2010       bra      $a359
A349: 9681       lda      $81
A34B: 8180       cmpa     #-128
A34D: 2307       bls      $a356
A34F: bda4b6     jsr      $a4b6
A352: 0a81       dec      $81
A354: 20f3       bra      $a349
A356: bda475     jsr      $a475
A359: 4f         clra     
A35A: d681       ldb      $81
A35C: 39         rts      
A35D: 4f         clra     
A35E: d681       ldb      $81
A360: 2705       beq      $a367
A362: c081       subb     #-127
A364: 2401       bcc      $a367
A366: 4a         deca     
A367: c30000     addd     #0
A36A: 39         rts      
A36B: 8d19       bsr      $a386
A36D: 2602       bne      $a371
A36F: 2014       bra      $a385
A371: 9681       lda      $81
A373: 8b02       adda     #2
A375: 250b       bcs      $a382
A377: 9781       sta      $81
A379: bda43f     jsr      $a43f
A37C: 0c81       inc      $81
A37E: 2702       beq      $a382
A380: 2003       bra      $a385
A382: 7ea2b5     jmp      $a2b5
A385: 39         rts      
A386: bda4a8     jsr      $a4a8
A389: dc85       ldd      $85
A38B: dd95       std      $95
A38D: dc83       ldd      $83
A38F: dd93       std      $93
A391: 9680       lda      $80
A393: 9790       sta      $90
A395: dc81       ldd      $81
A397: dd91       std      $91
A399: 39         rts      
A39A: 8d14       bsr      $a3b0
A39C: dd82       std      $82
A39E: 270b       beq      $a3ab
A3A0: 2a02       bpl      $a3a4
A3A2: 8d18       bsr      $a3bc
A3A4: 8690       lda      #-112
A3A6: 9781       sta      $81
A3A8: bda475     jsr      $a475
A3AB: 39         rts      
A3AC: 8601       lda      #1
A3AE: 9787       sta      $87
A3B0: 8e0000     ldx      #0
A3B3: 9f80       stx      $80
A3B5: 9f82       stx      $82
A3B7: 9f84       stx      $84
A3B9: 9f85       stx      $85
A3BB: 39         rts      
A3BC: 0380       com      $80
A3BE: 0382       com      $82
A3C0: 0383       com      $83
A3C2: 0384       com      $84
A3C4: 0385       com      $85
A3C6: 0086       neg      $86
A3C8: 2602       bne      $a3cc
A3CA: 8d09       bsr      $a3d5
A3CC: 39         rts      
A3CD: 8d06       bsr      $a3d5
A3CF: 2603       bne      $a3d4
A3D1: bda62c     jsr      $a62c
A3D4: 39         rts      
A3D5: 0c85       inc      $85
A3D7: 260a       bne      $a3e3
A3D9: 0c84       inc      $84
A3DB: 2606       bne      $a3e3
A3DD: 0c83       inc      $83
A3DF: 2602       bne      $a3e3
A3E1: 0c82       inc      $82
A3E3: 39         rts      
A3E4: 8e0090     ldx      #144
A3E7: 9680       lda      $80
A3E9: a184       cmpa     , x
A3EB: 270b       beq      $a3f8
A3ED: 4d         tsta     
A3EE: 2604       bne      $a3f4
A3F0: c601       ldb      #1
A3F2: 2002       bra      $a3f6
A3F4: c6ff       ldb      #-1
A3F6: 2021       bra      $a419
A3F8: 9681       lda      $81
A3FA: a101       cmpa     1, x
A3FC: 260e       bne      $a40c
A3FE: dc82       ldd      $82
A400: a302       subd     2, x
A402: 2608       bne      $a40c
A404: dc84       ldd      $84
A406: a304       subd     4, x
A408: 2602       bne      $a40c
A40A: 200d       bra      $a419
A40C: 9680       lda      $80
A40E: 2401       bcc      $a411
A410: 43         coma     
A411: 2604       bne      $a417
A413: c601       ldb      #1
A415: 2002       bra      $a419
A417: c6ff       ldb      #-1
A419: 39         rts      
A41A: d680       ldb      $80
A41C: 9681       lda      $81
A41E: 2705       beq      $a425
A420: 9680       lda      $80
A422: 2601       bne      $a425
A424: 5c         incb     
A425: 39         rts      
A426: 40         nega     
A427: 0000       neg      $00
A429: 0000       neg      $00
A42B: 1f01       tfr      d, x
A42D: bda2dd     jsr      $a2dd
A430: 0390       com      $90
A432: 200b       bra      $a43f
A434: bda2db     jsr      $a2db
A437: 2006       bra      $a43f
A439: 8ea426     ldx      #-23514
A43C: bda2dd     jsr      $a2dd
A43F: 9691       lda      $91
A441: 2732       beq      $a475
A443: d681       ldb      $81
A445: d091       subb     $91
A447: 2716       beq      $a45f
A449: 2305       bls      $a450
A44B: 8e0090     ldx      #144
A44E: 2006       bra      $a456
A450: 50         negb     
A451: 9781       sta      $81
A453: 8e0080     ldx      #128
A456: c128       cmpb     #40
A458: 2302       bls      $a45c
A45A: c628       ldb      #40
A45C: bda5ed     jsr      $a5ed
A45F: 9680       lda      $80
A461: 9190       cmpa     $90
A463: 2608       bne      $a46d
A465: bda613     jsr      $a613
A468: bda62e     jsr      $a62e
A46B: 2008       bra      $a475
A46D: bda644     jsr      $a644
A470: 2403       bcc      $a475
A472: bda3bc     jsr      $a3bc
A475: 4f         clra     
A476: d682       ldb      $82
A478: 2b21       bmi      $a49b
A47A: 261a       bne      $a496
A47C: 3402       pshs     a
A47E: dc83       ldd      $83
A480: dd82       std      $82
A482: dc85       ldd      $85
A484: dd84       std      $84
A486: 0f86       clr      $86
A488: 3502       puls     a
A48A: 8b08       adda     #8
A48C: 8120       cmpa     #32
A48E: 2604       bne      $a494
A490: 9681       lda      $81
A492: 2007       bra      $a49b
A494: 2003       bra      $a499
A496: 8d1e       bsr      $a4b6
A498: 4c         inca     
A499: 20db       bra      $a476
A49B: 9081       suba     $81
A49D: 2505       bcs      $a4a4
A49F: bda3b0     jsr      $a3b0
A4A2: 2003       bra      $a4a7
A4A4: 40         nega     
A4A5: 9781       sta      $81
A4A7: 39         rts      
A4A8: 0d81       tst      $81
A4AA: 2707       beq      $a4b3
A4AC: 0886       lsl      $86
A4AE: 2403       bcc      $a4b3
A4B0: bda3cd     jsr      $a3cd
A4B3: 0f86       clr      $86
A4B5: 39         rts      
A4B6: 1cfe       andcc    #-2
A4B8: 0986       rol      $86
A4BA: 0985       rol      $85
A4BC: 0984       rol      $84
A4BE: 0983       rol      $83
A4C0: 0982       rol      $82
A4C2: 39         rts      
A4C3: 1f01       tfr      d, x
A4C5: bda2dd     jsr      $a2dd
A4C8: 9691       lda      $91
A4CA: 2603       bne      $a4cf
A4CC: 7ea3b0     jmp      $a3b0
A4CF: 8d4a       bsr      $a51b
A4D1: c605       ldb      #5
A4D3: 8e0082     ldx      #130
A4D6: a684       lda      , x
A4D8: 3402       pshs     a
A4DA: 6f84       clr      , x
A4DC: 3001       leax     1, x
A4DE: 5a         decb     
A4DF: 26f5       bne      $a4d6
A4E1: 8e0080     ldx      #128
A4E4: 3504       puls     b
A4E6: 8d13       bsr      $a4fb
A4E8: 3504       puls     b
A4EA: 8d0f       bsr      $a4fb
A4EC: 3504       puls     b
A4EE: 8d0b       bsr      $a4fb
A4F0: 3504       puls     b
A4F2: 8d07       bsr      $a4fb
A4F4: 3504       puls     b
A4F6: 8d03       bsr      $a4fb
A4F8: 7ea475     jmp      $a475
A4FB: 5d         tstb     
A4FC: 2607       bne      $a505
A4FE: c608       ldb      #8
A500: bda5ed     jsr      $a5ed
A503: 2015       bra      $a51a
A505: 54         lsrb     
A506: ca80       orb      #-128
A508: 2403       bcc      $a50d
A50A: bda613     jsr      $a613
A50D: 6602       ror      2, x
A50F: 6603       ror      3, x
A511: 6604       ror      4, x
A513: 6605       ror      5, x
A515: 6606       ror      6, x
A517: 54         lsrb     
A518: 26ee       bne      $a508
A51A: 39         rts      
A51B: 9681       lda      $81
A51D: 2729       beq      $a548
A51F: d690       ldb      $90
A521: 2702       beq      $a525
A523: 0380       com      $80
A525: d680       ldb      $80
A527: d790       stb      $90
A529: 9b91       adda     $91
A52B: 240e       bcc      $a53b
A52D: 2a0a       bpl      $a539
A52F: 3510       puls     x
A531: bda2b5     jsr      $a2b5
A534: d690       ldb      $90
A536: d780       stb      $80
A538: 39         rts      
A539: 2007       bra      $a542
A53B: 2b05       bmi      $a542
A53D: 3510       puls     x
A53F: 7ea3ac     jmp      $a3ac
A542: 8b80       adda     #-128
A544: 9781       sta      $81
A546: 2002       bra      $a54a
A548: 3510       puls     x
A54A: 39         rts      
A54B: bda2db     jsr      $a2db
A54E: 200b       bra      $a55b
A550: 42         fcb      $42
A551: 2000       bra      $a553
A553: 0000       neg      $00
A555: 8ea550     ldx      #-23216
A558: bda2dd     jsr      $a2dd
A55B: 9691       lda      $91
A55D: 2610       bne      $a56f
A55F: 8603       lda      #3
A561: 9787       sta      $87
A563: 9680       lda      $80
A565: 3402       pshs     a
A567: bda2b9     jsr      $a2b9
A56A: 3502       puls     a
A56C: 9780       sta      $80
A56E: 39         rts      
A56F: bda4a8     jsr      $a4a8
A572: 0091       neg      $91
A574: 8da5       bsr      $a51b
A576: 0c81       inc      $81
A578: 2607       bne      $a581
A57A: bda2b5     jsr      $a2b5
A57D: d690       ldb      $90
A57F: d780       stb      $80
A581: c601       ldb      #1
A583: 8e0004     ldx      #4
A586: 8d3d       bsr      $a5c5
A588: 1fa8       tfr      cc, a
A58A: 59         rolb     
A58B: 2412       bcc      $a59f
A58D: 3404       pshs     b
A58F: 301f       leax     -1, x
A591: 2604       bne      $a597
A593: c640       ldb      #64
A595: 2008       bra      $a59f
A597: 3001       leax     1, x
A599: 2714       beq      $a5af
A59B: 301f       leax     -1, x
A59D: c601       ldb      #1
A59F: 1f8a       tfr      a, cc
A5A1: 2403       bcc      $a5a6
A5A3: bda644     jsr      $a644
A5A6: bda4b6     jsr      $a4b6
A5A9: 25dd       bcs      $a588
A5AB: 2adb       bpl      $a588
A5AD: 20d7       bra      $a586
A5AF: 3504       puls     b
A5B1: 54         lsrb     
A5B2: 56         rorb     
A5B3: 56         rorb     
A5B4: 3502       puls     a
A5B6: dd85       std      $85
A5B8: 3504       puls     b
A5BA: 3502       puls     a
A5BC: dd83       std      $83
A5BE: 3502       puls     a
A5C0: 9782       sta      $82
A5C2: 7ea475     jmp      $a475
A5C5: 9692       lda      $92
A5C7: 9182       cmpa     $82
A5C9: 261a       bne      $a5e5
A5CB: 9693       lda      $93
A5CD: 9183       cmpa     $83
A5CF: 2614       bne      $a5e5
A5D1: 9694       lda      $94
A5D3: 9184       cmpa     $84
A5D5: 260e       bne      $a5e5
A5D7: 9695       lda      $95
A5D9: 9185       cmpa     $85
A5DB: 2608       bne      $a5e5
A5DD: 9696       lda      $96
A5DF: 9186       cmpa     $86
A5E1: 2602       bne      $a5e5
A5E3: 1a01       orcc     #1
A5E5: 39         rts      
A5E6: 9681       lda      $81
A5E8: 2702       beq      $a5ec
A5EA: 0380       com      $80
A5EC: 39         rts      
A5ED: c008       subb     #8
A5EF: 2b10       bmi      $a601
A5F1: 3404       pshs     b
A5F3: ec04       ldd      4, x
A5F5: ed05       std      5, x
A5F7: ec02       ldd      2, x
A5F9: ed03       std      3, x
A5FB: 6f02       clr      2, x
A5FD: 3504       puls     b
A5FF: 20ec       bra      $a5ed
A601: cb08       addb     #8
A603: 5a         decb     
A604: 2b0c       bmi      $a612
A606: 6402       lsr      2, x
A608: 6603       ror      3, x
A60A: 6604       ror      4, x
A60C: 6605       ror      5, x
A60E: 6606       ror      6, x
A610: 20f1       bra      $a603
A612: 39         rts      
A613: 3404       pshs     b
A615: dc85       ldd      $85
A617: d395       addd     $95
A619: dd85       std      $85
A61B: dc83       ldd      $83
A61D: d994       adcb     $94
A61F: 9993       adca     $93
A621: dd83       std      $83
A623: 9682       lda      $82
A625: 9992       adca     $92
A627: 9782       sta      $82
A629: 3504       puls     b
A62B: 39         rts      
A62C: 1a01       orcc     #1
A62E: 2413       bcc      $a643
A630: 0c81       inc      $81
A632: 2605       bne      $a639
A634: bda2b5     jsr      $a2b5
A637: 200a       bra      $a643
A639: 0682       ror      $82
A63B: 0683       ror      $83
A63D: 0684       ror      $84
A63F: 0685       ror      $85
A641: 0686       ror      $86
A643: 39         rts      
A644: 3404       pshs     b
A646: dc85       ldd      $85
A648: 9395       subd     $95
A64A: dd85       std      $85
A64C: dc83       ldd      $83
A64E: d294       sbcb     $94
A650: 9293       sbca     $93
A652: dd83       std      $83
A654: 9682       lda      $82
A656: 9292       sbca     $92
A658: 9782       sta      $82
A65A: 3504       puls     b
A65C: 39         rts      
A65D: bda386     jsr      $a386
A660: 8ea881     ldx      #-22399
A663: bda2be     jsr      $a2be
A666: 9681       lda      $81
A668: 2778       beq      $a6e2
A66A: 3274       leas     -12, s
A66C: 9691       lda      $91
A66E: 260e       bne      $a67e
A670: 9680       lda      $80
A672: 2a05       bpl      $a679
A674: bda2b5     jsr      $a2b5
A677: 2003       bra      $a67c
A679: bda3b0     jsr      $a3b0
A67C: 205c       bra      $a6da
A67E: 1f41       tfr      s, x
A680: 6f05       clr      5, x
A682: bda2fc     jsr      $a2fc
A685: 9690       lda      $90
A687: 273f       beq      $a6c8
A689: 1f41       tfr      s, x
A68B: dc90       ldd      $90
A68D: ed06       std      6, x
A68F: dc92       ldd      $92
A691: ed08       std      8, x
A693: dc94       ldd      $94
A695: ed0a       std      10, x
A697: bda386     jsr      $a386
A69A: bda93e     jsr      $a93e
A69D: 0f86       clr      $86
A69F: 9685       lda      $85
A6A1: 8401       anda     #1
A6A3: 2704       beq      $a6a9
A6A5: 1f41       tfr      s, x
A6A7: 6305       com      5, x
A6A9: bda475     jsr      $a475
A6AC: bda3e4     jsr      $a3e4
A6AF: 260f       bne      $a6c0
A6B1: 1f41       tfr      s, x
A6B3: ec06       ldd      6, x
A6B5: dd90       std      $90
A6B7: ec08       ldd      8, x
A6B9: dd92       std      $92
A6BB: ec0a       ldd      10, x
A6BD: dd94       std      $94
A6BF: 4f         clra     
A6C0: 2706       beq      $a6c8
A6C2: 8604       lda      #4
A6C4: 9787       sta      $87
A6C6: 2012       bra      $a6da
A6C8: 0f90       clr      $90
A6CA: bda74b     jsr      $a74b
A6CD: 1f41       tfr      s, x
A6CF: bda4c5     jsr      $a4c5
A6D2: 8d0e       bsr      $a6e2
A6D4: 1f41       tfr      s, x
A6D6: a605       lda      5, x
A6D8: 9780       sta      $80
A6DA: 1f41       tfr      s, x
A6DC: c60c       ldb      #12
A6DE: 3a         abx      
A6DF: 1f14       tfr      x, s
A6E1: 39         rts      
A6E2: 8ea89a     ldx      #-22374
A6E5: bda4c5     jsr      $a4c5
A6E8: 9686       lda      $86
A6EA: 8b50       adda     #80
A6EC: 2403       bcc      $a6f1
A6EE: bda3cd     jsr      $a3cd
A6F1: 3402       pshs     a
A6F3: bda389     jsr      $a389
A6F6: 3502       puls     a
A6F8: 9796       sta      $96
A6FA: 9681       lda      $81
A6FC: 8188       cmpa     #-120
A6FE: 242c       bcc      $a72c
A700: bda939     jsr      $a939
A703: bda314     jsr      $a314
A706: 3404       pshs     b
A708: bda39a     jsr      $a39a
A70B: 3504       puls     b
A70D: cb81       addb     #-127
A70F: 271b       beq      $a72c
A711: 5a         decb     
A712: 3404       pshs     b
A714: bda430     jsr      $a430
A717: bda5e6     jsr      $a5e6
A71A: 8ea910     ldx      #-22256
A71D: bda83d     jsr      $a83d
A720: 3502       puls     a
A722: 9b81       adda     $81
A724: 8b80       adda     #-128
A726: 9781       sta      $81
A728: 0f80       clr      $80
A72A: 200c       bra      $a738
A72C: 9680       lda      $80
A72E: 2605       bne      $a735
A730: bda2b5     jsr      $a2b5
A733: 2003       bra      $a738
A735: bda3ac     jsr      $a3ac
A738: 39         rts      
A739: 9681       lda      $81
A73B: 2706       beq      $a743
A73D: 9680       lda      $80
A73F: 2602       bne      $a743
A741: 2005       bra      $a748
A743: 8604       lda      #4
A745: 9787       sta      $87
A747: 39         rts      
A748: bda386     jsr      $a386
A74B: 9691       lda      $91
A74D: 8080       suba     #-128
A74F: 3402       pshs     a
A751: 8680       lda      #-128
A753: 9791       sta      $91
A755: 8ea886     ldx      #-22394
A758: bda2be     jsr      $a2be
A75B: bda43f     jsr      $a43f
A75E: bda386     jsr      $a386
A761: 8ea88b     ldx      #-22389
A764: bda2be     jsr      $a2be
A767: bda55b     jsr      $a55b
A76A: bda386     jsr      $a386
A76D: 8ea8f6     ldx      #-22282
A770: bda2be     jsr      $a2be
A773: bda430     jsr      $a430
A776: 8ea8fb     ldx      #-22277
A779: bda81c     jsr      $a81c
A77C: 8ea895     ldx      #-22379
A77F: bda43c     jsr      $a43c
A782: bda386     jsr      $a386
A785: 4f         clra     
A786: 3504       puls     b
A788: 5d         tstb     
A789: 2a01       bpl      $a78c
A78B: 4a         deca     
A78C: bda39a     jsr      $a39a
A78F: bda43f     jsr      $a43f
A792: 8ea890     ldx      #-22384
A795: bda4c5     jsr      $a4c5
A798: 39         rts      
A799: 9680       lda      $80
A79B: 3402       pshs     a
A79D: 2a03       bpl      $a7a2
A79F: bda5e6     jsr      $a5e6
A7A2: 9681       lda      $81
A7A4: 3402       pshs     a
A7A6: 8181       cmpa     #-127
A7A8: 250c       bcs      $a7b6
A7AA: bda386     jsr      $a386
A7AD: 8ea8f6     ldx      #-22282
A7B0: bda2be     jsr      $a2be
A7B3: bda55b     jsr      $a55b
A7B6: 8ea8be     ldx      #-22338
A7B9: bda81c     jsr      $a81c
A7BC: 3502       puls     a
A7BE: 8181       cmpa     #-127
A7C0: 250c       bcs      $a7ce
A7C2: bda386     jsr      $a386
A7C5: 8ea872     ldx      #-22414
A7C8: bda2be     jsr      $a2be
A7CB: bda430     jsr      $a430
A7CE: 3502       puls     a
A7D0: 4d         tsta     
A7D1: 2a03       bpl      $a7d6
A7D3: bda5e6     jsr      $a5e6
A7D6: 39         rts      
A7D7: 8ea872     ldx      #-22414
A7DA: bda43c     jsr      $a43c
A7DD: 8ea877     ldx      #-22409
A7E0: bda558     jsr      $a558
A7E3: bda386     jsr      $a386
A7E6: bda939     jsr      $a939
A7E9: bda430     jsr      $a430
A7EC: bda5e6     jsr      $a5e6
A7EF: bda386     jsr      $a386
A7F2: 8ea87c     ldx      #-22404
A7F5: bda2be     jsr      $a2be
A7F8: bda430     jsr      $a430
A7FB: 9680       lda      $80
A7FD: 3402       pshs     a
A7FF: 2a05       bpl      $a806
A801: bda439     jsr      $a439
A804: 9680       lda      $80
A806: 2b03       bmi      $a80b
A808: bda5e6     jsr      $a5e6
A80B: 8ea87c     ldx      #-22404
A80E: bda43c     jsr      $a43c
A811: 3502       puls     a
A813: 4d         tsta     
A814: 2a03       bpl      $a819
A816: bda5e6     jsr      $a5e6
A819: 8ea89f     ldx      #-22369
A81C: 9f8e       stx      $8e
A81E: 3410       pshs     x
A820: 3410       pshs     x
A822: 327f       leas     -1, s
A824: 1f41       tfr      s, x
A826: bda2fc     jsr      $a2fc
A829: bda386     jsr      $a386
A82C: bda4c8     jsr      $a4c8
A82F: 8d0e       bsr      $a83f
A831: 1f41       tfr      s, x
A833: bda4c5     jsr      $a4c5
A836: 3510       puls     x
A838: 3510       puls     x
A83A: 3261       leas     1, s
A83C: 39         rts      
A83D: 9f8e       stx      $8e
A83F: 3410       pshs     x
A841: 3410       pshs     x
A843: 3410       pshs     x
A845: 1f41       tfr      s, x
A847: bda2fc     jsr      $a2fc
A84A: 9e8e       ldx      $8e
A84C: a684       lda      , x
A84E: 3001       leax     1, x
A850: 9f8e       stx      $8e
A852: 1f41       tfr      s, x
A854: a705       sta      5, x
A856: 9e8e       ldx      $8e
A858: bda4c5     jsr      $a4c5
A85B: 9e8e       ldx      $8e
A85D: c605       ldb      #5
A85F: 3a         abx      
A860: 9f8e       stx      $8e
A862: bda43c     jsr      $a43c
A865: 1f41       tfr      s, x
A867: 6a05       dec      5, x
A869: 26ed       bne      $a858
A86B: 3510       puls     x
A86D: 3510       puls     x
A86F: 3510       puls     x
A871: 39         rts      
A872: 40         nega     
A873: c90f       adcb     #15
A875: daa2       orb      $a2
A877: 41         fcb      $41
A878: c90f       adcb     #15
A87A: daa2       orb      $a2
A87C: 3f         swi      
A87D: 8000       suba     #0
A87F: 0000       neg      $00
A881: 40         nega     
A882: 0000       neg      $00
A884: 0000       neg      $00
A886: 40         nega     
A887: 3504       puls     b
A889: f33440     addd     $3440
A88C: b504f3     bita     $04f3
A88F: 3440       pshs     u
A891: 3172       leay     -14, s
A893: 17f8c0     lbsr     $a156
A896: 0000       neg      $00
A898: 0000       neg      $00
A89A: 40         nega     
A89B: b8aa3b     eora     $aa3b
A89E: 2905       bvs      $a8a5
A8A0: c266       sbcb     #102
A8A2: 1a2d       orcc     #45
A8A4: 1b         fcb      $1b
A8A5: 43         coma     
A8A6: 2807       bvc      $a8af
A8A8: fbf8c3     addb     $f8c3
A8AB: 9968       adca     $68
A8AD: 8901       adca     #1
A8AF: 43         coma     
A8B0: a335       subd     -11, y
A8B2: dfe1       stu      $e1
A8B4: c3255d     addd     #9565
A8B7: e728       stb      8, y
A8B9: 41         fcb      $41
A8BA: c90f       adcb     #15
A8BC: daa2       orb      $a2
A8BE: 0b         fcb      $0b
A8BF: bb3383     adda     $3383
A8C2: bdd33c     jsr      $d33c
A8C5: 9ef4       ldx      $f4
A8C7: a6f5       lda      [b, s]
A8C9: bd83fc     jsr      $83fc
A8CC: b0103e     suba     $103e
A8CF: 0c1f       inc      $1f
A8D1: 67         fcb      $67
A8D2: cabe       orb      #-66
A8D4: 5e         fcb      $5e
A8D5: 53         comb     
A8D6: cbc1       addb     #-63
A8D8: 3e         fcb      $3e
A8D9: 9464       anda     $64
A8DB: 704cbe     neg      $4cbe
A8DE: b7ea51     sta      $ea51
A8E1: 7a3ee3     dec      $3ee3
A8E4: 30887e     leax     126, x
A8E7: bf1244     stx      $1244
A8EA: 993a       adca     $3a
A8EC: 3f         swi      
A8ED: 4c         inca     
A8EE: cc91c7     ldd      #-28217
A8F1: bfaaaa     stx      $aaaa
A8F4: aa13       ora      -13, x
A8F6: 40         nega     
A8F7: 8000       suba     #0
A8F9: 0000       neg      $00
A8FB: 033f       com      $3f
A8FD: de56       ldu      $56
A8FF: cb79       addb     #121
A901: 40         nega     
A902: 13         sync     
A903: 9b0b       adda     $0b
A905: 6440       lsr      0, u
A907: 763893     ror      $3893
A90A: 164138     lbra     $ea45
A90D: aa3b       ora      -5, y
A90F: 2007       bra      $a918
A911: 38         fcb      $38
A912: b4583e     anda     $583e
A915: 56         rorb     
A916: 3a         abx      
A917: 167eb3     lbra     $27cd
A91A: 1b         fcb      $1b
A91B: 3b         rti      
A91C: af         fcb      $af
A91D: eee3       ldu      , --s
A91F: 853d       bita     #61
A921: 1d         sex      
A922: 841c       anda     #28
A924: 2a3e       bpl      $a964
A926: 6359       com      -7, u
A928: 58         lslb     
A929: 0a3f       dec      $3f
A92B: 75         fcb      $75
A92C: fde7c6     std      $e7c6
A92F: 40         nega     
A930: 3172       leay     -14, s
A932: 18         fcb      $18
A933: 10         fcb      $10
A934: 40         nega     
A935: 8000       suba     #0
A937: 0000       neg      $00
A939: 8d03       bsr      $a93e
A93B: 7ea475     jmp      $a475
A93E: 9681       lda      $81
A940: 2602       bne      $a944
A942: 2041       bra      $a985
A944: 8181       cmpa     #-127
A946: 2411       bcc      $a959
A948: 9680       lda      $80
A94A: 2605       bne      $a951
A94C: bda3b0     jsr      $a3b0
A94F: 2006       bra      $a957
A951: ccffff     ldd      #-1
A954: bda39a     jsr      $a39a
A957: 202c       bra      $a985
A959: 81a0       cmpa     #-96
A95B: 2428       bcc      $a985
A95D: 327f       leas     -1, s
A95F: 1f41       tfr      s, x
A961: 6f84       clr      , x
A963: 0482       lsr      $82
A965: 0683       ror      $83
A967: 0684       ror      $84
A969: 0685       ror      $85
A96B: 2402       bcc      $a96f
A96D: 6684       ror      , x
A96F: 4c         inca     
A970: 81a0       cmpa     #-96
A972: 26ef       bne      $a963
A974: 9781       sta      $81
A976: 9680       lda      $80
A978: 2707       beq      $a981
A97A: 6984       rol      , x
A97C: 2403       bcc      $a981
A97E: bda3d5     jsr      $a3d5
A981: 3261       leas     1, s
A983: 0f86       clr      $86
A985: 39         rts      
A986: aa         fcb      $aa
A987: aa         fcb      $aa
A988: aa         fcb      $aa
A989: aa         fcb      $aa
A98A: aa         fcb      $aa
A98B: aa         fcb      $aa
A98C: aa         fcb      $aa
A98D: aa         fcb      $aa
A98E: aa         fcb      $aa
A98F: aa10       ora      -16, x
A991: ce09ff     ldu      #2559
A994: bdb00c     jsr      $b00c
A997: 4f         clra     
A998: 5f         clrb     
A999: bda9af     jsr      $a9af
A99C: 3f         swi      
A99D: 20f1       bra      $a990
A99F: 10ce09ff   lds      #2559
A9A3: bdb00c     jsr      $b00c
A9A6: ccffff     ldd      #-1
A9A9: bda9af     jsr      $a9af
A9AC: 3f         swi      
A9AD: 20f0       bra      $a99f
A9AF: 3406       pshs     a, b
A9B1: bdb0ab     jsr      $b0ab
A9B4: cc000c     ldd      #12
A9B7: 3406       pshs     a, b
A9B9: cca99f     ldd      #-22113
A9BC: bdb006     jsr      $b006
A9BF: 3262       leas     2, s
A9C1: e661       ldb      1, s
A9C3: c1ff       cmpb     #-1
A9C5: 2606       bne      $a9cd
A9C7: c601       ldb      #1
A9C9: d732       stb      $32
A9CB: 2004       bra      $a9d1
A9CD: 8d45       bsr      $aa14
A9CF: 0f32       clr      $32
A9D1: d632       ldb      $32
A9D3: c108       cmpb     #8
A9D5: 2738       beq      $aa0f
A9D7: 4f         clra     
A9D8: 2020       bra      $a9fa
A9DA: 4f         clra     
A9DB: e663       ldb      3, s
A9DD: 3406       pshs     a, b
A9DF: cc0001     ldd      #1
A9E2: 9e2a       ldx      $2a
A9E4: ad84       jsr      , x
A9E6: 3262       leas     2, s
A9E8: 6f63       clr      3, s
A9EA: 39         rts      
A9EB: cc0002     ldd      #2
A9EE: 2003       bra      $a9f3
A9F0: cc0003     ldd      #3
A9F3: 9e2a       ldx      $2a
A9F5: 6e84       jmp      , x
A9F7: 0f32       clr      $32
A9F9: 39         rts      
A9FA: 8ea9f7     ldx      #-22025
A9FD: 4d         tsta     
A9FE: 2d0b       blt      $aa0b
AA00: c103       cmpb     #3
AA02: 2207       bhi      $aa0b
AA04: 8eaa1f     ldx      #-21985
AA07: 3a         abx      
AA08: 3a         abx      
AA09: ae84       ldx      , x
AA0B: ad84       jsr      , x
AA0D: 20c2       bra      $a9d1
AA0F: 4f         clra     
AA10: 5f         clrb     
AA11: 3262       leas     2, s
AA13: 39         rts      
AA14: cc0a00     ldd      #2560
AA17: dd20       std      $20
AA19: c375ff     addd     #30207
AA1C: dd22       std      $22
AA1E: 39         rts      
AA1F: aa66       ora      6, s
AA21: a9         fcb      $a9
AA22: daa9       orb      $a9
AA24: eba9f073   addb     -3981, y
AA28: 45         fcb      $45
AA29: 54         lsrb     
AA2A: 55         fcb      $55
AA2B: 50         negb     
AA2C: 006d       neg      $6d
AA2E: 4f         clra     
AA2F: 4e         fcb      $4e
AA30: 49         rola     
AA31: 54         lsrb     
AA32: 4f         clra     
AA33: 52         fcb      $52
AA34: 0061       neg      $61
AA36: 50         negb     
AA37: 4c         inca     
AA38: 0062       neg      $62
AA3A: 41         fcb      $41
AA3B: 53         comb     
AA3C: 49         rola     
AA3D: 43         coma     
AA3E: 0065       neg      $65
AA40: 44         lsra     
AA41: 49         rola     
AA42: 54         lsrb     
AA43: 0066       neg      $66
AA45: 4f         clra     
AA46: 52         fcb      $52
AA47: 54         lsrb     
AA48: 52         fcb      $52
AA49: 41         fcb      $41
AA4A: 4e         fcb      $4e
AA4B: 0070       neg      $70
AA4D: 41         fcb      $41
AA4E: 53         comb     
AA4F: 43         coma     
AA50: 41         fcb      $41
AA51: 4c         inca     
AA52: 0064       neg      $64
AA54: 45         fcb      $45
AA55: 56         rorb     
AA56: 45         fcb      $45
AA57: 4c         inca     
AA58: 4f         clra     
AA59: 50         negb     
AA5A: 4d         tsta     
AA5B: 45         fcb      $45
AA5C: 4e         fcb      $4e
AA5D: 54         lsrb     
AA5E: 0000       neg      $00
AA60: 0000       neg      $00
AA62: ad48       jsr      8, u
AA64: f02032     subb     $2032
AA67: 75         fcb      $75
AA68: ccabf2     ldd      #-21518
AA6B: bdb0b7     jsr      $b0b7
AA6E: ccaa27     ldd      #-21977
AA71: ed63       std      3, s
AA73: cc0020     ldd      #32
AA76: bdb0bd     jsr      $b0bd
AA79: cc0020     ldd      #32
AA7C: bdb0bd     jsr      $b0bd
AA7F: 4f         clra     
AA80: e6f803     ldb      [3, s]
AA83: bdb036     jsr      $b036
AA86: bdb0bd     jsr      $b0bd
AA89: ec63       ldd      3, s
AA8B: c30001     addd     #1
AA8E: ed63       std      3, s
AA90: 3406       pshs     a, b
AA92: 3510       puls     x
AA94: e684       ldb      , x
AA96: 26e7       bne      $aa7f
AA98: ec63       ldd      3, s
AA9A: c30001     addd     #1
AA9D: ed63       std      3, s
AA9F: bdb0c0     jsr      $b0c0
AAA2: e6f803     ldb      [3, s]
AAA5: 26cc       bne      $aa73
AAA7: bdb0c0     jsr      $b0c0
AAAA: bdb0c6     jsr      $b0c6
AAAD: e7e4       stb      , s
AAAF: c120       cmpb     #32
AAB1: 27f7       beq      $aaaa
AAB3: c10d       cmpb     #13
AAB5: 2608       bne      $aabf
AAB7: cc000c     ldd      #12
AABA: bdb0bd     jsr      $b0bd
AABD: 20a9       bra      $aa68
AABF: cc0400     ldd      #1024
AAC2: ed65       std      5, s
AAC4: ed63       std      3, s
AAC6: e6e4       ldb      , s
AAC8: c120       cmpb     #32
AACA: 271b       beq      $aae7
AACC: c10d       cmpb     #13
AACE: 2717       beq      $aae7
AAD0: ec63       ldd      3, s
AAD2: 3406       pshs     a, b
AAD4: c30001     addd     #1
AAD7: ed65       std      5, s
AAD9: 4f         clra     
AADA: e662       ldb      2, s
AADC: 3510       puls     x
AADE: e784       stb      , x
AAE0: bdb0c6     jsr      $b0c6
AAE3: e7e4       stb      , s
AAE5: 20df       bra      $aac6
AAE7: 6ff803     clr      [3, s]
AAEA: ec65       ldd      5, s
AAEC: ed63       std      3, s
AAEE: 4f         clra     
AAEF: e6f803     ldb      [3, s]
AAF2: bdb021     jsr      $b021
AAF5: 2707       beq      $aafe
AAF7: ec63       ldd      3, s
AAF9: c30001     addd     #1
AAFC: 20ee       bra      $aaec
AAFE: ec63       ldd      3, s
AB00: a365       subd     5, s
AB02: 3406       pshs     a, b
AB04: ec67       ldd      7, s
AB06: 3406       pshs     a, b
AB08: ccb124     ldd      #-20188
AB0B: bdb057     jsr      $b057
AB0E: 3264       leas     4, s
AB10: 260c       bne      $ab1e
AB12: ec65       ldd      5, s
AB14: 3406       pshs     a, b
AB16: ccac18     ldd      #-21480
AB19: bdb04e     jsr      $b04e
AB1C: 3262       leas     2, s
AB1E: ec65       ldd      5, s
AB20: ed63       std      3, s
AB22: ec63       ldd      3, s
AB24: 3406       pshs     a, b
AB26: c30001     addd     #1
AB29: ed65       std      5, s
AB2B: 3510       puls     x
AB2D: e684       ldb      , x
AB2F: c12e       cmpb     #46
AB31: 2705       beq      $ab38
AB33: e6f803     ldb      [3, s]
AB36: 26ea       bne      $ab22
AB38: 4f         clra     
AB39: 5f         clrb     
AB3A: dd2a       std      $2a
AB3C: ec63       ldd      3, s
AB3E: bdb045     jsr      $b045
AB41: 3406       pshs     a, b
AB43: ec65       ldd      5, s
AB45: 3406       pshs     a, b
AB47: ccaa27     ldd      #-21977
AB4A: bdb057     jsr      $b057
AB4D: 3264       leas     4, s
AB4F: ed61       std      1, s
AB51: 2746       beq      $ab99
AB53: 830002     subd     #2
AB56: 2f32       ble      $ab8a
AB58: ccaa27     ldd      #-21977
AB5B: ed69       std      9, s
AB5D: ec61       ldd      1, s
AB5F: c3ffff     addd     #-1
AB62: ed61       std      1, s
AB64: 2712       beq      $ab78
AB66: ec69       ldd      9, s
AB68: 3406       pshs     a, b
AB6A: ec6b       ldd      11, s
AB6C: bdb045     jsr      $b045
AB6F: c30001     addd     #1
AB72: e3e4       addd     , s
AB74: 3510       puls     x
AB76: 20e3       bra      $ab5b
AB78: ec63       ldd      3, s
AB7A: 3406       pshs     a, b
AB7C: ec6b       ldd      11, s
AB7E: bdb048     jsr      $b048
AB81: 3262       leas     2, s
AB83: ec63       ldd      3, s
AB85: bdb03c     jsr      $b03c
AB88: 200f       bra      $ab99
AB8A: ec61       ldd      1, s
AB8C: 58         lslb     
AB8D: 49         rola     
AB8E: c3aa60     addd     #-21920
AB91: 3406       pshs     a, b
AB93: 3510       puls     x
AB95: ec84       ldd      , x
AB97: dd2a       std      $2a
AB99: cc0400     ldd      #1024
AB9C: 3406       pshs     a, b
AB9E: ec67       ldd      7, s
ABA0: bdb045     jsr      $b045
ABA3: e3e4       addd     , s
ABA5: 3510       puls     x
ABA7: c30001     addd     #1
ABAA: ed67       std      7, s
ABAC: ed63       std      3, s
ABAE: e6e4       ldb      , s
ABB0: c10d       cmpb     #13
ABB2: 2717       beq      $abcb
ABB4: ec63       ldd      3, s
ABB6: 3406       pshs     a, b
ABB8: c30001     addd     #1
ABBB: ed65       std      5, s
ABBD: 4f         clra     
ABBE: e662       ldb      2, s
ABC0: 3510       puls     x
ABC2: e784       stb      , x
ABC4: bdb0c6     jsr      $b0c6
ABC7: e7e4       stb      , s
ABC9: 20e3       bra      $abae
ABCB: 4f         clra     
ABCC: 5f         clrb     
ABCD: ed63       std      3, s
ABCF: cc000c     ldd      #12
ABD2: bdb0bd     jsr      $b0bd
ABD5: c601       ldb      #1
ABD7: d732       stb      $32
ABD9: dc2a       ldd      $2a
ABDB: 2609       bne      $abe6
ABDD: ec65       ldd      5, s
ABDF: bdac20     jsr      $ac20
ABE2: 2602       bne      $abe6
ABE4: 0f32       clr      $32
ABE6: ec65       ldd      5, s
ABE8: 3406       pshs     a, b
ABEA: ec69       ldd      9, s
ABEC: bdb048     jsr      $b048
ABEF: 326d       leas     13, s
ABF1: 39         rts      
ABF2: 57         asrb     
ABF3: 61         fcb      $61
ABF4: 746572     lsr      $6572
ABF7: 6c6f       inc      15, s
ABF9: 6f20       clr      0, y
ABFB: 6d69       tst      9, s
ABFD: 6372       com      -14, s
ABFF: 6f53       clr      -13, u
AC01: 797374     rol      $7374
AC04: 65         fcb      $65
AC05: 6d73       tst      -13, s
AC07: 256e       bcs      $ac77
AC09: 256e       bcs      $ac79
AC0B: 53         comb     
AC0C: 65         fcb      $65
AC0D: 6c65       inc      5, s
AC0F: 6374       com      -12, s
AC11: 203a       bra      $ac4d
AC13: 256e       bcs      $ac83
AC15: 256e       bcs      $ac85
AC17: 0064       neg      $64
AC19: 6973       rol      -13, s
AC1B: 6b         fcb      $6b
AC1C: 2f31       ble      $ac4f
AC1E: 2e00       bgt      $ac20
AC20: 3406       pshs     a, b
AC22: 327d       leas     -3, s
AC24: ec63       ldd      3, s
AC26: 3406       pshs     a, b
AC28: ccad0e     ldd      #-21234
AC2B: bdb0b7     jsr      $b0b7
AC2E: 3262       leas     2, s
AC30: ccad1d     ldd      #-21219
AC33: 3406       pshs     a, b
AC35: ec65       ldd      5, s
AC37: bdb0ae     jsr      $b0ae
AC3A: 3262       leas     2, s
AC3C: ede4       std      , s
AC3E: 260a       bne      $ac4a
AC40: ccad1f     ldd      #-21217
AC43: bdb0b7     jsr      $b0b7
AC46: 6f62       clr      2, s
AC48: 2009       bra      $ac53
AC4A: 8d0d       bsr      $ac59
AC4C: e762       stb      2, s
AC4E: ece4       ldd      , s
AC50: bdb0b1     jsr      $b0b1
AC53: 4f         clra     
AC54: e662       ldb      2, s
AC56: 3265       leas     5, s
AC58: 39         rts      
AC59: 3406       pshs     a, b
AC5B: 3273       leas     -13, s
AC5D: 4f         clra     
AC5E: 5f         clrb     
AC5F: dd2a       std      $2a
AC61: 1f41       tfr      s, x
AC63: c607       ldb      #7
AC65: 3a         abx      
AC66: 3410       pshs     x
AC68: 3506       puls     a, b
AC6A: ed65       std      5, s
AC6C: cc0006     ldd      #6
AC6F: 3406       pshs     a, b
AC71: ec67       ldd      7, s
AC73: 3406       pshs     a, b
AC75: ece811     ldd      17, s
AC78: bdacfc     jsr      $acfc
AC7B: 3264       leas     4, s
AC7D: ec6d       ldd      13, s
AC7F: bdb0e1     jsr      $b0e1
AC82: 2663       bne      $ace7
AC84: ae65       ldx      5, s
AC86: e604       ldb      4, x
AC88: c102       cmpb     #2
AC8A: 2755       beq      $ace1
AC8C: dc2a       ldd      $2a
AC8E: 262d       bne      $acbd
AC90: e604       ldb      4, x
AC92: c101       cmpb     #1
AC94: 2622       bne      $acb8
AC96: cc0006     ldd      #6
AC99: 3406       pshs     a, b
AC9B: cc0039     ldd      #57
AC9E: 3406       pshs     a, b
ACA0: ccafd6     ldd      #-20522
ACA3: bdb04b     jsr      $b04b
ACA6: 3264       leas     4, s
ACA8: 4f         clra     
ACA9: ae65       ldx      5, s
ACAB: e605       ldb      5, x
ACAD: d73c       stb      $3c
ACAF: ec84       ldd      , x
ACB1: dd3d       std      $3d
ACB3: cc0039     ldd      #57
ACB6: 2003       bra      $acbb
ACB8: ecf805     ldd      [5, s]
ACBB: dd2a       std      $2a
ACBD: e604       ldb      4, x
ACBF: c101       cmpb     #1
ACC1: 2606       bne      $acc9
ACC3: 4f         clra     
ACC4: e605       ldb      5, x
ACC6: fdeffc     std      $effc
ACC9: ec02       ldd      2, x
ACCB: 3406       pshs     a, b
ACCD: ec84       ldd      , x
ACCF: 3406       pshs     a, b
ACD1: ece811     ldd      17, s
ACD4: 8d26       bsr      $acfc
ACD6: 3264       leas     4, s
ACD8: ec6d       ldd      13, s
ACDA: bdb0e1     jsr      $b0e1
ACDD: 2608       bne      $ace7
ACDF: 208b       bra      $ac6c
ACE1: c6ff       ldb      #-1
ACE3: e764       stb      4, s
ACE5: 2008       bra      $acef
ACE7: ccad33     ldd      #-21197
ACEA: bdb0b7     jsr      $b0b7
ACED: 6f64       clr      4, s
ACEF: 4f         clra     
ACF0: 5f         clrb     
ACF1: fdeffc     std      $effc
ACF4: 7f0220     clr      $0220
ACF7: e664       ldb      4, s
ACF9: 326f       leas     15, s
ACFB: 39         rts      
ACFC: 3406       pshs     a, b
ACFE: ec66       ldd      6, s
AD00: 3406       pshs     a, b
AD02: ec66       ldd      6, s
AD04: 3406       pshs     a, b
AD06: ec64       ldd      4, s
AD08: bdb108     jsr      $b108
AD0B: 3266       leas     6, s
AD0D: 39         rts      
AD0E: 4c         inca     
AD0F: 6f61       clr      1, s
AD11: 6469       lsr      9, s
AD13: 6e67       jmp      7, s
AD15: 2027       bra      $ad3e
AD17: 2573       bcs      $ad8c
AD19: 2725       beq      $ad40
AD1B: 6e00       jmp      0, x
AD1D: 4c         inca     
AD1E: 0050       neg      $50
AD20: 72         fcb      $72
AD21: 6f67       clr      7, s
AD23: 72         fcb      $72
AD24: 61         fcb      $61
AD25: 6d20       tst      0, y
AD27: 6e6f       jmp      15, s
AD29: 742066     lsr      $2066
AD2C: 6f75       clr      -11, s
AD2E: 6e64       jmp      4, s
AD30: 256e       bcs      $ada0
AD32: 0065       neg      $65
AD34: 72         fcb      $72
AD35: 72         fcb      $72
AD36: 6f72       clr      -14, s
AD38: 206c       bra      $ada6
AD3A: 6f61       clr      1, s
AD3C: 6469       lsr      9, s
AD3E: 6e67       jmp      7, s
AD40: 2066       bra      $ada8
AD42: 696c       rol      12, s
AD44: 65         fcb      $65
AD45: 256e       bcs      $adb5
AD47: 0034       neg      $34
AD49: 06e6       ror      $e6
AD4B: 61         fcb      $61
AD4C: c101       cmpb     #1
AD4E: 2602       bne      $ad52
AD50: 8d05       bsr      $ad57
AD52: 0f32       clr      $32
AD54: 3262       leas     2, s
AD56: 39         rts      
AD57: 8d05       bsr      $ad5e
AD59: 8d1f       bsr      $ad7a
AD5B: 7eb078     jmp      $b078
AD5E: ccafaa     ldd      #-20566
AD61: bdb08d     jsr      $b08d
AD64: ccaf66     ldd      #-20634
AD67: bdb045     jsr      $b045
AD6A: 3406       pshs     a, b
AD6C: ccaf66     ldd      #-20634
AD6F: bdb0ba     jsr      $b0ba
AD72: 3262       leas     2, s
AD74: cc0115     ldd      #277
AD77: 7eb087     jmp      $b087
AD7A: 32e8eb     leas     -21, s
AD7D: 1f41       tfr      s, x
AD7F: c60c       ldb      #12
AD81: 3a         abx      
AD82: 3410       pshs     x
AD84: 3506       puls     a, b
AD86: ed6a       std      10, s
AD88: ccaf64     ldd      #-20636
AD8B: 3406       pshs     a, b
AD8D: ccaf5b     ldd      #-20645
AD90: bdb0ae     jsr      $b0ae
AD93: 3262       leas     2, s
AD95: ed66       std      6, s
AD97: bdb0c6     jsr      $b0c6
AD9A: 83000d     subd     #13
AD9D: 26f8       bne      $ad97
AD9F: cc0001     ldd      #1
ADA2: ede4       std      , s
ADA4: 830006     subd     #6
ADA7: 2e6f       bgt      $ae18
ADA9: ece4       ldd      , s
ADAB: 1f98       tfr      b, a
ADAD: 5f         clrb     
ADAE: c30001     addd     #1
ADB1: bdb087     jsr      $b087
ADB4: cc000d     ldd      #13
ADB7: 3406       pshs     a, b
ADB9: ec68       ldd      8, s
ADBB: bdb0cf     jsr      $b0cf
ADBE: 3262       leas     2, s
ADC0: cc0015     ldd      #21
ADC3: ed62       std      2, s
ADC5: ec62       ldd      2, s
ADC7: c3ffff     addd     #-1
ADCA: ed62       std      2, s
ADCC: 2705       beq      $add3
ADCE: bdb0c6     jsr      $b0c6
ADD1: 20f2       bra      $adc5
ADD3: ec6a       ldd      10, s
ADD5: ed68       std      8, s
ADD7: bdb0c6     jsr      $b0c6
ADDA: e7f808     stb      [8, s]
ADDD: c10d       cmpb     #13
ADDF: 2717       beq      $adf8
ADE1: ec62       ldd      2, s
ADE3: 830008     subd     #8
ADE6: 2c07       bge      $adef
ADE8: ec68       ldd      8, s
ADEA: c30001     addd     #1
ADED: ed68       std      8, s
ADEF: ec62       ldd      2, s
ADF1: c30001     addd     #1
ADF4: ed62       std      2, s
ADF6: 20df       bra      $add7
ADF8: 6ff808     clr      [8, s]
ADFB: ece4       ldd      , s
ADFD: 58         lslb     
ADFE: 49         rola     
ADFF: c3afae     addd     #-20562
AE02: 3406       pshs     a, b
AE04: 3510       puls     x
AE06: ec84       ldd      , x
AE08: ed64       std      4, s
AE0A: ec6a       ldd      10, s
AE0C: adf804     jsr      [4, s]
AE0F: 2607       bne      $ae18
AE11: ece4       ldd      , s
AE13: c30001     addd     #1
AE16: 208a       bra      $ada2
AE18: ece4       ldd      , s
AE1A: 830007     subd     #7
AE1D: 2720       beq      $ae3f
AE1F: ece4       ldd      , s
AE21: 1f98       tfr      b, a
AE23: 5f         clrb     
AE24: c30015     addd     #21
AE27: bdb087     jsr      $b087
AE2A: cc003f     ldd      #63
AE2D: bdb0bd     jsr      $b0bd
AE30: cc0006     ldd      #6
AE33: bdb0bd     jsr      $b0bd
AE36: cc0008     ldd      #8
AE39: bdb0bd     jsr      $b0bd
AE3C: 7ead97     jmp      $ad97
AE3F: 4f         clra     
AE40: d6a3       ldb      $a3
AE42: 3406       pshs     a, b
AE44: d6a2       ldb      $a2
AE46: 3406       pshs     a, b
AE48: dca0       ldd      $a0
AE4A: 3406       pshs     a, b
AE4C: cceff0     ldd      #-4112
AE4F: bdb093     jsr      $b093
AE52: 3266       leas     6, s
AE54: ec66       ldd      6, s
AE56: bdb0b1     jsr      $b0b1
AE59: 32e815     leas     21, s
AE5C: 39         rts      
AE5D: 3406       pshs     a, b
AE5F: 327f       leas     -1, s
AE61: ec61       ldd      1, s
AE63: bdb00f     jsr      $b00f
AE66: dda0       std      $a0
AE68: ec61       ldd      1, s
AE6A: 3406       pshs     a, b
AE6C: c30001     addd     #1
AE6F: ed63       std      3, s
AE71: 3510       puls     x
AE73: 4f         clra     
AE74: e684       ldb      , x
AE76: bdb024     jsr      $b024
AE79: 102700ae   lbeq     $af2b
AE7D: e6f801     ldb      [1, s]
AE80: 26e6       bne      $ae68
AE82: 2068       bra      $aeec
AE84: 3406       pshs     a, b
AE86: 327d       leas     -3, s
AE88: 6fe4       clr      , s
AE8A: ec63       ldd      3, s
AE8C: bdb045     jsr      $b045
AE8F: 3406       pshs     a, b
AE91: ec65       ldd      5, s
AE93: 3406       pshs     a, b
AE95: ccafbc     ldd      #-20548
AE98: bdb057     jsr      $b057
AE9B: 3264       leas     4, s
AE9D: ed61       std      1, s
AE9F: 270e       beq      $aeaf
AEA1: c3afd1     addd     #-20527
AEA4: 3406       pshs     a, b
AEA6: 4f         clra     
AEA7: 3510       puls     x
AEA9: e684       ldb      , x
AEAB: d7a2       stb      $a2
AEAD: 2004       bra      $aeb3
AEAF: c6ff       ldb      #-1
AEB1: e7e4       stb      , s
AEB3: 4f         clra     
AEB4: e6e4       ldb      , s
AEB6: 3265       leas     5, s
AEB8: 39         rts      
AEB9: 3406       pshs     a, b
AEBB: 327f       leas     -1, s
AEBD: 6fe4       clr      , s
AEBF: e6f801     ldb      [1, s]
AEC2: c131       cmpb     #49
AEC4: 2604       bne      $aeca
AEC6: 0fa3       clr      $a3
AEC8: 2065       bra      $af2f
AECA: c132       cmpb     #50
AECC: 265d       bne      $af2b
AECE: c680       ldb      #-128
AED0: d7a3       stb      $a3
AED2: 207d       bra      $af51
AED4: 3406       pshs     a, b
AED6: 327f       leas     -1, s
AED8: ec61       ldd      1, s
AEDA: 8d55       bsr      $af31
AEDC: 274d       beq      $af2b
AEDE: cc037c     ldd      #892
AEE1: 3406       pshs     a, b
AEE3: ec63       ldd      3, s
AEE5: bdb01e     jsr      $b01e
AEE8: 3262       leas     2, s
AEEA: d77c       stb      $7c
AEEC: 2039       bra      $af27
AEEE: 3406       pshs     a, b
AEF0: 327f       leas     -1, s
AEF2: ec61       ldd      1, s
AEF4: 8d3b       bsr      $af31
AEF6: 2733       beq      $af2b
AEF8: ec61       ldd      1, s
AEFA: bdb045     jsr      $b045
AEFD: 830002     subd     #2
AF00: 2e29       bgt      $af2b
AF02: cc007b     ldd      #123
AF05: 2017       bra      $af1e
AF07: 3406       pshs     a, b
AF09: 327f       leas     -1, s
AF0B: ec61       ldd      1, s
AF0D: 8d22       bsr      $af31
AF0F: 271a       beq      $af2b
AF11: ec61       ldd      1, s
AF13: bdb045     jsr      $b045
AF16: 830002     subd     #2
AF19: 2e10       bgt      $af2b
AF1B: cc007d     ldd      #125
AF1E: 3406       pshs     a, b
AF20: ec63       ldd      3, s
AF22: bdb01e     jsr      $b01e
AF25: 3262       leas     2, s
AF27: 6fe4       clr      , s
AF29: 202a       bra      $af55
AF2B: c6ff       ldb      #-1
AF2D: e7e4       stb      , s
AF2F: 2024       bra      $af55
AF31: 3406       pshs     a, b
AF33: 327f       leas     -1, s
AF35: 4f         clra     
AF36: e6f801     ldb      [1, s]
AF39: bdb02a     jsr      $b02a
AF3C: 2715       beq      $af53
AF3E: ec61       ldd      1, s
AF40: c30001     addd     #1
AF43: ed61       std      1, s
AF45: 3406       pshs     a, b
AF47: 3510       puls     x
AF49: e684       ldb      , x
AF4B: 26e8       bne      $af35
AF4D: c6ff       ldb      #-1
AF4F: e7e4       stb      , s
AF51: 2002       bra      $af55
AF53: 6fe4       clr      , s
AF55: 4f         clra     
AF56: e6e4       ldb      , s
AF58: 3263       leas     3, s
AF5A: 39         rts      
AF5B: 6b         fcb      $6b
AF5C: 65         fcb      $65
AF5D: 79626f     rol      $626f
AF60: 61         fcb      $61
AF61: 72         fcb      $72
AF62: 6400       lsr      0, x
AF64: 57         asrb     
AF65: 000c       neg      $0c
AF67: 42         fcb      $42
AF68: 41         fcb      $41
AF69: 55         fcb      $55
AF6A: 44         lsra     
AF6B: 0932       rol      $32
AF6D: 3430       pshs     x, y
AF6F: 300d       leax     13, x
AF71: 50         negb     
AF72: 41         fcb      $41
AF73: 52         fcb      $52
AF74: 49         rola     
AF75: 54         lsrb     
AF76: 59         rolb     
AF77: 0945       rol      $45
AF79: 56         rorb     
AF7A: 45         fcb      $45
AF7B: 4e         fcb      $4e
AF7C: 0d53       tst      $53
AF7E: 54         lsrb     
AF7F: 4f         clra     
AF80: 50         negb     
AF81: 42         fcb      $42
AF82: 49         rola     
AF83: 54         lsrb     
AF84: 53         comb     
AF85: 0931       rol      $31
AF87: 0d50       tst      $50
AF89: 52         fcb      $52
AF8A: 4f         clra     
AF8B: 4d         tsta     
AF8C: 50         negb     
AF8D: 54         lsrb     
AF8E: 0931       rol      $31
AF90: 310d       leay     13, x
AF92: 4c         inca     
AF93: 49         rola     
AF94: 4e         fcb      $4e
AF95: 45         fcb      $45
AF96: 45         fcb      $45
AF97: 4e         fcb      $4e
AF98: 44         lsra     
AF99: 0930       rol      $30
AF9B: 44         lsra     
AF9C: 0d52       tst      $52
AF9E: 45         fcb      $45
AF9F: 53         comb     
AFA0: 50         negb     
AFA1: 4f         clra     
AFA2: 4e         fcb      $4e
AFA3: 53         comb     
AFA4: 45         fcb      $45
AFA5: 0931       rol      $31
AFA7: 330d       leau     13, x
AFA9: 0000       neg      $00
AFAB: 15         fcb      $15
AFAC: 0065       neg      $65
AFAE: 0000       neg      $00
AFB0: ae5d       ldx      -3, u
AFB2: ae84       ldx      , x
AFB4: aeb9aed4   ldx      [-20780, y]
AFB8: ae         fcb      $ae
AFB9: ee         fcb      $ee
AFBA: af07       stx      7, x
AFBC: 6f64       clr      4, s
AFBE: 6400       lsr      0, x
AFC0: 65         fcb      $65
AFC1: 76656e     ror      $656e
AFC4: 006d       neg      $6d
AFC6: 61         fcb      $61
AFC7: 72         fcb      $72
AFC8: 6b         fcb      $6b
AFC9: 0073       neg      $73
AFCB: 706163     neg      $6163
AFCE: 65         fcb      $65
AFCF: 0000       neg      $00
AFD1: 0020       neg      $20
AFD3: 60a0       neg      , y+
AFD5: e0bdb009   subb     [$5fe2, pcr]
AFD9: 0090       neg      $90
AFDB: 00dc       neg      $dc
AFDD: 83d294     subd     #-11628
AFE0: ffffff     stu      $ffff
AFE3: ffffff     stu      $ffff
AFE6: ffffff     stu      $ffff
AFE9: ffffff     stu      $ffff
AFEC: ffffff     stu      $ffff
AFEF: ffffff     stu      $ffff
AFF2: ffffff     stu      $ffff
AFF5: ffffff     stu      $ffff
AFF8: ffffff     stu      $ffff
AFFB: ffffff     stu      $ffff
AFFE: ffff7e     stu      $ff7e
B001: bc757e     cmpx     $757e
B004: bcab7e     cmpx     $ab7e
B007: bc2d7e     cmpx     $2d7e
B00A: bbf17e     adda     $f17e
B00D: bc217e     cmpx     $217e
B010: b87e7e     eora     $7e7e
B013: bacb7e     ora      $cb7e
B016: bb487e     adda     $487e
B019: ba587e     ora      $587e
B01C: b8e67e     eora     $e67e
B01F: b9467e     adca     $467e
B022: b9d87e     adca     $d87e
B025: ba067e     ora      $067e
B028: ba147e     ora      $147e
B02B: ba2d7e     ora      $2d7e
B02E: b9fa7e     adca     $fa7e
B031: b9ee7e     adca     $ee7e
B034: ba8d7e     ora      $8d7e
B037: bab47e     ora      $b47e
B03A: ba9e7e     ora      $9e7e
B03D: ba777e     ora      $777e
B040: b7827e     sta      $827e
B043: b7ce7e     sta      $ce7e
B046: b7b17e     sta      $b17e
B049: b7ff7e     sta      $ff7e
B04C: bb687e     adda     $687e
B04F: b80a7e     eora     $0a7e
B052: b8327e     eora     $327e
B055: b84a7e     eora     $4a7e
B058: bba77e     adda     $a77e
B05B: b6777e     lda      $777e
B05E: b67f7e     lda      $7f7e
B061: b6877e     lda      $877e
B064: b7427e     sta      $427e
B067: b6ce7e     lda      $ce7e
B06A: b6d87e     lda      $d87e
B06D: b7487e     sta      $487e
B070: b74a7e     sta      $4a7e
B073: b6c47e     lda      $c47e
B076: e15f       cmpb     -1, u
B078: 7ed4cc     jmp      $d4cc
B07B: 7ed4f0     jmp      $d4f0
B07E: 7ed4d2     jmp      $d4d2
B081: 7ed518     jmp      $d518
B084: 7ed500     jmp      $d500
B087: 7ed509     jmp      $d509
B08A: 7ed521     jmp      $d521
B08D: 7ed7e3     jmp      $d7e3
B090: 7ed817     jmp      $d817
B093: 7ed55c     jmp      $d55c
B096: 7ed598     jmp      $d598
B099: 7ed5bb     jmp      $d5bb
B09C: 7ed62e     jmp      $d62e
B09F: 7eb5d2     jmp      $b5d2
B0A2: 7eb610     jmp      $b610
B0A5: 7eb627     jmp      $b627
B0A8: 7ec1f5     jmp      $c1f5
B0AB: 7eb1c1     jmp      $b1c1
B0AE: 7eb23b     jmp      $b23b
B0B1: 7eb292     jmp      $b292
B0B4: 7eb4a3     jmp      $b4a3
B0B7: 7eb221     jmp      $b221
B0BA: 7eb20f     jmp      $b20f
B0BD: 7eb1ea     jmp      $b1ea
B0C0: 7eb1f9     jmp      $b1f9
B0C3: 7eb1fe     jmp      $b1fe
B0C6: 7eb1e5     jmp      $b1e5
B0C9: 7eb3aa     jmp      $b3aa
B0CC: 7eb367     jmp      $b367
B0CF: 7eb2fd     jmp      $b2fd
B0D2: 7eb324     jmp      $b324
B0D5: 7eb333     jmp      $b333
B0D8: 7eb2a6     jmp      $b2a6
B0DB: 7eb4c9     jmp      $b4c9
B0DE: 7eb4d3     jmp      $b4d3
B0E1: 7eb4e5     jmp      $b4e5
B0E4: 7eb510     jmp      $b510
B0E7: 7eb58b     jmp      $b58b
B0EA: 7eb518     jmp      $b518
B0ED: 7eb543     jmp      $b543
B0F0: 7ec1e1     jmp      $c1e1
B0F3: 7ee107     jmp      $e107
B0F6: 7ee158     jmp      $e158
B0F9: 7ee0f4     jmp      $e0f4
B0FC: 7ee13c     jmp      $e13c
B0FF: 7ede01     jmp      $de01
B102: 7ede07     jmp      $de07
B105: 7eb5bd     jmp      $b5bd
B108: 7ec2d8     jmp      $c2d8
B10B: 7ec52c     jmp      $c52c
B10E: 7ec6c6     jmp      $c6c6
B111: 0000       neg      $00
B113: 0000       neg      $00
B115: 0000       neg      $00
B117: 0000       neg      $00
B119: 0027       neg      $27
B11B: 10         fcb      $10
B11C: 03e8       com      $e8
B11E: 0064       neg      $64
B120: 000a       neg      $0a
B122: 0001       neg      $01
B124: 6469       lsr      9, s
B126: 736b00     com      $6b00
B129: 707269     neg      $7269
B12C: 6e74       jmp      -12, s
B12E: 65         fcb      $65
B12F: 72         fcb      $72
B130: 0069       neg      $69
B132: 65         fcb      $65
B133: 65         fcb      $65
B134: 65         fcb      $65
B135: 0068       neg      $68
B137: 6f73       clr      -13, s
B139: 740074     lsr      >$0074
B13C: 65         fcb      $65
B13D: 72         fcb      $72
B13E: 6d69       tst      9, s
B140: 6e61       jmp      1, s
B142: 6c00       inc      0, x
B144: 736572     com      $6572
B147: 6961       rol      1, s
B149: 6c00       inc      0, x
B14B: 6b         fcb      $6b
B14C: 65         fcb      $65
B14D: 79626f     rol      $626f
B150: 61         fcb      $61
B151: 72         fcb      $72
B152: 6400       lsr      0, x
B154: 0000       neg      $00
B156: 8182       cmpa     #-126
B158: 830405     subd     #1029
B15B: 0607       ror      $07
B15D: 6649       ror      9, u
B15F: 58         lslb     
B160: 45         fcb      $45
B161: 44         lsra     
B162: 0074       neg      $74
B164: 45         fcb      $45
B165: 58         lslb     
B166: 54         lsrb     
B167: 0076       neg      $76
B169: 41         fcb      $41
B16A: 52         fcb      $52
B16B: 49         rola     
B16C: 41         fcb      $41
B16D: 42         fcb      $42
B16E: 4c         inca     
B16F: 45         fcb      $45
B170: 0000       neg      $00
B172: 0066       neg      $66
B174: 747672     lsr      $7672
B177: 65         fcb      $65
B178: 6c00       inc      0, x
B17A: 736571     com      $6571
B17D: 0070       neg      $70
B17F: 72         fcb      $72
B180: 6700       asr      0, x
B182: 75         fcb      $75
B183: 737200     com      $7200
B186: 0000       neg      $00
B188: 0810       lsl      $10
B18A: 2010       bra      $b19c
B18C: 72         fcb      $72
B18D: 45         fcb      $45
B18E: 41         fcb      $41
B18F: 44         lsra     
B190: 0077       neg      $77
B192: 52         fcb      $52
B193: 49         rola     
B194: 54         lsrb     
B195: 45         fcb      $45
B196: 0075       neg      $75
B198: 50         negb     
B199: 44         lsra     
B19A: 41         fcb      $41
B19B: 54         lsrb     
B19C: 45         fcb      $45
B19D: 0061       neg      $61
B19F: 50         negb     
B1A0: 50         negb     
B1A1: 45         fcb      $45
B1A2: 4e         fcb      $4e
B1A3: 44         lsra     
B1A4: 006c       neg      $6c
B1A6: 4f         clra     
B1A7: 41         fcb      $41
B1A8: 44         lsra     
B1A9: 0073       neg      $73
B1AB: 54         lsrb     
B1AC: 4f         clra     
B1AD: 52         fcb      $52
B1AE: 45         fcb      $45
B1AF: 0000       neg      $00
B1B1: 0001       neg      $01
B1B3: 02         fcb      $02
B1B4: 0306       com      $06
B1B6: 8142       cmpa     #66
B1B8: 746572     lsr      $6572
B1BB: 6d69       tst      9, s
B1BD: 6e61       jmp      1, s
B1BF: 6c00       inc      0, x
B1C1: bdd406     jsr      $d406
B1C4: 4f         clra     
B1C5: 5f         clrb     
B1C6: dd6b       std      $6b
B1C8: ccb5ce     ldd      #-18994
B1CB: 3406       pshs     a, b
B1CD: ccb1b8     ldd      #-20040
B1D0: 8d69       bsr      $b23b
B1D2: 3262       leas     2, s
B1D4: dd6b       std      $6b
B1D6: ccb5d0     ldd      #-18992
B1D9: 3406       pshs     a, b
B1DB: ccb1b8     ldd      #-20040
B1DE: 8d5b       bsr      $b23b
B1E0: 3262       leas     2, s
B1E2: dd6d       std      $6d
B1E4: 39         rts      
B1E5: dc6b       ldd      $6b
B1E7: 7eb2a6     jmp      $b2a6
B1EA: 3406       pshs     a, b
B1EC: 4f         clra     
B1ED: e661       ldb      1, s
B1EF: 3406       pshs     a, b
B1F1: dc6d       ldd      $6d
B1F3: bdb2fd     jsr      $b2fd
B1F6: 3264       leas     4, s
B1F8: 39         rts      
B1F9: dc6d       ldd      $6d
B1FB: 7eb324     jmp      $b324
B1FE: 3406       pshs     a, b
B200: ec64       ldd      4, s
B202: 3406       pshs     a, b
B204: ec62       ldd      2, s
B206: 3406       pshs     a, b
B208: dc6b       ldd      $6b
B20A: bdb333     jsr      $b333
B20D: 200f       bra      $b21e
B20F: 3406       pshs     a, b
B211: ec64       ldd      4, s
B213: 3406       pshs     a, b
B215: ec62       ldd      2, s
B217: 3406       pshs     a, b
B219: dc6d       ldd      $6d
B21B: bdb367     jsr      $b367
B21E: 3266       leas     6, s
B220: 39         rts      
B221: 3510       puls     x
B223: 3406       pshs     a, b
B225: 3410       pshs     x
B227: 1f41       tfr      s, x
B229: c602       ldb      #2
B22B: 3a         abx      
B22C: 3410       pshs     x
B22E: dc6d       ldd      $6d
B230: bdb3ba     jsr      $b3ba
B233: 3262       leas     2, s
B235: 3510       puls     x
B237: 3262       leas     2, s
B239: 6e84       jmp      , x
B23B: 3406       pshs     a, b
B23D: 32e8ca     leas     -54, s
B240: bdd412     jsr      $d412
B243: ede4       std      , s
B245: 2745       beq      $b28c
B247: 1f41       tfr      s, x
B249: c602       ldb      #2
B24B: 3a         abx      
B24C: 3410       pshs     x
B24E: ece838     ldd      56, s
B251: bdb7ff     jsr      $b7ff
B254: 3262       leas     2, s
B256: 1f41       tfr      s, x
B258: c62b       ldb      #43
B25A: 3a         abx      
B25B: 3410       pshs     x
B25D: ece83c     ldd      60, s
B260: bdb7ff     jsr      $b7ff
B263: 3262       leas     2, s
B265: 1f41       tfr      s, x
B267: c62b       ldb      #43
B269: 3a         abx      
B26A: 3410       pshs     x
B26C: 1f41       tfr      s, x
B26E: c604       ldb      #4
B270: 3a         abx      
B271: 3410       pshs     x
B273: ec64       ldd      4, s
B275: bdc244     jsr      $c244
B278: 3264       leas     4, s
B27A: ece4       ldd      , s
B27C: bdc9d6     jsr      $c9d6
B27F: 2602       bne      $b283
B281: 2009       bra      $b28c
B283: ece4       ldd      , s
B285: bdd483     jsr      $d483
B288: 4f         clra     
B289: 5f         clrb     
B28A: ede4       std      , s
B28C: ece4       ldd      , s
B28E: 32e838     leas     56, s
B291: 39         rts      
B292: 3406       pshs     a, b
B294: ece4       ldd      , s
B296: bdc2a5     jsr      $c2a5
B299: ece4       ldd      , s
B29B: bdc9d6     jsr      $c9d6
B29E: ece4       ldd      , s
B2A0: bdd483     jsr      $d483
B2A3: 3262       leas     2, s
B2A5: 39         rts      
B2A6: 3406       pshs     a, b
B2A8: 327f       leas     -1, s
B2AA: ae61       ldx      1, s
B2AC: e69804     ldb      [4, x]
B2AF: c101       cmpb     #1
B2B1: 260c       bne      $b2bf
B2B3: e603       ldb      3, x
B2B5: c101       cmpb     #1
B2B7: 2706       beq      $b2bf
B2B9: c601       ldb      #1
B2BB: e703       stb      3, x
B2BD: 2037       bra      $b2f6
B2BF: cc0001     ldd      #1
B2C2: 3406       pshs     a, b
B2C4: 1f41       tfr      s, x
B2C6: c602       ldb      #2
B2C8: 3a         abx      
B2C9: 3410       pshs     x
B2CB: ec65       ldd      5, s
B2CD: bdc2d8     jsr      $c2d8
B2D0: 3264       leas     4, s
B2D2: ae61       ldx      1, s
B2D4: 4f         clra     
B2D5: e69804     ldb      [4, x]
B2D8: e703       stb      3, x
B2DA: c101       cmpb     #1
B2DC: 2614       bne      $b2f2
B2DE: e684       ldb      , x
B2E0: c174       cmpb     #116
B2E2: 2608       bne      $b2ec
B2E4: ae04       ldx      4, x
B2E6: e602       ldb      2, x
B2E8: c104       cmpb     #4
B2EA: 260e       bne      $b2fa
B2EC: ae61       ldx      1, s
B2EE: 6f03       clr      3, x
B2F0: 2008       bra      $b2fa
B2F2: e603       ldb      3, x
B2F4: 2704       beq      $b2fa
B2F6: c60d       ldb      #13
B2F8: e7e4       stb      , s
B2FA: 7eb50a     jmp      $b50a
B2FD: 3406       pshs     a, b
B2FF: 327f       leas     -1, s
B301: 4f         clra     
B302: e666       ldb      6, s
B304: e7e4       stb      , s
B306: cc0001     ldd      #1
B309: 3406       pshs     a, b
B30B: 1f41       tfr      s, x
B30D: c602       ldb      #2
B30F: 3a         abx      
B310: 3410       pshs     x
B312: ec65       ldd      5, s
B314: bdc52c     jsr      $c52c
B317: 3264       leas     4, s
B319: ae61       ldx      1, s
B31B: 4f         clra     
B31C: e69804     ldb      [4, x]
B31F: e703       stb      3, x
B321: 3263       leas     3, s
B323: 39         rts      
B324: 3406       pshs     a, b
B326: ccffff     ldd      #-1
B329: 3406       pshs     a, b
B32B: ec62       ldd      2, s
B32D: bdc6c6     jsr      $c6c6
B330: 7eb4bd     jmp      $b4bd
B333: 3406       pshs     a, b
B335: 327e       leas     -2, s
B337: 4f         clra     
B338: 5f         clrb     
B339: ede4       std      , s
B33B: ec62       ldd      2, s
B33D: bdc7f7     jsr      $c7f7
B340: ae62       ldx      2, s
B342: 4f         clra     
B343: e69804     ldb      [4, x]
B346: e703       stb      3, x
B348: 2619       bne      $b363
B34A: ec68       ldd      8, s
B34C: 3406       pshs     a, b
B34E: ec68       ldd      8, s
B350: 3406       pshs     a, b
B352: ec66       ldd      6, s
B354: bdc2d8     jsr      $c2d8
B357: 3264       leas     4, s
B359: ede4       std      , s
B35B: ae62       ldx      2, s
B35D: 4f         clra     
B35E: e69804     ldb      [4, x]
B361: e703       stb      3, x
B363: ece4       ldd      , s
B365: 2050       bra      $b3b7
B367: 3406       pshs     a, b
B369: ece4       ldd      , s
B36B: bdc7f7     jsr      $c7f7
B36E: aee4       ldx      , s
B370: 4f         clra     
B371: e69804     ldb      [4, x]
B374: e703       stb      3, x
B376: c103       cmpb     #3
B378: 10270199   lbeq     $b515
B37C: ec66       ldd      6, s
B37E: 3406       pshs     a, b
B380: ec66       ldd      6, s
B382: 3406       pshs     a, b
B384: ec64       ldd      4, s
B386: bdc52c     jsr      $c52c
B389: 3264       leas     4, s
B38B: aee4       ldx      , s
B38D: 4f         clra     
B38E: e69804     ldb      [4, x]
B391: e703       stb      3, x
B393: 5f         clrb     
B394: 3406       pshs     a, b
B396: ec62       ldd      2, s
B398: bdc6c6     jsr      $c6c6
B39B: 3262       leas     2, s
B39D: aee4       ldx      , s
B39F: e603       ldb      3, x
B3A1: c103       cmpb     #3
B3A3: 1027016e   lbeq     $b515
B3A7: 7eb4c1     jmp      $b4c1
B3AA: 3406       pshs     a, b
B3AC: 1f41       tfr      s, x
B3AE: c604       ldb      #4
B3B0: 3a         abx      
B3B1: 3410       pshs     x
B3B3: ec62       ldd      2, s
B3B5: 8d03       bsr      $b3ba
B3B7: 3264       leas     4, s
B3B9: 39         rts      
B3BA: 3406       pshs     a, b
B3BC: 32e8e5     leas     -27, s
B3BF: ecf81f     ldd      [31, s]
B3C2: ede818     std      24, s
B3C5: ece81f     ldd      31, s
B3C8: c30002     addd     #2
B3CB: ede81f     std      31, s
B3CE: 4f         clra     
B3CF: e6f818     ldb      [24, s]
B3D2: e7e815     stb      21, s
B3D5: 102700c6   lbeq     $b49f
B3D9: c125       cmpb     #37
B3DB: 1026009e   lbne     $b47d
B3DF: ece818     ldd      24, s
B3E2: c30001     addd     #1
B3E5: ede818     std      24, s
B3E8: 4f         clra     
B3E9: e6f818     ldb      [24, s]
B3EC: e7e815     stb      21, s
B3EF: bdbab4     jsr      $bab4
B3F2: e7e81a     stb      26, s
B3F5: c16e       cmpb     #110
B3F7: 10270092   lbeq     $b48d
B3FB: c163       cmpb     #99
B3FD: 2611       bne      $b410
B3FF: ecf81f     ldd      [31, s]
B402: e7e815     stb      21, s
B405: ece81f     ldd      31, s
B408: c30002     addd     #2
B40B: ede81f     std      31, s
B40E: 206d       bra      $b47d
B410: c164       cmpb     #100
B412: 2618       bne      $b42c
B414: 1f41       tfr      s, x
B416: 3410       pshs     x
B418: 3506       puls     a, b
B41A: ede816     std      22, s
B41D: ecf81f     ldd      [31, s]
B420: 3406       pshs     a, b
B422: ece818     ldd      24, s
B425: bdbacb     jsr      $bacb
B428: 3262       leas     2, s
B42A: 202b       bra      $b457
B42C: c168       cmpb     #104
B42E: 261d       bne      $b44d
B430: 1f41       tfr      s, x
B432: 3410       pshs     x
B434: 3506       puls     a, b
B436: ede816     std      22, s
B439: 3406       pshs     a, b
B43B: cc0001     ldd      #1
B43E: 3406       pshs     a, b
B440: ece823     ldd      35, s
B443: c30001     addd     #1
B446: bdb8e6     jsr      $b8e6
B449: 3264       leas     4, s
B44B: 200a       bra      $b457
B44D: c173       cmpb     #115
B44F: 262c       bne      $b47d
B451: ecf81f     ldd      [31, s]
B454: ede816     std      22, s
B457: ece81f     ldd      31, s
B45A: c30002     addd     #2
B45D: ede81f     std      31, s
B460: aee816     ldx      22, s
B463: e684       ldb      , x
B465: 272c       beq      $b493
B467: 4f         clra     
B468: 3406       pshs     a, b
B46A: ece81d     ldd      29, s
B46D: bdb2fd     jsr      $b2fd
B470: 3262       leas     2, s
B472: ece816     ldd      22, s
B475: c30001     addd     #1
B478: ede816     std      22, s
B47B: 20e3       bra      $b460
B47D: 4f         clra     
B47E: e6e815     ldb      21, s
B481: 3406       pshs     a, b
B483: ece81d     ldd      29, s
B486: bdb2fd     jsr      $b2fd
B489: 3262       leas     2, s
B48B: 2006       bra      $b493
B48D: ece81b     ldd      27, s
B490: bdb324     jsr      $b324
B493: ece818     ldd      24, s
B496: c30001     addd     #1
B499: ede818     std      24, s
B49C: 7eb3ce     jmp      $b3ce
B49F: 32e81d     leas     29, s
B4A2: 39         rts      
B4A3: 3406       pshs     a, b
B4A5: ece4       ldd      , s
B4A7: bdc7f7     jsr      $c7f7
B4AA: aee4       ldx      , s
B4AC: 4f         clra     
B4AD: e69804     ldb      [4, x]
B4B0: e703       stb      3, x
B4B2: 2661       bne      $b515
B4B4: ec64       ldd      4, s
B4B6: 3406       pshs     a, b
B4B8: ec62       ldd      2, s
B4BA: bdc846     jsr      $c846
B4BD: 3262       leas     2, s
B4BF: aee4       ldx      , s
B4C1: 4f         clra     
B4C2: e69804     ldb      [4, x]
B4C5: e703       stb      3, x
B4C7: 204c       bra      $b515
B4C9: 3406       pshs     a, b
B4CB: aee4       ldx      , s
B4CD: e603       ldb      3, x
B4CF: c101       cmpb     #1
B4D1: 2008       bra      $b4db
B4D3: 3406       pshs     a, b
B4D5: aee4       ldx      , s
B4D7: e603       ldb      3, x
B4D9: c102       cmpb     #2
B4DB: 2603       bne      $b4e0
B4DD: 86ff       lda      #-1
B4DF: 214f       brn      $b530
B4E1: 1f89       tfr      a, b
B4E3: 2030       bra      $b515
B4E5: 3406       pshs     a, b
B4E7: 327f       leas     -1, s
B4E9: ec61       ldd      1, s
B4EB: 2607       bne      $b4f4
B4ED: 4f         clra     
B4EE: d66a       ldb      $6a
B4F0: e7e4       stb      , s
B4F2: 2016       bra      $b50a
B4F4: bdc9d6     jsr      $c9d6
B4F7: ae61       ldx      1, s
B4F9: e703       stb      3, x
B4FB: c102       cmpb     #2
B4FD: 2704       beq      $b503
B4FF: c103       cmpb     #3
B501: 2605       bne      $b508
B503: 4f         clra     
B504: e7e4       stb      , s
B506: 2002       bra      $b50a
B508: 6fe4       clr      , s
B50A: 4f         clra     
B50B: e6e4       ldb      , s
B50D: 3263       leas     3, s
B50F: 39         rts      
B510: 3406       pshs     a, b
B512: cc0300     ldd      #768
B515: 3262       leas     2, s
B517: 39         rts      
B518: 3406       pshs     a, b
B51A: 32e8d5     leas     -43, s
B51D: bdd412     jsr      $d412
B520: ede4       std      , s
B522: 10270093   lbeq     $b5b9
B526: 1f41       tfr      s, x
B528: c602       ldb      #2
B52A: 3a         abx      
B52B: 3410       pshs     x
B52D: ece82d     ldd      45, s
B530: bdb7ff     jsr      $b7ff
B533: 3262       leas     2, s
B535: 1f41       tfr      s, x
B537: c602       ldb      #2
B539: 3a         abx      
B53A: 3410       pshs     x
B53C: ec62       ldd      2, s
B53E: bdc8c9     jsr      $c8c9
B541: 206f       bra      $b5b2
B543: 3406       pshs     a, b
B545: 32e8ac     leas     -84, s
B548: bdd412     jsr      $d412
B54B: ede4       std      , s
B54D: 2738       beq      $b587
B54F: 1f41       tfr      s, x
B551: c602       ldb      #2
B553: 3a         abx      
B554: 3410       pshs     x
B556: ece856     ldd      86, s
B559: bdb7ff     jsr      $b7ff
B55C: 3262       leas     2, s
B55E: 1f41       tfr      s, x
B560: c62b       ldb      #43
B562: 3a         abx      
B563: 3410       pshs     x
B565: ece85a     ldd      90, s
B568: bdb7ff     jsr      $b7ff
B56B: 3262       leas     2, s
B56D: 1f41       tfr      s, x
B56F: c62b       ldb      #43
B571: 3a         abx      
B572: 3410       pshs     x
B574: 1f41       tfr      s, x
B576: c604       ldb      #4
B578: 3a         abx      
B579: 3410       pshs     x
B57B: ec64       ldd      4, s
B57D: bdc90a     jsr      $c90a
B580: 3264       leas     4, s
B582: ece4       ldd      , s
B584: bdd483     jsr      $d483
B587: 32e856     leas     86, s
B58A: 39         rts      
B58B: 3406       pshs     a, b
B58D: 32e8d5     leas     -43, s
B590: bdd412     jsr      $d412
B593: ede4       std      , s
B595: 2722       beq      $b5b9
B597: 1f41       tfr      s, x
B599: c602       ldb      #2
B59B: 3a         abx      
B59C: 3410       pshs     x
B59E: ece82d     ldd      45, s
B5A1: bdb7ff     jsr      $b7ff
B5A4: 3262       leas     2, s
B5A6: 1f41       tfr      s, x
B5A8: c602       ldb      #2
B5AA: 3a         abx      
B5AB: 3410       pshs     x
B5AD: ec62       ldd      2, s
B5AF: bdc963     jsr      $c963
B5B2: 3262       leas     2, s
B5B4: ece4       ldd      , s
B5B6: bdd483     jsr      $d483
B5B9: 32e82d     leas     45, s
B5BC: 39         rts      
B5BD: 3406       pshs     a, b
B5BF: aee4       ldx      , s
B5C1: ae04       ldx      4, x
B5C3: 3410       pshs     x
B5C5: ec66       ldd      6, s
B5C7: 3510       puls     x
B5C9: ed07       std      7, x
B5CB: 3262       leas     2, s
B5CD: 39         rts      
B5CE: 52         fcb      $52
B5CF: 0057       neg      $57
B5D1: 0034       neg      $34
B5D3: 0632       ror      $32
B5D5: e8d5       eorb     [b, u]
B5D7: bdd412     jsr      $d412
B5DA: ede4       std      , s
B5DC: d66a       ldb      $6a
B5DE: 262a       bne      $b60a
B5E0: 1f41       tfr      s, x
B5E2: c602       ldb      #2
B5E4: 3a         abx      
B5E5: 3410       pshs     x
B5E7: ece82d     ldd      45, s
B5EA: bdb7ff     jsr      $b7ff
B5ED: 3262       leas     2, s
B5EF: 1f41       tfr      s, x
B5F1: c602       ldb      #2
B5F3: 3a         abx      
B5F4: 3410       pshs     x
B5F6: ec62       ldd      2, s
B5F8: bdca99     jsr      $ca99
B5FB: 3262       leas     2, s
B5FD: d66a       ldb      $6a
B5FF: 2709       beq      $b60a
B601: ece4       ldd      , s
B603: bdd483     jsr      $d483
B606: 4f         clra     
B607: 5f         clrb     
B608: ede4       std      , s
B60A: ece4       ldd      , s
B60C: 32e82d     leas     45, s
B60F: 39         rts      
B610: 3406       pshs     a, b
B612: ec64       ldd      4, s
B614: 3406       pshs     a, b
B616: ec62       ldd      2, s
B618: bdcaea     jsr      $caea
B61B: 3262       leas     2, s
B61D: aee4       ldx      , s
B61F: 4f         clra     
B620: e69804     ldb      [4, x]
B623: e703       stb      3, x
B625: 200c       bra      $b633
B627: 3406       pshs     a, b
B629: ece4       ldd      , s
B62B: bdcb98     jsr      $cb98
B62E: ece4       ldd      , s
B630: bdd483     jsr      $d483
B633: 3262       leas     2, s
B635: 39         rts      
B636: 0f6a       clr      $6a
B638: 8e0300     ldx      #768
B63B: 6f84       clr      , x
B63D: 39         rts      
B63E: c602       ldb      #2
B640: d76a       stb      $6a
B642: cc0300     ldd      #768
B645: 3406       pshs     a, b
B647: ccb666     ldd      #-18842
B64A: bdb7ff     jsr      $b7ff
B64D: 3262       leas     2, s
B64F: 39         rts      
B650: ccb66a     ldd      #-18838
B653: 3406       pshs     a, b
B655: c603       ldb      #3
B657: d76a       stb      $6a
B659: cc0300     ldd      #768
B65C: 3406       pshs     a, b
B65E: ec62       ldd      2, s
B660: bdb7ff     jsr      $b7ff
B663: 3264       leas     4, s
B665: 39         rts      
B666: 65         fcb      $65
B667: 6f66       clr      6, s
B669: 0049       neg      $49
B66B: 2f4f       ble      $b6bc
B66D: 2074       bra      $b6e3
B66F: 696d       rol      13, s
B671: 65         fcb      $65
B672: 2d6f       blt      $b6e3
B674: 75         fcb      $75
B675: 740032     lsr      >$0032
B678: 61         fcb      $61
B679: 301f       leax     -1, x
B67B: 26fa       bne      $b677
B67D: 2069       bra      $b6e8
B67F: 3261       leas     1, s
B681: 301f       leax     -1, x
B683: 26fa       bne      $b67f
B685: 2059       bra      $b6e0
B687: 8d68       bsr      $b6f1
B689: 1f41       tfr      s, x
B68B: e601       ldb      1, x
B68D: c401       andb     #1
B68F: e701       stb      1, x
B691: 5f         clrb     
B692: 6402       lsr      2, x
B694: 6603       ror      3, x
B696: 2404       bcc      $b69c
B698: e306       addd     6, x
B69A: 8d1b       bsr      $b6b7
B69C: 6d02       tst      2, x
B69E: 2604       bne      $b6a4
B6A0: 6d03       tst      3, x
B6A2: 2708       beq      $b6ac
B6A4: 6807       lsl      7, x
B6A6: 6906       rol      6, x
B6A8: 8d0d       bsr      $b6b7
B6AA: 20e6       bra      $b692
B6AC: 6601       ror      1, x
B6AE: 2403       bcc      $b6b3
B6B0: bdb742     jsr      $b742
B6B3: 6601       ror      1, x
B6B5: 2025       bra      $b6dc
B6B7: 2a0a       bpl      $b6c3
B6B9: 3404       pshs     b
B6BB: e601       ldb      1, x
B6BD: ca02       orb      #2
B6BF: e701       stb      1, x
B6C1: 3504       puls     b
B6C3: 39         rts      
B6C4: 2504       bcs      $b6ca
B6C6: 4f         clra     
B6C7: 5f         clrb     
B6C8: 2003       bra      $b6cd
B6CA: ccffff     ldd      #-1
B6CD: 39         rts      
B6CE: 8d21       bsr      $b6f1
B6D0: 8d47       bsr      $b719
B6D2: 1f41       tfr      s, x
B6D4: ec06       ldd      6, x
B6D6: 2004       bra      $b6dc
B6D8: 8d17       bsr      $b6f1
B6DA: 8d3d       bsr      $b719
B6DC: 3510       puls     x
B6DE: 3510       puls     x
B6E0: 3510       puls     x
B6E2: 3261       leas     1, s
B6E4: 3261       leas     1, s
B6E6: 3410       pshs     x
B6E8: 3410       pshs     x
B6EA: 1f41       tfr      s, x
B6EC: ed84       std      , x
B6EE: 3510       puls     x
B6F0: 39         rts      
B6F1: 3510       puls     x
B6F3: 3404       pshs     b
B6F5: 3402       pshs     a
B6F7: cc0010     ldd      #16
B6FA: 3402       pshs     a
B6FC: 3404       pshs     b
B6FE: 3410       pshs     x
B700: 1f41       tfr      s, x
B702: ec04       ldd      4, x
B704: 2a06       bpl      $b70c
B706: 8d3a       bsr      $b742
B708: ed04       std      4, x
B70A: 6c03       inc      3, x
B70C: ec08       ldd      8, x
B70E: 2a06       bpl      $b716
B710: 8d30       bsr      $b742
B712: ed08       std      8, x
B714: 6c03       inc      3, x
B716: 4f         clra     
B717: 5f         clrb     
B718: 39         rts      
B719: 6809       lsl      9, x
B71B: 6908       rol      8, x
B71D: 59         rolb     
B71E: 49         rola     
B71F: a304       subd     4, x
B721: 2a04       bpl      $b727
B723: e304       addd     4, x
B725: 2006       bra      $b72d
B727: 6c09       inc      9, x
B729: 2602       bne      $b72d
B72B: 6c08       inc      8, x
B72D: 6a02       dec      2, x
B72F: 26e8       bne      $b719
B731: 6603       ror      3, x
B733: 240c       bcc      $b741
B735: 8d0b       bsr      $b742
B737: ed04       std      4, x
B739: ec08       ldd      8, x
B73B: 8d05       bsr      $b742
B73D: ed08       std      8, x
B73F: ec04       ldd      4, x
B741: 39         rts      
B742: 43         coma     
B743: 53         comb     
B744: c30001     addd     #1
B747: 39         rts      
B748: 8df8       bsr      $b742
B74A: 1f41       tfr      s, x
B74C: 4d         tsta     
B74D: 2a10       bpl      $b75f
B74F: 8df1       bsr      $b742
B751: 8d21       bsr      $b774
B753: 5d         tstb     
B754: 2707       beq      $b75d
B756: 6402       lsr      2, x
B758: 6603       ror      3, x
B75A: 5a         decb     
B75B: 20f6       bra      $b753
B75D: 200c       bra      $b76b
B75F: 8d13       bsr      $b774
B761: 5d         tstb     
B762: 2707       beq      $b76b
B764: 6803       lsl      3, x
B766: 6902       rol      2, x
B768: 5a         decb     
B769: 20f6       bra      $b761
B76B: 3510       puls     x
B76D: 3502       puls     a
B76F: 3504       puls     b
B771: 7eb6e6     jmp      $b6e6
B774: 4d         tsta     
B775: 2606       bne      $b77d
B777: c10f       cmpb     #15
B779: 2202       bhi      $b77d
B77B: 2004       bra      $b781
B77D: 4f         clra     
B77E: 5f         clrb     
B77F: ed04       std      4, x
B781: 39         rts      
B782: 3406       pshs     a, b
B784: 327f       leas     -1, s
B786: e6f801     ldb      [1, s]
B789: e1f805     cmpb     [5, s]
B78C: 2615       bne      $b7a3
B78E: e6f801     ldb      [1, s]
B791: 2714       beq      $b7a7
B793: ec61       ldd      1, s
B795: c30001     addd     #1
B798: ed61       std      1, s
B79A: ec65       ldd      5, s
B79C: c30001     addd     #1
B79F: ed65       std      5, s
B7A1: 20e3       bra      $b786
B7A3: 6fe4       clr      , s
B7A5: 2004       bra      $b7ab
B7A7: c6ff       ldb      #-1
B7A9: e7e4       stb      , s
B7AB: 4f         clra     
B7AC: e6e4       ldb      , s
B7AE: 3263       leas     3, s
B7B0: 39         rts      
B7B1: 3406       pshs     a, b
B7B3: 327e       leas     -2, s
B7B5: 4f         clra     
B7B6: 5f         clrb     
B7B7: ede4       std      , s
B7B9: e6f802     ldb      [2, s]
B7BC: 10270202   lbeq     $b9c2
B7C0: ec62       ldd      2, s
B7C2: c30001     addd     #1
B7C5: ed62       std      2, s
B7C7: ece4       ldd      , s
B7C9: c30001     addd     #1
B7CC: 20e9       bra      $b7b7
B7CE: 3406       pshs     a, b
B7D0: ec66       ldd      6, s
B7D2: 271e       beq      $b7f2
B7D4: e6f4       ldb      [, s]
B7D6: e1f804     cmpb     [4, s]
B7D9: 2617       bne      $b7f2
B7DB: ece4       ldd      , s
B7DD: c30001     addd     #1
B7E0: ede4       std      , s
B7E2: ec64       ldd      4, s
B7E4: c30001     addd     #1
B7E7: ed64       std      4, s
B7E9: ec66       ldd      6, s
B7EB: c3ffff     addd     #-1
B7EE: ed66       std      6, s
B7F0: 20de       bra      $b7d0
B7F2: ec66       ldd      6, s
B7F4: 2603       bne      $b7f9
B7F6: 86ff       lda      #-1
B7F8: 214f       brn      $b849
B7FA: 1f89       tfr      a, b
B7FC: 3262       leas     2, s
B7FE: 39         rts      
B7FF: 3406       pshs     a, b
B801: ece4       ldd      , s
B803: 8dac       bsr      $b7b1
B805: c30001     addd     #1
B808: 201a       bra      $b824
B80A: 3406       pshs     a, b
B80C: ec64       ldd      4, s
B80E: 3406       pshs     a, b
B810: ec62       ldd      2, s
B812: 8d9d       bsr      $b7b1
B814: e3e4       addd     , s
B816: 3510       puls     x
B818: 3406       pshs     a, b
B81A: ec66       ldd      6, s
B81C: 8de1       bsr      $b7ff
B81E: 3262       leas     2, s
B820: ece4       ldd      , s
B822: 8d8d       bsr      $b7b1
B824: 3406       pshs     a, b
B826: ec66       ldd      6, s
B828: 3406       pshs     a, b
B82A: ec64       ldd      4, s
B82C: bdbb68     jsr      $bb68
B82F: 3266       leas     6, s
B831: 39         rts      
B832: 3406       pshs     a, b
B834: ec64       ldd      4, s
B836: 3406       pshs     a, b
B838: ec66       ldd      6, s
B83A: bdb7b1     jsr      $b7b1
B83D: e3e4       addd     , s
B83F: 3510       puls     x
B841: 3406       pshs     a, b
B843: ec62       ldd      2, s
B845: 8db8       bsr      $b7ff
B847: 7eb9c4     jmp      $b9c4
B84A: 3406       pshs     a, b
B84C: 327e       leas     -2, s
B84E: 4f         clra     
B84F: 5f         clrb     
B850: ede4       std      , s
B852: ec66       ldd      6, s
B854: 1027016a   lbeq     $b9c2
B858: c3ffff     addd     #-1
B85B: ed66       std      6, s
B85D: ece4       ldd      , s
B85F: 3406       pshs     a, b
B861: cc000a     ldd      #10
B864: bdb687     jsr      $b687
B867: 3406       pshs     a, b
B869: 4f         clra     
B86A: e6f804     ldb      [4, s]
B86D: c40f       andb     #15
B86F: e3e4       addd     , s
B871: 3510       puls     x
B873: ede4       std      , s
B875: ec62       ldd      2, s
B877: c30001     addd     #1
B87A: ed62       std      2, s
B87C: 20d4       bra      $b852
B87E: 3406       pshs     a, b
B880: 327c       leas     -4, s
B882: 4f         clra     
B883: 5f         clrb     
B884: ed62       std      2, s
B886: ede4       std      , s
B888: e6f804     ldb      [4, s]
B88B: c12b       cmpb     #43
B88D: 2609       bne      $b898
B88F: ec64       ldd      4, s
B891: c30001     addd     #1
B894: ed64       std      4, s
B896: 2010       bra      $b8a8
B898: c12d       cmpb     #45
B89A: 260c       bne      $b8a8
B89C: ec64       ldd      4, s
B89E: c30001     addd     #1
B8A1: ed64       std      4, s
B8A3: ccffff     ldd      #-1
B8A6: ed62       std      2, s
B8A8: ae64       ldx      4, s
B8AA: e684       ldb      , x
B8AC: c130       cmpb     #48
B8AE: 2526       bcs      $b8d6
B8B0: c139       cmpb     #57
B8B2: 2222       bhi      $b8d6
B8B4: ece4       ldd      , s
B8B6: 3406       pshs     a, b
B8B8: cc000a     ldd      #10
B8BB: bdb687     jsr      $b687
B8BE: 3406       pshs     a, b
B8C0: 4f         clra     
B8C1: e6f806     ldb      [6, s]
B8C4: c3ffd0     addd     #-48
B8C7: e3e4       addd     , s
B8C9: 3510       puls     x
B8CB: ede4       std      , s
B8CD: ec64       ldd      4, s
B8CF: c30001     addd     #1
B8D2: ed64       std      4, s
B8D4: 20d2       bra      $b8a8
B8D6: ec62       ldd      2, s
B8D8: 2707       beq      $b8e1
B8DA: ece4       ldd      , s
B8DC: bdb742     jsr      $b742
B8DF: ede4       std      , s
B8E1: ece4       ldd      , s
B8E3: 3266       leas     6, s
B8E5: 39         rts      
B8E6: 3406       pshs     a, b
B8E8: 327d       leas     -3, s
B8EA: ccb9c7     ldd      #-17977
B8ED: ede4       std      , s
B8EF: ec67       ldd      7, s
B8F1: 274d       beq      $b940
B8F3: c3ffff     addd     #-1
B8F6: ed67       std      7, s
B8F8: 4f         clra     
B8F9: e6f803     ldb      [3, s]
B8FC: e762       stb      2, s
B8FE: ec63       ldd      3, s
B900: c30001     addd     #1
B903: ed63       std      3, s
B905: 4f         clra     
B906: e662       ldb      2, s
B908: 44         lsra     
B909: 56         rorb     
B90A: 44         lsra     
B90B: 56         rorb     
B90C: 44         lsra     
B90D: 56         rorb     
B90E: 44         lsra     
B90F: 56         rorb     
B910: 4f         clra     
B911: c40f       andb     #15
B913: e3e4       addd     , s
B915: 3406       pshs     a, b
B917: 4f         clra     
B918: 3510       puls     x
B91A: e684       ldb      , x
B91C: e7f809     stb      [9, s]
B91F: ec69       ldd      9, s
B921: c30001     addd     #1
B924: ed69       std      9, s
B926: 4f         clra     
B927: e662       ldb      2, s
B929: c40f       andb     #15
B92B: e3e4       addd     , s
B92D: 3406       pshs     a, b
B92F: 4f         clra     
B930: 3510       puls     x
B932: e684       ldb      , x
B934: e7f809     stb      [9, s]
B937: ec69       ldd      9, s
B939: c30001     addd     #1
B93C: ed69       std      9, s
B93E: 20af       bra      $b8ef
B940: 6ff809     clr      [9, s]
B943: 3265       leas     5, s
B945: 39         rts      
B946: 3406       pshs     a, b
B948: 327e       leas     -2, s
B94A: 4f         clra     
B94B: 5f         clrb     
B94C: ede4       std      , s
B94E: ec62       ldd      2, s
B950: bdb7b1     jsr      $b7b1
B953: 3406       pshs     a, b
B955: cc0002     ldd      #2
B958: bdb6d8     jsr      $b6d8
B95B: 271f       beq      $b97c
B95D: 4f         clra     
B95E: e6f802     ldb      [2, s]
B961: bdba58     jsr      $ba58
B964: e7f806     stb      [6, s]
B967: ec66       ldd      6, s
B969: c30001     addd     #1
B96C: ed66       std      6, s
B96E: ece4       ldd      , s
B970: c30001     addd     #1
B973: ede4       std      , s
B975: ec62       ldd      2, s
B977: c30001     addd     #1
B97A: ed62       std      2, s
B97C: ae62       ldx      2, s
B97E: e684       ldb      , x
B980: 2740       beq      $b9c2
B982: 4f         clra     
B983: bdba58     jsr      $ba58
B986: 58         lslb     
B987: 49         rola     
B988: 58         lslb     
B989: 49         rola     
B98A: 58         lslb     
B98B: 49         rola     
B98C: 58         lslb     
B98D: 49         rola     
B98E: e7f806     stb      [6, s]
B991: ec62       ldd      2, s
B993: c30001     addd     #1
B996: ed62       std      2, s
B998: 4f         clra     
B999: e6f806     ldb      [6, s]
B99C: 3406       pshs     a, b
B99E: e6f804     ldb      [4, s]
B9A1: bdba58     jsr      $ba58
B9A4: e3e4       addd     , s
B9A6: 3510       puls     x
B9A8: e7f806     stb      [6, s]
B9AB: ec62       ldd      2, s
B9AD: c30001     addd     #1
B9B0: ed62       std      2, s
B9B2: ec66       ldd      6, s
B9B4: c30001     addd     #1
B9B7: ed66       std      6, s
B9B9: ece4       ldd      , s
B9BB: c30001     addd     #1
B9BE: ede4       std      , s
B9C0: 20ba       bra      $b97c
B9C2: ece4       ldd      , s
B9C4: 3264       leas     4, s
B9C6: 39         rts      
B9C7: 3031       leax     -15, y
B9C9: 3233       leas     -13, y
B9CB: 3435       pshs     cc, b, x, y
B9CD: 3637       pshu     cc, a, b, x, y
B9CF: 38         fcb      $38
B9D0: 39         rts      
B9D1: 61         fcb      $61
B9D2: 62         fcb      $62
B9D3: 6364       com      4, s
B9D5: 65         fcb      $65
B9D6: 6600       ror      0, x
B9D8: 3406       pshs     a, b
B9DA: 4f         clra     
B9DB: e661       ldb      1, s
B9DD: 8d0f       bsr      $b9ee
B9DF: 3406       pshs     a, b
B9E1: 4f         clra     
B9E2: e663       ldb      3, s
B9E4: 8d14       bsr      $b9fa
B9E6: aae4       ora      , s
B9E8: ea61       orb      1, s
B9EA: 3510       puls     x
B9EC: 203c       bra      $ba2a
B9EE: 3406       pshs     a, b
B9F0: e661       ldb      1, s
B9F2: c161       cmpb     #97
B9F4: 2531       bcs      $ba27
B9F6: c17a       cmpb     #122
B9F8: 2016       bra      $ba10
B9FA: 3406       pshs     a, b
B9FC: e661       ldb      1, s
B9FE: c141       cmpb     #65
BA00: 2525       bcs      $ba27
BA02: c15a       cmpb     #90
BA04: 200a       bra      $ba10
BA06: 3406       pshs     a, b
BA08: e661       ldb      1, s
BA0A: c130       cmpb     #48
BA0C: 2519       bcs      $ba27
BA0E: c139       cmpb     #57
BA10: 2215       bhi      $ba27
BA12: 2010       bra      $ba24
BA14: 3406       pshs     a, b
BA16: 4f         clra     
BA17: e661       ldb      1, s
BA19: 8dbd       bsr      $b9d8
BA1B: 260a       bne      $ba27
BA1D: 4f         clra     
BA1E: e661       ldb      1, s
BA20: 8de4       bsr      $ba06
BA22: 2603       bne      $ba27
BA24: 86ff       lda      #-1
BA26: 214f       brn      $ba77
BA28: 1f89       tfr      a, b
BA2A: 7ebac8     jmp      $bac8
BA2D: 3406       pshs     a, b
BA2F: 327f       leas     -1, s
BA31: c6ff       ldb      #-1
BA33: e7e4       stb      , s
BA35: 4f         clra     
BA36: e662       ldb      2, s
BA38: 8dcc       bsr      $ba06
BA3A: 2616       bne      $ba52
BA3C: 4f         clra     
BA3D: e662       ldb      2, s
BA3F: 8d73       bsr      $bab4
BA41: 830061     subd     #97
BA44: 2d0a       blt      $ba50
BA46: 4f         clra     
BA47: e662       ldb      2, s
BA49: 8d69       bsr      $bab4
BA4B: 830066     subd     #102
BA4E: 2f02       ble      $ba52
BA50: 6fe4       clr      , s
BA52: 4f         clra     
BA53: e6e4       ldb      , s
BA55: 3263       leas     3, s
BA57: 39         rts      
BA58: 3406       pshs     a, b
BA5A: 4f         clra     
BA5B: e661       ldb      1, s
BA5D: 8dce       bsr      $ba2d
BA5F: 2764       beq      $bac5
BA61: 4f         clra     
BA62: e661       ldb      1, s
BA64: 8da0       bsr      $ba06
BA66: 2608       bne      $ba70
BA68: 4f         clra     
BA69: e661       ldb      1, s
BA6B: c30009     addd     #9
BA6E: e761       stb      1, s
BA70: 4f         clra     
BA71: e661       ldb      1, s
BA73: c40f       andb     #15
BA75: 204c       bra      $bac3
BA77: 3406       pshs     a, b
BA79: aee4       ldx      , s
BA7B: e684       ldb      , x
BA7D: 2749       beq      $bac8
BA7F: 4f         clra     
BA80: 8d0b       bsr      $ba8d
BA82: e7f4       stb      [, s]
BA84: ece4       ldd      , s
BA86: c30001     addd     #1
BA89: ede4       std      , s
BA8B: 20ec       bra      $ba79
BA8D: 3406       pshs     a, b
BA8F: 4f         clra     
BA90: e661       ldb      1, s
BA92: bdb9ee     jsr      $b9ee
BA95: 272e       beq      $bac5
BA97: 4f         clra     
BA98: e661       ldb      1, s
BA9A: c4df       andb     #-33
BA9C: 2025       bra      $bac3
BA9E: 3406       pshs     a, b
BAA0: aee4       ldx      , s
BAA2: e684       ldb      , x
BAA4: 2722       beq      $bac8
BAA6: 4f         clra     
BAA7: 8d0b       bsr      $bab4
BAA9: e7f4       stb      [, s]
BAAB: ece4       ldd      , s
BAAD: c30001     addd     #1
BAB0: ede4       std      , s
BAB2: 20ec       bra      $baa0
BAB4: 3406       pshs     a, b
BAB6: 4f         clra     
BAB7: e661       ldb      1, s
BAB9: bdb9fa     jsr      $b9fa
BABC: 2707       beq      $bac5
BABE: 4f         clra     
BABF: e661       ldb      1, s
BAC1: ca20       orb      #32
BAC3: e761       stb      1, s
BAC5: 4f         clra     
BAC6: e661       ldb      1, s
BAC8: 3262       leas     2, s
BACA: 39         rts      
BACB: 3406       pshs     a, b
BACD: 327c       leas     -4, s
BACF: ec68       ldd      8, s
BAD1: 2c15       bge      $bae8
BAD3: c62d       ldb      #45
BAD5: e7f804     stb      [4, s]
BAD8: ec64       ldd      4, s
BADA: c30001     addd     #1
BADD: ed64       std      4, s
BADF: ec68       ldd      8, s
BAE1: bdb742     jsr      $b742
BAE4: ed68       std      8, s
BAE6: 2010       bra      $baf8
BAE8: ec68       ldd      8, s
BAEA: 260c       bne      $baf8
BAEC: c630       ldb      #48
BAEE: e7f804     stb      [4, s]
BAF1: ec64       ldd      4, s
BAF3: c30001     addd     #1
BAF6: ed64       std      4, s
BAF8: ccb11a     ldd      #-20198
BAFB: ede4       std      , s
BAFD: c630       ldb      #48
BAFF: e763       stb      3, s
BB01: c630       ldb      #48
BB03: e762       stb      2, s
BB05: ec68       ldd      8, s
BB07: aee4       ldx      , s
BB09: a384       subd     , x
BB0B: 2d0e       blt      $bb1b
BB0D: ec68       ldd      8, s
BB0F: a384       subd     , x
BB11: ed68       std      8, s
BB13: 4f         clra     
BB14: e662       ldb      2, s
BB16: c30001     addd     #1
BB19: 20e8       bra      $bb03
BB1B: 4f         clra     
BB1C: e663       ldb      3, s
BB1E: ea62       orb      2, s
BB20: e763       stb      3, s
BB22: c130       cmpb     #48
BB24: 270c       beq      $bb32
BB26: e662       ldb      2, s
BB28: e7f804     stb      [4, s]
BB2B: ec64       ldd      4, s
BB2D: c30001     addd     #1
BB30: ed64       std      4, s
BB32: ecf4       ldd      [, s]
BB34: 830001     subd     #1
BB37: 2709       beq      $bb42
BB39: ece4       ldd      , s
BB3B: c30002     addd     #2
BB3E: ede4       std      , s
BB40: 20bf       bra      $bb01
BB42: 6ff804     clr      [4, s]
BB45: 3266       leas     6, s
BB47: 39         rts      
BB48: 3406       pshs     a, b
BB4A: 327e       leas     -2, s
BB4C: ec66       ldd      6, s
BB4E: ede4       std      , s
BB50: ec62       ldd      2, s
BB52: 3406       pshs     a, b
BB54: cc0002     ldd      #2
BB57: 3406       pshs     a, b
BB59: 1f41       tfr      s, x
BB5B: c604       ldb      #4
BB5D: 3a         abx      
BB5E: 3410       pshs     x
BB60: 3506       puls     a, b
BB62: bdb8e6     jsr      $b8e6
BB65: 3268       leas     8, s
BB67: 39         rts      
BB68: 1f01       tfr      d, x
BB6A: 10ae62     ldy      2, s
BB6D: a362       subd     2, s
BB6F: 2307       bls      $bb78
BB71: ccbb97     ldd      #-17513
BB74: 3406       pshs     a, b
BB76: 200b       bra      $bb83
BB78: ec64       ldd      4, s
BB7A: 308b       leax     d, x
BB7C: 31ab       leay     d, y
BB7E: ccbb9f     ldd      #-17505
BB81: 3406       pshs     a, b
BB83: 6a66       dec      6, s
BB85: 2b03       bmi      $bb8a
BB87: 5f         clrb     
BB88: 2006       bra      $bb90
BB8A: e667       ldb      7, s
BB8C: 2706       beq      $bb94
BB8E: 6f67       clr      7, s
BB90: adf4       jsr      [, s]
BB92: 20ef       bra      $bb83
BB94: 3506       puls     a, b
BB96: 39         rts      
BB97: a680       lda      , x+
BB99: a7a0       sta      , y+
BB9B: 5a         decb     
BB9C: 26f9       bne      $bb97
BB9E: 39         rts      
BB9F: a682       lda      , -x
BBA1: a7a2       sta      , -y
BBA3: 5a         decb     
BBA4: 26f9       bne      $bb9f
BBA6: 39         rts      
BBA7: 1f02       tfr      d, y
BBA9: 6fe2       clr      , -s
BBAB: 6ce4       inc      , s
BBAD: ae63       ldx      3, s
BBAF: e666       ldb      6, s
BBB1: a680       lda      , x+
BBB3: 5d         tstb     
BBB4: 2601       bne      $bbb7
BBB6: 4f         clra     
BBB7: 8d2d       bsr      $bbe6
BBB9: 3402       pshs     a
BBBB: a6a4       lda      , y
BBBD: 8d27       bsr      $bbe6
BBBF: a1e0       cmpa     , s+
BBC1: 2608       bne      $bbcb
BBC3: 5d         tstb     
BBC4: 271c       beq      $bbe2
BBC6: 5a         decb     
BBC7: 3121       leay     1, y
BBC9: 20e6       bra      $bbb1
BBCB: 5d         tstb     
BBCC: 260a       bne      $bbd8
BBCE: a6a4       lda      , y
BBD0: 8141       cmpa     #65
BBD2: 2504       bcs      $bbd8
BBD4: 815a       cmpa     #90
BBD6: 230a       bls      $bbe2
BBD8: a6a0       lda      , y+
BBDA: 26fc       bne      $bbd8
BBDC: a6a4       lda      , y
BBDE: 26cb       bne      $bbab
BBE0: 6fe4       clr      , s
BBE2: 4f         clra     
BBE3: e6e0       ldb      , s+
BBE5: 39         rts      
BBE6: 8161       cmpa     #97
BBE8: 2506       bcs      $bbf0
BBEA: 817a       cmpa     #122
BBEC: 2202       bhi      $bbf0
BBEE: 845f       anda     #95
BBF0: 39         rts      
BBF1: 3530       puls     x, y
BBF3: 3406       pshs     a, b
BBF5: e680       ldb      , x+
BBF7: ae84       ldx      , x
BBF9: f10220     cmpb     $0220
BBFC: 271d       beq      $bc1b
BBFE: b60220     lda      $0220
BC01: 3622       pshu     a, y
BC03: f70220     stb      $0220
BC06: f7effc     stb      $effc
BC09: 3506       puls     a, b
BC0B: ad84       jsr      , x
BC0D: 3406       pshs     a, b
BC0F: 3724       pulu     b, y
BC11: f70220     stb      $0220
BC14: f7effc     stb      $effc
BC17: ece1       ldd      , s++
BC19: 6ea4       jmp      , y
BC1B: 3506       puls     a, b
BC1D: 3420       pshs     y
BC1F: 6e84       jmp      , x
BC21: ce02ff     ldu      #767
BC24: c600       ldb      #0
BC26: f70220     stb      $0220
BC29: f7effc     stb      $effc
BC2C: 39         rts      
BC2D: ae62       ldx      2, s
BC2F: 3406       pshs     a, b
BC31: 1f10       tfr      x, d
BC33: 54         lsrb     
BC34: 3a         abx      
BC35: 30890223   leax     547, x
BC39: f60220     ldb      $0220
BC3C: e780       stb      , x+
BC3E: 3506       puls     a, b
BC40: ed84       std      , x
BC42: ec62       ldd      2, s
BC44: 3406       pshs     a, b
BC46: c3bc67     addd     #-17305
BC49: bdff97     jsr      $ff97
BC4C: 3506       puls     a, b
BC4E: 39         rts      
BC4F: 3506       puls     a, b
BC51: 83bc69     subd     #-17303
BC54: 1f01       tfr      d, x
BC56: 54         lsrb     
BC57: 3a         abx      
BC58: 30890223   leax     547, x
BC5C: ccbc66     ldd      #-17306
BC5F: 3406       pshs     a, b
BC61: 3410       pshs     x
BC63: 7ebbf1     jmp      $bbf1
BC66: 39         rts      
BC67: 8de6       bsr      $bc4f
BC69: 8de4       bsr      $bc4f
BC6B: 8de2       bsr      $bc4f
BC6D: 8de0       bsr      $bc4f
BC6F: 8dde       bsr      $bc4f
BC71: 8ddc       bsr      $bc4f
BC73: 8dda       bsr      $bc4f
BC75: 3404       pshs     b
BC77: 3402       pshs     a
BC79: 327b       leas     -5, s
BC7B: dc2c       ldd      $2c
BC7D: ede4       std      , s
BC7F: 10df2c     sts      $2c
BC82: dc33       ldd      $33
BC84: ed62       std      2, s
BC86: df33       stu      $33
BC88: 4f         clra     
BC89: f60220     ldb      $0220
BC8C: e764       stb      4, s
BC8E: ae65       ldx      5, s
BC90: ad84       jsr      , x
BC92: ece4       ldd      , s
BC94: dd2c       std      $2c
BC96: ec62       ldd      2, s
BC98: dd33       std      $33
BC9A: 4f         clra     
BC9B: e664       ldb      4, s
BC9D: f70220     stb      $0220
BCA0: 4f         clra     
BCA1: e664       ldb      4, s
BCA3: f7effc     stb      $effc
BCA6: 4f         clra     
BCA7: 5f         clrb     
BCA8: 3267       leas     7, s
BCAA: 39         rts      
BCAB: 3404       pshs     b
BCAD: 3402       pshs     a
BCAF: 327b       leas     -5, s
BCB1: 10de2c     lds      $2c
BCB4: ece4       ldd      , s
BCB6: dd2c       std      $2c
BCB8: de33       ldu      $33
BCBA: ec62       ldd      2, s
BCBC: dd33       std      $33
BCBE: 4f         clra     
BCBF: e664       ldb      4, s
BCC1: f70220     stb      $0220
BCC4: 4f         clra     
BCC5: e664       ldb      4, s
BCC7: f7effc     stb      $effc
BCCA: cc0001     ldd      #1
BCCD: 3267       leas     7, s
BCCF: 39         rts      
BCD0: 3406       pshs     a, b
BCD2: 327e       leas     -2, s
BCD4: cc0014     ldd      #20
BCD7: bde76b     jsr      $e76b
BCDA: dc68       ldd      $68
BCDC: ede4       std      , s
BCDE: e6f4       ldb      [, s]
BCE0: 102700af   lbeq     $bd93
BCE4: cc00f0     ldd      #240
BCE7: 3406       pshs     a, b
BCE9: ec64       ldd      4, s
BCEB: bdc072     jsr      $c072
BCEE: 3262       leas     2, s
BCF0: ece4       ldd      , s
BCF2: c30001     addd     #1
BCF5: 3406       pshs     a, b
BCF7: 3510       puls     x
BCF9: e684       ldb      , x
BCFB: 270f       beq      $bd0c
BCFD: 4f         clra     
BCFE: e6f4       ldb      [, s]
BD00: bdbe6f     jsr      $be6f
BD03: ece4       ldd      , s
BD05: c30001     addd     #1
BD08: ede4       std      , s
BD0A: 20e4       bra      $bcf0
BD0C: 4f         clra     
BD0D: e6f4       ldb      [, s]
BD0F: 207f       bra      $bd90
BD11: 3406       pshs     a, b
BD13: cc0015     ldd      #21
BD16: bde76b     jsr      $e76b
BD19: aee4       ldx      , s
BD1B: e602       ldb      2, x
BD1D: c181       cmpb     #-127
BD1F: 2613       bne      $bd34
BD21: 4f         clra     
BD22: e60c       ldb      12, x
BD24: c440       andb     #64
BD26: 270c       beq      $bd34
BD28: cc00e0     ldd      #224
BD2B: 3406       pshs     a, b
BD2D: ec62       ldd      2, s
BD2F: bdc072     jsr      $c072
BD32: 3262       leas     2, s
BD34: ece4       ldd      , s
BD36: 9302       subd     $02
BD38: 10260130   lbne     $be6c
BD3C: d677       ldb      $77
BD3E: c140       cmpb     #64
BD40: 2606       bne      $bd48
BD42: bdc13c     jsr      $c13c
BD45: 7ebe6c     jmp      $be6c
BD48: c120       cmpb     #32
BD4A: 1026011e   lbne     $be6c
BD4E: 7ebe5a     jmp      $be5a
BD51: 3406       pshs     a, b
BD53: 327e       leas     -2, s
BD55: cc0016     ldd      #22
BD58: bde76b     jsr      $e76b
BD5B: dc68       ldd      $68
BD5D: ede4       std      , s
BD5F: cc00f0     ldd      #240
BD62: 3406       pshs     a, b
BD64: ec64       ldd      4, s
BD66: bdc072     jsr      $c072
BD69: 3262       leas     2, s
BD6B: 4f         clra     
BD6C: e6f4       ldb      [, s]
BD6E: bdbe6f     jsr      $be6f
BD71: e6f4       ldb      [, s]
BD73: c12c       cmpb     #44
BD75: 2709       beq      $bd80
BD77: ece4       ldd      , s
BD79: c30001     addd     #1
BD7C: ede4       std      , s
BD7E: 20eb       bra      $bd6b
BD80: cc004c     ldd      #76
BD83: bdbe6f     jsr      $be6f
BD86: cc002c     ldd      #44
BD89: bdbe6f     jsr      $be6f
BD8C: ae62       ldx      2, s
BD8E: ec05       ldd      5, x
BD90: bdbe53     jsr      $be53
BD93: 3264       leas     4, s
BD95: 39         rts      
BD96: 3406       pshs     a, b
BD98: c60f       ldb      #15
BD9A: aee4       ldx      , s
BD9C: e70a       stb      10, x
BD9E: cc0060     ldd      #96
BDA1: 3406       pshs     a, b
BDA3: ec62       ldd      2, s
BDA5: bdc072     jsr      $c072
BDA8: 3262       leas     2, s
BDAA: cc0053     ldd      #83
BDAD: bdbe6f     jsr      $be6f
BDB0: 9e68       ldx      $68
BDB2: e684       ldb      , x
BDB4: c12c       cmpb     #44
BDB6: 10270092   lbeq     $be4c
BDBA: 4f         clra     
BDBB: bdbe6f     jsr      $be6f
BDBE: 9e68       ldx      $68
BDC0: 3001       leax     1, x
BDC2: 9f68       stx      $68
BDC4: 20ea       bra      $bdb0
BDC6: 3406       pshs     a, b
BDC8: c60f       ldb      #15
BDCA: aee4       ldx      , s
BDCC: e70a       stb      10, x
BDCE: cc0060     ldd      #96
BDD1: 3406       pshs     a, b
BDD3: ec62       ldd      2, s
BDD5: bdc072     jsr      $c072
BDD8: 3262       leas     2, s
BDDA: cc0052     ldd      #82
BDDD: bdbe6f     jsr      $be6f
BDE0: 4f         clra     
BDE1: 9e68       ldx      $68
BDE3: e684       ldb      , x
BDE5: bdbe6f     jsr      $be6f
BDE8: 9e68       ldx      $68
BDEA: 3001       leax     1, x
BDEC: 9f68       stx      $68
BDEE: 4f         clra     
BDEF: e684       ldb      , x
BDF1: 8d7c       bsr      $be6f
BDF3: 9e68       ldx      $68
BDF5: 3001       leax     1, x
BDF7: 9f68       stx      $68
BDF9: ae64       ldx      4, s
BDFB: e684       ldb      , x
BDFD: 270c       beq      $be0b
BDFF: 4f         clra     
BE00: 8d6d       bsr      $be6f
BE02: ec64       ldd      4, s
BE04: c30001     addd     #1
BE07: ed64       std      4, s
BE09: 20ee       bra      $bdf9
BE0B: cc003d     ldd      #61
BE0E: 8d5f       bsr      $be6f
BE10: 9e68       ldx      $68
BE12: e684       ldb      , x
BE14: c12c       cmpb     #44
BE16: 2734       beq      $be4c
BE18: 4f         clra     
BE19: 8d54       bsr      $be6f
BE1B: 9e68       ldx      $68
BE1D: 3001       leax     1, x
BE1F: 9f68       stx      $68
BE21: 20ed       bra      $be10
BE23: 3406       pshs     a, b
BE25: c60f       ldb      #15
BE27: aee4       ldx      , s
BE29: e70a       stb      10, x
BE2B: cc0060     ldd      #96
BE2E: 3406       pshs     a, b
BE30: ec62       ldd      2, s
BE32: bdc072     jsr      $c072
BE35: 3262       leas     2, s
BE37: cc0049     ldd      #73
BE3A: 8d33       bsr      $be6f
BE3C: aee4       ldx      , s
BE3E: e60b       ldb      11, x
BE40: 2605       bne      $be47
BE42: cc0030     ldd      #48
BE45: 2003       bra      $be4a
BE47: cc0031     ldd      #49
BE4A: 8d23       bsr      $be6f
BE4C: cc000d     ldd      #13
BE4F: 8d02       bsr      $be53
BE51: 2019       bra      $be6c
BE53: 3406       pshs     a, b
BE55: 4f         clra     
BE56: e661       ldb      1, s
BE58: 8d05       bsr      $be5f
BE5A: bdc0d0     jsr      $c0d0
BE5D: 200d       bra      $be6c
BE5F: 3406       pshs     a, b
BE61: bdc176     jsr      $c176
BE64: 4f         clra     
BE65: e661       ldb      1, s
BE67: 8d06       bsr      $be6f
BE69: bdc17e     jsr      $c17e
BE6C: 3262       leas     2, s
BE6E: 39         rts      
BE6F: 3406       pshs     a, b
BE71: cc0017     ldd      #23
BE74: bde76b     jsr      $e76b
BE77: 9e02       ldx      $02
BE79: e684       ldb      , x
BE7B: 266d       bne      $beea
BE7D: 4f         clra     
BE7E: f6e823     ldb      $e823
BE81: ca08       orb      #8
BE83: f7e823     stb      $e823
BE86: f6e840     ldb      $e840
BE89: c441       andb     #65
BE8B: 830041     subd     #65
BE8E: 2608       bne      $be98
BE90: ccc1c8     ldd      #-15928
BE93: bdb653     jsr      $b653
BE96: 204c       bra      $bee4
BE98: ccffff     ldd      #-1
BE9B: e061       subb     1, s
BE9D: 8200       sbca     #0
BE9F: f7e822     stb      $e822
BEA2: 4f         clra     
BEA3: f6e840     ldb      $e840
BEA6: c440       andb     #64
BEA8: 27f8       beq      $bea2
BEAA: f6e823     ldb      $e823
BEAD: c4f7       andb     #-9
BEAF: f7e823     stb      $e823
BEB2: ec07       ldd      7, x
BEB4: dd06       std      $06
BEB6: 260a       bne      $bec2
BEB8: 4f         clra     
BEB9: f6e840     ldb      $e840
BEBC: c401       andb     #1
BEBE: 27f8       beq      $beb8
BEC0: 2028       bra      $beea
BEC2: cc0c80     ldd      #3200
BEC5: dd04       std      $04
BEC7: 4f         clra     
BEC8: f6e840     ldb      $e840
BECB: c401       andb     #1
BECD: 261b       bne      $beea
BECF: dc04       ldd      $04
BED1: c3ffff     addd     #-1
BED4: dd04       std      $04
BED6: 26ef       bne      $bec7
BED8: dc06       ldd      $06
BEDA: c3ffff     addd     #-1
BEDD: dd06       std      $06
BEDF: 26e1       bne      $bec2
BEE1: bdb650     jsr      $b650
BEE4: c603       ldb      #3
BEE6: 9e02       ldx      $02
BEE8: e784       stb      , x
BEEA: 4f         clra     
BEEB: f6e823     ldb      $e823
BEEE: ca08       orb      #8
BEF0: f7e823     stb      $e823
BEF3: c6ff       ldb      #-1
BEF5: f7e822     stb      $e822
BEF8: 3262       leas     2, s
BEFA: 39         rts      
BEFB: 327f       leas     -1, s
BEFD: cc0018     ldd      #24
BF00: bde76b     jsr      $e76b
BF03: 9e02       ldx      $02
BF05: e684       ldb      , x
BF07: 267c       bne      $bf85
BF09: 4f         clra     
BF0A: f6e821     ldb      $e821
BF0D: c4f7       andb     #-9
BF0F: f7e821     stb      $e821
BF12: f6e840     ldb      $e840
BF15: ca02       orb      #2
BF17: f7e840     stb      $e840
BF1A: ec07       ldd      7, x
BF1C: dd06       std      $06
BF1E: 260a       bne      $bf2a
BF20: 4f         clra     
BF21: f6e840     ldb      $e840
BF24: c480       andb     #-128
BF26: 2723       beq      $bf4b
BF28: 20f6       bra      $bf20
BF2A: cc0c80     ldd      #3200
BF2D: dd04       std      $04
BF2F: 4f         clra     
BF30: f6e840     ldb      $e840
BF33: c480       andb     #-128
BF35: 2714       beq      $bf4b
BF37: dc04       ldd      $04
BF39: c3ffff     addd     #-1
BF3C: dd04       std      $04
BF3E: 26ef       bne      $bf2f
BF40: dc06       ldd      $06
BF42: c3ffff     addd     #-1
BF45: dd06       std      $06
BF47: 2733       beq      $bf7c
BF49: 20df       bra      $bf2a
BF4B: f6e840     ldb      $e840
BF4E: c4fd       andb     #-3
BF50: f7e840     stb      $e840
BF53: f6e810     ldb      $e810
BF56: c440       andb     #64
BF58: 2606       bne      $bf60
BF5A: e60c       ldb      12, x
BF5C: ca01       orb      #1
BF5E: e70c       stb      12, x
BF60: ccffff     ldd      #-1
BF63: f0e820     subb     $e820
BF66: 8200       sbca     #0
BF68: e7e4       stb      , s
BF6A: 4f         clra     
BF6B: f6e821     ldb      $e821
BF6E: ca08       orb      #8
BF70: f7e821     stb      $e821
BF73: f6e840     ldb      $e840
BF76: c480       andb     #-128
BF78: 27f9       beq      $bf73
BF7A: 2009       bra      $bf85
BF7C: bdb650     jsr      $b650
BF7F: c603       ldb      #3
BF81: 9e02       ldx      $02
BF83: e784       stb      , x
BF85: 4f         clra     
BF86: f6e821     ldb      $e821
BF89: c4f7       andb     #-9
BF8B: f7e821     stb      $e821
BF8E: c6ff       ldb      #-1
BF90: f7e822     stb      $e822
BF93: e6e4       ldb      , s
BF95: 3261       leas     1, s
BF97: 39         rts      
BF98: 3406       pshs     a, b
BF9A: 32e8ef     leas     -17, s
BF9D: cc0019     ldd      #25
BFA0: bde76b     jsr      $e76b
BFA3: 4f         clra     
BFA4: aee811     ldx      17, s
BFA7: e609       ldb      9, x
BFA9: e769       stb      9, s
BFAB: ec07       ldd      7, x
BFAD: ed67       std      7, s
BFAF: 6fe4       clr      , s
BFB1: c60f       ldb      #15
BFB3: e76a       stb      10, s
BFB5: cc0060     ldd      #96
BFB8: 3406       pshs     a, b
BFBA: 1f41       tfr      s, x
BFBC: c602       ldb      #2
BFBE: 3a         abx      
BFBF: 3410       pshs     x
BFC1: 3506       puls     a, b
BFC3: bdc072     jsr      $c072
BFC6: 3262       leas     2, s
BFC8: cc0050     ldd      #80
BFCB: bdbe6f     jsr      $be6f
BFCE: 4f         clra     
BFCF: aee811     ldx      17, s
BFD2: e60a       ldb      10, x
BFD4: bdbe6f     jsr      $be6f
BFD7: ece815     ldd      21, s
BFDA: bdbe6f     jsr      $be6f
BFDD: ece815     ldd      21, s
BFE0: 1f89       tfr      a, b
BFE2: 4f         clra     
BFE3: bdbe6f     jsr      $be6f
BFE6: 4f         clra     
BFE7: 5f         clrb     
BFE8: bdbe53     jsr      $be53
BFEB: 32e813     leas     19, s
BFEE: 39         rts      
BFEF: 3406       pshs     a, b
BFF1: 32e8ed     leas     -19, s
BFF4: cc001a     ldd      #26
BFF7: bde76b     jsr      $e76b
BFFA: 4f         clra     
BFFB: aee813     ldx      19, s
BFFE: e609       ldb      9, x
