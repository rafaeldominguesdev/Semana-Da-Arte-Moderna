# Visita com celular em visor VR Box

Alvo configurado: Android 8 ou superior, ARM64, celular com giroscópio e visor com
perfil de lentes compatível com Cardboard. A câmera possui rastreamento de rotação
(3DoF). A locomoção é virtual; o visor não rastreia passos físicos.

## Antes de encaixar o celular

1. Abra o aplicativo com o celular na horizontal.
2. Toque em **Ler QR das lentes** e escaneie o código de configuração do seu visor.
   Esse código descreve as lentes; não é um QR de loja ou de download do aplicativo.
3. Com o perfil salvo, toque em **Entrar em VR** e encaixe o celular no visor.

O app só libera a entrada quando há um perfil de lentes salvo. Não inventa um
perfil para um modelo desconhecido. Se seu VR Box não veio com esse QR, obtenha
o perfil do fabricante/modelo antes de usar o modo VR. **Prévia sem óculos**
permite visitar em 2D. Nas opções dessa prévia, **Preparar visor VR** volta à preparação.

## Dentro do visor — sem tocar na tela

- Olhe para uma peça: sua ficha aparece em espaço 3D, visível pelos dois olhos.
- Olhe para um botão por **1 segundo**: o círculo da mira se completa e aciona o botão.
  Manter o olhar não repete a ação. Desvie e volte para acioná-lo novamente.
- **Anterior** e **Próxima** percorrem as páginas; não é necessário arrastar texto.
- **Fixar**, **Liberar** e **Fechar** também funcionam pelo olhar.
- **Opções** abre os ajustes. Sem ficha aberta, olhe para cima por **1,2 segundo**
  e volte a olhar à frente para encontrar o menu.
- Para caminhar, olhe para baixo; levante o olhar para parar. O ritmo em VR foi
  limitado a 1,1 m/s. Menu aberto ou olhar sobre a ficha suspendem o caminhar.
- O menu permite ampliar texto, resumir, mudar contraste, desativar fichas,
  reduzir animações, prolongar leitura, centralizar a visão e sair do VR.

A ficha fica no lugar ao girar a cabeça: seus controles podem ser alcançados pela
mira. Há 1,2 segundo de tolerância ao desviar de uma obra para alcançar a ficha
(ou 5 segundos com leitura longa). A interface funciona como sobreposição em 3D;
as paredes continuam bloqueando a detecção das obras.

## Implementação

O app usa o SDK oficial Google Cardboard XR Plugin, fixado na **tag v1.30.1**
(o package.json do fornecedor ainda informa 1.30.0). Essa tag antecede a remoção
do suporte ao Unity 2022.3 na versão 1.31.0. O SDK faz a renderização por olho,
a correção óptica e a leitura da pose. Não se trata de duplicar uma imagem 2D.

- `MobileVrMode`: preparação, QR, inicialização manual de XR e entrada/saída.
- `VrWorldGuide`: Canvas em espaço 3D, paginação e controles pelo olhar.
- `GazeActivation`: temporização e bloqueio de repetição dos botões.
- `GyroscopeController`: usa o SDK para a pose durante VR, sem somar duas rotações.
- `MobileVrSetup.Apply`: Android landscape, OpenGLES3, IL2CPP/ARM64, API26–35,
  Input Handling Both, dependências Gradle e loader Cardboard.

O formato original 2D continua disponível. A prévia do Editor testa interação;
somente o aparelho Android valida rastreamento nativo, renderização estereoscópica,
ajuste às lentes, latência e conforto de leitura. Não há suporte iOS configurado.

## APK e testes

APK com o redesign inspirado no Louvre, gerado em 9 de setembro e verificado em
10 de setembro de 2026:
[MuseudaSemanaArteModerna.apk](../artifacts/MuseumVR_Build/MuseudaSemanaArteModerna.apk)
(33,6 MB). Transfira para o celular Android e abra o arquivo para instalar.
Se o Android solicitar, autorize a instalação pelo aplicativo usado para abrir o APK.
Depois siga a preparação das lentes no início deste guia.

Os testes de interação no Editor e a regressão das 28 fichas passaram.
Veja o [redesign e a validação atual](../artifacts/LOUVRE_REDESIGN.md), incluindo os limites
dos testes sem aparelho físico.

No Unity: **Museum > VR Box > Gerar APK Android**.
O resultado é gravado em `artifacts/MuseumVR_Build/MuseudaSemanaArteModerna.apk`
na raiz do repositório.

Teste de interação no Editor, a partir da raiz do repositório:

```sh
/Applications/Unity/Hub/Editor/2022.3.62f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -projectPath "$PWD/UnityProject" \
  -executeMethod MuseumModerna.VrValidation.Run \
  -logFile /tmp/museum-vr-validation.log
```

O teste encerra o Unity automaticamente; não use `-quit`. Verifica dwell,
paginação, leitura, fixação, menu e saída sem simular toques ou invocar cliques.

Fontes técnicas: [Cardboard v1.30.1](https://github.com/googlevr/cardboard-xr-plugin/releases/tag/v1.30.1),
[alteração de compatibilidade em v1.31.0](https://github.com/googlevr/cardboard-xr-plugin/releases/tag/v1.31.0),
[SDK1.30 e dependências Android](https://github.com/googlevr/cardboard/blob/v1.30.0/sdk/build.gradle),
[guia oficial Cardboard](https://developers.google.com/cardboard/develop/unity/quickstart).
O guia atual refere-se a Unity6; o projeto usa a versão compatível fixada acima.
