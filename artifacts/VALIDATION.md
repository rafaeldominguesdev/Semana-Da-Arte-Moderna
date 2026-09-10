# Validação — visita guiada

Executada no Unity 2022.3.62f1 em 7 de setembro de 2026.

| Verificação | Resultado |
|---|---|
| Compilação dos scripts | Sem erros de C# |
| Catálogo | 28 fichas completas e IDs distintos |
| Estado da ficha | 12 cenários aprovados |
| Detecção na cena | 28 objetos alcançados por raycast de aproximação |
| Oclusão | Parede inserida entre câmera e obra bloqueou a seleção |
| Ciclo de interface | Abrir, fade-out, fixar, liberar e fechar aprovados em Play Mode |
| Navegação | Estado Walking mantido durante mediação |
| Acessibilidade | Texto ampliado, resumo e fundo de alto contraste aplicados |
| Renderização | Capturas 1440×900, 390×844 e 844×390 inspecionadas |
| Higiene do diff | `git diff --check` aprovado |

[Log resumido](validation.log).

## Capturas do Unity

- [Desktop](museum-guide-desktop.png)
- [Celular em retrato](museum-guide-mobile.png)
- [Celular em paisagem](museum-guide-landscape.png)
- [Texto ampliado e alto contraste](museum-guide-accessibility.png)
- [Galeria de esculturas](museum-sculpture-gallery.png)

As capturas usam a câmera real e o Canvas uGUI em RenderTexture nas resoluções
indicadas. Não representam execução em aparelho físico.

## Contraste calculado

Cálculo WCAG de luminância sRGB dos tokens. Para o painel semitransparente foi
considerado o fundo branco atrás da superfície, o caso mais claro.

| Par | Razão |
|---|---:|
| Texto / superfície | 15,13:1 |
| Texto bronze / superfície | 8,35:1 |
| Texto / botão | 12,86:1 |
| Branco / preto no modo de alto contraste | 21:1 |

## Limites e observações

- Giroscópio, toque real, recortes de tela e desempenho não foram testados em aparelho físico.
- O projeto já emitia avisos sobre Renderers cadastrados em LODGroups sobrepostos.
  Esses avisos permanecem e não impediram os testes. Não houve novo bake de lightmaps.
- Os arquivos de referência e os scripts de validação web mencionados pela skill
  `design-code` não estão instalados no caminho indicado. Esses gates não foram
  executados. Foram usadas verificações nativas de uGUI, capturas do Unity e cálculo
  de contraste; isso não equivale a uma auditoria completa de acessibilidade.
- Há navegação nativa por teclado; não foi adicionada integração de leitor de tela do sistema.
- A consulta de fontes foi usada para revisar contexto e cronologia. As imagens e
  esculturas são cenografia própria, com avisos explícitos, não reproduções documentais.
