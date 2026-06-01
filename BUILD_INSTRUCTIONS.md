# Como compilar e instalar o app VR no macOS (Sem Android Studio)

Este guia ensina como compilar o aplicativo Android diretamente pelo Terminal do macOS.

## Pré-requisitos
Você precisará do Java (JDK 17 recomendado) instalado.
1. Instale o Homebrew se não tiver:
   `/bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"`
2. Instale o JDK 17:
   `brew install openjdk@17`
3. Configure a variável de ambiente (adicione ao seu `~/.zshrc` ou `~/.bash_profile`):
   `export JAVA_HOME="/usr/local/opt/openjdk@17"`

## Passo 1: Baixar as ferramentas de linha de comando do Android
O Gradle baixará automaticamente o SDK necessário, mas é bom ter o Command Line Tools.
Se você não tem NADA do Android instalado:
1. Crie uma pasta para o SDK: `mkdir -p ~/Library/Android/sdk`
2. Exporte a variável de ambiente do SDK no terminal:
   `export ANDROID_HOME=$HOME/Library/Android/sdk`

## Passo 2: Executar o Build com o Gradle Wrapper
Este projeto vem com o Gradle Wrapper configurado (uma versão do Gradle embutida no projeto).
1. Abra o Terminal e vá para a pasta do projeto:
   `cd /Users/rafaeldomingues/SemanaArteModerna`
2. Dê permissão de execução ao script (se necessário):
   `chmod +x gradlew`
3. Compile o APK em modo de depuração (Debug):
   `./gradlew assembleDebug`

O Gradle fará o download do compilador (Android SDK Build-tools, Plataforma API 34, etc.).
O primeiro build pode demorar alguns minutos.

## Passo 3: Encontrar o APK gerado
Se o passo anterior terminar com `BUILD SUCCESSFUL`, o seu arquivo APK estará localizado em:
`app/build/outputs/apk/debug/app-debug.apk`

## Passo 4: Instalar no dispositivo Android
1. No seu celular Android, vá em **Configurações > Sobre o telefone** e toque 7 vezes em **Número da versão** (ou Build Number) para ativar o modo Desenvolvedor.
2. Vá em **Configurações > Opções do desenvolvedor** e ative a **Depuração USB**.
3. Conecte o celular ao Mac via cabo USB.
4. Para instalar, você precisa do comando `adb` (Android Debug Bridge). Ele costuma ficar em `~/Library/Android/sdk/platform-tools/adb`.
5. Execute no terminal:
   `~/Library/Android/sdk/platform-tools/adb install app/build/outputs/apk/debug/app-debug.apk`

## (Opcional) Adicionar as suas obras
Abra a pasta `/Users/rafaeldomingues/SemanaArteModerna/app/src/main/res/drawable/`. 
Você pode substituir o arquivo `obra_placeholder.xml` por arquivos de imagem reais (ex: `obra_abaporu.png`) e atualizar o código no `GalleryRenderer.kt` para carregar as texturas dessas imagens nos painéis 3D.
