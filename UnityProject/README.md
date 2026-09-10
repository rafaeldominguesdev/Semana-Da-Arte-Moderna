# Museu virtual — Semana de Arte Moderna de 1922

Unity 2022.3.62f1. Abra `Assets/Scenes/MuseumScene.unity` e pressione Play.
A cena contém 28 objetos com ficha própria: 24 painéis didáticos, três esculturas
cenográficas e uma vitrine documental. O arquivo Blender original foi preservado.

## VR Box com celular

O Android agora usa Cardboard para renderização estereoscópica e ajuste de lentes.
A preparação abre antes de entrar no visor. Dentro dele, os controles funcionam
pelo olhar, com páginas no lugar de rolagem. Consulte [uso e configuração do VR Box](VR_BOX.md).

## Visita em prévia 2D

- Direcione o centro da câmera a uma peça, a até 8 metros. A ficha aparece no canto inferior direito.
- Ao desviar, ela desaparece com fade; paredes e objetos sólidos bloqueiam a detecção.
- No computador, arraste com o botão direito para olhar. O movimento por olhar para baixo foi preservado.
- Clique diretamente numa peça para fixar sua ficha. `F` ou o botão **Fixar** fixa/libera a ficha atual.
- **Fechar** ou `Esc` fecha; a mesma ficha só volta depois de desviar e olhar novamente.
- Role a ficha com mouse/toque; `Page Up` e `Page Down` também percorrem o texto.
- **Opções da visita** permite desativar os painéis, ampliar o texto em três níveis,
  resumir a descrição, aumentar o contraste, prolongar a leitura por cinco segundos
  ou reduzir animações. Preferências são salvas localmente.
- No celular, use o giroscópio ou arraste se o sensor não estiver disponível.
  Um toque curto sobre a peça fixa sua ficha; arrastar não fixa.

A leitura não interrompe o caminhar. A interface respeita `Screen.safeArea`, oferece
rolagem e adapta a escala para retrato e paisagem. Botões usam navegação nativa uGUI
por teclado e estados de foco. Não foi implementada integração com leitor de tela do sistema.

## Conteúdo e autoria

As composições gráficas e os volumes 3D são criações cenográficas identificadas na
ficha e nas legendas. Não são reproduções das obras históricas nem são atribuídos
como originais aos artistas mencionados. As fichas distinguem o assunto histórico
do objeto criado para a visita. A arquitetura é própria, sem alegação de reconstrução do Theatro Municipal.

`Assets/Resources/MuseumCatalog.json` é o catálogo editorial revisado, com fontes
por ficha. `PaintingInfo` contém título, autor, período, técnica, categoria,
descrição, resumo, relação com 1922 e aviso de cenografia. Cada `PaintingExhibit`
associa um objeto a um `.asset` em `Assets/Resources/PaintingData`.

As fichas contextualizam Anita Malfatti, Di Cavalcanti, Victor Brecheret, Vicente do
Rego Monteiro, Zina Aita, Ferrignac, Villa-Lobos, Mário e Oswald de Andrade. Os
textos de 1924 e 1925 e o monumento inaugurado em 1953 estão marcados como posteriores.
Lasar Segall é apresentado como contexto, sem participação atribuída na Semana.
Não há Abaporu no percurso.

Fontes de base: [IEB-USP](https://anitamalfatti.ieb.usp.br/1921-1922/),
[MAM](https://mam.org.br/wp-content/uploads/2017/01/Release_AnitaMalfatti_MAM.pdf),
[MASP](https://masp.org.br/en/collections/works/1920s-women),
[Funarte](https://www.gov.br/funarte/pt-br/assuntos/noticias/todas-noticias/funarte-celebra-os-100-anos-da-semana-de-arte-moderna-e-realiza-eventos-ao-longo-de-2022)
e [Brasiliana-USP](https://www.brasiliana.usp.br/handle/bbm/9055).
As fontes específicas e ressalvas constam do catálogo.

## Manutenção

1. Edite `MuseumCatalog.json`.
2. Execute **MuseumModerna > Visita guiada > Atualizar fichas curatoriais**.
3. Para sincronizar as legendas e a cenografia, execute **Aplicar à cena do museu**.
   Esse comando abre e salva `MuseumScene`, substituindo apenas o grupo gerado
   `Museum_GuidedExhibition`. Salve alterações abertas antes de usá-lo.

Para uma nova peça, crie seu `PaintingInfo` e associe um `PaintingExhibit` ao objeto
ou ao pai dos seus colliders. Um `Collider` sólido é necessário. Filhos são
resolvidos pelo componente no ancestral; não é necessário colocar um Renderer
no objeto pai. Objetos na layer `Ignore Raycast` não são selecionados.

A referência de textura continua disponível em `PaintingInfo.paintingTexture`.
Ao substituir uma composição por reprodução histórica autorizada, revise também
o aviso e a proveniência da imagem. O instalador de cenografia gera composições
próprias e deve ser adaptado para preservar uma substituição manual.

## Implementação e validação

- `GazeDwellInteraction`: câmera, oclusão, clique/toque e publicação dos eventos existentes.
- `GuideFocusState`: estado único, fixação, fechamento, tempo de permanência e desativação.
- `MuseumGuidePanel` / `MuseumGuideTheme`: interface nativa, escala, animação e preferências.
- `GuidedMuseumSetup`: aplica a expografia sobre a cena existente; mantém bancos e arquitetura.
- `CuratorialCatalog`: sincroniza o JSON com os assets, preservando GUIDs e referências de mídia.

Validação automatizada no Unity (não requer instalação de pacote de testes):

```sh
/Applications/Unity/Hub/Editor/2022.3.62f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -projectPath "$PWD/UnityProject" \
  -executeMethod MuseumModerna.GuideValidation.Run \
  -logFile /tmp/museum-guide-validation.log
```

Execute a partir da raiz do repositório, sem outra instância do mesmo projeto.
Não use `-quit` nesta validação: ela entra em Play Mode e encerra automaticamente.
Verifica completude das fichas, transições de estado, oclusão, os 28 objetos da cena,
abertura/fechamento, fixação e navegação. Gera capturas desktop e retrato em `artifacts/`.
O teste usa a renderização do Unity; avaliação em aparelho físico continua necessária
para giroscópio, recortes de tela, conforto de leitura e desempenho móvel.
