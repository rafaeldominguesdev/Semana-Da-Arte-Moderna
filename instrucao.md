


Tenho um projeto Unity (C#) de museu virtual em VR para a Semana de Arte Moderna, já funcional (sala básica, giroscópio ok, física do personagem já configurada). Agora preciso MELHORAR AO MÁXIMO A AMBIENTAÇÃO da sala, sem refazer a estrutura existente. Foque em:

1. ILUMINAÇÃO E SOMBRAS
- Configurar Lighting (Window > Rendering > Lighting): ambient lighting, skybox adequado para ambiente interno de museu.
- Adicionar luzes pontuais/spotlights direcionados às obras/pedestais, com temperatura de cor quente (simulando iluminação de galeria, ~3000-4000K).
- Habilitar e configurar sombras em tempo real (Shadow Type: Soft Shadows) em todas as luzes e objetos relevantes.
- Configurar Lightmapping/baked lighting para otimizar performance no mobile, mantendo qualidade visual.
- Ajustar Light Probes e Reflection Probes para objetos dinâmicos receberem iluminação correta.

2. TEXTURAS E MATERIAIS
- Aplicar texturas realistas em piso (ex: madeira, mármore ou porcelanato), paredes (textura lisa/pintura com leve relevo) e teto.
- Usar materiais PBR (com mapas de Albedo, Normal, Roughness/Metallic) otimizados para mobile (texturas comprimidas, resolução adequada ~1024x1024 ou 2048x2048).
- Adicionar variação de material nas paredes (ex: rodapé, moldura decorativa) para evitar aparência "lisa demais".

3. OBJETOS DE AMBIENTAÇÃO
- Adicionar elementos decorativos típicos de museu: bancos/assentos no centro da sala, lixeiras discretas, plantas decorativas, tapetes/carpetes direcionais, cordões/correntes de isolamento próximos às obras, placas de sinalização (saída, setas de direção).
- Adicionar pedestais com acabamento realista para esculturas/obras.
- Adicionar molduras detalhadas nos quadros (com profundidade/relevo, não apenas planas).

4. DETALHES DE PROFUNDIDADE E REALISMO
- Adicionar pequenas imperfeições: sombras de contato (contact shadows) sob objetos, leve ambient occlusion.
- Configurar Post-Processing (URP Volume): Bloom sutil, Color Grading (tons levemente quentes), Vignette leve, Ambient Occlusion, Anti-aliasing (para suavizar bordas no mobile).
- Adicionar áudio ambiente (eco leve de passos, música instrumental de fundo baixa volume).

5. OTIMIZAÇÃO PARA MOBILE/VR
- Garantir que todas as melhorias mantenham performance estável (60fps): usar LOD (Level of Detail) em objetos complexos, baked lighting ao invés de real-time onde possível, texturas comprimidas (ASTC), occlusion culling configurado.

6. INSTRUÇÕES PRÁTICAS
- Passo a passo de configuração no Editor (onde clicar, quais settings ajustar no URP/Lighting/Quality).
- Sugestão de assets gratuitos da Unity Asset Store ou texturas free (ex: Poly Haven, ambientCG) compatíveis com a temática de museu/Semana de Arte Moderna.
- Código C# (se necessário) para scripts de luz dinâmica, troca de skybox ou efeitos ambientais.

Restrição: manter compatibilidade com o projeto mobile/VR já existente, sem comprometer performance. Entregue tudo de forma organizada e aplicável diretamente no projeto atual.