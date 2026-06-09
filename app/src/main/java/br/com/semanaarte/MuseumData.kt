package br.com.semanaarte

/**
 * Dados de uma obra de arte da Semana de Arte Moderna de 1922.
 *
 * @param id         Identificador único da obra (usado para rastrear progresso)
 * @param title      Título da obra
 * @param artist     Nome do artista
 * @param year       Ano de criação
 * @param movement   Movimento artístico (ex: Modernismo)
 * @param description Descrição detalhada exibida no painel de informações
 * @param funFact    Curiosidade sobre a obra (opcional)
 * @param drawableRes ID do drawable com a imagem da obra (ex: R.drawable.obra_abaporu)
 * @param angleInGallery Ângulo (graus) desta obra no arranjo circular da galeria
 */
data class Artwork(
    val id: String,
    val title: String,
    val artist: String,
    val year: Int,
    val movement: String,
    val description: String,
    val funFact: String = "",
    val drawableRes: Int = 0,
    val angleInGallery: Float = 0f
)

/**
 * Catálogo completo das obras exibidas no museu virtual.
 * Cada obra é associada a um ângulo no arranjo circular da galeria (0° a 360°).
 */
object MuseumCatalog {

    /** Lista de todas as obras do museu */
    val artworks: List<Artwork> = listOf(

        Artwork(
            id = "abaporu",
            title = "O Abaporu",
            artist = "Tarsila do Amaral",
            year = 1928,
            movement = "Antropofagismo",
            description = "Pintado como presente de aniversário para Oswald de Andrade, " +
                    "O Abaporu tornou-se o símbolo máximo do Modernismo brasileiro. " +
                    "A palavra 'Abaporu' vem do tupi: 'homem que come gente'. " +
                    "A figura tem membros desproporcionais, com pés enormes fincados " +
                    "na terra e uma pequena cabeça sob um sol escaldante.",
            funFact = "Esta obra inspirou o Manifesto Antropófago de Oswald de Andrade " +
                    "e hoje é a pintura brasileira mais valiosa do mundo.",
            angleInGallery = 0f
        ),

        Artwork(
            id = "estudante",
            title = "A Estudante",
            artist = "Anita Malfatti",
            year = 1915,
            movement = "Expressionismo",
            description = "Pintada durante os estudos de Anita Malfatti na Alemanha, " +
                    "A Estudante demonstra a influência do Expressionismo alemão " +
                    "com suas cores vibrantes e distorções intencionais da forma. " +
                    "A obra foi exibida na polêmica exposição de 1917, que dividiu " +
                    "opiniões e marcou o início do Modernismo no Brasil.",
            funFact = "Monteiro Lobato criticou ferozmente esta obra no artigo " +
                    "'Paranoia ou Mistificação?', o que paradoxalmente impulsionou " +
                    "o interesse pelo Modernismo brasileiro.",
            angleInGallery = 72f
        ),

        Artwork(
            id = "operarios",
            title = "Operários",
            artist = "Tarsila do Amaral",
            year = 1933,
            movement = "Modernismo Social",
            description = "Uma das obras mais importantes do período social de Tarsila, " +
                    "Operários retrata um mosaico de rostos de trabalhadores de diversas " +
                    "etnias — reflexo da São Paulo industrializada dos anos 1930. " +
                    "A composição geométrica e as cores terrosas remetem às raízes " +
                    "do povo brasileiro.",
            funFact = "Tarsila doou esta obra ao governo do estado de São Paulo, " +
                    "onde está exposta até hoje no Palácio Boa Vista em Campos do Jordão.",
            angleInGallery = 144f
        ),

        Artwork(
            id = "carnaval",
            title = "Carnaval em Madureira",
            artist = "Di Cavalcanti",
            year = 1924,
            movement = "Modernismo",
            description = "Di Cavalcanti foi um dos principais artistas da Semana de Arte " +
                    "Moderna de 1922 e até criou a capa do catálogo do evento. " +
                    "Em Carnaval em Madureira, retrata a alegria popular do Rio de Janeiro " +
                    "com traços expressivos e cores quentes. A obra celebra a cultura " +
                    "afro-brasileira e a festiva identidade carioca.",
            funFact = "Di Cavalcanti foi o principal organizador da Semana de 1922 " +
                    "ao lado de Mário de Andrade e Graça Aranha.",
            angleInGallery = 216f
        ),

        Artwork(
            id = "abolicao",
            title = "A Negra",
            artist = "Tarsila do Amaral",
            year = 1923,
            movement = "Pau-Brasil",
            description = "Considerada um autorretrato simbólico, A Negra é uma das primeiras " +
                    "obras do período Pau-Brasil de Tarsila. A figura monumental, de lábios " +
                    "proeminentes e seios expostos, foi inspirada nas memórias de infância " +
                    "da artista na fazenda da família, onde conviveu com mulheres negras. " +
                    "A obra é um marco da valorização da identidade brasileira.",
            funFact = "A Negra foi exibida em Paris em 1926, onde causou grande impressão " +
                    "entre os artistas modernistas europeus, incluindo Fernand Léger.",
            angleInGallery = 288f
        )
    )

    /**
     * Retorna uma obra pelo seu ID único.
     * @param id ID da obra (ex: "abaporu")
     * @return A obra correspondente, ou null se não encontrada.
     */
    fun findById(id: String): Artwork? = artworks.find { it.id == id }

    /**
     * Retorna o índice de uma obra na lista pelo seu ID.
     */
    fun indexOfId(id: String): Int = artworks.indexOfFirst { it.id == id }
}
