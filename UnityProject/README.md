# Museu da Semana de Arte Moderna 3D
## Unity Setup Guide — Versão 2022.3 LTS

---

## 📁 Estrutura do Projeto Unity

```
Assets/
  Scripts/
    Player/
      GyroscopeController.cs   ← Controle de câmera por giroscópio
      HeadGazeMovement.cs      ← Movimento ao olhar para baixo
      PlayerController.cs      ← Script mestre (estado + detecção de quadros)
    Museum/
      ExhibitManager.cs        ← Gerencia quadros, highlighting, progresso
      PaintingInfo.cs          ← ScriptableObject com dados de cada obra
    UI/
      UIManager.cs             ← Painel de info, crosshair, seta de gaze
    Utils/
      GyroCalibration.cs       ← Salva/carrega calibração via PlayerPrefs
  Models/
    versao0.2_sModerna.fbx     ← Modelo exportado do Blender (veja abaixo)
  Resources/
    PaintingData/              ← Arquivos .asset de cada PaintingInfo
  Materials/
  Prefabs/
  Scenes/
```

---

## 🎨 Exportando o Blender para Unity

### Arquivo: `versao0.2_sModerna.blend`

### Passo 1 — Preparar no Blender

1. Abra o arquivo `versao0.2_sModerna.blend`
2. Selecione **tudo** com `A`
3. Aplique transformações: `Ctrl+A` → **All Transforms**
4. Verifique se as normais estão corretas: Overlay → Face Orientation (azul = frente)

### Passo 2 — Exportar como FBX

No Blender, vá em: **File → Export → FBX (.fbx)**

**Configurações obrigatórias:**

| Configuração | Valor |
|---|---|
| **Scale** | `1.00` |
| **Apply Scalings** | `FBX All` |
| **Forward** | `-Z Forward` |
| **Up** | `Y Up` |
| **Apply Unit** | ✅ Ligado |
| **Apply Transform** | ✅ Ligado |
| **Mesh → Smoothing** | `Face` |
| **Mesh → Tangent Space** | ✅ Ligado |
| **Armature** | Desligado (sem rig) |
| **Bake Animation** | Desligado |

Salve como: `Assets/Models/versao0.2_sModerna.fbx`

### Passo 3 — Configurar no Unity

1. Clique no FBX importado no Project
2. No Inspector, aba **Model**:
   - Scale Factor: `1`
   - Mesh Compression: `Off`
   - Generate Colliders: ✅ (para as paredes)
3. Aba **Materials**:
   - Material Creation Mode: `Import via MaterialDescription`
   - Clique **Extract Textures** e **Extract Materials**

---

## 🎮 Configuração de Layers

Crie estas layers em **Edit → Project Settings → Tags and Layers**:

| Layer # | Nome | Uso |
|---|---|---|
| 6 | `Player` | O GameObject do player |
| 7 | `Wall` | Paredes e objetos sólidos |
| 8 | `Painting` | Quadros interativos |
| 9 | `Interactive` | Outros objetos interativos |

**Configuração de colisões** (Physics Matrix):
- `Player` NÃO colide com `Painting` (passa reto pelos quadros)
- `Player` COLIDE com `Wall`

---

## 🏗️ Hierarquia de GameObjects na Cena

```
Scene
├── [Lighting]
│   ├── Directional Light
│   └── Point Lights (nos quadros)
│
├── Museum
│   └── versao0.2_sModerna (FBX importado)
│       ├── Sala (Mesh + MeshCollider, Layer: Wall)
│       ├── Quadro_Abaporu (Layer: Painting)
│       │   └── → componente: PaintingExhibit → PaintingData: Abaporu.asset
│       ├── Quadro_Estudante (Layer: Painting)
│       └── ... (outros quadros)
│
├── Player  ← Tag: "Player", Layer: Player
│   ├── CharacterController (Height: 1.7, Radius: 0.3)
│   ├── PlayerController.cs
│   ├── HeadGazeMovement.cs
│   └── CameraRig  ← Pivot da câmera (Y offset: 1.6)
│       └── Main Camera
│           └── GyroscopeController.cs
│
├── Managers
│   ├── ExhibitManager.cs
│   └── UIManager (Canvas)
│
└── Canvas (Screen Space Overlay, CanvasScaler: Scale With Screen Size 1080x1920)
    ├── Crosshair (Image, centro da tela, 20x20px)
    ├── GazeIndicator
    │   └── ArrowImage (↓)
    └── PaintingPanel (CanvasGroup)
        ├── Background (Image, semi-transparente)
        ├── ThumbnailImage
        ├── TitleText (TextMeshPro)
        ├── ArtistText (TextMeshPro)
        ├── YearText (TextMeshPro)
        ├── DescriptionText (TextMeshPro, com ScrollRect)
        ├── CloseButton
        └── CalibrateButton
```

---

## ⚙️ Configuração da Câmera para Giroscópio

1. O **Player** GameObject tem `CharacterController`
2. A câmera está em um filho chamado `CameraRig`
3. `CameraRig.localPosition = (0, 1.6, 0)` — altura dos olhos
4. `GyroscopeController` fica na **Main Camera** (não no Player raiz)
5. `HeadGazeMovement` usa `cameraTransform` para a direção de movimento

**Importante:** NÃO rotacione o Player GameObject — apenas a câmera gira.
O movimento sempre vai para onde a câmera aponta (projetado no plano XZ).

---

## 📱 Configurações do Build Android

**Edit → Project Settings → Player → Android:**

| Configuração | Valor |
|---|---|
| Minimum API Level | `API 26 (Android 8.0)` |
| Target API Level | `API 34` |
| Scripting Backend | `IL2CPP` |
| ARM64 | ✅ |
| Internet Access | `Not Required` |
| Write Permission | `External (SDCard)` — apenas se necessário |

**Edit → Project Settings → Quality:**
- Use o perfil "Medium" para mobile
- Disable VSync (use Application.targetFrameRate = 60)

---

## 🗝️ PlayerPrefs Keys Documentadas

| Chave | Tipo | Descrição |
|---|---|---|
| `MuseumModerna_CalibX` | float | Quaternion X do offset de calibração |
| `MuseumModerna_CalibY` | float | Quaternion Y do offset de calibração |
| `MuseumModerna_CalibZ` | float | Quaternion Z do offset de calibração |
| `MuseumModerna_CalibW` | float | Quaternion W do offset de calibração |

---

## 🖼️ Criando Dados de Obras (PaintingInfo)

Para cada quadro na cena:

1. Botão direito na pasta `Assets/Resources/PaintingData`
2. **Create → Museum → Painting Info**
3. Preencha: título, artista, ano, descrição
4. Arraste a textura da obra para `paintingTexture`
5. No GameObject do quadro na cena, adicione o componente `PaintingExhibit`
6. Arraste o `.asset` criado para o campo `PaintingData`
7. Coloque o quadro na **Layer: Painting**
8. Adicione um `BoxCollider` ao quadro (para detecção por OverlapSphere)

---

## 🎨 Obras Pré-configuradas — Semana de Arte Moderna 1922

| Arquivo Asset | Obra | Artista | Ano |
|---|---|---|---|
| `Abaporu.asset` | O Abaporu | Tarsila do Amaral | 1928 |
| `Estudante.asset` | A Estudante | Anita Malfatti | 1915 |
| `Autorretrato.asset` | Autorretrato | Anita Malfatti | 1923 |
| `OperariasPortinari.asset` | Operários | Cândido Portinari | 1934 |
| `CarnavalEstacio.asset` | Carnaval em Madureira | Di Cavalcanti | 1924 |

---

## 🧪 Testando no Editor (Sem Giroscópio)

O `GyroscopeController` detecta automaticamente se há giroscópio:
- **Com giroscópio (Android):** usa `Input.gyro`
- **Sem giroscópio (Editor/PC):** usa mouse (botão direito) para rotar câmera

Para testar o head-gaze walking no Editor:
1. Clique com o **botão direito** e arraste para baixo
2. Quando o pitch atingir -30° → player começa a andar
3. Observe o Gizmo vermelho no Scene View

---

## 🚀 Build e Deploy

```bash
# Na pasta do projeto Unity (via terminal)
# Use Unity Hub ou linha de comando:

# Build Android (APK)
/Applications/Unity/Hub/Editor/2022.3.x/Unity.app/Contents/MacOS/Unity \
  -batchmode \
  -quit \
  -projectPath /caminho/para/UnityProject \
  -buildTarget Android \
  -executeMethod BuildScript.BuildAndroid

# Instalar no dispositivo
adb install -r build/SemanaArteModerna.apk
```

---

## 🔧 Solução de Problemas

| Problema | Solução |
|---|---|
| Câmera gira ao contrário | Em GyroscopeController.cs, inverta o sinal: `new Quaternion(-raw.x, -raw.y, raw.z, raw.w)` |
| Player atravessa paredes | Verifique se as paredes estão na Layer `Wall` e se o CharacterController está configurado |
| Quadros não são detectados | Verifique se os quadros estão na Layer `Painting` e têm Collider |
| Giroscópio não calibra | Segure o celular em posição neutra e pressione "Calibrar" |
| Painel UI não aparece | Verifique se UIManager tem todas as referências no Inspector |
