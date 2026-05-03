# Draftosaurus — Cliente C# (WinForms)

Cliente do jogo **Draftosaurus** que consome a DLL `DraftServer.dll`. Implementa **Verão e Inverno** (detectados automaticamente), em modo **multi-jogador real**.

---

## ✨ Features

- 🎲 **Verão e Inverno** — o cliente detecta automaticamente qual lado a partida está usando (via `ListarCercados`)
- 🦕 **Silhuetas vetoriais** distintas para cada espécie (Braquiossauro, Espinossauro, Estegossauro, Parasaurolófo, Tiranossauro, Tricerátops) — sem precisar de PNG
- 🖼️ **PNGs opcionais** — coloque imagens em `Resources/dinos/{Codigo}.png` (Br.png, Ti.png etc.) e elas substituem as silhuetas
- ✨ **Animações** — dinossauros "caem" no cercado quando colocados
- 🔊 **Sons** procedurais (gerados na primeira execução, sem dependência de arquivos) — pode ser silenciado pelo botão 🔊/🔇
- 🏆 **Tela de pontuação detalhada** ao final, mostrando o cálculo etapa por etapa de cada jogador (`ListarPontuacao`)
- 👁️ **Visualização do tabuleiro de outros jogadores** (duplo-clique no nome na lista)
- 📜 **Histórico** da partida acessível durante o jogo
- 🔧 **Diagnóstico da DLL** integrado no Lobby pra verificar se a integração está OK

---

## 📋 Pré-requisitos

- **Visual Studio 2022** (Community ou superior) com workload "Desenvolvimento para desktop com .NET"
- **.NET 8 SDK**
- **Windows** (a DLL é Windows-only)

---

## 🚀 Como rodar

1. Abra o `DraftosaurusClient.csproj` no Visual Studio (ou `dotnet run`).
2. **F5**.
3. Lobby → criar partida ou entrar em uma existente
4. Sala de espera → qualquer jogador pode iniciar quando todos estiverem conectados
5. Jogo!

### Testar local sozinho

Abra **2-3 instâncias** do executável compilado (`bin/Debug/net8.0-windows/DraftosaurusClient.exe`):
1. Primeira → cria a partida
2. Demais → entram com nomes diferentes
3. Qualquer uma clica em "Iniciar"
4. Joga revezando entre as janelas

---

## 🗂 Estrutura do projeto

```
DraftosaurusClient/
├── DraftosaurusClient.csproj
├── Program.cs
│
├── libs/
│   ├── DraftServer.dll         ← DLL do professor
│   └── DraftServer.xml
│
├── Resources/                  ← (OPCIONAL) sobrescrever assets gerados
│   ├── dinos/                  ← coloque {Codigo}.png aqui (Br.png, Ti.png...)
│   └── sons/                   ← coloque novoturno.wav, colocar.wav etc.
│
├── Models/
│   ├── Dinossauro.cs           ← Br, Ep, Et, Pa, Ti, Tr + cores
│   ├── Cercado.cs              ← VERÃO + INVERNO (CercadosVerao / CercadosInverno)
│   ├── FaceDado.cs             ← AL, FL, PR, TI, VZ, WC
│   └── Partida.cs              ← Partida, Jogador, EstadoPartida, JogadaTurno
│
├── Services/
│   └── DraftService.cs         ← encapsula a DLL + DetectarLado + ListarPontuacao
│
├── Helpers/
│   ├── DllHelper.cs            ← conversão DataTable/DataSet
│   ├── DinoRenderer.cs         ← silhuetas vetoriais (ou PNG se houver)
│   └── SomHelper.cs            ← gera WAVs procedurais
│
├── Controls/
│   ├── TabuleiroControl.cs     ← Verão/Inverno + animação de queda
│   └── MaoControl.cs           ← cards com silhueta + badge de quantidade
│
└── Forms/
    ├── FormLobby.cs
    ├── FormCriarPartida.cs
    ├── FormSalaEspera.cs
    ├── FormJogo.cs             ← tela principal
    └── FormPontuacao.cs        ← detalhamento ao fim
```

---

## 🎮 Cercados

### Lado Verão

| Cód | Nome                  | Cap | Lado     | Lateral     |
|-----|-----------------------|-----|----------|-------------|
| FI  | Floresta da Igualdade | 6   | Floresta | Alimentação |
| MT  | Mata Tripla           | 3   | Floresta | Alimentação |
| RS  | Rei da Selva          | 1   | Floresta | Banheiros   |
| PA  | Pradaria do Amor      | 6   | Pradaria | Alimentação |
| CD  | Campina da Diferença  | 6   | Pradaria | Banheiros   |
| IS  | Ilha Solitária        | 1   | Pradaria | Banheiros   |
| RI  | Rio                   | 12  | (rio)    | (centro)    |

### Lado Inverno

| Cód | Nome                       | Cap | Lado     | Lateral     |
|-----|----------------------------|-----|----------|-------------|
| FB  | Floresta Bem Ordenada      | 6   | Floresta | Alimentação |
| PE  | Ponte dos Amantes (esq.)   | 6   | Floresta | Alimentação |
| PD  | Ponte dos Amantes (dir.)   | 6   | Pradaria | Banheiros   |
| PI  | Pirâmide                   | 6   | Pradaria | Alimentação |
| VG  | Vigia                      | 1   | Floresta | Banheiros   |
| QU  | Quarentena                 | 1   | Pradaria | Banheiros   |
| RI  | Rio                        | 12  | (rio)    | (centro)    |

> ⚠ Os códigos do Inverno são **chutes** baseados no manual. Se a DLL usar códigos diferentes, basta abrir `Models/Cercado.cs` e ajustar o dicionário `CercadosInverno`. Use o botão **"🔧 Diagnóstico DLL"** no Lobby pra ver os códigos reais. A detecção de "é inverno" procura qualquer um destes: `PI, FB, VG, QU, PE, PD` — se a DLL usar outros, ajuste também o método `DetectarLado` em `Services/DraftService.cs`.

---

## 🎨 Customizando as imagens

Para usar PNGs reais em vez das silhuetas vetoriais:

1. Crie a pasta `Resources/dinos/` ao lado do `.exe` (ou na raiz do projeto e marque como "Copy to Output Directory")
2. Adicione os arquivos: `Br.png`, `Ep.png`, `Et.png`, `Pa.png`, `Ti.png`, `Tr.png`
3. Tamanho recomendado: 128x128 ou 256x256, fundo transparente
4. Pronto — o `DinoRenderer` carrega automaticamente

Mesmo esquema para sons em `Resources/sons/`: `novoturno.wav`, `colocar.wav`, `fim.wav`, `erro.wav`.

---

## 🔄 Multi-jogador

A DLL é o "servidor lógico". Cada cliente:
1. Cria uma instância local de `DraftService` (que internamente usa `new Draft.Jogo()`)
2. Polling a cada 1,5s via `VerificarPartida` e `VerificarTurno`
3. Quando detecta mudança, atualiza UI e dispara animação

---

## ⚠ Pontos de atenção

1. **Senha do jogador**: gerada por `Entrar()` — anote!
2. **Lista de partidas só mostra abertas** — partidas em andamento somem do lobby
3. **Polling de 1,5s** — ajustável em `FormJogo.cs` (`_timer.Interval`)
4. **Se a DLL retornar formato inesperado**: use o botão Diagnóstico no Lobby

---

**Autor:** Fernando Castilho — Senac Santo Amaro
