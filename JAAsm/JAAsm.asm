.data
ALIGN 16 ; Wyrównanie do 16 bajtów dla operacji AVX


    ; Wspó³czynniki dla efektu sepii (wartoœci z C#)
    ; REAL4 - liczby zmiennoprzecinkowe pojedyñczej prezycji
    r_multiplier REAL4 0.393, 0.769, 0.189, 0.0 ; Dla czerwonego
    g_multiplier REAL4 0.349, 0.686, 0.168, 0.0 ; Dla zielonego
    b_multiplier REAL4 0.272, 0.534, 0.131, 0.0 ; Dla niebieskiego

    ; Sta³e wektorowe dla operacji AVX
    const_1 REAL4 1.0, 1.0, 1.0, 1.0
    const_255 REAL4 255.0, 255.0, 255.0, 255.0
    const_0 REAL4 0.0, 0.0, 0.0, 0.0
    
    ; Sta³e dla generatora liczb pseudolosowych
    seed_base DQ 123456789   
    random_a DQ 1664525      
    random_c DQ 1013904223   

.code
ProcessSepiaEffect PROC
    ; RCX = input pointer
    ; RDX = output pointer
    ; R8 = pixel count
    ; XMM3 = intensity
    ; R9 = thread_index
    
    push rbx
    push rdi
    push rsi
    push r12
    
    mov rbx, r8        ; liczba pikseli
    mov rdi, rdx       ; output pointer
    mov rsi, rcx       ; input pointer
    
    ; Inicjalizacja ziarna
    mov rax, r9                    
    imul rax, [seed_base]          
    mov r12, rax                   
    
    ; Rozszerz intensity do xmm15 (wszystkie 4 elementy)
    vbroadcastss xmm15, xmm3
    
    ; Przygotuj 1 - intensity w xmm14
    vmovaps xmm14, const_1
    vsubps xmm14, xmm14, xmm15

pixel_loop:
    ; Wczytaj piksel
    movzx eax, BYTE PTR [rsi]      ; Blue
    movzx r9d, BYTE PTR [rsi+1]    ; Green
    movzx r11d, BYTE PTR [rsi+2]   ; Red
    
    ; Konwersja na float
    vcvtsi2ss xmm0, xmm0, eax     ; Blue
    vcvtsi2ss xmm1, xmm1, r9d     ; Green
    vcvtsi2ss xmm2, xmm2, r11d    ; Red
    
    ; Rozszerzenie do wektorów 4-elementowych
    vshufps xmm0, xmm0, xmm0, 0    ; Blue rozszerzony na wszystkie elementy
    vshufps xmm1, xmm1, xmm1, 0    ; Green rozszerzony na wszystkie elementy
    vshufps xmm2, xmm2, xmm2, 0    ; Red rozszerzony na wszystkie elementy
    
    ; Obliczenia sepii dla wszystkich kana³ów
    vmulps xmm3, xmm2, r_multiplier    ; Red sk³adowa
    vmulps xmm4, xmm1, r_multiplier+4  
    vmulps xmm5, xmm0, r_multiplier+8
    vaddps xmm3, xmm3, xmm4
    vaddps xmm3, xmm3, xmm5            ; Koñcowy czerwony
    
    vmulps xmm4, xmm2, g_multiplier    ; Green sk³adowa
    vmulps xmm5, xmm1, g_multiplier+4
    vmulps xmm6, xmm0, g_multiplier+8
    vaddps xmm4, xmm4, xmm5
    vaddps xmm4, xmm4, xmm6            ; Koñcowy zielony
    
    vmulps xmm5, xmm2, b_multiplier    ; Blue sk³adowa
    vmulps xmm6, xmm1, b_multiplier+4
    vmulps xmm7, xmm0, b_multiplier+8
    vaddps xmm5, xmm5, xmm6
    vaddps xmm5, xmm5, xmm7            ; Koñcowy niebieski
    
    ; Mieszanie z oryginalnym kolorem
    vmulps xmm3, xmm3, xmm15           ; sepia * intensity
    vcvtsi2ss xmm6, xmm6, r11d
    vshufps xmm6, xmm6, xmm6, 0
    vmulps xmm6, xmm6, xmm14           ; original * (1-intensity)
    vaddps xmm3, xmm3, xmm6            ; final red
    
    vmulps xmm4, xmm4, xmm15
    vcvtsi2ss xmm6, xmm6, r9d
    vshufps xmm6, xmm6, xmm6, 0
    vmulps xmm6, xmm6, xmm14
    vaddps xmm4, xmm4, xmm6            ; final green
    
    vmulps xmm5, xmm5, xmm15
    vcvtsi2ss xmm6, xmm6, eax
    vshufps xmm6, xmm6, xmm6, 0
    vmulps xmm6, xmm6, xmm14
    vaddps xmm5, xmm5, xmm6            ; final blue
    
    ; Generuj szum u¿ywaj¹c LCG
    mov rax, r12                   
    imul rax, [random_a]           
    add rax, [random_c]            
    mov r12, rax                   
    shr rax, 16                    
    and eax, 0C7h                  
    sub eax, 100                   
    
    ; Konwertuj szum na wektor
    vcvtsi2ss xmm6, xmm6, eax
    vshufps xmm6, xmm6, xmm6, 0
    vmulps xmm6, xmm6, xmm15              
    
    ; Dodaj szum do ka¿dego kana³u
    vaddps xmm3, xmm3, xmm6
    vaddps xmm4, xmm4, xmm6
    vaddps xmm5, xmm5, xmm6
    
    ; Ogranicz wartoœci (0-255)
    vmaxps xmm3, xmm3, const_0
    vminps xmm3, xmm3, const_255
    
    vmaxps xmm4, xmm4, const_0
    vminps xmm4, xmm4, const_255
    
    vmaxps xmm5, xmm5, const_0
    vminps xmm5, xmm5, const_255
    
    ; Konwersja na int i zapis do pamiêci
    vcvttss2si eax, xmm5
    mov BYTE PTR [rdi], al         ; Blue
    vcvttss2si eax, xmm4
    mov BYTE PTR [rdi+1], al       ; Green
    vcvttss2si eax, xmm3
    mov BYTE PTR [rdi+2], al       ; Red
    mov BYTE PTR [rdi+3], 255      ; Alpha
    
    ; Nastêpny piksel
    add rsi, 4
    add rdi, 4
    dec rbx
    jnz pixel_loop

    vzeroupper                     ; Czyœcimy stan AVX
    
    pop r12
    pop rsi
    pop rdi
    pop rbx
    ret
ProcessSepiaEffect ENDP

END