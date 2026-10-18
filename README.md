# SAKURA X PANEL — Android C#

Painel flutuante funcional para Android, feito em C#/.NET 9. Não altera o Free Fire, não é aimbot ou ESP e não promete FPS adicional. Sem root, key, anúncios ou rede.

## Recursos reais

- Bolha flutuante arrastável e painel de controle que pode ser minimizado.
- Brilho do Android (necessita permissão para alterar configurações do sistema).
- Volume de mídia.
- Não Perturbe (necessita autorização específica).
- Perfis Jogo e Economia para brilho, volume e tela ligada.
- Bateria, temperatura da bateria e RAM livre.
- Configuração de permissões, persistência local e parada do serviço.

## Publicar e compilar inteiramente no Android (Termux)

1. Baixe e extraia o pacote do projeto na pasta `~/storage/downloads/SAKURA_X_PANEL_GITHUB_READY` (ou qualquer diretório em que o Termux possa ler os arquivos).
2. Instale o Termux atualizado e permita acesso aos arquivos usando `termux-setup-storage`.
3. Entre na pasta onde extraiu o pacote e execute `bash PUBLICAR_PELO_TERMUX.sh`.
4. Autorize o GitHub CLI no navegador. O script usará **sua própria conta** para enviar os arquivos para [Sakura-x-panel](https://github.com/gaaara9990808-hue/Sakura-x-panel).
5. Abra [Actions](https://github.com/gaaara9990808-hue/Sakura-x-panel/actions) e aguarde o workflow **Android APK - Sakura X Panel**.
6. Se o workflow concluir com sucesso, abra o run e baixe o artifact **Sakura-X-Panel-APK**. Extraia o APK e instale pelo Android.

O APK é de desenvolvimento (Debug). Instale apenas o pacote produzido pelo seu próprio repositório.

## Permissões

A sobreposição é necessária para a bolha. Outras permissões são opcionais; o aplicativo pede autorização nas páginas de configurações do Android. No Android 13+, notificações podem exigir permissão adicional.

## Limites conhecidos

- O painel pode sobrepor apps que permitem overlays; alguns jogos bloqueiam sobreposições.
- Brilho definido manualmente pode desativar brilho automático (reconfigure nas Configurações do Android).
- O controle Não Perturbe varia entre fabricantes.
- A compilação em GitHub Actions não foi validada até que haja uma execução bem-sucedida no repositório.
- O aplicativo não injeta comandos, altera sensibilidade ou interage com a memória do Free Fire.
