# Validação do VR Box — 8 de setembro de 2026

Projeto: Unity 2022.3.62f1, Google Cardboard XR Plugin na tag v1.30.1.

| Verificação | Resultado |
|---|---|
| Loader Android | Cardboard configurado, inicialização manual |
| Temporização dos botões | 6 cenários aprovados; 1 segundo para acionar, sem repetição até desviar |
| Interface VR em Play Mode | Canvas em espaço 3D; Canvas 2D oculto durante VR |
| Leitura sem toque | Abrir ficha, avançar página, fixar, liberar e fechar aprovados |
| Menu sem toque | Abrir olhando para cima, trocar páginas e sair do VR aprovados |
| Estabilidade da ficha | Posição preservada ao girar a cabeça |
| Regressão da visita 2D | 28 fichas, 12 cenários de estado, raycast nos 28 objetos e integração aprovados |
| Shader da ficha para Android | Compilação OpenGLES3 aprovada |
| Compilação Android | APK gerado com sucesso em IL2CPP/ARM64, API mínima 26 e alvo 35 |
| Pacote | Biblioteca `libGfxPluginCardboard.so` presente; integridade ZIP aprovada |
| Assinatura | APK Signature Scheme v2 verificado com `apksigner` |
| Higiene das alterações | `git diff --check` aprovado |

APK desta validação anterior ao redesign: [MuseuVR-before-louvre.apk](before-louvre/MuseuVR-before-louvre.apk),
31.163.992 bytes (31,2 MB). SHA-256:
`6c143d4092fd64817ce9b52270cd46d11492029b5f2a8339efc24799a377f5ac`.

[Log resumido dos testes e da compilação](vr-validation.log).

Os testes movem a câmera e aguardam os temporizadores reais de interação; não
invocam os cliques dos botões diretamente. A revisão estática independente também
verificou entrada, pose, movimento, menu, saída e inclusão das configurações XR.

[Captura da ficha em espaço 3D](museum-vr-guide-editor.png).
Esta é uma captura **mono do Editor**, não uma demonstração de dois olhos no aparelho.

## Validação que depende do aparelho

Ainda não foram medidos FPS, latência, temperatura ou conforto no celular. O fluxo
de câmera/QR e a renderização estereoscópica nativa também precisam de teste Android.
Use o QR de lentes do fabricante/modelo do visor; o aplicativo exige um perfil salvo
antes de liberar **Entrar em VR**.

O projeto mantém os avisos preexistentes de Renderers em LODGroups sobrepostos.
Eles não impediram as validações. Não foi realizado novo bake de iluminação.

Consulte [preparação, controles e compilação](../UnityProject/VR_BOX.md).
