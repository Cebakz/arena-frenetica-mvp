# Arena Frenética — MVP

Um projeto Unity 6 novo e separado dos protótipos anteriores. A primeira versão usa o mapa limpo e as artes originais fornecidas pelo criador. O cenário é uma imagem de alta resolução; heróis, torres, bases, vida e colisões são elementos independentes.

**Jogue no navegador:** https://cebakz.github.io/arena-frenetica-mvp/

## Jogar

Abra `docs/index.html` por um servidor HTTP ou acesse a versão publicada no GitHub Pages. No computador, use WASD/setas, Espaço para atacar, Q para avançar e E para usar o poder. No celular, use o controle à esquerda e os três botões à direita.

## O que está implementado

- Menu inicial, seleção entre Brutus, Lyra, Nix e Sol, pausa e telas de vitória e derrota.
- Partida de três minutos contra um bot, com duas rotas, ondas automáticas de unidades, torres e bases com vida e dano.
- Ataque básico, avanço e poder por personagem, com recarga.
- Bloqueio das margens e da área aquática; travessia pelas rotas laterais.
- Interface vertical para celular e compilação Web sem dependência de configuração de compressão no servidor.

## Limites desta versão

É uma versão inicial para teste com jogadores. Ainda faltam animações de combate, som, progressão, contas, pareamento online, dragão e polimento de equilíbrio. A arte do mapa é fixa nesta versão; as estruturas e unidades continuam separadas para permitir evolução das regras sem refazer o cenário imediatamente. Teste em aparelhos reais antes de uma divulgação ampla.

## Abrir no Unity

Use o Unity 6000.3.24f1 e abra esta pasta como projeto. A cena principal é `Assets/Scenes/Arena.unity`. O código está em `Assets/ArenaGame.cs`. A classe `BuildMvp` em `Assets/Editor/BuildMvp.cs` recria a cena e gera as versões Windows e Web.

## Referência de desenvolvimento

Para aprofundar o trabalho com sprites e composição 2D, o projeto oficial [Unity 2D Tech Demos](https://github.com/Unity-Technologies/2d-techdemos) é uma referência com licença MIT. Este MVP foi escrito em um projeto novo; não foi copiado de um clone de Clash Royale.

As artes incluídas são do projeto Arena Frenética e não fazem parte da licença do repositório de referência.
