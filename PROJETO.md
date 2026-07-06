# match-libras

Jogo mobile infantil que ensina, de forma lúdica, a associação entre objetos, números e sinais de Libras (Língua Brasileira de Sinais).

**Público-alvo:** Crianças com deficiência auditiva.

**Unity:** 6000.3.11f1

---

## Estrutura de Cenas

| Cena | Função |
|------|--------|
| `Inicio` | Tela inicial / menu principal |
| `Jogo` | Jogo principal com fases completas |
| `Tutorial` | Cópia do Jogo, preparada para receber o sistema de tutorial |

Fluxo: `Inicio` → `Jogo` (atualmente o tutorial scene não é carregado automaticamente).

---

## Arquitetura dos Scripts

### Gerenciadores (Singletons)

| Script | Instância | Persistente? | Função |
|--------|-----------|--------------|--------|
| `SoundManager` | `SoundManager.Instance` | `DontDestroyOnLoad` | Toca efeitos sonoros por ID |
| `MusicManager` | `MusicManager.Instance` | `DontDestroyOnLoad` | Música de fundo com fade in/out |
| `Cronometro` | `Cronometro.Instance` | Não (um por cena) | Cronômetro crescente na HUD |
| `ScreenShake` | `ScreenShake.Instance` | Não (na câmera) | Efeito de tremor de câmera |

### Gerenciador de Jogo (Não-Singleton)

| Script | Onde está | Função |
|--------|-----------|--------|
| `LevelManager` | GameObject "Level Manager" na cena | Orquestra rodadas: spawn, vitória, derrota, vidas |

### Componentes de Peça

| Script | Componente em | Função |
|--------|---------------|--------|
| `DragDrop` | `Peça` (prefab) | Arrastar, clicar, auto-encaixar, animações de escala/sombra |
| `PecaFisica` | `Peça` (prefab) | Simulação física: repulsão entre peças, colisão com bordas |
| `ItemSlot` | Slots `Numero` e `Comidas` na cena | Recebe drop, valida se a peça é correta, organiza grid |

### Outros

| Script | Função |
|--------|--------|
| `PecaData` | Enums: `TipoPeca` (Numero, Comida), `ValorNumero` (0-9), `ValorComida` (Uva, Hamburger, ...) |
| `SceneLoader` | Carrega cenas por nome |
| `PauseMenu` | Pausa/retoma o jogo (tecla Escape) |
| `CameraScaler` | Ajusta câmera ortográfica para largura fixa |
| `TileFlipTransitionController` | Transição de cena com efeito tile-flip |
| `IntroAnimator` | Animação de entrada na tela inicial |
| `AudioSettingsController` | Sliders de volume música/SFX com persistência |
| `UIAudioButton` | Botão que toca som resolvendo singleton em tempo real |
| `NumbersSO` | ScriptableObject com sprites de números (não usado em código) |

---

## Fluxo do Jogo

### 1. Início (`LevelManager.Start`)

1. Embaralha números 1 a 9
2. Chama `SpawnarRodada()` após 2.5s

### 2. Cada Rodada (`SpawnarRodada`)

1. Pega o primeiro número da fila (`numeros[0]`)
2. Mostra o sinal de Libras correspondente (`spriteRenderer.sprite = LibraSprites[number]`)
3. Escolhe uma comida aleatória (`ValorComida`)
4. **Peças necessárias**: 1 número correto + N comidas corretas (N = número atual)
5. **Distratores**: números diferentes + comidas de outros tipos
6. Spawna tudo com animação caótica (entrada com rotação e offset)

### 3. Interação do Jogador

- **Arrastar**: `DragDrop.OnBeginDrag` → pausa física, eleva sorting order, escala
- **Soltar no slot**: `ItemSlot.OnDrop` → valida tag + tipo + slot vazio
  - **Acerto**: peça é aceita, `blocksRaycasts = false`, som de drop
  - **Erro**: `RejeitarPeca` (anima volta + som)
- **Duplo clique**: `DragDrop.OnPointerClick(clickCount == 2)` → auto-encaixa no slot correto

### 4. Validação (`VerificarVitoria`)

Chamada por um botão "Verificar" na UI.

- Percorre todos `ItemSlot`
- Se algum não estiver completo → `ExpulsarSlots()` (perde vida, shake, eject)
- Se todos completos → `AvancarRodada()` (remove número, spawna próxima)

### 5. Fim de Jogo

- **Vitória**: todos os 9 números concluídos → painel de vitória com tempo
- **Derrota**: 3 vidas perdidas → painel de derrota

---

## Estrutura do Prefab "Peça"

```
Peça (Layer: UI, Tag: "Numero")
├── DragDrop
├── PecaFisica
├── CanvasGroup
├── Dust (ParticleSystem) — brilho ao acertar
├── Sombra (Image) — sombra projetada
└── Canvas (World Space)
    ├── Tipo (Image) — sprite do número ou comida
    └── Base (Image, desativado)
```

A tag é alterada para `"Comida"` em tempo de execução conforme o tipo.

---


## Sprites

- **Números**: `Resources/Sprites/Numbers/` (sprites 0-9)
- **Comidas**: `Resources/Sprites/Comidas/` (Uva, Hamburger, Sorvete, etc.)
- **Libras**: Referenciados diretamente no Inspector do LevelManager (`LibraSprites`)
- **Mascote**: `Resources/Sprites/mascote_lino*` (normal, piscando, triste)

---

## PlayerPrefs

| Chave | Tipo | Onde é usado |
|-------|------|-------------|
| `VolumeMusica` | float | `MusicManager.Start`, `AudioSettingsController` |
| `VolumeEfeitos` | float | `AudioSettingsController` |
| `TutorialCompleto` | int (0/1) | `TutorialManager.CompletarTutorial` |

---

## Como Adicionar Novos Conteúdos

### Novo número
1. Adicione sprite em `Resources/Sprites/Numbers/`
2. Adicione sprite de Libras ao Inspector do `LevelManager.LibraSprites`
3. Adicione sprite numérico ao Inspector do prefab `Peça` → `DragDrop.NumeroSprites`

### Nova comida
1. Adicione sprite em `Resources/Sprites/Comidas/`
2. Adicione novo valor ao enum `ValorComida` em `PecaData.cs`
3. Adicione sprite ao Inspector do prefab `Peça` → `DragDrop.ComidasSprites`

### Nova fase/mecânica
- Crie um novo `TutorialStep` na lista do `TutorialManager` no Inspector
- A ordem dos steps define a progressão do tutorial
