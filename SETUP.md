# Setup do Museu Virtual — Semana de Arte Moderna

Guia consolidado para aplicar, no Unity Editor, todas as melhorias de ambientação
descritas em `instrucao.md`. Todas as ferramentas ficam no menu **MuseumModerna**
da barra de menus do Editor (namespace `MuseumModerna`, `Assets/Editor/*.cs`).

> **STATUS (2026-07-11): TUDO JÁ FOI EXECUTADO AUTOMATICAMENTE.**
> O pipeline inteiro abaixo rodou via Unity batch mode (`MuseumModerna.MuseumBatchPipeline.RunAll`,
> em `Assets/Editor/MuseumBatchPipeline.cs`), incluindo bake de lightmaps, occlusion culling,
> LOD, texturas PBR, decoração, áudio e pós-processamento — sem erros — e o APK Android foi
> gerado com sucesso em `~/Desktop/MuseumVR_Build/MuseudaSemanaArteModerna.apk`.
> A cena `MuseumScene.unity` já está salva com tudo aplicado. O passo-a-passo abaixo fica como
> referência caso você precise reexecutar alguma etapa individualmente no Editor.
> Backup da cena/settings anteriores em `/tmp/museu_backup_20260711/`.
> O que resta é validação humana: instalar o APK num celular, andar pelo museu e conferir
> visual, desempenho (~60fps) e áudio. Objetos de decoração (bancos, plantas, tapetes,
> lixeiras, barreiras, placa) foram criados na ORIGEM da cena — reposicione-os no Editor.

## Pré-requisito

Abra o projeto (`UnityProject/`) no Unity **2022.3.62f1** pelo menos uma vez para
que os novos scripts compilem e recebam seus arquivos `.meta`. Corrija qualquer
erro de compilação que aparecer no Console antes de continuar (nenhum script novo
foi testado por compilação real).

## Ordem recomendada de execução

Abra `Assets/Scenes/MuseumScene.unity` e execute nesta ordem, salvando a cena
(`Ctrl+S`) depois de cada etapa:

1. **`MuseumModerna → Construir Museu Completo`** (`MuseumCompleteBuilder.cs`)
   Cria a geometria base (`Museum_Geometry`: Piso/Teto/Parede_*/Rodape_*), as 5
   salas, os quadros/esculturas/pedestais e uma iluminação básica inicial. Pule
   se a cena já tiver essa geometria.

2. **`MuseumModerna → Configurar Física da Sala`** (`RoomPhysicsSetup.cs`)
   Colliders e física do personagem sobre a geometria criada no passo 1.

3. **`MuseumModerna → Decoração do Museu`** (`MuseumDecorationSetup.cs`)
   Bancos, barreiras/cordões, placas, molduras 3D nos quadros, **e os itens
   novos: plantas, tapetes e lixeiras discretas**. Reposicione manualmente cada
   grupo criado (tudo nasce na origem, por design).

4. **`MuseumModerna → Texturas e Materiais PBR`** (`MuseumTextureSetup.cs`, novo)
   Gera texturas reais (Albedo/Normal/Metallic-Smoothness) para piso, parede,
   teto e rodapé em `Assets/Textures/`, cria materiais Standard em
   `Assets/Materials/` e aplica tudo nos objetos `Piso/Teto/Parede_*/Rodape_*`.
   Também pode adicionar uma faixa de friso no topo das paredes. Rode "FAZER
   TUDO" ou os 3 passos manualmente.

5. **Iluminação dramática — escolha UMA das duas opções, não rode as duas:**
   - `MuseumModerna → Iluminação Dramática do Museu` (`MuseumLightingSetup.cs`) —
     mais completa: ambiente quase preto, spotlights quentes por quadro
     (nome `Spotlight_<quadro>`, filhos do próprio quadro), fog, luzes de
     preenchimento com `CandleFlicker`.
   - Ou simplesmente manter a iluminação que o passo 1 (`Construir Museu
     Completo`) já criou (spotlights `Spot_<quadro>`, filhos de um root de
     iluminação separado, sem flicker).
   ⚠️ **Atenção — conflito conhecido, pré-existente**: essas duas ferramentas usam
   nomes e parents diferentes para os spotlights (`Spot_*` vs `Spotlight_*`), então
   **não são idempotentes entre si** — rodar as duas cria luzes duplicadas em cada
   quadro (uma de cada ferramenta). Escolha uma abordagem só. Recomendação: use
   `Iluminação Dramática do Museu`, é a mais completa (fog + flicker), e ignore os
   spotlights do passo 1 (delete os `Spot_*` se sobrarem).

6. **`MuseumModerna → Configurar Pós-processamento`** (`MuseumPostProcessSetup.cs`)
   Bloom, vignette, color grading, grain e SAO (Post Processing Stack v2 — o
   projeto usa Built-in Render Pipeline, não URP).

7. **`MuseumModerna → Áudio Ambiente (Gerar Clipes)`** (`MuseumAudioSetup.cs`, novo)
   Gera 4 variações de passos + 1 loop ambiente sintetizados (placeholders, não
   são gravações reais) em `Assets/Audio/*.wav` e conecta ao `AmbientAudioManager`
   já presente na cena.

8. **`MuseumModerna → Iluminação Avançada (Bake + Probes)`** (`MuseumAdvancedLightingSetup.cs`)
   Roda por último entre as ferramentas de iluminação/estrutura: marca a
   geometria como estática, cria Light Probes e Reflection Probe, configura
   sombras mobile e o subtarget ASTC. Depois de rodar, ainda é necessário um
   passo **manual**: `Window → Rendering → Lighting → Generate Lighting` para
   efetivamente assar (bake) os lightmaps.

9. **`MuseumModerna → Otimização Mobile/VR`** (`MuseumOptimizationSetup.cs`, novo)
   Roda por último de todos (depende de tudo acima já estar posicionado e
   marcado como estático):
   - Adiciona `LODGroup` em pedestais, bancos, barreiras, molduras e no teto.
   - Verifica/corrige as flags `Occluder/OccludeeStatic` e dispara o bake real
     de Occlusion Culling (`StaticOcclusionCulling.Compute()` — a API correta no
     2022.3; **não existe** `GenerateInEditor()`, então ignore qualquer
     referência a esse nome).
   - Diagnóstico de compressão ASTC em todas as texturas do projeto, com botão
     separado (exige confirmação) para aplicar ASTC em massa.

## Verificação manual obrigatória (dentro do Unity)

- Console sem erros de compilação após abrir o projeto.
- Cada ferramenta acima rodada e sem exceptions no Console.
- Entrar em Play mode: andar pela sala, checar spotlights (sem duplicar),
  texturas aplicadas, plantas/tapetes/lixeiras em posições plausíveis, áudio de
  passos e ambiente tocando.
- `Window → Rendering → Lighting → Generate Lighting` executado com sucesso.
- `Window → Rendering → Occlusion Culling → Visualization`: mover a câmera pelas
  salas e confirmar que objetos atrás de paredes desaparecem, e que nada some
  cedo demais (ajustar sliders de LOD em `Otimização Mobile/VR` se necessário).
- Build Android de teste, checar ~60fps.

Só depois dessa verificação visual vale a pena commitar o trabalho.

## Lacunas que continuam exigindo conteúdo externo real

- **Áudio**: os clipes gerados são sintetizados (placeholder). Trocar por
  gravações reais depois (sugestões já citadas nas próprias ferramentas:
  freesound.org para passos, musopen.org/freemusicarchive.org para música
  instrumental).
- **Texturas**: as texturas em `Assets/Textures/` são proceduralmente geradas
  (sem acesso à internet neste ambiente para baixar de Poly Haven/ambientCG).
  Servem como PBR funcional, mas podem ser substituídas por fotografadas/reais
  depois, mantendo os mesmos nomes de material em `Assets/Materials/`.
