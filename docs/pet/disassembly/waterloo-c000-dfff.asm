; SuperPET Waterloo 6809 ROM $C000-$DFFF (970019-12) - disassembled from waterloo-c000-dfff.970019-12.bin
; Base address $C000, length 8192 ($2000) bytes
; Generated with capstone (CS_ARCH_M680X / CS_MODE_M680X_6809) - a real 6809
; disassembler, not hand-decoded. Labels/comments are NOT included here (raw
; mnemonic + operand listing only); see docs/pet/waterloo-investigation.md for
; the annotated, hand-verified routines this session actually traced through.

C000: e76b       stb      11, s
C002: ec07       ldd      7, x
C004: ed69       std      9, s
C006: 6f62       clr      2, s
C008: c60f       ldb      #15
C00A: e76c       stb      12, s
C00C: 1f41       tfr      s, x
C00E: c602       ldb      #2
C010: 3a         abx      
C011: 3410       pshs     x
C013: 3506       puls     a, b
C015: bdc0dd     jsr      $c0dd
C018: cc0300     ldd      #768
C01B: ede4       std      , s
C01D: bdbefb     jsr      $befb
C020: aee4       ldx      , s
C022: e784       stb      , x
C024: d66a       ldb      $6a
C026: 2643       bne      $c06b
C028: e684       ldb      , x
C02A: c10d       cmpb     #13
C02C: 2707       beq      $c035
C02E: ece4       ldd      , s
C030: c30001     addd     #1
C033: 20e6       bra      $c01b
C035: 6ff4       clr      [, s]
C037: bdc13c     jsr      $c13c
C03A: cc0002     ldd      #2
C03D: 3406       pshs     a, b
C03F: cc0300     ldd      #768
C042: 3406       pshs     a, b
C044: ccc1db     ldd      #-15909
C047: bdb7ce     jsr      $b7ce
C04A: 3264       leas     4, s
C04C: 261a       bne      $c068
C04E: cc0002     ldd      #2
C051: 3406       pshs     a, b
C053: cc0300     ldd      #768
C056: 3406       pshs     a, b
C058: ccc1de     ldd      #-15906
C05B: bdb7ce     jsr      $b7ce
C05E: 3264       leas     4, s
C060: 2606       bne      $c068
C062: c603       ldb      #3
C064: d76a       stb      $6a
C066: 2003       bra      $c06b
C068: bdb636     jsr      $b636
C06B: 4f         clra     
C06C: d66a       ldb      $6a
C06E: 32e815     leas     21, s
C071: 39         rts      
C072: 3406       pshs     a, b
C074: cc001b     ldd      #27
C077: bde76b     jsr      $e76b
C07A: ece4       ldd      , s
C07C: dd02       std      $02
C07E: d677       ldb      $77
C080: c120       cmpb     #32
C082: 261a       bne      $c09e
C084: 9e02       ldx      $02
C086: e609       ldb      9, x
C088: d179       cmpb     $79
C08A: 260e       bne      $c09a
C08C: e60a       ldb      10, x
C08E: d17a       cmpb     $7a
C090: 2608       bne      $c09a
C092: d678       ldb      $78
C094: e165       cmpb     5, s
C096: 1027009f   lbeq     $c139
C09A: 8d34       bsr      $c0d0
C09C: 2007       bra      $c0a5
C09E: c140       cmpb     #64
C0A0: 2603       bne      $c0a5
C0A2: bdc13c     jsr      $c13c
C0A5: c620       ldb      #32
C0A7: d777       stb      $77
C0A9: 4f         clra     
C0AA: e665       ldb      5, s
C0AC: d778       stb      $78
C0AE: 9e02       ldx      $02
C0B0: e609       ldb      9, x
C0B2: d779       stb      $79
C0B4: e60a       ldb      10, x
C0B6: d77a       stb      $7a
C0B8: bdc14f     jsr      $c14f
C0BB: 4f         clra     
C0BC: d679       ldb      $79
C0BE: c30020     addd     #32
C0C1: bdbe6f     jsr      $be6f
C0C4: 4f         clra     
C0C5: d67a       ldb      $7a
C0C7: db78       addb     $78
C0C9: 8900       adca     #0
C0CB: bdbe6f     jsr      $be6f
C0CE: 2067       bra      $c137
C0D0: cc001c     ldd      #28
C0D3: bde76b     jsr      $e76b
C0D6: 8d77       bsr      $c14f
C0D8: cc003f     ldd      #63
C0DB: 206a       bra      $c147
C0DD: 3406       pshs     a, b
C0DF: cc001d     ldd      #29
C0E2: bde76b     jsr      $e76b
C0E5: ece4       ldd      , s
C0E7: dd02       std      $02
C0E9: d677       ldb      $77
C0EB: c140       cmpb     #64
C0ED: 2612       bne      $c101
C0EF: 9e02       ldx      $02
C0F1: e609       ldb      9, x
C0F3: d179       cmpb     $79
C0F5: 2606       bne      $c0fd
C0F7: e60a       ldb      10, x
C0F9: d17a       cmpb     $7a
C0FB: 273c       beq      $c139
C0FD: 8d3d       bsr      $c13c
C0FF: 2006       bra      $c107
C101: c120       cmpb     #32
C103: 2602       bne      $c107
C105: 8dc9       bsr      $c0d0
C107: c640       ldb      #64
C109: d777       stb      $77
C10B: c660       ldb      #96
C10D: d778       stb      $78
C10F: 4f         clra     
C110: 9e02       ldx      $02
C112: e609       ldb      9, x
C114: d779       stb      $79
C116: e60a       ldb      10, x
C118: d77a       stb      $7a
C11A: 8d33       bsr      $c14f
C11C: 4f         clra     
C11D: d679       ldb      $79
C11F: c30040     addd     #64
C122: bdbe6f     jsr      $be6f
C125: 4f         clra     
C126: d67a       ldb      $7a
C128: c30060     addd     #96
C12B: bdbe6f     jsr      $be6f
C12E: 4f         clra     
C12F: f6e840     ldb      $e840
C132: c4fd       andb     #-3
C134: f7e840     stb      $e840
C137: 8d35       bsr      $c16e
C139: 3262       leas     2, s
C13B: 39         rts      
C13C: cc001e     ldd      #30
C13F: bde76b     jsr      $e76b
C142: 8d0b       bsr      $c14f
C144: cc005f     ldd      #95
C147: bdbe6f     jsr      $be6f
C14A: 8d22       bsr      $c16e
C14C: 0f77       clr      $77
C14E: 39         rts      
C14F: 4f         clra     
C150: f6e840     ldb      $e840
C153: ca02       orb      #2
C155: f7e840     stb      $e840
C158: f6e821     ldb      $e821
C15B: ca08       orb      #8
C15D: f7e821     stb      $e821
C160: f6e840     ldb      $e840
C163: c480       andb     #-128
C165: 27f9       beq      $c160
C167: f6e840     ldb      $e840
C16A: c4fb       andb     #-5
C16C: 2056       bra      $c1c4
C16E: 4f         clra     
C16F: f6e840     ldb      $e840
C172: ca04       orb      #4
C174: 204e       bra      $c1c4
C176: 4f         clra     
C177: f6e811     ldb      $e811
C17A: c4f7       andb     #-9
C17C: 2006       bra      $c184
C17E: 4f         clra     
C17F: f6e811     ldb      $e811
C182: ca08       orb      #8
C184: f7e811     stb      $e811
C187: 39         rts      
C188: 0f77       clr      $77
C18A: 4f         clra     
C18B: f6e811     ldb      $e811
C18E: ca38       orb      #56
C190: f7e811     stb      $e811
C193: 7fe810     clr      $e810
C196: ca04       orb      #4
C198: f7e811     stb      $e811
C19B: c638       ldb      #56
C19D: f7e821     stb      $e821
C1A0: 7fe820     clr      $e820
C1A3: ca04       orb      #4
C1A5: f7e821     stb      $e821
C1A8: c638       ldb      #56
C1AA: f7e823     stb      $e823
C1AD: c6ff       ldb      #-1
C1AF: f7e822     stb      $e822
C1B2: f6e823     ldb      $e823
C1B5: ca04       orb      #4
C1B7: f7e823     stb      $e823
C1BA: c6ff       ldb      #-1
C1BC: f7e822     stb      $e822
C1BF: c606       ldb      #6
C1C1: f7e842     stb      $e842
C1C4: f7e840     stb      $e840
C1C7: 39         rts      
C1C8: 44         lsra     
C1C9: 65         fcb      $65
C1CA: 766963     ror      $6963
C1CD: 65         fcb      $65
C1CE: 206e       bra      $c23e
C1D0: 6f74       clr      -12, s
C1D2: 2070       bra      $c244
C1D4: 72         fcb      $72
C1D5: 65         fcb      $65
C1D6: 73656e     com      $656e
C1D9: 740030     lsr      >$0030
C1DC: 3000       leax     0, x
C1DE: 3031       leax     -15, y
C1E0: 0034       neg      $34
C1E2: 06ec       ror      $ec
C1E4: 6634       ror      -12, y
C1E6: 06ec       ror      $ec
C1E8: 6634       ror      -12, y
C1EA: 064f       ror      $4f
C1EC: e665       ldb      5, s
C1EE: 9e2a       ldx      $2a
C1F0: ad84       jsr      , x
C1F2: 3266       leas     6, s
C1F4: 39         rts      
C1F5: bde77a     jsr      $e77a
C1F8: 4f         clra     
C1F9: 5f         clrb     
C1FA: 3406       pshs     a, b
C1FC: cc0060     ldd      #96
C1FF: 3406       pshs     a, b
C201: cc012c     ldd      #300
C204: 3406       pshs     a, b
C206: cceff0     ldd      #-4112
C209: bdd55c     jsr      $d55c
C20C: 3266       leas     6, s
C20E: 4f         clra     
C20F: 5f         clrb     
C210: 3406       pshs     a, b
C212: cc0060     ldd      #96
C215: 3406       pshs     a, b
C217: cc0960     ldd      #2400
C21A: bdcbda     jsr      $cbda
C21D: 3264       leas     4, s
C21F: 4f         clra     
C220: 5f         clrb     
C221: 3406       pshs     a, b
C223: cc0060     ldd      #96
C226: 3406       pshs     a, b
C228: cc2580     ldd      #9600
C22B: bdd4cc     jsr      $d4cc
C22E: 3264       leas     4, s
C230: bdc188     jsr      $c188
C233: c602       ldb      #2
C235: d77e       stb      $7e
C237: c60d       ldb      #13
C239: f70378     stb      $0378
C23C: c60a       ldb      #10
C23E: f70379     stb      $0379
C241: 7eca34     jmp      $ca34
C244: 3406       pshs     a, b
C246: 327e       leas     -2, s
C248: 4f         clra     
C249: 5f         clrb     
C24A: bde76b     jsr      $e76b
C24D: bdb636     jsr      $b636
C250: ae62       ldx      2, s
C252: ec04       ldd      4, x
C254: ede4       std      , s
C256: 6ff4       clr      [, s]
C258: ec66       ldd      6, s
C25A: dd68       std      $68
C25C: ec68       ldd      8, s
C25E: 3406       pshs     a, b
C260: ec62       ldd      2, s
C262: bdc9a5     jsr      $c9a5
C265: 3262       leas     2, s
C267: d66a       ldb      $6a
C269: 2634       bne      $c29f
C26B: ec62       ldd      2, s
C26D: bde1ee     jsr      $e1ee
C270: d66a       ldb      $6a
C272: 262b       bne      $c29f
C274: 4f         clra     
C275: aee4       ldx      , s
C277: e602       ldb      2, x
C279: c480       andb     #-128
C27B: 2707       beq      $c284
C27D: ece4       ldd      , s
C27F: bdd28c     jsr      $d28c
C282: 201b       bra      $c29f
C284: e602       ldb      2, x
C286: c104       cmpb     #4
C288: 2615       bne      $c29f
C28A: e6f808     ldb      [8, s]
C28D: bdbab4     jsr      $bab4
C290: 3406       pshs     a, b
C292: 4f         clra     
C293: e6f804     ldb      [4, s]
C296: 3406       pshs     a, b
C298: ec64       ldd      4, s
C29A: bdcc13     jsr      $cc13
C29D: 3264       leas     4, s
C29F: 4f         clra     
C2A0: d66a       ldb      $6a
C2A2: 7ec95f     jmp      $c95f
C2A5: 3406       pshs     a, b
C2A7: 327e       leas     -2, s
C2A9: cc0001     ldd      #1
C2AC: bde76b     jsr      $e76b
C2AF: bdb636     jsr      $b636
C2B2: ae62       ldx      2, s
C2B4: ec04       ldd      4, x
C2B6: ede4       std      , s
C2B8: aee4       ldx      , s
C2BA: 6f84       clr      , x
C2BC: 4f         clra     
C2BD: e602       ldb      2, x
C2BF: c480       andb     #-128
C2C1: 2707       beq      $c2ca
C2C3: ece4       ldd      , s
C2C5: bdd331     jsr      $d331
C2C8: 200b       bra      $c2d5
C2CA: e602       ldb      2, x
C2CC: c104       cmpb     #4
C2CE: 2605       bne      $c2d5
C2D0: ece4       ldd      , s
C2D2: bdcf49     jsr      $cf49
C2D5: 3264       leas     4, s
C2D7: 39         rts      
C2D8: 3406       pshs     a, b
C2DA: 327c       leas     -4, s
C2DC: cc0002     ldd      #2
C2DF: bde76b     jsr      $e76b
C2E2: bdb636     jsr      $b636
C2E5: ae64       ldx      4, s
C2E7: ec04       ldd      4, x
C2E9: ede4       std      , s
C2EB: aee4       ldx      , s
C2ED: 6f84       clr      , x
C2EF: 4f         clra     
C2F0: e601       ldb      1, x
C2F2: c410       andb     #16
C2F4: 2705       beq      $c2fb
C2F6: ec64       ldd      4, s
C2F8: bdc7f7     jsr      $c7f7
C2FB: 4f         clra     
C2FC: aee4       ldx      , s
C2FE: e601       ldb      1, x
C300: c401       andb     #1
C302: 260d       bne      $c311
C304: ccca57     ldd      #-13737
C307: bdb653     jsr      $b653
C30A: c603       ldb      #3
C30C: e7f4       stb      [, s]
C30E: 7ec4cd     jmp      $c4cd
C311: e602       ldb      2, x
C313: c480       andb     #-128
C315: 2705       beq      $c31c
C317: ece4       ldd      , s
C319: bdc0dd     jsr      $c0dd
C31C: 4f         clra     
C31D: aee4       ldx      , s
C31F: e601       ldb      1, x
C321: c408       andb     #8
C323: 265d       bne      $c382
C325: e601       ldb      1, x
C327: ca08       orb      #8
C329: e701       stb      1, x
C32B: ae64       ldx      4, s
C32D: ec01       ldd      1, x
C32F: aee4       ldx      , s
C331: ed05       std      5, x
C333: e602       ldb      2, x
C335: c181       cmpb     #-127
C337: 2649       bne      $c382
C339: e6f804     ldb      [4, s]
C33C: c176       cmpb     #118
C33E: 2642       bne      $c382
C340: ece4       ldd      , s
C342: bdd33b     jsr      $d33b
C345: aee4       ldx      , s
C347: ed05       std      5, x
C349: ec64       ldd      4, s
C34B: bdca20     jsr      $ca20
C34E: 2712       beq      $c362
C350: ece4       ldd      , s
C352: bdbfef     jsr      $bfef
C355: bdb63e     jsr      $b63e
C358: c602       ldb      #2
C35A: e7f4       stb      [, s]
C35C: 4f         clra     
C35D: 5f         clrb     
C35E: ed6a       std      10, s
C360: 2020       bra      $c382
C362: aee4       ldx      , s
C364: ec05       ldd      5, x
C366: 1f98       tfr      b, a
C368: 5f         clrb     
C369: 3406       pshs     a, b
C36B: ec62       ldd      2, s
C36D: bdd33b     jsr      $d33b
C370: e3e4       addd     , s
C372: 3510       puls     x
C374: aee4       ldx      , s
C376: ed05       std      5, x
C378: 2608       bne      $c382
C37A: 4f         clra     
C37B: 5f         clrb     
C37C: ed6a       std      10, s
C37E: c601       ldb      #1
C380: e784       stb      , x
C382: 4f         clra     
C383: 5f         clrb     
C384: ed62       std      2, s
C386: aee4       ldx      , s
C388: e601       ldb      1, x
C38A: c420       andb     #32
C38C: 102600c8   lbne     $c458
C390: ec6a       ldd      10, s
C392: 2f78       ble      $c40c
C394: ec64       ldd      4, s
C396: bdc4d2     jsr      $c4d2
C399: e7f808     stb      [8, s]
C39C: aee4       ldx      , s
C39E: e684       ldb      , x
C3A0: c102       cmpb     #2
C3A2: 2768       beq      $c40c
C3A4: c103       cmpb     #3
C3A6: 2764       beq      $c40c
C3A8: e6f804     ldb      [4, s]
C3AB: c174       cmpb     #116
C3AD: 2614       bne      $c3c3
C3AF: 4f         clra     
C3B0: e601       ldb      1, x
C3B2: c4c0       andb     #-64
C3B4: 260d       bne      $c3c3
C3B6: e6f808     ldb      [8, s]
C3B9: c10d       cmpb     #13
C3BB: 2606       bne      $c3c3
C3BD: c601       ldb      #1
C3BF: e7f4       stb      [, s]
C3C1: 2042       bra      $c405
C3C3: e602       ldb      2, x
C3C5: c105       cmpb     #5
C3C7: 2609       bne      $c3d2
C3C9: ec64       ldd      4, s
C3CB: bdca20     jsr      $ca20
C3CE: 2702       beq      $c3d2
C3D0: 2033       bra      $c405
C3D2: aee4       ldx      , s
C3D4: ec05       ldd      5, x
C3D6: 2718       beq      $c3f0
C3D8: ec03       ldd      3, x
C3DA: c30001     addd     #1
C3DD: ed03       std      3, x
C3DF: a305       subd     5, x
C3E1: 2f0d       ble      $c3f0
C3E3: c601       ldb      #1
C3E5: e784       stb      , x
C3E7: ec62       ldd      2, s
C3E9: c30001     addd     #1
C3EC: ed62       std      2, s
C3EE: 2015       bra      $c405
C3F0: ec62       ldd      2, s
C3F2: c30001     addd     #1
C3F5: ed62       std      2, s
C3F7: ec6a       ldd      10, s
C3F9: c3ffff     addd     #-1
C3FC: ed6a       std      10, s
C3FE: ec68       ldd      8, s
C400: c30001     addd     #1
C403: ed68       std      8, s
C405: ec64       ldd      4, s
C407: bdca20     jsr      $ca20
C40A: 2784       beq      $c390
C40C: ec64       ldd      4, s
C40E: bdca20     jsr      $ca20
C411: 2745       beq      $c458
C413: aee4       ldx      , s
C415: e602       ldb      2, x
C417: c181       cmpb     #-127
C419: 263d       bne      $c458
C41B: 4f         clra     
C41C: e60c       ldb      12, x
C41E: c408       andb     #8
C420: 2736       beq      $c458
C422: ec03       ldd      3, x
C424: 830002     subd     #2
C427: 260f       bne      $c438
C429: ec68       ldd      8, s
C42B: c3ffff     addd     #-1
C42E: 3406       pshs     a, b
C430: 3510       puls     x
C432: e684       ldb      , x
C434: c1ff       cmpb     #-1
C436: 2719       beq      $c451
C438: ece4       ldd      , s
C43A: bdbfef     jsr      $bfef
C43D: cc0002     ldd      #2
C440: 3406       pshs     a, b
C442: cc0300     ldd      #768
C445: 3406       pshs     a, b
C447: ccca66     ldd      #-13722
C44A: bdb7ce     jsr      $b7ce
C44D: 3264       leas     4, s
C44F: 2707       beq      $c458
C451: c602       ldb      #2
C453: e7f4       stb      [, s]
C455: bdb63e     jsr      $b63e
C458: e6f804     ldb      [4, s]
C45B: c166       cmpb     #102
C45D: 265c       bne      $c4bb
C45F: aee4       ldx      , s
C461: e684       ldb      , x
C463: c103       cmpb     #3
C465: 2754       beq      $c4bb
C467: c102       cmpb     #2
C469: 2750       beq      $c4bb
C46B: ec03       ldd      3, x
C46D: a305       subd     5, x
C46F: 2e12       bgt      $c483
C471: ec64       ldd      4, s
C473: bdca20     jsr      $ca20
C476: 270b       beq      $c483
C478: aee4       ldx      , s
C47A: 6f84       clr      , x
C47C: 4f         clra     
C47D: e601       ldb      1, x
C47F: ca20       orb      #32
C481: e701       stb      1, x
C483: ec6a       ldd      10, s
C485: 2f34       ble      $c4bb
C487: ec64       ldd      4, s
C489: bdca20     jsr      $ca20
C48C: 262d       bne      $c4bb
C48E: c620       ldb      #32
C490: e7f808     stb      [8, s]
C493: ec68       ldd      8, s
C495: c30001     addd     #1
C498: ed68       std      8, s
C49A: ec62       ldd      2, s
C49C: c30001     addd     #1
C49F: ed62       std      2, s
C4A1: ec6a       ldd      10, s
C4A3: c3ffff     addd     #-1
C4A6: ed6a       std      10, s
C4A8: aee4       ldx      , s
C4AA: ec03       ldd      3, x
C4AC: c30001     addd     #1
C4AF: ed03       std      3, x
C4B1: a305       subd     5, x
C4B3: 2fce       ble      $c483
C4B5: c601       ldb      #1
C4B7: e784       stb      , x
C4B9: 20c8       bra      $c483
C4BB: aee4       ldx      , s
C4BD: e684       ldb      , x
C4BF: 270c       beq      $c4cd
C4C1: 4f         clra     
C4C2: e601       ldb      1, x
C4C4: c4d7       andb     #-41
C4C6: e701       stb      1, x
C4C8: cc0001     ldd      #1
C4CB: ed03       std      3, x
C4CD: ec62       ldd      2, s
C4CF: 3266       leas     6, s
C4D1: 39         rts      
C4D2: 3406       pshs     a, b
C4D4: 327d       leas     -3, s
C4D6: cc0006     ldd      #6
C4D9: bde76b     jsr      $e76b
C4DC: ae63       ldx      3, s
C4DE: ec04       ldd      4, x
C4E0: ede4       std      , s
C4E2: 4f         clra     
C4E3: aee4       ldx      , s
C4E5: e602       ldb      2, x
C4E7: c480       andb     #-128
C4E9: 2707       beq      $c4f2
C4EB: ece4       ldd      , s
C4ED: bdd33b     jsr      $d33b
C4F0: 2032       bra      $c524
C4F2: e602       ldb      2, x
C4F4: c104       cmpb     #4
C4F6: 2607       bne      $c4ff
C4F8: ece4       ldd      , s
C4FA: bdcd5a     jsr      $cd5a
C4FD: 2025       bra      $c524
C4FF: c105       cmpb     #5
C501: 2607       bne      $c50a
C503: ece4       ldd      , s
C505: bdd4d2     jsr      $d4d2
C508: 201a       bra      $c524
C50A: c106       cmpb     #6
C50C: 260f       bne      $c51d
C50E: ece4       ldd      , s
C510: bdd5bb     jsr      $d5bb
C513: e762       stb      2, s
C515: 260f       bne      $c526
C517: e6f4       ldb      [, s]
C519: 27f3       beq      $c50e
C51B: 2009       bra      $c526
C51D: c107       cmpb     #7
C51F: 2605       bne      $c526
C521: bddd82     jsr      $dd82
C524: e762       stb      2, s
C526: 4f         clra     
C527: e662       ldb      2, s
C529: 3265       leas     5, s
C52B: 39         rts      
C52C: 3406       pshs     a, b
C52E: 327d       leas     -3, s
C530: cc0003     ldd      #3
C533: bde76b     jsr      $e76b
C536: bdb636     jsr      $b636
C539: ae63       ldx      3, s
C53B: ec04       ldd      4, x
C53D: ede4       std      , s
C53F: aee4       ldx      , s
C541: 6f84       clr      , x
C543: 4f         clra     
C544: e601       ldb      1, x
C546: c408       andb     #8
C548: 2705       beq      $c54f
C54A: ec63       ldd      3, s
C54C: bdc7f7     jsr      $c7f7
C54F: 4f         clra     
C550: aee4       ldx      , s
C552: e601       ldb      1, x
C554: c402       andb     #2
C556: 260d       bne      $c565
C558: ccca57     ldd      #-13737
C55B: bdb653     jsr      $b653
C55E: c603       ldb      #3
C560: e7f4       stb      [, s]
C562: 7ec666     jmp      $c666
C565: e602       ldb      2, x
C567: c480       andb     #-128
C569: 270c       beq      $c577
C56B: cc0060     ldd      #96
C56E: 3406       pshs     a, b
C570: ec62       ldd      2, s
C572: bdc072     jsr      $c072
C575: 3262       leas     2, s
C577: 4f         clra     
C578: aee4       ldx      , s
C57A: e601       ldb      1, x
C57C: c410       andb     #16
C57E: 2624       bne      $c5a4
C580: e601       ldb      1, x
C582: ca10       orb      #16
C584: e701       stb      1, x
C586: ae63       ldx      3, s
C588: ec01       ldd      1, x
C58A: aee4       ldx      , s
C58C: ed05       std      5, x
C58E: 2714       beq      $c5a4
C590: e602       ldb      2, x
C592: c181       cmpb     #-127
C594: 260e       bne      $c5a4
C596: e6f803     ldb      [3, s]
C599: c176       cmpb     #118
C59B: 2607       bne      $c5a4
C59D: ec05       ldd      5, x
C59F: c3fffe     addd     #-2
C5A2: ed05       std      5, x
C5A4: ec05       ldd      5, x
C5A6: 2724       beq      $c5cc
C5A8: a303       subd     3, x
C5AA: c30001     addd     #1
C5AD: 3406       pshs     a, b
C5AF: ec6b       ldd      11, s
C5B1: a3e4       subd     , s
C5B3: 3510       puls     x
C5B5: 2f15       ble      $c5cc
C5B7: aee4       ldx      , s
C5B9: ec05       ldd      5, x
C5BB: a303       subd     3, x
C5BD: c30001     addd     #1
C5C0: ed69       std      9, s
C5C2: ccca69     ldd      #-13719
C5C5: bdb653     jsr      $b653
C5C8: c603       ldb      #3
C5CA: e7f4       stb      [, s]
C5CC: aee4       ldx      , s
C5CE: e602       ldb      2, x
C5D0: c181       cmpb     #-127
C5D2: 262a       bne      $c5fe
C5D4: e6f803     ldb      [3, s]
C5D7: c176       cmpb     #118
C5D9: 2623       bne      $c5fe
C5DB: ec69       ldd      9, s
C5DD: 1f89       tfr      a, b
C5DF: 4f         clra     
C5E0: 3406       pshs     a, b
C5E2: ec62       ldd      2, s
C5E4: bdd37b     jsr      $d37b
C5E7: 3262       leas     2, s
C5E9: ec69       ldd      9, s
C5EB: 3406       pshs     a, b
C5ED: ec62       ldd      2, s
C5EF: bdd37b     jsr      $d37b
C5F2: 3262       leas     2, s
C5F4: aee4       ldx      , s
C5F6: ec05       ldd      5, x
C5F8: 2704       beq      $c5fe
C5FA: ec69       ldd      9, s
C5FC: ed05       std      5, x
C5FE: ec69       ldd      9, s
C600: 2f2f       ble      $c631
C602: ec05       ldd      5, x
C604: 2706       beq      $c60c
C606: ec03       ldd      3, x
C608: a305       subd     5, x
C60A: 2c25       bge      $c631
C60C: 4f         clra     
C60D: e6f807     ldb      [7, s]
C610: 3406       pshs     a, b
C612: ec65       ldd      5, s
C614: 8d53       bsr      $c669
C616: 3262       leas     2, s
C618: aee4       ldx      , s
C61A: ec03       ldd      3, x
C61C: c30001     addd     #1
C61F: ed03       std      3, x
C621: ec67       ldd      7, s
C623: c30001     addd     #1
C626: ed67       std      7, s
C628: ec69       ldd      9, s
C62A: c3ffff     addd     #-1
C62D: ed69       std      9, s
C62F: 20cd       bra      $c5fe
C631: ec69       ldd      9, s
C633: 2f31       ble      $c666
C635: ec03       ldd      3, x
C637: a305       subd     5, x
C639: 2e2b       bgt      $c666
C63B: ec03       ldd      3, x
C63D: c30001     addd     #1
C640: ed03       std      3, x
C642: e602       ldb      2, x
C644: c181       cmpb     #-127
C646: 2612       bne      $c65a
C648: e60c       ldb      12, x
C64A: 2705       beq      $c651
C64C: cc0008     ldd      #8
C64F: 2709       beq      $c65a
C651: 4f         clra     
C652: e6f807     ldb      [7, s]
C655: bdbe5f     jsr      $be5f
C658: 200c       bra      $c666
C65A: 4f         clra     
C65B: e6f807     ldb      [7, s]
C65E: 3406       pshs     a, b
C660: ec65       ldd      5, s
C662: 8d05       bsr      $c669
C664: 3262       leas     2, s
C666: 3265       leas     5, s
C668: 39         rts      
C669: 3406       pshs     a, b
C66B: 327e       leas     -2, s
C66D: cc0007     ldd      #7
C670: bde76b     jsr      $e76b
C673: ae62       ldx      2, s
C675: ec04       ldd      4, x
C677: ede4       std      , s
C679: 4f         clra     
C67A: aee4       ldx      , s
C67C: e602       ldb      2, x
C67E: c480       andb     #-128
C680: 270b       beq      $c68d
C682: e667       ldb      7, s
C684: 3406       pshs     a, b
C686: ec62       ldd      2, s
C688: bdd37b     jsr      $d37b
C68B: 2029       bra      $c6b6
C68D: e602       ldb      2, x
C68F: c104       cmpb     #4
C691: 260b       bne      $c69e
C693: e667       ldb      7, s
C695: 3406       pshs     a, b
C697: ec62       ldd      2, s
C699: bdcce6     jsr      $cce6
C69C: 2018       bra      $c6b6
C69E: c105       cmpb     #5
C6A0: 2607       bne      $c6a9
C6A2: e667       ldb      7, s
C6A4: bdd4f0     jsr      $d4f0
C6A7: 201a       bra      $c6c3
C6A9: c106       cmpb     #6
C6AB: 260d       bne      $c6ba
C6AD: e667       ldb      7, s
C6AF: 3406       pshs     a, b
C6B1: ec62       ldd      2, s
C6B3: bdd598     jsr      $d598
C6B6: 3262       leas     2, s
C6B8: 2009       bra      $c6c3
C6BA: c107       cmpb     #7
C6BC: 2605       bne      $c6c3
C6BE: e667       ldb      7, s
C6C0: bdddad     jsr      $ddad
C6C3: 3264       leas     4, s
C6C5: 39         rts      
C6C6: 3406       pshs     a, b
C6C8: 327c       leas     -4, s
C6CA: cc0008     ldd      #8
C6CD: bde76b     jsr      $e76b
C6D0: ae64       ldx      4, s
C6D2: ec04       ldd      4, x
C6D4: ede4       std      , s
C6D6: e669       ldb      9, s
C6D8: 2705       beq      $c6df
C6DA: bdb636     jsr      $b636
C6DD: 6ff4       clr      [, s]
C6DF: 4f         clra     
C6E0: aee4       ldx      , s
C6E2: e601       ldb      1, x
C6E4: c408       andb     #8
C6E6: 2705       beq      $c6ed
C6E8: ec64       ldd      4, s
C6EA: bdc7f7     jsr      $c7f7
C6ED: 4f         clra     
C6EE: aee4       ldx      , s
C6F0: e601       ldb      1, x
C6F2: c402       andb     #2
C6F4: 260d       bne      $c703
C6F6: ccca57     ldd      #-13737
C6F9: bdb653     jsr      $b653
C6FC: c603       ldb      #3
C6FE: e7f4       stb      [, s]
C700: 7ec7f4     jmp      $c7f4
C703: e602       ldb      2, x
C705: c480       andb     #-128
C707: 270c       beq      $c715
C709: cc0060     ldd      #96
C70C: 3406       pshs     a, b
C70E: ec62       ldd      2, s
C710: bdc072     jsr      $c072
C713: 3262       leas     2, s
C715: 4f         clra     
C716: aee4       ldx      , s
C718: e601       ldb      1, x
C71A: c410       andb     #16
C71C: 2608       bne      $c726
C71E: ae64       ldx      4, s
C720: ec01       ldd      1, x
C722: aee4       ldx      , s
C724: ed05       std      5, x
C726: e6f804     ldb      [4, s]
C729: c166       cmpb     #102
C72B: 2646       bne      $c773
C72D: ec03       ldd      3, x
C72F: a305       subd     5, x
C731: 2c17       bge      $c74a
C733: cc0020     ldd      #32
C736: 3406       pshs     a, b
C738: ec66       ldd      6, s
C73A: bdc669     jsr      $c669
C73D: 3262       leas     2, s
C73F: aee4       ldx      , s
C741: ec03       ldd      3, x
C743: c30001     addd     #1
C746: ed03       std      3, x
C748: 20e3       bra      $c72d
C74A: ec03       ldd      3, x
C74C: a305       subd     5, x
C74E: 2623       bne      $c773
C750: e602       ldb      2, x
C752: c181       cmpb     #-127
C754: 2611       bne      $c767
C756: e60c       ldb      12, x
C758: 2705       beq      $c75f
C75A: cc0008     ldd      #8
C75D: 2708       beq      $c767
C75F: cc0020     ldd      #32
C762: bdbe5f     jsr      $be5f
C765: 200c       bra      $c773
C767: cc0020     ldd      #32
C76A: 3406       pshs     a, b
C76C: ec66       ldd      6, s
C76E: bdc669     jsr      $c669
C771: 3262       leas     2, s
C773: aee4       ldx      , s
C775: e602       ldb      2, x
C777: c181       cmpb     #-127
C779: 2613       bne      $c78e
C77B: e6f804     ldb      [4, s]
C77E: c174       cmpb     #116
C780: 2664       bne      $c7e6
C782: ec05       ldd      5, x
C784: 270d       beq      $c793
C786: ec03       ldd      3, x
C788: a305       subd     5, x
C78A: 2e5a       bgt      $c7e6
C78C: 2005       bra      $c793
C78E: 4f         clra     
C78F: c480       andb     #-128
C791: 2707       beq      $c79a
C793: ece4       ldd      , s
C795: bdd3de     jsr      $d3de
C798: 204c       bra      $c7e6
C79A: e602       ldb      2, x
C79C: c104       cmpb     #4
C79E: 2607       bne      $c7a7
C7A0: ece4       ldd      , s
C7A2: bdcd26     jsr      $cd26
C7A5: 203f       bra      $c7e6
C7A7: c105       cmpb     #5
C7A9: 2616       bne      $c7c1
C7AB: e6f804     ldb      [4, s]
C7AE: c166       cmpb     #102
C7B0: 2607       bne      $c7b9
C7B2: ec05       ldd      5, x
C7B4: 830050     subd     #80
C7B7: 2c2d       bge      $c7e6
C7B9: cc000d     ldd      #13
C7BC: bdd4f0     jsr      $d4f0
C7BF: 2025       bra      $c7e6
C7C1: c106       cmpb     #6
C7C3: 2621       bne      $c7e6
C7C5: 5f         clrb     
C7C6: ed62       std      2, s
C7C8: d17e       cmpb     $7e
C7CA: 241a       bcc      $c7e6
C7CC: c30378     addd     #888
C7CF: 3406       pshs     a, b
C7D1: 4f         clra     
C7D2: 3510       puls     x
C7D4: e684       ldb      , x
C7D6: 3406       pshs     a, b
C7D8: ec62       ldd      2, s
C7DA: bdd598     jsr      $d598
C7DD: 3262       leas     2, s
C7DF: ec62       ldd      2, s
C7E1: c30001     addd     #1
C7E4: 20e0       bra      $c7c6
C7E6: 4f         clra     
C7E7: aee4       ldx      , s
C7E9: e601       ldb      1, x
C7EB: c4ef       andb     #-17
C7ED: e701       stb      1, x
C7EF: cc0001     ldd      #1
C7F2: ed03       std      3, x
C7F4: 3266       leas     6, s
C7F6: 39         rts      
C7F7: 3406       pshs     a, b
C7F9: 327d       leas     -3, s
C7FB: cc0009     ldd      #9
C7FE: bde76b     jsr      $e76b
C801: bdb636     jsr      $b636
C804: ae63       ldx      3, s
C806: ec04       ldd      4, x
C808: ede4       std      , s
C80A: aee4       ldx      , s
C80C: 6f84       clr      , x
C80E: 4f         clra     
C80F: e601       ldb      1, x
C811: c408       andb     #8
C813: 2723       beq      $c838
C815: cc0001     ldd      #1
C818: 3406       pshs     a, b
C81A: 1f41       tfr      s, x
C81C: c604       ldb      #4
C81E: 3a         abx      
C81F: 3410       pshs     x
C821: ec67       ldd      7, s
C823: bdc2d8     jsr      $c2d8
C826: 3264       leas     4, s
C828: e6f4       ldb      [, s]
C82A: 27e9       beq      $c815
C82C: aee4       ldx      , s
C82E: e684       ldb      , x
C830: c101       cmpb     #1
C832: 260f       bne      $c843
C834: 6f84       clr      , x
C836: 200b       bra      $c843
C838: e601       ldb      1, x
C83A: c410       andb     #16
C83C: 2705       beq      $c843
C83E: ec63       ldd      3, s
C840: bdc6c6     jsr      $c6c6
C843: 3265       leas     5, s
C845: 39         rts      
C846: 3406       pshs     a, b
C848: 327e       leas     -2, s
C84A: cc000a     ldd      #10
C84D: bde76b     jsr      $e76b
C850: bdb636     jsr      $b636
C853: ae62       ldx      2, s
C855: ec04       ldd      4, x
C857: ede4       std      , s
C859: aee4       ldx      , s
C85B: 6f84       clr      , x
C85D: e602       ldb      2, x
C85F: c105       cmpb     #5
C861: 260d       bne      $c870
C863: ec66       ldd      6, s
C865: 1f98       tfr      b, a
C867: 5f         clrb     
C868: c30101     addd     #257
C86B: bdd509     jsr      $d509
C86E: 204c       bra      $c8bc
C870: c104       cmpb     #4
C872: 260d       bne      $c881
C874: ec66       ldd      6, s
C876: 3406       pshs     a, b
C878: ec62       ldd      2, s
C87A: bdcef9     jsr      $cef9
C87D: 3262       leas     2, s
C87F: 203b       bra      $c8bc
C881: c181       cmpb     #-127
C883: 263a       bne      $c8bf
C885: 4f         clra     
C886: e60c       ldb      12, x
C888: c408       andb     #8
C88A: 2733       beq      $c8bf
C88C: ec66       ldd      6, s
C88E: c30001     addd     #1
C891: 3406       pshs     a, b
C893: ec62       ldd      2, s
C895: bdbf98     jsr      $bf98
C898: 3262       leas     2, s
C89A: d66a       ldb      $6a
C89C: 261e       bne      $c8bc
C89E: ece4       ldd      , s
C8A0: bdbfef     jsr      $bfef
C8A3: cc0002     ldd      #2
C8A6: 3406       pshs     a, b
C8A8: cc0300     ldd      #768
C8AB: 3406       pshs     a, b
C8AD: ccca66     ldd      #-13722
C8B0: bdb7ce     jsr      $b7ce
C8B3: 3264       leas     4, s
C8B5: 2705       beq      $c8bc
C8B7: bdb636     jsr      $b636
C8BA: 6ff4       clr      [, s]
C8BC: 7eca12     jmp      $ca12
C8BF: c603       ldb      #3
C8C1: e7f4       stb      [, s]
C8C3: ccca73     ldd      #-13709
C8C6: 7ec9a0     jmp      $c9a0
C8C9: 3406       pshs     a, b
C8CB: 327e       leas     -2, s
C8CD: cc000b     ldd      #11
C8D0: bde76b     jsr      $e76b
C8D3: bdb636     jsr      $b636
C8D6: ae62       ldx      2, s
C8D8: ec04       ldd      4, x
C8DA: ede4       std      , s
C8DC: 6ff4       clr      [, s]
C8DE: ec66       ldd      6, s
C8E0: dd68       std      $68
C8E2: ec62       ldd      2, s
C8E4: bde1ee     jsr      $e1ee
C8E7: aee4       ldx      , s
C8E9: e602       ldb      2, x
C8EB: c181       cmpb     #-127
C8ED: 2610       bne      $c8ff
C8EF: ece4       ldd      , s
C8F1: bdbd96     jsr      $bd96
C8F4: d66a       ldb      $6a
C8F6: 2669       bne      $c961
C8F8: ece4       ldd      , s
C8FA: bdbfef     jsr      $bfef
C8FD: 2062       bra      $c961
C8FF: c104       cmpb     #4
C901: 2654       bne      $c957
C903: ece4       ldd      , s
C905: bdcf64     jsr      $cf64
C908: 2057       bra      $c961
C90A: 3406       pshs     a, b
C90C: 327e       leas     -2, s
C90E: cc000c     ldd      #12
C911: bde76b     jsr      $e76b
C914: bdb636     jsr      $b636
C917: ae62       ldx      2, s
C919: ec04       ldd      4, x
C91B: ede4       std      , s
C91D: 6ff4       clr      [, s]
C91F: ec66       ldd      6, s
C921: dd68       std      $68
C923: ec62       ldd      2, s
C925: bde1ee     jsr      $e1ee
C928: aee4       ldx      , s
C92A: e602       ldb      2, x
C92C: c181       cmpb     #-127
C92E: 2616       bne      $c946
C930: ec68       ldd      8, s
C932: 3406       pshs     a, b
C934: ec62       ldd      2, s
C936: bdbdc6     jsr      $bdc6
C939: 3262       leas     2, s
C93B: d66a       ldb      $6a
C93D: 2664       bne      $c9a3
C93F: ece4       ldd      , s
C941: bdbfef     jsr      $bfef
C944: 205d       bra      $c9a3
C946: c104       cmpb     #4
C948: 260d       bne      $c957
C94A: ec68       ldd      8, s
C94C: 3406       pshs     a, b
C94E: ec62       ldd      2, s
C950: bdcf85     jsr      $cf85
C953: 3262       leas     2, s
C955: 204c       bra      $c9a3
C957: ccca7e     ldd      #-13698
C95A: bdb653     jsr      $b653
C95D: c603       ldb      #3
C95F: e7f4       stb      [, s]
C961: 2040       bra      $c9a3
C963: 3406       pshs     a, b
C965: 327e       leas     -2, s
C967: cc000d     ldd      #13
C96A: bde76b     jsr      $e76b
C96D: bdb636     jsr      $b636
C970: ae62       ldx      2, s
C972: ec04       ldd      4, x
C974: ede4       std      , s
C976: 6ff4       clr      [, s]
C978: ec66       ldd      6, s
C97A: dd68       std      $68
C97C: ec62       ldd      2, s
C97E: bde2ee     jsr      $e2ee
C981: d66a       ldb      $6a
C983: 261e       bne      $c9a3
C985: aee4       ldx      , s
C987: e602       ldb      2, x
C989: c181       cmpb     #-127
C98B: 2610       bne      $c99d
C98D: ece4       ldd      , s
C98F: bdbe23     jsr      $be23
C992: d66a       ldb      $6a
C994: 267c       bne      $ca12
C996: ece4       ldd      , s
C998: bdbfef     jsr      $bfef
C99B: 2075       bra      $ca12
C99D: ccca7e     ldd      #-13698
C9A0: bdb653     jsr      $b653
C9A3: 206d       bra      $ca12
C9A5: 3406       pshs     a, b
C9A7: ec64       ldd      4, s
C9A9: bdb7b1     jsr      $b7b1
C9AC: 3406       pshs     a, b
C9AE: ec66       ldd      6, s
C9B0: 3406       pshs     a, b
C9B2: ccb18c     ldd      #-20084
C9B5: bdbba7     jsr      $bba7
C9B8: 3264       leas     4, s
C9BA: c3b1b1     addd     #-20047
C9BD: 3406       pshs     a, b
C9BF: 4f         clra     
C9C0: 3510       puls     x
C9C2: e684       ldb      , x
C9C4: aee4       ldx      , s
C9C6: e701       stb      1, x
C9C8: 2667       bne      $ca31
C9CA: ccca8c     ldd      #-13684
C9CD: bdb653     jsr      $b653
C9D0: c603       ldb      #3
C9D2: e7f4       stb      [, s]
C9D4: 205b       bra      $ca31
C9D6: 3406       pshs     a, b
C9D8: 327e       leas     -2, s
C9DA: cc000e     ldd      #14
C9DD: bde76b     jsr      $e76b
C9E0: ae62       ldx      2, s
C9E2: ec04       ldd      4, x
C9E4: ede4       std      , s
C9E6: aee4       ldx      , s
C9E8: e684       ldb      , x
C9EA: 2623       bne      $ca0f
C9EC: e602       ldb      2, x
C9EE: c181       cmpb     #-127
C9F0: 261d       bne      $ca0f
C9F2: 4f         clra     
C9F3: e60c       ldb      12, x
C9F5: c404       andb     #4
C9F7: 2616       bne      $ca0f
C9F9: e60c       ldb      12, x
C9FB: c402       andb     #2
C9FD: 2610       bne      $ca0f
C9FF: ece4       ldd      , s
CA01: bdbfef     jsr      $bfef
CA04: aee4       ldx      , s
CA06: e784       stb      , x
CA08: 4f         clra     
CA09: e60c       ldb      12, x
CA0B: ca06       orb      #6
CA0D: e70c       stb      12, x
CA0F: 4f         clra     
CA10: e6f4       ldb      [, s]
CA12: 3264       leas     4, s
CA14: 39         rts      
CA15: 3406       pshs     a, b
CA17: aee4       ldx      , s
CA19: e69804     ldb      [4, x]
CA1C: c102       cmpb     #2
CA1E: 2009       bra      $ca29
CA20: 3406       pshs     a, b
CA22: aee4       ldx      , s
CA24: e69804     ldb      [4, x]
CA27: c101       cmpb     #1
CA29: 2603       bne      $ca2e
CA2B: 86ff       lda      #-1
CA2D: 214f       brn      $ca7e
CA2F: 1f89       tfr      a, b
CA31: 3262       leas     2, s
CA33: 39         rts      
CA34: cc0014     ldd      #20
CA37: fd0368     std      $0368
CA3A: fd036a     std      $036a
CA3D: 4f         clra     
CA3E: 5f         clrb     
CA3F: fd036c     std      $036c
CA42: fd036e     std      $036e
CA45: cc0064     ldd      #100
CA48: fd0370     std      $0370
CA4B: 4f         clra     
CA4C: 5f         clrb     
CA4D: fd0372     std      $0372
CA50: fd0374     std      $0374
CA53: fd0376     std      $0376
CA56: 39         rts      
CA57: 696e       rol      14, s
CA59: 76616c     ror      $616c
CA5C: 6964       rol      4, s
CA5E: 2061       bra      $cac1
CA60: 6363       com      3, s
CA62: 65         fcb      $65
CA63: 737300     com      $7300
CA66: 3530       puls     x, y
CA68: 0074       neg      $74
CA6A: 72         fcb      $72
CA6B: 75         fcb      $75
CA6C: 6e63       jmp      3, s
CA6E: 61         fcb      $61
CA6F: 746564     lsr      $6564
CA72: 004e       neg      $4e
CA74: 6f74       clr      -12, s
CA76: 2052       bra      $caca
CA78: 61         fcb      $61
CA79: 6e64       jmp      4, s
CA7B: 6f6d       clr      13, s
CA7D: 006e       neg      $6e
CA7F: 6f74       clr      -12, s
CA81: 2073       bra      $caf6
CA83: 75         fcb      $75
CA84: 70706f     neg      $706f
CA87: 72         fcb      $72
CA88: 746564     lsr      $6564
CA8B: 0069       neg      $69
CA8D: 6e76       jmp      -10, s
CA8F: 61         fcb      $61
CA90: 6c69       inc      9, s
CA92: 6420       lsr      0, y
CA94: 6d6f       tst      15, s
CA96: 6465       lsr      5, s
CA98: 0034       neg      $34
CA9A: 0632       ror      $32
CA9C: 7ecc00     jmp      $cc00
CA9F: 11         fcb      $11
CAA0: bde76b     jsr      $e76b
CAA3: bdb636     jsr      $b636
CAA6: ae62       ldx      2, s
CAA8: ec04       ldd      4, x
CAAA: ede4       std      , s
CAAC: aee4       ldx      , s
CAAE: 6f84       clr      , x
CAB0: ec66       ldd      6, s
CAB2: dd68       std      $68
CAB4: 6f01       clr      1, x
CAB6: ec62       ldd      2, s
CAB8: bde2ee     jsr      $e2ee
CABB: d66a       ldb      $6a
CABD: 2623       bne      $cae2
CABF: aee4       ldx      , s
CAC1: e602       ldb      2, x
CAC3: c181       cmpb     #-127
CAC5: 2612       bne      $cad9
CAC7: ece4       ldd      , s
CAC9: bdbcd0     jsr      $bcd0
CACC: ece4       ldd      , s
CACE: bdc0dd     jsr      $c0dd
CAD1: bdbefb     jsr      $befb
CAD4: bdbefb     jsr      $befb
CAD7: 2009       bra      $cae2
CAD9: c104       cmpb     #4
CADB: 2605       bne      $cae2
CADD: ece4       ldd      , s
CADF: bdcfd0     jsr      $cfd0
CAE2: 4f         clra     
CAE3: d66a       ldb      $6a
CAE5: e7f4       stb      [, s]
CAE7: 3264       leas     4, s
CAE9: 39         rts      
CAEA: 3406       pshs     a, b
CAEC: 327e       leas     -2, s
CAEE: cc0012     ldd      #18
CAF1: bde76b     jsr      $e76b
CAF4: bdb636     jsr      $b636
CAF7: ae62       ldx      2, s
CAF9: ec04       ldd      4, x
CAFB: ede4       std      , s
CAFD: aee4       ldx      , s
CAFF: 6f84       clr      , x
CB01: e602       ldb      2, x
CB03: c181       cmpb     #-127
CB05: 10260082   lbne     $cb8b
CB09: 4f         clra     
CB0A: e60c       ldb      12, x
CB0C: c401       andb     #1
CB0E: 266f       bne      $cb7f
CB10: ece4       ldd      , s
CB12: bdc0dd     jsr      $c0dd
CB15: bdbefb     jsr      $befb
CB18: bdbefb     jsr      $befb
CB1B: 4f         clra     
CB1C: aee4       ldx      , s
CB1E: e60c       ldb      12, x
CB20: c401       andb     #1
CB22: 265b       bne      $cb7f
CB24: bdbefb     jsr      $befb
CB27: 3406       pshs     a, b
CB29: bdbefb     jsr      $befb
CB2C: 1f98       tfr      b, a
CB2E: 5f         clrb     
CB2F: e3e4       addd     , s
CB31: 3510       puls     x
CB33: 3406       pshs     a, b
CB35: ec68       ldd      8, s
CB37: bdbacb     jsr      $bacb
CB3A: 3262       leas     2, s
CB3C: ec66       ldd      6, s
CB3E: 3406       pshs     a, b
CB40: cccbc8     ldd      #-13368
CB43: bdb832     jsr      $b832
CB46: 3262       leas     2, s
CB48: ec66       ldd      6, s
CB4A: 3406       pshs     a, b
CB4C: ec68       ldd      8, s
CB4E: bdb7b1     jsr      $b7b1
CB51: e3e4       addd     , s
CB53: 3510       puls     x
CB55: ed66       std      6, s
CB57: bdbefb     jsr      $befb
CB5A: ae66       ldx      6, s
CB5C: e784       stb      , x
CB5E: d66a       ldb      $6a
CB60: 2613       bne      $cb75
CB62: e684       ldb      , x
CB64: 270f       beq      $cb75
CB66: c112       cmpb     #18
CB68: 2604       bne      $cb6e
CB6A: c620       ldb      #32
CB6C: e784       stb      , x
CB6E: ec66       ldd      6, s
CB70: c30001     addd     #1
CB73: 20e0       bra      $cb55
CB75: d66a       ldb      $6a
CB77: 264c       bne      $cbc5
CB79: c601       ldb      #1
CB7B: e7f4       stb      [, s]
CB7D: 2046       bra      $cbc5
CB7F: 6ff806     clr      [6, s]
CB82: c602       ldb      #2
CB84: e7f4       stb      [, s]
CB86: bdb63e     jsr      $b63e
CB89: 203a       bra      $cbc5
CB8B: ec66       ldd      6, s
CB8D: 3406       pshs     a, b
CB8F: ec62       ldd      2, s
CB91: bdcfef     jsr      $cfef
CB94: 3262       leas     2, s
CB96: 202d       bra      $cbc5
CB98: 3406       pshs     a, b
CB9A: 327e       leas     -2, s
CB9C: cc0013     ldd      #19
CB9F: bde76b     jsr      $e76b
CBA2: bdb636     jsr      $b636
CBA5: ae62       ldx      2, s
CBA7: ec04       ldd      4, x
CBA9: ede4       std      , s
CBAB: aee4       ldx      , s
CBAD: 6f84       clr      , x
CBAF: e602       ldb      2, x
CBB1: c181       cmpb     #-127
CBB3: 2607       bne      $cbbc
CBB5: ece4       ldd      , s
CBB7: bdbd11     jsr      $bd11
CBBA: 2009       bra      $cbc5
CBBC: c104       cmpb     #4
CBBE: 2605       bne      $cbc5
CBC0: ece4       ldd      , s
CBC2: bdd032     jsr      $d032
CBC5: 3264       leas     4, s
CBC7: 39         rts      
CBC8: 2000       bra      $cbca
CBCA: 41         fcb      $41
CBCB: 42         fcb      $42
CBCC: 43         coma     
CBCD: 44         lsra     
CBCE: 45         fcb      $45
CBCF: 46         rora     
CBD0: 47         asra     
CBD1: 48         lsla     
CBD2: 49         rola     
CBD3: 4a         deca     
CBD4: 4b         fcb      $4b
CBD5: 4c         inca     
CBD6: 4d         tsta     
CBD7: 4e         fcb      $4e
CBD8: 4f         clra     
CBD9: 50         negb     
CBDA: 3406       pshs     a, b
CBDC: cc004f     ldd      #79
CBDF: dd0a       std      $0a
CBE1: 4f         clra     
CBE2: 5f         clrb     
CBE3: dd0c       std      $0c
CBE5: dd0e       std      $0e
CBE7: 0f10       clr      $10
CBE9: dd08       std      $08
CBEB: c613       ldb      #19
CBED: d77d       stb      $7d
CBEF: c60d       ldb      #13
CBF1: d77b       stb      $7b
CBF3: c601       ldb      #1
CBF5: d77c       stb      $7c
CBF7: c611       ldb      #17
CBF9: 8e037c     ldx      #892
CBFC: e784       stb      , x
CBFE: e667       ldb      7, s
CC00: 3406       pshs     a, b
CC02: e667       ldb      7, s
CC04: 3406       pshs     a, b
CC06: ec64       ldd      4, s
CC08: 3406       pshs     a, b
CC0A: cceff0     ldd      #-4112
CC0D: bdd55c     jsr      $d55c
CC10: 3268       leas     8, s
CC12: 39         rts      
CC13: 3406       pshs     a, b
CC15: 3273       leas     -13, s
CC17: cc001f     ldd      #31
CC1A: bde76b     jsr      $e76b
CC1D: ec6d       ldd      13, s
CC1F: bdcddc     jsr      $cddc
CC22: cc0076     ldd      #118
CC25: bdce66     jsr      $ce66
CC28: cc0038     ldd      #56
CC2B: bdce66     jsr      $ce66
CC2E: cc0030     ldd      #48
CC31: bdce66     jsr      $ce66
CC34: bdd0ab     jsr      $d0ab
CC37: bdd047     jsr      $d047
CC3A: e7f80d     stb      [13, s]
CC3D: d66a       ldb      $6a
CC3F: 102600a0   lbne     $cce3
CC43: ec6d       ldd      13, s
CC45: bdcddc     jsr      $cddc
CC48: cc006f     ldd      #111
CC4B: bdce66     jsr      $ce66
CC4E: 4f         clra     
CC4F: e6e814     ldb      20, s
CC52: bdce66     jsr      $ce66
CC55: ae6d       ldx      13, s
CC57: e68810     ldb      16, x
CC5A: 2705       beq      $cc61
CC5C: cc0062     ldd      #98
CC5F: 2003       bra      $cc64
CC61: cc0074     ldd      #116
CC64: bdce66     jsr      $ce66
CC67: cc0020     ldd      #32
CC6A: bdce66     jsr      $ce66
CC6D: cc0028     ldd      #40
CC70: bdce66     jsr      $ce66
CC73: 4f         clra     
CC74: e6e812     ldb      18, s
CC77: bdce66     jsr      $ce66
CC7A: ae6d       ldx      13, s
CC7C: ec05       ldd      5, x
CC7E: 2732       beq      $ccb2
CC80: 3406       pshs     a, b
CC82: 1f41       tfr      s, x
CC84: c604       ldb      #4
CC86: 3a         abx      
CC87: 3410       pshs     x
CC89: 3506       puls     a, b
CC8B: bdbacb     jsr      $bacb
CC8E: 3262       leas     2, s
CC90: cc003a     ldd      #58
CC93: bdce66     jsr      $ce66
CC96: 1f41       tfr      s, x
CC98: c602       ldb      #2
CC9A: 3a         abx      
CC9B: 3410       pshs     x
CC9D: 3506       puls     a, b
CC9F: ede4       std      , s
CCA1: aee4       ldx      , s
CCA3: e684       ldb      , x
CCA5: 270b       beq      $ccb2
CCA7: 4f         clra     
CCA8: bdce66     jsr      $ce66
CCAB: ece4       ldd      , s
CCAD: c30001     addd     #1
CCB0: 20ed       bra      $cc9f
CCB2: cc0029     ldd      #41
CCB5: bdce66     jsr      $ce66
CCB8: 9e68       ldx      $68
CCBA: e684       ldb      , x
CCBC: 270c       beq      $ccca
CCBE: 4f         clra     
CCBF: bdce66     jsr      $ce66
CCC2: 9e68       ldx      $68
CCC4: 3001       leax     1, x
CCC6: 9f68       stx      $68
CCC8: 20ee       bra      $ccb8
CCCA: bdd0ab     jsr      $d0ab
CCCD: bdd054     jsr      $d054
CCD0: e7f80d     stb      [13, s]
CCD3: d66a       ldb      $6a
CCD5: 260c       bne      $cce3
CCD7: bdce8b     jsr      $ce8b
CCDA: ae6d       ldx      13, s
CCDC: e70b       stb      11, x
CCDE: 6f0c       clr      12, x
CCE0: bdd09f     jsr      $d09f
CCE3: 326f       leas     15, s
CCE5: 39         rts      
CCE6: 3406       pshs     a, b
CCE8: cc0020     ldd      #32
CCEB: bde76b     jsr      $e76b
CCEE: ece4       ldd      , s
CCF0: bdcdc2     jsr      $cdc2
CCF3: c602       ldb      #2
CCF5: d710       stb      $10
CCF7: dc0e       ldd      $0e
CCF9: 2614       bne      $cd0f
CCFB: cc0070     ldd      #112
CCFE: bdce66     jsr      $ce66
CD01: 4f         clra     
CD02: 9e08       ldx      $08
CD04: e60b       ldb      11, x
CD06: bdce66     jsr      $ce66
CD09: cc006e     ldd      #110
CD0C: bdce66     jsr      $ce66
CD0F: aee4       ldx      , s
CD11: e68810     ldb      16, x
CD14: 2708       beq      $cd1e
CD16: 4f         clra     
CD17: e665       ldb      5, s
CD19: bdce16     jsr      $ce16
CD1C: 2039       bra      $cd57
CD1E: 4f         clra     
CD1F: e665       ldb      5, s
CD21: bdce66     jsr      $ce66
CD24: 2031       bra      $cd57
CD26: 3406       pshs     a, b
CD28: cc0021     ldd      #33
CD2B: bde76b     jsr      $e76b
CD2E: ece4       ldd      , s
CD30: bdcdc2     jsr      $cdc2
CD33: dc0e       ldd      $0e
CD35: 2616       bne      $cd4d
CD37: cc0070     ldd      #112
CD3A: bdce66     jsr      $ce66
CD3D: 4f         clra     
CD3E: 9e08       ldx      $08
CD40: e60b       ldb      11, x
CD42: bdce66     jsr      $ce66
CD45: cc007a     ldd      #122
CD48: bdce66     jsr      $ce66
CD4B: 2005       bra      $cd52
CD4D: c67a       ldb      #122
CD4F: f70382     stb      $0382
CD52: 0f10       clr      $10
CD54: bdce0b     jsr      $ce0b
CD57: 3262       leas     2, s
CD59: 39         rts      
CD5A: 3406       pshs     a, b
CD5C: 327f       leas     -1, s
CD5E: cc0022     ldd      #34
CD61: bde76b     jsr      $e76b
CD64: ec61       ldd      1, s
CD66: 8d5a       bsr      $cdc2
CD68: 2714       beq      $cd7e
CD6A: 9e08       ldx      $08
CD6C: e60c       ldb      12, x
CD6E: 270e       beq      $cd7e
CD70: ccffff     ldd      #-1
CD73: bdcebe     jsr      $cebe
CD76: 9e08       ldx      $08
CD78: 6f0c       clr      12, x
CD7A: ec0d       ldd      13, x
CD7C: dd0e       std      $0e
CD7E: dc0e       ldd      $0e
CD80: 930c       subd     $0c
CD82: 2d05       blt      $cd89
CD84: 4f         clra     
CD85: 5f         clrb     
CD86: bdcebe     jsr      $cebe
CD89: c601       ldb      #1
CD8B: d710       stb      $10
CD8D: dc0e       ldd      $0e
CD8F: 930c       subd     $0c
CD91: 2c11       bge      $cda4
CD93: 9e08       ldx      $08
CD95: e68810     ldb      16, x
CD98: 2705       beq      $cd9f
CD9A: bdce4b     jsr      $ce4b
CD9D: 2007       bra      $cda6
CD9F: bdce8b     jsr      $ce8b
CDA2: 2002       bra      $cda6
CDA4: c620       ldb      #32
CDA6: e7e4       stb      , s
CDA8: dc0e       ldd      $0e
CDAA: 930c       subd     $0c
CDAC: 2628       bne      $cdd6
CDAE: 9e08       ldx      $08
CDB0: e60f       ldb      15, x
CDB2: c1ff       cmpb     #-1
CDB4: 2608       bne      $cdbe
CDB6: e684       ldb      , x
CDB8: 2604       bne      $cdbe
CDBA: c601       ldb      #1
CDBC: e784       stb      , x
CDBE: 0f10       clr      $10
CDC0: 2014       bra      $cdd6
CDC2: 3406       pshs     a, b
CDC4: 327f       leas     -1, s
CDC6: 6fe4       clr      , s
CDC8: dc08       ldd      $08
CDCA: a361       subd     1, s
CDCC: 2708       beq      $cdd6
CDCE: ec61       ldd      1, s
CDD0: 8d0a       bsr      $cddc
CDD2: c6ff       ldb      #-1
CDD4: e7e4       stb      , s
CDD6: 4f         clra     
CDD7: e6e4       ldb      , s
CDD9: 3263       leas     3, s
CDDB: 39         rts      
CDDC: 3406       pshs     a, b
CDDE: dc0e       ldd      $0e
CDE0: 2f0a       ble      $cdec
CDE2: d610       ldb      $10
CDE4: c102       cmpb     #2
CDE6: 2604       bne      $cdec
CDE8: 8d21       bsr      $ce0b
CDEA: 2010       bra      $cdfc
CDEC: d610       ldb      $10
CDEE: c101       cmpb     #1
CDF0: 260a       bne      $cdfc
CDF2: c6ff       ldb      #-1
CDF4: 9e08       ldx      $08
CDF6: e70c       stb      12, x
CDF8: dc0e       ldd      $0e
CDFA: ed0d       std      13, x
CDFC: 0f10       clr      $10
CDFE: 4f         clra     
CDFF: 5f         clrb     
CE00: dd0c       std      $0c
CE02: dd0e       std      $0e
CE04: ece4       ldd      , s
CE06: dd08       std      $08
CE08: 3262       leas     2, s
CE0A: 39         rts      
CE0B: bdd0ab     jsr      $d0ab
CE0E: bdd047     jsr      $d047
CE11: 9e08       ldx      $08
CE13: e784       stb      , x
CE15: 39         rts      
CE16: 3406       pshs     a, b
CE18: 327e       leas     -2, s
CE1A: ccd27b     ldd      #-11653
CE1D: ede4       std      , s
CE1F: 4f         clra     
CE20: e663       ldb      3, s
CE22: 44         lsra     
CE23: 56         rorb     
CE24: 44         lsra     
CE25: 56         rorb     
CE26: 44         lsra     
CE27: 56         rorb     
CE28: 44         lsra     
CE29: 56         rorb     
CE2A: 4f         clra     
CE2B: c40f       andb     #15
CE2D: e3e4       addd     , s
CE2F: 3406       pshs     a, b
CE31: 4f         clra     
CE32: 3510       puls     x
CE34: e684       ldb      , x
CE36: 8d2e       bsr      $ce66
CE38: 4f         clra     
CE39: e663       ldb      3, s
CE3B: c40f       andb     #15
CE3D: e3e4       addd     , s
CE3F: 3406       pshs     a, b
CE41: 4f         clra     
CE42: 3510       puls     x
CE44: e684       ldb      , x
CE46: 8d1e       bsr      $ce66
CE48: 3264       leas     4, s
CE4A: 39         rts      
CE4B: 8d3e       bsr      $ce8b
CE4D: bdba58     jsr      $ba58
CE50: 58         lslb     
CE51: 49         rola     
CE52: 58         lslb     
CE53: 49         rola     
CE54: 58         lslb     
CE55: 49         rola     
CE56: 58         lslb     
CE57: 49         rola     
CE58: 3406       pshs     a, b
CE5A: 8d2f       bsr      $ce8b
CE5C: bdba58     jsr      $ba58
CE5F: aae4       ora      , s
CE61: ea61       orb      1, s
CE63: 3510       puls     x
CE65: 39         rts      
CE66: 3406       pshs     a, b
CE68: dc0e       ldd      $0e
CE6A: 3406       pshs     a, b
CE6C: c30001     addd     #1
CE6F: dd0e       std      $0e
CE71: 3506       puls     a, b
CE73: c30380     addd     #896
CE76: 3406       pshs     a, b
CE78: 4f         clra     
CE79: e663       ldb      3, s
CE7B: 3510       puls     x
CE7D: e784       stb      , x
CE7F: dc0e       ldd      $0e
CE81: 930a       subd     $0a
CE83: 2d71       blt      $cef6
CE85: 8d84       bsr      $ce0b
CE87: 0f10       clr      $10
CE89: 206b       bra      $cef6
CE8B: 327f       leas     -1, s
CE8D: dc0e       ldd      $0e
CE8F: 930c       subd     $0c
CE91: 2d03       blt      $ce96
CE93: bdd11c     jsr      $d11c
CE96: dc0e       ldd      $0e
CE98: 3406       pshs     a, b
CE9A: c30001     addd     #1
CE9D: dd0e       std      $0e
CE9F: 3506       puls     a, b
CEA1: c30380     addd     #896
CEA4: 3406       pshs     a, b
CEA6: 4f         clra     
CEA7: 3510       puls     x
CEA9: e684       ldb      , x
CEAB: e7e4       stb      , s
CEAD: dc0e       ldd      $0e
CEAF: 930c       subd     $0c
CEB1: 2d08       blt      $cebb
CEB3: 0f10       clr      $10
CEB5: 4f         clra     
CEB6: 5f         clrb     
CEB7: dd0c       std      $0c
CEB9: dd0e       std      $0e
CEBB: 7ed18c     jmp      $d18c
CEBE: 3406       pshs     a, b
CEC0: cc0067     ldd      #103
CEC3: 8da1       bsr      $ce66
CEC5: 4f         clra     
CEC6: 9e08       ldx      $08
CEC8: e60b       ldb      11, x
CECA: 8d9a       bsr      $ce66
CECC: e661       ldb      1, s
CECE: 2705       beq      $ced5
CED0: cc006c     ldd      #108
CED3: 8d91       bsr      $ce66
CED5: bdd0ab     jsr      $d0ab
CED8: bdd054     jsr      $d054
CEDB: 9e08       ldx      $08
CEDD: e784       stb      , x
CEDF: d66a       ldb      $6a
CEE1: 2613       bne      $cef6
CEE3: 8da6       bsr      $ce8b
CEE5: 83007a     subd     #122
CEE8: 2608       bne      $cef2
CEEA: c6ff       ldb      #-1
CEEC: 9e08       ldx      $08
CEEE: e70f       stb      15, x
CEF0: 2004       bra      $cef6
CEF2: 9e08       ldx      $08
CEF4: 6f0f       clr      15, x
CEF6: 7ed09c     jmp      $d09c
CEF9: 3406       pshs     a, b
CEFB: 3273       leas     -13, s
CEFD: cc0023     ldd      #35
CF00: bde76b     jsr      $e76b
CF03: ec6d       ldd      13, s
CF05: bdcddc     jsr      $cddc
CF08: ece811     ldd      17, s
CF0B: 3406       pshs     a, b
CF0D: 1f41       tfr      s, x
CF0F: c604       ldb      #4
CF11: 3a         abx      
CF12: 3410       pshs     x
CF14: 3506       puls     a, b
CF16: bdbacb     jsr      $bacb
CF19: 3262       leas     2, s
CF1B: cc0072     ldd      #114
CF1E: bdce66     jsr      $ce66
CF21: 1f41       tfr      s, x
CF23: c602       ldb      #2
CF25: 3a         abx      
CF26: 3410       pshs     x
CF28: 3506       puls     a, b
CF2A: ede4       std      , s
CF2C: aee4       ldx      , s
CF2E: e684       ldb      , x
CF30: 270b       beq      $cf3d
CF32: 4f         clra     
CF33: bdce66     jsr      $ce66
CF36: ece4       ldd      , s
CF38: c30001     addd     #1
CF3B: 20ed       bra      $cf2a
CF3D: bdd0ab     jsr      $d0ab
CF40: bdd047     jsr      $d047
CF43: e7f80d     stb      [13, s]
CF46: 326f       leas     15, s
CF48: 39         rts      
CF49: 3406       pshs     a, b
CF4B: cc0024     ldd      #36
CF4E: bde76b     jsr      $e76b
CF51: ece4       ldd      , s
CF53: bdcddc     jsr      $cddc
CF56: cc0063     ldd      #99
CF59: bdce66     jsr      $ce66
CF5C: 4f         clra     
CF5D: aee4       ldx      , s
CF5F: e60b       ldb      11, x
CF61: 7ed03c     jmp      $d03c
CF64: 3406       pshs     a, b
CF66: ece4       ldd      , s
CF68: bdcddc     jsr      $cddc
CF6B: cc0079     ldd      #121
CF6E: bdce66     jsr      $ce66
CF71: 9e68       ldx      $68
CF73: e684       ldb      , x
CF75: 102700c6   lbeq     $d03f
CF79: 4f         clra     
CF7A: bdce66     jsr      $ce66
CF7D: 9e68       ldx      $68
CF7F: 3001       leax     1, x
CF81: 9f68       stx      $68
CF83: 20ec       bra      $cf71
CF85: 3406       pshs     a, b
CF87: ece4       ldd      , s
CF89: bdcddc     jsr      $cddc
CF8C: cc0077     ldd      #119
CF8F: bdce66     jsr      $ce66
CF92: 9e68       ldx      $68
CF94: e684       ldb      , x
CF96: 270c       beq      $cfa4
CF98: 4f         clra     
CF99: bdce66     jsr      $ce66
CF9C: 9e68       ldx      $68
CF9E: 3001       leax     1, x
CFA0: 9f68       stx      $68
CFA2: 20ee       bra      $cf92
CFA4: bdd0ab     jsr      $d0ab
CFA7: bdd047     jsr      $d047
CFAA: e7f4       stb      [, s]
CFAC: d66a       ldb      $6a
CFAE: 102600ea   lbne     $d09c
CFB2: ece4       ldd      , s
CFB4: bdcddc     jsr      $cddc
CFB7: cc0062     ldd      #98
CFBA: bdce66     jsr      $ce66
CFBD: ae64       ldx      4, s
CFBF: e684       ldb      , x
CFC1: 277c       beq      $d03f
CFC3: 4f         clra     
CFC4: bdce66     jsr      $ce66
CFC7: ec64       ldd      4, s
CFC9: c30001     addd     #1
CFCC: ed64       std      4, s
CFCE: 20ed       bra      $cfbd
CFD0: 3406       pshs     a, b
CFD2: ece4       ldd      , s
CFD4: bdcddc     jsr      $cddc
CFD7: cc0064     ldd      #100
CFDA: bdce66     jsr      $ce66
CFDD: 9e68       ldx      $68
CFDF: e684       ldb      , x
CFE1: 275c       beq      $d03f
CFE3: 4f         clra     
CFE4: bdce66     jsr      $ce66
CFE7: 9e68       ldx      $68
CFE9: 3001       leax     1, x
CFEB: 9f68       stx      $68
CFED: 20ee       bra      $cfdd
CFEF: 3406       pshs     a, b
CFF1: 327e       leas     -2, s
CFF3: ec62       ldd      2, s
CFF5: bdcddc     jsr      $cddc
CFF8: 4f         clra     
CFF9: 5f         clrb     
CFFA: ede4       std      , s
CFFC: cc0066     ldd      #102
CFFF: bdce66     jsr      $ce66
D002: bdd0ab     jsr      $d0ab
D005: 8d4d       bsr      $d054
D007: e7f802     stb      [2, s]
D00A: d66a       ldb      $6a
D00C: 261f       bne      $d02d
D00E: dc0e       ldd      $0e
D010: 930c       subd     $0c
D012: 2c16       bge      $d02a
D014: ece4       ldd      , s
D016: c30001     addd     #1
D019: ede4       std      , s
D01B: bdce8b     jsr      $ce8b
D01E: e7f806     stb      [6, s]
D021: ec66       ldd      6, s
D023: c30001     addd     #1
D026: ed66       std      6, s
D028: 20e4       bra      $d00e
D02A: 6ff806     clr      [6, s]
D02D: ece4       ldd      , s
D02F: 3264       leas     4, s
D031: 39         rts      
D032: 3406       pshs     a, b
D034: ece4       ldd      , s
D036: bdcddc     jsr      $cddc
D039: cc006b     ldd      #107
D03C: bdce66     jsr      $ce66
D03F: 8d6a       bsr      $d0ab
D041: 8d04       bsr      $d047
D043: e7f4       stb      [, s]
D045: 2055       bra      $d09c
D047: 327f       leas     -1, s
D049: 8d09       bsr      $d054
D04B: e7e4       stb      , s
D04D: 2602       bne      $d051
D04F: 8d4e       bsr      $d09f
D051: 7ed18c     jmp      $d18c
D054: 327f       leas     -1, s
D056: d66a       ldb      $6a
D058: c103       cmpb     #3
D05A: 271f       beq      $d07b
D05C: bdce8b     jsr      $ce8b
D05F: e7e4       stb      , s
D061: c162       cmpb     #98
D063: 2605       bne      $d06a
D065: bdb636     jsr      $b636
D068: 2011       bra      $d07b
D06A: c165       cmpb     #101
D06C: 2607       bne      $d075
D06E: bdb63e     jsr      $b63e
D071: 8d2c       bsr      $d09f
D073: 2006       bra      $d07b
D075: c603       ldb      #3
D077: d76a       stb      $6a
D079: 8d06       bsr      $d081
D07B: 4f         clra     
D07C: d66a       ldb      $6a
D07E: 3261       leas     1, s
D080: 39         rts      
D081: 327e       leas     -2, s
D083: cc0300     ldd      #768
D086: ede4       std      , s
D088: dc0e       ldd      $0e
D08A: 930c       subd     $0c
D08C: 2c0c       bge      $d09a
D08E: bdce8b     jsr      $ce8b
D091: e7f4       stb      [, s]
D093: ece4       ldd      , s
D095: c30001     addd     #1
D098: 20ec       bra      $d086
D09A: 6ff4       clr      [, s]
D09C: 3262       leas     2, s
D09E: 39         rts      
D09F: dc0e       ldd      $0e
D0A1: 930c       subd     $0c
D0A3: 2c05       bge      $d0aa
D0A5: bdce8b     jsr      $ce8b
D0A8: 20f5       bra      $d09f
D0AA: 39         rts      
D0AB: 327c       leas     -4, s
D0AD: cc0025     ldd      #37
D0B0: bde76b     jsr      $e76b
D0B3: 6f61       clr      1, s
D0B5: 4f         clra     
D0B6: 5f         clrb     
D0B7: ed62       std      2, s
D0B9: ec62       ldd      2, s
D0BB: 930e       subd     $0e
D0BD: 2c25       bge      $d0e4
D0BF: ec62       ldd      2, s
D0C1: c30380     addd     #896
D0C4: 3406       pshs     a, b
D0C6: 4f         clra     
D0C7: 3510       puls     x
D0C9: e684       ldb      , x
D0CB: e7e4       stb      , s
D0CD: e661       ldb      1, s
D0CF: ebe4       addb     , s
D0D1: 8900       adca     #0
D0D3: e761       stb      1, s
D0D5: ec62       ldd      2, s
D0D7: c30001     addd     #1
D0DA: ed62       std      2, s
D0DC: 4f         clra     
D0DD: e6e4       ldb      , s
D0DF: bdd192     jsr      $d192
D0E2: 20d5       bra      $d0b9
D0E4: 4f         clra     
D0E5: e661       ldb      1, s
D0E7: c40f       andb     #15
D0E9: e761       stb      1, s
D0EB: c3cbca     addd     #-13366
D0EE: 3406       pshs     a, b
D0F0: 4f         clra     
D0F1: 3510       puls     x
D0F3: e684       ldb      , x
D0F5: bdd192     jsr      $d192
D0F8: 4f         clra     
D0F9: d67b       ldb      $7b
D0FB: bdd192     jsr      $d192
D0FE: 4f         clra     
D0FF: f60380     ldb      $0380
D102: e7e4       stb      , s
D104: 8d16       bsr      $d11c
D106: f60380     ldb      $0380
D109: c14e       cmpb     #78
D10B: 260c       bne      $d119
D10D: 4f         clra     
D10E: e6e4       ldb      , s
D110: f70380     stb      $0380
D113: ec62       ldd      2, s
D115: dd0e       std      $0e
D117: 209a       bra      $d0b3
D119: 3264       leas     4, s
D11B: 39         rts      
D11C: 327f       leas     -1, s
D11E: cc0026     ldd      #38
D121: bde76b     jsr      $e76b
D124: 4f         clra     
D125: 5f         clrb     
D126: dd0e       std      $0e
D128: dd0c       std      $0c
D12A: 8d4d       bsr      $d179
D12C: e7e4       stb      , s
D12E: d66a       ldb      $6a
D130: c103       cmpb     #3
D132: 275b       beq      $d18f
D134: e6e4       ldb      , s
D136: c10d       cmpb     #13
D138: 2719       beq      $d153
D13A: dc0c       ldd      $0c
D13C: 3406       pshs     a, b
D13E: c30001     addd     #1
D141: dd0c       std      $0c
D143: 3506       puls     a, b
D145: c30380     addd     #896
D148: 3406       pshs     a, b
D14A: 4f         clra     
D14B: e662       ldb      2, s
D14D: 3510       puls     x
D14F: e784       stb      , x
D151: 20d7       bra      $d12a
D153: f60380     ldb      $0380
D156: c14e       cmpb     #78
D158: 2735       beq      $d18f
D15A: dc0c       ldd      $0c
D15C: c3ffff     addd     #-1
D15F: dd0c       std      $0c
D161: 3406       pshs     a, b
D163: cc0380     ldd      #896
D166: bdd23f     jsr      $d23f
D169: 3262       leas     2, s
D16B: 2622       bne      $d18f
D16D: cc004e     ldd      #78
D170: 8d20       bsr      $d192
D172: 4f         clra     
D173: d67b       ldb      $7b
D175: 8d1b       bsr      $d192
D177: 20ab       bra      $d124
D179: 327f       leas     -1, s
D17B: dc08       ldd      $08
D17D: bdd5bb     jsr      $d5bb
D180: e7e4       stb      , s
D182: d17b       cmpb     $7b
D184: 2606       bne      $d18c
D186: 8d2e       bsr      $d1b6
D188: c60d       ldb      #13
D18A: e7e4       stb      , s
D18C: 4f         clra     
D18D: e6e4       ldb      , s
D18F: 3261       leas     1, s
D191: 39         rts      
D192: 3406       pshs     a, b
D194: 4f         clra     
D195: e661       ldb      1, s
D197: 3406       pshs     a, b
D199: dc08       ldd      $08
D19B: bdd598     jsr      $d598
D19E: 3262       leas     2, s
D1A0: e661       ldb      1, s
D1A2: d17b       cmpb     $7b
D1A4: 260d       bne      $d1b3
D1A6: d66a       ldb      $6a
D1A8: 2609       bne      $d1b3
D1AA: dc08       ldd      $08
D1AC: bdd5bb     jsr      $d5bb
D1AF: d17d       cmpb     $7d
D1B1: 26f3       bne      $d1a6
D1B3: 3262       leas     2, s
D1B5: 39         rts      
D1B6: 3279       leas     -7, s
D1B8: 4f         clra     
D1B9: 5f         clrb     
D1BA: ede4       std      , s
D1BC: 3406       pshs     a, b
D1BE: 1f41       tfr      s, x
D1C0: c604       ldb      #4
D1C2: 3a         abx      
D1C3: 3410       pshs     x
D1C5: 3506       puls     a, b
D1C7: e3e4       addd     , s
D1C9: 3510       puls     x
D1CB: 3406       pshs     a, b
D1CD: dc08       ldd      $08
D1CF: bdd5bb     jsr      $d5bb
D1D2: 3510       puls     x
D1D4: e784       stb      , x
D1D6: ece4       ldd      , s
D1D8: c30001     addd     #1
D1DB: ede4       std      , s
D1DD: d17c       cmpb     $7c
D1DF: 26db       bne      $d1bc
D1E1: c3ffff     addd     #-1
D1E4: ede4       std      , s
D1E6: 4f         clra     
D1E7: d67c       ldb      $7c
D1E9: 3406       pshs     a, b
D1EB: cc037c     ldd      #892
D1EE: 3406       pshs     a, b
D1F0: 1f41       tfr      s, x
D1F2: c606       ldb      #6
D1F4: 3a         abx      
D1F5: 3410       pshs     x
D1F7: 3506       puls     a, b
D1F9: bdb7ce     jsr      $b7ce
D1FC: 3264       leas     4, s
D1FE: 263c       bne      $d23c
D200: ece4       ldd      , s
D202: 3406       pshs     a, b
D204: 1f41       tfr      s, x
D206: c604       ldb      #4
D208: 3a         abx      
D209: 3410       pshs     x
D20B: 1f41       tfr      s, x
D20D: c606       ldb      #6
D20F: 3a         abx      
D210: 3410       pshs     x
D212: 3506       puls     a, b
D214: c30001     addd     #1
D217: bdbb68     jsr      $bb68
D21A: 3264       leas     4, s
D21C: ece4       ldd      , s
D21E: 3406       pshs     a, b
D220: 1f41       tfr      s, x
D222: c604       ldb      #4
D224: 3a         abx      
D225: 3410       pshs     x
D227: 3506       puls     a, b
D229: e3e4       addd     , s
D22B: 3510       puls     x
D22D: 3406       pshs     a, b
D22F: dc08       ldd      $08
D231: bdd5bb     jsr      $d5bb
D234: 3510       puls     x
D236: e784       stb      , x
D238: d66a       ldb      $6a
D23A: 27aa       beq      $d1e6
D23C: 3267       leas     7, s
D23E: 39         rts      
D23F: 3406       pshs     a, b
D241: 327f       leas     -1, s
D243: 6fe4       clr      , s
D245: 4f         clra     
D246: e6e4       ldb      , s
D248: ebf801     addb     [1, s]
D24B: 8900       adca     #0
D24D: e7e4       stb      , s
D24F: ec61       ldd      1, s
D251: c30001     addd     #1
D254: ed61       std      1, s
D256: ec65       ldd      5, s
D258: c3ffff     addd     #-1
D25B: ed65       std      5, s
D25D: 2ee6       bgt      $d245
D25F: 4f         clra     
D260: e6e4       ldb      , s
D262: c40f       andb     #15
D264: c3cbca     addd     #-13366
D267: 3406       pshs     a, b
D269: e6f803     ldb      [3, s]
D26C: 3510       puls     x
D26E: e184       cmpb     , x
D270: 2603       bne      $d275
D272: 86ff       lda      #-1
D274: 214f       brn      $d2c5
D276: 1f89       tfr      a, b
D278: 3263       leas     3, s
D27A: 39         rts      
D27B: 3031       leax     -15, y
D27D: 3233       leas     -13, y
D27F: 3435       pshs     cc, b, x, y
D281: 3637       pshu     cc, a, b, x, y
D283: 38         fcb      $38
D284: 39         rts      
D285: 41         fcb      $41
D286: 42         fcb      $42
D287: 43         coma     
D288: 44         lsra     
D289: 45         fcb      $45
D28A: 46         rora     
D28B: 0034       neg      $34
D28D: 06ae       ror      $ae
D28F: e4e6       andb     a, s
D291: 02         fcb      $02
D292: c181       cmpb     #-127
D294: 2672       bne      $d308
D296: 4f         clra     
D297: e60c       ldb      12, x
D299: c440       andb     #64
D29B: 276b       beq      $d308
D29D: e601       ldb      1, x
D29F: c440       andb     #64
D2A1: 2710       beq      $d2b3
D2A3: c601       ldb      #1
D2A5: e70a       stb      10, x
D2A7: dc68       ldd      $68
D2A9: 3406       pshs     a, b
D2AB: ccd3fb     ldd      #-11269
D2AE: bdb80a     jsr      $b80a
D2B1: 204a       bra      $d2fd
D2B3: e601       ldb      1, x
D2B5: c480       andb     #-128
D2B7: 2704       beq      $d2bd
D2B9: 6f0a       clr      10, x
D2BB: 2042       bra      $d2ff
D2BD: e601       ldb      1, x
D2BF: c401       andb     #1
D2C1: 2709       beq      $d2cc
D2C3: dc68       ldd      $68
D2C5: 3406       pshs     a, b
D2C7: ccd3fd     ldd      #-11267
D2CA: 202e       bra      $d2fa
D2CC: e601       ldb      1, x
D2CE: c404       andb     #4
D2D0: 2709       beq      $d2db
D2D2: dc68       ldd      $68
D2D4: 3406       pshs     a, b
D2D6: ccd400     ldd      #-11264
D2D9: 201f       bra      $d2fa
D2DB: e601       ldb      1, x
D2DD: c402       andb     #2
D2DF: 271e       beq      $d2ff
D2E1: e60c       ldb      12, x
D2E3: c408       andb     #8
D2E5: 2618       bne      $d2ff
D2E7: dc68       ldd      $68
D2E9: 3406       pshs     a, b
D2EB: ccd3fb     ldd      #-11269
D2EE: bdb80a     jsr      $b80a
D2F1: 3262       leas     2, s
D2F3: dc68       ldd      $68
D2F5: 3406       pshs     a, b
D2F7: ccd403     ldd      #-11261
D2FA: bdb832     jsr      $b832
D2FD: 3262       leas     2, s
D2FF: 4f         clra     
D300: aee4       ldx      , s
D302: e60c       ldb      12, x
D304: c4f9       andb     #-7
D306: e70c       stb      12, x
D308: e602       ldb      2, x
D30A: c181       cmpb     #-127
D30C: 260f       bne      $d31d
D30E: 4f         clra     
D30F: e60c       ldb      12, x
D311: c408       andb     #8
D313: 2708       beq      $d31d
D315: ece4       ldd      , s
D317: bdbd51     jsr      $bd51
D31A: 7ed3f8     jmp      $d3f8
D31D: ece4       ldd      , s
D31F: bdbcd0     jsr      $bcd0
D322: aee4       ldx      , s
D324: e602       ldb      2, x
D326: c182       cmpb     #-126
D328: 102600cc   lbne     $d3f8
D32C: 6f0b       clr      11, x
D32E: 7ed3ee     jmp      $d3ee
D331: 3406       pshs     a, b
D333: ece4       ldd      , s
D335: bdbd11     jsr      $bd11
D338: 3262       leas     2, s
D33A: 39         rts      
D33B: 3406       pshs     a, b
D33D: 327f       leas     -1, s
D33F: 4f         clra     
D340: ae61       ldx      1, s
D342: e60c       ldb      12, x
D344: c401       andb     #1
D346: 2709       beq      $d351
D348: c602       ldb      #2
D34A: e784       stb      , x
D34C: bdb63e     jsr      $b63e
D34F: 2024       bra      $d375
D351: e60c       ldb      12, x
D353: c4f9       andb     #-7
D355: e70c       stb      12, x
D357: bdbefb     jsr      $befb
D35A: e7e4       stb      , s
D35C: 4f         clra     
D35D: ae61       ldx      1, s
D35F: e60c       ldb      12, x
D361: c401       andb     #1
D363: 2710       beq      $d375
D365: e60c       ldb      12, x
D367: c408       andb     #8
D369: 270a       beq      $d375
D36B: c601       ldb      #1
D36D: e784       stb      , x
D36F: e60c       ldb      12, x
D371: c4fe       andb     #-2
D373: e70c       stb      12, x
D375: 4f         clra     
D376: e6e4       ldb      , s
D378: 3263       leas     3, s
D37A: 39         rts      
D37B: 3406       pshs     a, b
D37D: 4f         clra     
D37E: aee4       ldx      , s
D380: e60c       ldb      12, x
D382: c4f9       andb     #-7
D384: e70c       stb      12, x
D386: e602       ldb      2, x
D388: c182       cmpb     #-126
D38A: 264a       bne      $d3d6
D38C: e60b       ldb      11, x
D38E: 2708       beq      $d398
D390: 6f0b       clr      11, x
D392: cc0011     ldd      #17
D395: bdbe6f     jsr      $be6f
D398: e665       ldb      5, s
D39A: c141       cmpb     #65
D39C: 2504       bcs      $d3a2
D39E: c15a       cmpb     #90
D3A0: 231d       bls      $d3bf
D3A2: c161       cmpb     #97
D3A4: 2509       bcs      $d3af
D3A6: c17a       cmpb     #122
D3A8: 2205       bhi      $d3af
D3AA: 4f         clra     
D3AB: c4df       andb     #-33
D3AD: 2013       bra      $d3c2
D3AF: c15f       cmpb     #95
D3B1: 2604       bne      $d3b7
D3B3: c664       ldb      #100
D3B5: 200b       bra      $d3c2
D3B7: c15b       cmpb     #91
D3B9: 250b       bcs      $d3c6
D3BB: c15e       cmpb     #94
D3BD: 2207       bhi      $d3c6
D3BF: 4f         clra     
D3C0: ca80       orb      #-128
D3C2: e765       stb      5, s
D3C4: 2010       bra      $d3d6
D3C6: c10d       cmpb     #13
D3C8: 260c       bne      $d3d6
D3CA: aee4       ldx      , s
D3CC: e60a       ldb      10, x
D3CE: c102       cmpb     #2
D3D0: 2404       bcc      $d3d6
D3D2: c6ff       ldb      #-1
D3D4: e70b       stb      11, x
D3D6: 4f         clra     
D3D7: e665       ldb      5, s
D3D9: bdbe6f     jsr      $be6f
D3DC: 201a       bra      $d3f8
D3DE: 3406       pshs     a, b
D3E0: cc000d     ldd      #13
D3E3: bdbe5f     jsr      $be5f
D3E6: aee4       ldx      , s
D3E8: e602       ldb      2, x
D3EA: c182       cmpb     #-126
D3EC: 260a       bne      $d3f8
D3EE: e60a       ldb      10, x
D3F0: c102       cmpb     #2
D3F2: 2404       bcc      $d3f8
D3F4: c6ff       ldb      #-1
D3F6: e70b       stb      11, x
D3F8: 3262       leas     2, s
D3FA: 39         rts      
D3FB: 40         nega     
D3FC: 002c       neg      $2c
D3FE: 52         fcb      $52
D3FF: 002c       neg      $2c
D401: 41         fcb      $41
D402: 002c       neg      $2c
D404: 57         asrb     
D405: 00cc       neg      $cc
D407: 05         fcb      $05
D408: 7fdd75     clr      $dd75
D40B: dd73       std      $73
D40D: 9e73       ldx      $73
D40F: 6f84       clr      , x
D411: 39         rts      
D412: 327a       leas     -6, s
D414: bdb636     jsr      $b636
D417: dc75       ldd      $75
D419: ede4       std      , s
D41B: ece4       ldd      , s
D41D: 9373       subd     $73
D41F: 270d       beq      $d42e
D421: ece4       ldd      , s
D423: c3ffe6     addd     #-26
D426: ede4       std      , s
D428: e6f4       ldb      [, s]
D42A: c101       cmpb     #1
D42C: 26ed       bne      $d41b
D42E: e6f4       ldb      [, s]
D430: 2610       bne      $d442
D432: ece4       ldd      , s
D434: c3ffe6     addd     #-26
D437: ede4       std      , s
D439: 830480     subd     #1152
D43C: 2537       bcs      $d475
D43E: ece4       ldd      , s
D440: dd73       std      $73
D442: 6ff4       clr      [, s]
D444: ece4       ldd      , s
D446: c30001     addd     #1
D449: ed62       std      2, s
D44B: c30008     addd     #8
D44E: ed64       std      4, s
D450: dc6b       ldd      $6b
D452: 2606       bne      $d45a
D454: ec62       ldd      2, s
D456: ae62       ldx      2, s
D458: 200c       bra      $d466
D45A: 9e6b       ldx      $6b
D45C: ec06       ldd      6, x
D45E: ae62       ldx      2, s
D460: ed06       std      6, x
D462: ec62       ldd      2, s
D464: 9e6b       ldx      $6b
D466: ed06       std      6, x
D468: ec64       ldd      4, s
D46A: ae62       ldx      2, s
D46C: ed04       std      4, x
D46E: 6f03       clr      3, x
D470: 6ff804     clr      [4, s]
D473: 200a       bra      $d47f
D475: 4f         clra     
D476: 5f         clrb     
D477: ed62       std      2, s
D479: ccd4be     ldd      #-11074
D47C: bdb653     jsr      $b653
D47F: ec62       ldd      2, s
D481: 2038       bra      $d4bb
D483: 3406       pshs     a, b
D485: 327c       leas     -4, s
D487: ec64       ldd      4, s
D489: c3ffff     addd     #-1
D48C: ede4       std      , s
D48E: ec64       ldd      4, s
D490: ed62       std      2, s
D492: ae62       ldx      2, s
D494: ec06       ldd      6, x
D496: a364       subd     4, s
D498: 2704       beq      $d49e
D49A: ec06       ldd      6, x
D49C: 20f2       bra      $d490
D49E: ae64       ldx      4, s
D4A0: ec06       ldd      6, x
D4A2: ae62       ldx      2, s
D4A4: ed06       std      6, x
D4A6: c601       ldb      #1
D4A8: e7f4       stb      [, s]
D4AA: 9e73       ldx      $73
D4AC: e684       ldb      , x
D4AE: c101       cmpb     #1
D4B0: 2609       bne      $d4bb
D4B2: dc73       ldd      $73
D4B4: c3001a     addd     #26
D4B7: dd73       std      $73
D4B9: 20ef       bra      $d4aa
D4BB: 3266       leas     6, s
D4BD: 39         rts      
D4BE: 6f75       clr      -11, s
D4C0: 74206f     lsr      $206f
D4C3: 6620       ror      0, y
D4C5: 6d65       tst      5, s
D4C7: 6d6f       tst      15, s
D4C9: 72         fcb      $72
D4CA: 7900bd     rol      >$00bd
D4CD: d63a       ldb      $3a
D4CF: 7edd48     jmp      $dd48
D4D2: 3406       pshs     a, b
D4D4: 327f       leas     -1, s
D4D6: cc0027     ldd      #39
D4D9: bde76b     jsr      $e76b
D4DC: bdd6a7     jsr      $d6a7
D4DF: e7e4       stb      , s
D4E1: c10d       cmpb     #13
D4E3: 2605       bne      $d4ea
D4E5: c601       ldb      #1
D4E7: e7f801     stb      [1, s]
D4EA: 4f         clra     
D4EB: e6e4       ldb      , s
D4ED: 3263       leas     3, s
D4EF: 39         rts      
D4F0: 3406       pshs     a, b
D4F2: cc0028     ldd      #40
D4F5: bde76b     jsr      $e76b
D4F8: 4f         clra     
D4F9: e661       ldb      1, s
D4FB: bdd714     jsr      $d714
D4FE: 2039       bra      $d539
D500: cc0029     ldd      #41
D503: bde76b     jsr      $e76b
D506: 7ed793     jmp      $d793
D509: 3406       pshs     a, b
D50B: cc002a     ldd      #42
D50E: bde76b     jsr      $e76b
D511: ece4       ldd      , s
D513: bdd7be     jsr      $d7be
D516: 2021       bra      $d539
D518: cc002b     ldd      #43
D51B: bde76b     jsr      $e76b
D51E: 7eddd8     jmp      $ddd8
D521: 3406       pshs     a, b
D523: ece4       ldd      , s
D525: 830001     subd     #1
D528: 2605       bne      $d52f
D52A: bdd695     jsr      $d695
D52D: 200a       bra      $d539
D52F: ece4       ldd      , s
D531: 830002     subd     #2
D534: 2603       bne      $d539
D536: bdd69c     jsr      $d69c
D539: 3262       leas     2, s
D53B: 39         rts      
D53C: 0000       neg      $00
D53E: 0032       neg      $32
D540: 004b       neg      $4b
D542: 006e       neg      $6e
D544: 0087       neg      $87
D546: 0096       neg      $96
D548: 01         fcb      $01
D549: 2c02       bge      $d54d
D54B: 58         lslb     
D54C: 04b0       lsr      $b0
D54E: 0708       asr      $08
D550: 0960       rol      $60
D552: 0e10       jmp      $10
D554: 12         nop      
D555: c01c       subb     #28
D557: 2025       bra      $d57e
D559: 804b       suba     #75
D55B: 0034       neg      $34
D55D: 0632       ror      $32
D55F: 7f6fe4     clr      $6fe4
D562: 4f         clra     
D563: e6e4       ldb      , s
D565: 58         lslb     
D566: 49         rola     
D567: c3d53c     addd     #-10948
D56A: 3406       pshs     a, b
D56C: 3510       puls     x
D56E: ec84       ldd      , x
D570: a365       subd     5, s
D572: 270c       beq      $d580
D574: 4f         clra     
D575: e6e4       ldb      , s
D577: c30001     addd     #1
D57A: e7e4       stb      , s
D57C: c10f       cmpb     #15
D57E: 26e2       bne      $d562
D580: c6ff       ldb      #-1
D582: ae61       ldx      1, s
D584: e701       stb      1, x
D586: cc000b     ldd      #11
D589: ea68       orb      8, s
D58B: e702       stb      2, x
D58D: cc0030     ldd      #48
D590: eae4       orb      , s
D592: ea6a       orb      10, s
D594: e703       stb      3, x
D596: 2044       bra      $d5dc
D598: 3406       pshs     a, b
D59A: e6f4       ldb      [, s]
D59C: 261a       bne      $d5b8
D59E: cc0010     ldd      #16
D5A1: 3406       pshs     a, b
D5A3: ec62       ldd      2, s
D5A5: 8d38       bsr      $d5df
D5A7: 3262       leas     2, s
D5A9: 260d       bne      $d5b8
D5AB: aee4       ldx      , s
D5AD: ae09       ldx      9, x
D5AF: 3410       pshs     x
D5B1: 4f         clra     
D5B2: e667       ldb      7, s
D5B4: 3510       puls     x
D5B6: e784       stb      , x
D5B8: 3262       leas     2, s
D5BA: 39         rts      
D5BB: 3406       pshs     a, b
D5BD: 327f       leas     -1, s
D5BF: e6f801     ldb      [1, s]
D5C2: 2615       bne      $d5d9
D5C4: cc0008     ldd      #8
D5C7: 3406       pshs     a, b
D5C9: ec63       ldd      3, s
D5CB: 8d12       bsr      $d5df
D5CD: 3262       leas     2, s
D5CF: 2608       bne      $d5d9
D5D1: ae61       ldx      1, s
D5D3: 4f         clra     
D5D4: e69809     ldb      [9, x]
D5D7: e7e4       stb      , s
D5D9: 4f         clra     
D5DA: e6e4       ldb      , s
D5DC: 3263       leas     3, s
D5DE: 39         rts      
D5DF: 3406       pshs     a, b
D5E1: 327d       leas     -3, s
D5E3: ae63       ldx      3, s
D5E5: ec09       ldd      9, x
D5E7: ed61       std      1, s
D5E9: 6fe4       clr      , s
D5EB: ec07       ldd      7, x
D5ED: dd06       std      $06
D5EF: 260b       bne      $d5fc
D5F1: 4f         clra     
D5F2: ae61       ldx      1, s
D5F4: e601       ldb      1, x
D5F6: e468       andb     8, s
D5F8: 27f7       beq      $d5f1
D5FA: 202c       bra      $d628
D5FC: cc0a60     ldd      #2656
D5FF: dd04       std      $04
D601: 4f         clra     
D602: ae61       ldx      1, s
D604: e601       ldb      1, x
D606: e468       andb     8, s
D608: 261e       bne      $d628
D60A: dc04       ldd      $04
D60C: c3ffff     addd     #-1
D60F: dd04       std      $04
D611: 26ee       bne      $d601
D613: dc06       ldd      $06
D615: c3ffff     addd     #-1
D618: dd06       std      $06
D61A: 26e0       bne      $d5fc
D61C: bdb650     jsr      $b650
D61F: c603       ldb      #3
D621: e7f803     stb      [3, s]
D624: c6ff       ldb      #-1
D626: e7e4       stb      , s
D628: 4f         clra     
D629: e6e4       ldb      , s
D62B: 3265       leas     5, s
D62D: 39         rts      
D62E: 3406       pshs     a, b
D630: 4f         clra     
D631: aee4       ldx      , s
D633: e601       ldb      1, x
D635: c402       andb     #2
D637: 3262       leas     2, s
D639: 39         rts      
D63A: 327b       leas     -5, s
D63C: f6df3f     ldb      $df3f
D63F: 271e       beq      $d65f
D641: c611       ldb      #17
D643: e7e4       stb      , s
D645: 4f         clra     
D646: f7e880     stb      $e880
D649: c3e0bd     addd     #-8003
D64C: 3406       pshs     a, b
D64E: 4f         clra     
D64F: 3510       puls     x
D651: e684       ldb      , x
D653: f7e881     stb      $e881
D656: e6e4       ldb      , s
D658: c3ffff     addd     #-1
D65B: e7e4       stb      , s
D65D: 2ce6       bge      $d645
D65F: 4f         clra     
D660: 5f         clrb     
D661: ed61       std      1, s
D663: ed63       std      3, s
D665: ec63       ldd      3, s
D667: 58         lslb     
D668: 49         rola     
D669: c3010e     addd     #270
D66C: 3406       pshs     a, b
D66E: ec63       ldd      3, s
D670: 3510       puls     x
D672: ed84       std      , x
D674: ec61       ldd      1, s
D676: c30008     addd     #8
D679: ed61       std      1, s
D67B: ec63       ldd      3, s
D67D: c30001     addd     #1
D680: ed63       std      3, s
D682: 83000a     subd     #10
D685: 26de       bne      $d665
D687: 8d0c       bsr      $d695
D689: 7f0127     clr      $0127
D68C: 7f0126     clr      $0126
D68F: bdd9a9     jsr      $d9a9
D692: 3265       leas     5, s
D694: 39         rts      
D695: 7f0128     clr      $0128
D698: c60c       ldb      #12
D69A: 2007       bra      $d6a3
D69C: c6ff       ldb      #-1
D69E: f70128     stb      $0128
D6A1: c60e       ldb      #14
D6A3: f7e84c     stb      $e84c
D6A6: 39         rts      
D6A7: 327f       leas     -1, s
D6A9: cc0004     ldd      #4
D6AC: bde76b     jsr      $e76b
D6AF: f60126     ldb      $0126
D6B2: 2650       bne      $d704
D6B4: bddd82     jsr      $dd82
D6B7: e7e4       stb      , s
D6B9: c10d       cmpb     #13
D6BB: 270e       beq      $d6cb
D6BD: c103       cmpb     #3
D6BF: 270a       beq      $d6cb
D6C1: c180       cmpb     #-128
D6C3: 2406       bcc      $d6cb
D6C5: 4f         clra     
D6C6: bdd81b     jsr      $d81b
D6C9: 20e9       bra      $d6b4
D6CB: c103       cmpb     #3
D6CD: 271b       beq      $d6ea
D6CF: fc0122     ldd      $0122
D6D2: 3406       pshs     a, b
D6D4: cc0050     ldd      #80
D6D7: bdb6ce     jsr      $b6ce
D6DA: 3406       pshs     a, b
D6DC: cc0050     ldd      #80
D6DF: bdb687     jsr      $b687
D6E2: fd0124     std      $0124
D6E5: c6ff       ldb      #-1
D6E7: f70126     stb      $0126
D6EA: e6e4       ldb      , s
D6EC: c10d       cmpb     #13
D6EE: 2619       bne      $d709
D6F0: bdd872     jsr      $d872
D6F3: fc0124     ldd      $0124
D6F6: b30122     subd     $0122
D6F9: 2609       bne      $d704
D6FB: fc0124     ldd      $0124
D6FE: 830050     subd     #80
D701: fd0124     std      $0124
D704: bddb98     jsr      $db98
D707: e7e4       stb      , s
D709: c10d       cmpb     #13
D70B: 2603       bne      $d710
D70D: 7f0126     clr      $0126
D710: 4f         clra     
D711: 7edc45     jmp      $dc45
D714: 3406       pshs     a, b
D716: cc0005     ldd      #5
D719: bde76b     jsr      $e76b
D71C: 4f         clra     
D71D: e661       ldb      1, s
D71F: 2059       bra      $d77a
D721: fc0122     ldd      $0122
D724: c30001     addd     #1
D727: 2024       bra      $d74d
D729: fc0122     ldd      $0122
D72C: 830050     subd     #80
D72F: 201c       bra      $d74d
D731: fc0122     ldd      $0122
D734: c3ffff     addd     #-1
D737: c38000     addd     #-32768
D73A: 3406       pshs     a, b
D73C: cc0020     ldd      #32
D73F: 3510       puls     x
D741: e784       stb      , x
D743: fc0122     ldd      $0122
D746: c3ffff     addd     #-1
D749: 2002       bra      $d74d
D74B: 4f         clra     
D74C: 5f         clrb     
D74D: 7ed88d     jmp      $d88d
D750: 39         rts      
D751: f60128     ldb      $0128
D754: 2708       beq      $d75e
D756: 4f         clra     
D757: e663       ldb      3, s
D759: bdd828     jsr      $d828
D75C: e763       stb      3, s
D75E: fc0122     ldd      $0122
D761: c30001     addd     #1
D764: bdd88d     jsr      $d88d
D767: fc0122     ldd      $0122
D76A: c3ffff     addd     #-1
D76D: c38000     addd     #-32768
D770: 3406       pshs     a, b
D772: 4f         clra     
D773: e665       ldb      5, s
D775: 3510       puls     x
D777: e784       stb      , x
D779: 39         rts      
D77A: 8ed751     ldx      #-10415
D77D: c3ffff     addd     #-1
D780: 4d         tsta     
D781: 260b       bne      $d78e
D783: c17e       cmpb     #126
D785: 2207       bhi      $d78e
D787: 8edc4a     ldx      #-9142
D78A: 3a         abx      
D78B: 3a         abx      
D78C: ae84       ldx      , x
D78E: ad84       jsr      , x
D790: 3262       leas     2, s
D792: 39         rts      
D793: 327c       leas     -4, s
D795: fc0122     ldd      $0122
D798: 3406       pshs     a, b
D79A: cc0050     ldd      #80
D79D: bdb6ce     jsr      $b6ce
D7A0: c30001     addd     #1
D7A3: ede4       std      , s
D7A5: fc0122     ldd      $0122
D7A8: 3406       pshs     a, b
D7AA: cc0050     ldd      #80
D7AD: bdb6d8     jsr      $b6d8
D7B0: c30001     addd     #1
D7B3: ed62       std      2, s
D7B5: ece4       ldd      , s
D7B7: 1f98       tfr      b, a
D7B9: 5f         clrb     
D7BA: e362       addd     2, s
D7BC: 2056       bra      $d814
D7BE: 3406       pshs     a, b
D7C0: 327c       leas     -4, s
D7C2: ec64       ldd      4, s
D7C4: 1f89       tfr      a, b
D7C6: 4f         clra     
D7C7: c3ffff     addd     #-1
D7CA: ede4       std      , s
D7CC: ec64       ldd      4, s
D7CE: 4f         clra     
D7CF: c3ffff     addd     #-1
D7D2: ed62       std      2, s
D7D4: ece4       ldd      , s
D7D6: 3406       pshs     a, b
D7D8: cc0050     ldd      #80
D7DB: bdb687     jsr      $b687
D7DE: e362       addd     2, s
D7E0: 7eda4d     jmp      $da4d
D7E3: 3406       pshs     a, b
D7E5: 327e       leas     -2, s
D7E7: 4f         clra     
D7E8: 5f         clrb     
D7E9: ede4       std      , s
D7EB: ece4       ldd      , s
D7ED: 58         lslb     
D7EE: 49         rola     
D7EF: c3010e     addd     #270
D7F2: 3406       pshs     a, b
D7F4: ec64       ldd      4, s
D7F6: 3406       pshs     a, b
D7F8: c30002     addd     #2
D7FB: ed66       std      6, s
D7FD: 3510       puls     x
D7FF: ec84       ldd      , x
D801: c3ffff     addd     #-1
D804: 3510       puls     x
D806: ed84       std      , x
D808: ece4       ldd      , s
D80A: c30001     addd     #1
D80D: ede4       std      , s
D80F: 83000a     subd     #10
D812: 26d7       bne      $d7eb
D814: 3264       leas     4, s
D816: 39         rts      
D817: cc010e     ldd      #270
D81A: 39         rts      
D81B: 3406       pshs     a, b
D81D: e661       ldb      1, s
D81F: 2704       beq      $d825
D821: 4f         clra     
D822: bdd714     jsr      $d714
D825: 3262       leas     2, s
D827: 39         rts      
D828: 3406       pshs     a, b
D82A: 327d       leas     -3, s
D82C: cce084     ldd      #-8060
D82F: ede4       std      , s
D831: fc0122     ldd      $0122
D834: fd0124     std      $0124
D837: bddc30     jsr      $dc30
D83A: e762       stb      2, s
D83C: aee4       ldx      , s
D83E: e684       ldb      , x
D840: e164       cmpb     4, s
D842: 2606       bne      $d84a
D844: e601       ldb      1, x
D846: e162       cmpb     2, s
D848: 271d       beq      $d867
D84A: e684       ldb      , x
D84C: e162       cmpb     2, s
D84E: 2606       bne      $d856
D850: e601       ldb      1, x
D852: e164       cmpb     4, s
D854: 2711       beq      $d867
D856: ece4       ldd      , s
D858: c30003     addd     #3
D85B: ede4       std      , s
D85D: 3406       pshs     a, b
D85F: 3510       puls     x
D861: ec84       ldd      , x
D863: 26d7       bne      $d83c
D865: 2005       bra      $d86c
D867: 4f         clra     
D868: e602       ldb      2, x
D86A: e764       stb      4, s
D86C: 4f         clra     
D86D: e664       ldb      4, s
D86F: 3265       leas     5, s
D871: 39         rts      
D872: fc0122     ldd      $0122
D875: 3406       pshs     a, b
D877: cc0050     ldd      #80
D87A: bdb6ce     jsr      $b6ce
D87D: 3406       pshs     a, b
D87F: cc0050     ldd      #80
D882: bdb687     jsr      $b687
D885: 8d06       bsr      $d88d
D887: fc0122     ldd      $0122
D88A: c30050     addd     #80
D88D: 3406       pshs     a, b
D88F: fc0122     ldd      $0122
D892: c38000     addd     #-32768
D895: 3406       pshs     a, b
D897: 3510       puls     x
D899: 4f         clra     
D89A: e684       ldb      , x
D89C: c480       andb     #-128
D89E: 2719       beq      $d8b9
D8A0: fc0122     ldd      $0122
D8A3: c38000     addd     #-32768
D8A6: 3406       pshs     a, b
D8A8: fc0122     ldd      $0122
D8AB: c38000     addd     #-32768
D8AE: 3406       pshs     a, b
D8B0: 3510       puls     x
D8B2: 4f         clra     
D8B3: e684       ldb      , x
D8B5: c47f       andb     #127
D8B7: 2017       bra      $d8d0
D8B9: fc0122     ldd      $0122
D8BC: c38000     addd     #-32768
D8BF: 3406       pshs     a, b
D8C1: fc0122     ldd      $0122
D8C4: c38000     addd     #-32768
D8C7: 3406       pshs     a, b
D8C9: 3510       puls     x
D8CB: 4f         clra     
D8CC: e684       ldb      , x
D8CE: ca80       orb      #-128
D8D0: 3510       puls     x
D8D2: e784       stb      , x
D8D4: ece4       ldd      , s
D8D6: 2d09       blt      $d8e1
D8D8: 8307d0     subd     #2000
D8DB: 2404       bcc      $d8e1
D8DD: ece4       ldd      , s
D8DF: 2019       bra      $d8fa
D8E1: ece4       ldd      , s
D8E3: 2f18       ble      $d8fd
D8E5: 8307d0     subd     #2000
D8E8: 2513       bcs      $d8fd
D8EA: bddecd     jsr      $decd
D8ED: ece4       ldd      , s
D8EF: 8307d0     subd     #2000
D8F2: 2609       bne      $d8fd
D8F4: cc07d0     ldd      #2000
D8F7: 830050     subd     #80
D8FA: fd0122     std      $0122
D8FD: fc0122     ldd      $0122
D900: c38000     addd     #-32768
D903: 3406       pshs     a, b
D905: 3510       puls     x
D907: 4f         clra     
D908: e684       ldb      , x
D90A: c480       andb     #-128
D90C: 2719       beq      $d927
D90E: fc0122     ldd      $0122
D911: c38000     addd     #-32768
D914: 3406       pshs     a, b
D916: fc0122     ldd      $0122
D919: c38000     addd     #-32768
D91C: 3406       pshs     a, b
D91E: 3510       puls     x
D920: 4f         clra     
D921: e684       ldb      , x
D923: c47f       andb     #127
D925: 2017       bra      $d93e
D927: fc0122     ldd      $0122
D92A: c38000     addd     #-32768
D92D: 3406       pshs     a, b
D92F: fc0122     ldd      $0122
D932: c38000     addd     #-32768
D935: 3406       pshs     a, b
D937: 3510       puls     x
D939: 4f         clra     
D93A: e684       ldb      , x
D93C: ca80       orb      #-128
D93E: 3510       puls     x
D940: e784       stb      , x
D942: 3262       leas     2, s
D944: 39         rts      
D945: 327c       leas     -4, s
D947: fc0122     ldd      $0122
D94A: 3406       pshs     a, b
D94C: cc0050     ldd      #80
D94F: bdb6d8     jsr      $b6d8
D952: ede4       std      , s
D954: 4f         clra     
D955: 5f         clrb     
D956: ed62       std      2, s
D958: ec62       ldd      2, s
D95A: 58         lslb     
D95B: 49         rola     
D95C: c3010e     addd     #270
D95F: 3406       pshs     a, b
D961: 3510       puls     x
D963: ec84       ldd      , x
D965: a3e4       subd     , s
D967: 2e0c       bgt      $d975
D969: ec62       ldd      2, s
D96B: c30001     addd     #1
D96E: ed62       std      2, s
D970: 83000a     subd     #10
D973: 26e3       bne      $d958
D975: ec62       ldd      2, s
D977: 83000a     subd     #10
D97A: 2604       bne      $d980
D97C: 4f         clra     
D97D: 5f         clrb     
D97E: ed62       std      2, s
D980: ec62       ldd      2, s
D982: 58         lslb     
D983: 49         rola     
D984: c3010e     addd     #270
D987: 3406       pshs     a, b
D989: 3510       puls     x
D98B: ec84       ldd      , x
D98D: 3406       pshs     a, b
D98F: fc0122     ldd      $0122
D992: 3406       pshs     a, b
D994: cc0050     ldd      #80
D997: bdb6ce     jsr      $b6ce
D99A: 3406       pshs     a, b
D99C: cc0050     ldd      #80
D99F: bdb687     jsr      $b687
D9A2: e3e4       addd     , s
D9A4: 3510       puls     x
D9A6: 7edb92     jmp      $db92
D9A9: 327e       leas     -2, s
D9AB: 4f         clra     
D9AC: 5f         clrb     
D9AD: bdd88d     jsr      $d88d
D9B0: cc8000     ldd      #-32768
D9B3: c307d0     addd     #2000
D9B6: c30001     addd     #1
D9B9: ede4       std      , s
D9BB: ece4       ldd      , s
D9BD: c3ffff     addd     #-1
D9C0: ede4       std      , s
D9C2: 3406       pshs     a, b
D9C4: cc0020     ldd      #32
D9C7: 3510       puls     x
D9C9: e784       stb      , x
D9CB: ece4       ldd      , s
D9CD: 838000     subd     #-32768
D9D0: 26e9       bne      $d9bb
D9D2: 4f         clra     
D9D3: f68000     ldb      $8000
D9D6: ca80       orb      #-128
D9D8: f78000     stb      $8000
D9DB: 3262       leas     2, s
D9DD: 39         rts      
D9DE: 327a       leas     -6, s
D9E0: cc8000     ldd      #-32768
D9E3: 3406       pshs     a, b
D9E5: fc0122     ldd      $0122
D9E8: 3406       pshs     a, b
D9EA: cc0050     ldd      #80
D9ED: bdb6ce     jsr      $b6ce
D9F0: c30001     addd     #1
D9F3: 3406       pshs     a, b
D9F5: cc0050     ldd      #80
D9F8: bdb687     jsr      $b687
D9FB: e3e4       addd     , s
D9FD: 3510       puls     x
D9FF: ed62       std      2, s
DA01: c3ffff     addd     #-1
DA04: ede4       std      , s
DA06: fc0122     ldd      $0122
DA09: c38000     addd     #-32768
DA0C: ed64       std      4, s
DA0E: ece4       ldd      , s
DA10: a364       subd     4, s
DA12: 2606       bne      $da1a
DA14: c6a0       ldb      #-96
DA16: e7f4       stb      [, s]
DA18: 2036       bra      $da50
DA1A: ece4       ldd      , s
DA1C: a364       subd     4, s
DA1E: 271d       beq      $da3d
DA20: ec62       ldd      2, s
DA22: c3ffff     addd     #-1
DA25: ed62       std      2, s
DA27: 3406       pshs     a, b
DA29: ec62       ldd      2, s
DA2B: c3ffff     addd     #-1
DA2E: ed62       std      2, s
DA30: 3406       pshs     a, b
DA32: 3510       puls     x
DA34: 4f         clra     
DA35: e684       ldb      , x
DA37: 3510       puls     x
DA39: e784       stb      , x
DA3B: 20dd       bra      $da1a
DA3D: c620       ldb      #32
DA3F: e7f4       stb      [, s]
DA41: fc0122     ldd      $0122
DA44: c30001     addd     #1
DA47: fd0122     std      $0122
DA4A: c3ffff     addd     #-1
DA4D: bdd88d     jsr      $d88d
DA50: 3266       leas     6, s
DA52: 39         rts      
DA53: 327a       leas     -6, s
DA55: cc8000     ldd      #-32768
DA58: 3406       pshs     a, b
DA5A: fc0122     ldd      $0122
DA5D: 3406       pshs     a, b
DA5F: cc0050     ldd      #80
DA62: bdb6ce     jsr      $b6ce
DA65: c30001     addd     #1
DA68: 3406       pshs     a, b
DA6A: cc0050     ldd      #80
DA6D: bdb687     jsr      $b687
DA70: e3e4       addd     , s
DA72: 3510       puls     x
DA74: ede4       std      , s
DA76: cc8000     ldd      #-32768
DA79: f30122     addd     $0122
DA7C: ed62       std      2, s
DA7E: ece4       ldd      , s
DA80: c3ffff     addd     #-1
DA83: ede4       std      , s
DA85: a362       subd     2, s
DA87: 2706       beq      $da8f
DA89: e6f4       ldb      [, s]
DA8B: c120       cmpb     #32
DA8D: 27ef       beq      $da7e
DA8F: e6f4       ldb      [, s]
DA91: c1a0       cmpb     #-96
DA93: 262a       bne      $dabf
DA95: fc0122     ldd      $0122
DA98: 3406       pshs     a, b
DA9A: cc0050     ldd      #80
DA9D: bdb6d8     jsr      $b6d8
DAA0: 271b       beq      $dabd
DAA2: fc0122     ldd      $0122
DAA5: c3ffff     addd     #-1
DAA8: c38000     addd     #-32768
DAAB: 3406       pshs     a, b
DAAD: cc0020     ldd      #32
DAB0: 3510       puls     x
DAB2: e784       stb      , x
DAB4: fc0122     ldd      $0122
DAB7: c3ffff     addd     #-1
DABA: bdd88d     jsr      $d88d
DABD: 206e       bra      $db2d
DABF: cc0050     ldd      #80
DAC2: c3ffff     addd     #-1
DAC5: 3406       pshs     a, b
DAC7: fc0122     ldd      $0122
DACA: 3406       pshs     a, b
DACC: cc0050     ldd      #80
DACF: bdb6d8     jsr      $b6d8
DAD2: 3406       pshs     a, b
DAD4: ec62       ldd      2, s
DAD6: a3e4       subd     , s
DAD8: 3510       puls     x
DADA: 3510       puls     x
DADC: ed64       std      4, s
DADE: 2607       bne      $dae7
DAE0: c6a0       ldb      #-96
DAE2: e7f802     stb      [2, s]
DAE5: 2046       bra      $db2d
DAE7: ec62       ldd      2, s
DAE9: c30001     addd     #1
DAEC: ede4       std      , s
DAEE: fc0122     ldd      $0122
DAF1: c30001     addd     #1
DAF4: bdd88d     jsr      $d88d
DAF7: ec64       ldd      4, s
DAF9: 2724       beq      $db1f
DAFB: ec62       ldd      2, s
DAFD: 3406       pshs     a, b
DAFF: c30001     addd     #1
DB02: ed64       std      4, s
DB04: ec62       ldd      2, s
DB06: 3406       pshs     a, b
DB08: c30001     addd     #1
DB0B: ed64       std      4, s
DB0D: 3510       puls     x
DB0F: 4f         clra     
DB10: e684       ldb      , x
DB12: 3510       puls     x
DB14: e784       stb      , x
DB16: ec64       ldd      4, s
DB18: c3ffff     addd     #-1
DB1B: ed64       std      4, s
DB1D: 20d8       bra      $daf7
DB1F: c620       ldb      #32
DB21: e7f802     stb      [2, s]
DB24: fc0122     ldd      $0122
DB27: c3ffff     addd     #-1
DB2A: fd0122     std      $0122
DB2D: 3266       leas     6, s
DB2F: 39         rts      
DB30: 327c       leas     -4, s
DB32: cc0050     ldd      #80
DB35: c30001     addd     #1
DB38: 3406       pshs     a, b
DB3A: fc0122     ldd      $0122
DB3D: 3406       pshs     a, b
DB3F: cc0050     ldd      #80
DB42: bdb6d8     jsr      $b6d8
DB45: 3406       pshs     a, b
DB47: ec62       ldd      2, s
DB49: a3e4       subd     , s
DB4B: 3510       puls     x
DB4D: 3510       puls     x
DB4F: ed62       std      2, s
DB51: fc0122     ldd      $0122
DB54: c38000     addd     #-32768
DB57: ede4       std      , s
DB59: ec62       ldd      2, s
DB5B: c3ffff     addd     #-1
DB5E: ed62       std      2, s
DB60: 2712       beq      $db74
DB62: ece4       ldd      , s
DB64: 3406       pshs     a, b
DB66: c30001     addd     #1
DB69: ed62       std      2, s
DB6B: cc0020     ldd      #32
DB6E: 3510       puls     x
DB70: e784       stb      , x
DB72: 20e5       bra      $db59
DB74: fc0122     ldd      $0122
DB77: c38000     addd     #-32768
DB7A: 3406       pshs     a, b
DB7C: fc0122     ldd      $0122
DB7F: c38000     addd     #-32768
DB82: 3406       pshs     a, b
DB84: 3510       puls     x
DB86: 4f         clra     
DB87: e684       ldb      , x
DB89: ca80       orb      #-128
DB8B: 3510       puls     x
DB8D: e784       stb      , x
DB8F: fc0122     ldd      $0122
DB92: bdd88d     jsr      $d88d
DB95: 3264       leas     4, s
DB97: 39         rts      
DB98: 327a       leas     -6, s
DB9A: f60127     ldb      $0127
DB9D: 2705       beq      $dba4
DB9F: 7f0127     clr      $0127
DBA2: 207b       bra      $dc1f
DBA4: bddc30     jsr      $dc30
DBA7: e7e4       stb      , s
DBA9: fc0124     ldd      $0124
DBAC: 3406       pshs     a, b
DBAE: cc0050     ldd      #80
DBB1: bdb6d8     jsr      $b6d8
DBB4: 3406       pshs     a, b
DBB6: cc0050     ldd      #80
DBB9: c3ffff     addd     #-1
DBBC: 3406       pshs     a, b
DBBE: ec62       ldd      2, s
DBC0: a3e4       subd     , s
DBC2: 3510       puls     x
DBC4: 3510       puls     x
DBC6: 2751       beq      $dc19
DBC8: fc0124     ldd      $0124
DBCB: c30001     addd     #1
DBCE: ed62       std      2, s
DBD0: 3406       pshs     a, b
DBD2: cc0050     ldd      #80
DBD5: bdb6ce     jsr      $b6ce
DBD8: c30001     addd     #1
DBDB: 3406       pshs     a, b
DBDD: cc0050     ldd      #80
DBE0: bdb687     jsr      $b687
DBE3: a362       subd     2, s
DBE5: ed64       std      4, s
DBE7: ec64       ldd      4, s
DBE9: 271f       beq      $dc0a
DBEB: ec62       ldd      2, s
DBED: 3406       pshs     a, b
DBEF: c30001     addd     #1
DBF2: ed64       std      4, s
DBF4: 3506       puls     a, b
DBF6: c38000     addd     #-32768
DBF9: 3406       pshs     a, b
DBFB: 3510       puls     x
DBFD: e684       ldb      , x
DBFF: c120       cmpb     #32
DC01: 2607       bne      $dc0a
DC03: ec64       ldd      4, s
DC05: c3ffff     addd     #-1
DC08: 20db       bra      $dbe5
DC0A: ec64       ldd      4, s
DC0C: 270b       beq      $dc19
DC0E: fc0124     ldd      $0124
DC11: c30001     addd     #1
DC14: fd0124     std      $0124
DC17: 2011       bra      $dc2a
DC19: e6e4       ldb      , s
DC1B: c120       cmpb     #32
DC1D: 2606       bne      $dc25
DC1F: c60d       ldb      #13
DC21: e7e4       stb      , s
DC23: 2005       bra      $dc2a
DC25: c6ff       ldb      #-1
DC27: f70127     stb      $0127
DC2A: 4f         clra     
DC2B: e6e4       ldb      , s
DC2D: 3266       leas     6, s
DC2F: 39         rts      
DC30: 327f       leas     -1, s
DC32: fc0124     ldd      $0124
DC35: c38000     addd     #-32768
DC38: 3406       pshs     a, b
DC3A: 4f         clra     
DC3B: 3510       puls     x
DC3D: e684       ldb      , x
DC3F: e7e4       stb      , s
DC41: c47f       andb     #127
DC43: e7e4       stb      , s
DC45: e6e4       ldb      , s
DC47: 3261       leas     1, s
DC49: 39         rts      
DC4A: d74b       stb      $4b
DC4C: d750       stb      $50
DC4E: d750       stb      $50
DC50: da53       orb      $53
DC52: d9de       adcb     $de
DC54: db30       addb     $30
DC56: d721       stb      $21
DC58: d743       stb      $43
DC5A: d945       adcb     $45
DC5C: d887       eorb     $87
DC5E: d729       stb      $29
DC60: d9a9       adcb     $a9
DC62: d872       eorb     $72
DC64: d751       stb      $51
DC66: d751       stb      $51
DC68: d751       stb      $51
DC6A: d751       stb      $51
DC6C: d751       stb      $51
DC6E: d751       stb      $51
DC70: d751       stb      $51
DC72: d751       stb      $51
DC74: d751       stb      $51
DC76: d751       stb      $51
DC78: d751       stb      $51
DC7A: d751       stb      $51
DC7C: d751       stb      $51
DC7E: d751       stb      $51
DC80: d751       stb      $51
DC82: d751       stb      $51
DC84: d751       stb      $51
DC86: d751       stb      $51
DC88: d751       stb      $51
DC8A: d751       stb      $51
DC8C: d751       stb      $51
DC8E: d751       stb      $51
DC90: d751       stb      $51
DC92: d751       stb      $51
DC94: d751       stb      $51
DC96: d751       stb      $51
DC98: d751       stb      $51
DC9A: d751       stb      $51
DC9C: d751       stb      $51
DC9E: d751       stb      $51
DCA0: d751       stb      $51
DCA2: d751       stb      $51
DCA4: d751       stb      $51
DCA6: d751       stb      $51
DCA8: d751       stb      $51
DCAA: d751       stb      $51
DCAC: d751       stb      $51
DCAE: d751       stb      $51
DCB0: d751       stb      $51
DCB2: d751       stb      $51
DCB4: d751       stb      $51
DCB6: d751       stb      $51
DCB8: d751       stb      $51
DCBA: d751       stb      $51
DCBC: d751       stb      $51
DCBE: d751       stb      $51
DCC0: d751       stb      $51
DCC2: d751       stb      $51
DCC4: d751       stb      $51
DCC6: d751       stb      $51
DCC8: d751       stb      $51
DCCA: d751       stb      $51
DCCC: d751       stb      $51
DCCE: d751       stb      $51
DCD0: d751       stb      $51
DCD2: d751       stb      $51
DCD4: d751       stb      $51
DCD6: d751       stb      $51
DCD8: d751       stb      $51
DCDA: d751       stb      $51
DCDC: d751       stb      $51
DCDE: d751       stb      $51
DCE0: d751       stb      $51
DCE2: d751       stb      $51
DCE4: d751       stb      $51
DCE6: d751       stb      $51
DCE8: d751       stb      $51
DCEA: d751       stb      $51
DCEC: d751       stb      $51
DCEE: d751       stb      $51
DCF0: d751       stb      $51
DCF2: d751       stb      $51
DCF4: d751       stb      $51
DCF6: d751       stb      $51
DCF8: d751       stb      $51
DCFA: d751       stb      $51
DCFC: d751       stb      $51
DCFE: d751       stb      $51
DD00: d751       stb      $51
DD02: d751       stb      $51
DD04: d751       stb      $51
DD06: d751       stb      $51
DD08: d751       stb      $51
DD0A: d751       stb      $51
DD0C: d751       stb      $51
DD0E: d751       stb      $51
DD10: d751       stb      $51
DD12: d751       stb      $51
DD14: d751       stb      $51
DD16: d751       stb      $51
DD18: d751       stb      $51
DD1A: d751       stb      $51
DD1C: d751       stb      $51
DD1E: d751       stb      $51
DD20: d751       stb      $51
DD22: d751       stb      $51
DD24: d751       stb      $51
DD26: d751       stb      $51
DD28: d751       stb      $51
DD2A: d751       stb      $51
DD2C: d751       stb      $51
DD2E: d751       stb      $51
DD30: d751       stb      $51
DD32: d751       stb      $51
DD34: d751       stb      $51
DD36: d751       stb      $51
DD38: d751       stb      $51
DD3A: d751       stb      $51
DD3C: d751       stb      $51
DD3E: d751       stb      $51
DD40: d751       stb      $51
DD42: d751       stb      $51
DD44: d751       stb      $51
DD46: d731       stb      $31
DD48: c61e       ldb      #30
DD4A: f7012a     stb      $012a
DD4D: 7f012b     clr      $012b
DD50: cc0008     ldd      #8
DD53: 3406       pshs     a, b
DD55: ccde0b     ldd      #-8693
DD58: bdff97     jsr      $ff97
DD5B: 3262       leas     2, s
DD5D: bdde01     jsr      $de01
DD60: cc010e     ldd      #270
DD63: c30022     addd     #34
DD66: fd012c     std      $012c
DD69: cc010e     ldd      #270
DD6C: c30022     addd     #34
DD6F: fd012e     std      $012e
DD72: c60f       ldb      #15
DD74: f7e810     stb      $e810
DD77: c63c       ldb      #60
DD79: f7e811     stb      $e811
DD7C: c63d       ldb      #61
DD7E: f7e813     stb      $e813
DD81: 39         rts      
DD82: 327f       leas     -1, s
DD84: 6fe4       clr      , s
DD86: fc012c     ldd      $012c
DD89: b3012e     subd     $012e
DD8C: 2719       beq      $dda7
DD8E: be012c     ldx      $012c
DD91: 4f         clra     
DD92: e684       ldb      , x
DD94: e7e4       stb      , s
DD96: fc012c     ldd      $012c
DD99: bddea7     jsr      $dea7
DD9C: fd012c     std      $012c
DD9F: e6e4       ldb      , s
DDA1: 27e3       beq      $dd86
DDA3: c1ff       cmpb     #-1
DDA5: 27df       beq      $dd86
DDA7: 4f         clra     
DDA8: e6e4       ldb      , s
DDAA: 3261       leas     1, s
DDAC: 39         rts      
DDAD: 3406       pshs     a, b
DDAF: 327e       leas     -2, s
DDB1: bde768     jsr      $e768
DDB4: fc012e     ldd      $012e
DDB7: bddea7     jsr      $dea7
DDBA: ede4       std      , s
DDBC: b3012c     subd     $012c
DDBF: 2711       beq      $ddd2
DDC1: be012e     ldx      $012e
DDC4: 3410       pshs     x
DDC6: 4f         clra     
DDC7: e665       ldb      5, s
DDC9: 3510       puls     x
DDCB: e784       stb      , x
DDCD: ece4       ldd      , s
DDCF: fd012e     std      $012e
DDD2: bde765     jsr      $e765
DDD5: 3264       leas     4, s
DDD7: 39         rts      
DDD8: 327d       leas     -3, s
DDDA: 6f62       clr      2, s
DDDC: fc012c     ldd      $012c
DDDF: ede4       std      , s
DDE1: b3012e     subd     $012e
DDE4: 2715       beq      $ddfb
DDE6: aee4       ldx      , s
DDE8: e684       ldb      , x
DDEA: c103       cmpb     #3
DDEC: 2606       bne      $ddf4
DDEE: c6ff       ldb      #-1
DDF0: e762       stb      2, s
DDF2: 6f84       clr      , x
DDF4: ece4       ldd      , s
DDF6: bddea7     jsr      $dea7
DDF9: 20e4       bra      $dddf
DDFB: 4f         clra     
DDFC: e662       ldb      2, s
DDFE: 3263       leas     3, s
DE00: 39         rts      
DE01: c6ff       ldb      #-1
DE03: f70129     stb      $0129
DE06: 39         rts      
DE07: 7f0129     clr      $0129
DE0A: 39         rts      
DE0B: 327f       leas     -1, s
DE0D: 4f         clra     
DE0E: f6e813     ldb      $e813
DE11: c480       andb     #-128
DE13: 2605       bne      $de1a
DE15: f6eff1     ldb      $eff1
DE18: 2061       bra      $de7b
DE1A: bde0cf     jsr      $e0cf
DE1D: f60129     ldb      $0129
DE20: 2755       beq      $de77
DE22: bddef3     jsr      $def3
DE25: e7e4       stb      , s
DE27: 2744       beq      $de6d
DE29: f1012b     cmpb     $012b
DE2C: 2618       bne      $de46
DE2E: 4f         clra     
DE2F: f6012a     ldb      $012a
DE32: c3ffff     addd     #-1
DE35: f7012a     stb      $012a
DE38: 2643       bne      $de7d
DE3A: 4f         clra     
DE3B: e6e4       ldb      , s
DE3D: 8d41       bsr      $de80
DE3F: bdddad     jsr      $ddad
DE42: c604       ldb      #4
DE44: 202c       bra      $de72
DE46: 4f         clra     
DE47: f6012b     ldb      $012b
DE4A: c3ffaf     addd     #-81
DE4D: 3406       pshs     a, b
DE4F: 4f         clra     
DE50: e662       ldb      2, s
DE52: a3e4       subd     , s
DE54: 3510       puls     x
DE56: 2608       bne      $de60
DE58: 4f         clra     
DE59: e6e4       ldb      , s
DE5B: f7012b     stb      $012b
DE5E: 2010       bra      $de70
DE60: 4f         clra     
DE61: e6e4       ldb      , s
DE63: f7012b     stb      $012b
DE66: 8d18       bsr      $de80
DE68: bdddad     jsr      $ddad
DE6B: 2003       bra      $de70
DE6D: 7f012b     clr      $012b
DE70: c61e       ldb      #30
DE72: f7012a     stb      $012a
DE75: 2006       bra      $de7d
DE77: 4f         clra     
DE78: f6e812     ldb      $e812
DE7B: e7e4       stb      , s
DE7D: 3261       leas     1, s
DE7F: 39         rts      
DE80: 3406       pshs     a, b
DE82: 327f       leas     -1, s
DE84: f60128     ldb      $0128
DE87: c1ff       cmpb     #-1
DE89: 2608       bne      $de93
DE8B: 4f         clra     
DE8C: e662       ldb      2, s
DE8E: c3dfe2     addd     #-8222
DE91: 2006       bra      $de99
DE93: 4f         clra     
DE94: e662       ldb      2, s
DE96: c3df40     addd     #-8384
DE99: 3406       pshs     a, b
DE9B: 4f         clra     
DE9C: 3510       puls     x
DE9E: e684       ldb      , x
DEA0: e7e4       stb      , s
DEA2: e6e4       ldb      , s
DEA4: 3263       leas     3, s
DEA6: 39         rts      
DEA7: 3406       pshs     a, b
DEA9: ece4       ldd      , s
DEAB: c30001     addd     #1
DEAE: ede4       std      , s
DEB0: cc010e     ldd      #270
DEB3: c3004a     addd     #74
DEB6: 3406       pshs     a, b
DEB8: ec62       ldd      2, s
DEBA: a3e4       subd     , s
DEBC: 3510       puls     x
DEBE: 2608       bne      $dec8
DEC0: cc010e     ldd      #270
DEC3: c30022     addd     #34
DEC6: ede4       std      , s
DEC8: ece4       ldd      , s
DECA: 3262       leas     2, s
DECC: 39         rts      
DECD: cc07d0     ldd      #2000
DED0: 830050     subd     #80
DED3: 1f02       tfr      d, y
DED5: 8e8000     ldx      #-32768
DED8: e6890050   ldb      80, x
DEDC: e780       stb      , x+
DEDE: 313f       leay     -1, y
DEE0: 2702       beq      $dee4
DEE2: 20f4       bra      $ded8
DEE4: 108e0050   ldy      #80
DEE8: c620       ldb      #32
DEEA: e780       stb      , x+
DEEC: 313f       leay     -1, y
DEEE: 2702       beq      $def2
DEF0: 20f8       bra      $deea
DEF2: 39         rts      
DEF3: 8600       lda      #0
DEF5: 3402       pshs     a
DEF7: b7e810     sta      $e810
DEFA: 108e0000   ldy      #0
DEFE: 8e0050     ldx      #80
DF01: 8608       lda      #8
DF03: f6e812     ldb      $e812
DF06: f1e812     cmpb     $e812
DF09: 2702       beq      $df0d
DF0B: 20f6       bra      $df03
DF0D: c1ff       cmpb     #-1
DF0F: 2604       bne      $df15
DF11: 3018       leax     -8, x
DF13: 2016       bra      $df2b
DF15: 54         lsrb     
DF16: 250c       bcs      $df24
DF18: 6d89df40   tst      -8384, x
DF1C: 2604       bne      $df22
DF1E: 6ce4       inc      , s
DF20: 2002       bra      $df24
DF22: 1f12       tfr      x, y
DF24: 301f       leax     -1, x
DF26: 4a         deca     
DF27: 2702       beq      $df2b
DF29: 20ea       bra      $df15
DF2B: 3084       leax     , x
DF2D: 2705       beq      $df34
DF2F: 7ce810     inc      $e810
DF32: 20cd       bra      $df01
DF34: 3502       puls     a
DF36: 4d         tsta     
DF37: 2703       beq      $df3c
DF39: 31a851     leay     81, y
DF3C: 1f20       tfr      y, d
DF3E: 39         rts      
DF3F: ff00ff     stu      >$00ff
DF42: ff3a03     stu      $3a03
DF45: 39         rts      
DF46: 3633       pshu     cc, a, x, y
DF48: 5f         clrb     
DF49: 312f       leay     15, y
DF4B: ff016d     stu      $016d
DF4E: 2078       bra      $dfc8
DF50: ff327f     stu      $327f
DF53: ff302c     stu      $302c
DF56: 6e76       jmp      -10, s
DF58: 7a3300     dec      $3300
DF5B: ff2e2e     stu      $2e2e
DF5E: 62         fcb      $62
DF5F: 6300       com      0, x
DF61: 345b       pshs     cc, a, dp, x, u
DF63: 6f0a       clr      10, x
DF65: 75         fcb      $75
DF66: 746571     lsr      $6571
DF69: 0470       lsr      $70
DF6B: 695c       rol      -4, u
DF6D: 797277     rol      $7277
DF70: 0936       rol      $36
DF72: 40         nega     
DF73: 6c0d       inc      13, x
DF75: 6a67       dec      7, s
DF77: 6461       lsr      1, s
DF79: 353b       puls     cc, a, dp, x, y
DF7B: 6b         fcb      $6b
DF7C: 5d         tstb     
DF7D: 6866       lsl      6, s
DF7F: 730639     com      $0639
DF82: ff5e37     stu      $5e37
DF85: 3037       leax     -9, y
DF87: 3431       pshs     cc, x, y
DF89: ffff07     stu      $ff07
DF8C: 38         fcb      $38
DF8D: 2d38       blt      $dfc7
DF8F: 3532       puls     a, x, y
DF91: 00ff       neg      $ff
DF93: ff2a02     stu      $2a02
DF96: 2926       bvs      $dfbe
DF98: 235f       bls      $dff9
DF9A: 813f       cmpa     #63
DF9C: ff0c4d     stu      $0c4d
DF9F: 2058       bra      $dff9
DFA1: ff827f     stu      $827f
DFA4: ff8a3c     stu      $8a3c
DFA7: 4e         fcb      $4e
DFA8: 56         rorb     
DFA9: 5a         decb     
DFAA: 8300ff     subd     #255
DFAD: 8b3e       adda     #62
DFAF: 42         fcb      $42
DFB0: 43         coma     
DFB1: 0084       neg      $84
DFB3: 7b         fcb      $7b
DFB4: 4f         clra     
DFB5: 0b         fcb      $0b
DFB6: 55         fcb      $55
DFB7: 54         lsrb     
DFB8: 45         fcb      $45
DFB9: 51         fcb      $51
DFBA: 05         fcb      $05
DFBB: 50         negb     
DFBC: 49         rola     
DFBD: 7c5952     inc      $5952
DFC0: 57         asrb     
DFC1: 0986       rol      $86
DFC3: 604c       neg      12, u
DFC5: 0d4a       tst      $4a
DFC7: 47         asra     
DFC8: 44         lsra     
DFC9: 41         fcb      $41
DFCA: 852b       bita     #43
DFCC: 4b         fcb      $4b
DFCD: 7d4846     tst      $4846
DFD0: 53         comb     
DFD1: 0689       ror      $89
DFD3: ff7e87     stu      $7e87
DFD6: 3027       leax     7, y
DFD8: 2421       bcc      $dffb
DFDA: ffff08     stu      $ff08
DFDD: 883d       eora     #61
DFDF: 2825       bvc      $e006
DFE1: 2200       bhi      $dfe3
DFE3: ffff2d     stu      $ff2d
DFE6: 0339       com      $39
DFE8: 3633       pshu     cc, a, x, y
DFEA: ff312f     stu      $312f
DFED: ff016d     stu      $016d
DFF0: 2078       bra      $e06a
DFF2: ff327f     stu      $327f
DFF5: ff302c     stu      $302c
DFF8: 6e76       jmp      -10, s
DFFA: 7a3300     dec      $3300
DFFD: ff2e2e     stu      $2e2e
